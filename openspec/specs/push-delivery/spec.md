# push-delivery Specification

## Purpose
Sends a web push message to every stored subscription, keeps the subscription list clean, and
defines how the visitor's browser shows the notification and responds to a click.

## Requirements

### Requirement: Push is sent to every subscription
When asked to send a notification, the system SHALL attempt delivery to every stored subscription,
signed with the site's VAPID keys.

#### Scenario: Several subscribers
- **WHEN** a notification is sent and three subscriptions are stored
- **THEN** a delivery attempt is made to each of the three endpoints

#### Scenario: No subscribers
- **WHEN** a notification is sent and no subscriptions are stored
- **THEN** no delivery is attempted and the send completes without error

### Requirement: Notification payload
Each push message SHALL carry a title, a message and an absolute URL to open on click. The title
MUST come from configuration, not code.

#### Scenario: Payload contents
- **WHEN** a notification is sent for an announcement with a heading and page URL
- **THEN** the payload contains the configured title, a message that includes the heading, and
  the page's absolute URL

### Requirement: Expired subscriptions are removed
The system SHALL delete a stored subscription when its push service reports that it no longer
exists (HTTP 404 or 410).

#### Scenario: Gone subscription
- **WHEN** delivery to a subscription returns 404 or 410
- **THEN** that subscription is deleted
- **AND** delivery to the remaining subscriptions continues

### Requirement: Delivery failures are logged, not fatal
Any other delivery failure SHALL be logged with its status and SHALL NOT stop delivery to the
remaining subscriptions. The subscription MUST be kept.

#### Scenario: Transient failure
- **WHEN** delivery to one subscription fails with a status other than 404 or 410
- **THEN** the failure is logged with the status code
- **AND** that subscription is kept
- **AND** delivery to the remaining subscriptions continues

### Requirement: Send outcome is reported
A send SHALL report how many deliveries were attempted, succeeded, failed and how many expired
subscriptions were removed.

#### Scenario: Mixed outcome
- **WHEN** a send to four subscriptions has two successes, one failure and one gone subscription
- **THEN** the outcome reports 4 attempted, 2 succeeded, 1 failed and 1 removed

### Requirement: VAPID private key is never committed
The VAPID private key SHALL be read from secret configuration (user secrets locally, a secret
store in hosted environments) and MUST NOT appear in source-controlled files. The site SHALL fail
fast at startup if the VAPID keys or subject are missing.

#### Scenario: Keys missing
- **WHEN** the site starts without a configured VAPID public key, private key or subject
- **THEN** startup fails with an error naming the missing setting

### Requirement: Browser shows the notification
The service worker SHALL show a notification with the payload's title and message and the
configured icon when a push message arrives. A newer announcement MUST replace an older one still
on screen rather than stack.

#### Scenario: Push received
- **WHEN** the visitor's browser receives a push message for the site
- **THEN** a notification shows the payload's title, message and the site icon

#### Scenario: Second announcement
- **WHEN** a second announcement arrives while the first is still displayed
- **THEN** the second replaces the first

### Requirement: Clicking opens the announcement
Clicking the notification SHALL close it and focus an open tab already showing the payload URL,
or open the URL in a new window if none is open.

#### Scenario: Tab already open
- **WHEN** the visitor clicks the notification and a tab already shows that URL
- **THEN** that tab is focused and no new window opens

#### Scenario: No matching tab
- **WHEN** the visitor clicks the notification and no tab shows that URL
- **THEN** the URL opens in a new window
