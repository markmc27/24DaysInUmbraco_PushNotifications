# Tasks

## 1. Scaffold the extension library

- [x] 1.1 Create `src/NotificationsRevisit.Announcements` with `dotnet new umbraco-extension --include-example --site-domain https://localhost:44371`, add it to `NotificationsRevisit.slnx` and reference it from the web project; verify `dotnet build NotificationsRevisit.slnx` succeeds
- [x] 1.2 Align Umbraco package versions to 17.8.2 in `Directory.Packages.props` (no versions in the RCL `.csproj`); verify `dotnet list package` shows 17.8.2 for every Umbraco package
- [x] 1.3 Install the client dependencies, build the example and run the site; verify the example dashboard appears in the backoffice (proves the Vite build, static web assets and manifest wiring)
- [x] 1.4 Create `tests/NotificationsRevisit.Announcements.Tests` (xUnit) referencing the RCL, added to the solution; verify `dotnet test` runs with one placeholder test passing
- [x] 1.5 Record the scaffolding steps, what the template generated and any surprises in `docs/build-log.md`; verify the entry exists

## 2. Persistence

- [x] 2.1 Add the subscription and history POCOs and a package migration plan creating `announcementsPushSubscription` (unique Endpoint) and `announcementsHistory`; verify on a fresh SQLite database that both tables exist after start and the migration shows in the Packages section
- [x] 2.2 Implement the subscription repository (upsert by endpoint, list, count, delete by endpoints) and the history repository (insert, list newest first) using scopes; verify with unit or integration tests that resubmitting an endpoint leaves one row with the latest keys

## 3. Push delivery

- [x] 3.1 Add the `WebPush` package centrally and the `Announcements:Push` options (title, icon, max concurrency, VAPID subject and keys) with validation on start; verify the site refuses to start with a clear error when a VAPID setting is missing
- [x] 3.2 Generate VAPID keys with `npx web-push generate-vapid-keys`, store the private key in user secrets and the public key and subject in `appsettings.Development.json`; verify `git grep` finds no private key in the repo
- [x] 3.3 Implement `IPushService` with bounded concurrency, 404/410 removal after the loop, logged failures and a `PushSendResult`; verify unit tests with a fake sender cover all-success, mixed outcome (4 attempted, 2 ok, 1 failed, 1 removed) and no subscribers
- [x] 3.4 Document VAPID key generation and the push settings in the repo README; verify the documented command runs as written

## 4. Public subscribe endpoint

- [x] 4.1 Implement `POST /api/announcements/push-subscriptions` with request validation (required fields, endpoint length) and antiforgery; verify requests without a token or with missing fields are rejected and store nothing
- [x] 4.2 Register a per-IP fixed window rate limiting policy and the middleware through an Umbraco pipeline filter in a composer, leaving `Program.cs` unchanged; verify the sixth call within the window returns 429 with the default limit of 5

## 5. Content model and site front end

- [x] 5.1 Add the web project package migration creating Home, Announcements and Announcement doc types, templates and the Home and Announcements nodes; verify on a fresh database that the nodes exist and Home renders
- [x] 5.2 Add the `Announcements:ContentModel` options with startup validation and configure them in `appsettings.json`; verify the site refuses to start when an alias is missing
- [x] 5.3 Scaffold `src/NotificationsRevisit.Web/Client/` (Vite, TypeScript, entry points `push.ts` and `sw.ts` with fixed output names `wwwroot/js/push.js` and `wwwroot/sw.js`, WebWorker types for the service worker), gitignore the outputs, and add the web `.csproj` target that runs `npm ci` when needed and `npm run build` before build; verify `dotnet build` from a clean clone produces both files and `git status` does not list them
- [x] 5.4 Add the shared layout (antiforgery token, VAPID public key, opt-in control), the Home and Announcement views, and implement `Client/src/push.ts` and `Client/src/sw.ts`; verify in the browser that no permission prompt appears on load, opting in stores one subscription, and the control then shows the enabled state
- [x] 5.5 Verify the opt-in is hidden in an insecure context and after subscribing on reload, and record the results in `docs/build-log.md`

## 6. Announcements API

- [x] 6.1 Add the custom authorisation policy (Content section access and not in Translators) and the Management API controller in its own Swagger document with `GET subscriptions/count` and `GET history`; verify both endpoints appear in the Swagger UI and return data for an admin
- [x] 6.2 Implement the announcement service and `POST announcements` following design D5 (validate, find parent, create, publish, resolve URL, send, record history); verify unit tests cover page naming and that a failed publish sends nothing
- [x] 6.3 Verify the error paths against the running site: missing parent returns 400 with an explanation and sends nothing; a translator-only user gets 403 on all three endpoints

## 7. Backoffice dashboard

- [x] 7.1 Remove the template's example code, regenerate the TypeScript client from the Announcements Swagger document; verify the generated client contains only the three announcement endpoints and the client build passes
- [x] 7.2 Register the dashboard in `umbraco-package.json` with the Content section and `GroupId` `noneOf` Translators conditions (confirm the group key from Umbraco's constants); verify it shows for an admin and is hidden for a translator-only user
- [x] 7.3 Build the dashboard element: subscriber count with loading state, history table newest first with edit links by page key, empty state; verify against seeded history in the browser
- [x] 7.4 Build the add announcement sidebar modal (short name, heading, main text, explanation, validation, busy state, notifications); verify cancelling creates nothing, empty required fields are flagged, and a double click sends once
- [x] 7.5 Verify keyboard-only use and labelled fields (WCAG 2.2 AA) on the dashboard and sidebar, and record findings in `docs/build-log.md`

## 8. End-to-end verification

- [x] 8.1 In Playwright, grant notification permission, subscribe on the site and send an announcement from the dashboard; verify the page publishes as `yyyyMMdd-{shortName}`, `registration.getNotifications()` shows the configured title and "dd MMM heading", and the history row shows correct delivery counts
- [x] 8.2 Ask the human to click the notification (Playwright cannot click OS notifications); verify with them that it focuses an already open tab on that page, and opens a new window when none is open
- [x] 8.3 In Playwright, unsubscribe in the browser and send again; verify the push service's 410 removes the subscription and it is counted as removed
- [x] 8.4 Record the end-to-end results, screenshots and the 2018 versus 2026 comparison notes in `docs/build-log.md`; verify `openspec validate rebuild-announcements-dashboard` passes

## Workflow follow-up

- Archive the change once the article review is complete.
- Open a follow-up change for the deferred improvements: background sending, unsubscribe, iOS web app manifest, packaging, send on publish, and Umbraco Automate.
