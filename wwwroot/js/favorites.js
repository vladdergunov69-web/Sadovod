(function () {
    'use strict';

    function token() {
        const el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    async function post(url) {
        const resp = await fetch(url, {
            method: 'POST',
            headers: { 'RequestVerificationToken': token() }
        });
        // Гость / истёкшая сессия — отправляем на страницу входа.
        if (resp.status === 401 || resp.status === 403) {
            window.location.href = '/Account/Login';
            return null;
        }
        if (!resp.ok) throw new Error('Request failed: ' + resp.status);
        return await resp.json();
    }

    function updateCount(count) {
        const badge = document.getElementById('fav-count');
        if (!badge) return;
        badge.textContent = count;
        badge.classList.toggle('d-none', count <= 0);
    }

    function applyState(btn, isFav) {
        btn.dataset.fav = isFav ? '1' : '0';
        btn.classList.toggle('is-fav', isFav);
        btn.setAttribute('aria-pressed', isFav ? 'true' : 'false');
        btn.title = isFav ? 'Убрать из избранного' : 'В избранное';
        const icon = btn.querySelector('.fav-icon');
        if (icon) icon.textContent = isFav ? '❤' : '🤍';
    }

    function maybeShowEmpty() {
        const grid = document.getElementById('fav-grid');
        if (grid && grid.querySelectorAll('[data-fav-card]').length === 0) {
            grid.classList.add('d-none');
            const empty = document.getElementById('fav-empty');
            if (empty) empty.classList.remove('d-none');
        }
    }

    async function onToggle(btn) {
        const id = btn.dataset.productId;
        if (!id) return;
        const isFav = btn.dataset.fav === '1';
        const url = (isFav ? '/Favorites/Remove/' : '/Favorites/Add/') + encodeURIComponent(id);
        try {
            btn.disabled = true;
            const data = await post(url);
            if (!data) return;
            applyState(btn, data.isFavorite);
            updateCount(data.count);
            // На странице «Избранное» убираем карточку при снятии отметки.
            if (!data.isFavorite && btn.dataset.removeCard !== undefined) {
                const card = btn.closest('[data-fav-card]');
                if (card) card.remove();
                maybeShowEmpty();
            }
        } catch (err) {
            console.error('Favorite toggle failed', err);
        } finally {
            btn.disabled = false;
        }
    }

    document.addEventListener('click', function (e) {
        const btn = e.target.closest('.fav-btn[data-product-id]');
        if (btn) {
            e.preventDefault();
            onToggle(btn);
        }
    });
})();
