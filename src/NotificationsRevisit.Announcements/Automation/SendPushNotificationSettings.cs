using Umbraco.Automate.Core.Settings;

namespace NotificationsRevisit.Announcements.Automation;

/// <summary>Settings for the "Send push notification" action. Both accept bindings.</summary>
public class SendPushNotificationSettings
{
    [Field(Label = "Message", Description = "The notification text, for example the page's heading.", SupportsBindings = true, SortOrder = 1)]
    public string? Message { get; set; }

    [Field(Label = "Content key", Description = "The page the notification opens when clicked.", SupportsBindings = true, SortOrder = 2)]
    public string? ContentKey { get; set; }
}
