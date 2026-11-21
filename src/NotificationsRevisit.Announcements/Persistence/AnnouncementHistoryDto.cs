using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace NotificationsRevisit.Announcements.Persistence;

/// <summary>One sent announcement and its delivery counts.</summary>
[TableName(TableName)]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class AnnouncementHistoryDto
{
    public const string TableName = "announcementsHistory";

    [Column("Id")]
    [PrimaryKeyColumn(AutoIncrement = true)]
    public int Id { get; set; }

    [Column("Heading")]
    [Length(255)]
    public string Heading { get; set; } = string.Empty;

    [Column("SentUtc")]
    public DateTime SentUtc { get; set; }

    [Column("UserKey")]
    public Guid UserKey { get; set; }

    [Column("UserName")]
    [Length(255)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>The page key, not its integer id: the backoffice routes by key.</summary>
    [Column("PageKey")]
    public Guid PageKey { get; set; }

    [Column("Attempted")]
    public int Attempted { get; set; }

    [Column("Succeeded")]
    public int Succeeded { get; set; }

    [Column("Failed")]
    public int Failed { get; set; }
}
