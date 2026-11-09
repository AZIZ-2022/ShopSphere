(function () {
    "use strict";

    // ------------------------------------------------------------- toasts
    function toast(message, kind) {
        var stack = document.getElementById("toastStack");
        if (!stack) { return; }
        var el = document.createElement("div");
        el.className = "toast-msg toast-msg--" + (kind || "info");
        el.textContent = message;
        stack.appendChild(el);
        requestAnimationFrame(function () { el.classList.add("is-in"); });
        setTimeout(function () {
            el.classList.remove("is-in");
            setTimeout(function () { el.remove(); }, 250);
        }, 3200);
    }

    function postJson(url, body) {
        return fetch(url, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(body || {})
        }).then(function (r) {
            return r.json().catch(function () { return {}; }).then(function (d) {
                return { ok: r.ok, status: r.status, body: d };
            });
        });
    }

    // ----------------------------------------------- sticky header shadow
    var header = document.getElementById("siteHeader");
    if (header) {
        var onScroll = function () { header.classList.toggle("is-stuck", window.scrollY > 4); };
        onScroll();
        window.addEventListener("scroll", onScroll, { passive: true });
    }

    // ------------------------------------------------ cart badge updater
    function applyCartSummary(data) {
        if (!data) { return; }
        document.querySelectorAll("[data-cart-count]").forEach(function (n) {
            n.textContent = data.itemCount;
            n.classList.toggle("d-none", !data.itemCount);
        });
        document.querySelectorAll("[data-cart-subtotal], [data-cart-amount]").forEach(function (n) {
            if (data.subTotalDisplay) { n.textContent = data.subTotalDisplay; }
        });
    }

    // -------------------------------------------------- add to cart
    document.addEventListener("click", function (e) {
        var btn = e.target.closest("[data-add-to-cart]");
        if (!btn || btn.disabled) { return; }
        e.preventDefault();

        var id = parseInt(btn.getAttribute("data-product-id"), 10);
        var qtyInput = document.querySelector(".pdp__buy .qty-stepper input");
        var qty = qtyInput ? Math.max(1, parseInt(qtyInput.value, 10) || 1) : 1;

        btn.disabled = true;
        postJson("/api/cart/add", { productId: id, quantity: qty })
            .then(function (res) {
                if (res.ok && res.body.success) {
                    applyCartSummary(res.body.data);
                    toast(res.body.message || "Added to cart.", "success");
                } else {
                    toast((res.body && res.body.message) || "Could not add to cart.", "error");
                }
            })
            .catch(function () { toast("Network error.", "error"); })
            .finally(function () { btn.disabled = false; });
    });

    // -------------------------------------------------- wishlist toggle
    document.addEventListener("click", function (e) {
        var btn = e.target.closest("[data-wishlist-toggle]");
        if (!btn) { return; }
        e.preventDefault();

        var id = parseInt(btn.getAttribute("data-product-id"), 10);
        postJson("/api/wishlist/toggle", { productId: id })
            .then(function (res) {
                if (res.status === 401) {
                    toast("Please log in to use your wishlist.", "info");
                    setTimeout(function () {
                        window.location.href = "/account/login?returnUrl=" + encodeURIComponent(window.location.pathname);
                    }, 900);
                    return;
                }
                if (res.ok && res.body.success) {
                    var on = res.body.data.inWishlist;
                    btn.classList.toggle("is-active", on);
                    document.querySelectorAll("[data-wishlist-count]").forEach(function (n) {
                        n.textContent = res.body.data.count;
                        n.classList.toggle("d-none", !res.body.data.count);
                    });
                    toast(res.body.message, "success");
                } else {
                    toast((res.body && res.body.message) || "Could not update wishlist.", "error");
                }
            })
            .catch(function () { toast("Network error.", "error"); });
    });

    // ------------------------------------- hydrate wishlist hearts on load
    if (document.querySelector("[data-wishlist-toggle]")) {
        fetch("/api/wishlist/ids", { headers: { "Accept": "application/json" } })
            .then(function (r) { return r.ok ? r.json() : null; })
            .then(function (d) {
                if (!d || !d.success || !d.data || !d.data.ids) { return; }
                var set = new Set(d.data.ids);
                document.querySelectorAll("[data-wishlist-toggle]").forEach(function (btn) {
                    if (set.has(parseInt(btn.getAttribute("data-product-id"), 10))) {
                        btn.classList.add("is-active");
                    }
                });
            })
            .catch(function () { /* not logged in — ignore */ });
    }

    // --------------------------------------- generic qty steppers (+/-)
    document.addEventListener("click", function (e) {
        var hit = e.target.closest("[data-qty-inc], [data-qty-dec], .qty-stepper [data-qty]");
        if (!hit) { return; }
        var wrap = hit.closest(".qty-stepper");
        var input = wrap && wrap.querySelector("input");
        if (!input) { return; }

        var step = hit.hasAttribute("data-qty-inc") ? 1
            : hit.hasAttribute("data-qty-dec") ? -1
            : (parseInt(hit.getAttribute("data-qty"), 10) || 0);

        var min = parseInt(input.getAttribute("min"), 10) || 1;
        var max = parseInt(input.getAttribute("max"), 10) || 99;
        input.value = Math.min(max, Math.max(min, (parseInt(input.value, 10) || 1) + step));
        input.dispatchEvent(new Event("change", { bubbles: true }));
    });

    // ------------------------- cart page: auto-submit qty form on change
    document.querySelectorAll("[data-cart-qty-form] input[name='quantity']").forEach(function (input) {
        input.addEventListener("change", function () {
            input.closest("form").submit();
        });
    });

    // --------------------------------------- newsletter (footer + band)
    document.querySelectorAll("[data-newsletter-form]").forEach(function (form) {
        var msg = form.parentElement.querySelector("[data-newsletter-msg]") ||
                  form.querySelector("[data-newsletter-msg]");
        form.addEventListener("submit", function (e) {
            e.preventDefault();
            var email = form.querySelector('input[name="email"]').value.trim();
            if (!email) { return; }
            var btn = form.querySelector("button");
            btn.disabled = true;
            if (msg) { msg.textContent = "Subscribing…"; }
            postJson(form.getAttribute("action"), { email: email })
                .then(function (res) {
                    var text = (res.body && res.body.message) ||
                        (res.ok ? "Thanks for subscribing!" : "Could not subscribe.");
                    if (msg) { msg.textContent = text; }
                    toast(text, res.ok ? "success" : "error");
                    if (res.ok) { form.reset(); }
                })
                .catch(function () { if (msg) { msg.textContent = "Network error."; } })
                .finally(function () { btn.disabled = false; });
        });
    });
})();
