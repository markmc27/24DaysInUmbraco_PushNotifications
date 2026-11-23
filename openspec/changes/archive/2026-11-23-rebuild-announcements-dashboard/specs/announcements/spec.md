# Spec Delta

## Purpose

Gives editors an Announcements dashboard in the Umbraco backoffice to see who is subscribed,
review past announcements, and publish a new announcement page that is pushed to subscribers.

## ADDED Requirements

### Requirement: Dashboard in the Content section
The backoffice SHALL show an "Announcements" dashboard in the Content section to users who may
use it.

#### Scenario: Editor opens Content
- **WHEN** a user with access to the Content section, who is not a translator, opens Content
- **THEN** an "Announcements" dashboard is available

### Requirement: Translators cannot use announcements
Users in the Translators group SHALL NOT see the dashboard, and the server MUST reject their
announcement API calls, so hiding the UI is not the only protection.

#### Scenario: Translator opens Content
- **WHEN** a user whose only group is Translators opens the Content section
- **THEN** the Announcements dashboard is not shown

#### Scenario: Translator calls the API directly
- **WHEN** a Translators-only user calls any announcements API endpoint
- **THEN** the server responds with a forbidden error and changes nothing

#### Scenario: No Content access
- **WHEN** a user without Content section access calls any announcements API endpoint
- **THEN** the server responds with a forbidden error

### Requirement: Subscriber count
The dashboard SHALL show the current number of stored push subscriptions, with a loading state
until the count arrives.

#### Scenario: Count shown
- **WHEN** an editor opens the dashboard and 12 subscriptions are stored
- **THEN** the dashboard shows a subscriber count of 12

### Requirement: Announcement history
The dashboard SHALL list past announcements newest first, each with its heading, sent date and
time, the sending user's name, and a link to open its page in the backoffice.

#### Scenario: History listed
- **WHEN** an editor opens the dashboard and announcements have been sent
- **THEN** each appears with heading, sent date, user and a link to edit its page, newest first

#### Scenario: Nothing sent yet
- **WHEN** an editor opens the dashboard and no announcements exist
- **THEN** the dashboard says that no announcements have been made yet

### Requirement: Compose an announcement
The dashboard SHALL let an editor open a sidebar with fields for short name, heading and main
text, plus a "What is a push notification?" explanation, and submit or cancel it.

#### Scenario: Open and cancel
- **WHEN** an editor opens the add announcement sidebar and cancels
- **THEN** the sidebar closes and nothing is created or sent

#### Scenario: Required fields
- **WHEN** an editor submits with the short name or heading empty
- **THEN** the sidebar shows which fields are required and nothing is created or sent

### Requirement: Submitting creates and publishes a page
Submitting a valid announcement SHALL create and publish an announcement page under the
configured announcements parent, named from the current date and the short name, holding the
heading and main text.

#### Scenario: Page published
- **WHEN** an editor submits short name "pool-closed", heading "Pool closed" and some text on
  21 November 2026
- **THEN** a page named "20261121-pool-closed" is published under the announcements parent
- **AND** its heading and main text properties hold the submitted values

#### Scenario: Parent missing
- **WHEN** no content node of the configured announcements parent type exists
- **THEN** nothing is created or sent and the editor sees an error explaining the missing parent

### Requirement: Push follows a successful publish only
After the page publishes, the system SHALL send a push notification to all subscribers linking
to the page's absolute URL. If the publish fails, it MUST NOT send.

#### Scenario: Publish succeeds
- **WHEN** the announcement page publishes successfully
- **THEN** a push notification linking to the page's absolute URL is sent to all subscribers

#### Scenario: Publish fails
- **WHEN** the announcement page fails to publish
- **THEN** no push notification is sent
- **AND** the editor sees an error with the reason

### Requirement: Send is recorded in history
Each sent announcement SHALL be recorded with heading, sent time in UTC, sending user, the page,
and the delivery counts (attempted, succeeded, failed).

#### Scenario: History updated
- **WHEN** an announcement is sent
- **THEN** a history record is stored with its heading, time, user, page and delivery counts
- **AND** the dashboard's history and subscriber count refresh to include it

### Requirement: Editor sees the result
After submitting, the editor SHALL see a success or failure message, and the submit control MUST
show a busy state and block resubmission while the request runs.

#### Scenario: Success
- **WHEN** an announcement is created and sent
- **THEN** the editor sees a success message including how many subscribers it was sent to

#### Scenario: Double submit
- **WHEN** an editor activates submit twice quickly
- **THEN** only one announcement is created and sent

### Requirement: Content model is configurable
The announcement page document type, the parent document type and the heading and main text
property aliases SHALL come from configuration, validated at startup.

#### Scenario: Missing alias
- **WHEN** the site starts with any of the announcement aliases unset
- **THEN** startup fails with an error naming the missing setting

### Requirement: Dashboard meets WCAG 2.2 AA
The dashboard and sidebar SHALL be operable by keyboard, with labelled form fields and status
messages announced to assistive technology.

#### Scenario: Keyboard only
- **WHEN** an editor uses only the keyboard
- **THEN** they can open the sidebar, fill every field, read the tooltip text, and submit or cancel
