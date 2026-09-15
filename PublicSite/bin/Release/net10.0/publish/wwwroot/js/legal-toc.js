(function () {
  const rows = Array.from(document.querySelectorAll('.legal-toc-row'));
  const sections = Array.from(document.querySelectorAll('.legal-section'));
  if (rows.length === 0 || sections.length === 0) {
    return;
  }

  const rowById = new Map(rows.map((row) => [row.dataset.legalTarget, row]));
  const toc = document.querySelector('.legal-toc');
  let lockUntil = 0;

  function syncStickyOffset() {
    const header = document.querySelector('.site-sticky');
    if (!header) {
      return;
    }

    const height = Math.ceil(header.getBoundingClientRect().height);
    document.documentElement.style.setProperty('--site-sticky-height', `${height}px`);
  }

  function clearTransforms(row) {
    const num = row.querySelector('.legal-toc-num');
    const title = row.querySelector('.legal-toc-title');
    if (num) num.style.transform = '';
    if (title) title.style.transform = '';
  }

  function swapRow(row) {
    const num = row.querySelector('.legal-toc-num');
    const title = row.querySelector('.legal-toc-title');
    if (!num || !title) {
      return;
    }

    const gap = Math.max(0, row.clientWidth - num.offsetWidth - title.offsetWidth);
    const titleShift = num.offsetWidth + gap;
    const numShift = title.offsetWidth + gap;
    const rtl = getComputedStyle(row).direction === 'rtl';
    title.style.transform = rtl ? `translateX(${-titleShift}px)` : `translateX(${titleShift}px)`;
    num.style.transform = rtl ? `translateX(${numShift}px)` : `translateX(${-numShift}px)`;
  }

  function setActive(row) {
    if (!row || row.classList.contains('is-active')) {
      return;
    }

    rows.forEach((item) => {
      item.classList.remove('is-active');
      clearTransforms(item);
    });
    row.classList.add('is-active');
    swapRow(row);
    ensureRowVisible(row);
  }

  function ensureRowVisible(row) {
    if (!toc) {
      return;
    }

    const tocRect = toc.getBoundingClientRect();
    const rowRect = row.getBoundingClientRect();
    if (rowRect.top < tocRect.top) {
      toc.scrollTop -= tocRect.top - rowRect.top;
    } else if (rowRect.bottom > tocRect.bottom) {
      toc.scrollTop += rowRect.bottom - tocRect.bottom;
    }
  }

  rows.forEach((row) => {
    row.addEventListener('click', (event) => {
      const target = document.getElementById(row.dataset.legalTarget);
      if (!target) {
        return;
      }

      event.preventDefault();
      lockUntil = Date.now() + 900;
      setActive(row);
      target.scrollIntoView({ behavior: 'smooth', block: 'start' });
    });
  });

  const observer = new IntersectionObserver((entries) => {
    if (Date.now() < lockUntil) {
      return;
    }

    const visible = entries
      .filter((entry) => entry.isIntersecting)
      .sort((a, b) => b.intersectionRatio - a.intersectionRatio);
    if (visible.length === 0) {
      return;
    }

    const row = rowById.get(visible[0].target.id);
    setActive(row);
  }, { rootMargin: '-18% 0px -62% 0px', threshold: [0.15, 0.4, 0.7] });

  sections.forEach((sectionEl) => observer.observe(sectionEl));
  syncStickyOffset();
  setActive(rows[0]);
  window.addEventListener('resize', () => {
    syncStickyOffset();
    const active = document.querySelector('.legal-toc-row.is-active');
    if (active) {
      swapRow(active);
    }
  });
})();
