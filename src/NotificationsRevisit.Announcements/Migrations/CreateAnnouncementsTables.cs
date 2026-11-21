using NotificationsRevisit.Announcements.Persistence;
using Umbraco.Cms.Infrastructure.Migrations;

namespace NotificationsRevisit.Announcements.Migrations;

/// <summary>Creates the push subscription and announcement history tables.</summary>
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

        if (!TableExists(AnnouncementHistoryDto.TableName))
        {
            Create.Table<AnnouncementHistoryDto>().Do();
        }

        return Task.CompletedTask;
    }
}
