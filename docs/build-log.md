# Build log

A running record of what we did, in order, and why. Written to feed the 24 Days in Umbraco article,
so it records the reasoning and the dead ends, not just the commands.

Times are UK time (GMT) on 21 November 2026, taken from the session transcript and Git commit
times. They are wall-clock times: they include the human's thinking and review time between
messages, not just the agent's working time.

### Timeline

| Time | What happened | Elapsed |
|---|---|---|
| 11:01 | Brief: vanilla Umbraco 17 with Automate, AI-first basics, document decisions | 0 min |
| 11:05 | Project created from the `umbraco` template | 4 min |
| 11:06 | First boot fails (machine-wide connection string override) | 5 min |
| 11:08 | Site boots: unattended install and Automate migrations complete | 7 min |
| 11:11 | Phase 1 committed: running site, `AGENTS.md`, build log | **10 min** |
| 11:16 | Phase 2 starts: OpenSpec explore mode, approach logged first | 15 min |
| 11:21 | 2018 article and repo researched; scope decided (parity, no Automate) | 20 min |
| 11:23 to 11:41 | Four design decisions, one question at a time | 22 to 40 min |
| 11:44 | Capture approved: `openspec init`, proposal, three specs, design, tasks | 43 min |
| 11:51 | Plan committed, strict validation passing | **50 min** |
| 12:04 to 12:07 | Plan revised (site scripts in TypeScript and Vite) and committed | 66 min |
| 12:10 to 12:15 | Agent permissions allowlist; Playwright push spike proves real pushes arrive | 69 to 74 min |
| 12:32 | Verification plan revised to match the spike | 91 min |
| 12:33 | Timer paused by the human (break; not counted in elapsed time) | 92 min |
| 13:32 | Timer resumed; implementation handed to a new agent session (orchestrator pattern) | 92 min |
| 13:33 | Phase 4 starts: `/openspec-apply-change`, group 1 (scaffold the RCL) | 93 min |
| 13:44 | Group 2 (persistence): migration plan, repositories, SQLite-backed tests | 104 min |
| 13:45 to 13:49 | Group 3 (push delivery): options, WebPush sender, push service, VAPID keys, README | 105 to 109 min |
| 13:50 to 13:52 | Group 4 (subscribe endpoint): antiforgery, validation, per-IP rate limit via a pipeline filter | 110 to 112 min |
| 13:53 to 14:05 | Group 5 (site): content migration, fresh database, Vite site scripts, opt-in verified in Playwright | 113 to 125 min |
| 14:06 to 14:10 | Group 6 (backoffice API): policy, controller, announcement service, 403 and 400 paths verified | 126 to 130 min |
| 14:11 to 14:17 | Group 7 (dashboard): generated client, manifest conditions, Lit dashboard and sidebar, keyboard pass | 131 to 137 min |
| 14:15 | First real announcement sent from the dashboard and received by the browser (task 8.1) | 135 min |
| 14:18 to 14:21 | Human click check in Playwright's Chromium fails: Windows drops the click (task 8.2) | 138 to 141 min |
| 14:22 to 14:23 | Unsubscribe and resend: 410 after about 45 seconds, subscription removed (task 8.3) | 142 to 143 min |
| 14:24 to 14:26 | Click check repeated in the human's own browser: both rounds pass (task 8.2) | 144 to 146 min |
| 14:27 | All 30 tasks ticked, strict validation run, write-up complete (task 8.4) | **147 min** |
| 14:33 | Implementation committed (`75aac04`); timer paused by the human for code review and the 2018 comparison | 153 min |

## Phase 1: setting up the PoC (11:01 to 11:11)

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

### 2. Creating the project (11:05)

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

### 6. First run and verification (11:06 to 11:10)

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

## Phase 2: gathering requirements from the 2018 article (11:16 to 12:07)


### The goal

Recreate, and improve, the custom backoffice extension from the 2018 24 Days article
[Push notifications and Umbraco](https://archive.24days.in/umbraco-cms/2018/push-notifications-and-umbraco/)
on modern Umbraco, then compare what changed.

### Approach (decided before starting the research, 11:17)

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

### Decision: scope of the first version (11:21)

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

### Decision: the parity boundary (11:23)

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

### Decision: a separate Razor class library (11:34)

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

### Decision: NPoco with a migration plan (11:38)

**Decided (21 November 2026):** the subscriptions and send history tables use NPoco POCOs and
an Umbraco package migration plan, run at startup.

**Why:**
- NPoco is PetaPoco's descendant, so the code reads as a direct "then and now" with 2018.
- The real change is the approach: 2018 checked "does the table exist? if not, create it".
  Today a versioned migration plan records each step, so schema changes later are repeatable.
- One migration runs on both SQLite (local) and SQL Server (Azure). EF Core would need a
  migration set per provider, which is work about EF, not about Umbraco.

**Rejected:** EF Core. Familiar and supported, but heavier than two small tables need.

### Decision: the site owns the content model, the RCL is configured (11:41)

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

### Capturing the plan as an OpenSpec change (11:44 to 11:51)

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

### Revision: site scripts in TypeScript, built by Vite (12:04 to 12:07)

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

---

## Phase 3: preparing to build (12:10)

### Agent permissions allowlist (12:11)

Added `.claude/settings.json` (committed, so it applies to anyone using Claude Code in the repo):

- **Allowed without a prompt:** `dotnet build`, `restore`, `run`, `test`, `list package`, and
  `npm ci`, `install`, `run`, for both the Bash and PowerShell tools.
- **Denied:** reading `secrets.json` (user secrets) and `*.sqlite.db` files.

**Why now:** implementation runs these commands dozens of times. Without an allowlist every one is
a permission prompt, which breaks the flow the article wants to show. The deny rules keep secrets
and the database out of the agent's context even if it goes looking.

**Process note:** in phase 1 Claude Code's safety check blocked the agent from writing this file
on its own initiative, because it changes the agent's own permissions. This time the human asked
for it explicitly, and it went through. That is the right split: the agent can suggest
permissions, but a person decides.

### Spike: can Playwright receive a real web push? (12:12 to 12:15)

Task 8.1 needs a browser the agent can drive that actually receives push messages. The in-app
preview browser was uncertain, so we tested Playwright with a throwaway page (in the agent's
scratchpad, not the repo): Node's `web-push` library, a service worker and a subscribe button.

**Result: yes.**

| Step | Outcome |
|---|---|
| Grant notification permission via Playwright (`context.grantPermissions`) | Granted, no prompt |
| `pushManager.subscribe` with a VAPID key | Real subscription on `fcm.googleapis.com` (Playwright's Chromium 156) |
| Server sends a push | Push service returned **201** |
| Did the service worker show it? | `registration.getNotifications()` returned "Spike / 21 Nov Pool closed" |
| Unsubscribe in the browser, then send again | Push service returned **410**, the signal our cleanup relies on |

**What Playwright cannot do:** click the OS notification. Native toasts sit outside the page, so
the `notificationclick` handler (focus or open the announcement page) needs a manual check, or a
person clicking the toast during the end-to-end run.

**Gotchas from the spike:**
- `getNotifications()` on the service worker registration is the reliable way to assert a
  notification was shown; the OS toast itself is invisible to the test.
- Playwright MCP writes console logs to `.playwright-mcp/` in the working directory. Now gitignored.

### Revision: verification plan follows the spike (12:32)

Updated the OpenSpec change so the plan matches what the spike proved:

- **Design D10:** end-to-end testing runs in Playwright (not "in-app or Playwright"), grants
  notification permission up front, asserts with `registration.getNotifications()`, and tests
  the 410 cleanup against the real push service. A person checks the click.
- **Tasks:** 8.1 is the automated send and display check, new 8.2 is the human click check,
  8.3 is the unsubscribe and 410 check, and the write-up moves to 8.4.

**Rejected:** dispatching a synthetic `notificationclick` inside the service worker. It would
pass, but it would only prove our test harness works, not that the browser behaves.

**Process note:** the spike took about three minutes and replaced an assumption in the plan
("the agent can verify the click") with evidence. Cheap spikes before implementation are one of
the easiest wins in AI-assisted work: the agent can build a throwaway test faster than we can
debate whether something will work.

---

## Phase 4: implementation (13:33 onwards)

A second agent session implements the OpenSpec change with `/openspec-apply-change
rebuild-announcements-dashboard`, working through `tasks.md` in order and ticking a box only once
its stated verification has passed. The planning session acts as orchestrator: it handed over the
brief and relays questions to the human.

**Task count:** `tasks.md` has 30 tasks, and implementation added none (the diff to `tasks.md` is
checkbox ticks only). Phase 2 recorded 26 tasks when the plan was first captured. The later
revisions in phases 2 and 3 brought it to 30: new task 5.3 for the TypeScript site scripts, and
group 8 rewritten as 8.1 to 8.4 after the Playwright spike.

### Group 1: scaffold the extension library (13:33 to 13:43)

**Done:**

- `dotnet new umbraco-extension -n NotificationsRevisit.Announcements --include-example
  --site-domain https://localhost:44371 --version 17.8.2` into `src/`, added to the solution and
  referenced from the web project.
- We generated it into a scratch folder first and read every file before creating it in the repo.
  Cheap, and it surfaced the first surprise before it could cause damage.
- `tests/NotificationsRevisit.Announcements.Tests` from `dotnet new xunit`, versions moved to
  `Directory.Packages.props`, one placeholder test passing.

**What the template generates** (the 17.6.0 template installed on the machine):

| File | What it is |
|---|---|
| `NotificationsRevisit.Announcements.csproj` | Razor SDK, `StaticWebAssetBasePath` `/`, four Umbraco package references, `Client/` excluded from content |
| `Directory.Packages.props` | Its **own** central package file, inside the project folder |
| `Composers/...ApiComposer.cs` | Registers a Swagger document for the extension and an operation ID handler for tidy client method names |
| `Controllers/...ApiController(Base).cs` | Example Management API controller: `ping`, `whatsTheTimeMrWolf`, `whatsMyName`, `whoAmI` |
| `Client/` | Vite, Lit and TypeScript: a bundle manifest, an entry point that wires the generated client into backoffice auth, an example dashboard, and a pre-generated hey-api client |

**Surprises and how we handled them:**

1. **A nested `Directory.Packages.props`.** With "Central" package management the template writes
   its own props file in the project folder. MSBuild uses the nearest one, so it would silently
   shadow the root file. We deleted it and moved its four `PackageVersion` entries (already
   17.8.2, because we passed `--version 17.8.2`) to the root file. `dotnet list package` shows
   17.8.2 for every Umbraco package.
2. **`npm install` failed on a peer dependency.** The 17.6.0 template pins `@hey-api/openapi-ts`
   `^0.85.2`, but `@umbraco-cms/backoffice` 17.8.2 declares a peer of `>=0.99.0 <1.0.0`. We bumped
   it to `^0.99.0`. Lesson: when you align NuGet packages to a newer patch, the npm side moves too.
3. **The pre-generated API client no longer type-checked** against the 17.8.2 backoffice types
   (a `QuerySerializerOptions` mismatch). We regenerated it from the running site's Swagger
   (`npm run generate-client`), which is the template's own workflow.
4. **hey-api 0.99 resolves `runtimeConfigPath` from the working directory**, not from the output
   folder, so the generated import pointed three folders up. Changed `'../hey-api'` to
   `'./src/hey-api'` in `scripts/generate-openapi.js`.

**Adjustment (recorded, not a design change):** design D9 specifies an npm build target for the
site's scripts. The RCL needs exactly the same thing, or its dashboard bundle (gitignored build
output) would be missing from a fresh clone. So the target lives in one shared file,
`build/ClientBuild.targets`, imported by both projects. It runs `npm ci` when `node_modules` is
missing, runs `npm run build` only when `Client/` has changed (declared inputs and outputs), fails
with a clear message when npm is missing, and adds the freshly built files to the static web assets.
Verified with a clean build: the bundle is regenerated and served at
`/App_Plugins/NotificationsRevisitAnnouncements/notifications-revisit-announcements.js` (200).

**Still open in this group:** 1.3 asks for the example dashboard to be seen in the backoffice. That
needs a backoffice login (see below), so it is verified with the later backoffice checks.

**Process note: logging in as an agent.** The admin password lives in user secrets, and the repo's
agent settings deny reading `secrets.json`. The agent first tried a Playwright snippet that read
the secrets file inside the browser and filled the login form, so the value would never reach the
transcript. Claude Code's auto-mode classifier blocked it as working around a deny rule. That is the
right call: the rule's intent is that the agent does not touch that file at all. The human's answer
was pragmatic: this is a throwaway PoC, so use a known dummy admin password. It still lives in user
secrets (nothing changes in the repo), and the agent sets it before the fresh-database install.

### Group 2: persistence (13:44)

**Open question resolved: the migration base class.** Checked against the installed 17.8.2
`Umbraco.Infrastructure` package: both `MigrationBase` (sync `Migrate()`) and `AsyncMigrationBase`
(`MigrateAsync()`) exist, alongside `PackageMigrationPlan` in `Umbraco.Core`. We used
`AsyncMigrationBase`, the newer of the two.

**Done:**

- Two NPoco DTOs with Umbraco's schema annotations: `announcementsPushSubscription` (unique index on
  `Endpoint`, length 800) and `announcementsHistory` (page **key**, not id).
- `AnnouncementsMigrationPlan : PackageMigrationPlan` with one step, `CreateAnnouncementsTables`.
  Umbraco discovers package migration plans by type, so no registration is needed.
- Two repositories using `IScopeProvider`: upsert by endpoint, list, count and delete by endpoints;
  history insert and list newest first.

**Decision: test the SQL without Umbraco.** The repositories are thin: open a scope, call a small
internal "queries" class with the scope's NPoco database, complete. The tests run that queries class
against an **in-memory SQLite** database through plain NPoco, with the same unique index as the
migration. Five tests, including "resubmitting an endpoint leaves one row with the latest keys".

**Rejected:** Umbraco's integration test package (NUnit based, boots a whole Umbraco instance: heavy
for two tables) and mocking NPoco (it would test the mock, not the SQL).

**Still open in this group:** 2.1 (tables exist on a fresh database, migration visible in the
Packages section) is verified together with 5.1, so the database is reset once, not twice.

### Group 3: push delivery (13:45 to 13:49)

**Done:**

- `WebPush` 1.0.13 added centrally. It ships no XML docs, so we coded against its known API and let
  the compiler confirm it (`WebPushClient(HttpClient)`, `SendNotificationAsync`,
  `WebPushException.StatusCode`).
- `PushOptions` bound from `Announcements:Push`, with a custom `IValidateOptions` and
  `ValidateOnStart`. Data annotations would not recurse into the nested `Vapid` object, and a custom
  validator lets each message say what to do: "Vapid:PrivateKey is missing. Store it in user
  secrets, never in appsettings."
- `IPushSender` (one delivery, returns the push service's status code) and `WebPushSender`,
  registered as a typed `HttpClient` through `IHttpClientFactory`.
- `PushService`: `Parallel.ForEachAsync` with the configured concurrency. 2xx counts as success;
  404 and 410 are collected and deleted **after** the loop; anything else (including no response)
  is logged with its status and the subscription kept. Returns
  `PushSendResult(Attempted, Succeeded, Failed, Removed)`.
- The payload keeps the 2018 shape (`title`, `message`, `url`) and adds `icon`, so the service
  worker does not hard-code an icon path.
- VAPID keys generated with `npx web-push generate-vapid-keys --json`. The private key was piped
  straight into user secrets, so it never appeared on screen or in the agent's context. The public
  key and subject are in `appsettings.Development.json`; title, icon and concurrency in
  `appsettings.json`.
- `README.md` documents key generation, where each setting lives, and that changing keys
  invalidates existing subscriptions.

**Verified:**

- The site refuses to start with no push settings:
  `OptionsValidationException: Announcements:Push:Title is missing.; ... Vapid:PrivateKey is
  missing. Store it in user secrets, never in appsettings.`
- `git grep --untracked` finds no private key in the repo.
- Six push service tests with a fake sender: all success; the spec's mixed outcome (4 attempted,
  2 ok, 1 failed, 1 removed, with only the gone endpoint deleted); 404 counted as gone; no response
  counted as a failure and kept; no subscribers; payload contents.
- The README's `npx web-push generate-vapid-keys` runs as written.

**Gotcha:** the Edit tool rewrote the `+` escape in `appsettings.json` when matching text near
it, so we appended the new section with a small text replace instead. Small, but the kind of thing
that silently corrupts a secret-looking value.

### Group 4: public subscribe endpoint (13:50 to 13:52, finished 14:07)

**Done:**

- `PushSubscriptionsController`, a plain `[ApiController]` on `ControllerBase` at
  `POST /api/announcements/push-subscriptions`. The request model mirrors the browser's
  `PushSubscription.toJSON()` (`endpoint`, `keys.p256dh`, `keys.auth`), so the site script posts the
  subscription object as it is. Data annotations: all required, endpoint a URL of at most 800
  characters.
- `[ValidateAntiForgeryToken]`. The layout renders the token in a meta tag and the script sends it
  in the `RequestVerificationToken` header (ASP.NET Core's default header name).
- `[EnableRateLimiting]` with ASP.NET Core's built-in limiter: a fixed window partitioned by client
  IP, default 5 calls per 60 minutes (`Announcements:Subscribe`). The middleware goes in through
  an `UmbracoPipelineFilter` with `PostRouting = app => app.UseRateLimiter()`, registered in the
  composer. `Program.cs` is untouched.

**Verified against the running site:**

| Request | Result |
|---|---|
| No antiforgery token (calls 1 to 5) | 400 |
| Sixth and seventh call in the window | 429 |
| Real token from the page, `keys` missing | 400, `"The Keys field is required."` |
| Real token, endpoint missing / `auth` missing / endpoint of 800+ characters | 400 each |
| Wrong token | 400 |
| Subscriber count afterwards (checked in group 6) | 1, the browser's real subscription only |

**Decision: a one-minute window in development.** Testing the limit from localhost would otherwise
lock the agent's own browser out of subscribing for an hour. `appsettings.Development.json` sets
`WindowMinutes: 1`; the default (5 per hour) is unchanged everywhere else.

**Observation for the article:** the rate limiter runs before the antiforgery filter, so rejected
requests still use up the allowance. That is the behaviour you want against a flood, but it means
a bot can lock out a shared IP. The design already notes that IP rate limiting is coarse.

### Group 5: the site's content model and scripts (13:53 to 14:05)

**Done:**

- **Content model migration** in the web project (`SiteContentMigrationPlan`, step
  `CreateSiteContent`): templates `layout`, `home` and `announcement` created from the committed
  view files; document types Home (allowed at root, allows Announcements), Announcements (allows
  Announcement) and Announcement (`heading` Textstring, mandatory; `mainText` Textarea); and the
  Home and Announcements nodes, published. Fixed keys, and every step checks before creating.
  It uses the same async editing services the announcement flow uses: `IContentTypeEditingService`,
  `IContentEditingService`, `IContentPublishingService`.
- **`Announcements:ContentModel` options** in the RCL with a validator naming each missing alias,
  configured in the site's `appsettings.json`.
- **Site scripts**: `Client/` with Vite and TypeScript. `push.ts` (DOM types) and `sw.ts`
  (WebWorker types) are type-checked by two small tsconfigs, because the DOM and WebWorker
  libraries conflict in one program. Vite writes `wwwroot/sw.js` and `wwwroot/js/push.js` with fixed
  names; `emptyOutDir` is off because `wwwroot` also holds committed files. Both outputs are
  gitignored and built by the shared `build/ClientBuild.targets`.
- **Layout and views**: one shared layout with the antiforgery token, the VAPID public key and the
  opt-in control (hidden by default; `push.ts` reveals it). The status line is a `role="status"`
  live region. A 192 pixel icon at `/images/announcement-icon.png`.

**Surprise: Umbraco reads `Layout = "..."` in a template as its master template.** Creating the
`home` template failed with `MasterTemplateNotFound`, because its content said
`Layout = "_Layout.cshtml"` and there was no `_Layout` template. The Umbraco-native fix: make the
layout a real (master) template, `layout.cshtml`, created first. That is also exactly how 2018 did
it, with master templates.

**Fresh database, once.** We renamed the existing `Umbraco.sqlite.db` to `.bak-1356` (not deleted)
and let the unattended install build a new one, with the dummy admin password set in user secrets
first. One start then verified group 2 and group 5 together:

```
Starting package migration for NotificationsRevisit.Announcements
Package migration completed for NotificationsRevisit.Announcements (9ms)
Starting package migration for NotificationsRevisit.Site
Package migration completed for NotificationsRevisit.Site (869ms)
```

**Verified:**

- 2.1: both migrations ran on the fresh database. The Packages section lists
  `NotificationsRevisit.Site` under Migrations. `NotificationsRevisit.Announcements` appears as an
  installed package, because its migration plan and its `umbraco-package.json` share a name. The
  subscriber count and history endpoints (group 6) read from both tables.
- 5.1: Home and Announcements exist in the content tree; Home renders (200).
- 5.2: started without the aliases, the site stopped with
  `Announcements:ContentModel:PageDocTypeAlias is missing.; ...ParentDocTypeAlias...;
  ...HeadingPropertyAlias...; ...TextPropertyAlias is missing.`
- 5.3: deleted both `node_modules` folders and all build output, ran `dotnet build`: `npm ci` ran
  for both clients (15 and 163 packages), both outputs were produced, and `git status` lists none
  of them.
- 5.4, in Playwright: on load, `Notification.permission` stayed `default` and
  `Notification.requestPermission` (wrapped to count calls) had been called 0 times, so no prompt.
  After granting permission and clicking: the POST returned 204, a real `fcm.googleapis.com`
  subscription existed, the button read "Notifications are on" and was disabled, and the subscriber
  count was 1.
- 5.5: after a reload the control stayed hidden (already subscribed). For the insecure context we
  routed `http://insecure.test/` to the real site's responses in Playwright, so the same page ran on
  plain HTTP: `isSecureContext` false, no `navigator.serviceWorker`, control hidden.
  (`http://localhost` would not do: browsers treat localhost as a secure context.)

### Group 6: the backoffice API (14:06 to 14:10)

**Open question resolved, and the plan was wrong: the Translators group key.** The design expected
`550e8400-e29b-41d4-a716-446655440000`. Reading `Constants.Security.TranslatorGroupKey` from the
installed 17.8.2 `Umbraco.Core.dll` gives **`f2012e4c-d232-4bd1-8eae-4384032d97d8`**, and creating a
user with that group id succeeded, so it is the key in the database. The C# code uses the constant;
the manifest (group 7) needs the literal GUID. This is exactly why the plan left it open: a GUID
from memory looks just as plausible as the real one.

**Done:**

- **Policy `Announcements.Access`**: built in `PostConfigure<AuthorizationOptions>` from Umbraco's
  own `SectionAccessContent` policy (same schemes and requirements) plus a
  `NotInTranslatorsGroupRequirement`, whose handler checks the current backoffice user's group keys.
- **`AnnouncementsController : ManagementApiControllerBase`** at `/umbraco/announcements/api/v1/`,
  in its own Swagger document (`announcements`): `GET subscriptions/count`, `GET history`,
  `POST announcements`. The template's example controllers are deleted and its Swagger composer
  renamed and kept.
- **`AnnouncementService`** follows D5 step by step. The content side sits behind a small
  `IAnnouncementPages` interface (find parent, create and publish, resolve the absolute URL), so
  the orchestration can be unit tested without Umbraco. A publish failure returns the reason and
  nothing is sent; a published page without an absolute URL is also treated as a failure rather
  than sending a broken link.

**Verified:**

- The Swagger UI's "Announcements Backoffice API" document lists exactly the three endpoints.
  Anonymous calls get 401. As admin: count 1, history `[]`.
- Nine unit tests (20 in total now): page naming (`20261121-pool-closed`, trimmed), the message
  (`21 Nov Pool closed`), the success path (URL pushed, history recorded with user, page key and
  counts), **failed publish sends nothing and records nothing**, missing parent creates nothing,
  and required fields.
- Missing parent against the running site: we moved the Announcements node to the recycle bin and
  posted an announcement. Result: 400, "There is no announcements folder to create the page in...",
  and history unchanged. Then we restored and republished the node.
- Translator-only test user (`translator@example.com`, dummy password, local only): 403 on all three
  endpoints, and nothing created.
- **Stronger check:** we put the same user in Editors **and** Translators. Now they can read the
  content tree (200), so Umbraco's section policy passes, but all three announcement endpoints still
  return 403. That proves our translator requirement does real work, rather than Umbraco's section
  check doing it for us.

**Dead ends worth a line:**

- Calling the Management API from the backoffice page with cookies alone gives 401. The API wants
  the backoffice's bearer token, so the test captures it from one of the backoffice's own requests.
- Restoring from the recycle bin with `target: null` returned 400 "NotAllowed"; restoring with the
  parent as the explicit target worked. Trashing also unpublishes, so the node needed republishing.

### Group 7: the dashboard and sidebar (14:11 to 14:17)

**Done:**

- **Example code removed** (the example dashboard; its controllers went in group 6).
  `generate-client` now points at `/umbraco/swagger/announcements/swagger.json`. The regenerated
  client has one class, `AnnouncementsService`, with exactly `createAnnouncement`, `getHistory`
  and `getSubscriberCount`.
- **The dashboard is registered in `umbraco-package.json`**, with its conditions in plain JSON:
  `Umb.Condition.SectionAlias` matching `Umb.Section.Content`, and `Umb.Condition.CurrentUser.GroupId`
  with `noneOf: ["f2012e4c-d232-4bd1-8eae-4384032d97d8"]`. That is the closest modern equivalent of
  2018's `Dashboard.config` entry with `<deny>translator</deny>`, and it reads almost the same. The
  condition's config type (`match`, `oneOf`, `allOf`, `noneOf`) was confirmed in the installed
  17.8.2 `@umbraco-cms/backoffice` package.
- To reference the dashboard element from JSON it needs a stable file name, so Vite now has **two
  library entries**: the bundle (entry point and modal manifests) and `announcements-dashboard.js`.
  Shared code, including the configured API client, lands in one chunk both import.
- **Dashboard element** (Lit, UUI): subscriber count with a loader and `aria-live`, "Add new
  announcement", and a `uui-table` of history (heading, sent date in en-GB, sender, "1 of 1"
  delivered, "Edit page" linking to `section/content/workspace/document/edit/{pageKey}`). Empty state
  "No announcements have been made yet." It refreshes after a send.
- **Sidebar**: a `UmbModalToken` with `type: 'sidebar'`, opened with `umbOpenModal` (which rejects
  on cancel, so "cancelled" needs no special code). The modal sends the request itself, so it can
  keep the editor's input on an error. Labelled `uui-input` and `uui-textarea` fields with hints,
  inline required-field errors (`aria-invalid`, `aria-describedby`, focus to the first bad field),
  a server error in a `role="alert"` box, "What is a push notification?" as a button-triggered
  `uui-popover-container`, and a "Publish and send" button that shows UUI's waiting state and
  ignores clicks while busy. Success raises a notification: "Pool closed" was published and sent
  to 1 of 1 subscriber.

**Small things the compiler caught:** the entry point module type requires an `onUnload` export (a
no-op now), and the template's `console.log` greetings are gone.

**Verified:**

- 7.1: client generated from the Announcements document only; `npm run build` passes.
- 7.2: as admin, the Content section shows Welcome, Redirect URL Management and **Announcements**
  (the example dashboard has gone). As a user in Editors **and** Translators, Content shows only
  Welcome and Redirect URL Management, and going straight to `.../dashboard/announcements` renders
  no dashboard element. As translator-only, the user lands in the Translation section with no
  Content section at all.
- 7.3: empty state on the fresh database; after the first send the history row shows "Pool closed,
  21 Nov 2026, 14:15, PoC Admin, 1 of 1"; "Edit page" opens the document workspace for
  `20261121-pool-closed` by key. After the second send (group 8) the table showed "Gala moved to
  Saturday, 14:18" above "Pool closed, 14:15", each with its own "Edit page" link by page key, and
  the count "1 subscriber". 7.3 was ticked only then, once newest-first had been seen with two rows.
- 7.4: opening and cancelling sent nothing (no POST, still the empty state). Submitting empty
  flagged "Enter a short name." and "Enter a heading." and sent nothing. A **double click** on
  "Publish and send" made exactly **one** POST; the button was in its `waiting` state.
- 7.5, keyboard only:
  - From the top of the page, Tab reaches "Add new announcement" (after the backoffice's own
    navigation) and Enter opens the sidebar.
  - Tab order in the sidebar: Short name, Heading, Main text, "What is a push notification?",
    Cancel, Publish and send.
  - Enter on the explanation button shows the text; Escape closes just the explanation; Enter on
    Cancel closes the sidebar; Escape also closes it.
  - Accessible names: "Short name", "Heading", "Main text".
  - **Finding, fixed:** focus did not move into the sidebar when it opened (it stayed on the scroll
    container). The sidebar now focuses Short name on open. Verified.
  - **Finding, recorded:** after the last button, Tab leaves the sidebar and reaches the page
    behind it. That is the backoffice's own modal container, not our element, so we left it alone.
    Worth raising with Umbraco rather than working around in an extension.
  - The two console errors during the run come from Umbraco Automate (its SignalR hub and a 401
    on its API), not from this extension.

### Group 8: end to end (14:15 to 14:27)

#### 8.1: send from the dashboard, receive in the browser (Playwright)

Playwright's Chromium was subscribed in task 5.4 (permission granted with
`context.grantPermissions`). With the public site open in a second tab, we sent from the dashboard:
short name `pool-closed`, heading "Pool closed", and a line of main text.

| Check | Result |
|---|---|
| Requests from a double click on "Publish and send" | 1 |
| Response | 200, page `20261121-pool-closed`, URL `https://localhost:44371/announcements/20261121-pool-closed/`, attempted 1, succeeded 1, failed 0, removed 0 |
| Public page | 200, title and `<h1>` "Pool closed", date "21 November 2026", main text rendered |
| `registration.getNotifications()` | title "Swim club announcement", body "21 Nov Pool closed", tag `announcement`, absolute icon URL, `data.url` the page URL |
| Dashboard | Toast: "Pool closed" was published and sent to 1 of 1 subscriber. History row: Pool closed, 21 Nov 2026, 14:15, PoC Admin, 1 of 1 |

A second announcement ("Gala moved to Saturday") **replaced** the first notification rather than
stacking (only one was showing), which is the fixed-tag behaviour the spec asks for.

#### 8.2: the human clicks the notification

**First attempt, in Playwright's Chromium: it could not be verified.** The human clicked the Windows
toast and nothing happened. To find out why, we added a test-only `notificationclick` listener
inside the running service worker (through Playwright's service worker handle; the site code was not
changed) and sent a fresh notification. After the second click the probe was still installed, but
had recorded **no** click event, and the notification had gone. So Windows dismissed the toast
without passing the click back to the browser. Our handler never ran.

Our explanation: Playwright's bundled Chromium ("Chrome for Testing") is not registered with
Windows as an app that toast clicks are routed back to; an installed Chrome or Edge is. The phase 3
spike could not have caught this, because it never clicked a notification.

**Second attempt, in the human's own browser: passed.** The human opened the site in their
installed browser, opted in ("Notifications are on"; the subscriber count went from 0 to 1), and we
sent two announcements from the backoffice API:

| Round | Set-up | Click result |
|---|---|---|
| A: tab already open | The announcement page open in a tab, another tab in front | The browser switched to the existing tab; no new tab or window |
| B: no tab open | No tab showing that page | The page opened in a new tab |

**Lessons for the article:**

- A test browser that receives pushes is not the same as one that handles a click on the OS
  notification. The probe turned "it didn't seem to work" into a fact (the event never arrived)
  in one step, rather than a debugging session on code that was never called.
- Keep the human check in a real, installed browser. It is also what a visitor uses.

#### 8.3: unsubscribe and send again (Playwright)

We unsubscribed in the browser (`subscription.unsubscribe()`), then sent from the dashboard.

**Surprise: the push service does not say "gone" straight away.** The first send after
unsubscribing was still accepted (201: attempted 1, succeeded 1, removed 0). About 45 seconds
later the next send got the 410 we expected:

| Check | Result |
|---|---|
| Send result | attempted 1, succeeded 0, failed 0, **removed 1** |
| Subscriber count | 1 before, **0** after |
| Server log | `Removed 1 expired push subscriptions` |

So the cleanup path works against Google's real push service, and a 410 is counted as removed,
not failed. Anyone writing an automated test for this needs to wait or retry: the spike happened to
leave enough time between steps, so it never showed the delay.

#### Screenshots

Saved in [docs/screenshots](screenshots/):

| File | Shows |
|---|---|
| `01-example-dashboard.png` | The template's example dashboard, proving the scaffold (task 1.3) |
| `02-site-opt-in.png` | The site with the opt-in button, before permission |
| `03-site-notifications-on.png` | "Notifications are on" after subscribing |
| `04-dashboard-empty.png` | Dashboard: 1 subscriber, "No announcements have been made yet." |
| `05-sidebar-required-fields.png` | Sidebar with both required-field errors |
| `06-sidebar-tooltip-keyboard.png` | "What is a push notification?" opened from the keyboard |
| `07-sent-and-history.png` | Success notification and the first history row |
| `08-history-newest-first.png` | Two rows, newest first |
| `09-editor-translator-no-dashboard.png` | Content section for an Editor plus Translator: no Announcements tab |
| `10-swagger-announcements.png` | The extension's own Swagger document with three endpoints |

### Same feature, two eras: 2018 versus 2026

| Concern | 2018 (Umbraco 7.12) | 2026 (Umbraco 17.8.2 LTS) |
|---|---|---|
| Where the feature lives | `Core` class library plus the site | Razor class library from `dotnet new umbraco-extension`, carrying its own front end as static web assets |
| Register the dashboard | `Dashboard.config` entry | `umbraco-package.json`: a `dashboard` extension with section and user group conditions |
| Hide from translators | `<deny>translator</deny>`, UI only | `noneOf` the Translators group key in the manifest, **and** a server policy that returns 403 |
| Backoffice UI | AngularJS controller, views, `umb-overlay` | Lit and TypeScript with UUI, a sidebar `UmbModalToken`, built by Vite |
| Calling the server | `umbRequestHelper` and `$http` | TypeScript client generated from the extension's own OpenAPI document |
| Backoffice API | `UmbracoAuthorizedJsonController` | `ManagementApiControllerBase`, versioned route, custom policy built on Umbraco's |
| Public subscribe endpoint | `UmbracoApiController`, `System.Web` antiforgery, hand-written throttle | Plain `ControllerBase`, ASP.NET Core antiforgery, built-in rate limiter added through a pipeline filter |
| Tables | `ApplicationEventHandler`: "create the table if missing" | A versioned package migration plan, recorded and visible in the Packages section |
| Data access | PetaPoco, `ApplicationContext.Current` | NPoco through scopes, dependency injection throughout |
| History | A JSON file in `App_Data` | A database table with delivery counts |
| Content model | Hard-coded `newsPage` and `announcementsFolder` | Owned by the site, created by the site's own migration; the extension reads aliases from validated configuration |
| Create and publish the page | `IContentService.SaveAndPublish`, then carry on whatever happened | `IContentEditingService` and `IContentPublishingService`; a failed publish stops the send and tells the editor why |
| Sending | WebPush 1.0.11, one at a time, errors swallowed | WebPush 1.0.13 behind an interface, bounded concurrency, failures logged, gone subscriptions removed after the loop |
| Duplicate subscriptions | A new row every time | One row per endpoint (upsert, unique index) |
| VAPID private key | `vapid.json` in a public Git repo | User secrets locally; startup fails, naming the setting, if missing |
| Site scripts | jQuery modules bundled by Gulp | TypeScript built by Vite, from `dotnet build`, output gitignored |
| Service worker | Largely the same | Largely the same, now type-checked; the icon path is absolute |
| Tests | None | 20 xUnit tests, plus Playwright end to end and one human click check |

The last three rows carry the article's point. The web platform code barely changed in eight
years, while almost every Umbraco extension point was replaced, and all for the better: the modern
version is typed end to end, secured on the server, versioned in the database, and testable.

### Deviations from the plan

All small, and recorded where they happened:

1. **Translators group key** is `f2012e4c-d232-4bd1-8eae-4384032d97d8`, not the GUID design.md
   expected (open question, resolved against the installed package).
2. **Migration base class:** `AsyncMigrationBase` (open question, resolved).
3. **Shared npm build target** for both projects (`build/ClientBuild.targets`), not just the
   site: the RCL's bundle is gitignored build output too.
4. **Template fixes:** nested `Directory.Packages.props` removed; `@hey-api/openapi-ts` bumped to
   `^0.99.0`; `runtimeConfigPath` corrected for hey-api 0.99.
5. **The site layout is an Umbraco master template** (`layout`), because Umbraco treats a
   template's `Layout = "..."` as its master.
6. **Two Vite entries in the RCL**, so `umbraco-package.json` can reference the dashboard element
   by a stable name.
7. **One-minute rate-limit window in development** only, so testing the limit does not lock the
   tester out for an hour.
8. **Sidebar focuses its first field on open** (an accessibility finding from 7.5).
9. **The human click check ran in the human's own browser**, not Playwright's Chromium, which
   cannot receive Windows toast clicks.

### Left behind on the local machine (not in Git)

- The original CMS database, renamed to `umbraco/Data/Umbraco.sqlite.db.bak-1356`. Now gitignored
  (`*.bak-*`); delete it when you no longer need it.
- Test data in the fresh database: a translator test user (`translator@example.com`, dummy
  password) and seven announcement pages from the end-to-end runs.
- The admin password in user secrets is now a dummy value, at the human's request for this PoC.
