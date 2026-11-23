using System.Text.Json;
using System.Text.Json.Serialization;
using Umbraco.Automate.Core.Automations;
using Umbraco.Automate.Core.Automations.Transfer;
using Umbraco.Automate.Core.Workspaces;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace NotificationsRevisit.Web.Automations;

/// <summary>
/// Installs the "Announce new pages" automation from <c>Automations/announce-new-pages.json</c> once,
/// so a fresh clone needs no backoffice set-up (design D4). Automate's workspace needs an API user as
/// its service account, so that is created too.
/// </summary>
/// <remarks>
/// Runs at application start rather than as a package migration step: on a fresh database Umbraco
/// runs package migrations before Automate creates its own schema, so a migration cannot import.
/// A key-value marker keeps the migration's run-once behaviour: an automation deleted on purpose
/// is not brought back on the next restart.
/// </remarks>
public class AnnounceAutomationStartedHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    public const string AutomationAlias = "announce-new-pages";
    public const string WorkspaceAlias = "site";
    public const string ServiceAccountEmail = "automate-site@example.com";
    private const string InstalledMarker = "NotificationsRevisit.Site.AnnounceNewPagesInstalled";

    // A fixed key, so the service account is the same user on every machine.
    private static readonly Guid ServiceAccountKey = new("5E9A3B71-0C2D-4F8E-A6B1-3D7C9E2F1C01");

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly IRuntimeState _runtimeState;
    private readonly IKeyValueService _keyValues;
    private readonly IUserService _users;
    private readonly IWorkspaceService _workspaces;
    private readonly IAutomationService _automations;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AnnounceAutomationStartedHandler> _logger;

    public AnnounceAutomationStartedHandler(
        IRuntimeState runtimeState,
        IKeyValueService keyValues,
        IUserService users,
        IWorkspaceService workspaces,
        IAutomationService automations,
        IWebHostEnvironment environment,
        ILogger<AnnounceAutomationStartedHandler> logger)
    {
        _runtimeState = runtimeState;
        _keyValues = keyValues;
        _users = users;
        _workspaces = workspaces;
        _automations = automations;
        _environment = environment;
        _logger = logger;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        if (_runtimeState.Level != RuntimeLevel.Run || _keyValues.GetValue(InstalledMarker) is not null)
        {
            return;
        }

        IUser serviceAccount = await EnsureServiceAccountAsync();
        Workspace workspace = await EnsureWorkspaceAsync(serviceAccount, cancellationToken);

        Automation? automation = await _automations.GetAutomationByAliasAsync(AutomationAlias, cancellationToken);
        if (automation is null)
        {
            await ImportAndPublishAsync(workspace, cancellationToken);
        }

        _keyValues.SetValue(InstalledMarker, DateTime.UtcNow.ToString("O"));
    }

    private async Task<IUser> EnsureServiceAccountAsync()
    {
        IUser? existing = await _users.GetAsync(ServiceAccountKey) ?? _users.GetByEmail(ServiceAccountEmail);
        if (existing is not null)
        {
            return existing;
        }

        var model = new UserCreateModel
        {
            Id = ServiceAccountKey,
            Email = ServiceAccountEmail,
            UserName = ServiceAccountEmail,
            Name = "Automate Site",
            Kind = UserKind.Api,
            // Writers can read content, which is all Get Content needs.
            UserGroupKeys = new HashSet<Guid> { Constants.Security.WriterGroupKey },
        };

        Attempt<UserCreationResult, UserOperationStatus> created =
            await _users.CreateAsync(Constants.Security.SuperUserKey, model, approveUser: true);
        if (!created.Success || created.Result.CreatedUser is null)
        {
            throw new InvalidOperationException($"Could not create the Automate service account: {created.Status}.");
        }

        _logger.LogInformation("Created API user {Email} as the Automate service account", ServiceAccountEmail);
        return created.Result.CreatedUser;
    }

    private async Task<Workspace> EnsureWorkspaceAsync(IUser serviceAccount, CancellationToken cancellationToken)
    {
        Workspace? existing = await _workspaces.GetWorkspaceByAliasAsync(WorkspaceAlias, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        Workspace created = await _workspaces.CreateWorkspaceAsync(
            new Workspace
            {
                Alias = WorkspaceAlias,
                Name = "Site",
                ServiceAccountKey = serviceAccount.Key,
                UserGroups = [Constants.Security.AdminGroupKey],
            },
            cancellationToken: cancellationToken);

        _logger.LogInformation("Created Automate workspace {Alias}", WorkspaceAlias);
        return created;
    }

    private async Task ImportAndPublishAsync(Workspace workspace, CancellationToken cancellationToken)
    {
        string path = Path.Combine(_environment.ContentRootPath, "Automations", $"{AutomationAlias}.json");
        AutomationExportModel model = JsonSerializer.Deserialize<AutomationExportModel>(
            await System.IO.File.ReadAllTextAsync(path, cancellationToken), Json)
            ?? throw new InvalidOperationException($"{path} is empty.");

        AutomationImportResult imported = await _automations.ImportAutomationAsync(
            model, workspace.Id, cancellationToken: cancellationToken);
        if (!imported.Success || imported.AutomationId is not { } id)
        {
            throw new InvalidOperationException(
                $"Importing {AutomationAlias} failed: {string.Join(" ", imported.Errors)}");
        }

        // Imports arrive as drafts, and drafts do not respond to events.
        await _automations.PublishAutomationAsync(id, cancellationToken: cancellationToken);
        _logger.LogInformation("Imported and published automation {Alias}", AutomationAlias);
    }
}
