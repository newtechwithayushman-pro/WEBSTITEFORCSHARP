/* CAMPUSLY - organizer check-in. Three ways in: live camera, an uploaded image, or a typed code.
   Every path ends in the same server call (POST /Organizer/CheckIn) that validates the token. */
(function () {
  'use strict';
  var root = document.getElementById('scan-root');
  if (!root) return;
  var $ = function (s) { return document.querySelector(s); };
  var eventId = root.dataset.eventId, url = root.dataset.url;
  var csrf = root.querySelector('input[name="__RequestVerificationToken"]').value;
  var view = $('#scan-view'), video = $('#scan-video'), canvas = $('#scan-canvas'), ctx = canvas.getContext('2d', { willReadFrequently: true });
  var startBtn = $('#scan-start'), stopBtn = $('#scan-stop'), result = $('#scan-result'), help = $('#scan-help');
  var stream = null, timer = null, busy = false, lastText = '', lastAt = 0;

  function banner(kind, title, sub) {
    result.hidden = false; result.className = 'scan-result ' + kind;
    var icon = kind === 'ok' ? 'circle-check' : 'triangle-alert';
    result.innerHTML = '<svg class="ic"><use href="#i-' + icon + '"/></svg><div></div>';
    var box = result.querySelector('div'); box.textContent = title;
    if (sub) { var s = document.createElement('small'); s.textContent = sub; box.appendChild(s); }
  }

  function logEntry(name, time) {
    var list = $('#scan-log'), empty = list.querySelector('.log-empty'); if (empty) empty.remove();
    var li = document.createElement('li');
    li.innerHTML = '<span class="n-ic n-attendance"><svg class="ic"><use href="#i-user-check"/></svg></span><div><strong></strong><small></small></div>';
    li.querySelector('strong').textContent = name; li.querySelector('small').textContent = time + ' via QR';
    list.insertBefore(li, list.firstChild);
    while (list.children.length > 8) list.lastChild.remove();
  }

  function send(text) {
    if (busy) return Promise.resolve();
    busy = true;
    var body = new URLSearchParams({ eventId: eventId, token: text });
    return fetch(url, { method: 'POST', credentials: 'same-origin', headers: { 'RequestVerificationToken': csrf, 'Content-Type': 'application/x-www-form-urlencoded' }, body: body })
      .then(function (r) { return r.json(); })
      .then(function (d) {
        if (d.ok) {
          banner('ok', d.message, [d.department, d.time].filter(Boolean).join(' - '));
          $('#scan-in').textContent = d.checkedIn; $('#scan-total').textContent = d.total;
          $('#scan-bar').style.width = (d.total ? Math.round(100 * d.checkedIn / d.total) : 0) + '%';
          logEntry(d.student, d.time);
          if (navigator.vibrate) navigator.vibrate(60);
        } else banner(d.code === 'duplicate' ? 'warn' : 'err', d.message);
      })
      .catch(function () { banner('err', 'Could not reach the server. Check your connection and try again.'); })
      .then(function () { busy = false; });
  }

  /* ---- camera ---- */
  function loop() {
    if (!stream) return;
    if (video.readyState === video.HAVE_ENOUGH_DATA && video.videoWidth) {
      var w = Math.min(640, video.videoWidth), h = Math.round(w * video.videoHeight / video.videoWidth);
      canvas.width = w; canvas.height = h; ctx.drawImage(video, 0, 0, w, h);
      var code = window.jsQR && jsQR(ctx.getImageData(0, 0, w, h).data, w, h, { inversionAttempts: 'dontInvert' });
      if (code && code.data) {
        var now = Date.now();
        if (code.data !== lastText || now - lastAt > 4000) { lastText = code.data; lastAt = now; send(code.data); }
      }
    }
    timer = setTimeout(loop, 200);
  }
  function stop() {
    if (stream) stream.getTracks().forEach(function (t) { t.stop(); });
    stream = null; clearTimeout(timer); video.srcObject = null;
    view.classList.remove('is-live'); startBtn.hidden = false; stopBtn.hidden = true;
  }
  startBtn.addEventListener('click', function () {
    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
      banner('err', 'Camera access isn\'t available here.', 'It needs HTTPS or localhost. Use the code box or upload an image instead.'); return;
    }
    navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment' }, audio: false }).then(function (s) {
      stream = s; video.srcObject = s; video.play();
      view.classList.add('is-live'); startBtn.hidden = true; stopBtn.hidden = false; result.hidden = true; loop();
    }).catch(function (e) {
      banner('err', e && e.name === 'NotAllowedError' ? 'Camera permission was denied.' : 'No camera could be started.', 'Use the code box or upload an image instead.');
    });
  });
  stopBtn.addEventListener('click', stop);
  window.addEventListener('pagehide', stop);

  /* ---- image upload ---- */
  $('#scan-file').addEventListener('change', function (e) {
    var file = e.target.files && e.target.files[0]; if (!file) return;
    var img = new Image();
    img.onload = function () {
      var scale = Math.min(1, 1000 / Math.max(img.width, img.height));
      canvas.width = Math.round(img.width * scale); canvas.height = Math.round(img.height * scale);
      ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
      var data = ctx.getImageData(0, 0, canvas.width, canvas.height);
      var code = jsQR(data.data, data.width, data.height, { inversionAttempts: 'attemptBoth' });
      URL.revokeObjectURL(img.src);
      if (code && code.data) send(code.data); else banner('err', 'No QR code found in that image.', 'Try a sharper, closer picture of the pass.');
    };
    img.onerror = function () { banner('err', 'That file could not be read as an image.'); };
    img.src = URL.createObjectURL(file); e.target.value = '';
  });

  /* ---- typed code ---- */
  $('#scan-form').addEventListener('submit', function (e) {
    e.preventDefault();
    var input = $('#scan-code'), text = input.value.trim();
    if (!text) { banner('err', 'Type or paste a pass code first.'); input.focus(); return; }
    send(text).then(function () { input.value = ''; input.focus(); });
  });
})();
