using NotificationsRevisit.Announcements.Persistence;

namespace NotificationsRevisit.Announcements.Tests.Persistence;

public class PushedContentQueryTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 23, 17, 0, 0, DateTimeKind.Utc);
    private readonly SqliteTestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task First_claim_succeeds()
    {
        Assert.True(await PushedContentQueries.TryClaimAsync(_db.Database, Guid.NewGuid(), Now));
    }

    [Fact]
    public async Task Second_claim_for_the_same_key_reports_already_claimed()
    {
        Guid key = Guid.NewGuid();
        await PushedContentQueries.TryClaimAsync(_db.Database, key, Now);

        Assert.False(await PushedContentQueries.TryClaimAsync(_db.Database, key, Now.AddMinutes(5)));
    }

    [Fact]
    public async Task Claims_for_different_keys_are_independent()
    {
        await PushedContentQueries.TryClaimAsync(_db.Database, Guid.NewGuid(), Now);

        Assert.True(await PushedContentQueries.TryClaimAsync(_db.Database, Guid.NewGuid(), Now));
    }

    [Fact]
    public async Task Released_key_can_be_claimed_again()
    {
        Guid key = Guid.NewGuid();
        await PushedContentQueries.TryClaimAsync(_db.Database, key, Now);

        await PushedContentQueries.ReleaseAsync(_db.Database, key);

        Assert.True(await PushedContentQueries.TryClaimAsync(_db.Database, key, Now));
    }
}
