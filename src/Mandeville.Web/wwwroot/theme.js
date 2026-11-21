// Light / dark theme.
// Loaded in <head> (before the page paints) so the right colours show immediately, with no flash.
// The choice is kept in localStorage; until the person picks one, the computer's own setting is followed.
(function () {
    var KEY = 'theme';
    var root = document.documentElement;
    var query = window.matchMedia ? window.matchMedia('(prefers-color-scheme: dark)') : null;

    function stored() {
        try { var v = localStorage.getItem(KEY); return v === 'light' || v === 'dark' ? v : null; } catch (e) { return null; }
    }

    function current() {
        return stored() || (query && query.matches ? 'dark' : 'light');
    }

    function apply(theme) {
        root.setAttribute('data-theme', theme);
        var meta = document.querySelector('meta[name="theme-color"]');
        if (meta) meta.setAttribute('content', theme === 'dark' ? '#080d1a' : '#f2f6fd');
    }

    apply(current());

    // Follow the computer's setting while no choice has been saved
    if (query && query.addEventListener) {
        query.addEventListener('change', function () { if (!stored()) apply(current()); });
    }

    // Blazor page updates can rewrite the <html> tag; put the theme back if that happens
    new MutationObserver(function () {
        var want = current();
        if (root.getAttribute('data-theme') !== want) apply(want);
    }).observe(root, { attributes: true, attributeFilter: ['data-theme'] });

    // Any button marked data-theme-toggle switches the theme (works for buttons Blazor adds later too)
    document.addEventListener('click', function (e) {
        var button = e.target.closest ? e.target.closest('[data-theme-toggle]') : null;
        if (!button) return;
        var next = root.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
        try { localStorage.setItem(KEY, next); } catch (err) { /* private mode: the choice lasts until the page closes */ }
        apply(next);
    });
})();
