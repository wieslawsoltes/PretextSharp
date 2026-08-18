using System.Reflection;

namespace Pretext;

public static partial class PretextLayout
{
    private static IPretextTextMeasurerFactory? _textMeasurerFactory;
    private static IPretextTextShaperFactory? _textShaperFactory;
    private static bool _isTextShaperFactoryExplicitlyConfigured;

    public static void SetTextMeasurerFactory(IPretextTextMeasurerFactory? textMeasurerFactory)
    {
        _textMeasurerFactory = textMeasurerFactory;
        if (!_isTextShaperFactoryExplicitlyConfigured)
        {
            _textShaperFactory = textMeasurerFactory as IPretextTextShaperFactory;
        }

        ClearCache();
    }

    public static void SetTextShaperFactory(IPretextTextShaperFactory? textShaperFactory)
    {
        _textShaperFactory = textShaperFactory;
        _isTextShaperFactoryExplicitlyConfigured = textShaperFactory is not null;
        ClearCache();
    }

    private static IPretextTextMeasurerFactory GetTextMeasurerFactory()
    {
        if (_textMeasurerFactory is { } configuredFactory)
        {
            return configuredFactory;
        }

        lock (FontStateGate)
        {
            if (_textMeasurerFactory is { } cachedFactory)
            {
                return cachedFactory;
            }

            _textMeasurerFactory = DiscoverTextMeasurerFactory()
                ?? throw new InvalidOperationException(
                    "No Pretext text measurer factory is configured. Reference a backend package such as Pretext.SkiaSharp or call PretextLayout.SetTextMeasurerFactory(...).");
            return _textMeasurerFactory;
        }
    }

    private static IPretextTextShaperFactory GetTextShaperFactory()
    {
        if (_textShaperFactory is { } configuredFactory)
        {
            return configuredFactory;
        }

        if (_textMeasurerFactory is IPretextTextShaperFactory configuredMeasurerFactory)
        {
            return configuredMeasurerFactory;
        }

        lock (FontStateGate)
        {
            if (_textShaperFactory is { } cachedFactory)
            {
                return cachedFactory;
            }

            if (_textMeasurerFactory is IPretextTextShaperFactory cachedMeasurerFactory)
            {
                return cachedMeasurerFactory;
            }

            _textShaperFactory = DiscoverTextShaperFactory()
                ?? throw new InvalidOperationException(
                    "No Pretext text shaper factory is configured. Reference a shaping-capable backend package such as Pretext.FreeType or Pretext.CoreText, or call PretextLayout.SetTextShaperFactory(...).");
            return _textShaperFactory;
        }
    }

    private static IPretextTextMeasurerFactory? DiscoverTextMeasurerFactory()
    {
        var candidateTypes = new Dictionary<string, Type>(StringComparer.Ordinal);
        var visitedAssemblies = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<Assembly>();

        void Enqueue(Assembly? assembly)
        {
            if (assembly is null)
            {
                return;
            }

            var identity = assembly.FullName;
            if (string.IsNullOrEmpty(identity) || !visitedAssemblies.Add(identity))
            {
                return;
            }

            queue.Enqueue(assembly);
        }

        Enqueue(typeof(PretextLayout).Assembly);
        Enqueue(Assembly.GetEntryAssembly());

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Enqueue(assembly);
        }

        foreach (var assemblyPath in GetProbeAssemblyPaths())
        {
            try
            {
                Enqueue(Assembly.Load(AssemblyName.GetAssemblyName(assemblyPath)));
            }
            catch
            {
                // Ignore candidate files that are not loadable assemblies in this runtime.
            }
        }

        while (queue.Count > 0)
        {
            var assembly = queue.Dequeue();
            AddFactoryCandidates(assembly, candidateTypes);

            foreach (var referencedAssembly in GetReferencedAssembliesSafe(assembly))
            {
                try
                {
                    Enqueue(Assembly.Load(referencedAssembly));
                }
                catch
                {
                    // Ignore assemblies that are unavailable in the current app context.
                }
            }
        }

        if (candidateTypes.Count == 0)
        {
            return null;
        }

        var supportedFactories = new List<IPretextTextMeasurerFactory>();
        foreach (var factoryType in candidateTypes.Values)
        {
            if (Activator.CreateInstance(factoryType) is not IPretextTextMeasurerFactory factory)
            {
                throw new InvalidOperationException($"Unable to create Pretext text measurer factory '{factoryType.FullName}'.");
            }

            if (factory.IsSupported)
            {
                supportedFactories.Add(factory);
            }
        }

        if (supportedFactories.Count == 0)
        {
            return null;
        }

        var highestPriority = supportedFactories.Max(static factory => factory.Priority);
        var winners = supportedFactories
            .Where(factory => factory.Priority == highestPriority)
            .OrderBy(factory => factory.Name, StringComparer.Ordinal)
            .ToArray();

        if (winners.Length > 1)
        {
            throw new InvalidOperationException(
                $"Multiple supported Pretext text measurer factories were discovered at priority {highestPriority} ({string.Join(", ", winners.Select(static factory => factory.Name))}). Call PretextLayout.SetTextMeasurerFactory(...) to select one explicitly.");
        }

        return winners[0];
    }

    private static void AddFactoryCandidates(Assembly assembly, IDictionary<string, Type> candidateTypes)
    {
        object[] attributes;
        try
        {
            attributes = assembly.GetCustomAttributes(typeof(PretextTextMeasurerFactoryAttribute), inherit: false);
        }
        catch
        {
            return;
        }

        foreach (PretextTextMeasurerFactoryAttribute attribute in attributes)
        {
            var factoryType = attribute.FactoryType;
            if (!typeof(IPretextTextMeasurerFactory).IsAssignableFrom(factoryType) || factoryType.IsAbstract)
            {
                throw new InvalidOperationException(
                    $"Assembly '{assembly.GetName().Name}' advertises invalid Pretext text measurer factory '{factoryType.FullName}'.");
            }

            var identity = factoryType.AssemblyQualifiedName ?? factoryType.FullName ?? factoryType.Name;
            candidateTypes[identity] = factoryType;
        }
    }

    private static IPretextTextShaperFactory? DiscoverTextShaperFactory()
    {
        var candidateTypes = new Dictionary<string, Type>(StringComparer.Ordinal);
        var visitedAssemblies = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<Assembly>();

        void Enqueue(Assembly? assembly)
        {
            if (assembly is null)
            {
                return;
            }

            var identity = assembly.FullName;
            if (string.IsNullOrEmpty(identity) || !visitedAssemblies.Add(identity))
            {
                return;
            }

            queue.Enqueue(assembly);
        }

        Enqueue(typeof(PretextLayout).Assembly);
        Enqueue(Assembly.GetEntryAssembly());

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Enqueue(assembly);
        }

        foreach (var assemblyPath in GetProbeAssemblyPaths())
        {
            try
            {
                Enqueue(Assembly.Load(AssemblyName.GetAssemblyName(assemblyPath)));
            }
            catch
            {
                // Ignore candidate files that are not loadable assemblies in this runtime.
            }
        }

        while (queue.Count > 0)
        {
            var assembly = queue.Dequeue();
            AddShaperFactoryCandidates(assembly, candidateTypes);

            foreach (var referencedAssembly in GetReferencedAssembliesSafe(assembly))
            {
                try
                {
                    Enqueue(Assembly.Load(referencedAssembly));
                }
                catch
                {
                    // Ignore assemblies that are unavailable in the current app context.
                }
            }
        }

        if (candidateTypes.Count == 0)
        {
            return null;
        }

        var supportedFactories = new List<IPretextTextShaperFactory>();
        foreach (var factoryType in candidateTypes.Values)
        {
            if (Activator.CreateInstance(factoryType) is not IPretextTextShaperFactory factory)
            {
                throw new InvalidOperationException($"Unable to create Pretext text shaper factory '{factoryType.FullName}'.");
            }

            if (factory.IsSupported)
            {
                supportedFactories.Add(factory);
            }
        }

        if (supportedFactories.Count == 0)
        {
            return null;
        }

        var highestPriority = supportedFactories.Max(static factory => factory.Priority);
        var winners = supportedFactories
            .Where(factory => factory.Priority == highestPriority)
            .OrderBy(factory => factory.Name, StringComparer.Ordinal)
            .ToArray();

        if (winners.Length > 1)
        {
            throw new InvalidOperationException(
                $"Multiple supported Pretext text shaper factories were discovered at priority {highestPriority} ({string.Join(", ", winners.Select(static factory => factory.Name))}). Call PretextLayout.SetTextShaperFactory(...) to select one explicitly.");
        }

        return winners[0];
    }

    private static void AddShaperFactoryCandidates(Assembly assembly, IDictionary<string, Type> candidateTypes)
    {
        object[] attributes;
        try
        {
            attributes = assembly.GetCustomAttributes(typeof(PretextTextShaperFactoryAttribute), inherit: false);
        }
        catch
        {
            return;
        }

        foreach (PretextTextShaperFactoryAttribute attribute in attributes)
        {
            var factoryType = attribute.FactoryType;
            if (!typeof(IPretextTextShaperFactory).IsAssignableFrom(factoryType) || factoryType.IsAbstract)
            {
                throw new InvalidOperationException(
                    $"Assembly '{assembly.GetName().Name}' advertises invalid Pretext text shaper factory '{factoryType.FullName}'.");
            }

            var identity = factoryType.AssemblyQualifiedName ?? factoryType.FullName ?? factoryType.Name;
            candidateTypes[identity] = factoryType;
        }
    }

    private static IEnumerable<AssemblyName> GetReferencedAssembliesSafe(Assembly assembly)
    {
        try
        {
            return assembly.GetReferencedAssemblies();
        }
        catch
        {
            return Array.Empty<AssemblyName>();
        }
    }

    private static IEnumerable<string> GetProbeAssemblyPaths()
    {
        var baseDirectory = AppContext.BaseDirectory;
        if (string.IsNullOrWhiteSpace(baseDirectory) || !Directory.Exists(baseDirectory))
        {
            return Array.Empty<string>();
        }

        try
        {
            return Directory.GetFiles(baseDirectory, "Pretext*.dll", SearchOption.TopDirectoryOnly);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}
