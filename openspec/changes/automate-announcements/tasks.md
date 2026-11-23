# Tasks

## 1. Spike: prove the Automate pieces before removing anything

- [ ] 1.1 Record "before" line counts for the RCL (C#, hand-written TypeScript, manifest) in `docs/build-log.md` using the same counting rule as `docs/2018-vs-2026-code.md`; verify the numbers are in the log
- [ ] 1.2 Add `Umbraco.Automate.Core` 17.6.0 to the RCL through `Directory.Packages.props` and a minimal `SendPushNotificationAction` that only logs its bound settings; verify it appears in the Automate canvas after a restart
- [ ] 1.3 In the backoffice, create workspace "Site" and automation `announce-new-pages` (Content Published filtered to Announcement, Get Content, the spike action), publish it, publish an Announcement page; verify the run log shows the bound heading and content key
- [ ] 1.4 Export the automation to `src/NotificationsRevisit.Web/Automations/announce-new-pages.json`, delete it in the backoffice, re-import it through `IAutomationService.ImportAutomationAsync` from a scratch test, and publish it; verify it runs again, and record what the import parameters and workspace handling turned out to be in `docs/build-log.md`

## 2. The Send push notification action

- [ ] 2.1 Add the claim table `announcementsPushedContent` as a new step in `AnnouncementsMigrationPlan`, with a repository (claim, release); verify with SQLite query tests that a second claim for the same key reports already claimed
- [ ] 2.2 Add `Umbraco.Automate.Testing` 17.6.0 to the test project and implement the action per design D2 and D3 (validate, resolve URL, claim, send, release on exception, outputs and log); verify action tests cover first send, already sent, missing settings, no public URL, and claim released when the send throws

## 3. Install the automation with the site

- [ ] 3.1 Add a step to `SiteContentMigrationPlan` that imports `Automations/announce-new-pages.json` when no automation with that alias exists, ensures the "Site" workspace, and publishes it; verify on a fresh SQLite database that the automation exists and is published, and that a restart creates no duplicate

## 4. Remove approach 1's orchestration and UI

- [ ] 4.1 Remove the dashboard, modal, generated API client and dashboard manifest entry from the RCL's `Client/`, or the whole `Client/` and its build import if nothing is left (design D1); verify `dotnet build` passes from a clean clone and the backoffice shows no Announcements dashboard
- [ ] 4.2 Remove `AnnouncementsController`, `AnnouncementsApiComposer`, `AnnouncementService`, `AnnouncementPages`, `ContentModelOptions`, `AnnouncementsAuthorization`, the history repository and DTO, their registrations, tests and `Announcements:ContentModel` settings; add the migration step that drops `announcementsHistory`; verify `dotnet test` passes and the history table is gone after start
- [ ] 4.3 Update `README.md` and `AGENTS.md` for approach 2 (how announcements are sent, where the automation lives, how to re-export it after changing it in the backoffice); verify the documented steps match the running site

## 5. End-to-end verification

- [ ] 5.1 In Playwright, subscribe on the site, then publish a new Announcement page in the backoffice; verify `registration.getNotifications()` shows the configured title and the heading within about 20 seconds, and the run log shows the delivery outputs
- [ ] 5.2 Republish the same page with an edited heading; verify no new notification arrives and the run log shows `AlreadySent`; publish the Home page and verify no automation run is created
- [ ] 5.3 Ask the human to click the notification; verify with them that it opens or focuses the announcement page
- [ ] 5.4 Record "after" line counts, the approach 1 versus approach 2 comparison (code removed, editor experience, latency, what was lost), timestamps and screenshots in `docs/build-log.md`; verify `openspec validate automate-announcements --strict` passes

## Workflow follow-up

- Update the `announcements` main spec's Purpose on archive (it still describes the dashboard).
- Tag `approach-2-automate` once committed, then archive the change after review.
- Approach 3 (OneSignal) gets its own change.
