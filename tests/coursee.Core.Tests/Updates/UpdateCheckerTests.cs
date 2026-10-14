using Coursee.Core.Tests.Recents;

namespace Coursee.Core.Tests.Updates;

public sealed class UpdateCheckerTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));
    private readonly ManualClock _clock = new();
    private readonly RingLog _log = new();
    private readonly FakeFeed _feed = new();

    private string FilePath => Path.Combine(_directory, "updates.json");

    private sealed class FakeFeed : IReleaseFeed
    {
        public Release? Latest { get; set; }

        public Exception? Error { get; set; }

        public int Calls { get; private set; }

        public Task<Release?> LatestAsync(CancellationToken cancellation)
        {
            Calls++;
            return Error is null ? Task.FromResult(Latest) : Task.FromException<Release?>(Error);
        }
    }

    private UpdateChecker Checker(string current = "0.7") => new(_feed, new UpdateStore(FilePath), current, _clock, _log);

    [Fact]
    public async Task ANewerRelease_IsAvailable()
    {
        _feed.Latest = new Release("v0.8", "https://page");
        var checker = Checker();

        Assert.True(await checker.CheckIfDueAsync());

        Assert.Equal(_feed.Latest, checker.Available);
        Assert.Equal(_clock.Now, checker.LastCheck);
        Assert.Contains(_log.Entries, e => e.Category == UpdateChecker.LogCategory && e.Message.Contains("newer than 0.7"));
    }

    [Fact]
    public async Task TheSameRelease_IsNotAvailable()
    {
        _feed.Latest = new Release("0.7", "https://page");
        var checker = Checker();

        await checker.CheckIfDueAsync();

        Assert.Null(checker.Available);
    }

    [Fact]
    public async Task NoRelease_IsLogged()
    {
        var checker = Checker();

        Assert.True(await checker.CheckIfDueAsync());

        Assert.Null(checker.Available);
        Assert.Contains(_log.Entries, e => e.Message == "No release published");
    }

    [Fact]
    public async Task ItChecksAtMostOnceADay()
    {
        var checker = Checker();
        await checker.CheckIfDueAsync();

        _clock.Advance(TimeSpan.FromHours(23));
        Assert.False(checker.IsDue);
        Assert.False(await checker.CheckIfDueAsync());

        _clock.Advance(TimeSpan.FromHours(1));
        Assert.True(await checker.CheckIfDueAsync());
        Assert.Equal(2, _feed.Calls);
    }

    [Fact]
    public async Task TheLastCheck_SurvivesARestart()
    {
        _feed.Latest = new Release("0.8", "https://page");
        await Checker().CheckIfDueAsync();

        var restarted = Checker();

        Assert.False(restarted.IsDue);
        Assert.Equal("0.8", restarted.Available?.Version);
    }

    [Fact]
    public async Task AFailedCheck_IsLogged_KeepsWhatWasFound_AndWaitsADay()
    {
        _feed.Latest = new Release("0.8", "https://page");
        var checker = Checker();
        await checker.CheckIfDueAsync();
        _clock.Advance(TimeSpan.FromDays(1));
        _feed.Error = new HttpRequestException("offline");

        Assert.False(await checker.CheckIfDueAsync());

        Assert.Equal("0.8", checker.Available?.Version);
        Assert.False(checker.IsDue);
        Assert.Contains(_log.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("offline"));
    }

    [Fact]
    public async Task AnUnsavableCheck_IsLogged()
    {
        Directory.CreateDirectory(FilePath); // a folder where the file should be
        var checker = Checker();

        Assert.True(await checker.CheckIfDueAsync());

        Assert.Contains(_log.Entries, e => e.Level == LogLevel.Warning && e.Message.StartsWith("Cannot save"));
    }

    [Fact]
    public async Task AnUpgradedCopy_NoLongerSeesItsOwnVersionAsNew()
    {
        _feed.Latest = new Release("0.8", "https://page");
        await Checker("0.7").CheckIfDueAsync();

        Assert.Null(Checker("0.8").Available);
    }

    [Fact]
    public async Task ANewerRelease_IsAnnounced_UntilSeen()
    {
        _feed.Latest = new Release("0.8", "https://page");
        var checker = Checker();
        await checker.CheckIfDueAsync();
        Assert.True(checker.ShouldNotify);

        checker.Acknowledge();

        Assert.False(checker.ShouldNotify);
        Assert.False(Checker().ShouldNotify);
        Assert.Equal("0.8", checker.Available?.Version);
        Assert.Contains(_log.Entries, e => e.Message == "Release 0.8 seen");
    }

    [Fact]
    public async Task ALaterRelease_IsAnnouncedAgain()
    {
        _feed.Latest = new Release("0.8", "https://page");
        var checker = Checker();
        await checker.CheckIfDueAsync();
        checker.Acknowledge();

        _feed.Latest = new Release("0.9", "https://page");
        _clock.Advance(UpdateChecker.Interval);
        await checker.CheckIfDueAsync();

        Assert.True(checker.ShouldNotify);
    }

    [Fact]
    public async Task AnotherInstalledVersion_StartsAfresh()
    {
        _feed.Latest = new Release("0.9", "https://page");
        var checker = Checker("0.7");
        await checker.CheckIfDueAsync();
        checker.Acknowledge();

        Assert.True(Checker("0.8").ShouldNotify);
    }

    [Fact]
    public void NothingNewer_IsNeverAnnounced_NorAcknowledged()
    {
        var checker = Checker();

        checker.Acknowledge();

        Assert.False(checker.ShouldNotify);
        Assert.False(File.Exists(FilePath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
