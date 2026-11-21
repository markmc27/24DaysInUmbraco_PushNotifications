using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace NotificationsRevisit.Announcements.Persistence;

/// <summary>A browser's push subscription. One row per endpoint (unique index).</summary>
[TableName(TableName)]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class PushSubscriptionDto
{
    public const string TableName = "announcementsPushSubscription";

    /// <summary>Endpoint URLs longer than this are rejected rather than truncated.</summary>
    public const int EndpointMaxLength = 800;

    [Column("Id")]
    [PrimaryKeyColumn(AutoIncrement = true)]
    public int Id { get; set; }

    [Column("Endpoint")]
    [Length(EndpointMaxLength)]
    [Index(IndexTypes.UniqueNonClustered, Name = "IX_" + TableName + "_Endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    [Column("P256dh")]
    [Length(255)]
    public string P256dh { get; set; } = string.Empty;

    [Column("Auth")]
    [Length(255)]
    public string Auth { get; set; } = string.Empty;

    [Column("CreatedUtc")]
    public DateTime CreatedUtc { get; set; }
}
