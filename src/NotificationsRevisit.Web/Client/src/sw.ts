// Service worker for announcements. Close to the 2018 sw.js: show the push, open or focus the page on click.
declare const self: ServiceWorkerGlobalScope;

interface AnnouncementPayload {
  title: string;
  message: string;
  url: string;
  icon?: string;
}

// A fixed tag means a newer announcement replaces an older one still on screen, rather than stacking.
const NOTIFICATION_TAG = "announcement";

self.addEventListener("push", (event) => {
  const payload = event.data?.json() as AnnouncementPayload | undefined;
  if (!payload) {
    return;
  }

  event.waitUntil(
    self.registration.showNotification(payload.title, {
      body: payload.message,
      // Absolute path from the server (2018 used a relative path, which broke on sub-pages).
      icon: payload.icon,
      tag: NOTIFICATION_TAG,
      data: { url: payload.url },
    }),
  );
});

self.addEventListener("notificationclick", (event) => {
  event.notification.close();

  const url = (event.notification.data as { url?: string } | null)?.url;
  if (!url) {
    return;
  }

  event.waitUntil(focusOrOpen(new URL(url, self.location.origin).href));
});

async function focusOrOpen(url: string): Promise<void> {
  const windows = await self.clients.matchAll({ type: "window", includeUncontrolled: true });
  const open = windows.find((client) => client.url === url);

  if (open) {
    await open.focus();
    return;
  }

  await self.clients.openWindow(url);
}
