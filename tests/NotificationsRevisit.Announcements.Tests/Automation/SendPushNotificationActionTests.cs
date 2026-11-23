using NotificationsRevisit.Announcements.Automation;
using NotificationsRevisit.Announcements.Persistence;
using NotificationsRevisit.Announcements.Push;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Testing;

namespace NotificationsRevisit.Announcements.Tests.Automation;

public class SendPushNotificationActionTests
{
    private const string Url = "https://swim.example/announcements/gala/";
    private static readonly Guid PageKey = Guid.Parse("6a51404e-f35f-427b-84c5-87ab7d251309");

    private readonly FakePushService _push = new();
    private readonly FakePushedContent _pushed = new();
    private readonly FakeUrls _urls = new() { [PageKey] = Url };

    private Task<ActionResult> RunAsync(string? message = "Gala entries close Friday", string? contentKey = null)
        => ActionTestHarness.For<SendPushNotificationAction>()
            .WithService<IPushService>(_push)
            .WithService<IPushedContentRepository>(_pushed)
            .WithService<IContentUrlResolver>(_urls)
            .WithSettings(new SendPushNotificationSettings { Message = message, ContentKey = contentKey ?? PageKey.ToString() })
            .ExecuteAsync();

    [Fact]
    public async Task First_send_pushes_to_the_page_url_and_records_the_page()
    {
        _push.Result = new PushSendResult(4, 2, 1, 1);

        ActionResult result = await RunAsync();

        Assert.Equal(ActionResultStatus.Success, result.Status);
        (string message, string url) = Assert.Single(_push.Calls);
        Assert.Equal("Gala entries close Friday", message);
        Assert.Equal(Url, url);
        Assert.Contains(PageKey, _pushed.Claimed);

        var output = Assert.IsType<SendPushNotificationOutput>(result.OutputData);
        Assert.Equal((4, 2, 1, 1, false), (output.Attempted, output.Succeeded, output.Failed, output.Removed, output.AlreadySent));
    }

    [Fact]
    public async Task Already_sent_page_succeeds_without_sending()
    {
        _pushed.Claimed.Add(PageKey);

        ActionResult result = await RunAsync();

        Assert.Equal(ActionResultStatus.Success, result.Status);
        Assert.Empty(_push.Calls);
        Assert.True(Assert.IsType<SendPushNotificationOutput>(result.OutputData).AlreadySent);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Missing_message_fails_and_sends_nothing(string? message)
    {
        ActionResult result = await RunAsync(message: message);

        AssertValidationFailure(result, "Message");
        Assert.Empty(_push.Calls);
        Assert.Empty(_pushed.Claimed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-key")]
    public async Task Missing_or_invalid_content_key_fails_and_sends_nothing(string contentKey)
    {
        ActionResult result = await RunAsync(contentKey: contentKey);

        AssertValidationFailure(result, "Content key");
        Assert.Empty(_push.Calls);
    }

    [Fact]
    public async Task Content_with_no_public_url_fails_and_sends_nothing()
    {
        _urls.Remove(PageKey);

        ActionResult result = await RunAsync();

        AssertValidationFailure(result, "no public URL");
        Assert.Empty(_push.Calls);
        Assert.Empty(_pushed.Claimed);
    }

    [Fact]
    public async Task Claim_is_released_when_the_send_throws()
    {
        _push.Throw = new HttpRequestException("push service down");

        ActionResult result = await RunAsync();

        Assert.Equal(ActionResultStatus.Failed, result.Status);
        Assert.Same(_push.Throw, result.Exception);
        Assert.DoesNotContain(PageKey, _pushed.Claimed);
        Assert.Contains(PageKey, _pushed.Released);
    }

    private static void AssertValidationFailure(ActionResult result, string mentions)
    {
        Assert.Equal(ActionResultStatus.Failed, result.Status);
        Assert.Equal(StepRunErrorCategory.Validation, result.ErrorCategory);
        Assert.Contains(mentions, result.Exception?.Message);
    }

    private sealed class FakePushService : IPushService
    {
        public List<(string Message, string Url)> Calls { get; } = [];

        public PushSendResult Result { get; set; } = new(1, 1, 0, 0);

        public Exception? Throw { get; set; }

        public Task<PushSendResult> SendAsync(string message, string url, CancellationToken cancellationToken = default)
        {
            if (Throw is not null)
            {
                throw Throw;
            }

            Calls.Add((message, url));
            return Task.FromResult(Result);
        }
    }

    private sealed class FakePushedContent : IPushedContentRepository
    {
        public HashSet<Guid> Claimed { get; } = [];

        public List<Guid> Released { get; } = [];

        public Task<bool> TryClaimAsync(Guid contentKey) => Task.FromResult(Claimed.Add(contentKey));

        public Task ReleaseAsync(Guid contentKey)
        {
            Claimed.Remove(contentKey);
            Released.Add(contentKey);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUrls : Dictionary<Guid, string>, IContentUrlResolver
    {
        public string? GetAbsoluteUrl(Guid contentKey) => TryGetValue(contentKey, out string? url) ? url : null;
    }
}
