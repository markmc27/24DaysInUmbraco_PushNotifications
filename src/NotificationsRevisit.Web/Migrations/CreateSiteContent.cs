using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Models.ContentPublishing;
using Umbraco.Cms.Core.Models.ContentTypeEditing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.ContentTypeEditing;
using Umbraco.Cms.Infrastructure.Migrations;

namespace NotificationsRevisit.Web.Migrations;

/// <summary>
/// Creates the templates, document types and the Home and Announcements nodes. Each step checks
/// first, so a partly set-up database is completed rather than duplicated.
/// </summary>
public class CreateSiteContent : AsyncMigrationBase
{
    // Fixed keys keep the content model the same on every machine.
    private static readonly Guid AnnouncementTypeKey = new("5E9A3B71-0C2D-4F8E-A6B1-3D7C9E2F1A01");
    private static readonly Guid AnnouncementsTypeKey = new("5E9A3B71-0C2D-4F8E-A6B1-3D7C9E2F1A02");
    private static readonly Guid HomeTypeKey = new("5E9A3B71-0C2D-4F8E-A6B1-3D7C9E2F1A03");
    private static readonly Guid HomeNodeKey = new("5E9A3B71-0C2D-4F8E-A6B1-3D7C9E2F1B01");
    private static readonly Guid AnnouncementsNodeKey = new("5E9A3B71-0C2D-4F8E-A6B1-3D7C9E2F1B02");

    private static readonly Guid UserKey = Constants.Security.SuperUserKey;

    private readonly ITemplateService _templateService;
    private readonly IContentTypeService _contentTypeService;
    private readonly IContentTypeEditingService _contentTypeEditingService;
    private readonly IContentService _contentService;
    private readonly IContentEditingService _contentEditingService;
    private readonly IContentPublishingService _contentPublishingService;
    private readonly IWebHostEnvironment _environment;

    public CreateSiteContent(
        IMigrationContext context,
        ITemplateService templateService,
        IContentTypeService contentTypeService,
        IContentTypeEditingService contentTypeEditingService,
        IContentService contentService,
        IContentEditingService contentEditingService,
        IContentPublishingService contentPublishingService,
        IWebHostEnvironment environment)
        : base(context)
    {
        _templateService = templateService;
        _contentTypeService = contentTypeService;
        _contentTypeEditingService = contentTypeEditingService;
        _contentService = contentService;
        _contentEditingService = contentEditingService;
        _contentPublishingService = contentPublishingService;
        _environment = environment;
    }

    protected override async Task MigrateAsync()
    {
        // The layout first: Umbraco reads `Layout = "layout.cshtml"` in the other templates as their master.
        await EnsureTemplateAsync("Layout", "layout");
        ITemplate homeTemplate = await EnsureTemplateAsync("Home", "home");
        ITemplate announcementTemplate = await EnsureTemplateAsync("Announcement", "announcement");

        await EnsureContentTypeAsync(new ContentTypeCreateModel
        {
            Key = AnnouncementTypeKey,
            Alias = "announcement",
            Name = "Announcement",
            Icon = "icon-message",
            AllowedTemplateKeys = [announcementTemplate.Key],
            DefaultTemplateKey = announcementTemplate.Key,
            Containers = [new ContentTypePropertyContainerModel { Key = ContainerKey(1), Name = "Content", Type = "Group" }],
            Properties =
            [
                Property(1, "heading", "Heading", Constants.DataTypes.Guids.TextstringGuid, mandatory: true),
                Property(2, "mainText", "Main text", Constants.DataTypes.Guids.TextareaGuid, mandatory: false),
            ],
        });

        await EnsureContentTypeAsync(new ContentTypeCreateModel
        {
            Key = AnnouncementsTypeKey,
            Alias = "announcements",
            Name = "Announcements",
            Icon = "icon-folder",
            AllowedContentTypes = [new ContentTypeSort(AnnouncementTypeKey, 0, "announcement")],
        });

        await EnsureContentTypeAsync(new ContentTypeCreateModel
        {
            Key = HomeTypeKey,
            Alias = "home",
            Name = "Home",
            Icon = "icon-home",
            AllowedAsRoot = true,
            AllowedTemplateKeys = [homeTemplate.Key],
            DefaultTemplateKey = homeTemplate.Key,
            AllowedContentTypes = [new ContentTypeSort(AnnouncementsTypeKey, 0, "announcements")],
        });

        await EnsurePublishedNodeAsync(HomeNodeKey, HomeTypeKey, parentKey: null, "Home", homeTemplate.Key);
        await EnsurePublishedNodeAsync(AnnouncementsNodeKey, AnnouncementsTypeKey, HomeNodeKey, "Announcements", templateKey: null);
    }

    /// <summary>Creates the template from the committed view file, so the file stays the source of truth.</summary>
    private async Task<ITemplate> EnsureTemplateAsync(string name, string alias)
    {
        ITemplate? existing = await _templateService.GetAsync(alias);
        if (existing is not null)
        {
            return existing;
        }

        string viewPath = Path.Combine(_environment.ContentRootPath, "Views", alias + ".cshtml");
        string content = await System.IO.File.ReadAllTextAsync(viewPath);

        var result = await _templateService.CreateAsync(name, alias, content, UserKey);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Could not create template '{alias}': {result.Status}");
        }

        return result.Result;
    }

    private async Task EnsureContentTypeAsync(ContentTypeCreateModel model)
    {
        if (await _contentTypeService.GetAsync(model.Key!.Value) is not null)
        {
            return;
        }

        var result = await _contentTypeEditingService.CreateAsync(model, UserKey);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Could not create document type '{model.Alias}': {result.Status}");
        }
    }

    private async Task EnsurePublishedNodeAsync(Guid key, Guid contentTypeKey, Guid? parentKey, string name, Guid? templateKey)
    {
        if (_contentService.GetById(key) is null)
        {
            var created = await _contentEditingService.CreateAsync(
                new ContentCreateModel
                {
                    Key = key,
                    ContentTypeKey = contentTypeKey,
                    ParentKey = parentKey,
                    TemplateKey = templateKey,
                    Variants = [new VariantModel { Name = name }],
                },
                UserKey);

            if (!created.Success)
            {
                throw new InvalidOperationException($"Could not create '{name}': {created.Status}");
            }
        }

        var published = await _contentPublishingService.PublishAsync(
            key, [new CulturePublishScheduleModel { Culture = null }], UserKey);
        if (!published.Success)
        {
            throw new InvalidOperationException($"Could not publish '{name}': {published.Status}");
        }
    }

    private static Guid ContainerKey(int n) => new($"5E9A3B71-0C2D-4F8E-A6B1-3D7C9E2F1C{n:D2}");

    private static ContentTypePropertyTypeModel Property(int n, string alias, string name, Guid dataTypeKey, bool mandatory) => new()
    {
        Key = new Guid($"5E9A3B71-0C2D-4F8E-A6B1-3D7C9E2F1D{n:D2}"),
        ContainerKey = ContainerKey(1),
        SortOrder = n,
        Alias = alias,
        Name = name,
        DataTypeKey = dataTypeKey,
        Validation = new PropertyTypeValidation { Mandatory = mandatory },
    };
}
