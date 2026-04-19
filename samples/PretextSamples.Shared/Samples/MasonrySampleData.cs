using System.Reflection;
using System.Text.Json;

namespace PretextSamples.Samples;

public static class MasonrySampleData
{
    private static readonly Lazy<IReadOnlyList<string>> s_cards = new(LoadCardsCore, LazyThreadSafetyMode.ExecutionAndPublication);

    public static IReadOnlyList<string> LoadCards() => s_cards.Value;

    private static IReadOnlyList<string> LoadCardsCore()
    {
        const string resourceName = "PretextSamples.Shared.Assets.shower_thoughts.json";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded sample asset '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<string[]>(json) ?? [];
    }
}
