namespace Pretext;

public interface IPretextTextMeasurer : IDisposable
{
    double MeasureText(string text);
}

public interface IPretextTextMeasurerFactory
{
    string Name { get; }

    bool IsSupported { get; }

    int Priority { get; }

    IPretextTextMeasurer Create(string font);
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class PretextTextMeasurerFactoryAttribute : Attribute
{
    public PretextTextMeasurerFactoryAttribute(Type factoryType)
    {
        FactoryType = factoryType ?? throw new ArgumentNullException(nameof(factoryType));
    }

    public Type FactoryType { get; }
}
