using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace NotificationsRevisit.Announcements.Persistence;

/// <summary>A content item the push action has claimed (and so sent, or is sending).</summary>
[TableName(TableName)]
[PrimaryKey("ContentKey", AutoIncrement = false)]
[ExplicitColumns]
public class PushedContentDto
{
    public const string TableName = "announcementsPushedContent";

    /// <summary>The content key as a string ("D" format), so SQLite and SQL Server compare it the same way.</summary>
    [Column("ContentKey")]
    [PrimaryKeyColumn(AutoIncrement = false)]
    [Length(36)]
    public string ContentKey { get; set; } = string.Empty;

    [Column("SentUtc")]
    public DateTime SentUtc { get; set; }
}
