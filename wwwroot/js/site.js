/* CAMPUSLY - core interface behaviour (vanilla JS, no framework).
   Pages still work without JavaScript; this adds the interactive layer. */
(function () {
  'use strict';
  var $ = function (s, r) { return (r || document).querySelector(s); };
  var $$ = function (s, r) { return Array.prototype.slice.call((r || document).querySelectorAll(s)); };
  var reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  /* ---- theme (initial value is set in <head> to avoid a flash) ---- */
  function toggleTheme() {
    var next = document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark';
    document.documentElement.dataset.theme = next;
    try { localStorage.setItem('theme', next); } catch (e) { /* storage blocked */ }
    document.dispatchEvent(new CustomEvent('themechange'));
  }
  $$('.theme-toggle, .theme-toggle-btn').forEach(function (b) { b.addEventListener('click', toggleTheme); });

  /* ---- public nav drawer ---- */
  var navBtn = $('.nav-toggle'), nav = $('#site-nav');
  if (navBtn && nav) {
    navBtn.addEventListener('click', function () { var open = nav.classList.toggle('is-open'); navBtn.setAttribute('aria-expanded', open); });
  }

  /* ---- notification bell ---- */
  $$('[data-bell]').forEach(function (bell) {
    var btn = $('.bell-btn', bell), menu = $('.bell-menu', bell);
    var close = function () { menu.hidden = true; btn.setAttribute('aria-expanded', 'false'); };
    btn.addEventListener('click', function (e) { e.stopPropagation(); var open = menu.hidden; $$('.bell-menu').forEach(function (m) { m.hidden = true; }); menu.hidden = !open; btn.setAttribute('aria-expanded', open); });
    document.addEventListener('click', function (e) { if (!bell.contains(e.target)) close(); });
  });
  document.addEventListener('keydown', function (e) {
    if (e.key !== 'Escape') return;
    $$('.bell-menu').forEach(function (m) { m.hidden = true; });
    if (nav && nav.classList.contains('is-open')) { nav.classList.remove('is-open'); navBtn.setAttribute('aria-expanded', 'false'); }
  });

  /* ---- toasts (messages come from TempData via data attributes) ---- */
  var toastBox = $('#toasts');
  function toast(text, isError) {
    if (!text || !toastBox) return;
    var el = document.createElement('div');
    el.className = 'toast' + (isError ? ' is-error' : '');
    var p = document.createElement('p'); p.textContent = text;
    var b = document.createElement('button'); b.type = 'button'; b.setAttribute('aria-label', 'Dismiss message');
    b.innerHTML = '<svg class="ic"><use href="#i-x"/></svg>';
    var close = function () { el.remove(); };
    b.addEventListener('click', close);
    el.appendChild(p); el.appendChild(b); toastBox.appendChild(el);
    setTimeout(close, isError ? 8000 : 5000);
  }
  window.CampuslyToast = toast;
  if (toastBox) { toast(toastBox.dataset.success, false); toast(toastBox.dataset.error, true); }

  /* ---- confirm dialog for any <form data-confirm="..."> ---- */
  var dlg = $('#confirm-dialog');
  if (dlg && typeof dlg.showModal === 'function') {
    var pending = null;
    document.addEventListener('submit', function (e) {
      var f = e.target;
      if (!f.matches || !f.matches('form[data-confirm]') || f.dataset.confirmed) return;
      e.preventDefault();
      pending = f;
      $('#confirm-title').textContent = f.dataset.confirmTitle || 'Are you sure?';
      $('#confirm-text').textContent = f.dataset.confirm;
      $('#confirm-ok').textContent = f.dataset.confirmOk || 'Confirm';
      dlg.showModal();
    });
    dlg.addEventListener('close', function () {
      if (dlg.returnValue === 'ok' && pending) { pending.dataset.confirmed = '1'; pending.requestSubmit(); }
      pending = null; dlg.returnValue = '';
    });
    dlg.addEventListener('click', function (e) { if (e.target === dlg) dlg.close('cancel'); });
  } else {
    document.addEventListener('submit', function (e) {
      var f = e.target;
      if (f.matches && f.matches('form[data-confirm]') && !window.confirm(f.dataset.confirm)) e.preventDefault();
    });
  }
  /* any other <dialog> closes when its backdrop is clicked */
  $$('dialog.dialog').forEach(function (d) { d.addEventListener('click', function (e) { if (e.target === d && d.id !== 'confirm-dialog') d.close(); }); });

  /* ---- stop double submits and show a spinner on the pressed button ---- */
  document.addEventListener('submit', function (e) {
    if (e.defaultPrevented) return;
    var f = e.target;
    if (f.matches && f.matches('#scan-form, .bell form')) return;
    var btn = f.querySelector('button:not([type="button"])');
    if (btn) setTimeout(function () { btn.classList.add('is-loading'); btn.disabled = true; }, 0);
  });
  window.addEventListener('pageshow', function (e) {
    if (e.persisted) $$('.is-loading').forEach(function (b) { b.classList.remove('is-loading'); b.disabled = false; });
  });

  /* ---- live countdowns ---- */
  $$('.countdown').forEach(function (cd) {
    var target = new Date(cd.dataset.target).getTime();
    var u = { d: $('[data-unit="d"]', cd), h: $('[data-unit="h"]', cd), m: $('[data-unit="m"]', cd), s: $('[data-unit="s"]', cd) };
    var tick = function () {
      var s = Math.floor(Math.max(0, target - Date.now()) / 1000);
      u.d.textContent = Math.floor(s / 86400);
      u.h.textContent = String(Math.floor(s % 86400 / 3600)).padStart(2, '0');
      u.m.textContent = String(Math.floor(s % 3600 / 60)).padStart(2, '0');
      u.s.textContent = String(s % 60).padStart(2, '0');
    };
    tick(); setInterval(tick, 1000);
  });

  /* ---- animated numbers (count up once when scrolled into view) ---- */
  var counters = $$('b[data-count]');
  function countUp(el) {
    var end = parseInt(el.dataset.count, 10) || 0, suffix = el.dataset.suffix || '';
    if (reduceMotion || end === 0) { el.textContent = end + suffix; return; }
    var start = performance.now(), dur = 1000;
    (function frame(now) {
      var p = Math.min(1, (now - start) / dur);
      el.textContent = Math.round(end * (1 - Math.pow(1 - p, 3))) + suffix;
      if (p < 1) requestAnimationFrame(frame);
    })(start);
  }
  if ('IntersectionObserver' in window) {
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (en) { if (en.isIntersecting) { countUp(en.target); io.unobserve(en.target); } });
    }, { threshold: .3 });
    counters.forEach(function (c) { io.observe(c); });
  } else counters.forEach(countUp);

  /* ---- tabs with arrow-key support ---- */
  $$('[data-tabs]').forEach(function (root) {
    var tabs = $$('[role="tab"]', root);
    var select = function (tab) {
      tabs.forEach(function (t) {
        var on = t === tab;
        t.setAttribute('aria-selected', on); t.tabIndex = on ? 0 : -1;
        document.getElementById(t.getAttribute('aria-controls')).hidden = !on;
      });
      tab.focus();
    };
    tabs.forEach(function (t, i) {
      t.addEventListener('click', function () { select(t); });
      t.addEventListener('keydown', function (e) {
        if (e.key === 'ArrowRight') select(tabs[(i + 1) % tabs.length]);
        if (e.key === 'ArrowLeft') select(tabs[(i - 1 + tabs.length) % tabs.length]);
      });
    });
  });

  /* ---- searchable / filterable tables (organizer, participants, admin) ---- */
  var search = $('#table-search'), filter = $('#table-filter');
  var rows = $$('table.data tbody tr[data-search]');
  if (search && rows.length) {
    var empty = $('#table-empty');
    var run = function () {
      var q = search.value.trim().toLowerCase(), f = filter ? filter.value : 'all', n = 0;
      rows.forEach(function (r) {
        var ok = (!q || r.dataset.search.indexOf(q) !== -1) && (f === 'all' || r.dataset.group === f);
        r.hidden = !ok; if (ok) n++;
      });
      if (empty) empty.hidden = n !== 0;
    };
    search.addEventListener('input', run); if (filter) filter.addEventListener('change', run); run();
  }

  /* ---- login helpers ---- */
  $$('.demo-btn').forEach(function (b) {
    b.addEventListener('click', function () {
      $('#Email').value = b.dataset.email; $('#Password').value = b.dataset.password; $('#Password').focus();
    });
  });
  var pw = $('.pw-toggle');
  if (pw) pw.addEventListener('click', function () {
    var input = $('#Password'), show = input.type === 'password';
    input.type = show ? 'text' : 'password';
    pw.setAttribute('aria-pressed', show); pw.setAttribute('aria-label', show ? 'Hide password' : 'Show password');
  });

  /* ---- character counter on long text fields ---- */
  $$('textarea[data-count]').forEach(function (t) {
    var out = t.parentNode.querySelector('.char-count'), max = +t.dataset.count;
    var upd = function () { out.textContent = t.value.length + ' of ' + max + ' characters'; };
    t.addEventListener('input', upd); upd();
  });

  /* ---- share button (native share sheet, or copy the link) ---- */
  $$('[data-share]').forEach(function (b) {
    b.addEventListener('click', function () {
      var data = { title: b.dataset.title, url: b.dataset.url };
      if (navigator.share) { navigator.share(data).catch(function () {}); return; }
      var done = function () { toast('Link copied to the clipboard.', false); };
      if (navigator.clipboard && navigator.clipboard.writeText) navigator.clipboard.writeText(data.url).then(done, function () { window.prompt('Copy this link:', data.url); });
      else window.prompt('Copy this link:', data.url);
    });
  });
})();
