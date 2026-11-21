using System.Globalization;
using NotificationsRevisit.Announcements.Persistence;
using NotificationsRevisit.Announcements.Push;

namespace NotificationsRevisit.Announcements.Announcements;

public record CreateAnnouncement(string? ShortName, string? Heading, string? MainText);

/// <summary>The backoffice user sending the announcement.</summary>
public record AnnouncementSender(Guid Key, string Name);

public record AnnouncementSent(Guid PageKey, string Url, PushSendResult Delivery);

/// <summary>Either the sent announcement, or why nothing was sent.</summary>
public record AnnouncementOutcome(AnnouncementSent? Sent, string? Error)
{
    public bool Success => Sent is not null;

    public static AnnouncementOutcome Failed(string error) => new(null, error);
}

public interface IAnnouncementService
{
    Task<AnnouncementOutcome> CreateAndSendAsync(CreateAnnouncement request, AnnouncementSender sender, CancellationToken cancellationToken = default);
}

/// <summary>
/// Design D5: validate, find the parent, create and publish the page, resolve its URL, push, record.
/// Push happens only after a successful publish; any earlier failure sends nothing.
/// </summary>
public class AnnouncementService : IAnnouncementService
{
    public const int ShortNameMaxLength = 100;
    public const int HeadingMaxLength = 255;

    private static readonly CultureInfo UkCulture = CultureInfo.GetCultureInfo("en-GB");

    private readonly IAnnouncementPages _pages;
    private readonly IPushService _push;
    private readonly IAnnouncementHistoryRepository _history;
    private readonly TimeProvider _timeProvider;

    public AnnouncementService(IAnnouncementPages pages, IPushService push, IAnnouncementHistoryRepository history, TimeProvider timeProvider)
    {
        _pages = pages;
        _push = push;
        _history = history;
        _timeProvider = timeProvider;
    }

    /// <summary>The page name: the sent date and the short name, as in 2018 (for example "20261121-pool-closed").</summary>
    public static string PageName(DateTime date, string shortName) => $"{date:yyyyMMdd}-{shortName.Trim()}";

    /// <summary>The notification message: "21 Nov Pool closed", as in 2018.</summary>
    public static string Message(DateTime date, string heading) => $"{date.ToString("dd MMM", UkCulture)} {heading.Trim()}";

    public async Task<AnnouncementOutcome> CreateAndSendAsync(CreateAnnouncement request, AnnouncementSender sender, CancellationToken cancellationToken = default)
    {
        string? invalid = Validate(request);
        if (invalid is not null)
        {
            return AnnouncementOutcome.Failed(invalid);
        }

        Guid? parentKey = await _pages.FindParentKeyAsync();
        if (parentKey is null)
        {
            return AnnouncementOutcome.Failed(
                "There is no announcements folder to create the page in. Create a content node of the configured announcements parent type first.");
        }

        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
        string heading = request.Heading!.Trim();

        PageResult page = await _pages.CreateAndPublishAsync(
            parentKey.Value, PageName(now, request.ShortName!), heading, request.MainText?.Trim() ?? string.Empty, sender.Key);
        if (!page.Success)
        {
            return AnnouncementOutcome.Failed(page.Error!);
        }

        PushSendResult delivery = await _push.SendAsync(Message(now, heading), page.Url!, cancellationToken);

        await _history.InsertAsync(new AnnouncementHistoryDto
        {
            Heading = heading,
            SentUtc = now,
            UserKey = sender.Key,
            UserName = sender.Name,
            PageKey = page.Key!.Value,
            Attempted = delivery.Attempted,
            Succeeded = delivery.Succeeded,
            Failed = delivery.Failed,
        });

        return new AnnouncementOutcome(new AnnouncementSent(page.Key.Value, page.Url!, delivery), null);
    }

    private static string? Validate(CreateAnnouncement request)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(request.ShortName)) missing.Add("short name");
        if (string.IsNullOrWhiteSpace(request.Heading)) missing.Add("heading");
        if (missing.Count > 0)
        {
            return $"Please enter a {string.Join(" and a ", missing)}.";
        }

        if (request.ShortName!.Trim().Length > ShortNameMaxLength)
        {
            return $"The short name must be {ShortNameMaxLength} characters or fewer.";
        }

        if (request.Heading!.Trim().Length > HeadingMaxLength)
        {
            return $"The heading must be {HeadingMaxLength} characters or fewer.";
        }

        return null;
    }
}
