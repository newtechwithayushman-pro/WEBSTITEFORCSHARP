/* CAMPUSLY - charts. Each <canvas data-chart="bar|line|hbar|doughnut" data-source="#json" data-names="A,B"> is drawn
   with Chart.js from JSON that the server computed from real database rows. */
(function () {
  'use strict';
  if (typeof Chart === 'undefined') return;
  var reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  var CAT = { Technical: '#4da3ff', Robotics: '#2fd1b5', Hackathons: '#ffc247', Cultural: '#f472a8', Sports: '#ff8a4c', Workshops: '#a3e05b', Seminars: '#94a3b8', Entrepreneurship: '#ff6b6b', Arts: '#c4a1ff' };
  var PALETTE = ['#ffc247', '#35d0ba', '#4da3ff', '#f472a8', '#a3e05b', '#ff8a4c'];
  var instances = [];

  function css(name) { return getComputedStyle(document.documentElement).getPropertyValue(name).trim(); }

  function build(canvas) {
    var src = document.querySelector(canvas.dataset.source);
    if (!src) return null;
    var d = JSON.parse(src.textContent);
    var names = (canvas.dataset.names || 'Value').split(',');
    var type = canvas.dataset.chart;
    var ink = css('--ink-2'), grid = css('--line'), accent = css('--accent'), teal = css('--teal'), surface = css('--surface');
    var cfg;

    if (type === 'doughnut') {
      var colors = d.labels.map(function (l, i) { return CAT[l] || PALETTE[i % PALETTE.length]; });
      cfg = { type: 'doughnut', data: { labels: d.labels, datasets: [{ data: d.a, backgroundColor: colors, borderColor: surface, borderWidth: 3 }] },
        options: { maintainAspectRatio: false, cutout: '66%', plugins: { legend: { position: 'right', labels: { color: ink, boxWidth: 10, boxHeight: 10, usePointStyle: true } } } } };
    } else {
      var ds = [{ label: names[0], data: d.a }];
      if (names.length > 1 && d.b && d.b.length) ds.push({ label: names[1], data: d.b });
      var colorsFor = [accent, teal];
      ds.forEach(function (s, i) {
        s.borderColor = colorsFor[i]; s.backgroundColor = colorsFor[i];
        if (type === 'line') { s.tension = .35; s.pointRadius = 3; s.pointBackgroundColor = colorsFor[i]; s.borderWidth = 2.5; s.fill = i === 0 ? 'origin' : false; if (i === 0) s.backgroundColor = colorsFor[i] + '22'; }
        else { s.borderRadius = 6; s.borderSkipped = false; s.maxBarThickness = 34; }
      });
      var horizontal = type === 'hbar';
      var scaleCommon = { ticks: { color: ink, precision: 0 }, grid: { color: grid } };
      cfg = { type: type === 'line' ? 'line' : 'bar', data: { labels: d.labels, datasets: ds },
        options: { maintainAspectRatio: false, indexAxis: horizontal ? 'y' : 'x', interaction: { mode: 'index', intersect: false },
          plugins: { legend: { display: ds.length > 1, labels: { color: ink, usePointStyle: true, boxWidth: 8, boxHeight: 8 } }, tooltip: { backgroundColor: surface, titleColor: css('--ink'), bodyColor: ink, borderColor: grid, borderWidth: 1, padding: 10 } },
          scales: horizontal ? { x: Object.assign({ beginAtZero: true }, scaleCommon), y: { ticks: { color: ink }, grid: { display: false } } }
                              : { x: { ticks: { color: ink, maxRotation: d.labels.length > 6 ? 40 : 0, autoSkip: false }, grid: { display: false } }, y: Object.assign({ beginAtZero: true }, scaleCommon) } } };
    }
    cfg.options.animation = reduceMotion ? false : { duration: 800 };
    cfg.options.responsive = true;
    return new Chart(canvas, cfg);
  }

  function render() {
    instances.forEach(function (c) { c.destroy(); }); instances = [];
    document.querySelectorAll('canvas[data-chart]').forEach(function (canvas) {
      var chart = build(canvas); if (chart) instances.push(chart);
      var sk = canvas.parentNode.querySelector('.skeleton'); if (sk) sk.remove();
    });
  }

  Chart.defaults.font.family = getComputedStyle(document.body).fontFamily;
  Chart.defaults.font.size = 12;
  // Draw when a chart scrolls into view so the entrance animation is seen
  render();
  document.addEventListener('themechange', render);
  window.CampuslyCharts = { count: function () { return instances.length; } };
})();
