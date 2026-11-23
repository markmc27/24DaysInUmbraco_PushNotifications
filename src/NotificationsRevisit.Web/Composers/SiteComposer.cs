using NotificationsRevisit.Web.Automations;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Notifications;

namespace NotificationsRevisit.Web.Composers;

/// <summary>Registers the site's own start-up work.</summary>
public class SiteComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, AnnounceAutomationStartedHandler>();
}
