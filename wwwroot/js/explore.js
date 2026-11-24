/* CAMPUSLY - Explore events: search, category, date, availability, sorting and grid/list view. All client-side. */
(function () {
  'use strict';
  var grid = document.getElementById('ex-grid');
  if (!grid) return;
  var $ = function (s) { return document.querySelector(s); };
  var cards = Array.prototype.slice.call(grid.querySelectorAll('.event-card'));
  var search = $('#ex-search'), dateSel = $('#ex-date'), sortSel = $('#ex-sort'), openOnly = $('#ex-open'), showPast = $('#ex-past');
  var chips = Array.prototype.slice.call(document.querySelectorAll('#ex-chips .chip-btn'));
  var count = $('#ex-count'), empty = $('#ex-empty'), cat = 'all';

  // Preselect a category from ?category=Technical (links from the home page)
  var wanted = new URLSearchParams(location.search).get('category');
  if (wanted && chips.some(function (c) { return c.dataset.cat === wanted; })) cat = wanted;
  chips.forEach(function (c) { c.setAttribute('aria-pressed', c.dataset.cat === cat); });

  function inRange(card, mode) {
    if (mode === 'any') return true;
    var d = new Date(card.dataset.date), now = new Date();
    var startOfToday = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    var days = (d - startOfToday) / 86400000;
    if (mode === 'today') return days >= 0 && days < 1;
    if (mode === 'week') return days >= 0 && days < 7;
    if (mode === 'month') return days >= 0 && days < 30;
    return true;
  }

  function apply() {
    var q = search.value.trim().toLowerCase(), shown = 0;
    cards.forEach(function (c) {
      var ok = (!q || c.dataset.search.indexOf(q) !== -1)
        && (cat === 'all' || (cat === 'mine' ? c.dataset.mine === '1' : c.dataset.category === cat))
        && inRange(c, dateSel.value)
        && (!openOnly.checked || c.dataset.open === '1')
        && (showPast.checked || c.dataset.past === '0');
      c.hidden = !ok; if (ok) shown++;
    });
    var key = sortSel.value;
    cards.slice().sort(function (a, b) {
      if (key === 'pop') return (+b.dataset.pop) - (+a.dataset.pop);
      if (key === 'seats') return (+b.dataset.seats) - (+a.dataset.seats);
      if (key === 'new') return (+b.dataset.created) - (+a.dataset.created);
      var pa = a.dataset.past === '1', pb = b.dataset.past === '1';      // upcoming first, then past (newest first)
      if (pa !== pb) return pa ? 1 : -1;
      var da = new Date(a.dataset.date), db = new Date(b.dataset.date);
      return pa ? db - da : da - db;
    }).forEach(function (c) { grid.appendChild(c); });
    count.textContent = shown + (shown === 1 ? ' event' : ' events');
    empty.hidden = shown !== 0; grid.hidden = shown === 0;
  }

  search.addEventListener('input', apply);
  [dateSel, sortSel, openOnly, showPast].forEach(function (el) { el.addEventListener('change', apply); });
  chips.forEach(function (b) { b.addEventListener('click', function () { cat = b.dataset.cat; chips.forEach(function (x) { x.setAttribute('aria-pressed', x === b); }); apply(); }); });
  $('#ex-reset').addEventListener('click', function () {
    search.value = ''; cat = 'all'; dateSel.value = 'any'; openOnly.checked = false; showPast.checked = false;
    chips.forEach(function (x) { x.setAttribute('aria-pressed', x.dataset.cat === 'all'); }); apply(); search.focus();
  });

  // grid / list layout, remembered on this device
  var views = Array.prototype.slice.call(document.querySelectorAll('.seg-btn'));
  function setView(v) { grid.classList.toggle('is-list', v === 'list'); views.forEach(function (b) { b.setAttribute('aria-pressed', b.dataset.view === v); }); try { localStorage.setItem('exview', v); } catch (e) {} }
  views.forEach(function (b) { b.addEventListener('click', function () { setView(b.dataset.view); }); });
  var saved = 'grid'; try { saved = localStorage.getItem('exview') || 'grid'; } catch (e) {}
  setView(saved);
  apply();
})();
