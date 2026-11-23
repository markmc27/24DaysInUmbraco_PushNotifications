# Spec Delta

## MODIFIED Requirements

### Requirement: Push follows a successful publish only
When an editor publishes an Announcement page for the first time, an automation SHALL send a push
notification to all subscribers linking to the page's absolute URL. Nothing MUST be sent for a
save without publish, or for a publish that fails.

#### Scenario: Publish succeeds
- **WHEN** an editor publishes a new Announcement page
- **THEN** a push notification with its heading, linking to the page's absolute URL, is sent to all subscribers

#### Scenario: Publish fails
- **WHEN** the announcement page fails to publish
- **THEN** no push notification is sent

#### Scenario: Saved but not published
- **WHEN** an editor saves an Announcement page without publishing it
- **THEN** no push notification is sent

## ADDED Requirements

### Requirement: Republishing does not notify again
Republishing an Announcement page that has already been pushed SHALL NOT send another push.

#### Scenario: Typo fixed
- **WHEN** an editor edits and republishes an Announcement page that was already pushed
- **THEN** no push notification is sent

### Requirement: Only announcement pages trigger a push
Publishing content of any type other than Announcement SHALL NOT send a push.

#### Scenario: Home page published
- **WHEN** an editor publishes the Home page
- **THEN** no push notification is sent

### Requirement: Automation is installed with the site
The announcement automation SHALL be defined in source control and installed and published on a
fresh site without manual backoffice set-up.

#### Scenario: Fresh clone
- **WHEN** the site starts for the first time on an empty database
- **THEN** the "Announce new pages" automation exists, is published, and responds to the next announcement publish

#### Scenario: Restart
- **WHEN** the site restarts with the automation already installed
- **THEN** no duplicate automation is created

## REMOVED Requirements

### Requirement: Dashboard in the Content section
**Reason**: Approach 2 replaces the custom dashboard with Umbraco Automate; editors publish an Announcement page in the Content tree instead.
**Migration**: Create and publish an Announcement page under the Announcements folder. Approach 1 remains available at tag `approach-1-dashboard`.

### Requirement: Translators cannot use announcements
**Reason**: There is no custom dashboard or API left to restrict.
**Migration**: Who can send an announcement is now who can publish Announcement pages, governed by Umbraco's own user group permissions.

### Requirement: Subscriber count
**Reason**: Removed with the dashboard.
**Migration**: None in approach 2. Each run's log shows how many subscribers were attempted.

### Requirement: Announcement history
**Reason**: Removed with the dashboard and history table.
**Migration**: Automate's run history lists each automation run with its step outputs.

### Requirement: Compose an announcement
**Reason**: Editors compose in the Announcement page editor instead of a sidebar.
**Migration**: Use the Announcement page's heading and main text fields.

### Requirement: Submitting creates and publishes a page
**Reason**: Editors create and publish the page themselves.
**Migration**: Create the page under the Announcements folder and publish it.

### Requirement: Send is recorded in history
**Reason**: The history table is removed.
**Migration**: The push action's outputs (attempted, succeeded, failed, removed) are stored with each Automate run.

### Requirement: Editor sees the result
**Reason**: Sending happens in the background after publish, so there is no submit result to show.
**Migration**: Editors see the normal publish confirmation; delivery results are in the automation's run history.

### Requirement: Content model is configurable
**Reason**: The extension no longer creates pages, so it needs no content model aliases.
**Migration**: The Announcement document type is selected in the automation's trigger filter, and the heading is bound from a Get Content step.

### Requirement: Dashboard meets WCAG 2.2 AA
**Reason**: Removed with the dashboard; the editor uses Umbraco's own content editor and the Automate canvas.
**Migration**: None.
