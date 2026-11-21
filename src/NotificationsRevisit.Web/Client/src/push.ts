// Opt-in to announcements as push notifications. Never asks for permission on page load:
// only after the visitor activates the button.

const SUBSCRIBE_URL = "/api/announcements/push-subscriptions";

const container = document.getElementById("push-opt-in");
const button = document.getElementById("push-opt-in-button") as HTMLButtonElement | null;
const status = document.getElementById("push-opt-in-status");

function meta(name: string): string {
  return document.querySelector<HTMLMetaElement>(`meta[name="${name}"]`)?.content ?? "";
}

function isSupported(): boolean {
  return (
    window.isSecureContext &&
    "serviceWorker" in navigator &&
    "PushManager" in window &&
    "Notification" in window
  );
}

/** VAPID keys are URL-safe base64; the Push API wants the raw bytes. */
function base64UrlToBytes(value: string): Uint8Array<ArrayBuffer> {
  const base64 = (value + "=".repeat((4 - (value.length % 4)) % 4)).replace(/-/g, "+").replace(/_/g, "/");
  const raw = atob(base64);
  const bytes = new Uint8Array(new ArrayBuffer(raw.length));
  for (let i = 0; i < raw.length; i++) {
    bytes[i] = raw.charCodeAt(i);
  }
  return bytes;
}

function setStatus(message: string): void {
  if (status) {
    status.textContent = message;
  }
}

function showEnabled(): void {
  if (button) {
    button.textContent = "Notifications are on";
    button.disabled = true;
  }
  setStatus("We will send you a notification when we post an announcement.");
}

async function subscribe(): Promise<void> {
  if (!button) {
    return;
  }

  button.disabled = true;
  setStatus("Turning on notifications…");

  const permission = await Notification.requestPermission();
  if (permission !== "granted") {
    button.disabled = false;
    setStatus("Notifications are not allowed in your browser, so we have not turned them on.");
    return;
  }

  try {
    const registration = await navigator.serviceWorker.ready;
    const subscription = await registration.pushManager.subscribe({
      userVisibleOnly: true,
      applicationServerKey: base64UrlToBytes(meta("vapid-public-key")),
    });

    const response = await fetch(SUBSCRIBE_URL, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        RequestVerificationToken: meta("request-verification-token"),
      },
      // PushSubscription.toJSON() gives { endpoint, expirationTime, keys: { p256dh, auth } }.
      body: JSON.stringify(subscription),
    });

    if (!response.ok) {
      // Undo the browser side, so the visitor is offered the button again next time.
      await subscription.unsubscribe();
      throw new Error(`Subscribe failed with status ${response.status}`);
    }

    showEnabled();
  } catch (error) {
    console.error(error);
    button.disabled = false;
    setStatus("Sorry, we could not turn on notifications. Please try again later.");
  }
}

async function init(): Promise<void> {
  // Unsupported browsers and insecure pages keep the control hidden.
  if (!container || !button || !isSupported()) {
    return;
  }

  const registration = await navigator.serviceWorker.register("/sw.js", { scope: "/" });

  // Already subscribed in this browser: do not offer it again.
  if (await registration.pushManager.getSubscription()) {
    return;
  }

  container.hidden = false;
  button.addEventListener("click", () => void subscribe());
}

void init();
