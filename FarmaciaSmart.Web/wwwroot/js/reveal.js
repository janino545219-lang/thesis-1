/* ================================================================
   reveal.js — Scroll-triggered reveal using IntersectionObserver.
   Elements with class "reveal" get "is-visible" added when they
   enter the viewport.  Respects prefers-reduced-motion.
   ================================================================ */
(function () {
    'use strict';

    var els = document.querySelectorAll('.reveal');

    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
        els.forEach(function (el) { el.classList.add('is-visible'); });
        return;
    }

    var observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (entry.isIntersecting) {
                entry.target.classList.add('is-visible');
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.15 });

    els.forEach(function (el) { observer.observe(el); });
})();
