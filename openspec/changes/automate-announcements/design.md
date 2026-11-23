# Design

## Context

- See `proposal.md` (Why) and the delta specs. The main specs describe approach 1, archived as
  `2026-11-23-rebuild-announcements-dashboard`; approach 1's code is tagged `approach-1-dashboard`.
- Umbraco Automate 17.6.0 is installed. Checked against the installed `Umbraco.Automate.Core`
  assembly (not the public docs, which track 18 and 19):
  - `ContentPublishedTrigger` with a `ContentTypes` filter; output is content key, name, content
    type key and alias, cultures. No URL, no property values.
  - Built-in `GetContentAction` reads the published page's properties.
  - Custom actions: `ActionBase<TSettings, TOutput>`, `[Action(alias, name)]`, `ExecuteAsync(ActionContext, CancellationToken)`,
    `context.GetSettings<T>()`, `Success(output)`, `context.LogInfo(...)`; settings fields use `[Field]`.
  - `IAutomationService` has `ExportAutomationAsync`, `ImportAutomationAsync`, `GetAutomationByAliasAsync`
    and `PublishAutomationAsync`.
  - `Umbraco.Automate.Testing` 17.6.0 exists on NuGet.
- The built-in HTTP Request action only calls public destinations, so it cannot reach our own
  push service locally. A custom action is needed for approach 2 (approach 3 may not need one).

## Goals / Non-Goals

**Goals:**
- A pure alternative to approach 1: the diff between the two tags shows what Automate replaces.
- The push stack, subscriptions and site front end stay byte-for-byte where possible, so the
  comparison isolates orchestration.
- A fresh clone works end to end with no manual Automate set-up.
- A generic action (bindable message, any content key) that later automations can reuse, such as
  session reminders.

**Non-Goals:**
- Replacing the subscriber count or delivery table with a custom view.
- Targeting a subset of subscribers (needed for session reminders; a later change).
- Approach 3 (OneSignal).

## Decisions

### D1. Remove approach 1's orchestration and UI; keep the push stack

| Removed | Kept unchanged |
|---|---|
| `Client/src/dashboard`, `Client/src/modal`, the dashboard entry in `umbraco-package.json` | `IPushService`, `PushService`, `WebPushSender`, `PushOptions` |
| `AnnouncementsController`, `AnnouncementsApiComposer`, `Constants.ApiName` | `PushSubscriptionsController`, rate limiting, subscription repository and table |
| `AnnouncementService`, `AnnouncementPages`, `ContentModelOptions` | Site `push.ts`, `sw.ts`, layout, content model migration |
| `AnnouncementsAuthorization` | `build/ClientBuild.targets` |
| `AnnouncementHistoryRepository`, `AnnouncementHistoryDto`, their tests | `PushService` and subscription repository tests |

- The RCL's `Client/` keeps only what the backoffice still needs. If nothing remains, the RCL's
  client build is removed too, and that shrinkage is itself an article point. Decide at
  implementation by what is left; prefer removing the whole `Client/` folder if possible.
- 2018 comparison: approach 2 has no backoffice UI code of its own at all.

### D2. The "Send push notification" action

```csharp
[Action("notificationsRevisit.sendPushNotification", "Send push notification")]
public class SendPushNotificationAction : ActionBase<SendPushNotificationSettings, SendPushNotificationOutput>
```

- **Settings** (`[Field(SupportsBindings = true)]`): `Message` (required) and `ContentKey`
  (required). The automation binds `Message` to the Get Content step's heading and `ContentKey`
  to the trigger's content key.
- **Output:** `Attempted`, `Succeeded`, `Failed`, `Removed`, `AlreadySent`. Also written to the
  step log with `context.LogInfo`.
- **Behaviour:** validate settings, resolve the absolute URL with `IPublishedUrlProvider`
  (fail the step if none), claim the content key (D3), call `IPushService.SendAsync`, return the
  outcome.
- The action depends on `IPushService` only, so the push code is shared with approach 1 unchanged.
- Alternative rejected: an action that reads the heading itself from a hard-coded property alias.
  Binding keeps it generic and keeps content knowledge in the automation, where editors can see it.

### D3. Once per page: claim, then send

- New table `announcementsPushedContent` (ContentKey as primary key, SentUtc), added as a new step
  in the existing `AnnouncementsMigrationPlan`, which also drops `announcementsHistory`.
- The action **inserts the claim first**. A primary key conflict means it was already sent: log
  "already sent", return success with `AlreadySent = true`, send nothing. If the send then throws,
  the claim is deleted so a later publish can retry.
- Claim-then-send avoids a race if two runs for the same page overlap (for example a publish and
  an ancestor republish close together).
- Alternatives rejected: check-then-insert (racy); using Automate's run history as the record
  (no supported query for "has this content been pushed").

### D4. The automation lives in the repo

- Alias `announce-new-pages`, name "Announce new pages":
  Content Published (`ContentTypes` = Announcement) -> Get Content (trigger content key) ->
  Send push notification (`Message` = heading, `ContentKey` = trigger content key).
- Built once in the backoffice, exported with `ExportAutomationAsync` (or the backoffice export),
  committed as `src/NotificationsRevisit.Web/Automations/announce-new-pages.json`.
- A new step in the site's `SiteContentMigrationPlan` reads that file and, if
  `GetAutomationByAliasAsync("announce-new-pages")` returns nothing, imports it with
  `ImportAutomationAsync` and publishes it with `PublishAutomationAsync`. Draft automations do
  not respond to events, so publishing is required.
- Automate groups automations in workspaces; the import needs a workspace. The migration creates
  or reuses one named "Site" (see Open Questions).
- 2018 and approach 1 comparison: the orchestration moves from C# into a reviewable JSON
  definition that editors can also see and change in the backoffice.

### D5. Background execution

- No configuration change: Automate's default execution mode runs workflows on its scheduler,
  polling every 10 seconds. Expect up to roughly 10 seconds between publish and push. That is fine
  for announcements; it is noted in the article as the trade-off for not blocking the editor.

### D6. Tests

- Action tests with `Umbraco.Automate.Testing` 17.6.0 (`ActionTestHarness`): first send records
  and sends; repeat returns `AlreadySent` without sending; missing settings fail; no public URL
  fails; a throwing send releases the claim.
- Repository query tests for the claim table against in-memory SQLite, like approach 1.
- Remove the announcement service and history tests with their code.

### D7. Verification

- Fresh database: start the site, confirm the automation exists, is published and is in the
  "Site" workspace.
- Playwright end to end (as in approach 1): subscribe, publish a new Announcement page in the
  backoffice, assert `registration.getNotifications()` within about 20 seconds, check the run log
  outputs. Republish the same page: no new notification, run log says already sent. Publish the
  Home page: no automation run.
- The human checks the notification click once, as before (Playwright cannot click OS toasts).
- Record before and after line counts for the article, then tag `approach-2-automate`.

## Risks / Trade-offs

- [Automate packages are pre-1.0 and the docs track newer versions] -> Every API above was checked
  in the 17.6.0 assembly; a spike (task group 1) proves export, import and the action before the
  removals start.
- [The import API's parameters are not documented] -> Its signature takes the export model, a
  workspace key and two optional keys; confirm their meaning in the spike.
- [Content Published also fires when an ancestor republish republishes descendants] -> The
  claim table makes repeat sends harmless.
- [Get Content might read a stale version if the trigger fires before the publish commits] ->
  Verified in the end-to-end run by changing the heading on first publish.
- [Up to ~10 seconds' delay] -> Accepted; documented.
- [Losing the dashboard's subscriber count] -> Accepted for the comparison; noted in the article.

## Migration Plan

- Database: the announcements migration plan gains a step that drops `announcementsHistory` and
  creates `announcementsPushedContent`. Existing subscriptions are untouched.
- Rollback: check out tag `approach-1-dashboard` on a fresh database.

## Open Questions

- How the automation export represents the workspace and whether `ImportAutomationAsync` creates
  one: confirm in the spike. The approach (commit JSON, import in a migration) does not change.
