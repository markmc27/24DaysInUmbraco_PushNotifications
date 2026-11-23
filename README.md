# Notifications revisited

A proof of concept for a 24 Days in Umbraco article. It revisits the 2018 "Announcements" push
notifications for a swimming club on Umbraco 17 LTS, in alternative approaches:

- **Approach 1** (tag `approach-1-dashboard`): a custom backoffice dashboard, API and service.
- **Approach 2** (this code): Umbraco Automate sends the push when an editor publishes an
  Announcement page.

See [AGENTS.md](AGENTS.md) for the stack and layout. The decisions and why are in
[docs/build-log.md](docs/build-log.md) (approach 1) and
[docs/build-log-approach-2.md](docs/build-log-approach-2.md) (approach 2).

## Requirements

- .NET 10 SDK
- Node.js LTS (20.17 or later). `dotnet build` runs `npm ci` and `npm run build` for the site
  scripts (the opt-in and the service worker), so Node must be on your path.

## Run it

```powershell
dotnet build NotificationsRevisit.slnx
dotnet run --project src/NotificationsRevisit.Web --launch-profile Umbraco.Web.UI
```

The first run installs Umbraco unattended into a local SQLite database. Set the admin password in
user secrets before the first run:

```powershell
dotnet user-secrets set "Umbraco:CMS:Unattended:UnattendedUserPassword" "<a password of 10+ characters>" --project src/NotificationsRevisit.Web
```

## How announcements are sent

1. A visitor opts in on the site, which stores their browser's push subscription.
2. An editor creates an **Announcement** page under **Home > Announcements** in the Content
   section and publishes it.
3. The **Announce new pages** automation (Automation section, workspace **Site**) picks up the
   publish: Content Published (Announcement pages only), then Get Content, then **Send push
   notification**, with the page's heading as the message and the page as the link.
4. Automate runs it in the background, usually within about 10 seconds. Each run, with the step
   outputs (attempted, succeeded, failed, removed), is under the automation's **Runs** tab.

The automation runs outside any web request, so Umbraco needs to be told the site's address to
build the link in the notification. Set `Umbraco:CMS:WebRouting:UmbracoApplicationUrl` in every
environment (`https://localhost:44371/` is set in `appsettings.Development.json`). Without it,
links point at `http://localhost/`.

A page is pushed once. Republishing it (to fix a typo, say) runs the automation again, but the
push step records "already sent" and sends nothing.

### Where the automation lives

The automation is defined in
[src/NotificationsRevisit.Web/Automations/announce-new-pages.json](src/NotificationsRevisit.Web/Automations/announce-new-pages.json).
On the first start with an empty database, the site installs it: it creates an API user
(`automate-site@example.com`, Writers group) as the workspace's service account, the **Site**
workspace, then imports and publishes the automation. This runs once; it does not overwrite or
re-create the automation on later starts.

The **Send push notification** action is code in the extension project
(`src/NotificationsRevisit.Announcements/Automation/`). Any automation can use it.

### Changing the automation

1. Change it in the backoffice and publish it.
2. Open the automation's actions menu (**...** next to its name) and choose **Export**.
3. Save the file over `src/NotificationsRevisit.Web/Automations/announce-new-pages.json` and commit it.

A fresh database picks up the new file. An existing database keeps what it has; to update it,
use **Import** on the **Site** workspace's actions menu (**...** next to Site in the Automation
tree).

## Push notification settings

Web push needs a VAPID key pair: the public key goes to browsers when they subscribe, and the
private key signs every message. The site refuses to start until all push settings are present,
and the error names the missing one.

### 1. Generate a key pair (once)

```powershell
npx web-push generate-vapid-keys
```

### 2. Store the keys

The private key is a secret. Keep it in user secrets locally (or a secret store such as Azure Key
Vault when hosted). Never put it in `appsettings*.json`.

```powershell
dotnet user-secrets set "Announcements:Push:Vapid:PrivateKey" "<private key>" --project src/NotificationsRevisit.Web
```

The public key and subject are not secret. Put them in `appsettings.Development.json` (or your
environment's settings):

```json
"Announcements": {
  "Push": {
    "Vapid": {
      "Subject": "mailto:you@example.com",
      "PublicKey": "<public key>"
    }
  }
}
```

If you change the keys, existing browser subscriptions stop working: visitors need to subscribe
again.

### All push settings

| Setting (`Announcements:Push:...`) | Where | Purpose |
|---|---|---|
| `Title` | `appsettings.json` | Notification title, for example the club name |
| `IconUrl` | `appsettings.json` | Absolute path of the notification icon |
| `MaxConcurrency` | `appsettings.json` | Deliveries sent at once (default 4) |
| `Vapid:Subject` | `appsettings.Development.json` | `mailto:` or `https:` contact for the push service |
| `Vapid:PublicKey` | `appsettings.Development.json` | Public VAPID key, sent to browsers |
| `Vapid:PrivateKey` | user secrets | Private VAPID key, signs messages |
