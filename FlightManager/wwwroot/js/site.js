const prefersReducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

function setupAirportAutocomplete(displayId, hiddenId, suggestionsId) {
    const displayInput = document.getElementById(displayId);
    const hiddenInput = document.getElementById(hiddenId);
    const suggestions = document.getElementById(suggestionsId);

    if (!displayInput || !hiddenInput || !suggestions) return;
    if (displayInput.dataset.autocompleteInitialized === "true") return;
    displayInput.dataset.autocompleteInitialized = "true";

    let timeout = null;
    let requestController = null;
    let results = [];
    let activeIndex = -1;

    function closeSuggestions() {
        suggestions.replaceChildren();
        displayInput.setAttribute("aria-expanded", "false");
        displayInput.removeAttribute("aria-activedescendant");
        results = [];
        activeIndex = -1;
    }

    displayInput.addEventListener("input", function () {
        const query = displayInput.value.trim();
        hiddenInput.value = "";
        displayInput.classList.remove("field-invalid");

        clearTimeout(timeout);
        if (requestController) requestController.abort();
        closeSuggestions();

        if (query.length < 2) return;

        timeout = setTimeout(async function () {
            requestController = new AbortController();
            try {
                const response = await fetch(`/Home/LocationSuggestions?query=${encodeURIComponent(query)}`, {
                    signal: requestController.signal
                });
                if (!response.ok) return;

                const airports = await response.json();
                if (displayInput.value.trim() !== query) return;
                results = airports || [];
                activeIndex = -1;
                suggestions.replaceChildren();
                if (results.length === 0) {
                    displayInput.setAttribute("aria-expanded", "false");
                    return;
                }

                results.forEach(function (airport, index) {
                    const item = document.createElement("button");
                    item.type = "button";
                    item.className = "airport-suggestion";
                    item.id = `${suggestionsId}-option-${index}`;
                    item.setAttribute("role", "option");
                    item.setAttribute("aria-selected", "false");
                    item.dataset.index = String(index);
                    item.innerHTML = `
                        <strong>${escapeHtml(airport.airportName)}</strong>
                        <span>${escapeHtml(airport.code)} | ${escapeHtml(airport.city)}${airport.country ? " | " + escapeHtml(airport.country) : ""}</span>
                    `;

                    item.addEventListener("click", function () {
                        displayInput.value = airport.airportName;
                        hiddenInput.value = airport.code.toUpperCase();
                        closeSuggestions();
                        displayInput.classList.remove("field-invalid");
                    });

                    suggestions.appendChild(item);
                });
                displayInput.setAttribute("aria-expanded", "true");
            } catch (error) {
                if (error.name !== "AbortError") console.error("Airport autocomplete failed:", error);
            }
        }, 220);
    });

    displayInput.addEventListener("keydown", function (event) {
        if (event.key === "Escape") {
            closeSuggestions();
            return;
        }

        if (results.length === 0) return;
        if (event.key === "ArrowDown" || event.key === "ArrowUp") {
            event.preventDefault();
            const direction = event.key === "ArrowDown" ? 1 : -1;
            activeIndex = (activeIndex + direction + results.length) % results.length;
            suggestions.querySelectorAll(".airport-suggestion").forEach((option, index) => {
                const active = index === activeIndex;
                option.classList.toggle("is-active", active);
                option.setAttribute("aria-selected", String(active));
            });
            displayInput.setAttribute("aria-activedescendant", `${suggestionsId}-option-${activeIndex}`);
        } else if (event.key === "Enter" && activeIndex >= 0) {
            event.preventDefault();
            suggestions.querySelector(`#${suggestionsId}-option-${activeIndex}`)?.click();
        }
    });
    displayInput.addEventListener("blur", function () {
        window.setTimeout(() => {
            if (displayInput.value.trim() && !hiddenInput.value) {
                displayInput.classList.add("field-invalid");
            }
        }, 150);
    });

    document.addEventListener("click", function (event) {
        if (!displayInput.contains(event.target) && !suggestions.contains(event.target)) {
            closeSuggestions();
        }
    });
}

function escapeHtml(value) {
    if (value === null || value === undefined) return "";
    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

function initAirportAutocompleteFields() {
    setupAirportAutocomplete("originDisplay", "origin", "originSuggestions");
    setupAirportAutocomplete("destinationDisplay", "destination", "destinationSuggestions");
}

document.addEventListener("DOMContentLoaded", () => {
    initRevealAnimations();
    initRouteBars();
    initStickyHeader();
    initNavPill();
    initTravelerDateInputs();
    initDismissibleAlerts();
    initFormLoadingState();
    initCityBubbleStagger();
    initAirportAutocompleteFields();
    initPulseRouteLines();
    initQuickDestinations();
    document.querySelectorAll('[data-submit-on-change]').forEach((form) => {
        form.addEventListener('change', (event) => {
            if (event.target instanceof HTMLInputElement && event.target.name === 'Priority') {
                form.requestSubmit();
            }
        });
    });
});

function initTravelerDateInputs() {
    let timeZone = "UTC";
    try {
        timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone || "UTC";
    } catch {
    }

    document.querySelectorAll(".client-time-zone").forEach((input) => {
        input.value = timeZone;
    });

    const formatLocalDate = (date) => {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, "0");
        const day = String(date.getDate()).padStart(2, "0");
        return `${year}-${month}-${day}`;
    };

    const now = new Date();
    const today = formatLocalDate(now);
    const defaultDeparture = new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1);

    document.querySelectorAll("[data-traveler-date]").forEach((input) => {
        input.min = input.dataset.defaultLocal === "true"
            ? formatLocalDate(defaultDeparture)
            : today;
        if (input.dataset.defaultLocal === "true") {
            input.value = formatLocalDate(defaultDeparture);
        }
    });

    const departure = document.querySelector("[data-traveler-date]");
    const returns = document.querySelectorAll("[data-return-date]");
    const updateReturnDates = () => {
        returns.forEach((input) => {
            input.min = departure?.value || formatLocalDate(defaultDeparture);
        });
    };

    departure?.addEventListener("change", updateReturnDates);
    updateReturnDates();
}

function initNavPill() {
    const menu = document.getElementById("navPill");
    if (!menu) return;

    const cursor = menu.querySelector(".nav-pill-cursor");
    const items = Array.from(menu.querySelectorAll(":scope > li:not(.nav-pill-cursor)"));
    if (!cursor || items.length === 0) return;

    const moveCursor = (item, visible = true) => {
        const link = item.querySelector("a");
        if (!link) return;

        items.forEach((candidate) => candidate.classList.toggle("is-active", candidate === item));
        cursor.style.left = `${item.offsetLeft}px`;
        cursor.style.width = `${item.offsetWidth}px`;
        cursor.style.opacity = visible ? "1" : "0";
    };

    const currentPath = window.location.pathname.replace(/\/$/, "").toLowerCase() || "/";
    const currentItem = items.find((item) => {
        const link = item.querySelector("a");
        if (!link) return false;
        const linkPath = new URL(link.href, window.location.origin).pathname.replace(/\/$/, "").toLowerCase() || "/";
        return linkPath === currentPath ||
            (linkPath === "/" && ["/home/index", "/home/search"].includes(currentPath));
    });

    if (currentItem) moveCursor(currentItem);

    items.forEach((item) => {
        item.addEventListener("mouseenter", () => moveCursor(item));
        item.addEventListener("focusin", () => moveCursor(item));
    });

    const restoreCurrent = () => {
        if (currentItem) moveCursor(currentItem);
        else {
            items.forEach((item) => item.classList.remove("is-active"));
            cursor.style.opacity = "0";
        }
    };

    menu.addEventListener("mouseleave", restoreCurrent);
    menu.addEventListener("focusout", (event) => {
        if (!menu.contains(event.relatedTarget)) restoreCurrent();
    });
    window.addEventListener("resize", restoreCurrent, { passive: true });
}

function initRevealAnimations() {
    const revealTargets = document.querySelectorAll(".reveal");
    if (revealTargets.length === 0) return;

    if (prefersReducedMotion) {
        revealTargets.forEach((target) => target.classList.add("show"));
        return;
    }

    const groups = new Map();
    revealTargets.forEach((target) => {
        const parent = target.parentElement;
        if (!groups.has(parent)) groups.set(parent, []);
        groups.get(parent).push(target);
    });
    groups.forEach((siblings) => {
        siblings.forEach((el, index) => {
            el.style.setProperty("--reveal-delay", Math.min(index * 70, 420));
        });
    });

    const revealObserver = new IntersectionObserver((entries) => {
        entries.forEach((entry) => {
            if (entry.isIntersecting) {
                entry.target.classList.add("show");
                revealObserver.unobserve(entry.target);
            }
        });
    }, { threshold: 0.08, rootMargin: "0px 0px -40px 0px" });

    revealTargets.forEach((target) => revealObserver.observe(target));
}

function initRouteBars() {
    const routeBars = document.querySelectorAll(".reveal-bar");
    if (routeBars.length === 0) return;

    window.setTimeout(() => {
        routeBars.forEach((bar) => {
            const targetWidth = bar.style.getPropertyValue("--target-width");
            if (targetWidth) {
                bar.style.width = prefersReducedMotion ? targetWidth : targetWidth;
            }
        });
    }, prefersReducedMotion ? 0 : 350);
}

function initStickyHeader() {
    const header = document.getElementById("appHeader");
    if (!header) return;

    const updateHeaderState = () => {
        header.classList.toggle("scrolled", window.scrollY > 8);
    };

    updateHeaderState();
    window.addEventListener("scroll", updateHeaderState, { passive: true });
}

function initDismissibleAlerts() {
    document.querySelectorAll(".status-alert").forEach((alert) => {
        const dismiss = () => {
            alert.classList.add("leaving");
            alert.addEventListener("animationend", () => alert.remove(), { once: true });
        };

        const button = alert.querySelector(".alert-dismiss");
        if (button) button.addEventListener("click", dismiss);

        if (alert.classList.contains("success")) {
            window.setTimeout(dismiss, 5000);
        }
    });
}

function initFormLoadingState() {
    document.querySelectorAll(".airscout-form").forEach((form) => {
        form.addEventListener("submit", () => {
            const button = form.querySelector(".search-button");
            if (button && !button.classList.contains("is-loading")) {
                button.classList.add("is-loading");
                button.setAttribute("aria-busy", "true");
            }
        });
    });
}

function initCityBubbleStagger() {
    document.querySelectorAll(".city-bubble").forEach((bubble, index) => {
        bubble.style.setProperty("--bubble-delay", Math.min(index * 60, 480));
    });
}

function initPulseRouteLines() {
    const svg = document.querySelector(".pulse-lines");
    if (!svg) return;

    const bubbleByCode = {};
    document.querySelectorAll(".city-bubble").forEach((bubble) => {
        const code = bubble.dataset.code;
        if (code) bubbleByCode[code] = bubble;
    });

    svg.querySelectorAll("line[data-from][data-to]").forEach((line, index) => {
        const from = bubbleByCode[line.dataset.from];
        const to = bubbleByCode[line.dataset.to];
        if (!from || !to) {
            line.remove();
            return;
        }

        line.setAttribute("x1", parseFloat(from.style.left));
        line.setAttribute("y1", parseFloat(from.style.top));
        line.setAttribute("x2", parseFloat(to.style.left));
        line.setAttribute("y2", parseFloat(to.style.top));

        if (!prefersReducedMotion) {
            const length = Math.hypot(
                parseFloat(to.style.left) - parseFloat(from.style.left),
                parseFloat(to.style.top) - parseFloat(from.style.top)
            ) * 4;
            line.style.strokeDasharray = `${length}`;
            line.style.strokeDashoffset = `${length}`;
            line.style.transitionDelay = `${Math.min(index * 90, 500)}ms`;
            requestAnimationFrame(() => {
                requestAnimationFrame(() => {
                    line.style.strokeDashoffset = "0";
                });
            });
        }
    });
}

function initQuickDestinations() {
    document.querySelectorAll("[data-quick-destination]").forEach((chip) => {
        chip.addEventListener("click", () => {
            const code = chip.dataset.quickDestination;
            const name = chip.dataset.quickDestinationName;
            const destinationDisplay = document.getElementById("destinationDisplay");
            const destinationHidden = document.getElementById("destination");
            const originHidden = document.getElementById("origin");
            const form = document.querySelector(".airscout-form");

            if (!destinationDisplay || !destinationHidden) return;

            destinationDisplay.value = name;
            destinationHidden.value = code;
            destinationDisplay.classList.remove("field-invalid");

            if (originHidden && originHidden.value && form) {
                form.requestSubmit();
            } else {
                document.getElementById("originDisplay")?.focus();
            }
        });
    });
}
