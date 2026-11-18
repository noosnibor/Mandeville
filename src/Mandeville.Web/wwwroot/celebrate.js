// Builds a birthday / anniversary card image and hands it to WhatsApp.
// Phones: the share sheet opens with the picture attached (the admin picks the group).
// Computers: the picture is downloaded, the caption is copied and WhatsApp Web opens.
window.celebrate = (function () {
    var themes = {
        birthday: { from: '#0f766e', to: '#5eead4', emoji: '\uD83C\uDF82' },
        anniversary: { from: '#9f1239', to: '#fb7185', emoji: '\uD83D\uDC8D' }
    };

    function wrap(ctx, text, maxWidth) {
        var words = text.split(' '), lines = [], line = '';
        words.forEach(function (w) {
            var test = line ? line + ' ' + w : w;
            if (ctx.measureText(test).width > maxWidth && line) { lines.push(line); line = w; } else { line = test; }
        });
        if (line) lines.push(line);
        return lines;
    }

    function drawGradient(d) {
        var size = 1080, c = document.createElement('canvas');
        c.width = size; c.height = size;
        var ctx = c.getContext('2d'), t = themes[d.kind] || themes.birthday;

        var g = ctx.createLinearGradient(0, 0, size, size);
        g.addColorStop(0, t.from); g.addColorStop(1, t.to);
        ctx.fillStyle = g; ctx.fillRect(0, 0, size, size);

        // soft circles for decoration
        ctx.fillStyle = 'rgba(255,255,255,0.10)';
        ctx.beginPath(); ctx.arc(960, 120, 260, 0, Math.PI * 2); ctx.fill();
        ctx.beginPath(); ctx.arc(100, 980, 300, 0, Math.PI * 2); ctx.fill();

        ctx.textAlign = 'center'; ctx.fillStyle = '#ffffff';
        ctx.font = '170px "Segoe UI Emoji","Apple Color Emoji","Noto Color Emoji",sans-serif';
        ctx.fillText(t.emoji, size / 2, 330);

        ctx.font = '600 46px "Segoe UI",Arial,sans-serif';
        ctx.globalAlpha = 0.92;
        ctx.fillText(d.heading, size / 2, 430);
        ctx.globalAlpha = 1;

        var fs = 104, lines;
        do {
            ctx.font = '800 ' + fs + 'px "Segoe UI",Arial,sans-serif';
            lines = wrap(ctx, d.name, 900);
            fs -= 6;
        } while (lines.length > 3 && fs > 50);
        var y = 560;
        lines.forEach(function (l) { ctx.fillText(l, size / 2, y); y += fs + 30; });

        ctx.font = '500 44px "Segoe UI",Arial,sans-serif';
        ctx.globalAlpha = 0.95;
        ctx.fillText(d.detail, size / 2, Math.max(y + 20, 800));
        ctx.globalAlpha = 0.85;
        ctx.font = '600 32px "Segoe UI",Arial,sans-serif';
        ctx.fillText(d.church.toUpperCase().split('').join(String.fromCharCode(8202)), size / 2, 990);
        ctx.globalAlpha = 1;
        return c;
    }

    // Photo card: the picture on top and a name plate underneath. Falls back to the gradient card if the picture cannot load.
    function loadImage(src) {
        return new Promise(function (resolve) {
            if (!src) { resolve(null); return; }
            var img = new Image();
            img.onload = function () { resolve(img); };
            img.onerror = function () { resolve(null); };
            img.src = src;
        });
    }

    // The person's photo (a small square picture) drawn as a circle with a gold ring
    function drawAvatar(ctx, img, cx, cy, r) {
        ctx.save();
        ctx.shadowColor = 'rgba(0,0,0,0.35)'; ctx.shadowBlur = 26; ctx.shadowOffsetY = 8;
        ctx.fillStyle = '#c9a24d';
        ctx.beginPath(); ctx.arc(cx, cy, r + 10, 0, Math.PI * 2); ctx.fill();
        ctx.restore();
        ctx.fillStyle = '#ffffff';
        ctx.beginPath(); ctx.arc(cx, cy, r + 4, 0, Math.PI * 2); ctx.fill();
        ctx.save();
        ctx.beginPath(); ctx.arc(cx, cy, r, 0, Math.PI * 2); ctx.clip();
        var side = Math.min(img.width, img.height);
        ctx.drawImage(img, (img.width - side) / 2, (img.height - side) / 2, side, side, cx - r, cy - r, r * 2, r * 2);
        ctx.restore();
    }

    async function draw(d) {
        var img = await loadImage(d.image);
        if (!img) return drawGradient(d);
        var avatar = await loadImage(d.avatar);

        var width = 1080, photoH = Math.round(img.height * width / img.width);
        var maxPhoto = 1500; // keep very tall pictures a sensible size for WhatsApp
        var sy = 0, sh = img.height;
        if (photoH > maxPhoto) { sh = Math.round(img.height * maxPhoto / photoH); photoH = maxPhoto; }

        var beside = !!avatar && d.pos === 'beside';
        var r = d.size === 'sm' ? 95 : 135;       // photo radius for the corner positions
        var rb = 90;                              // photo radius beside the message

        // The plate under the picture carries the message. Text area: the whole plate, or the part to the right of the photo.
        var left = beside ? 60 + rb * 2 + 50 : 70, right = width - 70, maxW = right - left;
        var font = '34px Georgia,"Times New Roman",serif', lineH = 48, gap = 24;
        var measure = document.createElement('canvas').getContext('2d');
        measure.font = font;
        var rows = [];                            // each row is a line of text, or null for the space between paragraphs
        (d.caption || '').split(/\r?\n/).forEach(function (para) {
            if (!para.trim()) { if (rows.length && rows[rows.length - 1] !== null) rows.push(null); return; }
            wrap(measure, para.trim(), maxW).forEach(function (l) { rows.push(l); });
        });
        while (rows.length && rows[rows.length - 1] === null) rows.pop();
        var textH = rows.reduce(function (h, l) { return h + (l === null ? gap : lineH); }, 0);
        var plateH = Math.max(textH + 100, beside ? rb * 2 + 100 : 160);

        var c = document.createElement('canvas');
        c.width = width; c.height = photoH + plateH;
        var ctx = c.getContext('2d');
        ctx.drawImage(img, 0, sy, img.width, sh, 0, 0, width, photoH);

        var g = ctx.createLinearGradient(0, photoH, 0, photoH + plateH);
        g.addColorStop(0, '#ffffff'); g.addColorStop(1, '#fbf6f0');
        ctx.fillStyle = g; ctx.fillRect(0, photoH, width, plateH);
        ctx.fillStyle = '#c9a24d'; ctx.fillRect(0, photoH, width, 8);

        ctx.textAlign = 'left'; ctx.fillStyle = '#5b4a3a'; ctx.font = font;
        var y = photoH + (plateH - textH) / 2 + lineH * 0.72;
        rows.forEach(function (l) {
            if (l === null) { y += gap; return; }
            ctx.fillText(l, left, y);
            y += lineH;
        });

        // the photo of the person
        if (avatar) {
            if (beside) drawAvatar(ctx, avatar, 60 + rb, photoH + plateH / 2 + 4, rb);
            else if (d.pos === 'tl') drawAvatar(ctx, avatar, 34 + r, 34 + r, r);
            else drawAvatar(ctx, avatar, width - 34 - r, 34 + r, r);
        }
        return c;
    }

    // Reads the chosen picture file, crops it to a square and shrinks it to a small JPEG (returned as a data address).
    function resizePhoto(file, size) {
        return new Promise(function (resolve) {
            if (!file || !/^image\//.test(file.type)) { resolve(null); return; }
            var reader = new FileReader();
            reader.onerror = function () { resolve(null); };
            reader.onload = function () {
                var img = new Image();
                img.onerror = function () { resolve(null); };
                img.onload = function () {
                    var side = Math.min(img.width, img.height), c = document.createElement('canvas');
                    c.width = size; c.height = size;
                    c.getContext('2d').drawImage(img, (img.width - side) / 2, (img.height - side) / 2, side, side, 0, 0, size, size);
                    resolve(c.toDataURL('image/jpeg', 0.82));
                };
                img.src = reader.result;
            };
            reader.readAsDataURL(file);
        });
    }

    function toBlob(canvas) {
        return new Promise(function (resolve) { canvas.toBlob(resolve, 'image/png'); });
    }

    async function copyBlob(blob) {
        try {
            if (!window.ClipboardItem || !navigator.clipboard || !navigator.clipboard.write) return false;
            await navigator.clipboard.write([new ClipboardItem({ 'image/png': blob })]);
            return true;
        } catch (e) { return false; }
    }

    function readData() {
        var el = document.getElementById('cel-data');
        var box = document.getElementById('cel-extra');
        if (!el) return null;
        return {
            kind: el.dataset.kind, heading: el.dataset.heading, name: el.dataset.name,
            detail: el.dataset.detail, church: el.dataset.church, file: el.dataset.file, image: el.dataset.image,
            pos: el.dataset.pos, size: el.dataset.size,
            avatar: (document.getElementById('cel-avatar') || {}).src || '',
            // The message printed on the card
            caption: box ? box.value : ''
        };
    }

    function markSent() {
        var b = document.getElementById('cel-mark');
        if (b) b.click();
    }

    return {
        /// Called by the pages after a picture is chosen: returns a small square JPEG as a data address (or null) and clears the file box.
        pickPhoto: async function (inputId) {
            var box = document.getElementById(inputId);
            if (!box || !box.files || !box.files.length) return null;
            var result = await resizePhoto(box.files[0], 320);
            box.value = '';
            return result;
        },

        /// Called after a card image is chosen: a JPEG or PNG up to 2 MB, shrunk to 1000 pixels wide. Returns a data address, 'toolarge', 'badtype', or null.
        pickImage: async function (inputId) {
            var box = document.getElementById(inputId);
            if (!box || !box.files || !box.files.length) return null;
            var file = box.files[0];
            box.value = '';
            if (file.type !== 'image/jpeg' && file.type !== 'image/png') return 'badtype';
            if (file.size > 2 * 1024 * 1024) return 'toolarge';
            var src = await new Promise(function (resolve) {
                var r = new FileReader();
                r.onload = function () { resolve(r.result); };
                r.onerror = function () { resolve(null); };
                r.readAsDataURL(file);
            });
            var img = await loadImage(src);
            if (!img) return 'badtype';
            var w = Math.min(1000, img.width), h = Math.round(img.height * w / img.width);
            var c = document.createElement('canvas');
            c.width = w; c.height = h;
            var ctx = c.getContext('2d');
            ctx.fillStyle = '#ffffff'; ctx.fillRect(0, 0, w, h);
            ctx.drawImage(img, 0, 0, w, h);
            return c.toDataURL('image/jpeg', 0.82);
        },

        send: async function () {
            var d = readData();
            if (!d) return;
            var blob = await toBlob(await draw(d));
            var file = new File([blob], d.file, { type: 'image/png' });

            // Phone / tablet: native share sheet with the picture attached.
            try {
                if (navigator.canShare && navigator.canShare({ files: [file] })) {
                    await navigator.share({ files: [file] });
                    markSent();
                    return;
                }
            } catch (e) {
                if (e && e.name === 'AbortError') return; // the admin closed the share sheet
            }

            // Computer: copy the picture to the clipboard (download it when the browser will not allow that) and open WhatsApp Web.
            // The admin pastes the picture into the group (the message is printed on the card).
            if (!(await copyBlob(blob))) {
                var url = URL.createObjectURL(blob), a = document.createElement('a');
                a.href = url; a.download = d.file; document.body.appendChild(a); a.click(); a.remove();
                setTimeout(function () { URL.revokeObjectURL(url); }, 4000);
            }
            window.open('https://web.whatsapp.com/', '_blank');
            markSent();
        },

        /// Copies the card picture to the clipboard. Returns true when it worked.
        copyImage: async function () {
            var d = readData();
            if (!d) return false;
            return await copyBlob(await toBlob(await draw(d)));
        },

        download: async function () {
            var d = readData();
            if (!d) return;
            var blob = await toBlob(await draw(d));
            var url = URL.createObjectURL(blob), a = document.createElement('a');
            a.href = url; a.download = d.file; document.body.appendChild(a); a.click(); a.remove();
            setTimeout(function () { URL.revokeObjectURL(url); }, 4000);
        }
    };
})();
