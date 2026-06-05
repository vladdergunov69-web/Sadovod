self.addEventListener('install', function (event) {
    self.skipWaiting();
});

self.addEventListener('activate', function (event) {
    event.waitUntil(self.clients.claim());
});

self.addEventListener('push', function (event) {
    let data = { title: 'Садовод', body: 'У нас новости!', url: '/' };
    try {
        if (event.data) data = Object.assign(data, event.data.json());
    } catch (e) { /* keep defaults */ }

    const options = {
        body: data.body,
        icon: '/images/products/no-photo.png',
        badge: '/images/products/no-photo.png',
        data: { url: data.url || '/' }
    };
    event.waitUntil(self.registration.showNotification(data.title || 'Садовод', options));
});

self.addEventListener('notificationclick', function (event) {
    event.notification.close();
    const url = (event.notification.data && event.notification.data.url) || '/';
    event.waitUntil(clients.matchAll({ type: 'window', includeUncontrolled: true }).then(function (list) {
        for (const c of list) {
            if (c.url.indexOf(url) >= 0 && 'focus' in c) return c.focus();
        }
        if (clients.openWindow) return clients.openWindow(url);
    }));
});
