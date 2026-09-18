// ═══════════════════════════════════════════════════════════════════
//  READ-ONLY — these checks are how you know the lab is done. They are not
//  your grade: the points come from your DEPLOYED app (see homework.md).
//  Run them with:  dotnet test Cryptids.Checks   (from the parent folder)
//  Your job is turning ❌ into ✅ by editing Cryptids.Web — never this file.
//
//  This week the Registry grows a second table. "Reports on file" stops
//  being a number somebody typed and becomes the reports themselves, each
//  one pointing back at a creature. Same rules as weeks 7 and 8: everything
//  runs against an in-memory database, so no wifi and no SQL Server needed —
//  and 6/6 still doesn't prove your connection string works. Your browser
//  proves that.
//
//  Nothing below names your Sighting class as a type, because on the day you
//  start it doesn't exist yet. The checks find it the way the database does:
//  by looking for an entity with a foreign key pointing at Cryptid.
// ═══════════════════════════════════════════════════════════════════
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Cryptids.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace Cryptids.Checks;

public class RelatedDataChecks : IClassFixture<RegistryApp>
{
    private readonly RegistryApp _app;
    private readonly HttpClient _client;

    private const string Home = "/";
    private const string Index = "/Cryptids";

    public RelatedDataChecks(RegistryApp app)
    {
        _app = app;
        _client = app.NewClient();
        _app.EnsureSeeded();
    }

    private async Task<string> Html(string url) => await _client.GetStringAsync(url);

    // Everything the page actually SAYS, with the markup taken out. Counts are
    // matched against this rather than the raw HTML, because "col-md-4" and
    // "row-cols-md-3" contain digits with word boundaries around them and will
    // happily satisfy a search for the number 4.
    private static string VisibleText(string html) =>
        WebUtility.HtmlDecode(Regex.Replace(Regex.Replace(html,
            @"<(script|style)\b[\s\S]*?</\1>", " ", RegexOptions.IgnoreCase),
            @"<[^>]*>", " "));

    // ── Finding the second table without naming it ────────────────────────
    //
    // "The entity that holds a foreign key pointing at Cryptid." That is the
    // definition of the dependent side of a one-to-many, and it is true no
    // matter what you called the class.

    private static IEntityType? DependentOf(DbContext context) =>
        context.Model.GetEntityTypes()
            .FirstOrDefault(e => e.ClrType != typeof(Cryptid)
                && e.GetForeignKeys().Any(fk => fk.PrincipalEntityType.ClrType == typeof(Cryptid)));

    // The collection Cryptid holds — Cryptid.Sightings, if you named it that.
    private static INavigation? CollectionOnCryptid(DbContext context) =>
        context.Model.FindEntityType(typeof(Cryptid))?
            .GetNavigations()
            .FirstOrDefault(n => n.IsCollection);

    private (IServiceScope scope, DbContext context) Db() => _app.NewContext();

    private const string MakeTheModel =
        "Task 1 creates it: a Sighting class in Models/, with an int CryptidId foreign key and a "
        + "Cryptid? navigation property — and, on Cryptid, "
        + "public ICollection<Sighting> Sightings { get; set; } = new List<Sighting>();";

    // ── 1 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Check1_TheRegistryYouFinishedLastWeekStillWorks()
    {
        foreach (var url in new[] { Home, Index, $"{Index}/Details/1", $"{Index}/Create" })
        {
            var response = await _client.GetAsync(url);
            Assert.True(response.IsSuccessStatusCode,
                $"GET {url} should return 200 — this one passes before you touch anything. "
                + "It's week 8's finished Registry: the list, the details page, the form and full "
                + "CRUD. If it's red, something in Cryptids.Web got broken; undo it before starting.");
        }
    }

    // ── 2 ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Check2_ReportsOnFileIsNoLongerANumberYouTyped()
    {
        var (scope, context) = Db();
        using (scope)
        {
            var dependent = DependentOf(context);
            Assert.True(dependent != null,
                "there is no second table yet — nothing in your context has a foreign key "
                + "pointing at Cryptid. " + MakeTheModel);

            var collection = CollectionOnCryptid(context);
            Assert.True(collection != null,
                "your Sighting class exists, but Cryptid has no collection pointing back at it, "
                + "so nothing can ask a creature for its reports. Add "
                + "public ICollection<Sighting> Sightings { get; set; } = new List<Sighting>(); "
                + "to Cryptid.");

            // The old int has to be gone, not renamed alongside the collection.
            // Two properties both claiming to be the report count is exactly
            // the ambiguity this week exists to remove.
            var scalarCount = typeof(Cryptid)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => p.PropertyType == typeof(int)
                    && p.Name.Contains("Sighting", StringComparison.OrdinalIgnoreCase)
                    && !p.Name.Equals("FirstSighting", StringComparison.OrdinalIgnoreCase));

            Assert.True(scalarCount == null,
                $"Cryptid still has an int called {scalarCount?.Name}. That was the made-up number "
                + "— 47 reports for The Hodag because someone typed 47. The rows are the count "
                + "now, so delete the int property, take its field off Create.cshtml and "
                + "Edit.cshtml, and take its name out of the [Bind] list on the Edit POST.");
        }
    }

    // ── 3 ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Check3_TheTableHasRealAccountsInIt()
    {
        var (scope, context) = Db();
        using (scope)
        {
            var dependent = DependentOf(context);
            Assert.True(dependent != null, "no second table yet. " + MakeTheModel);

            var rows = (IEnumerable<object>)context
                .GetType()
                .GetMethod(nameof(DbContext.Set), 1, Type.EmptyTypes)!
                .MakeGenericMethod(dependent!.ClrType)
                .Invoke(context, null)!;

            var all = rows.Cast<object>().ToList();
            Assert.True(all.Count > 0,
                "the Sightings table is empty. Task 2 seeds it in OnModelCreating with "
                + "modelBuilder.Entity<Sighting>().HasData(...), the same way the creatures "
                + "themselves are seeded — then a migration carries the rows into the database.");

            // Every row must actually point at a creature. A seeded FK of 0 is
            // the classic copy-paste slip and the page would show nothing.
            var fk = dependent.GetForeignKeys()
                .First(f => f.PrincipalEntityType.ClrType == typeof(Cryptid))
                .Properties[0];
            var fkProp = dependent.ClrType.GetProperty(fk.Name)!;

            var cryptidIds = context.Set<Cryptid>().Select(c => c.Id).ToHashSet();
            var orphans = all.Count(r => !cryptidIds.Contains((int)fkProp.GetValue(r)!));

            Assert.True(orphans == 0,
                $"{orphans} seeded report(s) point at a creature id that isn't in the registry. "
                + $"Every row needs a {fk.Name} matching one of the six seeded Cryptid ids.");
        }
    }

    // ── 4 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Check4_ThePagesActuallyShowTheAccounts()
    {
        var (scope, context) = Db();
        string witness;
        int expectedForFirst;
        using (scope)
        {
            var dependent = DependentOf(context);
            Assert.True(dependent != null, "no second table yet. " + MakeTheModel);

            var fkName = dependent!.GetForeignKeys()
                .First(f => f.PrincipalEntityType.ClrType == typeof(Cryptid))
                .Properties[0].Name;
            var fkProp = dependent.ClrType.GetProperty(fkName)!;

            var rows = ((IEnumerable<object>)context
                .GetType()
                .GetMethod(nameof(DbContext.Set), 1, Type.EmptyTypes)!
                .MakeGenericMethod(dependent.ClrType)
                .Invoke(context, null)!).ToList();

            var firstFor1 = rows.FirstOrDefault(r => (int)fkProp.GetValue(r)! == 1);
            Assert.True(firstFor1 != null,
                "none of your seeded reports belong to creature 1, so there is nothing to look "
                + "for on its details page. Give The Hodag at least one.");

            // The longest string on the row is the account; the shortest useful
            // one is usually the witness. Either will do — we just need text
            // that can only be on the page if the related row was loaded.
            var strings = dependent.ClrType.GetProperties()
                .Where(p => p.PropertyType == typeof(string))
                .Select(p => p.GetValue(firstFor1) as string)
                .Where(v => !string.IsNullOrWhiteSpace(v) && v!.Length > 8)
                .ToList();

            Assert.True(strings.Count > 0,
                "your seeded report for creature 1 has no text on it worth showing. Give a "
                + "sighting something to say — who reported it, and what they saw.");

            witness = strings.OrderByDescending(v => v!.Length).First()!;
            expectedForFirst = rows.Count(r => (int)fkProp.GetValue(r)! == 1);
        }

        var detailsHtml = await Html($"{Index}/Details/1");
        Assert.True(detailsHtml.Contains(WebUtility.HtmlEncode(witness)) || detailsHtml.Contains(witness),
            "the details page for creature 1 doesn't show its reports, even though the database "
            + "has them. This is the one that catches everybody: a navigation property is EMPTY "
            + "until the query asks for it. In CryptidsController.Details:\n"
            + "    var cryptid = _context.Cryptids\n"
            + "        .Include(c => c.Sightings)\n"
            + "        .FirstOrDefault(c => c.Id == id);\n"
            + "...and then render Model.Sightings in the view. No exception, no warning — an "
            + "empty list looks exactly like a creature nobody has reported.");

        var indexHtml = VisibleText(await Html(Index));
        Assert.True(Regex.IsMatch(indexHtml, $@"\b{expectedForFirst}\b"),
            $"the registry page never prints the number {expectedForFirst}, which is how many "
            + "reports creature 1 actually has. The card counts the rows now — "
            + "@Model.Sightings.Count — and Index needs its own .Include(c => c.Sightings) to "
            + "load them. Include is per-query: adding it to Details did nothing for this page.");
    }

    // ── 5 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Check5_AnyoneCanFileAReport()
    {
        var formUrl = await FindReportForm();

        var formHtml = await Html(formUrl);
        Assert.True(Regex.IsMatch(formHtml, "<select", RegexOptions.IgnoreCase),
            $"{formUrl} has no <select> on it. A report has to say WHICH creature it is about, "
            + "and the reporter picks it from a list. That list is a SelectList, built in the "
            + "controller and handed to the view:\n"
            + "    Cryptids = new SelectList(_context.Cryptids.OrderBy(c => c.Name), \"Id\", \"Name\")\n"
            + "...then <select asp-for=\"Sighting.CryptidId\" asp-items=\"Model.Cryptids\">.");

        Assert.True(Regex.Matches(formHtml, "<option", RegexOptions.IgnoreCase).Count > 1,
            "the dropdown on your report form has no creatures in it. asp-items needs the "
            + "SelectList the controller built — and if the POST rejects a report it has to "
            + "REBUILD that list before redisplaying the form, because a dropdown is never "
            + "posted back.");

        var before = await CountRows();

        // The bad report first: nothing should be filed.
        var bad = await PostReport(formUrl, formHtml, valid: false);
        Assert.True((int)bad.StatusCode == 200,
            $"posting an empty report returned {(int)bad.StatusCode}. A rejected form comes back "
            + "as the form again — return View(form) — not a redirect and not a crash.");

        var afterBad = await CountRows();
        Assert.True(afterBad == before,
            $"an empty report was still filed ({before} rows became {afterBad}). The POST needs "
            + "its guard: if (!ModelState.IsValid) rebuild the dropdown and return View(form).");

        // Then the real one.
        var good = await PostReport(formUrl, formHtml, valid: true);
        Assert.True((int)good.StatusCode is 301 or 302 or 303,
            $"a valid report returned {(int)good.StatusCode} instead of a redirect. After saving, "
            + "send the browser somewhere: RedirectToAction(\"Details\", \"Cryptids\", "
            + "new { id = form.Sighting.CryptidId }).");

        var afterGood = await CountRows();
        Assert.True(afterGood == before + 1,
            $"a valid report didn't reach the database ({before} rows, still {afterGood} after). "
            + "_context.Sightings.Add(form.Sighting) remembers it; SaveChangesAsync() writes it.");
    }

    // ── 6 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Check6_ClosingAFileSaysWhatElseGoesWithIt()
    {
        var (scope, context) = Db();
        var counts = new List<(int cryptidId, int attached)>();
        using (scope)
        {
            var dependent = DependentOf(context);
            Assert.True(dependent != null, "no second table yet. " + MakeTheModel);

            var fkName = dependent!.GetForeignKeys()
                .First(f => f.PrincipalEntityType.ClrType == typeof(Cryptid))
                .Properties[0].Name;
            var fkProp = dependent.ClrType.GetProperty(fkName)!;

            var rows = ((IEnumerable<object>)context
                .GetType()
                .GetMethod(nameof(DbContext.Set), 1, Type.EmptyTypes)!
                .MakeGenericMethod(dependent.ClrType)
                .Invoke(context, null)!).ToList();

            counts = rows.GroupBy(r => (int)fkProp.GetValue(r)!)
                         .Select(g => (cryptidId: g.Key, attached: g.Count()))
                         .OrderByDescending(g => g.attached)
                         .ToList();
        }

        // Two creatures with DIFFERENT report counts. One page could show the
        // right number by accident; two can't, and a hard-coded number can't
        // be right on both.
        var busiest = counts.First();
        var quieter = counts.FirstOrDefault(c => c.attached != busiest.attached);

        Assert.True(quieter.cryptidId != 0,
            "every creature in your seed has the same number of reports, so I can't tell a real "
            + "count from a typed one. Give them different numbers of sightings — some creatures "
            + "are better documented than others.");

        foreach (var (cryptidId, attached) in new[] { busiest, quieter })
        {
            var deleteText = VisibleText(await Html($"{Index}/Delete/{cryptidId}"));

            Assert.True(Regex.IsMatch(deleteText, $@"\b{attached}\b"),
                $"creature {cryptidId} has {attached} report(s) attached, and the Close-the-file "
                + $"page never says {attached} anywhere a human can read it. Deleting the creature "
                + "deletes those reports too — the foreign key was created with onDelete: Cascade "
                + "— so the page that asks 'are you sure' has to say what else is about to go.\n"
                + "Two halves to this: load them in the Delete GET with\n"
                + "    .Include(c => c.Sightings)\n"
                + "and then show Model.Sightings.Count on the page. Without the Include the count "
                + "is 0 and the warning quietly never appears.");
        }
    }

    // ── plumbing for check 5 ──────────────────────────────────────────────

    // The report form is wherever you put it. Try the conventional route
    // first, then any link on a creature's page that isn't already known.
    private async Task<string> FindReportForm()
    {
        foreach (var candidate in new[] { "/Sightings/Create", "/Sighting/Create", "/Reports/Create" })
        {
            var probe = await _client.GetAsync(candidate);
            if (probe.IsSuccessStatusCode)
            {
                return candidate;
            }
        }

        var detailsHtml = await Html($"{Index}/Details/1");
        foreach (Match m in Regex.Matches(detailsHtml, @"href=""(/[^""#]*[Cc]reate[^""]*)"""))
        {
            var href = m.Groups[1].Value;
            if (href.StartsWith("/Cryptids", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var probe = await _client.GetAsync(href);
            if (probe.IsSuccessStatusCode)
            {
                return href;
            }
        }

        Assert.Fail(
            "I couldn't find a form for filing a report. Task 4 builds one: a SightingsController "
            + "with a Create pair, a Views/Sightings/Create.cshtml carrying the creature dropdown, "
            + "and a link to it from a creature's details page. I looked at /Sightings/Create and "
            + "at every Create link on /Cryptids/Details/1.");
        return "";
    }

    private async Task<int> CountRows()
    {
        var (scope, context) = Db();
        using (scope)
        {
            var dependent = DependentOf(context)!;
            var rows = (IEnumerable<object>)context
                .GetType()
                .GetMethod(nameof(DbContext.Set), 1, Type.EmptyTypes)!
                .MakeGenericMethod(dependent.ClrType)
                .Invoke(context, null)!;
            return rows.Cast<object>().Count();
        }
    }

    // Fills in the form the way a browser would, using the field names the
    // page itself declares — so nothing here depends on what you called your
    // properties. An invalid report leaves every value blank except the token.
    private async Task<HttpResponseMessage> PostReport(string url, string formHtml, bool valid)
    {
        var (scope, context) = Db();
        var fields = new Dictionary<string, string>();
        using (scope)
        {
            var dependent = DependentOf(context)!;
            var fkName = dependent.GetForeignKeys()
                .First(f => f.PrincipalEntityType.ClrType == typeof(Cryptid))
                .Properties[0].Name;

            var token = Regex.Match(formHtml,
                @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
            if (token.Success)
            {
                fields["__RequestVerificationToken"] = token.Groups[1].Value;
            }

            var form = Regex.Match(formHtml, @"<form[^>]*method=""post""[\s\S]*?</form>",
                RegexOptions.IgnoreCase);
            var body = form.Success ? form.Value : formHtml;

            foreach (Match m in Regex.Matches(body, @"name=""([^""]+)""") )
            {
                var name = m.Groups[1].Value;
                if (name == "__RequestVerificationToken" || fields.ContainsKey(name))
                {
                    continue;
                }

                fields[name] = valid ? ValueFor(name, fkName, dependent) : "";
            }
        }

        return await _client.PostAsync(url, new FormUrlEncodedContent(fields));
    }

    // A value the student's own rules will accept, worked out from their model.
    private static string ValueFor(string fieldName, string fkName, IEntityType dependent)
    {
        var bare = fieldName.Contains('.') ? fieldName[(fieldName.LastIndexOf('.') + 1)..] : fieldName;

        if (bare.Equals(fkName, StringComparison.OrdinalIgnoreCase))
        {
            return "1";
        }

        var prop = dependent.ClrType.GetProperty(bare);
        if (prop == null)
        {
            return "1";
        }

        var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

        if (type == typeof(DateTime))
        {
            return DateTime.Today.ToString("yyyy-MM-dd");
        }

        if (type == typeof(int) || type == typeof(long))
        {
            return "1";
        }

        if (type == typeof(decimal) || type == typeof(double))
        {
            return "1";
        }

        if (type == typeof(bool))
        {
            return "false";
        }

        // A string long enough for any MinimumLength, clipped to MaxLength.
        var maxLength = dependent.FindProperty(bare)?.GetMaxLength() ?? 200;
        var text = "Seen from the county road just after dusk, moving away from the water.";
        return text.Length > maxLength ? text[..maxLength] : text;
    }
}
