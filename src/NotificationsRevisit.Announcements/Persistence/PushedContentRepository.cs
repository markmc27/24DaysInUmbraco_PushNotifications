using System.Data.Common;
using NPoco;
using Umbraco.Cms.Infrastructure.Scoping;

namespace NotificationsRevisit.Announcements.Persistence;

/// <summary>Records which content items have been pushed, so each is pushed once (design D3).</summary>
public interface IPushedContentRepository
{
    /// <summary>Claims a content item for sending. Returns false if it was already claimed.</summary>
    Task<bool> TryClaimAsync(Guid contentKey);

    /// <summary>Releases a claim, so a later publish can try again.</summary>
    Task ReleaseAsync(Guid contentKey);
}

public class PushedContentRepository : IPushedContentRepository
{
    private readonly IScopeProvider _scopeProvider;
    private readonly TimeProvider _timeProvider;

    public PushedContentRepository(IScopeProvider scopeProvider, TimeProvider timeProvider)
    {
        _scopeProvider = scopeProvider;
        _timeProvider = timeProvider;
    }

    public async Task<bool> TryClaimAsync(Guid contentKey)
    {
        using IScope scope = _scopeProvider.CreateScope();
        bool claimed = await PushedContentQueries.TryClaimAsync(scope.Database, contentKey, _timeProvider.GetUtcNow().UtcDateTime);
        scope.Complete();
        return claimed;
    }

    public async Task ReleaseAsync(Guid contentKey)
    {
        using IScope scope = _scopeProvider.CreateScope();
        await PushedContentQueries.ReleaseAsync(scope.Database, contentKey);
        scope.Complete();
    }
}

/// <summary>The SQL, kept apart from Umbraco scopes so it can be tested against a plain NPoco database.</summary>
internal static class PushedContentQueries
{
    public static async Task<bool> TryClaimAsync(IDatabase db, Guid contentKey, DateTime nowUtc)
    {
        string key = contentKey.ToString("D");
        try
        {
            // Insert only if absent. The primary key catches the rare race where two runs both pass the check.
            int inserted = await db.ExecuteAsync(
                $"""
                INSERT INTO {PushedContentDto.TableName} (ContentKey, SentUtc)
                SELECT @0, @1 WHERE NOT EXISTS (SELECT 1 FROM {PushedContentDto.TableName} WHERE ContentKey = @0)
                """,
                key,
                nowUtc);
            return inserted == 1;
        }
        catch (DbException)
        {
            if (await ExistsAsync(db, key))
            {
                return false;
            }

            throw;
        }
    }

    public static Task ReleaseAsync(IDatabase db, Guid contentKey)
        => db.ExecuteAsync($"DELETE FROM {PushedContentDto.TableName} WHERE ContentKey = @0", contentKey.ToString("D"));

    public static async Task<bool> ExistsAsync(IDatabase db, string key)
        => await db.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {PushedContentDto.TableName} WHERE ContentKey = @0", key) > 0;
}
