using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Web.Common.Authorization;

namespace NotificationsRevisit.Announcements.Authorization;

/// <summary>
/// Who may use announcements: Content section access, and not in the Translators group.
/// The server enforces what the dashboard condition hides. 2018 only hid the tab
/// (<c>&lt;deny&gt;translator&lt;/deny&gt;</c> in Dashboard.config) and left the API open.
/// </summary>
public static class AnnouncementsAuthorization
{
    public const string PolicyName = "Announcements.Access";

    public static IUmbracoBuilder AddAnnouncementsAuthorization(this IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IAuthorizationHandler, NotInTranslatorsGroupHandler>();

        // Build on Umbraco's own Content section policy (same schemes and requirements), then add ours.
        // PostConfigure runs after Umbraco has registered its policies.
        builder.Services.PostConfigure<AuthorizationOptions>(options =>
        {
            AuthorizationPolicy contentAccess = options.GetPolicy(AuthorizationPolicies.SectionAccessContent)
                ?? throw new InvalidOperationException($"Umbraco policy '{AuthorizationPolicies.SectionAccessContent}' is not registered.");

            options.AddPolicy(PolicyName, new AuthorizationPolicyBuilder(contentAccess)
                .AddRequirements(new NotInTranslatorsGroupRequirement())
                .Build());
        });

        return builder;
    }
}

public class NotInTranslatorsGroupRequirement : IAuthorizationRequirement
{
}

/// <summary>Fails when the current backoffice user belongs to the Translators group.</summary>
public class NotInTranslatorsGroupHandler : AuthorizationHandler<NotInTranslatorsGroupRequirement>
{
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;

    public NotInTranslatorsGroupHandler(IBackOfficeSecurityAccessor backOfficeSecurityAccessor)
        => _backOfficeSecurityAccessor = backOfficeSecurityAccessor;

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, NotInTranslatorsGroupRequirement requirement)
    {
        IUser? user = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;

        if (user is not null && user.Groups.All(g => g.Key != Umbraco.Cms.Core.Constants.Security.TranslatorGroupKey))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
