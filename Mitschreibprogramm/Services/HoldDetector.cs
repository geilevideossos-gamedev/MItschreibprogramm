namespace Mitschreibprogramm.Services;

// Whether the pen rested at the end of a stroke: no movement beyond the tolerance for the given time before lifting.
// Times are input timestamps in milliseconds; they wrap around, so only differences are used.
public sealed class HoldDetector(double tolerance, int milliseconds)
{
    private (double X, double Y) _anchor;
    private int _anchorTime;

    public void Start(double x, double y, int time)
    {
        _anchor = (x, y);
        _anchorTime = time;
    }

    public void Move(double x, double y, int time)
    {
        if (Polyline.Distance(_anchor, (x, y)) > tolerance)
        {
            Start(x, y, time);
        }
    }

    public bool HeldAt(int endTime) => unchecked(endTime - _anchorTime) >= milliseconds;
}
