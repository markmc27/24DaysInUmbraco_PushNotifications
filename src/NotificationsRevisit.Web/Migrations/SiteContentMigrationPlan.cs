using Umbraco.Cms.Core.Packaging;

namespace NotificationsRevisit.Web.Migrations;

/// <summary>
/// The site's own content model: Home, an Announcements folder and the Announcement page, with
/// templates and the two fixed nodes, so a fresh clone runs end to end without backoffice set-up.
/// The "Announce new pages" automation filters on the Announcement type by its fixed key, so keep the keys stable.
/// </summary>
public class SiteContentMigrationPlan : PackageMigrationPlan
{
    public SiteContentMigrationPlan()
        : base("NotificationsRevisit.Site")
    {
    }

    protected override void DefinePlan()
    {
        To<CreateSiteContent>(new Guid("8F0B6D2E-1A47-4C59-9E3B-6D2C7A5F4E10"));
    }
}
