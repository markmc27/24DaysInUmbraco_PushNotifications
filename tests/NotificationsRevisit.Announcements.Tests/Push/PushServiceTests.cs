using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NotificationsRevisit.Announcements.Persistence;
using NotificationsRevisit.Announcements.Push;

namespace NotificationsRevisit.Announcements.Tests.Push;

public class PushServiceTests
{
    private readonly FakeRepository _repository = new();
    private readonly FakeSender _sender = new();

    private PushService CreateService() => new(
        _repository,
        _sender,
        new StaticOptionsMonitor(new PushOptions { Title = "Swim club", IconUrl = "/icon.png", MaxConcurrency = 2 }),
        NullLogger<PushService>.Instance);

    [Fact]
    public async Task All_successful_deliveries_are_counted()
    {
        _repository.Add("https://push.example/a", "https://push.example/b", "https://push.example/c");

        PushSendResult result = await CreateService().SendAsync("21 Nov Pool closed", "https://site/a");

        Assert.Equal(new PushSendResult(3, 3, 0, 0), result);
        Assert.Equal(3, _sender.Calls.Count);
        Assert.Empty(_repository.Deleted);
    }

    [Fact]
    public async Task Mixed_outcome_reports_counts_and_removes_only_the_gone_subscription()
    {
        _repository.Add("https://push.example/ok1", "https://push.example/ok2", "https://push.example/fail", "https://push.example/gone");
        _sender.Status["https://push.example/fail"] = HttpStatusCode.InternalServerError;
        _sender.Status["https://push.example/gone"] = HttpStatusCode.Gone;

        PushSendResult result = await CreateService().SendAsync("msg", "https://site/a");

        Assert.Equal(new PushSendResult(Attempted: 4, Succeeded: 2, Failed: 1, Removed: 1), result);
        Assert.Equal(["https://push.example/gone"], _repository.Deleted);
        Assert.Equal(4, _sender.Calls.Count);
    }

    [Fact]
    public async Task Not_found_also_counts_as_gone()
    {
        _repository.Add("https://push.example/missing");
        _sender.Status["https://push.example/missing"] = HttpStatusCode.NotFound;

        PushSendResult result = await CreateService().SendAsync("msg", "https://site/a");

        Assert.Equal(new PushSendResult(1, 0, 0, 1), result);
    }

    [Fact]
    public async Task No_response_is_a_failure_and_the_subscription_is_kept()
    {
        _repository.Add("https://push.example/offline");
        _sender.Status["https://push.example/offline"] = null;

        PushSendResult result = await CreateService().SendAsync("msg", "https://site/a");

        Assert.Equal(new PushSendResult(1, 0, 1, 0), result);
        Assert.Empty(_repository.Deleted);
    }

    [Fact]
    public async Task No_subscribers_attempts_nothing()
    {
        PushSendResult result = await CreateService().SendAsync("msg", "https://site/a");

        Assert.Equal(new PushSendResult(0, 0, 0, 0), result);
        Assert.Empty(_sender.Calls);
    }

    [Fact]
    public async Task Payload_carries_configured_title_message_url_and_icon()
    {
        _repository.Add("https://push.example/a");

        await CreateService().SendAsync("21 Nov Pool closed", "https://site/announcements/20261121-pool-closed/");

        using JsonDocument payload = JsonDocument.Parse(_sender.Calls.Single().Payload);
        JsonElement root = payload.RootElement;
        Assert.Equal("Swim club", root.GetProperty("title").GetString());
        Assert.Equal("21 Nov Pool closed", root.GetProperty("message").GetString());
        Assert.Equal("https://site/announcements/20261121-pool-closed/", root.GetProperty("url").GetString());
        Assert.Equal("/icon.png", root.GetProperty("icon").GetString());
    }

    private sealed class FakeSender : IPushSender
    {
        public ConcurrentDictionary<string, HttpStatusCode?> Status { get; } = new();

        public ConcurrentBag<(string Endpoint, string Payload)> Calls { get; } = [];

        public Task<HttpStatusCode?> SendAsync(PushSubscriptionDto subscription, string payload, CancellationToken cancellationToken)
        {
            Calls.Add((subscription.Endpoint, payload));
            return Task.FromResult(Status.TryGetValue(subscription.Endpoint, out HttpStatusCode? s) ? s : HttpStatusCode.Created);
        }
    }

    private sealed class FakeRepository : IPushSubscriptionRepository
    {
        private readonly List<PushSubscriptionDto> _rows = [];

        public List<string> Deleted { get; } = [];

        public void Add(params string[] endpoints)
        {
            foreach (string e in endpoints)
            {
                _rows.Add(new PushSubscriptionDto { Id = _rows.Count + 1, Endpoint = e, P256dh = "k", Auth = "a" });
            }
        }

        public Task UpsertAsync(string endpoint, string p256dh, string auth) => throw new NotSupportedException();

        public Task<IReadOnlyList<PushSubscriptionDto>> GetAllAsync() => Task.FromResult<IReadOnlyList<PushSubscriptionDto>>(_rows.ToList());

        public Task<int> CountAsync() => Task.FromResult(_rows.Count);

        public Task<int> DeleteByEndpointsAsync(IReadOnlyCollection<string> endpoints)
        {
            Deleted.AddRange(endpoints);
            return Task.FromResult(_rows.RemoveAll(r => endpoints.Contains(r.Endpoint)));
        }
    }

    private sealed class StaticOptionsMonitor(PushOptions value) : IOptionsMonitor<PushOptions>
    {
        public PushOptions CurrentValue => value;

        public PushOptions Get(string? name) => value;

        public IDisposable? OnChange(Action<PushOptions, string?> listener) => null;
    }
}
