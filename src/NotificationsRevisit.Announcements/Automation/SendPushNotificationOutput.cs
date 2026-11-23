namespace NotificationsRevisit.Announcements.Automation;

/// <summary>What the "Send push notification" action reports for later steps and the run log.</summary>
public class SendPushNotificationOutput
{
    public int Attempted { get; set; }

    public int Succeeded { get; set; }

    public int Failed { get; set; }

    public int Removed { get; set; }

    public bool AlreadySent { get; set; }
}
