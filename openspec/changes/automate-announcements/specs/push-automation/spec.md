# Spec Delta

## Purpose

Provides a reusable Umbraco Automate action that sends a web push notification to every
subscriber, so any automation can notify site visitors without new orchestration code.

## ADDED Requirements

### Requirement: Send push notification action is available
Automation authors SHALL be able to add a "Send push notification" step to any automation in the
Automate canvas.

#### Scenario: Action listed
- **WHEN** an author adds a step to an automation
- **THEN** "Send push notification" is offered among the available actions

### Requirement: Message and page are bindable settings
The action SHALL take a message and a content key as settings, and both MUST accept bindings to
trigger or earlier step values.

#### Scenario: Bound values used
- **WHEN** the message is bound to a Get Content step's heading and the content key to the trigger's content key
- **THEN** the push carries that heading and opens that content's page on click

#### Scenario: Missing settings
- **WHEN** the message or content key resolves to an empty value
- **THEN** the step fails with an error naming the missing setting and nothing is sent

### Requirement: The push links to the page's public URL
The action SHALL resolve the absolute public URL of the content item and send it as the push's
click-through URL. It MUST NOT send if the content has no public URL.

#### Scenario: Unpublished or unroutable content
- **WHEN** the content key does not resolve to a published page with an absolute URL
- **THEN** the step fails with an explanation and nothing is sent

### Requirement: A page is pushed only once
The action SHALL record each content item it has pushed, and SHALL skip any content item already
recorded, completing successfully without sending.

#### Scenario: First publish
- **WHEN** the action runs for a content item it has not pushed before
- **THEN** a push is sent to every subscriber and the content item is recorded as sent

#### Scenario: Republish
- **WHEN** the action runs again for a content item already recorded as sent
- **THEN** no push is sent, the step succeeds, and the run log says it was already sent

#### Scenario: Failed send is not recorded
- **WHEN** sending fails before any delivery is attempted
- **THEN** the content item is not recorded, so a later publish can try again

### Requirement: Delivery outcome is visible in the run
The action SHALL report how many deliveries were attempted, succeeded, failed and how many expired
subscriptions were removed, as step outputs and in the step's run log.

#### Scenario: Outcome recorded
- **WHEN** a push is sent to four subscriptions with two successes, one failure and one gone subscription
- **THEN** the step outputs and log show 4 attempted, 2 succeeded, 1 failed and 1 removed

### Requirement: Push runs in the background
Sending SHALL happen in the Automate workflow, not in the request that published the page.

#### Scenario: Editor publishes
- **WHEN** an editor publishes a page that triggers the automation
- **THEN** the publish completes without waiting for push delivery
