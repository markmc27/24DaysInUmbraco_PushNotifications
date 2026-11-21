using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace NotificationsRevisit.Announcements.Subscriptions;

/// <summary>Settings for the subscribe endpoint's rate limit, bound from <c>Announcements:Subscribe</c>.</summary>
public class SubscribeRateLimitOptions
{
    public const string SectionName = "Announcements:Subscribe";

    /// <summary>Calls allowed per client IP address in each window.</summary>
    public int PermitLimit { get; set; } = 5;

    public int WindowMinutes { get; set; } = 60;
}

/// <summary>
/// A fixed window rate limit per client IP for the subscribe endpoint. ASP.NET Core's built-in
/// limiter replaces the 2018 hand-written ThrottleAttribute.
/// </summary>
public static class SubscribeRateLimiting
{
    public const string PolicyName = "Announcements.Subscribe";

    public static IUmbracoBuilder AddSubscribeRateLimiting(this IUmbracoBuilder builder)
    {
        var settings = new SubscribeRateLimitOptions();
        builder.Config.GetSection(SubscribeRateLimitOptions.SectionName).Bind(settings);

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(PolicyName, context => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.PermitLimit,
                    Window = TimeSpan.FromMinutes(settings.WindowMinutes),
                    QueueLimit = 0,
                }));
        });

        // Add the middleware through Umbraco's pipeline, after routing so endpoint policies apply.
        // Program.cs stays untouched.
        builder.Services.Configure<UmbracoPipelineOptions>(options =>
            options.AddFilter(new UmbracoPipelineFilter("Announcements.RateLimiting")
            {
                PostRouting = app => app.UseRateLimiter(),
            }));

        return builder;
    }
}
