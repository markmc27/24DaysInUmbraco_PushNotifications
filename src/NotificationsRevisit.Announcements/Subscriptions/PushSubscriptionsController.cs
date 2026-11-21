using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NotificationsRevisit.Announcements.Persistence;

namespace NotificationsRevisit.Announcements.Subscriptions;

/// <summary>
/// Public endpoint for site visitors to store a push subscription. A plain ASP.NET Core controller:
/// a public JSON endpoint needs no Umbraco base class.
/// </summary>
[ApiController]
[Route("api/announcements/push-subscriptions")]
[ApiExplorerSettings(IgnoreApi = true)]
public class PushSubscriptionsController : ControllerBase
{
    private readonly IPushSubscriptionRepository _subscriptions;

    public PushSubscriptionsController(IPushSubscriptionRepository subscriptions) => _subscriptions = subscriptions;

    /// <summary>Stores the subscription, or refreshes the keys of an existing one for the same endpoint.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(SubscribeRateLimiting.PolicyName)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Post([FromBody] PushSubscriptionRequest request)
    {
        await _subscriptions.UpsertAsync(request.Endpoint!, request.Keys!.P256dh!, request.Keys.Auth!);
        return NoContent();
    }
}

/// <summary>The shape of the browser's <c>PushSubscription.toJSON()</c>, so the client can post it as is.</summary>
public class PushSubscriptionRequest
{
    [Required]
    [MaxLength(PushSubscriptionDto.EndpointMaxLength)]
    [Url]
    public string? Endpoint { get; set; }

    [Required]
    public PushSubscriptionKeys? Keys { get; set; }
}

public class PushSubscriptionKeys
{
    [Required]
    [MaxLength(255)]
    public string? P256dh { get; set; }

    [Required]
    [MaxLength(255)]
    public string? Auth { get; set; }
}
