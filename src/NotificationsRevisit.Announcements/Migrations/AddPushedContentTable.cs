using NotificationsRevisit.Announcements.Persistence;
using Umbraco.Cms.Infrastructure.Migrations;

namespace NotificationsRevisit.Announcements.Migrations;

/// <summary>Approach 2: the table that records which pages have been pushed.</summary>
public class AddPushedContentTable : AsyncMigrationBase
{
    public AddPushedContentTable(IMigrationContext context)
        : base(context)
    {
    }

    protected override Task MigrateAsync()
    {
        if (!TableExists(PushedContentDto.TableName))
        {
            Create.Table<PushedContentDto>().Do();
        }

        return Task.CompletedTask;
    }
}
