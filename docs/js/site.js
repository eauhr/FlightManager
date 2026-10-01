document.addEventListener("DOMContentLoaded", () => {
    let timeZone = "UTC";
    try {
        timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone || "UTC";
    } catch {
    }

    document.querySelectorAll(".client-time-zone").forEach((input) => {
        input.value = timeZone;
    });

    const localDate = (date) => {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, "0");
        const day = String(date.getDate()).padStart(2, "0");
        return `${year}-${month}-${day}`;
    };

    const now = new Date();
    const tomorrow = new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1);
    const departure = document.querySelector("[data-traveler-date]");
    const returns = document.querySelectorAll("[data-return-date]");

    if (departure) {
        departure.min = localDate(tomorrow);
        departure.value = localDate(tomorrow);
    }

    const updateReturnDates = () => {
        returns.forEach((input) => {
            input.min = departure?.value || localDate(tomorrow);
        });
    };

    departure?.addEventListener("change", updateReturnDates);
    updateReturnDates();

    const menu = document.getElementById("navPill");
    const cursor = menu?.querySelector(".nav-pill-cursor");
    const items = menu ? Array.from(menu.querySelectorAll(":scope > li:not(.nav-pill-cursor)")) : [];
    const moveCursor = (item) => {
        if (!cursor) return;
        cursor.style.left = `${item.offsetLeft}px`;
        cursor.style.width = `${item.offsetWidth}px`;
        cursor.style.opacity = "1";
    };

    items.forEach((item) => {
        item.addEventListener("mouseenter", () => moveCursor(item));
        item.addEventListener("focusin", () => moveCursor(item));
    });

    menu?.addEventListener("mouseleave", () => {
        if (items[0]) moveCursor(items[0]);
    });

    window.addEventListener("resize", () => {
        if (items[0]) moveCursor(items[0]);
    }, { passive: true });

    const form = document.getElementById("flightSearchForm");
    const notice = document.getElementById("searchValidation");
    form?.addEventListener("submit", (event) => {
        event.preventDefault();
        if (notice) notice.hidden = false;
    });
});
