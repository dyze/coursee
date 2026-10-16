using Coursee.Core.Tests.Recents;

namespace Coursee.Core.Tests.Logging;

public class FreezeWatchTests
{
    private readonly ManualClock _clock = new();

    private FreezeWatch Watch() => new(TimeSpan.FromSeconds(2), _clock);

    [Fact]
    public void AQuickAnswer_IsNoFreeze()
    {
        var watch = Watch();
        Assert.True(watch.Ask());
        _clock.Advance(TimeSpan.FromMilliseconds(100));

        Assert.Null(watch.Frozen());
        Assert.Null(watch.Answered());
    }

    [Fact]
    public void NoNewAsk_WhileTheLastOneIsUnanswered()
    {
        var watch = Watch();
        Assert.True(watch.Ask());

        Assert.False(watch.Ask());
        watch.Answered();
        Assert.True(watch.Ask());
    }

    [Fact]
    public void NothingAsked_IsNoFreeze() =>
        Assert.Null(Watch().Frozen());

    [Fact]
    public void AFreeze_IsReportedOnce_ThenItsEnd()
    {
        var watch = Watch();
        watch.Ask();
        _clock.Advance(TimeSpan.FromSeconds(1));
        Assert.Null(watch.Frozen());

        _clock.Advance(TimeSpan.FromSeconds(1.5));
        Assert.Equal(TimeSpan.FromSeconds(2.5), watch.Frozen());
        _clock.Advance(TimeSpan.FromSeconds(5));
        Assert.Null(watch.Frozen());

        Assert.Equal(TimeSpan.FromSeconds(7.5), watch.Answered());

        // The next freeze is a new one.
        watch.Ask();
        Assert.Null(watch.Answered());
    }
}
