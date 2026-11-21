using Microsoft.Extensions.Options;

namespace NotificationsRevisit.Announcements.Announcements;

/// <summary>
/// The site's content model, bound from <c>Announcements:ContentModel</c>. The site owns its document
/// types; the extension only needs to know their aliases.
/// </summary>
public class ContentModelOptions
{
    public const string SectionName = "Announcements:ContentModel";

    /// <summary>Document type of each announcement page.</summary>
    public string PageDocTypeAlias { get; set; } = string.Empty;

    /// <summary>Document type of the node announcement pages are created under.</summary>
    public string ParentDocTypeAlias { get; set; } = string.Empty;

    public string HeadingPropertyAlias { get; set; } = string.Empty;

    public string TextPropertyAlias { get; set; } = string.Empty;
}

/// <summary>Fails startup with a message naming each missing alias.</summary>
public class ContentModelOptionsValidator : IValidateOptions<ContentModelOptions>
{
    public ValidateOptionsResult Validate(string? name, ContentModelOptions options)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(options.PageDocTypeAlias)) missing.Add(nameof(options.PageDocTypeAlias));
        if (string.IsNullOrWhiteSpace(options.ParentDocTypeAlias)) missing.Add(nameof(options.ParentDocTypeAlias));
        if (string.IsNullOrWhiteSpace(options.HeadingPropertyAlias)) missing.Add(nameof(options.HeadingPropertyAlias));
        if (string.IsNullOrWhiteSpace(options.TextPropertyAlias)) missing.Add(nameof(options.TextPropertyAlias));

        return missing.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(missing.Select(m => $"{ContentModelOptions.SectionName}:{m} is missing."));
    }
}
