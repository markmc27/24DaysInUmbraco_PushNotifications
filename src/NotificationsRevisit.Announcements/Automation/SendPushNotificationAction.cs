using NotificationsRevisit.Announcements.Persistence;
using NotificationsRevisit.Announcements.Push;
using Umbraco.Automate.Core.Actions;

namespace NotificationsRevisit.Announcements.Automation;

/// <summary>
/// Sends a web push notification to every subscriber, linking to a content item's public URL.
/// Each content item is pushed once: the trigger fires on every publish, so the key is claimed
/// before sending and repeats are skipped (design D2 and D3).
/// </summary>
[Action("notificationsRevisit.sendPushNotification", "Send push notification",
    Description = "Sends a web push notification to every subscriber, once per content item.",
    Group = "Notifications",
    Icon = "icon-bell")]
public class SendPushNotificationAction : ActionBase<SendPushNotificationSettings, SendPushNotificationOutput>
{
    private readonly IPushService _push;
    private readonly IPushedContentRepository _pushed;
    private readonly IContentUrlResolver _urls;

    public SendPushNotificationAction(
        ActionInfrastructure infrastructure,
        IPushService push,
        IPushedContentRepository pushed,
        IContentUrlResolver urls)
        : base(infrastructure)
    {
        _push = push;
        _pushed = pushed;
        _urls = urls;
    }

    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        SendPushNotificationSettings settings = context.GetSettings<SendPushNotificationSettings>();

        string? message = settings.Message?.Trim();
        if (string.IsNullOrEmpty(message))
        {
            return Invalid("Message is required. Check its binding resolves to some text.");
        }

        if (!Guid.TryParse(settings.ContentKey, out Guid contentKey))
        {
            return Invalid($"Content key is required and must be a content item's key (got '{settings.ContentKey}').");
        }

        string? url = _urls.GetAbsoluteUrl(contentKey);
        if (url is null)
        {
            return Invalid($"Content {contentKey} is not published or has no public URL, so no notification was sent.");
        }

        if (!await _pushed.TryClaimAsync(contentKey))
        {
            context.LogInfo($"Content {contentKey} was already sent, so no notification was sent this time.");
            return Success(new SendPushNotificationOutput { AlreadySent = true });
        }

        PushSendResult result;
        try
        {
            result = await _push.SendAsync(message, url, cancellationToken);
        }
        catch (Exception ex)
        {
            // Not sent, so do not record it: a later publish can try again.
            await _pushed.ReleaseAsync(contentKey);
            context.LogError($"Sending failed, so content {contentKey} was not recorded as sent: {ex.Message}");
            if (ex is OperationCanceledException)
            {
                throw;
            }

            return ActionResult.Failed(ex);
        }

        context.LogInfo(
            $"Sent '{message}' linking to {url}: {result.Attempted} attempted, {result.Succeeded} succeeded, " +
            $"{result.Failed} failed, {result.Removed} expired subscriptions removed.");

        return Success(new SendPushNotificationOutput
        {
            Attempted = result.Attempted,
            Succeeded = result.Succeeded,
            Failed = result.Failed,
            Removed = result.Removed,
        });
    }

    private static ActionResult Invalid(string reason)
        => ActionResult.Failed(new ArgumentException(reason), StepRunErrorCategory.Validation);
}
