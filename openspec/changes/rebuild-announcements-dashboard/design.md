# Design

## Context

- See `proposal.md` (Why) for motivation and `specs/` for the behaviour contract.
- The repo is a vanilla Umbraco 17.8.2 site on .NET 10 with SQLite, central package management
  and ModelsBuilder in `SourceCodeAuto` for development. Umbraco Automate is installed but not
  used by this change.
- The 2018 baseline (Umbraco 7.12.3) is described in `docs/build-log.md`, phase 2. Decisions below
  name the 2018 equivalent where the comparison is useful for the article.
- Local site runs on `https://localhost:44371`, which satisfies the secure context push needs.

## Goals / Non-Goals

**Goals:**
- Behavioural parity with the 2018 feature, built only with current, documented Umbraco 17
  extension points.
- A clean boundary: the feature lives in the RCL; the content model and site markup belong to the
  web project.
- Every architectural choice has a 2018 counterpart the article can compare against.
- The agent can build and verify the feature end to end without manual backoffice set-up.

**Non-Goals:**
- Backoffice localisation beyond English.
- A polished public site design: the site pages exist only to host the opt-in and show an
  announcement.
- Scale beyond a small club: sending stays inside the request (see Risks).

## Decisions

### D1. Project layout: RCL from the official template

```
src/
  NotificationsRevisit.Web/             site: content model migration, views, sw.js, push.js
  NotificationsRevisit.Announcements/   RCL: API, services, persistence, Client/ (Lit + TS)
tests/
  NotificationsRevisit.Announcements.Tests/
```

- Create with `dotnet new umbraco-extension -n NotificationsRevisit.Announcements --include-example
  --site-domain https://localhost:44371`, align Umbraco package versions to 17.8.2 through
  `Directory.Packages.props`, then replace the example with the feature.
- 2018: a `Core` class library plus the website. Same split, but the RCL also carries its
  front-end assets as static web assets.
- Alternative rejected: everything in the web project (we would hand-roll the Vite build and
  client generation, and extract it later to package it).

### D2. Persistence: NPoco and a package migration plan

- Two tables, created by a `PackageMigrationPlan` in the RCL, so the migration is versioned,
  recorded by Umbraco and visible in the backoffice Packages section.
  - `announcementsPushSubscription`: Id (PK, identity), Endpoint (nvarchar(800), unique index),
    P256dh, Auth, CreatedUtc.
  - `announcementsHistory`: Id (PK), Heading, SentUtc, UserKey, UserName, PageKey (Guid),
    Attempted, Succeeded, Failed.
- Access through `IScopeProvider` and NPoco in two small repositories. Upsert on Endpoint gives the
  "no duplicates" behaviour.
- History stores the page **key** (Guid), not the integer id, because the modern backoffice routes
  by key.
- 2018: PetaPoco, `ApplicationEventHandler` with "create the table if it is missing", a JSON file
  for history.
- Alternative rejected: EF Core (needs a migration set per database provider for two small tables).

### D3. Content model: owned by the site, aliases configured

- Web project: a package migration plan creates doc types Home (root, allows Announcements),
  Announcements (folder, allows Announcement), Announcement (heading: Textstring, main text:
  Textarea), their templates, and a Home and Announcements node, so a fresh clone works. Views are
  committed `.cshtml` files; the migration links them.
- RCL reads `Announcements:ContentModel` options: `PageDocTypeAlias`, `ParentDocTypeAlias`,
  `HeadingPropertyAlias`, `TextPropertyAlias`, validated at startup.
- 2018: hard-coded `newsPage` and `announcementsFolder` from the starter kit.

### D4. Backoffice API: Management API controller with a custom policy

- One controller deriving from `ManagementApiControllerBase`, on a versioned backoffice route
  (`/umbraco/announcements/api/v1/...`), in its own Swagger document so the generated TypeScript
  client contains only these endpoints:
  - `GET subscriptions/count`
  - `GET history`
  - `POST announcements`: returns page key, URL and delivery counts.
- Authorisation: a custom policy requiring Content section access **and** not being in the
  Translators group, so the server enforces what the UI hides. 2018 relied only on
  `Dashboard.config`'s `<deny>translator</deny>`, which hid the tab but left the API open.
- 2018: `UmbracoAuthorizedJsonController` with `[PluginController]`, called with `$http`.

### D5. Announcement creation flow

```
POST announcements
  -> validate request (short name, heading required)
  -> find parent node of ParentDocTypeAlias          (none: 400, nothing sent)
  -> content editing service: create "yyyyMMdd-{shortName}" with heading and text
  -> content publishing service: publish             (fails: 400 with reason, nothing sent)
  -> resolve absolute URL of the published page
  -> IPushService.SendAsync(payload)                 -> PushSendResult {attempted, ok, failed, removed}
  -> history repository: insert record (current backoffice user)
  -> 200 { pageKey, url, result }
```

- Uses the async `IContentEditingService` and `IContentPublishingService` introduced with the new
  backoffice, rather than the legacy `IContentService.SaveAndPublish`.
- Push message keeps the 2018 format: configured title, message `"{dd MMM} {heading}"`.

### D6. Push delivery: WebPush library behind an interface

- `WebPush` 1.0.13 (the same library as 2018) wrapped by `IPushService`, registered with
  `IHttpClientFactory` so the `HttpClient` is managed.
- Sends with bounded concurrency (`Parallel.ForEachAsync`, default 4, configurable). Gone
  endpoints (404/410) are collected and deleted after the loop, avoiding the 2018 pattern of
  deleting while iterating. Other failures are logged with status code.
- `Announcements:Push` options: `Title`, `IconUrl`, `MaxConcurrency`, and `Vapid` (`Subject`,
  `PublicKey`, `PrivateKey`), validated on start. The private key lives in user secrets locally.
- VAPID keys generated once with `npx web-push generate-vapid-keys` (Node is already needed for
  the RCL build). Documented in the README, never committed.
- 2018: sequential sends inside the request, swallowed errors, keys in `vapid.json` in Git.

### D7. Public subscribe endpoint: plain ASP.NET Core

- `POST /api/announcements/push-subscriptions`, an `[ApiController]` deriving from
  `ControllerBase` (no Umbraco base class needed for a public JSON endpoint).
- `[ValidateAntiForgeryToken]`: the site layout renders the token; `push.js` sends it in the
  `RequestVerificationToken` header.
- ASP.NET Core rate limiting: a fixed window policy partitioned by client IP (default 5 per hour,
  configurable), applied with `[EnableRateLimiting]`. The middleware is added through an Umbraco
  pipeline filter registered in a composer, so `Program.cs` stays untouched.
- 2018: `UmbracoApiController`, `System.Web` antiforgery and a hand-written `ThrottleAttribute`.

### D8. Backoffice UI: Lit dashboard and a sidebar modal

- `umbraco-package.json` registers a `dashboard` extension with two conditions:
  `Umb.Condition.SectionAlias` matching `Umb.Section.Content`, and
  `Umb.Condition.CurrentUser.GroupId` with `noneOf` the Translators group key.
- Dashboard element (Lit, TypeScript, UUI): subscriber count in a header, `uui-table` of history,
  "Add new announcement" button. Edit links point to the document workspace by page key.
- Sidebar: a modal token with `type: 'sidebar'` opened through the modal manager. Form uses UUI
  inputs with labels and a `uui-popover`-style explanation for "What is a push notification?".
- Calls go through the TypeScript client generated from the controller's Swagger document
  (`npm run generate-client`), authenticated by the backoffice's own token.
- Feedback through the notification context; the submit button uses UUI's waiting state, which
  also blocks double submit.
- 2018: AngularJS controller and resource, `umb-overlay position="right"`, `umb-toggle`,
  `notificationsService`.

### D9. Site front end: minimal and framework-free

- `wwwroot/sw.js` at the site root (scope `/`), close to the 2018 file: `push` shows the
  notification with a fixed `tag`; `notificationclick` focuses or opens the URL. The icon path is
  absolute, fixing the 2018 relative path.
- `wwwroot/js/push.js`: plain modern JavaScript (no jQuery). Feature detection, secure context
  check, opt-in button, `pushManager.subscribe` with the VAPID public key rendered into the page
  by the layout, POST with `fetch`.
- One shared layout with the opt-in, used by the Home and Announcement templates.

### D10. Verification strategy

- **Unit tests** (xUnit) for the logic most likely to break silently: delivery outcome counting
  and gone-subscription removal (with a fake sender), and page naming.
- **Agent-driven end-to-end** in the in-app or Playwright browser: subscribe on the site, send an
  announcement from the dashboard, confirm the history row, the published page and the delivery
  counts. Results and screenshots recorded in `docs/build-log.md`.
- **Translator check:** create a translator-only user and confirm the dashboard is hidden and the
  API returns 403.

## Risks / Trade-offs

- [Sending inside the request blocks the editor and could time out with many subscribers] ->
  Bounded concurrency keeps a small club fast; background sending is the first item in the
  deferred improvements change.
- [Endpoint URLs longer than 800 characters would fail the unique index] -> Real push endpoints are
  well under this; reject over-long endpoints with a 400 rather than truncate.
- [Rate limiting by IP is coarse behind proxies or shared networks] -> Use forwarded headers if
  hosted behind a proxy; acceptable for a demo.
- [Translators group key in the manifest is a fixed GUID] -> It is Umbraco's well-known constant
  for the default group; renamed or custom translator groups are not covered, and the server-side
  policy remains the real control.
- [Generated TypeScript client drifts from the API] -> Regenerate after any controller change; the
  task list makes it an explicit step.
- [Browser support differs (iOS needs a home screen web app)] -> Out of scope here; the opt-in is
  hidden where unsupported.

## Migration Plan

- Additive only: two new tables and new content types created by migrations on first start.
- Rollback: remove the RCL reference; the tables and content can be deleted by hand. No existing
  data is changed.

## Open Questions

- Translators group key: confirm the GUID from Umbraco's constants at implementation time
  (expected `550e8400-e29b-41d4-a716-446655440000`).
- Exact NPoco and migration base class names in 17.8.2 (sync or async migration base): confirm
  against the installed packages when implementing; the approach does not change.
