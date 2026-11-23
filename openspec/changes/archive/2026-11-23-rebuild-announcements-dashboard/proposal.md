# Proposal

## Why

In 2018 a 24 Days in Umbraco article built an "Announcements" push notification dashboard on
Umbraco 7 (AngularJS, `Dashboard.config`, PetaPoco, `UmbracoAuthorizedJsonController`). Almost every
one of those extension points has since been replaced. Rebuilding the same feature, like for like,
on Umbraco 17 LTS gives the revisit article an honest "same feature, two eras" comparison and
a worked example of building a modern backoffice extension with a spec-driven, AI-assisted approach.

## What Changes

- New Razor class library (from `dotnet new umbraco-extension`) holding the feature, referenced by
  the web project.
- New backoffice dashboard in the Content section, built with Lit and TypeScript, showing the
  subscriber count and past announcements, with a sidebar to add a new announcement (short name,
  heading, main text, and a "What is a push notification?" tooltip). Hidden from translators.
- New authorised Management API endpoints for the dashboard: subscriber count, history, and
  create announcement.
- Creating an announcement publishes an announcement page, sends a web push notification to every
  subscriber linking to that page, and records the send in a history table.
- New public endpoint for site visitors to subscribe to push notifications, protected by
  antiforgery and rate limiting.
- New database tables for push subscriptions and announcement history, created by a versioned
  migration plan.
- Web project: a migration creating a minimal "Announcements" folder, an "Announcement" page doc
  type and template; a minimal site page with an opt-in button, client script and service worker.
- Modernised by default: VAPID private key in user secrets (never committed), errors logged rather
  than swallowed, a failed page publish stops the send and is reported to the editor, content type
  and property aliases configured rather than hard-coded.
- Dropped from 2018: the Email toggle (never implemented) and the Facebook toggle (browser-side
  token, outdated API).

**Out of scope (deferred to a later improvements change):** background or queued sending,
unsubscribe, iOS web app manifest, NuGet packaging, sending automatically on publish, and Umbraco
Automate.

## Capabilities

### New Capabilities

- `push-subscriptions`: site visitors opting in to push notifications and the server storing
  their subscriptions safely.
- `push-delivery`: sending a web push message to all stored subscriptions, removing expired ones,
  and how the browser displays the notification and handles a click.
- `announcements`: the backoffice Announcements dashboard, who can use it, composing an
  announcement, publishing its page, and the send history.

### Modified Capabilities

None. This is the project's first change.

## Impact

- **New project:** `src/NotificationsRevisit.Announcements` (RCL with a Vite, Lit and TypeScript
  client). Added to `NotificationsRevisit.slnx` and referenced by the web project.
- **Web project:** new content model migration, a site template and view, a Vite and TypeScript
  client (`push.ts`, `sw.ts`) built into `wwwroot/js/push.js` and `wwwroot/sw.js`, configuration for
  aliases and the VAPID public key, user secret for the private key.
- **Dependencies:** `WebPush` NuGet package (the same library as 2018, now 1.0.13). Node.js for the
  front-end builds of both the RCL and the site.
- **Database:** two new tables in the CMS database (SQLite locally).
- **APIs:** new Management API endpoints (backoffice auth) and one public subscribe endpoint.
- **Requirements:** HTTPS (service workers and push need it; the local profile already uses it).
