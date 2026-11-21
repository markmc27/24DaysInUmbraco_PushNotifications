# Build log

A running record of what we did, in order, and why. Written to feed the 24 Days in Umbraco article,
so it records the reasoning and the dead ends, not just the commands.

Date: 21 November 2026.

## Phase 1: setting up the PoC

### Summary

- Vanilla Umbraco 17.8.2 (LTS) on .NET 10, created with the official `dotnet new umbraco` template.
- Umbraco Automate 17.6.0 added as a single NuGet package. No `Program.cs` changes needed.
- SQLite for local development, with a separate SQLite file for Automate.
- A small set of AI-first files: `AGENTS.md`, `CLAUDE.md`, and this log.
- Verified end to end: the site boots, installs unattended, runs Automate migrations,
  and the backoffice shows the Automation section.

### 1. Choosing the versions

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

### 2. Creating the project

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

### 3. Repo layout

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

### 4. Adding Umbraco Automate

```xml
<!-- Directory.Packages.props -->
<PackageVersion Include="Umbraco.Automate" Version="17.6.0" />

<!-- NotificationsRevisit.Web.csproj -->
<PackageReference Include="Umbraco.Automate" />
```

That is all the code it needs. Automate registers itself through a composer, so `Program.cs`
is untouched. The single `Umbraco.Automate` package brings in the engine, the backoffice UI,
and both SQLite and SQL Server persistence.

#### Where Automate stores its data

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

### 5. Secrets

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

### 6. First run and verification

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

### 7. AI-first setup (kept deliberately light)

The goal: an AI agent opening this repo cold should build it, run it and follow our conventions
without guessing. For a PoC that needs three things, not a framework.

#### `AGENTS.md`, with `CLAUDE.md` importing it

- `AGENTS.md` is the cross-tool convention: Copilot, Cursor, Codex and others read it.
- Claude Code reads `CLAUDE.md`, which contains one line, `@AGENTS.md`, so there is one source of truth.
- Contents: what the project is, versions, layout, build and run commands, conventions, and gotchas.
- The gotchas section carries the two things that cost us time: docs that show newer versions
  than we run, and the environment variable override. Writing down a mistake once stops every
  future agent session repeating it.

#### ModelsBuilder in `SourceCodeAuto`

`IDEDevelopment` mode generates strongly typed content models as `.cs` files under
`umbraco/models/`, instead of in memory. That matters for agents: they can read the actual
document types and property aliases from disk rather than guessing them. It also means a
compile error, not a runtime error, when a handler refers to a property that does not exist.
Production uses `Nothing`, the template default, so nothing is generated on live servers.

#### Unattended install and a local launch config

An agent can start the site with one command, read the logs, and check the backoffice in a
browser. No installer wizard to click through. `.claude/launch.json` describes how to start it,
so Claude Code's preview browser can boot and inspect the site directly.

#### Suggested, not yet added: agent permissions

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

#### Rejected, for now

| Idea | Why not (yet) |
|---|---|
| Umbraco MCP server for the backoffice | Useful later, to let an agent create document types. Not needed to boot the PoC. A candidate next step. |
| Custom agent skills, hooks, subagents | Overkill for a single-project PoC. |
| Spec frameworks (OpenSpec and similar) | Adds ceremony to a small demo. Worth it on a real project. |
| `.editorconfig` and analyser rules | Template defaults are fine for a PoC. |

### Next steps

1. Create a simple document type (for example "Article") so ModelsBuilder has something to generate.
2. Build the "classic" version: an `INotificationAsyncHandler<ContentPublishedNotification>` in a composer.
3. Build the same behaviour as an Automate automation, and compare.
4. Decide whether to add the Umbraco MCP server so an agent can set up content in the backoffice.

---

## Phase 2: gathering requirements from the 2018 article

Date: 21 November 2026.

### The goal

Recreate, and improve, the custom backoffice extension from the 2018 24 Days article
[Push notifications and Umbraco](https://archive.24days.in/umbraco-cms/2018/push-notifications-and-umbraco/)
on modern Umbraco, then compare what changed.

### Approach (decided before starting the research)

1. **Write the approach down first.** This section exists before any research, so the article
   can show the plan as it was, not as it looks in hindsight.
2. **Read the original article in full**, as primary source. Extract what it actually built:
   features, user journeys, the moving parts, and the Umbraco APIs it used. Separate what the
   article states from what we infer.
3. **Turn that into a requirements baseline**: "the 2018 version does X". This is the parity list.
4. **Map each 2018 concept to its Umbraco 17 equivalent** (AngularJS backoffice to Lit web
   components and the extension manifest, `ContentService` events to notifications, and so on).
   That mapping is the spine of the "what changed" comparison.
5. **Identify improvements**, kept separate from parity, so the article can say clearly what is
   like for like and what is new.
6. **Use OpenSpec explore mode** for this thinking. Explore mode is for discussion and research
   only: it writes no code. Requirements stay in the conversation until we agree to capture
   them as an OpenSpec change (proposal, specs, design, tasks).

**Why OpenSpec:** it gives the AI agent and the human a shared, versioned statement of what we
are building before anyone writes code. For an article about how building for Umbraco has
changed, "spec first, then let the agent build" is itself part of the story.

**Why explore before propose:** the original article is eight years old. We need to understand
it, and the gap to modern Umbraco, before committing to a scope. Proposing too early would bake
in guesses.

### Research method

- **Primary sources only:** the article itself, plus the public repo it links to
  (`markmc27/UmbracoPushNotifications`, MIT). The article shows simplified code; the repo is what
  actually ran, so where they differ we treat the repo as the baseline and note the difference.
- A subagent did the first pass over the article and part of the repo. It flagged files it had not
  read (dashboard, data model, client JS), so we read those directly rather than infer them.
  Lesson for the article: an AI summary that lists its own gaps is far more useful than one that
  sounds complete.
- Current state checked live on NuGet: the `WebPush` library used in 2018 is still maintained
  (1.0.13, April 2026; 1.0.11 in 2018).

### What the 2018 version actually is

- **Umbraco 7.12.3** on .NET Framework 4.6.1/4.7.2. The article never states the version; the
  repo's `packages.config` does.
- **Problem:** a swimming club needed to tell parents about last-minute session cancellations.
- **Backoffice:** an "Announcements" tab on the **Content** dashboard, registered in
  `config/Dashboard.config`, denied to translators. AngularJS controller, resource and two views.
  - Shows a subscriber count and a "Past Announcements" table (title, sent date, user, link to
    the page in the backoffice).
  - "Add New Announcement" opens a right-hand overlay: short name, heading, main text, and three
    toggles: **Send Email?**, **Send Push Notification?** (with a "What is a push notification?"
    tooltip) and **Publish to Facebook?**
- **On send** (`CommunicationController.NewAnnouncement`, an `UmbracoAuthorizedJsonController`):
  1. Creates and publishes a `newsPage` under the `announcementsFolder` node, named
     `yyyyMMdd-{shortName}`, hidden from search and the sitemap.
  2. Sends a push to every subscriber, linking to that page.
  3. Appends an entry to `App_Data/CommunicationHistory/Log.json`.
- **Channels, honestly:** only Push is implemented on the server. Email has a toggle but no code.
  Facebook posts from the browser via the Facebook JS SDK. The article does not mention the page
  creation or the Facebook toggle.
- **Site visitor:** an opt-in button (never prompt on page load). Client code registers `/sw.js`,
  subscribes with the VAPID public key, and POSTs endpoint, p256dh and auth keys plus an
  anti-forgery token to `/umbraco/api/push/add` (an `UmbracoApiController`, throttled per IP to
  one call an hour).
- **Data:** a `PushSubscriptions` table (Id, PushEndpoint, PushP256DH, PushAuth, CreatedOn),
  created by an `ApplicationEventHandler` at startup with PetaPoco.
- **Sending:** the `WebPush` library, one subscriber at a time. Payload `{ title, message, url }`,
  with a hard-coded title. Subscriptions returning 404 or 410 are deleted; other errors are swallowed.
- **Service worker:** shows the notification (fixed tag, so a new one replaces the old) and on
  click focuses an open tab with that URL or opens a new one.

### Problems in the 2018 code (our reading, not the article's claims)

- **Secrets committed to a public repo:** the VAPID private key (`vapid.json`) and a Facebook
  page access token in the dashboard controller JS. Both should be treated as compromised.
- The send runs inside the HTTP request, sequentially, with no logging of failures.
- If page creation fails, the code goes on to read node `-1` and will most likely throw.
- History lives in a JSON file, so it has no per-recipient results and does not survive a
  load-balanced or cloud setup.
- The author's own open question: "should this be a package?". He had not built one.

### Old to new: the concept map

| 2018 (Umbraco 7) | Today (Umbraco 17) |
|---|---|
| `Dashboard.config` entry | `umbraco-package.json` manifest, `dashboard` extension with a section condition |
| AngularJS controller and views | Lit web component in TypeScript, built with Vite, UUI components |
| `umb-overlay position="right"` | Sidebar modal through the modal manager |
| `umbRequestHelper` resource with `$http` | Generated TypeScript client from the controller's OpenAPI spec |
| `UmbracoAuthorizedJsonController` | Management API controller with backoffice authorisation |
| `UmbracoApiController` (public) | Plain ASP.NET Core controller |
| `ApplicationEventHandler` creates the table | Migration plan run at startup (or a custom EF Core context) |
| PetaPoco, `ApplicationContext.Current` | NPoco via scopes, or EF Core; dependency injection throughout |
| `System.Web` anti-forgery, custom `ThrottleAttribute` | ASP.NET Core antiforgery and built-in rate limiting |
| `appSettings` keys, `vapid.json` in Git | Options pattern, user secrets locally, Key Vault in Azure |
| JSON log file in `App_Data` | Database table |
| `ContentService` events | Notification handlers (`ContentPublishedNotification`), or Umbraco Automate |
| Package: "not learnt how yet" | `dotnet new umbraco-extension` gives a Razor class library ready to pack |
| `sw.js`, Push API, VAPID | **Largely unchanged.** Web standards held; the browser landscape changed |

That last row is a story in itself: the web platform code from 2018 barely needs touching,
while almost everything on the Umbraco side has been replaced.

### Browser landscape then and now (to verify before publishing)

- 2018: Chrome, Firefox, Opera, Android; Edge behind a setting; no Safari or iOS.
- Now: all major desktop browsers support standard Web Push. iOS and iPadOS support it from 16.4,
  but only for sites added to the home screen, which needs a web app manifest. [TK: confirm
  current Safari and iOS behaviour, including Declarative Web Push.]

### Decision: scope of the first version

**Decided (21 November 2026):** rebuild the 2018 extension as it was, inside this app, without
Umbraco Automate. Automate comes later.

- Stack: TypeScript and Lit for the dashboard, modern Umbraco extension points, and a .NET push service.
- Platform: this repo, so Umbraco 17.8.2 LTS. (We read "the latest Umbraco" as the current LTS,
  for the reasons in phase 1.)

**Why parity first:** the article's point here is how building a backoffice extension has
changed, and what spec-driven, AI-assisted development looks like. A like-for-like rebuild keeps
the comparison honest: same feature, two eras. Adding Automate now would mix two stories.

**Article angle for this part:** show the plan (the OpenSpec change), a few architectural
decisions, and how the agent implemented and validated the feature.

### Decision: the parity boundary

**Decided (21 November 2026), as proposed:**

- **Keep:** the Content dashboard (subscriber count, history table, "Add" sidebar); the short
  name, heading and text fields and the push tooltip; creating and publishing an announcement
  page that the push links to; the push channel; a minimal site opt-in with service worker and
  click-through; hiding the dashboard from translators.
- **Drop, and say so in the article:** the Email toggle (it never had an implementation) and the
  Facebook toggle (a browser-side SDK call with a hard-coded token, against an API that has
  since changed).
- **Modernised by default:** secrets in user secrets, history in a database table, logged
  errors, and handling a failed page publish. These are not new features; they are simply how
  you would build it today.
- **Deferred to an improvements pass:** background sending, unsubscribe, iOS web app manifest,
  packaging, and sending on publish.

**Why:** parity means the behaviour editors and visitors saw, not every toggle on screen.
Rebuilding switches that did nothing would make the comparison less honest, not more.

### Decision: a separate Razor class library

**Decided (21 November 2026):** the extension lives in its own RCL, created with
`dotnet new umbraco-extension --include-example --site-domain https://localhost:44371`.
The web project references it. The example code is scaffolding to learn from, removed once the
real feature replaces it. Umbraco packages are aligned to 17.8.2 through central package management.

**Why:**
- It is Umbraco's official starting point for an extension: Vite, Lit, TypeScript, a sample
  Management API controller and the OpenAPI client generator come pre-wired. That is exactly
  this section's reader takeaway.
- It mirrors the 2018 split (a `Core` library plus the website), so the comparison stays like for like.
- The deferred question "should it be a package?" becomes one `dotnet pack` away.
- For the agent, starting from Umbraco's own conventions means less invented structure and code
  that is easier for a human to review.

**Rejected:** putting everything in the web project. Fewer projects, but we would hand-roll the
front-end build and client generation, and extract it all later to package it.

### Decision: NPoco with a migration plan

**Decided (21 November 2026):** the subscriptions and send history tables use NPoco POCOs and
an Umbraco package migration plan, run at startup.

**Why:**
- NPoco is PetaPoco's descendant, so the code reads as a direct "then and now" with 2018.
- The real change is the approach: 2018 checked "does the table exist? if not, create it".
  Today a versioned migration plan records each step, so schema changes later are repeatable.
- One migration runs on both SQLite (local) and SQL Server (Azure). EF Core would need a
  migration set per provider, which is work about EF, not about Umbraco.

**Rejected:** EF Core. Familiar and supported, but heavier than two small tables need.

### Decision: the site owns the content model, the RCL is configured

**Decided (21 November 2026):**

- The RCL reads the announcement doc type alias, parent doc type alias, and heading and text
  property aliases from configuration (options pattern, validated at startup). Nothing is
  hard-coded.
- The web project has its own small migration that creates a minimal "Announcements" folder,
  an "Announcement" page doc type and a template, so the site runs end to end from a fresh clone.

**Why:** 2018 hard-coded `newsPage` and `announcementsFolder`, which tied the feature to one
site. Configuration keeps the RCL usable on any site (and package-ready), while the site
migration keeps the demo reproducible for readers and for the agent's end-to-end testing.
The boundary is clean: the feature lives in the RCL; the content model belongs to the site.

**Rejected:** the RCL creating its own doc types. It works out of the box, but forces its
content model on every site that installs it.

### Capturing the plan as an OpenSpec change

**Done (21 November 2026):**

- `openspec init --tools none`: creates `openspec/` with the default `spec-driven` schema. We
  chose `--tools none` because the OpenSpec skills were already installed for the machine;
  readers who want them in the repo can rerun init with `--tools claude` (or their tool).
- `openspec new change rebuild-announcements-dashboard`, then the four artifacts in dependency
  order: proposal, specs, design, tasks. `openspec validate --strict` passes.

**What is where** (`openspec/changes/rebuild-announcements-dashboard/`):

| Artifact | Holds |
|---|---|
| `proposal.md` | Why, what changes, what is out of scope, three capabilities |
| `specs/push-subscriptions` | Visitor opt-in, storage, antiforgery, rate limiting, HTTPS |
| `specs/push-delivery` | Sending, payload, expired subscriptions, VAPID secrets, notification display and click |
| `specs/announcements` | Dashboard, translator restriction, compose, publish, history, configuration, accessibility |
| `design.md` | Ten decisions (D1 to D10), each with its 2018 counterpart and rejected alternative |
| `tasks.md` | 26 tasks in eight groups; each says how it is verified |

**Choices made while writing, worth a mention in the article:**

- **Specs describe behaviour, not code.** No class names or libraries in the specs; those live in
  the design. That keeps the spec valid even if the design changes.
- **Three capabilities, not one.** Subscriptions, delivery and announcements change for different
  reasons (the deferred improvements touch them separately), so each owns its requirements.
- **Small fixes surfaced by writing scenarios**, all within the agreed "modernised by default"
  scope: no duplicate subscriptions per endpoint (2018 inserted a new row each time), the server
  enforces the translator restriction (2018 only hid the tab), and history records delivery counts.
- **Verified against the installed packages, not memory:** the 17.8.2 backoffice assets confirm an
  `Umb.Condition.CurrentUser.GroupId` condition with a `noneOf` option, which maps directly onto
  2018's `<deny>translator</deny>`.
- **Verification lives in every task**, not in a final testing phase, so failures surface in the
  group that caused them. Group 8 is integration only.
- **Two open questions deliberately left for implementation** (the Translators group GUID and the
  exact migration base class in 17.8.2): neither changes what gets built.

### Revision: site scripts in TypeScript, built by Vite

**Decided (21 November 2026):** the design first had the site's `push.js` and `sw.js` as plain
JavaScript in `wwwroot`. Changed to TypeScript under `src/NotificationsRevisit.Web/Client/`,
built by Vite into `wwwroot`, revised with `/openspec-update-change`.

- Two entry points with fixed output names: `wwwroot/js/push.js` and `wwwroot/sw.js`. No content
  hashes, because a service worker needs a stable URL at the site root.
- `sw.ts` is type-checked against the WebWorker library, so push and click events are typed.
- Build output is gitignored. A web `.csproj` target runs `npm ci` (when needed) and
  `npm run build` before the .NET build, so `dotnet build` works from a fresh clone.

**Why:** one front-end approach across the repo (the RCL already uses Vite and TypeScript), type
checking for code that is otherwise hard to debug, and a build an agent can run with one command.

**Rejected:** plain JavaScript (no type checking, mixed approaches) and committing built files
(stale output, noisy diffs). 2018 used jQuery modules bundled by Gulp.

**Process note for the article:** this was a change of mind after the plan was written. Updating
the spec first (design D1, D7, D9 and a new risk; the proposal's impact; a new task 5.3) kept the
plan and the code from drifting apart before any code existed. The behaviour specs did not change,
which is the point: they describe what visitors experience, not the language it is written in.
