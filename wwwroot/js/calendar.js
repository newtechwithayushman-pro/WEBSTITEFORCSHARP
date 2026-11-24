/* CAMPUSLY - month calendar on the student dashboard. Marks days with the student's registered events. */
(function () {
  'use strict';
  var root = document.querySelector('[data-calendar]');
  if (!root) return;
  var events = JSON.parse(document.querySelector(root.dataset.source).textContent).map(function (e) { e.d = new Date(e.date); return e; });
  var list = document.getElementById('cal-list');
  var view = new Date(); view.setDate(1);
  var selected = null;
  var DOW = ['M', 'T', 'W', 'T', 'F', 'S', 'S'];
  var key = function (d) { return d.getFullYear() + '-' + d.getMonth() + '-' + d.getDate(); };
  var byDay = {};
  events.forEach(function (e) { (byDay[key(e.d)] = byDay[key(e.d)] || []).push(e); });

  function showList() {
    var items, heading;
    if (selected && byDay[selected]) { items = byDay[selected]; heading = null; }
    else { items = events.filter(function (e) { return e.d >= new Date(); }).sort(function (a, b) { return a.d - b.d; }).slice(0, 3); heading = items.length ? 'Coming up' : 'No upcoming events in your calendar.'; }
    list.innerHTML = '';
    if (heading) { var h = document.createElement('small'); h.className = 'muted'; h.textContent = heading; list.appendChild(h); }
    items.forEach(function (e) {
      var a = document.createElement('a'); a.href = '/Events/Details/' + e.id;
      var s = document.createElement('strong'); s.textContent = e.title;
      var t = document.createElement('small'); t.textContent = e.d.toLocaleDateString(undefined, { weekday: 'short', day: 'numeric', month: 'short' }) + ', ' + e.d.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' }) + ' - ' + e.venue;
      a.appendChild(s); a.appendChild(t); list.appendChild(a);
    });
  }

  function draw() {
    var y = view.getFullYear(), m = view.getMonth(), today = key(new Date());
    var first = (new Date(y, m, 1).getDay() + 6) % 7, days = new Date(y, m + 1, 0).getDate();
    root.innerHTML = '';
    var head = document.createElement('div'); head.className = 'cal-head';
    var prev = document.createElement('button'); prev.type = 'button'; prev.className = 'icon-btn'; prev.setAttribute('aria-label', 'Previous month'); prev.innerHTML = '<svg class="ic flip"><use href="#i-arrow-right"/></svg>';
    var next = document.createElement('button'); next.type = 'button'; next.className = 'icon-btn'; next.setAttribute('aria-label', 'Next month'); next.innerHTML = '<svg class="ic"><use href="#i-arrow-right"/></svg>';
    var title = document.createElement('strong'); title.textContent = view.toLocaleDateString(undefined, { month: 'long', year: 'numeric' });
    head.appendChild(prev); head.appendChild(title); head.appendChild(next); root.appendChild(head);
    prev.onclick = function () { view = new Date(y, m - 1, 1); draw(); };
    next.onclick = function () { view = new Date(y, m + 1, 1); draw(); };
    var grid = document.createElement('div'); grid.className = 'cal-grid';
    DOW.forEach(function (d) { var c = document.createElement('div'); c.className = 'cal-dow'; c.textContent = d; grid.appendChild(c); });
    for (var i = 0; i < first; i++) grid.appendChild(document.createElement('span'));
    for (var d = 1; d <= days; d++) {
      var k = y + '-' + m + '-' + d, cell = document.createElement('button'); cell.type = 'button';
      cell.className = 'cal-day' + (k === today ? ' is-today' : '') + (byDay[k] ? ' has-event' : '') + (k === selected ? ' is-selected' : '');
      cell.textContent = d;
      if (byDay[k]) { cell.setAttribute('aria-label', d + ' ' + title.textContent + ': ' + byDay[k].length + ' event(s)'); (function (kk) { cell.onclick = function () { selected = selected === kk ? null : kk; draw(); showList(); }; })(k); }
      else cell.tabIndex = -1;
      grid.appendChild(cell);
    }
    root.appendChild(grid);
  }
  draw(); showList();
})();
