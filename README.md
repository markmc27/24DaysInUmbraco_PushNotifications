# Notifications revisited

A proof of concept for a 24 Days in Umbraco article. It rebuilds the 2018 "Announcements" push
notification dashboard on Umbraco 17 LTS. See [AGENTS.md](AGENTS.md) for the stack and layout, and
[docs/build-log.md](docs/build-log.md) for every decision and why.

## Requirements

- .NET 10 SDK
- Node.js LTS (20.17 or later). `dotnet build` runs `npm ci` and `npm run build` for the
  backoffice extension and the site scripts, so Node must be on your path.

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
