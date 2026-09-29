using Mitschreibprogramm.Services;

namespace Mitschreibprogramm.Tests;

public sealed class HoldDetectorTests
{
    private readonly HoldDetector _hold = new(tolerance: 4, milliseconds: 500);

    [Fact]
    public void JitterBelowTheToleranceCountsAsHolding()
    {
        _hold.Start(100, 100, 1000);
        _hold.Move(200, 100, 1300);
        _hold.Move(202, 101, 1500);
        _hold.Move(199, 98, 1700);

        Assert.True(_hold.HeldAt(1800));
        Assert.False(_hold.HeldAt(1799));
    }

    [Fact]
    public void MovingUntilTheEndIsNoHold()
    {
        _hold.Start(100, 100, 0);
        _hold.Move(150, 100, 900);
        _hold.Move(160, 100, 1000);

        Assert.False(_hold.HeldAt(1100));
    }

    [Fact]
    public void StandingStillWithoutMoveEventsCountsAsHolding()
    {
        _hold.Start(100, 100, 0);
        _hold.Move(300, 100, 400);

        Assert.True(_hold.HeldAt(950));
    }

    [Fact]
    public void TimestampsMayWrapAround()
    {
        _hold.Start(0, 0, int.MaxValue - 100);

        Assert.True(_hold.HeldAt(int.MinValue + 500));
    }
}
