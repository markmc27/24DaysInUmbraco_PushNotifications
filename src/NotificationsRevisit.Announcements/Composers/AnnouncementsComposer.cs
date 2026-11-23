using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NotificationsRevisit.Announcements.Automation;
using NotificationsRevisit.Announcements.Persistence;
using NotificationsRevisit.Announcements.Push;
using NotificationsRevisit.Announcements.Subscriptions;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace NotificationsRevisit.Announcements.Composers;

/// <summary>
/// Registers the push services. The "Send push notification" Automate action needs no registration:
/// Automate discovers it from its [Action] attribute.
/// </summary>
public class AnnouncementsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.TryAddSingleton(TimeProvider.System);

        // Persistence
        builder.Services.AddSingleton<IPushSubscriptionRepository, PushSubscriptionRepository>();
        builder.Services.AddSingleton<IPushedContentRepository, PushedContentRepository>();

        // Push delivery: options validated at startup, HttpClient managed by IHttpClientFactory
        builder.Services.AddOptions<PushOptions>()
            .Bind(builder.Config.GetSection(PushOptions.SectionName))
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<PushOptions>, PushOptionsValidator>();
        builder.Services.AddHttpClient<IPushSender, WebPushSender>();
        builder.Services.AddTransient<IPushService, PushService>();

        // Public subscribe endpoint: per-IP rate limit
        builder.AddSubscribeRateLimiting();

        // The push action resolves page URLs outside a request
        builder.Services.AddTransient<IContentUrlResolver, ContentUrlResolver>();
    }
}
