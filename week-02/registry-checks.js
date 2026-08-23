// Week 2 Cryptid Registry Self-Check
// Already wired into each page. Open the console (F12) on any page — it grades
// THAT page. Refresh after each change; work one ❌ at a time.
// (Leave this file in when you deploy — it doesn't affect grading.)

(function (root) {
  function runChecks(doc, page) {
    const $ = (sel) => doc.querySelector(sel);
    const $$ = (sel) => doc.querySelectorAll(sel);
    const styleText = [...$$("style")].map(s => s.textContent).join("\n");
    const results = { page, required: [], homework: [], warnings: [] };
    const req = (label, pass) => results.required.push({ label, pass: !!pass });
    const hw = (label, pass) => results.homework.push({ label, pass: !!pass });

    // ── every page ──
    req("viewport meta present", $('meta[name="viewport"]'));
    req("Bootstrap CSS linked", $('link[href*="bootstrap"][rel="stylesheet"]'));
    req("Bootstrap JS bundle at end of body", $('script[src*="bootstrap.bundle"]'));
    // Rows live inside containers. Without one, .row's negative gutters bleed past
    // the viewport — measured at 402px in a 390px screen — so the page scrolls
    // sideways on a phone while every other check stays green. Deliberately asks
    // for a container ANCESTOR, not a container on <main>: wrapping <main> in one
    // is just as correct, and a stricter test failed that page.
    // Two halves: a container has to exist, and any rows have to be inside one.
    // Requiring rows to EXIST made this unpassable until the feature row was
    // built — task 1 stayed red until task 5, on a page that was already right.
    const rows = [...$$(".row")];
    req("page content sits in a container",
      $(".container, .container-fluid") && rows.every(r => r.closest(".container, .container-fluid")));
    req("navbar with brand", $("nav.navbar") && $(".navbar-brand"));
    // "hamburger works" is part of the label, so the toggler has to actually point
    // at the collapse. A data-bs-target naming an id that doesn't exist renders a
    // button that does nothing, and the old check passed it.
    const toggler = $(".navbar-toggler");
    const togTarget = (toggler && (toggler.getAttribute("data-bs-target") || toggler.getAttribute("href")) || "").trim();
    let togHit = null;
    if (togTarget) { try { togHit = $(togTarget); } catch (e) { togHit = null; } }
    req("navbar toggler + collapse (hamburger works)",
      toggler && $(".navbar-collapse") && togHit && togHit.classList.contains("collapse"));
    // three links all pointing at the same page is not "links to all 3 pages"
    const navFiles = new Set([...$$("a.nav-link")]
      .map(a => (a.getAttribute("href") || "").toLowerCase().split(/[?#]/)[0].split("/").pop())
      .map(f => (f === "" || f === ".") ? "index.html" : f));
    req("nav links to all 3 pages", navFiles.size >= 3);
    // ...on THIS page's link. Pasting one navbar onto all three pages leaves every
    // page highlighting Home, and "is something active?" called that correct.
    const activeHref = ($("a.nav-link.active")?.getAttribute("href") || "").toLowerCase();
    const activeFile = activeHref.split(/[?#]/)[0].split("/").pop();
    req("current page marked .active in nav", $("a.nav-link.active") && (page === "index"
      ? ["", ".", "index.html"].includes(activeFile)   // "/", "./" and "" all mean home
      : activeFile === page + ".html"));
    req("Bootstrap Icons stylesheet linked", $('link[href*="bootstrap-icons"]'));
    req("at least one icon used (bi-*)", $('[class*="bi-"]'));
    // "padded" is part of the label, so it has to be part of the check — any real
    // vertical padding utility on the footer or something inside it (py-4, p-3, pt-5…).
    const footerClasses = [$("footer"), ...$$("footer [class]")]
      .map(e => (e && e.className) || "").join(" ");
    req("footer: centered, muted, padded",
      $("footer.text-center") && $("footer .mb-0, footer p") &&
      /text-muted/.test($("footer")?.className + " " + ($("footer p")?.className || "")) &&
      /(^|\s)(p|py|pt|pb)-[1-9]/.test(footerClasses));

    // ── per page ──
    if (page === "index") {
      req("hero: display-* heading", $('[class*="display-"]'));
      req("hero: lead paragraph", $(".lead"));
      req("hero: button to the registry page", [...$$('a[href*="registry"]')].some(a => /btn/.test(a.className)));
      // "full width on phones, thirds on md and up" needs a breakpoint in the class.
      // Three plain `col` sit side by side at every width, phone included.
      req("feature row: 3+ responsive columns in a row", $(".row") &&
        [...$$('.row [class*="col-"]')].filter(c => /(^|\s)col-(sm|md|lg|xl|xxl)-\d/.test(c.className)).length >= 3);
    }
    if (page === "registry") {
      req("6+ cards", $$(".card").length >= 6);
      req("cards live in responsive columns", $$('[class*="col-"] .card').length >= 6);
      req("cards use h-100 (equal heights)", $$(".card.h-100").length >= 6);
      req("row has gutters (g-*)", [...$$(".row")].some(r => /(^|\s)g[xy]?-\d/.test(r.className)));
      req("badges on cards", $$(".card .badge").length >= 3);
    }
    if (page === "report") {
      req("form present", $("form"));
      req("3+ form-control inputs", $$(".form-control").length >= 3);
      req("labels use form-label", $$(".form-label").length >= 3);
      req("a form-select dropdown", $(".form-select"));
      req("submit button", $('button[type="submit"], form button'));
      // Was labeled "info alert above the form" while accepting any .alert
      // anywhere. Position is not worth DOM logic; the variant is, and the lab
      // asks for alert-info by name.
      req("an alert-info on the page", $(".alert-info"));
      req("form constrained with the grid (col-*)", [...$$('[class*="col-"]')].some(c => c.querySelector("form")));
    }

    // ── homework (Part 2 — not needed during the lab) ──
    const themeLink = [...$$('link[rel="stylesheet"]')].find(l => /bootswatch/.test(l.getAttribute("href") || ""));
    hw("Bootswatch theme, and not Flatly",
      themeLink && !/\/flatly\//i.test(themeLink.getAttribute("href") || ""));

    // "one heading font, one body font" — both names have to be in the Google URL(s)
    const families = [...$$('link[href*="fonts.googleapis.com"]')]
      .flatMap(l => [...(l.getAttribute("href") || "").matchAll(/family=([^&:]+)/g)].map(m => m[1]));
    hw("two Google Font families linked (heading + body)", new Set(families).size >= 2);
    hw("font override: --bs-body-font-family and a heading rule",
      /--bs-body-font-family/.test(styleText) && /h1[^{]*\{[^}]*font-family/.test(styleText));

    if (page === "registry") {
      // The homework says to reuse the "artist unknown" plate rather than draw
      // one, so the card has to carry an image like the other six do.
      hw("a seventh cryptid of your own — badge and plate",
        $$(".card").length >= 7 && $$(".card .badge").length >= 7 && $$(".card img").length >= 7);
    }
    hw("a docs component we didn't cover (this page)",
      $('.accordion, .carousel, .modal, .list-group, .offcanvas, [data-bs-toggle="tooltip"], [data-bs-toggle="popover"], [data-bs-theme-toggle], #themeToggle'));

    // ── warnings (deduction risks) ──
    const allowed = styleText.replace(/[^{}]*\{[^}]*font-family[^}]*\}/g, "").replace(/:root\s*\{\s*\}/g, "").trim();
    if (allowed.length > 0) results.warnings.push("custom CSS beyond the font override? Check your <style> block — utilities only!");
    const badLinks = [...$$('link[rel="stylesheet"]')].filter(l =>
      !/bootstrap|bootswatch|fonts\.googleapis|fonts\.gstatic/.test(l.href));
    if (badLinks.length) results.warnings.push(`non-Bootstrap stylesheet linked: ${badLinks.map(l => l.getAttribute("href")).join(", ")}`);
    return results;
  }

  // Browser runner
  if (typeof window !== "undefined" && typeof document !== "undefined") {
    // Match on the FILE name, not the whole path — the folder is called
    // cryptid-registry, so path.includes("registry") would call every page the
    // registry page, including the home page.
    const file = (location.pathname.toLowerCase().split("/").pop() || "");
    const page = file.startsWith("registry") ? "registry" : file.startsWith("report") ? "report" : "index";
    const r = runChecks(document, page);
    const show = (title, list) => {
      console.log(`%c── ${title} ──`, "font-weight: bold");
      list.forEach(c => console.log(`${c.pass ? "✅" : "❌"} ${c.label}`));
    };
    console.log(`%c── Week 2 Self-Check: ${page}.html ──`, "font-weight: bold; font-size: 1.1em");
    show("Required", r.required);
    const pr = r.required.filter(c => c.pass).length;
    const allGreen = pr === r.required.length;
    console.log(`%c${pr}/${r.required.length} required — check all 3 pages!`,
      allGreen ? "color: green; font-weight: bold" : "color: orange; font-weight: bold");
    r.warnings.forEach(w => console.log(`%c⚠️ ${w}`, "color: orange"));

    // Homework stays out of the way until the lab checklist on this page is done.
    const ph = r.homework.filter(c => c.pass).length;
    if (!allGreen) {
      console.log(`%c── Homework (Part 2) ── ${ph}/${r.homework.length} — finish the checklist above first`,
        "color: gray");
    } else {
      show("Homework (Part 2)", r.homework);
      console.log("%cThe extra component only has to appear on ONE page — a ❌ here is fine if another page has it.", "color: gray");
      console.log(`%c${ph}/${r.homework.length} homework`,
        ph === r.homework.length ? "color: green; font-weight: bold" : "color: orange; font-weight: bold");
    }
  }

  if (typeof module !== "undefined") module.exports = { runChecks };
})(this);
