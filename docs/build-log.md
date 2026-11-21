# Build log: setting up the PoC

A running record of what we did, in order, and why. Written to feed the 24 Days in Umbraco article,
so it records the reasoning and the dead ends, not just the commands.

Date: 21 November 2026.

## Summary

- Vanilla Umbraco 17.8.2 (LTS) on .NET 10, created with the official `dotnet new umbraco` template.
- Umbraco Automate 17.6.0 added as a single NuGet package. No `Program.cs` changes needed.
- SQLite for local development, with a separate SQLite file for Automate.
- A small set of AI-first files: `AGENTS.md`, `CLAUDE.md`, and this log.
- Verified end to end: the site boots, installs unattended, runs Automate migrations,
  and the backoffice shows the Automation section.

## 1. Choosing the versions

**Question:** which Umbraco 17 build supports Automate?

We checked NuGet rather than trusting docs or memory:

| Package | Versions on NuGet (17 line) | Notes |
|---|---|---|
| `Umbraco.Templates` | up to 17.8.2 | latest 17 patch |
| `Umbraco.Automate` | 17.0.0 to 17.6.0 | then 18.x and 19.0.0-rc1 |

The `Umbraco.Automate.Core` 17.6.0 nuspec depends on `Umbraco.Cms.Core [17.6.0, 17.999.999)`.
So Automate 17.6.0 needs Umbraco 17.6 or later and refuses Umbraco 18.

**Decision:** Umbraco.Cms 17.8.2 plus Umbraco.Automate 17.6.0.

**Why:**
- Umbraco 17 is the current LTS. 18 is short-term support, so it is the wrong base for anything
  readers might copy into a real project.
- 17.8.2 is the newest 17 patch. It also cleared vulnerability warnings that 17.6.0 raised
  (`SixLabors.ImageSharp` 3.1.12 and `System.Security.Cryptography.Pkcs` 10.0.0).
- 17.6.0 is the newest Automate for the 17 line.

**Gotcha worth a line in the article:** the Automate docs at docs.umbraco.com track the newest
release and state "Umbraco CMS 18.0 or later". The GitHub README says 17.x. NuGet is the source of
truth: the package metadata settles it in seconds.

## 2. Creating the project

The machine already had `Umbraco.Templates` 17.6.0 installed. Rather than upgrade a global
template (which affects every other project on the machine), we created from 17.6.0 and bumped the
package version afterwards. Same result, no side effects.

```powershell
dotnet new umbraco -n NotificationsRevisit.Web -o src/NotificationsRevisit.Web `
  --development-database-type SQLite `
  --development-mode IDEDevelopment `
  --friendly-name "PoC Admin" --email "admin@example.com" --password "<generated>" `
  --telemetry-level Minimal
```

Template options and why:

| Option | Choice | Why |
|---|---|---|
| Template | `umbraco` (not a scaffold) | The brief: vanilla Umbraco, so readers can reproduce it. |
| Database | SQLite | Zero setup. Anyone can clone and run it. |
| Development mode | `IDEDevelopment` | Sets ModelsBuilder to `SourceCodeAuto` in Development. See "AI-first" below. |
| Unattended install | name, email, password | The site installs itself on first run. No installer clicks, so an agent can boot it too. |
| Package management | Central (default) | One place for versions: `Directory.Packages.props`. |
| Telemetry | Minimal | A throwaway PoC does not need detailed telemetry. |
| Delivery API, Docker, starter kit | off | Not needed to show notifications. Keep the surface small. |

## 3. Repo layout

```
NotificationsRevisit.slnx
Directory.Packages.props
global.json
AGENTS.md / CLAUDE.md
docs/build-log.md
src/NotificationsRevisit.Web/
```

- **`src/` folder plus a solution at the root.** Leaves room for a second project (a test
  project or a class library for handlers) without moving things later.
- **`.slnx`** rather than `.sln`. The new XML format is the default direction for .NET 10,
  is tiny and readable, and merges cleanly.
- **`Directory.Packages.props` and `.gitignore` moved to the root.** They apply to the whole repo.
  Moving the `.gitignore` broke its rooted Umbraco paths (`/umbraco/Data/...`), so we rewrote them as
  `src/*/umbraco/...`.
- **`global.json`** pins the .NET 10 SDK (`10.0.102`, roll forward on feature band). The machine
  also has the .NET 9 SDK, and pinning stops surprises for readers with several SDKs installed.

## 4. Adding Umbraco Automate

```xml
<!-- Directory.Packages.props -->
<PackageVersion Include="Umbraco.Automate" Version="17.6.0" />

<!-- NotificationsRevisit.Web.csproj -->
<PackageReference Include="Umbraco.Automate" />
```

That is all the code it needs. Automate registers itself through a composer, so `Program.cs`
is untouched. The single `Umbraco.Automate` package brings in the engine, the backoffice UI,
and both SQLite and SQL Server persistence.

### Where Automate stores its data

Automate keeps run history, an outbox and workflow engine state. The docs offer two options:

1. Its own connection string: `umbracoAutomateDbDSN`.
2. Share the CMS database with `Umbraco:Automate:UseNamedConnectionString`.

**Decision:** a dedicated SQLite file.

```json
"umbracoAutomateDbDSN": "Data Source=|DataDirectory|/Umbraco.Automate.sqlite.db;Cache=Shared;Foreign Keys=True;Pooling=True",
"umbracoAutomateDbDSN_ProviderName": "Microsoft.Data.Sqlite"
```

**Why:** the docs recommend a dedicated database, because Automate adds outbox, run history and
engine traffic. With SQLite that matters more: SQLite allows one writer at a time, so two busy
subsystems sharing one file would contend for the lock. A second file costs nothing.

The docs give only a SQL Server example. The SQLite string above is our own, modelled on the one
Umbraco generates for the CMS. It works on 17.6.0 (verified in step 6).

## 5. Secrets

The template writes the unattended admin password into `appsettings.Development.json` in plain text,
which would then be committed. We moved it to .NET user secrets:

```powershell
dotnet user-secrets init --project src/NotificationsRevisit.Web
dotnet user-secrets set "Umbraco:CMS:Unattended:UnattendedUserPassword" "<password>" --project src/NotificationsRevisit.Web
```

Umbraco reads it through normal ASP.NET Core configuration, so nothing else changes.
Same plan for Automate: its `Umbraco:Automate:Secrets:*` values should go in user secrets, never in
`appsettings.json`.

This is also an AI-first point: secrets not in the repo means secrets not in an agent's context
window.

## 6. First run and verification

First run failed. The trace log showed:

> Connection string keyword 'server' is not supported.

The cause: a machine-wide environment variable, `ConnectionStrings__umbracoDbDSN`, left over from
another project and pointing at SQL Server. ASP.NET Core configuration ranks environment variables
above `appsettings.json`, so it silently overrode the SQLite string.

We did not change the machine's environment. Instead, the local launch config clears that variable
for this process only. Readers will not hit this unless they have the same variable set, but it is a
good reminder that configuration precedence bites, and the log tells you exactly what happened.

Second run, from the logs:

- Unattended install completed, `Umbraco.sqlite.db` created.
- `Running {Count} pending Automate migrations` then `Automate migrations completed successfully`.
- `Umbraco.Automate.sqlite.db` created.
- `Starting WorkflowCore host`, outbox dispatcher started.

Backoffice check: logged in as the unattended admin. The **Automation** section sat in the top bar
with no user group changes needed (the docs mention granting it if missing). It opens on
"Welcome to Umbraco Automate" with Workspaces and Connections.

**Known warning:** the build reports `OpenTelemetry.Api` 1.1.0 (moderate advisory) as a transitive
dependency, which arrives with Automate's packages. Fine for a local PoC. For production, check
whether a newer Automate patch lifts it, or pin a fixed version in `Directory.Packages.props`.

## 7. AI-first setup (kept deliberately light)

The goal: an AI agent opening this repo cold should build it, run it and follow our conventions
without guessing. For a PoC that needs three things, not a framework.

### `AGENTS.md`, with `CLAUDE.md` importing it

- `AGENTS.md` is the cross-tool convention: Copilot, Cursor, Codex and others read it.
- Claude Code reads `CLAUDE.md`, which contains one line, `@AGENTS.md`, so there is one source of truth.
- Contents: what the project is, versions, layout, build and run commands, conventions, and gotchas.
- The gotchas section carries the two things that cost us time: docs that show newer versions
  than we run, and the environment variable override. Writing down a mistake once stops every
  future agent session repeating it.

### ModelsBuilder in `SourceCodeAuto`

`IDEDevelopment` mode generates strongly typed content models as `.cs` files under
`umbraco/models/`, instead of in memory. That matters for agents: they can read the actual
document types and property aliases from disk rather than guessing them. It also means a
compile error, not a runtime error, when a handler refers to a property that does not exist.
Production uses `Nothing`, the template default, so nothing is generated on live servers.

### Unattended install and a local launch config

An agent can start the site with one command, read the logs, and check the backoffice in a
browser. No installer wizard to click through. `.claude/launch.json` describes how to start it,
so Claude Code's preview browser can boot and inspect the site directly.

### Suggested, not yet added: agent permissions

A project-level `.claude/settings.json` can pre-approve safe commands and block reading secrets.
Claude Code's own safety checks stopped it writing this file itself, rightly: changing an agent's
permissions should be a human decision. Suggested contents, if you want them:

```json
{
  "permissions": {
    "allow": ["Bash(dotnet build:*)", "Bash(dotnet restore:*)", "Bash(dotnet run:*)"],
    "deny": ["Read(**/secrets.json)", "Read(**/*.sqlite.db)"]
  }
}
```

### Rejected, for now

| Idea | Why not (yet) |
|---|---|
| Umbraco MCP server for the backoffice | Useful later, to let an agent create document types. Not needed to boot the PoC. A candidate next step. |
| Custom agent skills, hooks, subagents | Overkill for a single-project PoC. |
| Spec frameworks (OpenSpec and similar) | Adds ceremony to a small demo. Worth it on a real project. |
| `.editorconfig` and analyser rules | Template defaults are fine for a PoC. |

## Next steps

1. Create a simple document type (for example "Article") so ModelsBuilder has something to generate.
2. Build the "classic" version: an `INotificationAsyncHandler<ContentPublishedNotification>` in a composer.
3. Build the same behaviour as an Automate automation, and compare.
4. Decide whether to add the Umbraco MCP server so an agent can set up content in the backoffice.
