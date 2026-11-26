// Sign-in page behaviour. The page is static SSR, so this is plain JS using event delegation.
(function () {
    'use strict';
    // A plain name like jane.brown (the app adds the domain) or a full email address
    var USERNAME = /^[A-Za-z0-9]+([._-][A-Za-z0-9]+)*(@[A-Za-z0-9.-]+\.[A-Za-z]{2,})?$/;

    function setError(input, msg) {
        var field = input.closest('.field');
        if (!field) return;
        var out = field.querySelector('.field-error');
        if (msg) {
            if (!out) { out = document.createElement('div'); out.className = 'field-error'; out.setAttribute('role', 'alert'); field.appendChild(out); }
            out.textContent = msg;
            input.setAttribute('aria-invalid', 'true');
        } else {
            if (out) out.remove();
            input.removeAttribute('aria-invalid');
        }
    }

    function check(input) {
        var v = input.value.trim();
        if (input.name.indexOf('Email') > -1) {
            if (!v) return 'Enter your username, like firstname.lastname.';
            if (!USERNAME.test(v)) return 'Use letters and numbers, like firstname.lastname.';
        } else if (input.type === 'password' || input.dataset.pw) {
            if (!input.value) return 'Enter your password.';
        }
        return '';
    }

    document.addEventListener('click', function (e) {
        var t = e.target.closest('[data-pw-toggle]');
        if (!t) return;
        var input = document.getElementById(t.getAttribute('data-pw-toggle'));
        if (!input) return;
        var show = input.type === 'password';
        input.type = show ? 'text' : 'password';
        input.dataset.pw = '1';
        t.setAttribute('aria-pressed', show ? 'true' : 'false');
        t.setAttribute('aria-label', show ? 'Hide password' : 'Show password');
        t.classList.toggle('on', show);
        input.focus();
    });

    document.addEventListener('focusout', function (e) {
        var i = e.target;
        if (i && i.matches && i.matches('.auth-form [data-validate]')) setError(i, check(i));
    });
    document.addEventListener('input', function (e) {
        var i = e.target;
        if (i && i.matches && i.matches('.auth-form [data-validate][aria-invalid]')) setError(i, check(i));
    });

    document.addEventListener('submit', function (e) {
        var form = e.target.closest ? e.target.closest('.auth-form') : null;
        if (!form) return;
        var bad = null;
        form.querySelectorAll('[data-validate]').forEach(function (i) {
            var m = check(i);
            setError(i, m);
            if (m && !bad) bad = i;
        });
        if (bad) { e.preventDefault(); bad.focus(); return; }
        var btn = form.querySelector('[data-loading]');
        if (btn) { btn.classList.add('loading'); btn.setAttribute('aria-busy', 'true'); setTimeout(function () { btn.disabled = true; }, 0); }
    });

    // Back/forward cache: never leave the button stuck in its loading state.
    window.addEventListener('pageshow', function () {
        document.querySelectorAll('[data-loading].loading').forEach(function (b) { b.classList.remove('loading'); b.disabled = false; b.removeAttribute('aria-busy'); });
    });
})();
