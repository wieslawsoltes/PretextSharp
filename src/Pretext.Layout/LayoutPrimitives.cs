namespace Pretext.Layout;

public readonly record struct WrapMetrics(int LineCount, double Height, double MaxLineWidth);

public readonly record struct PositionedLine(string Text, double X, double Y, double Width);

public readonly record struct RectObstacle(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;

    public double Bottom => Y + Height;
}

public readonly record struct CircleObstacle(double X, double Y, double Radius);

public readonly record struct Interval(double Left, double Right)
{
    public double Width => Right - Left;
}
