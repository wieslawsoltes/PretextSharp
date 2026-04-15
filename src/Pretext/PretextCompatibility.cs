using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Pretext;

internal static class GuardCompat
{
    public static void ThrowIfNull(object? value, string paramName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }
    }
}

internal static class PlatformCompat
{
    public static bool IsWindows()
        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public static bool IsLinux()
        => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    public static bool IsMacOS()
        => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
}

internal static class MarshalCompat
{
    public static IntPtr StringToCoTaskMemUtf8(string value)
    {
#if NET6_0_OR_GREATER
        return Marshal.StringToCoTaskMemUTF8(value);
#else
        var bytes = Encoding.UTF8.GetBytes(value);
        var buffer = Marshal.AllocCoTaskMem(bytes.Length + 1);
        Marshal.Copy(bytes, 0, buffer, bytes.Length);
        Marshal.WriteByte(buffer, bytes.Length, 0);
        return buffer;
#endif
    }

    public static T GetDelegateForFunctionPointer<T>(IntPtr pointer) where T : class
    {
#if NET6_0_OR_GREATER
        return Marshal.GetDelegateForFunctionPointer<T>(pointer);
#else
        return (T)(object)Marshal.GetDelegateForFunctionPointer(pointer, typeof(T));
#endif
    }
}

internal static class StreamCompat
{
    public static void ReadExactly(Stream stream, IntPtr destination, int length)
    {
        var buffer = new byte[Math.Min(length, 81920)];
        var offset = 0;

        while (offset < length)
        {
            var toRead = Math.Min(buffer.Length, length - offset);
            var read = stream.Read(buffer, 0, toRead);
            if (read <= 0)
            {
                throw new EndOfStreamException();
            }

            Marshal.Copy(buffer, 0, IntPtr.Add(destination, offset), read);
            offset += read;
        }
    }
}

internal static class NativeMemoryCompat
{
    public static IntPtr AlignedAlloc(int length, int alignment)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        if (alignment <= 0 || (alignment & (alignment - 1)) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(alignment));
        }

        var raw = Marshal.AllocHGlobal(length + alignment - 1 + IntPtr.Size);
        var rawAddress = raw.ToInt64() + IntPtr.Size;
        var alignedAddress = (rawAddress + alignment - 1) & ~((long)alignment - 1);
        var aligned = new IntPtr(alignedAddress);
        Marshal.WriteIntPtr(IntPtr.Add(aligned, -IntPtr.Size), raw);
        return aligned;
    }

    public static void AlignedFree(IntPtr pointer)
    {
        if (pointer == IntPtr.Zero)
        {
            return;
        }

        var raw = Marshal.ReadIntPtr(IntPtr.Add(pointer, -IntPtr.Size));
        Marshal.FreeHGlobal(raw);
    }
}

internal static class NativeLibraryCompat
{
    private const int RtldNow = 2;

    public static IntPtr Load(string libraryName, Assembly assembly, DllImportSearchPath searchPath)
    {
#if NET6_0_OR_GREATER
        return NativeLibrary.Load(libraryName, assembly, searchPath);
#else
        if (TryLoad(libraryName, assembly, searchPath, out var handle))
        {
            return handle;
        }

        throw new DllNotFoundException($"Unable to load native library '{libraryName}'.");
#endif
    }

    public static IntPtr Load(string libraryPath)
    {
#if NET6_0_OR_GREATER
        return NativeLibrary.Load(libraryPath);
#else
        if (TryLoadPlatformLibrary(libraryPath, out var handle))
        {
            return handle;
        }

        throw new DllNotFoundException($"Unable to load native library '{libraryPath}'.");
#endif
    }

    public static bool TryLoad(string libraryName, out IntPtr handle)
    {
#if NET6_0_OR_GREATER
        return NativeLibrary.TryLoad(libraryName, out handle);
#else
        return TryLoadPlatformLibrary(libraryName, out handle);
#endif
    }

    public static bool TryLoad(string libraryName, Assembly assembly, DllImportSearchPath searchPath, out IntPtr handle)
    {
#if NET6_0_OR_GREATER
        try
        {
            handle = NativeLibrary.Load(libraryName, assembly, searchPath);
            return true;
        }
        catch
        {
            handle = IntPtr.Zero;
            return false;
        }
#else
        if (TryLoadPlatformLibrary(libraryName, out handle))
        {
            return true;
        }

        foreach (var candidatePath in EnumerateCandidatePaths(libraryName, assembly, searchPath))
        {
            if (TryLoadPlatformLibrary(candidatePath, out handle))
            {
                return true;
            }
        }

        handle = IntPtr.Zero;
        return false;
#endif
    }

    public static bool TryGetExport(IntPtr handle, string symbolName, out IntPtr symbol)
    {
#if NET6_0_OR_GREATER
        return NativeLibrary.TryGetExport(handle, symbolName, out symbol);
#else
        if (handle == IntPtr.Zero)
        {
            symbol = IntPtr.Zero;
            return false;
        }

        if (PlatformCompat.IsWindows())
        {
            symbol = GetProcAddress(handle, symbolName);
            return symbol != IntPtr.Zero;
        }

        symbol = PlatformCompat.IsMacOS() ? dlsym_osx(handle, symbolName) : dlsym_linux(handle, symbolName);
        return symbol != IntPtr.Zero;
#endif
    }

#if !NET6_0_OR_GREATER
    private static IEnumerable<string> EnumerateCandidatePaths(string libraryName, Assembly assembly, DllImportSearchPath searchPath)
    {
        var comparer = PlatformCompat.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var seen = new HashSet<string>(comparer);

        foreach (var directory in EnumerateSearchDirectories(assembly, searchPath))
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }

            foreach (var variant in EnumerateLibraryNameVariants(libraryName))
            {
                var candidate = Path.Combine(directory, variant);
                if (seen.Add(candidate))
                {
                    yield return candidate;
                }
            }
        }
    }

    private static IEnumerable<string> EnumerateSearchDirectories(Assembly assembly, DllImportSearchPath searchPath)
    {
        if ((searchPath & DllImportSearchPath.AssemblyDirectory) != 0)
        {
            yield return Path.GetDirectoryName(assembly.Location) ?? string.Empty;
        }

        if ((searchPath & DllImportSearchPath.ApplicationDirectory) != 0)
        {
            yield return AppContext.BaseDirectory;
        }

        if ((searchPath & DllImportSearchPath.UserDirectories) != 0)
        {
            foreach (var directory in EnumerateUserDirectories())
            {
                yield return directory;
            }
        }

        if (searchPath == 0)
        {
            yield return Path.GetDirectoryName(assembly.Location) ?? string.Empty;
            yield return AppContext.BaseDirectory;
        }
    }

    private static IEnumerable<string> EnumerateUserDirectories()
    {
        var rawDirectories = GetAppContextData("NATIVE_DLL_SEARCH_DIRECTORIES")
            ?? AppDomain.CurrentDomain.GetData("NATIVE_DLL_SEARCH_DIRECTORIES") as string;
        if (string.IsNullOrWhiteSpace(rawDirectories))
        {
            yield break;
        }

        foreach (var directory in rawDirectories!.Split(new[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = directory.Trim();
            if (candidate.Length > 0)
            {
                yield return candidate.ToString();
            }
        }
    }

    private static string? GetAppContextData(string key)
    {
        var getDataMethod = typeof(AppContext).GetMethod(
            "GetData",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string) },
            modifiers: null);
        return getDataMethod?.Invoke(null, new object[] { key }) as string;
    }

    private static IEnumerable<string> EnumerateLibraryNameVariants(string libraryName)
    {
        yield return libraryName;

        if (Path.IsPathRooted(libraryName))
        {
            yield break;
        }

        var extension = Path.GetExtension(libraryName);
        if (PlatformCompat.IsWindows())
        {
            if (string.IsNullOrEmpty(extension))
            {
                yield return $"{libraryName}.dll";
            }

            yield break;
        }

        var hasLibPrefix = libraryName.StartsWith("lib", StringComparison.Ordinal);
        var unixExtension = PlatformCompat.IsMacOS() ? ".dylib" : ".so";

        if (string.IsNullOrEmpty(extension))
        {
            yield return hasLibPrefix ? $"{libraryName}{unixExtension}" : $"lib{libraryName}{unixExtension}";
            if (!hasLibPrefix)
            {
                yield return $"lib{libraryName}";
            }
        }
        else if (!hasLibPrefix)
        {
            yield return $"lib{libraryName}";
        }
    }

    private static bool TryLoadPlatformLibrary(string libraryName, out IntPtr handle)
    {
        handle = IntPtr.Zero;

        if (PlatformCompat.IsWindows())
        {
            handle = LoadLibrary(libraryName);
            return handle != IntPtr.Zero;
        }

        handle = PlatformCompat.IsMacOS()
            ? dlopen_osx(libraryName, RtldNow)
            : dlopen_linux(libraryName, RtldNow);
        return handle != IntPtr.Zero;
    }

    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32", SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport("libdl", EntryPoint = "dlopen")]
    private static extern IntPtr dlopen_linux(string fileName, int flags);

    [DllImport("libdl", EntryPoint = "dlsym")]
    private static extern IntPtr dlsym_linux(IntPtr handle, string symbol);

    [DllImport("/usr/lib/libSystem.B.dylib", EntryPoint = "dlopen")]
    private static extern IntPtr dlopen_osx(string fileName, int flags);

    [DllImport("/usr/lib/libSystem.B.dylib", EntryPoint = "dlsym")]
    private static extern IntPtr dlsym_osx(IntPtr handle, string symbol);
#endif
}

internal static class UnicodeCompat
{
#if !NET6_0_OR_GREATER
    public static bool AnyCodePoint(string text, Func<int, bool> predicate)
    {
        for (var index = 0; index < text.Length;)
        {
            var codePoint = ReadCodePoint(text, ref index, out _);
            if (predicate(codePoint))
            {
                return true;
            }
        }

        return false;
    }

    public static bool AnyCodePoint(string text, Func<int, UnicodeCategory, bool> predicate)
    {
        for (var index = 0; index < text.Length;)
        {
            var codePoint = ReadCodePoint(text, ref index, out var scalarIndex);
            var category = CharUnicodeInfo.GetUnicodeCategory(text, scalarIndex);
            if (predicate(codePoint, category))
            {
                return true;
            }
        }

        return false;
    }

    public static bool AllCodePoints(string text, Func<int, UnicodeCategory, bool> predicate, out bool sawAny)
    {
        sawAny = false;
        for (var index = 0; index < text.Length;)
        {
            var codePoint = ReadCodePoint(text, ref index, out var scalarIndex);
            var category = CharUnicodeInfo.GetUnicodeCategory(text, scalarIndex);
            sawAny = true;
            if (!predicate(codePoint, category))
            {
                return false;
            }
        }

        return true;
    }

    private static int ReadCodePoint(string text, ref int index, out int scalarIndex)
    {
        scalarIndex = index;
        if (index + 1 < text.Length &&
            char.IsHighSurrogate(text[index]) &&
            char.IsLowSurrogate(text[index + 1]))
        {
            var codePoint = char.ConvertToUtf32(text[index], text[index + 1]);
            index += 2;
            return codePoint;
        }

        return text[index++];
    }
#endif
}
