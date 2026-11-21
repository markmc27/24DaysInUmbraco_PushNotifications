using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationsRevisit.Announcements.Persistence;

namespace NotificationsRevisit.Announcements.Push;

/// <summary>How a send went. <see cref="Removed"/> counts expired subscriptions deleted afterwards.</summary>
public record PushSendResult(int Attempted, int Succeeded, int Failed, int Removed);

public interface IPushService
{
    /// <summary>Sends a notification with the configured title to every stored subscription.</summary>
    Task<PushSendResult> SendAsync(string message, string url, CancellationToken cancellationToken = default);
}

public class PushService : IPushService
{
    private readonly IPushSubscriptionRepository _subscriptions;
    private readonly IPushSender _sender;
    private readonly IOptionsMonitor<PushOptions> _options;
    private readonly ILogger<PushService> _logger;

    public PushService(
        IPushSubscriptionRepository subscriptions,
        IPushSender sender,
        IOptionsMonitor<PushOptions> options,
        ILogger<PushService> logger)
    {
        _subscriptions = subscriptions;
        _sender = sender;
        _options = options;
        _logger = logger;
    }

    public async Task<PushSendResult> SendAsync(string message, string url, CancellationToken cancellationToken = default)
    {
        PushOptions options = _options.CurrentValue;
        IReadOnlyList<PushSubscriptionDto> subscriptions = await _subscriptions.GetAllAsync();
        if (subscriptions.Count == 0)
        {
            return new PushSendResult(0, 0, 0, 0);
        }

        // Same payload shape as 2018, plus the icon so the service worker does not hard-code it.
        string payload = JsonSerializer.Serialize(new
        {
            title = options.Title,
            message,
            url,
            icon = options.IconUrl,
        });

        int succeeded = 0;
        int failed = 0;
        var gone = new ConcurrentBag<string>();

        await Parallel.ForEachAsync(
            subscriptions,
            new ParallelOptions { MaxDegreeOfParallelism = options.MaxConcurrency, CancellationToken = cancellationToken },
            async (subscription, ct) =>
            {
                HttpStatusCode? status = await _sender.SendAsync(subscription, payload, ct);

                if (status is { } code && (int)code is >= 200 and < 300)
                {
                    Interlocked.Increment(ref succeeded);
                }
                else if (status is HttpStatusCode.NotFound or HttpStatusCode.Gone)
                {
                    gone.Add(subscription.Endpoint);
                }
                else
                {
                    Interlocked.Increment(ref failed);
                    _logger.LogWarning(
                        "Push delivery failed with status {StatusCode} for subscription {SubscriptionId}; keeping it",
                        status is null ? "no response" : (int)status.Value,
                        subscription.Id);
                }
            });

        // Remove expired subscriptions after the loop, not while iterating (the 2018 pattern).
        int removed = gone.IsEmpty ? 0 : await _subscriptions.DeleteByEndpointsAsync(gone.ToArray());
        if (removed > 0)
        {
            _logger.LogInformation("Removed {Count} expired push subscriptions", removed);
        }

        return new PushSendResult(subscriptions.Count, succeeded, failed, removed);
    }
}
