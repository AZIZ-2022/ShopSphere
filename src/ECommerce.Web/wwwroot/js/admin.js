(function () {
    "use strict";

    // ------------------------------------------- mobile sidebar off-canvas
    var toggle = document.querySelector("[data-admin-sidebar-toggle]");
    var sidebar = document.getElementById("adminSidebar");
    var backdrop = document.querySelector("[data-admin-sidebar-backdrop]");
    if (!toggle || !sidebar) { return; }

    function setOpen(open) {
        sidebar.classList.toggle("is-open", open);
        if (backdrop) { backdrop.classList.toggle("is-open", open); }
        toggle.setAttribute("aria-expanded", open ? "true" : "false");
    }

    toggle.addEventListener("click", function () {
        setOpen(!sidebar.classList.contains("is-open"));
    });

    if (backdrop) {
        backdrop.addEventListener("click", function () { setOpen(false); });
    }

    sidebar.querySelectorAll("a").forEach(function (a) {
        a.addEventListener("click", function () { setOpen(false); });
    });
})();
