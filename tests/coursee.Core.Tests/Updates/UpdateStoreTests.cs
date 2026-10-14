namespace Coursee.Core.Tests.Updates;

public sealed class UpdateStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "coursee-tests-" + Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(_directory, "updates.json");

    [Fact]
    public void NoFile_MeansNeverChecked()
    {
        var store = new UpdateStore(FilePath);

        Assert.Null(store.LastCheck);
        Assert.Null(store.Latest);
    }

    [Fact]
    public void ACheck_IsPersisted()
    {
        var time = new DateTimeOffset(2026, 10, 14, 8, 0, 0, TimeSpan.Zero);
        new UpdateStore(FilePath).Record(time, new Release("0.8", "https://page"), succeeded: true);

        var reloaded = new UpdateStore(FilePath);

        Assert.Equal(time, reloaded.LastCheck);
        Assert.Equal(new Release("0.8", "https://page"), reloaded.Latest);
    }

    [Fact]
    public void AFailedCheck_KeepsTheReleaseFoundBefore()
    {
        var store = new UpdateStore(FilePath);
        store.Record(DateTimeOffset.UnixEpoch, new Release("0.8", "https://page"), succeeded: true);

        store.Record(DateTimeOffset.UnixEpoch.AddDays(1), null, succeeded: false);

        Assert.Equal("0.8", new UpdateStore(FilePath).Latest?.Version);
    }

    [Fact]
    public void TheAcknowledgement_IsPersisted_AndSurvivesALaterCheck()
    {
        var store = new UpdateStore(FilePath);
        store.Acknowledge("0.8", "0.7");

        store.Record(DateTimeOffset.UnixEpoch, new Release("0.8", "https://page"), succeeded: true);

        var reloaded = new UpdateStore(FilePath);
        Assert.Equal("0.8", reloaded.AcknowledgedVersion);
        Assert.Equal("0.7", reloaded.AcknowledgedBy);
    }

    [Fact]
    public void ACorruptFile_MeansNeverChecked()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(FilePath, "{ not json");

        Assert.Null(new UpdateStore(FilePath).LastCheck);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
