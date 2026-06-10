/* ═══════════════════════════════════════════
   PlayPair Promotional Site · script.js
   ═══════════════════════════════════════════ */

(function () {
  'use strict';

  // ── Nav: blur on scroll ─────────────────────────────────
  const navbar = document.getElementById('navbar');
  window.addEventListener('scroll', () => {
    navbar.classList.toggle('scrolled', window.scrollY > 40);
  }, { passive: true });

  // ── Scroll Reveal ───────────────────────────────────────
  const reveals = document.querySelectorAll('.reveal');
  const revealObserver = new IntersectionObserver((entries) => {
    entries.forEach((entry, i) => {
      if (entry.isIntersecting) {
        // Stagger children in same parent
        const siblings = entry.target.parentElement.querySelectorAll('.reveal');
        let delay = 0;
        siblings.forEach((sib, idx) => {
          if (sib === entry.target) delay = idx * 80;
        });
        setTimeout(() => {
          entry.target.classList.add('in-view');
        }, delay);
        revealObserver.unobserve(entry.target);
      }
    });
  }, { threshold: 0.1, rootMargin: '0px 0px -40px 0px' });
  reveals.forEach(el => revealObserver.observe(el));

  // ── Hero progress bar: animate forward ─────────────────
  const hostProgress = document.getElementById('host-progress');
  const guestProgress = document.getElementById('guest-progress');
  let pct = 49;

  function tickProgress() {
    pct = (pct + 0.04) % 100;
    const w = pct.toFixed(2) + '%';
    if (hostProgress)  hostProgress.style.width = w;
    if (guestProgress) guestProgress.style.width = w;
    requestAnimationFrame(tickProgress);
  }
  requestAnimationFrame(tickProgress);

  // ── Room code copy button ───────────────────────────────
  const copyBtn = document.getElementById('rc-copy-btn');
  if (copyBtn) {
    copyBtn.addEventListener('click', () => {
      const code = 'LH93LL';
      navigator.clipboard.writeText(code).then(() => {
        copyBtn.classList.add('copied');
        const originalTitle = copyBtn.title;
        copyBtn.title = 'Copied!';
        setTimeout(() => {
          copyBtn.classList.remove('copied');
          copyBtn.title = originalTitle;
        }, 1800);
      }).catch(() => {
        // Fallback for browsers without clipboard API
        copyBtn.classList.add('copied');
        setTimeout(() => copyBtn.classList.remove('copied'), 1800);
      });
    });
  }

  // ── Smooth scroll for anchor links ─────────────────────
  document.querySelectorAll('a[href^="#"]').forEach(link => {
    link.addEventListener('click', (e) => {
      const target = document.querySelector(link.getAttribute('href'));
      if (!target) return;
      e.preventDefault();
      const offset = 70;
      const top = target.getBoundingClientRect().top + window.scrollY - offset;
      window.scrollTo({ top, behavior: 'smooth' });
    });
  });

  // ── Active nav link highlight on scroll ────────────────
  const sections = document.querySelectorAll('section[id]');
  const navLinks = document.querySelectorAll('.nav-pill-link[href^="#"]');
  const sectionObserver = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
      if (entry.isIntersecting) {
        const id = entry.target.id;
        navLinks.forEach(link => {
          link.classList.toggle('nav-active', link.getAttribute('href') === '#' + id);
        });
      }
    });
  }, { threshold: 0.4 });
  sections.forEach(s => sectionObserver.observe(s));

  // ── Marquee: pause on hover ─────────────────────────────
  const marqueeInner = document.querySelector('.marquee-inner');
  if (marqueeInner) {
    const track = marqueeInner.closest('.marquee-track');
    if (track) {
      track.addEventListener('mouseenter', () => { marqueeInner.style.animationPlayState = 'paused'; });
      track.addEventListener('mouseleave', () => { marqueeInner.style.animationPlayState = 'running'; });
    }
  }

  // ── Simulate play/pause toggle in hero ─────────────────
  // Randomly toggles hero cards between play/pause to show sync
  const hostCard  = document.getElementById('host-card');
  const guestCard = document.getElementById('guest-card');

  function simulateSync() {
    if (!hostCard || !guestCard) return;
    const icons = document.querySelectorAll('.play-icon svg');
    const isPause = Math.random() > 0.4; // bias toward playing

    icons.forEach(icon => {
      if (isPause) {
        // Show pause bars
        icon.innerHTML = '<rect x="6" y="4" width="4" height="16" rx="1"/><rect x="14" y="4" width="4" height="16" rx="1"/>';
      } else {
        // Show play triangle
        icon.innerHTML = '<polygon points="5 3 19 12 5 21 5 3"/>';
      }
    });
  }

  // Occasionally simulate a sync event
  setInterval(simulateSync, 3500);

  // ── Stagger bento cards ─────────────────────────────────
  document.querySelectorAll('.bento-card').forEach((card, i) => {
    card.style.transitionDelay = (i * 60) + 'ms';
  });

  // ── rc-chars: animate in on scroll ─────────────────────
  const rcChars = document.querySelectorAll('.rc-char');
  const rcObserver = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
      if (entry.isIntersecting) {
        rcChars.forEach((char, i) => {
          setTimeout(() => {
            char.style.opacity = '1';
            char.style.transform = 'translateY(0)';
          }, i * 80);
        });
        rcObserver.unobserve(entry.target);
      }
    });
  }, { threshold: 0.5 });
  rcChars.forEach(char => {
    char.style.opacity = '0';
    char.style.transform = 'translateY(8px)';
    char.style.transition = 'opacity 0.4s ease, transform 0.4s ease';
  });
  if (rcChars.length) rcObserver.observe(rcChars[0].closest('.bento-card') || rcChars[0]);

  // ── Add nav-active style ────────────────────────────────
  const style = document.createElement('style');
  style.textContent = `.nav-active { color: var(--text-1) !important; }`;
  document.head.appendChild(style);

})();
