using Pretext;
using Pretext.SkiaSharp;

[assembly: PretextTextMeasurerFactory(typeof(SkiaSharpTextMeasurerFactory))]
[assembly: PretextTextShaperFactory(typeof(SkiaSharpTextMeasurerFactory))]
