using NPoco;
using Umbraco.Cms.Infrastructure.Scoping;

namespace NotificationsRevisit.Announcements.Persistence;

public interface IAnnouncementHistoryRepository
{
    Task InsertAsync(AnnouncementHistoryDto record);

    /// <summary>All sent announcements, newest first.</summary>
    Task<IReadOnlyList<AnnouncementHistoryDto>> GetAllAsync();
}

public class AnnouncementHistoryRepository : IAnnouncementHistoryRepository
{
    private readonly IScopeProvider _scopeProvider;

    public AnnouncementHistoryRepository(IScopeProvider scopeProvider) => _scopeProvider = scopeProvider;

    public async Task InsertAsync(AnnouncementHistoryDto record)
    {
        using IScope scope = _scopeProvider.CreateScope();
        await AnnouncementHistoryQueries.InsertAsync(scope.Database, record);
        scope.Complete();
    }

    public async Task<IReadOnlyList<AnnouncementHistoryDto>> GetAllAsync()
    {
        using IScope scope = _scopeProvider.CreateScope(autoComplete: true);
        return await AnnouncementHistoryQueries.GetAllAsync(scope.Database);
    }
}

/// <summary>The SQL, kept apart from Umbraco scopes so it can be tested against a plain NPoco database.</summary>
internal static class AnnouncementHistoryQueries
{
    public static Task InsertAsync(IDatabase db, AnnouncementHistoryDto record) => db.InsertAsync(record);

    public static async Task<IReadOnlyList<AnnouncementHistoryDto>> GetAllAsync(IDatabase db)
        => await db.FetchAsync<AnnouncementHistoryDto>(
            $"SELECT * FROM {AnnouncementHistoryDto.TableName} ORDER BY SentUtc DESC, Id DESC");
}
