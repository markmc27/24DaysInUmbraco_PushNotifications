using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Web;

namespace NotificationsRevisit.Announcements.Automation;

/// <summary>Finds a published content item's absolute public URL.</summary>
public interface IContentUrlResolver
{
    /// <summary>The absolute URL, or null if the item is not published or has no routable URL.</summary>
    string? GetAbsoluteUrl(Guid contentKey);
}

public class ContentUrlResolver : IContentUrlResolver
{
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IPublishedUrlProvider _urlProvider;

    public ContentUrlResolver(IUmbracoContextFactory umbracoContextFactory, IPublishedUrlProvider urlProvider)
    {
        _umbracoContextFactory = umbracoContextFactory;
        _urlProvider = urlProvider;
    }

    public string? GetAbsoluteUrl(Guid contentKey)
    {
        // Automate runs steps in the background, outside a request, so make sure there is an Umbraco context.
        using UmbracoContextReference _ = _umbracoContextFactory.EnsureUmbracoContext();
        string url = _urlProvider.GetUrl(contentKey, UrlMode.Absolute);
        return Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) && uri.Scheme is "http" or "https" ? url : null;
    }
}
