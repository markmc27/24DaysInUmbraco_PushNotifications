using NotificationsRevisit.Announcements.Persistence;

namespace NotificationsRevisit.Announcements.Tests.Persistence;

public class RepositoryQueryTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 21, 13, 0, 0, DateTimeKind.Utc);
    private readonly SqliteTestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Resubmitting_an_endpoint_leaves_one_row_with_the_latest_keys()
    {
        await PushSubscriptionQueries.UpsertAsync(_db.Database, "https://push.example/abc", "key-1", "auth-1", Now);
        await PushSubscriptionQueries.UpsertAsync(_db.Database, "https://push.example/abc", "key-2", "auth-2", Now.AddHours(1));

        IReadOnlyList<PushSubscriptionDto> all = await PushSubscriptionQueries.GetAllAsync(_db.Database);

        PushSubscriptionDto only = Assert.Single(all);
        Assert.Equal("key-2", only.P256dh);
        Assert.Equal("auth-2", only.Auth);
        Assert.Equal(Now, only.CreatedUtc);
    }

    [Fact]
    public async Task Different_endpoints_are_stored_separately_and_counted()
    {
        await PushSubscriptionQueries.UpsertAsync(_db.Database, "https://push.example/a", "k", "a", Now);
        await PushSubscriptionQueries.UpsertAsync(_db.Database, "https://push.example/b", "k", "a", Now);

        Assert.Equal(2, await PushSubscriptionQueries.CountAsync(_db.Database));
    }

    [Fact]
    public async Task Delete_by_endpoints_removes_only_those_endpoints()
    {
        foreach (string e in new[] { "https://push.example/a", "https://push.example/b", "https://push.example/c" })
        {
            await PushSubscriptionQueries.UpsertAsync(_db.Database, e, "k", "a", Now);
        }

        int removed = await PushSubscriptionQueries.DeleteByEndpointsAsync(
            _db.Database, ["https://push.example/a", "https://push.example/c"]);

        Assert.Equal(2, removed);
        PushSubscriptionDto left = Assert.Single(await PushSubscriptionQueries.GetAllAsync(_db.Database));
        Assert.Equal("https://push.example/b", left.Endpoint);
    }

    [Fact]
    public async Task Delete_with_no_endpoints_removes_nothing()
    {
        await PushSubscriptionQueries.UpsertAsync(_db.Database, "https://push.example/a", "k", "a", Now);

        Assert.Equal(0, await PushSubscriptionQueries.DeleteByEndpointsAsync(_db.Database, []));
        Assert.Equal(1, await PushSubscriptionQueries.CountAsync(_db.Database));
    }
}
