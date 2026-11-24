# announcements Specification

## Purpose
Lets editors tell subscribers about a new announcement simply by publishing an Announcement page:
an Umbraco Automate automation, installed with the site, pushes each page once.

## Requirements

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
