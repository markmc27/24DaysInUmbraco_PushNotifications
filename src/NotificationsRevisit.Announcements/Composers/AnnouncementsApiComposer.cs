using Asp.Versioning;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Umbraco.Cms.Api.Common.OpenApi;
using Umbraco.Cms.Api.Management.OpenApi;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace NotificationsRevisit.Announcements.Composers;

/// <summary>
/// The extension's own Swagger document (from the umbraco-extension template). The TypeScript
/// client for the dashboard is generated from it.
/// </summary>
public class AnnouncementsApiComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IOperationIdHandler, AnnouncementsOperationIdHandler>();

        builder.Services.Configure<SwaggerGenOptions>(options =>
        {
            options.SwaggerDoc(Constants.ApiName, new OpenApiInfo
            {
                Title = "Announcements Backoffice API",
                Version = "1.0",
            });

            // Backoffice authentication for this document's operations
            options.OperationFilter<AnnouncementsOperationSecurityFilter>();
        });
    }

    public class AnnouncementsOperationSecurityFilter : BackOfficeSecurityRequirementsOperationFilterBase
    {
        protected override string ApiName => Constants.ApiName;
    }

    /// <summary>Operation IDs are the action names, so the generated client has short method names.</summary>
    public class AnnouncementsOperationIdHandler : OperationIdHandler
    {
        public AnnouncementsOperationIdHandler(IOptions<ApiVersioningOptions> apiVersioningOptions)
            : base(apiVersioningOptions)
        {
        }

        protected override bool CanHandle(ApiDescription apiDescription, ControllerActionDescriptor controllerActionDescriptor)
            => controllerActionDescriptor.ControllerTypeInfo.Namespace?.StartsWith(
                "NotificationsRevisit.Announcements.Controllers", StringComparison.InvariantCultureIgnoreCase) is true;

        public override string Handle(ApiDescription apiDescription) => $"{apiDescription.ActionDescriptor.RouteValues["action"]}";
    }
}
