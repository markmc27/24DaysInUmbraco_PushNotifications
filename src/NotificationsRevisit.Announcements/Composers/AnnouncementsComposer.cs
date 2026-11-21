using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NotificationsRevisit.Announcements.Announcements;
using NotificationsRevisit.Announcements.Authorization;
using NotificationsRevisit.Announcements.Persistence;
using NotificationsRevisit.Announcements.Push;
using NotificationsRevisit.Announcements.Subscriptions;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace NotificationsRevisit.Announcements.Composers;

/// <summary>Registers the announcements services.</summary>
public class AnnouncementsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.TryAddSingleton(TimeProvider.System);

        // The site's content model aliases, validated at startup
        builder.Services.AddOptions<ContentModelOptions>()
            .Bind(builder.Config.GetSection(ContentModelOptions.SectionName))
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<ContentModelOptions>, ContentModelOptionsValidator>();

        // Persistence
        builder.Services.AddSingleton<IPushSubscriptionRepository, PushSubscriptionRepository>();
        builder.Services.AddSingleton<IAnnouncementHistoryRepository, AnnouncementHistoryRepository>();

        // Push delivery: options validated at startup, HttpClient managed by IHttpClientFactory
        builder.Services.AddOptions<PushOptions>()
            .Bind(builder.Config.GetSection(PushOptions.SectionName))
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<PushOptions>, PushOptionsValidator>();
        builder.Services.AddHttpClient<IPushSender, WebPushSender>();
        builder.Services.AddTransient<IPushService, PushService>();

        // Public subscribe endpoint: per-IP rate limit
        builder.AddSubscribeRateLimiting();

        // Backoffice API: Content access and not a translator; the announcement flow (design D5)
        builder.AddAnnouncementsAuthorization();
        builder.Services.AddTransient<IAnnouncementPages, AnnouncementPages>();
        builder.Services.AddTransient<IAnnouncementService, AnnouncementService>();
    }
}
