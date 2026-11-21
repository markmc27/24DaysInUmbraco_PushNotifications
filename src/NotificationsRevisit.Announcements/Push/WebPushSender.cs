using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationsRevisit.Announcements.Persistence;
using WebPush;

namespace NotificationsRevisit.Announcements.Push;

/// <summary>Delivers one push message to one subscription.</summary>
public interface IPushSender
{
    /// <summary>
    /// Returns the push service's HTTP status code, or <c>null</c> when no response was received
    /// (for example a network error).
    /// </summary>
    Task<HttpStatusCode?> SendAsync(PushSubscriptionDto subscription, string payload, CancellationToken cancellationToken);
}

/// <summary>Sends with the WebPush library, signing with the configured VAPID keys.</summary>
public class WebPushSender : IPushSender
{
    private readonly WebPushClient _client;
    private readonly IOptionsMonitor<PushOptions> _options;
    private readonly ILogger<WebPushSender> _logger;

    public WebPushSender(HttpClient httpClient, IOptionsMonitor<PushOptions> options, ILogger<WebPushSender> logger)
    {
        _client = new WebPushClient(httpClient);
        _options = options;
        _logger = logger;
    }

    public async Task<HttpStatusCode?> SendAsync(PushSubscriptionDto subscription, string payload, CancellationToken cancellationToken)
    {
        VapidOptions vapid = _options.CurrentValue.Vapid;
        var target = new PushSubscription(subscription.Endpoint, subscription.P256dh, subscription.Auth);
        var details = new VapidDetails(vapid.Subject, vapid.PublicKey, vapid.PrivateKey);

        try
        {
            await _client.SendNotificationAsync(target, payload, details, cancellationToken);
            return HttpStatusCode.Created;
        }
        catch (WebPushException ex)
        {
            return ex.StatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Push delivery to {Endpoint} got no response", subscription.Endpoint);
            return null;
        }
    }
}
