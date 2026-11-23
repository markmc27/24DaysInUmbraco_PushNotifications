using Umbraco.Cms.Infrastructure.Migrations;

namespace NotificationsRevisit.Announcements.Migrations;

/// <summary>Approach 2: drops approach 1's announcement history table. Automate's run history replaces it.</summary>
public class DropAnnouncementsHistoryTable : AsyncMigrationBase
{
    private const string TableName = "announcementsHistory";

    public DropAnnouncementsHistoryTable(IMigrationContext context)
        : base(context)
    {
    }

    protected override Task MigrateAsync()
    {
        if (TableExists(TableName))
        {
            Delete.Table(TableName).Do();
        }

        return Task.CompletedTask;
    }
}
