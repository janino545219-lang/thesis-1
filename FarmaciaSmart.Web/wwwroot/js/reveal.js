/* ================================================================
   reveal.js — Scroll-triggered reveal & sticky navbar shadow
   Respects prefers-reduced-motion.
   ================================================================ */
(function () {
    'use strict';

    // Sticky navbar shadow trigger on scroll
    var navbar = document.getElementById('landingNavbarWrap');
    if (navbar) {
        var handleScroll = function () {
            if (window.scrollY > 15) {
                navbar.classList.add('is-scrolled');
            } else {
                navbar.classList.remove('is-scrolled');
            }
        };
        window.addEventListener('scroll', handleScroll, { passive: true });
        handleScroll();
    }

    // Scroll reveal observer
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
    }, { threshold: 0.12 });

    els.forEach(function (el) { observer.observe(el); });
})();
