using Umbraco.Cms.Core.Packaging;

namespace NotificationsRevisit.Announcements.Migrations;

/// <summary>
/// Versioned schema for the extension. Umbraco discovers package migration plans, runs pending steps
/// at startup and records them, so they show in the backoffice Packages section.
/// </summary>
public class AnnouncementsMigrationPlan : PackageMigrationPlan
{
    public AnnouncementsMigrationPlan()
        : base("NotificationsRevisit.Announcements")
    {
    }

    protected override void DefinePlan()
    {
        To<CreateAnnouncementsTables>(new Guid("3C1E7A52-6E0B-4C1F-9B7D-2F4B8C1D5A01"));
        To<AddPushedContentTable>(new Guid("3C1E7A52-6E0B-4C1F-9B7D-2F4B8C1D5A02"));
        To<DropAnnouncementsHistoryTable>(new Guid("3C1E7A52-6E0B-4C1F-9B7D-2F4B8C1D5A03"));
    }
}
