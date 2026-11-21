using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Models.ContentPublishing;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;

namespace NotificationsRevisit.Announcements.Announcements;

/// <summary>Either the published page's key and absolute URL, or why it failed.</summary>
public record PageResult(Guid? Key, string? Url, string? Error)
{
    public bool Success => Error is null;

    public static PageResult Failed(string error) => new(null, null, error);
}

/// <summary>The content side of an announcement: where pages go, and creating and publishing one.</summary>
public interface IAnnouncementPages
{
    /// <summary>The key of the first node of the configured parent type, or null when there is none.</summary>
    Task<Guid?> FindParentKeyAsync();

    Task<PageResult> CreateAndPublishAsync(Guid parentKey, string name, string heading, string mainText, Guid userKey);
}

/// <summary>
/// Uses the async content editing and publishing services that came with the new backoffice,
/// rather than the legacy <c>IContentService.SaveAndPublish</c>.
/// </summary>
public class AnnouncementPages : IAnnouncementPages
{
    private readonly IContentTypeService _contentTypeService;
    private readonly IContentService _contentService;
    private readonly IContentEditingService _contentEditingService;
    private readonly IContentPublishingService _contentPublishingService;
    private readonly IPublishedUrlProvider _urlProvider;
    private readonly IOptionsMonitor<ContentModelOptions> _options;

    public AnnouncementPages(
        IContentTypeService contentTypeService,
        IContentService contentService,
        IContentEditingService contentEditingService,
        IContentPublishingService contentPublishingService,
        IPublishedUrlProvider urlProvider,
        IOptionsMonitor<ContentModelOptions> options)
    {
        _contentTypeService = contentTypeService;
        _contentService = contentService;
        _contentEditingService = contentEditingService;
        _contentPublishingService = contentPublishingService;
        _urlProvider = urlProvider;
        _options = options;
    }

    public Task<Guid?> FindParentKeyAsync()
    {
        IContentType? parentType = _contentTypeService.Get(_options.CurrentValue.ParentDocTypeAlias);
        if (parentType is null)
        {
            return Task.FromResult<Guid?>(null);
        }

        IContent? parent = _contentService
            .GetPagedOfType(parentType.Id, 0, 1, out _, filter: null!)
            .FirstOrDefault(c => !c.Trashed);

        return Task.FromResult(parent?.Key);
    }

    public async Task<PageResult> CreateAndPublishAsync(Guid parentKey, string name, string heading, string mainText, Guid userKey)
    {
        ContentModelOptions model = _options.CurrentValue;

        IContentType? pageType = _contentTypeService.Get(model.PageDocTypeAlias);
        if (pageType is null)
        {
            return PageResult.Failed($"The announcement page type '{model.PageDocTypeAlias}' does not exist.");
        }

        var created = await _contentEditingService.CreateAsync(
            new ContentCreateModel
            {
                ContentTypeKey = pageType.Key,
                ParentKey = parentKey,
                TemplateKey = pageType.DefaultTemplate?.Key,
                Variants = [new VariantModel { Name = name }],
                Properties =
                [
                    new PropertyValueModel { Alias = model.HeadingPropertyAlias, Value = heading },
                    new PropertyValueModel { Alias = model.TextPropertyAlias, Value = mainText },
                ],
            },
            userKey);

        if (!created.Success || created.Result.Content is null)
        {
            return PageResult.Failed($"The announcement page could not be created ({created.Status}).");
        }

        Guid key = created.Result.Content.Key;

        var published = await _contentPublishingService.PublishAsync(key, [new CulturePublishScheduleModel { Culture = null }], userKey);
        if (!published.Success)
        {
            return PageResult.Failed($"The announcement page was saved but could not be published ({published.Status}), so no notification was sent.");
        }

        string url = _urlProvider.GetUrl(key, UrlMode.Absolute);
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            return PageResult.Failed($"The announcement page was published but has no public URL ('{url}'), so no notification was sent.");
        }

        return new PageResult(key, url, null);
    }
}
