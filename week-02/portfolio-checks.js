// Week 2 Portfolio Self-Check
// Already wired into each starter page. Open the console (F12) on any page —
// it grades THAT page's checklist. Refresh after each change; work one ❌ at a time.
// (Leave this file in when you deploy — it doesn't affect grading.)

(function (root) {
  function runChecks(doc, page) {
    const $ = (sel) => doc.querySelector(sel);
    const $$ = (sel) => doc.querySelectorAll(sel);
    const styleText = [...$$("style")].map(s => s.textContent).join("\n");
    const results = { page, required: [], yours: [], warnings: [] };
    const req = (label, pass) => results.required.push({ label, pass: !!pass });
    const yours = (label, pass) => results.yours.push({ label, pass: !!pass });

    // ── every page ──
    req("viewport meta present", $('meta[name="viewport"]'));
    req("Bootstrap CSS linked", $('link[href*="bootstrap"][rel="stylesheet"]'));
    req("Bootstrap JS bundle at end of body", $('script[src*="bootstrap.bundle"]'));
    req("navbar with brand", $("nav.navbar") && $(".navbar-brand"));
    req("navbar toggler + collapse (hamburger works)", $(".navbar-toggler") && $(".navbar-collapse"));
    req("nav links to all 3 pages", $$("a.nav-link").length >= 3);
    req("current page marked .active in nav", $("a.nav-link.active"));
    req("Bootstrap Icons stylesheet linked", $('link[href*="bootstrap-icons"]'));
    req("at least one icon used (bi-*)", $('[class*="bi-"]'));
    req("footer: centered, muted, padded", $("footer.text-center") && $("footer .mb-0, footer p") && /text-muted/.test($("footer")?.className + " " + ($("footer p")?.className || "")));

    // ── per page ──
    if (page === "index") {
      req("hero: display-* heading", $('[class*="display-"]'));
      req("hero: lead paragraph", $(".lead"));
      req("hero: button to projects page", [...$$('a[href*="projects"]')].some(a => /btn/.test(a.className)));
      req("feature row: 3+ columns in a row", $(".row") && $$('.row [class*="col"]').length >= 3);
    }
    if (page === "projects") {
      req("6+ cards", $$(".card").length >= 6);
      req("cards live in responsive columns", $$('[class*="col-"] .card').length >= 6);
      req("cards use h-100 (equal heights)", $$(".card.h-100").length >= 6);
      req("row has gutters (g-*)", [...$$(".row")].some(r => /(^|\s)g[xy]?-\d/.test(r.className)));
      req("badges on cards", $$(".card .badge").length >= 3);
    }
    if (page === "contact") {
      req("form present", $("form"));
      req("3+ form-control inputs", $$(".form-control").length >= 3);
      req("labels use form-label", $$(".form-label").length >= 3);
      req("a form-select dropdown", $(".form-select"));
      req("submit button", $('button[type="submit"], form button'));
      req("info alert above the form", $(".alert"));
      req("form constrained with the grid (col-*)", [...$$('[class*="col-"]')].some(c => c.querySelector("form")));
    }

    // ── make it yours ──
    yours("Bootswatch theme (not stock Bootstrap)", $('link[href*="bootswatch"]'));
    yours("Google Fonts linked", $('link[href*="fonts.googleapis.com"]'));
    yours("--bs-body-font-family override", /--bs-body-font-family/.test(styleText));

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
    const path = location.pathname.toLowerCase();
    const page = path.includes("projects") ? "projects" : path.includes("contact") ? "contact" : "index";
    const r = runChecks(document, page);
    const show = (title, list) => {
      console.log(`%c── ${title} ──`, "font-weight: bold");
      list.forEach(c => console.log(`${c.pass ? "✅" : "❌"} ${c.label}`));
    };
    console.log(`%c── Week 2 Self-Check: ${page}.html ──`, "font-weight: bold; font-size: 1.1em");
    show("Required", r.required);
    show("Make it yours", r.yours);
    const pr = r.required.filter(c => c.pass).length, py = r.yours.filter(c => c.pass).length;
    console.log(`%c${pr}/${r.required.length} required · ${py}/${r.yours.length} make-it-yours — check all 3 pages!`,
      pr === r.required.length ? "color: green; font-weight: bold" : "color: orange; font-weight: bold");
    r.warnings.forEach(w => console.log(`%c⚠️ ${w}`, "color: orange"));
  }

  if (typeof module !== "undefined") module.exports = { runChecks };
})(this);
