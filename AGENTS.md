# AGENTS.md

Context for AI coding agents (Claude Code, Copilot, Cursor and others) working in this repo.
Humans: see [docs/build-log.md](docs/build-log.md) (approach 1) and
[docs/build-log-approach-2.md](docs/build-log-approach-2.md) (approach 2) for the why behind every choice.

## What this is

A proof of concept for a 24 Days in Umbraco article. It compares alternative ways to let a swimming
club tell parents about announcements by web push. Approach 1, a custom backoffice dashboard, is
at tag `approach-1-dashboard`. The current code is approach 2: an Umbraco Automate automation sends
the push when an editor publishes an Announcement page.
Keep it small. Prefer the simplest thing that demonstrates the point.

## How approach 2 fits together

- The automation **Announce new pages** (alias `announce-new-pages`, workspace `site`) is
  Content Published (Announcement only), then Get Content, then our **Send push notification**
  action. Its definition is committed at `src/NotificationsRevisit.Web/Automations/announce-new-pages.json`.
- `AnnounceAutomationStartedHandler` installs it once on a fresh database: an API user as the
  workspace's service account, the workspace, then import and publish. It runs at application
  start, not as a package migration, because package migrations run before Automate creates its
  own schema.
- `SendPushNotificationAction` (extension project, `Automation/`) claims the content key in the
  `announcementsPushedContent` table before sending, so each page is pushed once.
- If you change the automation in the backoffice, export it (actions menu, Export) over the JSON
  file and commit it.

## Stack

- Umbraco CMS 17.8.2 (LTS) on .NET 10. Vanilla `dotnet new umbraco`, no scaffold.
- Umbraco Automate 17.6.0 (`Umbraco.Automate`).
- SQLite for local development. Two files in `src/NotificationsRevisit.Web/umbraco/Data/`:
  `Umbraco.sqlite.db` (CMS) and `Umbraco.Automate.sqlite.db` (Automate run history and outbox).
- Central package management: versions live in `Directory.Packages.props` only.

## Layout

```
NotificationsRevisit.slnx
Directory.Packages.props        package versions (edit here, not in .csproj)
global.json                     pins the .NET 10 SDK
src/NotificationsRevisit.Web/   the Umbraco site
  Program.cs                    Umbraco builder; register things via composers instead
  Automations/                  committed automation JSON and the handler that installs it
  Migrations/                   site content model (document types, templates, fixed nodes)
  Client/                       site scripts (opt-in, service worker), TypeScript built by Vite
  umbraco/models/               generated ModelsBuilder classes (SourceCodeAuto, do not hand-edit)
src/NotificationsRevisit.Announcements/   extension library: push stack, subscribe endpoint,
                                          the Automate action, its tables and migrations
tests/NotificationsRevisit.Announcements.Tests/   xUnit tests (repository SQL, push, action)
docs/build-log.md               decision log, approach 1
docs/build-log-approach-2.md    decision log, approach 2
```

## Commands

```
dotnet build NotificationsRevisit.slnx
dotnet run --project src/NotificationsRevisit.Web --launch-profile Umbraco.Web.UI
```

Site: https://localhost:44371. Backoffice: https://localhost:44371/umbraco.
The first run installs Umbraco unattended and runs Automate migrations.
The admin password is in user secrets (`dotnet user-secrets list --project src/NotificationsRevisit.Web`).

## Conventions

- Register services and notification handlers in an `IComposer`, not in `Program.cs`.
- One notification handler per class, named `<Thing><Event>Handler`, e.g. `ArticlePublishedHandler`.
- Use the async notification handlers (`INotificationAsyncHandler<T>`) for anything that does I/O.
- Keep secrets out of `appsettings*.json`. Use user secrets locally.
  Automate secrets go under `Umbraco:Automate:Secrets` (also via user secrets).
- Do not edit generated files in `umbraco/models/`. Change the document type, and ModelsBuilder regenerates them.
- British English in docs and UI text.

## Gotchas

- Umbraco 17 docs live under the "17.latest" path at docs.umbraco.com. Search results often show
  newer versions (18, 19). Check the version before copying an API.
- Automate docs at docs.umbraco.com/umbraco-automate track the newest release. Check that
  features exist in 17.6 before relying on them. The installed assemblies are the reference:
  `ilspycmd` on `~/.nuget/packages/umbraco.automate.core/17.6.0` shows the real API.
- Automate workspaces need an API user as service account; actions that read content
  authorise as that user.
- Automate runs steps on its scheduler (about every 10 seconds), outside any HTTP request.
- A machine-wide `ConnectionStrings__umbracoDbDSN` environment variable overrides `appsettings`.
  If the site tries to reach SQL Server instead of SQLite, that is why.

## When you change something notable

Add a short entry to the build log for the current approach (`docs/build-log-approach-2.md`):
what changed, why, and what you rejected.
