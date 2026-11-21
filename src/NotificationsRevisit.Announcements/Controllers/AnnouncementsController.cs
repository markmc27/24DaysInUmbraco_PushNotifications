using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NotificationsRevisit.Announcements.Announcements;
using NotificationsRevisit.Announcements.Authorization;
using NotificationsRevisit.Announcements.Persistence;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Web.Common.Routing;

namespace NotificationsRevisit.Announcements.Controllers;

/// <summary>
/// Backoffice API for the Announcements dashboard, at /umbraco/announcements/api/v1/.
/// 2018: an UmbracoAuthorizedJsonController with [PluginController], called with $http.
/// </summary>
[ApiVersion("1.0")]
[BackOfficeRoute("announcements/api/v{version:apiVersion}")]
[ApiExplorerSettings(GroupName = "Announcements")]
[MapToApi(Constants.ApiName)]
[Authorize(Policy = AnnouncementsAuthorization.PolicyName)]
public class AnnouncementsController : ManagementApiControllerBase
{
    private readonly IPushSubscriptionRepository _subscriptions;
    private readonly IAnnouncementHistoryRepository _history;
    private readonly IAnnouncementService _announcements;
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;

    public AnnouncementsController(
        IPushSubscriptionRepository subscriptions,
        IAnnouncementHistoryRepository history,
        IAnnouncementService announcements,
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor)
    {
        _subscriptions = subscriptions;
        _history = history;
        _announcements = announcements;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    /// <summary>How many browsers are subscribed to push notifications.</summary>
    [HttpGet("subscriptions/count")]
    [ProducesResponseType<SubscriberCountResponse>(StatusCodes.Status200OK)]
    public async Task<SubscriberCountResponse> GetSubscriberCount()
        => new(await _subscriptions.CountAsync());

    /// <summary>Sent announcements, newest first.</summary>
    [HttpGet("history")]
    [ProducesResponseType<IEnumerable<AnnouncementHistoryItem>>(StatusCodes.Status200OK)]
    public async Task<IEnumerable<AnnouncementHistoryItem>> GetHistory()
        => (await _history.GetAllAsync()).Select(h => new AnnouncementHistoryItem(
            h.Id, h.Heading, DateTime.SpecifyKind(h.SentUtc, DateTimeKind.Utc), h.UserName, h.PageKey, h.Attempted, h.Succeeded, h.Failed));

    /// <summary>Creates and publishes an announcement page, then pushes it to every subscriber.</summary>
    [HttpPost("announcements")]
    [ProducesResponseType<CreateAnnouncementResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAnnouncement(CreateAnnouncementRequest request, CancellationToken cancellationToken)
    {
        IUser? user = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
        if (user is null)
        {
            return Unauthorized();
        }

        AnnouncementOutcome outcome = await _announcements.CreateAndSendAsync(
            new CreateAnnouncement(request.ShortName, request.Heading, request.MainText),
            new AnnouncementSender(user.Key, user.Name ?? user.Username),
            cancellationToken);

        if (!outcome.Success)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "The announcement was not sent",
                Detail = outcome.Error,
                Status = StatusCodes.Status400BadRequest,
            });
        }

        AnnouncementSent sent = outcome.Sent!;
        return Ok(new CreateAnnouncementResponse(
            sent.PageKey, sent.Url, sent.Delivery.Attempted, sent.Delivery.Succeeded, sent.Delivery.Failed, sent.Delivery.Removed));
    }
}

public record SubscriberCountResponse(int Count);

public record AnnouncementHistoryItem(
    int Id, string Heading, DateTime SentUtc, string UserName, Guid PageKey, int Attempted, int Succeeded, int Failed);

public record CreateAnnouncementRequest(string? ShortName, string? Heading, string? MainText);

public record CreateAnnouncementResponse(Guid PageKey, string Url, int Attempted, int Succeeded, int Failed, int Removed);
