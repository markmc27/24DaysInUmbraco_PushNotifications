using NPoco;
using Umbraco.Cms.Infrastructure.Scoping;

namespace NotificationsRevisit.Announcements.Persistence;

public interface IPushSubscriptionRepository
{
    /// <summary>Stores a subscription, or updates the keys of the one with the same endpoint.</summary>
    Task UpsertAsync(string endpoint, string p256dh, string auth);

    Task<IReadOnlyList<PushSubscriptionDto>> GetAllAsync();

    Task<int> CountAsync();

    /// <summary>Deletes the subscriptions with these endpoints and returns how many were removed.</summary>
    Task<int> DeleteByEndpointsAsync(IReadOnlyCollection<string> endpoints);
}

public class PushSubscriptionRepository : IPushSubscriptionRepository
{
    private readonly IScopeProvider _scopeProvider;
    private readonly TimeProvider _timeProvider;

    public PushSubscriptionRepository(IScopeProvider scopeProvider, TimeProvider timeProvider)
    {
        _scopeProvider = scopeProvider;
        _timeProvider = timeProvider;
    }

    public async Task UpsertAsync(string endpoint, string p256dh, string auth)
    {
        using IScope scope = _scopeProvider.CreateScope();
        await PushSubscriptionQueries.UpsertAsync(scope.Database, endpoint, p256dh, auth, _timeProvider.GetUtcNow().UtcDateTime);
        scope.Complete();
    }

    public async Task<IReadOnlyList<PushSubscriptionDto>> GetAllAsync()
    {
        using IScope scope = _scopeProvider.CreateScope(autoComplete: true);
        return await PushSubscriptionQueries.GetAllAsync(scope.Database);
    }

    public async Task<int> CountAsync()
    {
        using IScope scope = _scopeProvider.CreateScope(autoComplete: true);
        return await PushSubscriptionQueries.CountAsync(scope.Database);
    }

    public async Task<int> DeleteByEndpointsAsync(IReadOnlyCollection<string> endpoints)
    {
        using IScope scope = _scopeProvider.CreateScope();
        int removed = await PushSubscriptionQueries.DeleteByEndpointsAsync(scope.Database, endpoints);
        scope.Complete();
        return removed;
    }
}

/// <summary>
/// The SQL, kept apart from Umbraco scopes so it can be tested against a plain NPoco database.
/// </summary>
internal static class PushSubscriptionQueries
{
    public static async Task UpsertAsync(IDatabase db, string endpoint, string p256dh, string auth, DateTime nowUtc)
    {
        PushSubscriptionDto? existing = await db.FirstOrDefaultAsync<PushSubscriptionDto>(
            $"SELECT * FROM {PushSubscriptionDto.TableName} WHERE Endpoint = @0", endpoint);

        if (existing is null)
        {
            await db.InsertAsync(new PushSubscriptionDto
            {
                Endpoint = endpoint,
                P256dh = p256dh,
                Auth = auth,
                CreatedUtc = nowUtc,
            });
            return;
        }

        existing.P256dh = p256dh;
        existing.Auth = auth;
        await db.UpdateAsync(existing);
    }

    public static async Task<IReadOnlyList<PushSubscriptionDto>> GetAllAsync(IDatabase db)
        => await db.FetchAsync<PushSubscriptionDto>($"SELECT * FROM {PushSubscriptionDto.TableName} ORDER BY Id");

    public static Task<int> CountAsync(IDatabase db)
        => db.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {PushSubscriptionDto.TableName}");

    public static async Task<int> DeleteByEndpointsAsync(IDatabase db, IReadOnlyCollection<string> endpoints)
    {
        if (endpoints.Count == 0)
        {
            return 0;
        }

        return await db.ExecuteAsync($"DELETE FROM {PushSubscriptionDto.TableName} WHERE Endpoint IN (@0)", endpoints);
    }
}
