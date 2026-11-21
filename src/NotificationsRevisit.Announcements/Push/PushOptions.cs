using Microsoft.Extensions.Options;

namespace NotificationsRevisit.Announcements.Push;

/// <summary>Web push settings, bound from <c>Announcements:Push</c>.</summary>
public class PushOptions
{
    public const string SectionName = "Announcements:Push";

    /// <summary>Notification title shown by the browser, for example the site or club name.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Absolute path or URL of the notification icon.</summary>
    public string IconUrl { get; set; } = string.Empty;

    /// <summary>How many deliveries run at once.</summary>
    public int MaxConcurrency { get; set; } = 4;

    public VapidOptions Vapid { get; set; } = new();
}

/// <summary>VAPID details. Keep the private key in user secrets or a secret store, never in Git.</summary>
public class VapidOptions
{
    /// <summary>A <c>mailto:</c> or <c>https:</c> contact for the push service.</summary>
    public string Subject { get; set; } = string.Empty;

    public string PublicKey { get; set; } = string.Empty;

    public string PrivateKey { get; set; } = string.Empty;
}

/// <summary>Fails startup with a message naming each missing setting.</summary>
public class PushOptionsValidator : IValidateOptions<PushOptions>
{
    public ValidateOptionsResult Validate(string? name, PushOptions options)
    {
        var failures = new List<string>();

        void Require(string value, string key, string hint = "")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                failures.Add($"{PushOptions.SectionName}:{key} is missing.{hint}");
            }
        }

        Require(options.Title, nameof(PushOptions.Title));
        Require(options.IconUrl, nameof(PushOptions.IconUrl));
        Require(options.Vapid.Subject, "Vapid:Subject");
        Require(options.Vapid.PublicKey, "Vapid:PublicKey", " Generate keys with: npx web-push generate-vapid-keys");
        Require(options.Vapid.PrivateKey, "Vapid:PrivateKey", " Store it in user secrets, never in appsettings.");

        if (options.MaxConcurrency < 1)
        {
            failures.Add($"{PushOptions.SectionName}:{nameof(PushOptions.MaxConcurrency)} must be 1 or more.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
