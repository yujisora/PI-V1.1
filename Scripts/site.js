/* =========================================================================
   site.js
   Handles ONLY client-side-only UI behavior that doesn't need the server:
     - Dark/light mode toggle (persisted in localStorage)
     - Mobile hamburger menu open/close
     - Accessibility panel: font size, letter spacing, line height,
       dyslexia-friendly font, high contrast, reduce motion - all persisted
       in localStorage and re-applied instantly on every page (see the
       small inline "anti-flash" script at the top of _Layout.cshtml and
       Login.cshtml, which applies the same saved values before this file
       even loads, so there's no flash of un-adjusted content).
   Anything that changes DATA (search, save, add, login...) is a normal
   server form submit / link, handled by the MVC controllers instead.
   ========================================================================= */

document.addEventListener('DOMContentLoaded', function () {

    /* ---------------------------------------------------------------
       Dark mode toggle
       --------------------------------------------------------------- */
    var darkToggleBtn = document.getElementById('darkModeToggle');
    var darkIcon = document.getElementById('darkModeIcon');

    function updateDarkIcon() {
        if (!darkIcon) return;
        var isDark = document.documentElement.classList.contains('dark');
        // Sun icon when dark (click to go light), moon icon when light (click to go dark)
        darkIcon.innerHTML = isDark ? '&#9728;' : '&#9789;';
        if (darkToggleBtn) {
            darkToggleBtn.setAttribute('aria-label', isDark ? 'Switch to light mode' : 'Switch to dark mode');
        }
    }
    updateDarkIcon();

    if (darkToggleBtn) {
        darkToggleBtn.addEventListener('click', function () {
            var isDark = document.documentElement.classList.toggle('dark');
            // Saving ANY value here (even 'false') marks this as an explicit
            // user override, so it takes priority over the OS setting from
            // now on - see the anti-flash script and the listener below.
            localStorage.setItem('foodscan-dark', isDark ? 'true' : 'false');
            updateDarkIcon();
        });
    }

    // If the person has never clicked the toggle (no explicit choice saved),
    // keep following the OS/browser color-scheme setting live - so if they
    // switch their system from light to dark while this tab is open, the
    // app follows along automatically.
    if (window.matchMedia) {
        var colorSchemeQuery = window.matchMedia('(prefers-color-scheme: dark)');
        var handleSystemThemeChange = function (e) {
            if (localStorage.getItem('foodscan-dark') !== null) return; // explicit override in place, ignore OS changes
            document.documentElement.classList.toggle('dark', e.matches);
            updateDarkIcon();
        };
        // addEventListener is the modern API; addListener is a fallback for
        // older WebView/browser engines that might be used in dev/testing.
        if (colorSchemeQuery.addEventListener) {
            colorSchemeQuery.addEventListener('change', handleSystemThemeChange);
        } else if (colorSchemeQuery.addListener) {
            colorSchemeQuery.addListener(handleSystemThemeChange);
        }
    }

    /* ---------------------------------------------------------------
       Responsive nav: force the hamburger menu when the pill nav
       doesn't actually fit in one row, even if the CSS breakpoint
       would otherwise show it (e.g. a moderately narrow desktop
       window). Re-checked on resize AND whenever an accessibility
       text setting changes, since those can widen the pills without
       any window resize event ever firing.
       --------------------------------------------------------------- */
    function updateNavFit() {
        var header = document.querySelector('.site-header');
        if (!header) return; // not present on the Login page
        var headerInner = header.querySelector('.header-inner');
        if (!headerInner) return;

        // Start from the CSS breakpoint's own decision (remove any
        // previous forced-compact state) so we measure the pill layout's
        // natural width fresh each time, not whatever we forced last time.
        header.classList.remove('nav-force-compact');

        // Measure with wrapping temporarily disabled - otherwise flex-wrap
        // would just absorb the overflow vertically and scrollWidth would
        // never exceed clientWidth, hiding the exact problem we're
        // checking for. Restored immediately after as a CSS-level safety
        // net for any remaining edge case.
        var previousWrap = headerInner.style.flexWrap;
        headerInner.style.flexWrap = 'nowrap';
        var overflowing = headerInner.scrollWidth > headerInner.clientWidth + 1;
        headerInner.style.flexWrap = previousWrap;

        if (overflowing) {
            header.classList.add('nav-force-compact');
        }
    }
    updateNavFit();

    var navFitResizeTimer = null;
    window.addEventListener('resize', function () {
        clearTimeout(navFitResizeTimer);
        navFitResizeTimer = setTimeout(updateNavFit, 150);
    });

    /* ---------------------------------------------------------------
       Allergen checklist dropdowns (Edit screen only): checking an
       allergen as "Contains" disables (and unchecks) its "May Contain"
       counterpart, since a product can't be both for the same allergen.
       This only runs one direction, as requested - checking something
       as "May Contain" does NOT restrict "Contains".
       --------------------------------------------------------------- */
    var containsBoxes = document.querySelectorAll('input[data-role="contains"]');
    var mayContainBoxes = document.querySelectorAll('input[data-role="maycontain"]');

    function findMayContainBox(key) {
        return document.querySelector('input[data-role="maycontain"][data-allergen-key="' + key + '"]');
    }

    function updateExclusivity(containsBox) {
        var key = containsBox.getAttribute('data-allergen-key');
        var mayContainBox = findMayContainBox(key);
        if (!mayContainBox) return;
        var row = mayContainBox.closest('.allergen-checklist-item');

        if (containsBox.checked) {
            mayContainBox.checked = false;
            mayContainBox.disabled = true;
            if (row) row.classList.add('is-disabled');
        } else {
            mayContainBox.disabled = false;
            if (row) row.classList.remove('is-disabled');
        }
    }

    function updateSummary(role, summaryElementId) {
        var summaryEl = document.getElementById(summaryElementId);
        if (!summaryEl) return;
        var boxes = document.querySelectorAll('input[data-role="' + role + '"]:checked');
        if (boxes.length === 0) {
            summaryEl.textContent = 'None selected';
            return;
        }
        var names = [];
        boxes.forEach(function (box) {
            var item = box.closest('.allergen-checklist-item');
            if (item) names.push(item.getAttribute('data-allergen-label'));
        });
        summaryEl.textContent = names.join(', ');
    }

    if (containsBoxes.length > 0 || mayContainBoxes.length > 0) {
        containsBoxes.forEach(function (box) {
            updateExclusivity(box); // set initial disabled state from server-rendered values
            box.addEventListener('change', function () {
                updateExclusivity(box);
                updateSummary('contains', 'containsSummaryText');
                updateSummary('maycontain', 'mayContainSummaryText');
            });
        });
        mayContainBoxes.forEach(function (box) {
            box.addEventListener('change', function () {
                updateSummary('maycontain', 'mayContainSummaryText');
            });
        });
        updateSummary('contains', 'containsSummaryText');
        updateSummary('maycontain', 'mayContainSummaryText');
    }

    /* ---------------------------------------------------------------
       Edit History table: click (or Enter/Space) a truncated value to
       expand it in place, click again to re-truncate.
       --------------------------------------------------------------- */
    document.querySelectorAll('.truncate-value').forEach(function (el) {
        el.addEventListener('click', function () {
            el.classList.toggle('expanded');
        });
        el.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                el.classList.toggle('expanded');
            }
        });
    });

    /* ---------------------------------------------------------------
       Mobile menu (three-dot button in header - not present on Login)
       --------------------------------------------------------------- */
    var mobileToggle = document.getElementById('mobileMenuToggle');
    var mobileMenu = document.getElementById('mobileMenu');

    if (mobileToggle && mobileMenu) {
        mobileToggle.addEventListener('click', function (e) {
            e.stopPropagation();
            var isOpen = !mobileMenu.hidden;
            mobileMenu.hidden = isOpen;
            mobileToggle.setAttribute('aria-expanded', (!isOpen).toString());
        });
        document.addEventListener('click', function (e) {
            if (!mobileMenu.hidden && !mobileMenu.contains(e.target) && e.target !== mobileToggle) {
                mobileMenu.hidden = true;
                mobileToggle.setAttribute('aria-expanded', 'false');
            }
        });
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                mobileMenu.hidden = true;
                mobileToggle.setAttribute('aria-expanded', 'false');
            }
        });
    }

    /* ---------------------------------------------------------------
       Accessibility panel open/close (shared by every page)
       --------------------------------------------------------------- */
    var a11yToggle = document.getElementById('accessibilityToggle');
    var a11yMenu = document.getElementById('accessibilityMenu');

    function closeA11yMenu() {
        if (!a11yMenu || a11yMenu.hidden) return;
        a11yMenu.hidden = true;
        if (a11yToggle) a11yToggle.setAttribute('aria-expanded', 'false');
    }

    if (a11yToggle && a11yMenu) {
        a11yToggle.addEventListener('click', function (e) {
            e.stopPropagation();
            var isOpen = !a11yMenu.hidden;
            a11yMenu.hidden = isOpen;
            a11yToggle.setAttribute('aria-expanded', (!isOpen).toString());
            // Move focus into the panel when it opens, for keyboard users.
            if (!isOpen) {
                var firstControl = a11yMenu.querySelector('button');
                if (firstControl) firstControl.focus();
            }
        });
        document.addEventListener('click', function (e) {
            if (!a11yMenu.hidden && !a11yMenu.contains(e.target) && e.target !== a11yToggle) {
                closeA11yMenu();
            }
        });
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && !a11yMenu.hidden) {
                closeA11yMenu();
                a11yToggle.focus();
            }
        });
    }

    /* ---------------------------------------------------------------
       Accessibility: choice rows (font size / letter spacing / line height)
       Each button has data-setting="fontsize|spacing|lineheight" and
       data-value="...". aria-pressed marks whichever one is active.
       --------------------------------------------------------------- */
    var choiceButtons = document.querySelectorAll('.accessibility-option[data-setting]');

    var CSS_VAR_BY_SETTING = {
        fontsize: '--font-size',
        spacing: '--letter-spacing',
        lineheight: '--line-height-base'
        // "font" is handled separately below - it swaps a CSS class
        // (font-atkinson / font-opendyslexic) rather than a single CSS
        // variable value, since each option loads a different typeface.
    };
    var STORAGE_KEY_BY_SETTING = {
        fontsize: 'foodscan-fontsize',
        spacing: 'foodscan-spacing',
        lineheight: 'foodscan-lineheight',
        font: 'foodscan-font'
    };
    function cssValueFor(setting, value) {
        if (setting === 'fontsize') {
            return value === '150' ? '24px' : value === '200' ? '32px' : '16px';
        }
        if (setting === 'spacing') {
            return value === 'wide' ? '0.05em' : value === 'wider' ? '0.1em' : 'normal';
        }
        if (setting === 'lineheight') {
            return value === 'relaxed' ? '1.8' : value === 'loose' ? '2.1' : '1.5';
        }
        return null;
    }

    function applyChoice(setting, value, persist) {
        if (setting === 'font') {
            // Default = Lexend (no class needed, it's the CSS base font).
            document.documentElement.classList.remove('font-atkinson', 'font-opendyslexic');
            if (value === 'atkinson') document.documentElement.classList.add('font-atkinson');
            else if (value === 'opendyslexic') document.documentElement.classList.add('font-opendyslexic');
        } else {
            var cssVar = CSS_VAR_BY_SETTING[setting];
            if (cssVar) {
                document.documentElement.style.setProperty(cssVar, cssValueFor(setting, value));
            }
        }
        if (persist) {
            localStorage.setItem(STORAGE_KEY_BY_SETTING[setting], value);
        }
        // Update aria-pressed / active styling on every button in this group.
        document.querySelectorAll('.accessibility-option[data-setting="' + setting + '"]').forEach(function (btn) {
            var isActive = btn.getAttribute('data-value') === value;
            btn.setAttribute('aria-pressed', isActive.toString());
            btn.classList.toggle('accessibility-option-active', isActive);
        });

        // Font size/spacing/font choice can all widen the header's pill
        // nav without triggering a window resize event - re-check.
        updateNavFit();
    }

    choiceButtons.forEach(function (btn) {
        btn.addEventListener('click', function () {
            var setting = btn.getAttribute('data-setting');
            var value = btn.getAttribute('data-value');

            // Once the person picks a spacing or line-height value directly,
            // that's a manual override - it always wins from now on and a
            // later font change will never auto-adjust it again. Spacing/
            // line-height choices, in turn, NEVER touch the font setting -
            // this linkage only flows one direction (font -> spacing/height).
            if (setting === 'spacing' || setting === 'lineheight') {
                localStorage.setItem('foodscan-' + setting + '-manual', 'true');
            }

            applyChoice(setting, value, true);

            if (setting === 'font') {
                var spacingIsManual = localStorage.getItem('foodscan-spacing-manual') === 'true';
                var lineheightIsManual = localStorage.getItem('foodscan-lineheight-manual') === 'true';
                var isNonDefaultFont = value !== 'default';

                // Wider/more distinct fonts (Atkinson Hyperlegible, OpenDyslexic)
                // generally read better with more letter and line spacing, so
                // suggest that automatically - but only where the person
                // hasn't already told us what they want.
                if (!spacingIsManual) {
                    applyChoice('spacing', isNonDefaultFont ? 'wide' : 'normal', true);
                }
                if (!lineheightIsManual) {
                    applyChoice('lineheight', isNonDefaultFont ? 'relaxed' : 'normal', true);
                }
            }
        });
    });

    // Sync each choice row's active button to whatever is already saved
    // (the anti-flash script already applied the font/CSS variables
    // themselves; this just makes the buttons' aria-pressed / highlighting
    // match on load).
    ['fontsize', 'spacing', 'lineheight', 'font'].forEach(function (setting) {
        var defaultValue = setting === 'fontsize' ? '100' : setting === 'font' ? 'default' : 'normal';
        var saved = localStorage.getItem(STORAGE_KEY_BY_SETTING[setting]) || defaultValue;
        applyChoice(setting, saved, false);
    });

    /* ---------------------------------------------------------------
       Accessibility: on/off toggle switches (dyslexia font, high
       contrast, reduce motion). Each is a role="switch" button.
       --------------------------------------------------------------- */
    var TOGGLES = [
        { id: 'toggleHighlightHeadings', cssClass: 'highlight-headings', storageKey: 'foodscan-highlight-headings' },
        { id: 'toggleHighlightLinks', cssClass: 'highlight-links', storageKey: 'foodscan-highlight-links' },
        { id: 'toggleHighContrast', cssClass: 'high-contrast', storageKey: 'foodscan-contrast' },
        { id: 'toggleReduceMotion', cssClass: 'reduce-motion', storageKey: 'foodscan-motion' }
    ];

    function setToggleState(toggle, isOn, persist) {
        var btn = document.getElementById(toggle.id);
        document.documentElement.classList.toggle(toggle.cssClass, isOn);
        if (persist) localStorage.setItem(toggle.storageKey, isOn ? 'true' : 'false');
        if (btn) {
            btn.setAttribute('aria-checked', isOn.toString());
            btn.setAttribute('aria-pressed', isOn.toString());
        }
    }

    TOGGLES.forEach(function (toggle) {
        var btn = document.getElementById(toggle.id);
        // Sync initial visual state to whatever the anti-flash script already applied.
        var alreadyOn = document.documentElement.classList.contains(toggle.cssClass);
        setToggleState(toggle, alreadyOn, false);

        if (btn) {
            btn.addEventListener('click', function () {
                var isOn = !document.documentElement.classList.contains(toggle.cssClass);
                setToggleState(toggle, isOn, true);
            });
        }
    });

    /* ---------------------------------------------------------------
       Accessibility: reset to defaults
       --------------------------------------------------------------- */
    var resetBtn = document.getElementById('accessibilityReset');
    if (resetBtn) {
        resetBtn.addEventListener('click', function () {
            ['foodscan-fontsize', 'foodscan-spacing', 'foodscan-lineheight', 'foodscan-font',
                'foodscan-contrast', 'foodscan-motion', 'foodscan-highlight-headings', 'foodscan-highlight-links',
                'foodscan-spacing-manual', 'foodscan-lineheight-manual'].forEach(function (key) {
                    localStorage.removeItem(key);
                });
            applyChoice('fontsize', '100', false);
            applyChoice('spacing', 'normal', false);
            applyChoice('lineheight', 'normal', false);
            applyChoice('font', 'default', false);
            TOGGLES.forEach(function (toggle) { setToggleState(toggle, false, false); });
            resetBtn.focus();
        });
    }
});

(function () {
    var toggle = document.getElementById('userMenuToggle');
    var menu = document.getElementById('userMenu');
    if (!toggle || !menu) return;

    toggle.addEventListener('click', function (e) {
        e.stopPropagation();
        var isOpen = !menu.hidden;
        menu.hidden = isOpen;
        toggle.setAttribute('aria-expanded', (!isOpen).toString());
    });

    document.addEventListener('click', function (e) {
        if (!menu.hidden && !menu.contains(e.target) && e.target !== toggle) {
            menu.hidden = true;
            toggle.setAttribute('aria-expanded', 'false');
        }
    });
})();