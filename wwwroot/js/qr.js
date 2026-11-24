/* CAMPUSLY - QR passes. Draws a real QR code (via the bundled qrcode-generator library) that encodes
   "CAMPUSLY:<secret token>". The organizer's scanner decodes this text and the server checks the token. */
(function () {
  'use strict';
  if (typeof qrcode === 'undefined') return;

  function render(el, text, size) {
    var qr = qrcode(0, 'M');                 // type 0 = pick the smallest version that fits
    qr.addData(text); qr.make();
    var n = qr.getModuleCount(), quiet = 2, total = n + quiet * 2, d = '';
    for (var r = 0; r < n; r++) for (var c = 0; c < n; c++) if (qr.isDark(r, c)) d += 'M' + (c + quiet) + ' ' + (r + quiet) + 'h1v1h-1z';
    el.innerHTML = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ' + total + ' ' + total + '" shape-rendering="crispEdges" role="img" aria-label="QR code">' +
      '<rect width="' + total + '" height="' + total + '" fill="#fff"/><path d="' + d + '" fill="#000"/></svg>';
    el.style.width = (size || 240) + 'px';
    el.dataset.rendered = text;
  }
  window.CampuslyQR = { render: render };

  Array.prototype.forEach.call(document.querySelectorAll('[data-qr]'), function (el) { render(el, el.dataset.qr, +el.dataset.size); });

  // Pop-up pass on the "My activities" page
  var dlg = document.getElementById('pass-dialog');
  if (dlg) {
    Array.prototype.forEach.call(document.querySelectorAll('[data-qr-open]'), function (b) {
      b.addEventListener('click', function () {
        document.getElementById('pd-title').textContent = b.dataset.title;
        document.getElementById('pd-when').textContent = b.dataset.when;
        document.getElementById('pd-code').textContent = b.dataset.code;
        var holder = document.getElementById('pd-qr'); render(holder, b.dataset.token, +holder.dataset.size);
        dlg.showModal();
      });
    });
  }
})();
