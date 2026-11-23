# Build log: approach 2 (Umbraco Automate)

A running record of building approach 2, in order, and why. Approach 1 (the custom dashboard)
and all planning up to this point are in [build-log.md](build-log.md); the plan for this approach
is the OpenSpec change `openspec/changes/automate-announcements/`.

Times are UK time (GMT) on 23 November 2026, from the session transcript and Git. They are wall-clock times,
including the human's thinking and review time. The timer starts at 0 here; approach 1 took
153 minutes (see the main build log).

## Timeline

| Time | What happened | Elapsed |
|---|---|---|
| 16:28 | Timer started; plan committed; build handed to a new agent session | 0 min |
| 16:29 | 1.1 before line counts recorded | 1 min |
| 16:40 | 1.2 spike action listed in the Automate canvas (after a human backoffice sign-in) | 12 min |
| 16:42 | 1.3 automation built in the backoffice; run log shows the bound heading and key | 14 min |
| 16:45 | 1.4 export, delete, import and publish proven; group 1 done | 17 min |
| 16:46 | Human approves S1 (migration also creates the API user) | 18 min |
| 16:48 | 2.1 claim table and query tests | 20 min |
| 16:50 | 2.2 action and nine action tests; 33 tests passing | 22 min |
| 16:54 | 3.1 installer proven on a fresh database and a restart (moved from a migration to a start-up handler) | 26 min |
| 16:57 | 4.1 and 4.2 approach 1 code removed; clean build; history table dropped; 23 tests | 29 min |
| 16:58 | 4.3 README and AGENTS updated | 30 min |
| 17:00 | Human signs in again (fresh database); 3.1 and 4.1 checked in the backoffice; Export compared | 32 min |
| 17:02 | 5.1 first push arrives, but with a `http://localhost/` link | 34 min |
| 17:06 | 5.2 republish sends nothing (`AlreadySent`); Home publish creates no run | 38 min |
| 17:08 | `UmbracoApplicationUrl` set; 5.1 passes with the correct link; 4.3 ticked | 40 min |
| 17:08 to 17:31 | Waiting for the human (click test set-up, backoffice sign-in) | 40 to 63 min |
| 17:33 | 5.3 human clicks the notification and the page opens; 5.4 counts and comparison | 65 min |

## Starting point

- Approach 1 is tagged `approach-1-dashboard`.
- Plan: proposal, delta specs for `announcements` and the new `push-automation`, design D1 to D7,
  and 15 tasks in five groups, starting with a spike.

## 1. Spike

### 1.1 Before line counts (16:29)

Counted with the same rule as [2018-vs-2026-code.md](2018-vs-2026-code.md): non-blank lines,
skipping lines that are only a `//` comment, a `/* ... */` block or an `<!-- -->` comment. The
script reproduces that document's figures exactly, so before and after are comparable.

| Area (extension RCL, approach 1) | Lines |
|---|---|
| Server C# | 854 |
| Backoffice TypeScript, hand-written (dashboard, modal, entry points, manifests) | 395 |
| `umbraco-package.json` manifest | 35 |
| **Backoffice UI total** | **430** |
| Client build config (`vite.config.ts`, `tsconfig.json`, `package.json`, OpenAPI script) | 105 |
| Generated API client (not hand-written) | 1,572 |
| Tests (C#) | 321 |

Note: the tasks say to log in `docs/build-log.md`; approach 2 entries go here instead, as agreed
when the build was handed over.

### 1.2 The spike action (16:33 to 16:40)

**Done:** `Umbraco.Automate.Core` 17.6.0 added to `Directory.Packages.props` and the extension
project. `Automation/SendPushNotificationAction.cs` with its settings (`Message`, `ContentKey`,
both `[Field(SupportsBindings = true)]`) and output (`Attempted`, `Succeeded`, `Failed`,
`Removed`, `AlreadySent`) classes. For the spike it only writes its two settings to the step log.

**How we checked the API:** not from the public docs (they track Automate 18 and 19). The
installed 17.6.0 `Umbraco.Automate.Core.dll` was decompiled with `ilspycmd`. That gave the exact
signatures and the source of the built-in Get Content action, a good model for our own.
Findings that match the design: `ActionBase<TSettings, TOutput>` with an abstract
`ExecuteAsync(ActionContext, CancellationToken)`; `[Action(alias, name)]` plus optional
`Description`, `Group` and `Icon`; `[Field]` is a short alias for `EditableModelFieldAttribute`;
`context.GetSettings<T>()` returns settings with bindings already resolved. Umbraco's type
scanning finds actions (they implement `IDiscoverable`), so no composer registration is needed.

**Verified:** after a restart, "Send push notification" appears in the canvas's action picker in
a "Notifications" group ([screenshot](screenshots/11-automate-action-listed.jpg)).

**Process note:** the backoffice needs a login, and the agent cannot read the admin password from
user secrets (the repo's agent settings deny it). The human signed in once in the browser pane;
the session survived the later site restarts.

### 1.3 Build the automation in the backoffice (16:35 to 16:42)

**Surprise: a workspace needs an API user.** The "Create workspace" form requires a service
account (an Umbraco API user) and at least one user group. The decompiled code explains why:
built-in actions that touch content, Get Content included, authorise against the workspace's
service account, and fail with "No backoffice identity available" without one. The design said
the migration would create or reuse a workspace named Site; it now also has to create or reuse
an API user. A small addition, not a design change: decision S1 below.

**Done, all in the backoffice:**
- API user "Automate Site" (`automate-site@example.com`) in the Writers group. Writers is the
  least-privileged built-in group that can read content. The automation never writes content
  through Umbraco, so it does not need Editors.
- Workspace "Site" (alias `site`): service account "Automate Site", user group Administrators.
- Automation "Announce new pages"; the alias `announce-new-pages` came from the name
  automatically. Trigger: Content Published, Content Types = Announcement. Step 1: Get Content,
  Content Key bound to `${ trigger.contentKey }`. Step 2, on Get Content's "Found" outcome: Send
  push notification, Message = `${ steps.getContent.properties.heading }`, Content key =
  `${ trigger.contentKey }`. Saved and published.

**How binding works in the canvas:** fields with a picker (Get Content's Content Key) have a
"Use a binding expression" toggle and an "Insert binding" list. Our plain text fields take the
`${ ... }` expression typed straight in. The path syntax (`steps.<step alias>.<output>`) was
confirmed in the decompiled `BindingEvaluator`.

**Verified:** published page "Spike gala" (heading "Spike: gala entries close Friday"). About 10
seconds later the run completed, and the push step's log read: `Spike: message 'Spike: gala
entries close Friday', content key '6a51404e-f35f-427b-84c5-87ab7d251309'`
([screenshot](screenshots/12-spike-run-log.jpg)). So binding to a Get Content property works,
and Get Content read the newly published heading.

### 1.4 Export, delete, re-import (16:42 to 16:45)

**Done:** a throwaway, development-only endpoint in the web project called
`ExportAutomationAsync` and wrote `src/NotificationsRevisit.Web/Automations/announce-new-pages.json`
with `System.Text.Json` web defaults (camelCase, indented, enums as names). It stood in for the
planned scratch test: a unit test cannot reach the running site's Automate services and database.
The automation was then deleted in the backoffice (actions menu, Delete). A second call read the
file back, looked up the workspace by alias `site`, and called `ImportAutomationAsync` then
`PublishAutomationAsync`. The endpoint was deleted afterwards.

**What the import parameters turned out to be** (from the 17.6.0 code and XML docs):
`ImportAutomationAsync(exportModel, workspaceId, existingAutomationId = null, userId = null)`.
- `workspaceId` is required and must exist (it is validated). The export file holds **no
  workspace at all**: the importer chooses where it goes, and the import never creates one.
- `existingAutomationId = null` creates a new automation as **Draft**, keeping the file's ID.
  The import fails if that ID or alias already exists. Passing an ID instead overwrites that
  automation in place, keeping its status and workspace.
- `userId` is only recorded as the author.
- There is no separate "enabled" flag in 17.6: `Status` is Draft, Published or Unpublished, and
  `PublishAutomationAsync` sets Published.
- The trigger's content type filter is stored as the Announcement document type's key
  (`5e9a3b71-...-1a01`). The site content migration creates that type with a fixed key, so the
  file works on a fresh clone unedited.

**Verified:** the import kept the original ID (`f3049e9d-...`), landed in workspace `site` and was
Published. Publishing a second page ("Spike reimport") produced a completed run whose push step
logged `Spike: message 'Spike: imported automation works', content key
'e8192d70-9320-43b1-bf78-18ae2fc05aa8'` ([screenshot](screenshots/13-spike-reimported-run-log.png)).

**Surprise:** deleting an automation also deletes its run history. Harmless here, but worth
knowing before a "delete and re-import" on a real site.

### Spike decisions

- **S1. The migration also ensures an API user.** On a fresh database it creates an API user
  ("Automate Site", `automate-site@example.com`, Writers) if none exists, then the workspace
  `site` with that service account and the Administrators group, then imports and publishes.
  `IUserService.CreateAsync` with `UserKind.Api` exists in Umbraco 17.8.2. Rejected: a workspace
  with no service account (the API allows it, but Get Content then fails every run), and the
  admin as service account (Automate refuses: it must be an API user).
- **S2. The scratch test became a scratch endpoint**, for the reason above. Same purpose, deleted.
- **S3. We write the JSON ourselves rather than downloading it from the backoffice.** It is the
  same model (`AutomationExportModel`) the backoffice's Export menu returns from the same service
  method. Our reader accepts camelCase and enum names or numbers, so a file re-exported from the
  backoffice can be dropped in.

The human confirmed S1 ("carry on") at 16:46.

## 2. The Send push notification action

### 2.1 The claim table (16:46 to 16:48)

**Done:** `PushedContentDto` (table `announcementsPushedContent`: `ContentKey` primary key,
`SentUtc`), `PushedContentRepository` (`TryClaimAsync`, `ReleaseAsync`) with its SQL in a static
`PushedContentQueries` class like approach 1's repositories, and a new step
`AddPushedContentTable` in `AnnouncementsMigrationPlan`.

**Decisions:**
- The key is stored as a string (`"D"` format), not a `Guid` column. SQLite has no GUID type and
  `Microsoft.Data.Sqlite` binds `Guid` parameters as blobs, while Umbraco's own SQLite set-up maps
  them to text. A string compares the same way everywhere.
- The claim is one conditional insert
  (`INSERT ... SELECT ... WHERE NOT EXISTS (...)`), and the primary key catches the rare race where
  two runs pass the check together. On a database error the code checks whether the key now
  exists: if so it reports "already claimed", otherwise it rethrows. Rejected: relying only on
  catching a primary key violation, because SQLite and SQL Server report it differently, and
  catching every database error would hide real faults.

**Verified:** four query tests against in-memory SQLite: first claim succeeds, a **second claim
for the same key reports already claimed**, different keys are independent, a released key can be
claimed again. 24 tests passing.

### 2.2 The action (16:48 to 16:50)

**Done:** `Umbraco.Automate.Testing` 17.6.0 added to the test project. Its `ActionTestHarness`
(checked by decompiling) builds the action with `ActivatorUtilities`, so tests supply fakes with
`WithService<T>()`, set settings with `WithSettings()`, and get the `ActionResult` back.
Exceptions are not caught by the harness. The step log is not exposed, so tests assert on
outputs and results.

The action, as design D2 and D3: validate settings, resolve the absolute URL, claim, send, release
the claim if the send throws, return outputs and write a log line. Failures use
`ActionResult.Failed(..., StepRunErrorCategory.Validation)` with a message naming the setting.

**Decisions:**
- **URL resolution sits behind `IContentUrlResolver`.** Automate runs steps on a background
  scheduler with no HTTP request, so the resolver calls `IUmbracoContextFactory.EnsureUmbracoContext()`
  and then `IPublishedUrlProvider.GetUrl(key, UrlMode.Absolute)`, the same call approach 1 made
  inside a request. Built-in Get Content does the same. The interface also makes the "no public URL" case
  testable. So the action depends on `IPushService` (unchanged from approach 1), the claim
  repository and the resolver.
- **The message is sent as bound.** Approach 1 prefixed the date ("23 Nov ..."); a generic action
  should not. The automation binds the heading, which still meets push-delivery's "a message that
  includes the heading". Anyone who wants a date can add it in the binding.
- A cancelled send releases the claim and rethrows, rather than reporting a failure.

**Verified:** nine action tests: first send (message, URL, claim, and outputs 4/2/1/1 from the
spec's example), already sent (success, `AlreadySent`, no send), missing message (null, empty,
spaces), missing or invalid content key, no public URL (nothing claimed or sent), and **claim
released when the send throws**. 33 tests passing.

## 3. Install the automation with the site

### 3.1 Install on a fresh database (16:50 to 16:54)

**Surprise: a package migration cannot import the automation on a fresh database.** The plan was
a new step in `SiteContentMigrationPlan`. The 17.6.0 code shows why that fails on a fresh clone:
Automate creates its own schema (11 EF Core migrations, separate SQLite file) during component
initialisation, and Umbraco's unattended install runs package migrations *before* that.
Automate's own comments describe this ordering, and its schema initialiser refuses to run until
the runtime level is Run. On this already-migrated database a migration step would have appeared
to work, so only a fresh-database test would catch it. The fresh-database log confirmed the order:
`Migration plans run: UmbracoAutomate, NotificationsRevisit.Announcements, NotificationsRevisit.Site`,
then `Running 11 pending Automate migrations`.

**Adjustment (same behaviour, different hook):** `AnnounceAutomationStartedHandler` in
`src/NotificationsRevisit.Web/Automations/`, registered by a new `SiteComposer`, handles
`UmbracoApplicationStartedNotification`. When the runtime level is Run and a key-value marker is
absent, it:
1. finds or creates the API user "Automate Site" (`automate-site@example.com`, fixed key, Writers)
   with `IUserService.CreateAsync` and `UserKind.Api` (decision S1);
2. finds or creates the workspace `site` with that service account and the Administrators group;
3. imports `announce-new-pages.json` and publishes it, if no automation has that alias;
4. sets the marker.

The marker keeps the migration's run-once behaviour: an automation an editor deletes on purpose
is not brought back at the next start. Rejected: checking only the alias on every start, which
would resurrect deleted automations; and calling Automate's schema initialiser from the migration,
which skips itself until the runtime level is Run.

**Verified, fresh SQLite database** (the spike databases were moved aside, not deleted): the site
installed unattended, then logged `Created API user automate-site@example.com`, `Created Automate
workspace site`, `Imported and published automation announce-new-pages`, with no errors. The
`announcementsPushedContent` table was created with its primary key. **Restart:** none of those
lines appeared again, so there are no duplicates. The backoffice check (automation listed,
published, in "Site") waits for the next human sign-in, because the fresh database invalidated
the session.

## 4. Remove approach 1's orchestration and UI

### 4.1 Client code (16:54 to 16:56)

**Done:** nothing in the extension's `Client/` was still needed. The entry point only wired the
generated API client into the backoffice's auth, for the dashboard. So the whole folder went, as
design D1 preferred: dashboard, modal, generated client, manifests, Vite and TypeScript config,
`package.json`. Also removed: the extension's build import of `ClientBuild.targets` (the site
still uses it), its `wwwroot` output, the `.gitignore` entry for that output, and the template's
`README.txt` (Node set-up for the client). **Approach 2 has no backoffice UI code of its own.**

**Verified:** a clean copy of the working tree (tracked and new files only, no `bin`, `obj` or
`node_modules`) builds with 0 errors; npm ran only for the site's scripts. The old dashboard
bundle URL now returns 404. Not yet ticked: the backoffice check that no Announcements dashboard
shows, which waits for the sign-in.

### 4.2 Server code (16:55 to 16:57)

**Removed:** `AnnouncementsController`, `AnnouncementsApiComposer`, `Constants` (the Swagger
document name), `AnnouncementService`, `AnnouncementPages`, `ContentModelOptions`,
`AnnouncementsAuthorization`, `AnnouncementHistoryRepository`, `AnnouncementHistoryDto`, their
registrations, nine service tests and one history test, and `Announcements:ContentModel` from
`appsettings.json`. The extension also no longer needs the `Umbraco.Cms.Api.Common` and
`Umbraco.Cms.Api.Management` packages (they were for the backoffice API); it builds without them.

**Migration:** a new step `DropAnnouncementsHistoryTable` drops `announcementsHistory` by name.
Approach 1's first step (`CreateAnnouncementsTables`) referenced the history DTO, so it now creates
only the subscriptions table. Editing an applied step is normally a smell, but here it only
changes fresh databases, which no longer create a table just to drop it. Existing databases
already recorded that step and get the drop.

**Verified:** `dotnet test` passes (23 tests: 33, minus nine service tests and one history test).
After a start on the database from 3.1, the log shows `DROP TABLE "announcementsHistory"`.

### 4.3 Docs (16:57 to 16:58)

`README.md`: what approach 2 is, how announcements are sent, where the automation lives, what
the first start installs, and how to re-export after changing it in the backoffice. `AGENTS.md`:
what the repo is now, how approach 2 fits together, an updated layout, and Automate gotchas found
here (service account, background execution, decompile to check the API). Handler named
`AnnounceAutomationStartedHandler` to follow the repo's `<Thing><Event>Handler` convention.
Ticked after 5.1, once the documented steps had been checked against the running site.

### Backoffice checks after the human signed in again (16:59 to 17:01)

- **3.1 ticked:** the fresh database's Automation section shows 1 published, 0 drafts, and "Announce
  new pages" in workspace **Site** with its three steps
  ([screenshot](screenshots/14-fresh-db-automation-installed.jpg)).
- **4.1 ticked:** the Content section's dashboards are "Welcome to Umbraco" and "Redirect URL
  Management" only ([screenshot](screenshots/15-content-no-dashboard.jpg)).
- **Export compared:** clicking **Export** in the backoffice (with the human's OK) called
  `GET /umbraco/automate/management/api/v1/automations/{id}/export`. The response is the same model
  as our committed file: camelCase, enums as names, same IDs, bindings and trigger filter. So the
  README's "export over the file" step works as written. **Import** is on the Site workspace's
  actions menu; the README now says so.

## 5. End-to-end verification

### 5.1 and 5.2 in Playwright (17:01 to 17:08)

Playwright's Chromium was granted notification permission with
`context.grantPermissions(['notifications'], { origin })` and subscribed through the site's opt-in
(a real FCM subscription). Pages were published in the backoffice (browser pane), and
`registration.getNotifications()` was polled in Playwright.

**First run: the notification arrived, but its link was wrong.** "Gala entries" (heading "Gala
entries close Friday") was published at 17:02:05. By 17:02:11 the notification was there, with
title "Swim club announcement" and the heading as its text. But its URL was
`http://localhost/announcements/gala-entries/`: wrong scheme, no port.

**Cause, from the 17.8.2 code:** outside a request, `UmbracoContext.OriginalRequestUrl` falls back
to `IHostingEnvironment.ApplicationMainUrl`, and to `http://localhost` when that is not set. It
was not set here: no `UmbracoApplicationUrl` was configured, and it was not detected from requests.
Approach 1 never met this, because it resolved the URL inside the editor's request. **Fix:**
`Umbraco:CMS:WebRouting:UmbracoApplicationUrl` = `https://localhost:44371/` in
`appsettings.Development.json`. Umbraco confirmed it at start ("Application URL configured as
https://localhost:44371/"). The README now says every environment needs it. Rejected: building
the URL ourselves from a request host (the action has no request either), and a site setting of
our own (Umbraco already has one).

**5.1, after the fix:** "Pool closed Saturday" published at 17:07:50. The notification was
present at 17:07:53, with title "Swim club announcement", text "Pool closed on Saturday for
maintenance" and URL `https://localhost:44371/announcements/pool-closed-saturday/`. Run log:
`Sent 'Pool closed on Saturday for maintenance' linking to
https://localhost:44371/announcements/pool-closed-saturday/: 1 attempted, 1 succeeded, 0 failed,
0 expired subscriptions removed` ([screenshot](screenshots/18-run-log-delivery.png); step outputs
[here](screenshots/16-run-output-first-send.jpg)).

**5.2:** "Gala entries" was republished with the heading changed to "Gala entries close Friday at
6pm" at 17:04:33. Twenty seconds later Playwright still had exactly one notification, with the
original heading. The run's push step output: Attempted 0, Already sent **true**
([screenshot](screenshots/17-run-output-already-sent.jpg)). Home was then published at 17:06:19.
After 20 seconds the run list still had only the two runs, so the trigger's Announcement filter
works.

Also confirmed: Get Content read the newly published heading on first publish, so the design's
"stale version" risk did not happen.

### 5.3 The human clicks the notification (17:31 to 17:33)

The human subscribed in their own browser (Playwright on Windows drops clicks on OS
notifications, as in approach 1). After another backoffice sign-in (the session had lapsed),
"Click test" was published at 17:31:52. Run log: `2 attempted, 2 succeeded, 0 failed`. The human
clicked the Windows notification, and it **opened the announcement page**.

### 5.4 After counts and the comparison (17:33)

Same counting rule as 1.1. `dotnet test`: 23 passing.

| Area | Approach 1 | Approach 2 |
|---|---|---|
| Extension C# (push stack, subscribe endpoint, persistence, migrations, and the approach's own code) | 854 | 642 |
| Site C# for the approach (installer handler and composer) | 0 | 133 |
| Automation definition (JSON exported by Automate, not hand-written) | 0 | 82 |
| Backoffice UI (TypeScript and manifest) | 430 | **0** |
| Backoffice client build config | 105 | **0** |
| Generated API client | 1,572 | **0** |
| Tests | 321 | 341 |
| NuGet packages in the extension | 5 | 4 (`Umbraco.Automate.Core` in; the two Management API packages out) |

**What each approach needs beyond the shared push stack** (the push service, sender, options,
subscribe endpoint, rate limiting and subscription repository are unchanged):

| | Approach 1 | Approach 2 |
|---|---|---|
| Orchestration C# | 435 (controller, API composer, service, page creation, content model options, authorisation, history) | 365 (action 118, claim table and migrations 114, installer 133) |
| Backoffice UI and its build | 535 hand-written + 1,572 generated | none |
| **Hand-written total** | **about 970** | **about 365, plus an 82-line exported JSON** |

So approach 2 needs roughly **60% less hand-written code** for the same job, and no backoffice
front-end or Node build at all. A third of what is left (the installer) exists only so a fresh
clone works without clicking. A real site would build the automation once in the backoffice and
could drop it.

**Editor experience.**
- Approach 1: a dedicated Announcements dashboard with a sidebar form, a subscriber count, a
  history table, and an immediate "sent to N, failed M" result. One purpose-built screen.
- Approach 2: editors create and publish an Announcement page in the normal Content tree, like
  any page. Nothing new to learn. The result is not shown to the editor; it is in the
  automation's Runs tab (Automation section), with each step's inputs, outputs and logs.
- Who may send: approach 1 needed its own authorisation policy (no translators). In approach 2
  it is whoever may publish Announcement pages, under Umbraco's own permissions.
- Republishing to fix a typo is safe in approach 2 (the claim table). Approach 1 created and sent
  in one step, so it had no republish case.

**Latency.** Approach 1 sent inside the editor's request: the result was on screen when the
request returned, but the editor waited for every delivery. In approach 2, publish returns at
once, and the push followed 3 to 6 seconds later in our runs. Automate's scheduler polls about
every 10 seconds, so up to about 10 seconds is expected.

**What was lost.** The subscriber count, the editor-facing history table and the immediate send
result. Automate's run history replaces them, for administrators only. The date prefix on the
message ("23 Nov ...") went because the action is generic; it could come back in the binding.

**What was gained.** Orchestration that is visible and editable in the backoffice without a
deployment; a reusable action any automation can call (session reminders next); background
sending; run history with replay; no backoffice front-end to maintain.

**New things to know** (each cost time here): workspaces need an API user as service account;
package migrations run before Automate's schema exists; background steps need
`UmbracoApplicationUrl` for absolute URLs; an exported automation does not carry its workspace;
deleting an automation deletes its run history.
