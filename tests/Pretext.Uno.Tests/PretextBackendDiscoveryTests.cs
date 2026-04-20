using System.Reflection;
using System.Runtime.InteropServices;

using Pretext;
using Xunit;

namespace Pretext.Tests;

public sealed class PretextBackendDiscoveryTests : IDisposable
{
    public PretextBackendDiscoveryTests()
    {
        PretextLayout.SetTextMeasurerFactory(null);
        PretextLayout.ClearCache();
    }

    public void Dispose()
    {
        PretextLayout.SetTextMeasurerFactory(null);
        PretextLayout.ClearCache();
    }

    [Fact(DisplayName = "prepare auto-discovers the best supported backend for the current OS")]
    public void Prepare_AutoDiscoversBestSupportedBackend()
    {
        var prepared = PretextLayout.PrepareWithSegments("Hello world", "16px Arial");
        var selectedFactory = GetSelectedFactory();

        Assert.NotEmpty(prepared.Widths);
        Assert.True(prepared.Widths[0] > 0);
        Assert.Equal(1, PretextLayout.Layout(prepared, 1000, 20).LineCount);
        Assert.Equal(GetExpectedBackendName(), selectedFactory.Name);
    }

    private static IPretextTextMeasurerFactory GetSelectedFactory()
    {
        var field = typeof(PretextLayout).GetField("_textMeasurerFactory", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsAssignableFrom<IPretextTextMeasurerFactory>(field!.GetValue(null));
    }

    private static string GetExpectedBackendName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "DirectWrite";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "FreeType";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "CoreText";
        }

        return "SkiaSharp";
    }
}
