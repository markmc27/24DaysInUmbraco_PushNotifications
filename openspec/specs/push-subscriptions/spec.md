# push-subscriptions Specification

## Purpose
Lets site visitors opt in to web push notifications from the site, and stores their push
subscriptions safely so announcements can reach them.

## Requirements

### Requirement: Opt-in is visitor initiated
The site SHALL ask for notification permission only after the visitor activates an opt-in
control. It MUST NOT prompt for permission on page load.

#### Scenario: Page load does not prompt
- **WHEN** a visitor loads a page that contains the opt-in control
- **THEN** no browser notification permission prompt is shown

#### Scenario: Visitor opts in
- **WHEN** the visitor activates the opt-in control and grants permission
- **THEN** the browser creates a push subscription using the site's public VAPID key
- **AND** the site sends that subscription to the server

### Requirement: Opt-in hidden where unsupported
The site SHALL hide the opt-in control when the browser does not support service workers or the
Push API.

#### Scenario: Unsupported browser
- **WHEN** a visitor's browser lacks service worker or Push API support
- **THEN** the opt-in control is not shown

### Requirement: Opt-in reflects current state
The opt-in control SHALL show a confirmed state after a successful subscription, and SHALL NOT be
offered again to a browser that already has a subscription for the site.

#### Scenario: Successful subscription
- **WHEN** the server accepts the visitor's subscription
- **THEN** the control shows that notifications are enabled and cannot be activated again

#### Scenario: Already subscribed
- **WHEN** a visitor whose browser already holds a push subscription for the site loads a page
- **THEN** the opt-in control is not offered

#### Scenario: Permission denied
- **WHEN** the visitor denies notification permission
- **THEN** no subscription is sent to the server
- **AND** the control does not show the enabled state

### Requirement: Subscription is stored
The server SHALL store each accepted subscription's endpoint URL, p256dh key, auth secret and
creation time in UTC.

#### Scenario: Valid subscription
- **WHEN** the server receives a subscription with an endpoint, p256dh key and auth secret
- **THEN** the subscription is stored with its creation time
- **AND** the server responds with success

#### Scenario: Missing fields
- **WHEN** the server receives a subscription missing the endpoint, p256dh key or auth secret
- **THEN** the server rejects it with a client error and stores nothing

### Requirement: Duplicate subscriptions are not stored twice
The server SHALL store at most one subscription per endpoint URL. Resubmitting an endpoint MUST
update its keys rather than create a second record.

#### Scenario: Same browser subscribes again
- **WHEN** the server receives a subscription whose endpoint is already stored
- **THEN** exactly one stored subscription has that endpoint, holding the latest keys

### Requirement: Subscribe endpoint is protected against forgery
The subscribe endpoint SHALL reject requests that do not carry a valid antiforgery token issued
by the site.

#### Scenario: Missing or invalid token
- **WHEN** a request to the subscribe endpoint has no valid antiforgery token
- **THEN** the server rejects it and stores nothing

### Requirement: Subscribe endpoint is rate limited
The subscribe endpoint SHALL limit how often a single client IP address can call it within a
configurable time window, and reject calls over the limit.

#### Scenario: Limit exceeded
- **WHEN** one IP address exceeds the configured number of subscribe calls within the window
- **THEN** further calls in that window are rejected with a "too many requests" response
- **AND** nothing is stored for the rejected calls

### Requirement: Subscribe works only over HTTPS
Push subscription SHALL be offered only on pages served over HTTPS, because browsers require a
secure context for service workers and push.

#### Scenario: Insecure context
- **WHEN** the page is not served in a secure context
- **THEN** the opt-in control is not shown
