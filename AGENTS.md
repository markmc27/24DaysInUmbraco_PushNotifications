# AGENTS.md

Context for AI coding agents (Claude Code, Copilot, Cursor and others) working in this repo.
Humans: see [docs/build-log.md](docs/build-log.md) for the why behind every choice.

## What this is

A proof of concept for a 24 Days in Umbraco article. It compares classic Umbraco
notification handlers (`INotificationHandler<T>`) with Umbraco Automate.
Keep it small. Prefer the simplest thing that demonstrates the point.

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
  umbraco/models/               generated ModelsBuilder classes (SourceCodeAuto, do not hand-edit)
docs/build-log.md               decision log for the article
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
  features exist in 17.6 before relying on them.
- A machine-wide `ConnectionStrings__umbracoDbDSN` environment variable overrides `appsettings`.
  If the site tries to reach SQL Server instead of SQLite, that is why.

## When you change something notable

Add a short entry to `docs/build-log.md`: what changed, why, and what you rejected.
