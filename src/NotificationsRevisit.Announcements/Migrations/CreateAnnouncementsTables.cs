using NotificationsRevisit.Announcements.Persistence;
using Umbraco.Cms.Infrastructure.Migrations;

namespace NotificationsRevisit.Announcements.Migrations;

/// <summary>Creates the push subscription table.</summary>
/// <remarks>
/// Approach 1 also created an announcement history table here. Approach 2 removed it, so a fresh
/// database no longer gets it, and <see cref="DropAnnouncementsHistoryTable"/> drops it from
/// databases that already have it.
/// </remarks>
public class CreateAnnouncementsTables : AsyncMigrationBase
{
    public CreateAnnouncementsTables(IMigrationContext context)
        : base(context)
    {
    }

    protected override Task MigrateAsync()
    {
        if (!TableExists(PushSubscriptionDto.TableName))
        {
            Create.Table<PushSubscriptionDto>().Do();
        }

        return Task.CompletedTask;
    }
}
