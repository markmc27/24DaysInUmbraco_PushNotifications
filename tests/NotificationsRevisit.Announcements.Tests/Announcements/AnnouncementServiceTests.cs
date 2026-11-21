using NotificationsRevisit.Announcements.Announcements;
using NotificationsRevisit.Announcements.Persistence;
using NotificationsRevisit.Announcements.Push;

namespace NotificationsRevisit.Announcements.Tests.Announcements;

public class AnnouncementServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 21, 13, 30, 0, TimeSpan.Zero);
    private static readonly AnnouncementSender Editor = new(Guid.NewGuid(), "Editor");

    private readonly FakePages _pages = new();
    private readonly FakePush _push = new();
    private readonly FakeHistory _history = new();

    private AnnouncementService CreateService() => new(_pages, _push, _history, new FixedTime(Now));

    [Theory]
    [InlineData("pool-closed", "20261121-pool-closed")]
    [InlineData("  gala  ", "20261121-gala")]
    public void Page_is_named_from_the_date_and_short_name(string shortName, string expected)
        => Assert.Equal(expected, AnnouncementService.PageName(Now.UtcDateTime, shortName));

    [Fact]
    public void Message_is_day_month_and_heading()
        => Assert.Equal("21 Nov Pool closed", AnnouncementService.Message(Now.UtcDateTime, " Pool closed "));

    [Fact]
    public async Task Success_creates_the_page_sends_to_its_url_and_records_history()
    {
        AnnouncementOutcome outcome = await CreateService().CreateAndSendAsync(
            new CreateAnnouncement("pool-closed", "Pool closed", "No swimming tonight."), Editor);

        Assert.True(outcome.Success);
        Assert.Equal("20261121-pool-closed", _pages.CreatedName);
        Assert.Equal(("21 Nov Pool closed", FakePages.Url), Assert.Single(_push.Sent));
        AnnouncementHistoryDto record = Assert.Single(_history.Records);
        Assert.Equal("Pool closed", record.Heading);
        Assert.Equal(FakePages.PageKey, record.PageKey);
        Assert.Equal(Editor.Key, record.UserKey);
        Assert.Equal(Now.UtcDateTime, record.SentUtc);
        Assert.Equal((3, 2, 1), (record.Attempted, record.Succeeded, record.Failed));
    }

    [Fact]
    public async Task Failed_publish_sends_nothing_and_records_nothing()
    {
        _pages.PublishError = "The announcement page was saved but could not be published (ContentInvalid), so no notification was sent.";

        AnnouncementOutcome outcome = await CreateService().CreateAndSendAsync(
            new CreateAnnouncement("pool-closed", "Pool closed", null), Editor);

        Assert.False(outcome.Success);
        Assert.Equal(_pages.PublishError, outcome.Error);
        Assert.Empty(_push.Sent);
        Assert.Empty(_history.Records);
    }

    [Fact]
    public async Task Missing_parent_creates_nothing_and_sends_nothing()
    {
        _pages.ParentKey = null;

        AnnouncementOutcome outcome = await CreateService().CreateAndSendAsync(
            new CreateAnnouncement("pool-closed", "Pool closed", null), Editor);

        Assert.False(outcome.Success);
        Assert.Contains("no announcements folder", outcome.Error);
        Assert.Null(_pages.CreatedName);
        Assert.Empty(_push.Sent);
    }

    [Theory]
    [InlineData(null, "Pool closed", "short name")]
    [InlineData("pool-closed", "  ", "heading")]
    [InlineData("", null, "short name and a heading")]
    public async Task Required_fields_are_checked_before_anything_happens(string? shortName, string? heading, string expectedInError)
    {
        AnnouncementOutcome outcome = await CreateService().CreateAndSendAsync(new CreateAnnouncement(shortName, heading, null), Editor);

        Assert.False(outcome.Success);
        Assert.Contains(expectedInError, outcome.Error);
        Assert.Null(_pages.CreatedName);
        Assert.Empty(_push.Sent);
    }

    private sealed class FakePages : IAnnouncementPages
    {
        public static readonly Guid PageKey = Guid.NewGuid();
        public const string Url = "https://site.test/announcements/20261121-pool-closed/";

        public Guid? ParentKey { get; set; } = Guid.NewGuid();

        public string? PublishError { get; set; }

        public string? CreatedName { get; private set; }

        public Task<Guid?> FindParentKeyAsync() => Task.FromResult(ParentKey);

        public Task<PageResult> CreateAndPublishAsync(Guid parentKey, string name, string heading, string mainText, Guid userKey)
        {
            CreatedName = name;
            return Task.FromResult(PublishError is null ? new PageResult(PageKey, Url, null) : PageResult.Failed(PublishError));
        }
    }

    private sealed class FakePush : IPushService
    {
        public List<(string Message, string Url)> Sent { get; } = [];

        public Task<PushSendResult> SendAsync(string message, string url, CancellationToken cancellationToken = default)
        {
            Sent.Add((message, url));
            return Task.FromResult(new PushSendResult(3, 2, 1, 0));
        }
    }

    private sealed class FakeHistory : IAnnouncementHistoryRepository
    {
        public List<AnnouncementHistoryDto> Records { get; } = [];

        public Task InsertAsync(AnnouncementHistoryDto record)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AnnouncementHistoryDto>> GetAllAsync() => Task.FromResult<IReadOnlyList<AnnouncementHistoryDto>>(Records);
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
