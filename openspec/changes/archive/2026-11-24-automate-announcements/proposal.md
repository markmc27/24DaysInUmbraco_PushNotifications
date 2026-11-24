# Proposal

## Why

Approach 1 (tag `approach-1-dashboard`) rebuilt the 2018 custom dashboard: our own UI, API and
orchestration, sending every push inside the editor's request. The article explores Umbraco
Automate as an **alternative** answer to the same problem: the editor publishes an announcement
page, and an automation sends the push in the background. This shows how much custom code
Automate replaces, and opens the door to other triggers (for example session reminders from the
club's booking system) without new orchestration code.

## What Changes

- **BREAKING (for editors):** the Announcements dashboard, its sidebar, the backoffice API
  controller, the announcement service and the history table are removed. Editors create and
  publish an Announcement page in the normal Content tree instead.
- New custom Automate action, "Send push notification", in the extension library. It wraps the
  existing push service, takes a bindable message and a content key, resolves the page URL, sends
  to every subscriber, and reports attempted, succeeded, failed and removed counts in the run log.
- The action pushes a page only once: it records which pages it has sent and skips repeats,
  because Automate's Content Published trigger fires on every publish.
- New automation, "Announce new pages": Content Published (filtered to the Announcement type),
  then Get Content, then Send push notification. Exported as JSON into the repo and imported by a
  site migration, so a fresh clone works without backoffice set-up.
- Unchanged: the push stack, the public subscribe endpoint and table, the site opt-in and the
  service worker. Visitors see no difference.

**Out of scope:** approach 3 (OneSignal), session reminders, an editor "send notification"
tickbox, and a replacement subscriber count view. Approach 2 is tagged `approach-2-automate` when
done.

## Capabilities

### New Capabilities

- `push-automation`: the reusable "Send push notification" Automate action: its settings,
  outputs, run log behaviour and once-per-page rule.

### Modified Capabilities

- `announcements`: the dashboard, compose, history and configured content model requirements are
  removed; publishing an Announcement page sends a push once, through an automation installed with
  the site.

## Impact

- **Extension library:** remove `Client/` dashboard and modal code, `AnnouncementsController`,
  `AnnouncementsApiComposer`, `AnnouncementService`, `AnnouncementPages`, `ContentModelOptions`,
  the authorisation policy, the history repository and DTO. Add the Automate action, a small "sent
  pages" table and its migration step.
- **Packages:** the library references `Umbraco.Automate.Core` 17.6.0 (already installed in the
  site through `Umbraco.Automate`).
- **Web project:** a migration that imports the committed automation JSON; `appsettings`
  loses the `Announcements:ContentModel` section.
- **Database:** history table dropped by a new migration step; a "sent pages" table added.
- **Tests:** announcement service and history tests removed; action tests added (Automate provides
  `ActionTestHarness`).
