
(() => {
    "use strict";

    const body = document.body;

    const toggle = document.querySelector(
        "[data-admin-sidebar-toggle]"
    );

    const overlay = document.querySelector(
        "[data-admin-sidebar-overlay]"
    );

    const sidebar = document.querySelector(
        "[data-admin-sidebar]"
    );

    if (!toggle || !sidebar || !overlay) {
        return;
    }

    const desktopMedia = window.matchMedia("(min-width: 992px)");

    // ==============================
    // Sidebar Controls
    // ==============================

    const closeSidebar = () => {
        body.classList.remove("admin-sidebar-open");
        toggle.setAttribute("aria-expanded", "false");
    };

    const openSidebar = () => {
        body.classList.add("admin-sidebar-open");
        toggle.setAttribute("aria-expanded", "true");
    };

    const toggleDesktopSidebar = () => {
        const isCollapsed = body.classList.toggle(
            "admin-sidebar-collapsed"
        );

        toggle.setAttribute(
            "aria-expanded",
            String(!isCollapsed)
        );
    };

    toggle.addEventListener("click", () => {

        if (desktopMedia.matches) {
            toggleDesktopSidebar();
            return;
        }

        if (body.classList.contains("admin-sidebar-open")) {
            closeSidebar();
        } else {
            openSidebar();
        }
    });

    // Close mobile sidebar using overlay
    overlay.addEventListener("click", closeSidebar);

    // Close using Escape
    document.addEventListener("keydown", event => {
        if (event.key === "Escape") {
            closeSidebar();
        }
    });

    // Close mobile sidebar after navigation
    sidebar.querySelectorAll("a").forEach(link => {
        link.addEventListener("click", () => {
            if (!desktopMedia.matches) {
                closeSidebar();
            }
        });
    });

    // Reset mobile state when resizing to desktop
    const handleScreenChange = event => {
        if (event.matches) {
            closeSidebar();
        } else {
            body.classList.remove("admin-sidebar-collapsed");
            toggle.setAttribute("aria-expanded", "false");
        }
    };

    if (desktopMedia.addEventListener) {
        desktopMedia.addEventListener("change", handleScreenChange);
    } else {
        desktopMedia.addListener(handleScreenChange);
    }

    // ==============================
    // Active Navigation
    // ==============================

    const normalizePath = path => {
        return path.replace(/\/+$/, "").toLowerCase() || "/";
    };

    const currentPath = normalizePath(
        window.location.pathname
    );

    const navLinks = sidebar.querySelectorAll(
        ".admin-nav-link"
    );

    navLinks.forEach(link => {

        const linkPath = normalizePath(
            new URL(link.href, window.location.origin).pathname
        );

        const isActive =
            currentPath === linkPath ||
            (
                linkPath !== "/" &&
                currentPath.startsWith(linkPath + "/")
            );

        if (isActive) {
            link.classList.add("active");
            link.setAttribute("aria-current", "page");
        } else {
            link.classList.remove("active");
            link.removeAttribute("aria-current");
        }
    });

    // ==============================
    // Navigation Search
    // ==============================

    const searchInput = document.querySelector(
        "[data-admin-nav-search]"
    );

    if (searchInput) {

        const groups = sidebar.querySelectorAll(
            ".admin-nav-group"
        );

        searchInput.addEventListener("input", () => {

            const query = searchInput.value
                .trim()
                .toLowerCase();

            groups.forEach(group => {

                const links = group.querySelectorAll(
                    ".admin-nav-link"
                );

                let hasVisibleLink = false;

                links.forEach(link => {

                    const linkText = link.textContent
                        .trim()
                        .toLowerCase();

                    const matches =
                        query.length === 0 ||
                        linkText.includes(query);

                    link.hidden = !matches;

                    if (matches) {
                        hasVisibleLink = true;
                    }
                });

                group.hidden =
                    query.length > 0 && !hasVisibleLink;
            });
        });
    }

})();
