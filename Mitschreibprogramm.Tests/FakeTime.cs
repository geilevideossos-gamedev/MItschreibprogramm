namespace Mitschreibprogramm.Tests;

internal sealed class FakeTime : TimeProvider
{
    public DateTimeOffset Now { get; private set; } = new(2026, 9, 28, 10, 0, 0, TimeSpan.Zero);

    public void Advance(TimeSpan by) => Now += by;

    public override DateTimeOffset GetUtcNow() => Now;
}
