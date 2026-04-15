using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Pretext;

public static partial class PretextLayout
{
    private static bool TryEnumerateLocaleAwareWordSegments(string text, out List<WordSegment> segments)
    {
        var locale = GetEffectiveSegmenterLocale();
        return IcuWordSegmenter.TrySegment(text, locale, out segments);
    }

    private static string? GetEffectiveSegmenterLocale()
    {
        if (!string.IsNullOrWhiteSpace(_locale))
        {
            return _locale;
        }

        var currentLocale = CultureInfo.CurrentCulture.Name;
        return string.IsNullOrWhiteSpace(currentLocale) ? null : currentLocale;
    }

    private static class IcuWordSegmenter
    {
        private const int WordBoundaryType = 1;
        private const int BreakDone = -1;
        private const int MinSupportedIcuVersion = 50;
        private const int MaxSupportedIcuVersion = 100;

        private static readonly DllImportSearchPath NativeLibrarySearchDirectories =
              DllImportSearchPath.ApplicationDirectory
            | DllImportSearchPath.AssemblyDirectory
            | DllImportSearchPath.UserDirectories;

        private static readonly object Gate = new();
        private static readonly Dictionary<Type, object> LookupCache = new();

        private static bool _initialized;
        private static bool _available;
        private static IntPtr _icuLibrary;
        private static int _icuVersion;
        private static bool _commonDataInitialized;
        private static IntPtr _commonDataBuffer;

        internal static bool TrySegment(string text, string? locale, out List<WordSegment> segments)
        {
            segments = [];
            if (string.IsNullOrEmpty(text))
            {
                return true;
            }

            if (!EnsureInitialized())
            {
                return false;
            }

            IntPtr localePtr = IntPtr.Zero;
            IntPtr textPtr = IntPtr.Zero;
            IntPtr breakIterator = IntPtr.Zero;

            try
            {
                if (!string.IsNullOrWhiteSpace(locale))
                {
                    localePtr = MarshalCompat.StringToCoTaskMemUtf8(locale!);
                }

                textPtr = Marshal.StringToHGlobalUni(text);
                breakIterator = GetMethod<ubrk_open>()(WordBoundaryType, localePtr, textPtr, text.Length, out var status);
                CheckErrorCode<ubrk_open>(status);

                GetMethod<ubrk_first>()(breakIterator);
                var previous = 0;
                while (GetMethod<ubrk_next>()(breakIterator) is var next && next != BreakDone)
                {
                    if (next > previous)
                    {
                        var segmentText = text.Substring(previous, next - previous);
                        segments.Add(new WordSegment(segmentText, IsWordLikeText(segmentText), previous));
                    }

                    previous = next;
                }

                if (previous < text.Length)
                {
                    var segmentText = text.Substring(previous);
                    segments.Add(new WordSegment(segmentText, IsWordLikeText(segmentText), previous));
                }

                return true;
            }
            catch
            {
                segments = [];
                return false;
            }
            finally
            {
                if (breakIterator != IntPtr.Zero)
                {
                    GetMethod<ubrk_close>()(breakIterator);
                }

                if (localePtr != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(localePtr);
                }

                if (textPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(textPtr);
                }
            }
        }

        private static bool EnsureInitialized()
        {
            lock (Gate)
            {
                if (_initialized)
                {
                    return _available;
                }

                _initialized = true;
                try
                {
                    Initialize();
                    _available = true;
                }
                catch
                {
                    _available = false;
                }

                return _available;
            }
        }

        private static void Initialize()
        {
            var assembly = typeof(PretextLayout).Assembly;

            if (PlatformCompat.IsWindows())
            {
                if (!TryLoad("icuuc77", assembly, NativeLibrarySearchDirectories, out _icuLibrary) &&
                    !TryLoad("icuuc", assembly, NativeLibrarySearchDirectories, out _icuLibrary))
                {
                    throw new DllNotFoundException("Failed to load ICU word-break library.");
                }
            }
            else if (PlatformCompat.IsMacOS())
            {
                if (!TryLoad("icudata", assembly, NativeLibrarySearchDirectories, out _))
                {
                    TryLoadUnoPackLibrary("uno.icu-macos", "runtimes/osx/native/libicudata.dylib", out _);
                }

                if (!TryLoad("icuuc", assembly, NativeLibrarySearchDirectories, out _icuLibrary) &&
                    !TryLoad("libicuuc", assembly, NativeLibrarySearchDirectories, out _icuLibrary) &&
                    !TryLoadUnoPackLibrary("uno.icu-macos", "runtimes/osx/native/libicuuc.dylib", out _icuLibrary))
                {
                    throw new DllNotFoundException("Failed to load ICU word-break library.");
                }
            }
            else if (PlatformCompat.IsLinux())
            {
                if (!TryLoad("icui18n", assembly, NativeLibrarySearchDirectories, out _icuLibrary) &&
                    !TryLoad("libicui18n", assembly, NativeLibrarySearchDirectories, out _icuLibrary) &&
                    !TryLoadVersionedLinuxLibrary("libicui18n.so.", out _icuLibrary) &&
                    !TryLoad("icuuc", assembly, NativeLibrarySearchDirectories, out _icuLibrary) &&
                    !TryLoad("libicuuc", assembly, NativeLibrarySearchDirectories, out _icuLibrary) &&
                    !TryLoadVersionedLinuxLibrary("libicuuc.so.", out _icuLibrary))
                {
                    throw new DllNotFoundException("Failed to load ICU word-break library.");
                }
            }
            else
            {
                throw new PlatformNotSupportedException("Locale-aware word segmentation is only available on desktop targets.");
            }

            if (NativeLibraryCompat.TryGetExport(_icuLibrary, nameof(ubrk_open), out _))
            {
                _icuVersion = 0;
                InitializeCommonDataIfAvailable();
                return;
            }

            for (var version = MaxSupportedIcuVersion; version >= MinSupportedIcuVersion; version--)
            {
                if (NativeLibraryCompat.TryGetExport(_icuLibrary, $"{nameof(ubrk_open)}_{version}", out _))
                {
                    _icuVersion = version;
                    InitializeCommonDataIfAvailable();
                    return;
                }
            }

            throw new MissingMethodException("Failed to locate ICU break-iterator symbols.");
        }

        private static bool TryLoad(string libraryName, Assembly assembly, DllImportSearchPath searchPath, out IntPtr handle)
        {
            handle = IntPtr.Zero;
            try
            {
                handle = NativeLibraryCompat.Load(libraryName, assembly, searchPath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryLoadVersionedLinuxLibrary(string libraryNamePrefix, out IntPtr handle)
        {
            handle = IntPtr.Zero;

            for (var version = MaxSupportedIcuVersion; version >= MinSupportedIcuVersion; version--)
            {
                if (NativeLibraryCompat.TryLoad($"{libraryNamePrefix}{version}", out handle))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryLoadUnoPackLibrary(string packageId, string relativePath, out IntPtr handle)
        {
            handle = IntPtr.Zero;

            try
            {
                var packageRoot = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
                if (string.IsNullOrWhiteSpace(packageRoot))
                {
                    packageRoot = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".nuget",
                        "packages");
                }

                var packageDirectory = Path.Combine(packageRoot, packageId);
                if (!Directory.Exists(packageDirectory))
                {
                    return false;
                }

                var matches = Directory
                    .EnumerateDirectories(packageDirectory)
                    .OrderByDescending(static path => path, StringComparer.Ordinal)
                    .Select(path => Path.Combine(path, relativePath))
                    .Where(File.Exists)
                    .ToArray();
                if (matches.Length == 0)
                {
                    return false;
                }

                handle = NativeLibraryCompat.Load(matches[0]);
                return true;
            }
            catch
            {
                handle = IntPtr.Zero;
                return false;
            }
        }

        private static void InitializeCommonDataIfAvailable()
        {
            if (_commonDataInitialized || !(PlatformCompat.IsMacOS() || PlatformCompat.IsWindows()))
            {
                return;
            }

            _commonDataInitialized = true;

            Stream? stream = null;
            try
            {
                stream = TryOpenEmbeddedIcuDataStream() ?? TryOpenUnoPackIcuDataStream();
                if (stream is null)
                {
                    return;
                }

                _commonDataBuffer = NativeMemoryCompat.AlignedAlloc((int)stream.Length, 16);
                StreamCompat.ReadExactly(stream, _commonDataBuffer, (int)stream.Length);
                GetMethod<udata_setCommonData>()(_commonDataBuffer, out var status);
                CheckErrorCode<udata_setCommonData>(status);
            }
            catch
            {
                if (_commonDataBuffer != IntPtr.Zero)
                {
                    NativeMemoryCompat.AlignedFree(_commonDataBuffer);
                    _commonDataBuffer = IntPtr.Zero;
                }
            }
            finally
            {
                stream?.Dispose();
            }
        }

        private static Stream? TryOpenEmbeddedIcuDataStream()
        {
            var candidateAssemblies = new List<Assembly>();
            if (Assembly.GetEntryAssembly() is { } entryAssembly)
            {
                candidateAssemblies.Add(entryAssembly);
            }

            candidateAssemblies.Add(typeof(PretextLayout).Assembly);
            candidateAssemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies());

            foreach (var assembly in candidateAssemblies.Distinct())
            {
                var resourceName = assembly
                    .GetManifestResourceNames()
                    .FirstOrDefault(static name => name.EndsWith("icudt.dat", StringComparison.OrdinalIgnoreCase));
                if (resourceName is null)
                {
                    continue;
                }

                if (assembly.GetManifestResourceStream(resourceName) is { } stream)
                {
                    return stream;
                }
            }

            return null;
        }

        private static Stream? TryOpenUnoPackIcuDataStream()
        {
            string? packageId = null;
            if (PlatformCompat.IsMacOS())
            {
                packageId = "uno.icu-macos";
            }
            else if (PlatformCompat.IsWindows())
            {
                packageId = "uno.icu-win";
            }

            if (packageId is null)
            {
                return null;
            }

            if (!TryFindUnoPackFile(packageId, "buildTransitive/icudt.dat", out var path))
            {
                return null;
            }

            return File.OpenRead(path);
        }

        private static bool TryFindUnoPackFile(string packageId, string relativePath, out string path)
        {
            path = string.Empty;

            try
            {
                var packageRoot = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
                if (string.IsNullOrWhiteSpace(packageRoot))
                {
                    packageRoot = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".nuget",
                        "packages");
                }

                var packageDirectory = Path.Combine(packageRoot, packageId);
                if (!Directory.Exists(packageDirectory))
                {
                    return false;
                }

                path = Directory
                    .EnumerateDirectories(packageDirectory)
                    .OrderByDescending(static item => item, StringComparer.Ordinal)
                    .Select(item => Path.Combine(item, relativePath))
                    .FirstOrDefault(File.Exists) ?? string.Empty;
                return !string.IsNullOrEmpty(path);
            }
            catch
            {
                path = string.Empty;
                return false;
            }
        }

        private static T GetMethod<T>() where T : Delegate
        {
            if (LookupCache.TryGetValue(typeof(T), out var cached))
            {
                return (T)cached;
            }

            var methodName = typeof(T).Name;
            if (!NativeLibraryCompat.TryGetExport(_icuLibrary, methodName, out var pointer))
            {
                if (_icuVersion <= 0 || !NativeLibraryCompat.TryGetExport(_icuLibrary, $"{methodName}_{_icuVersion}", out pointer))
                {
                    throw new MissingMethodException($"Failed to load ICU symbol {methodName}.");
                }
            }

            var method = MarshalCompat.GetDelegateForFunctionPointer<T>(pointer);
            LookupCache[typeof(T)] = method;
            return method;
        }

        private static void CheckErrorCode<T>(int status)
        {
            if (status > 0)
            {
                throw new InvalidOperationException($"{typeof(T).Name} failed with ICU status {status}.");
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr ubrk_open(int type, IntPtr locale, IntPtr text, int textLength, out int status);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void ubrk_close(IntPtr bi);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int ubrk_first(IntPtr bi);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int ubrk_next(IntPtr bi);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void udata_setCommonData(IntPtr bytes, out int errorCode);
    }
}
