(function () {
    'use strict';

    function urlBase64ToUint8Array(base64String) {
        const padding = '='.repeat((4 - base64String.length % 4) % 4);
        const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
        const rawData = atob(base64);
        const out = new Uint8Array(rawData.length);
        for (let i = 0; i < rawData.length; ++i) out[i] = rawData.charCodeAt(i);
        return out;
    }

    async function subscribe(btn) {
        if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
            alert('Ваш браузер не поддерживает push-уведомления.');
            return;
        }
        try {
            btn.disabled = true;
            const reg = await navigator.serviceWorker.register('/service-worker.js');
            const perm = await Notification.requestPermission();
            if (perm !== 'granted') {
                alert('Вы запретили уведомления.');
                btn.disabled = false;
                return;
            }
            const keyResp = await fetch('/api/push/key');
            const keyJson = await keyResp.json();
            const sub = await reg.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: urlBase64ToUint8Array(keyJson.publicKey)
            });
            const payload = {
                endpoint: sub.endpoint,
                keys: {
                    p256dh: btoa(String.fromCharCode.apply(null, new Uint8Array(sub.getKey('p256dh')))),
                    auth:   btoa(String.fromCharCode.apply(null, new Uint8Array(sub.getKey('auth'))))
                }
            };
            const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
            await fetch('/api/push/subscribe', {
                method: 'POST',
                headers: Object.assign({ 'Content-Type': 'application/json' },
                    tokenInput ? { 'RequestVerificationToken': tokenInput.value } : {}),
                body: JSON.stringify(payload)
            });
            btn.textContent = '✓ Подписка активна';
            btn.classList.remove('btn-outline-success');
            btn.classList.add('btn-success');
        } catch (err) {
            console.error('Push subscribe failed', err);
            alert('Не удалось подписаться на уведомления.');
            btn.disabled = false;
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        const btn = document.getElementById('push-subscribe');
        if (btn) btn.addEventListener('click', function () { subscribe(btn); });
    });
})();
