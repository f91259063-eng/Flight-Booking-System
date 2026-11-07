
/* =========================================================
   Flight Booking System
   Customer Layout JavaScript
   ========================================================= */

document.addEventListener("DOMContentLoaded", function () {

    const shell = document.querySelector(".customer-shell");
    const sidebar = document.querySelector("[data-customer-sidebar]");
    const overlay = document.querySelector("[data-customer-sidebar-overlay]");
    const toggleButton = document.querySelector("[data-customer-sidebar-toggle]");
    const searchInput = document.querySelector("[data-customer-nav-search]");

    if (!shell || !sidebar) return;

    // Sidebar Toggle
    function openSidebar() {
        shell.classList.add("sidebar-open");
        toggleButton?.setAttribute("aria-expanded", "true");
    }

    function closeSidebar() {
        shell.classList.remove("sidebar-open");
        toggleButton?.setAttribute("aria-expanded", "false");
    }

    toggleButton?.addEventListener("click", function () {

        if (shell.classList.contains("sidebar-open")) {
            closeSidebar();
        } else {
            openSidebar();
        }

    });

    // Overlay Click
    overlay?.addEventListener("click", closeSidebar);

    // Escape Key
    document.addEventListener("keydown", function (event) {

        if (event.key === "Escape") {
            closeSidebar();
        }

    });

    // Close Sidebar After Navigation on Mobile
    sidebar.querySelectorAll("a.customer-nav-link").forEach(function (link) {

        link.addEventListener("click", function () {

            if (window.innerWidth < 1200) {
                closeSidebar();
            }

        });

    });

    // Active Navigation
    const currentPath = window.location.pathname.toLowerCase();

    sidebar.querySelectorAll("a.customer-nav-link").forEach(function (link) {

        const linkPath = new URL(link.href, window.location.origin)
            .pathname.toLowerCase();

        if (linkPath === currentPath) {
            link.classList.add("active");
            link.setAttribute("aria-current", "page");
        }

    });

    // Navigation Search
    searchInput?.addEventListener("input", function () {

        const searchValue = this.value.trim().toLowerCase();

        const links = sidebar.querySelectorAll("a.customer-nav-link");

        links.forEach(function (link) {

            const text = link.textContent.toLowerCase();

            link.style.display = text.includes(searchValue) ? "flex" : "none";

        });

        // Hide empty navigation groups
        sidebar.querySelectorAll(".customer-nav-group").forEach(function (group) {

            const visibleLinks = group.querySelectorAll(
                "a.customer-nav-link:not([style*='display: none'])"
            );

            group.style.display = visibleLinks.length > 0 ? "" : "none";

        });

    });

    // Restore navigation after clearing search
    searchInput?.addEventListener("search", function () {

        if (!this.value.trim()) {

            sidebar.querySelectorAll("a.customer-nav-link").forEach(function (link) {
                link.style.display = "";
            });

            sidebar.querySelectorAll(".customer-nav-group").forEach(function (group) {
                group.style.display = "";
            });

        }

    });

    // Responsive Reset
    window.addEventListener("resize", function () {

        if (window.innerWidth >= 1200) {
            closeSidebar();
        }

    });

});
