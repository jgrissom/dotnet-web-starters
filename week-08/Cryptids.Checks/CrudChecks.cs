// ═══════════════════════════════════════════════════════════════════
//  READ-ONLY — these checks are how you know the lab is done. They are not
//  your grade: the points come from your DEPLOYED app (see homework.md).
//  Run them with:  dotnet test Cryptids.Checks   (from the parent folder)
//  Your job is turning ❌ into ✅ by editing Cryptids.Web — never this file.
//
//  This week the Registry gets the rest of CRUD: a scaffolded reference,
//  an Edit you ported, a Delete that asks first, and two new columns on a
//  table that already has rows. Same rules as week 7: everything runs
//  against an in-memory database, so no wifi and no SQL Server needed —
//  and 6/6 still doesn't prove your connection string works. Your browser
//  proves that.
// ═══════════════════════════════════════════════════════════════════
using System.Reflection;
using System.Text.RegularExpressions;
using Cryptids.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;

namespace Cryptids.Checks;

public class CrudChecks : IClassFixture<RegistryApp>
{
    private readonly RegistryApp _app;
    private readonly HttpClient _client;

    private const string Home = "/";
    private const string Index = "/Cryptids";

    public CrudChecks(RegistryApp app)
    {
        _app = app;
        _client = app.NewClient();
        _app.EnsureSeeded();        // the in-memory database exists before any page is asked for
    }

    private async Task<string> Html(string url) => await _client.GetStringAsync(url);

    private static List<int> DetailsIds(string html) =>
        Regex.Matches(html, @"/Cryptids/Details/(\d+)", RegexOptions.IgnoreCase)
             .Select(m => int.Parse(m.Groups[1].Value))
             .Distinct()
             .ToList();

    // Reads the first POST form on a page the way a browser would: every
    // input's name and current value, ready to send back. Checkbox inputs are
    // skipped — their hidden "false" partner is what an unticked box submits.
    private static Dictionary<string, string> FormFields(string html)
    {
        var form = Regex.Match(html, @"<form[^>]*method=""post""[\s\S]*?</form>", RegexOptions.IgnoreCase);
        var fields = new Dictionary<string, string>();
        if (!form.Success)
        {
            return fields;
        }

        foreach (Match m in Regex.Matches(form.Value, @"<input\b[^>]*>", RegexOptions.IgnoreCase))
        {
            string? At(string attr)
            {
                var a = Regex.Match(m.Value, $@"\b{attr}=""([^""]*)""", RegexOptions.IgnoreCase);
                return a.Success ? a.Groups[1].Value : null;
            }

            var type = At("type")?.ToLowerInvariant() ?? "text";
            if (type is "checkbox" or "submit" or "button")
            {
                continue;
            }

            var name = At("name");
            if (name == null || fields.ContainsKey(name))
            {
                continue;
            }

            fields[name] = At("value") ?? "";
        }
        return fields;
    }

    private async Task<HttpResponseMessage> PostForm(string url, Dictionary<string, string> fields) =>
        await _client.PostAsync(url, new FormUrlEncodedContent(fields));

    // The controller the Registry has had since week 4, found by name so a
    // renamed file fails loudly rather than mysteriously.
    private static Type RequireController()
    {
        var type = typeof(Program).Assembly.GetTypes()
            .FirstOrDefault(t => t.Name == "CryptidsController");
        return type ?? throw new Xunit.Sdk.XunitException(
            "there's no CryptidsController in Cryptids.Web any more. The lab adds actions to the "
            + "controller you already have — don't rename or delete it.");
    }

    [Fact] // passes out of the box — the app arrives as week 7 finished it
    public async Task Check1_TheSiteYouWereGivenWorks()
    {
        foreach (var url in new[] { Home, Index, $"{Index}/Details/1", $"{Index}/Create" })
        {
            var response = await _client.GetAsync(url);
            Assert.True(response.IsSuccessStatusCode,
                $"GET {url} should return 200 — this one passes before you touch anything. "
                + "It's week 7's finished Registry: the database-backed list, details and form. "
                + "If it's red, something in Cryptids.Web got broken; undo it before starting.");
        }
    }

    [Fact] // Task 3: an Edit action pair, ported from the scaffold — and async
    public async Task Check2_TheEditFormShowsTheRecord()
    {
        var controller = RequireController();

        var editGet = controller.GetMethods()
            .FirstOrDefault(m => m.Name == "Edit" && m.GetCustomAttribute<HttpPostAttribute>() == null);
        Assert.True(editGet != null,
            "CryptidsController has no Edit action. Task 3 ports one from the scaffold — the pair "
            + "of Edit methods in CryptidsScaffoldController move across (GET shows the form, POST "
            + "saves the correction), plus a Views/Cryptids/Edit.cshtml for them to render.");

        Assert.True(typeof(Task).IsAssignableFrom(editGet!.ReturnType),
            "your Edit action is synchronous. The scaffolder writes async, and this is the week "
            + "the Registry goes async with it:\n"
            + "    public async Task<IActionResult> Edit(int? id)\n"
            + "    {\n"
            + "        ...await _context.Cryptids.FindAsync(id);\n"
            + "    }\n"
            + "Port the scaffold's version rather than translating it back to sync.");

        var editPost = controller.GetMethods()
            .FirstOrDefault(m => m.Name == "Edit" && m.GetCustomAttribute<HttpPostAttribute>() != null);
        Assert.True(editPost != null,
            "there's an Edit GET but no [HttpPost] Edit action — the form has nowhere to send the "
            + "correction. Port both halves of the pair from the scaffold.");

        Assert.True(typeof(Task).IsAssignableFrom(editPost!.ReturnType),
            "your [HttpPost] Edit action is synchronous — port the scaffold's async version, and "
            + "await the SaveChangesAsync().");

        var page = await _client.GetAsync($"{Index}/Edit/2");
        Assert.True(page.IsSuccessStatusCode,
            $"GET {Index}/Edit/2 returned {(int)page.StatusCode}. The action exists, so the likely "
            + "gap is the view: the scaffold's Edit.cshtml lives in Views/CryptidsScaffold/, and "
            + "your copy of it belongs at Views/Cryptids/Edit.cshtml.");

        var html = await page.Content.ReadAsStringAsync();
        Assert.True(html.Contains(@"value=""Bigfoot"""),
            "the Edit form for /Cryptids/Edit/2 isn't pre-filled — Bigfoot's name should already "
            + "be in the Name box. The GET half looks the record up and hands it to the view:\n"
            + "    var cryptid = await _context.Cryptids.FindAsync(id);\n"
            + "    return View(cryptid);\n"
            + "An empty form here means the view got no model (or a new record's model).");

        Assert.True(Regex.IsMatch(html, @"name=""Id""[^>]*value=""2"""),
            "the Edit form has no hidden Id in it. This is the answer to last week's reading "
            + "question — the POST knows WHICH record it's editing because the form carries the id "
            + "along:\n"
            + "    <input type=\"hidden\" asp-for=\"Id\" />\n"
            + "Without it the posted record has Id 0, the URL says 2, and the action refuses the "
            + "mismatch.");
    }

    [Fact] // Task 3, the other half: a correction is an UPDATE, not an INSERT
    public async Task Check3_ACorrectionIsSaved()
    {
        RequireController();

        var before = DetailsIds(await Html(Index));

        var editPage = await _client.GetAsync($"{Index}/Edit/3");
        Assert.True(editPage.IsSuccessStatusCode,
            $"GET {Index}/Edit/3 returned {(int)editPage.StatusCode} — check 2 covers getting the "
            + "form on screen; this one needs it working first.");

        var fields = FormFields(await editPage.Content.ReadAsStringAsync());
        Assert.True(fields.Count > 0,
            "I couldn't find a POST form on your Edit page.");

        // Correct Mothman's report count, exactly as a visitor would: take the
        // form as rendered, change one box, send it back.
        fields["Sightings"] = "103";
        var response = await PostForm($"{Index}/Edit/3", fields);

        Assert.True((int)response.StatusCode is 302 or 303,
            (int)response.StatusCode == 404
                ? "posting the correction came back 404. The URL said record 3 but the posted form "
                  + "didn't agree — that's the missing hidden Id again: without it the form posts "
                  + "Id 0, and the action's  if (id != cryptid.Id) return NotFound();  fires."
                : $"posting a valid correction returned {(int)response.StatusCode} instead of a "
                  + "redirect. The POST half ends the same way Create always has:\n"
                  + "    return RedirectToAction(nameof(Index));");

        var after = DetailsIds(await Html(Index));
        Assert.True(after.Count == before.Count,
            $"the registry had {before.Count} records before the correction and {after.Count} "
            + "after it. An edit is an UPDATE to the row that exists — if the count grew, the POST "
            + "called Add and filed a duplicate instead of Update:\n"
            + "    _context.Update(cryptid);\n"
            + "    await _context.SaveChangesAsync();");

        var (scope, context) = _app.NewContext();
        using (scope)
        {
            var mothman = context.Set<Cryptid>().AsNoTracking().First(c => c.Id == 3);
            Assert.True(mothman.Sightings == 103,
                $"I corrected Mothman's report count to 103 through your form, got the redirect, "
                + $"and the database still says {mothman.Sightings}. A redirect with no saved "
                + "change means the UPDATE never ran — Update() only marks the record modified; "
                + "nothing reaches the database until SaveChangesAsync().");
        }
    }

    [Fact] // Task 4: Delete asks first, then deletes — and the scaffold is gone
    public async Task Check4_AFileCanBeClosed()
    {
        var controller = RequireController();

        var deleteGet = controller.GetMethods()
            .FirstOrDefault(m => m.Name == "Delete" && m.GetCustomAttribute<HttpPostAttribute>() == null);
        Assert.True(deleteGet != null,
            "CryptidsController has no Delete action. Task 4 ports the pair from the scaffold: a "
            + "GET that shows a confirmation page, and a POST (DeleteConfirmed) that actually "
            + "deletes. Two steps on purpose — a link must never change data.");

        Assert.True(typeof(Task).IsAssignableFrom(deleteGet!.ReturnType),
            "your Delete action is synchronous — port the scaffold's async version.");

        var before = DetailsIds(await Html(Index));

        var confirm = await _client.GetAsync($"{Index}/Delete/5");
        Assert.True(confirm.IsSuccessStatusCode,
            $"GET {Index}/Delete/5 returned {(int)confirm.StatusCode}. If the action is there, "
            + "the missing piece is usually the view — your copy of the scaffold's Delete.cshtml "
            + "belongs at Views/Cryptids/Delete.cshtml.");

        var confirmHtml = await confirm.Content.ReadAsStringAsync();
        Assert.True(confirmHtml.Contains("The Jersey Devil"),
            "the confirmation page for /Cryptids/Delete/5 doesn't show the record it's about to "
            + "delete. Nobody should confirm a deletion blind — the GET looks the record up and "
            + "renders it, exactly like Details does.");

        Assert.True(Regex.IsMatch(confirmHtml, @"<form[^>]*method=""post""", RegexOptions.IgnoreCase),
            "the confirmation page has no POST form on it, so there's no way to actually delete. "
            + "The page shows the record AND carries the button:\n"
            + "    <form asp-action=\"Delete\" method=\"post\">");

        var stillThere = DetailsIds(await Html(Index));
        Assert.True(stillThere.Count == before.Count,
            "loading the confirmation page DELETED the record. The GET half only ever shows the "
            + "page — the deletion belongs in the POST half, behind the button. A GET that changes "
            + "data gets triggered by link previews, browser prefetching and crawlers.");

        var fields = FormFields(confirmHtml);
        var response = await PostForm($"{Index}/Delete/5", fields);
        Assert.True((int)response.StatusCode is 302 or 303,
            $"posting the confirmation returned {(int)response.StatusCode} instead of a redirect. "
            + "The scaffold's POST half is DeleteConfirmed, with [HttpPost, ActionName(\"Delete\")] "
            + "keeping its URL at /Cryptids/Delete — port it as is.");

        var after = DetailsIds(await Html(Index));
        Assert.True(after.Count == before.Count - 1 && !after.Contains(5),
            "the confirmation posted and redirected, but The Jersey Devil is still in the "
            + "registry. Remove() only marks the record — the DELETE runs at SaveChangesAsync().");

        var details = await _client.GetAsync($"{Index}/Details/5");
        Assert.True(details.StatusCode == System.Net.HttpStatusCode.NotFound,
            $"the record is gone from the list, but {Index}/Details/5 still returns "
            + $"{(int)details.StatusCode}. Details should 404 for a record that no longer exists — "
            + "which it already does if it looks the id up in the table.");

        Assert.True(RegistryApp.ScaffoldIsGone,
            "Delete works — last step: CryptidsScaffoldController is still in the project. The "
            + "scaffold was the reference you ported from, and leaving it in ships a second, "
            + "unthemed admin UI at /CryptidsScaffold that nobody maintains. Delete the "
            + "controller file and the Views/CryptidsScaffold folder.");
    }

    [Fact] // Task 5: two nullable columns, added to a table that already has rows
    public async Task Check5_TheRegistryGrowsTwoColumns()
    {
        await Task.CompletedTask;

        foreach (var name in new[] { "LatinName", "ImageUrl" })
        {
            var property = typeof(Cryptid).GetProperty(name);
            Assert.True(property != null && property.PropertyType == typeof(string),
                $"Cryptid has no string property called {name} yet — task 5 adds it:\n"
                + $"    public string? {name} {{ get; set; }}");

            var nullability = new NullabilityInfoContext().Create(property!);
            Assert.True(nullability.WriteState == NullabilityState.Nullable,
                $"{name} is string, but not string? — the ? is the point. The table already has "
                + "rows, and every record filed through your form arrives without a "
                + $"{name}. Nullable means 'this column may be empty', which is simply true here. "
                + "(It's also why the views can fall back to the unillustrated plate.)");
        }

        // The migration has to be ADDITIVE. Deleting the Migrations folder and
        // regenerating stopped being the reset button the moment the table
        // held data your database remembers applying.
        var migrations = typeof(Program).Assembly.GetTypes()
            .Where(t => typeof(Migration).IsAssignableFrom(t)
                     && !t.IsAbstract
                     && t.GetCustomAttribute<MigrationAttribute>() != null)
            .Select(t => (Migration)Activator.CreateInstance(t)!)
            .ToList();

        var addedColumns = migrations
            .SelectMany(m => m.UpOperations)
            .OfType<AddColumnOperation>()
            .Where(o => o.Table.Equals("Cryptids", StringComparison.OrdinalIgnoreCase))
            .Select(o => o.Name)
            .ToList();

        Assert.True(addedColumns.Contains("LatinName") && addedColumns.Contains("ImageUrl"),
            "no migration ADDS the LatinName and ImageUrl columns. This week that distinction "
            + "matters: your table already has rows, and your database's __EFMigrationsHistory "
            + "already lists InitialCreate as applied — so deleting the Migrations folder and "
            + "regenerating one big migration can never be applied to it. The move is additive:\n"
            + "    dotnet ef migrations add AddFieldGuidePlates\n"
            + "    dotnet ef database update\n"
            + $"(Migrations found: {migrations.Count}; columns added by them: "
            + $"{(addedColumns.Count == 0 ? "none" : string.Join(", ", addedColumns))})");

        // And the six archive records get their Latin names through the seed
        // data, so the same migration carries an UpdateData for each.
        var (scope, context) = _app.NewContext();
        using (scope)
        {
            var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Cryptid));
            var seed = entity!.GetSeedData().ToList();

            var latinNames = seed.Select(row => row["LatinName"] as string).ToList();
            var expected = new[]
            {
                "Bovine spiritus", "Gigantopithecus canadensis", "Noctua pontiensis",
                "Nessiteras rhombopteryx", "Diabolus pinorum", "Caprivorus portoricensis",
            };
            foreach (var name in expected)
            {
                Assert.True(latinNames.Contains(name),
                    $"\"{name}\" isn't in your seed data. The six archive records get their Latin "
                    + "names in HasData — the exact spellings are in the lab README, and the "
                    + "migration turns the change into six UPDATEs. "
                    + $"(I found: {string.Join(", ", latinNames.Select(n => n ?? "null"))})");
            }

            var plates = seed.Select(row => row["ImageUrl"] as string).ToList();
            Assert.True(plates.All(p => p != null && p.StartsWith("/img/cryptids/")),
                "every seeded record needs its plate: an ImageUrl under /img/cryptids/. The seven "
                + "images already sit in wwwroot/img/cryptids/ — the seed data just has to point "
                + $"at them. (I found: {string.Join(", ", plates.Select(p => p ?? "null"))})");
        }
    }

    [Fact] // Task 6: the plates are on display, and the new fields are editable
    public async Task Check6_ThePlatesAreOnDisplay()
    {
        RequireController();

        var indexHtml = await Html(Index);
        // Count against the records actually on the page, not a fixed 6: check 4
        // closes a file, and these checks share one in-memory database.
        var onIndex = DetailsIds(indexHtml).Count;
        Assert.True(onIndex > 0 && Regex.Matches(indexHtml, "/img/cryptids/").Count >= onIndex,
            "the registry page doesn't show the plates. The card partial gets an image at the "
            + "top:\n"
            + "    <img src=\"@(Model.ImageUrl ?? \"/img/cryptids/unillustrated.webp\")\" "
            + "class=\"card-img-top\" alt=\"Field-guide plate: @Model.Name\" />");

        // A record with no plate is normal, not broken — file one straight into
        // the database and the card should fall back to the archivist's
        // "artist unknown" placeholder.
        var (scope, context) = _app.NewContext();
        using (scope)
        {
            if (!context.Set<Cryptid>().Any(c => c.Name == "The Beast of Bray Road"))
            {
                context.Add(new Cryptid
                {
                    Name = "The Beast of Bray Road",
                    Region = "Elkhorn, Wisconsin",
                    FirstSighting = 1936,
                    Sightings = 42,
                    IsDebunked = false,
                });
                context.SaveChanges();
            }
        }

        var withNewRecord = await Html(Index);
        Assert.True(withNewRecord.Contains("unillustrated.webp"),
            "I filed a record with no ImageUrl, and nothing on the page shows the placeholder "
            + "plate. That's what the ?? is for — null means 'no plate on file', and the card "
            + "shows /img/cryptids/unillustrated.webp instead of a broken image.");

        var detailsHtml = await Html($"{Index}/Details/1");
        Assert.True(detailsHtml.Contains("hodag.webp"),
            "the details page for The Hodag doesn't show its plate. Details gets the same image "
            + "treatment as the card — src from ImageUrl, with the placeholder as fallback.");

        var homeHtml = await Html(Home);
        Assert.True(homeHtml.Contains("/img/cryptids/"),
            "the home page doesn't feature a record. Task 6 puts HomeController on the context — "
            + "the same constructor move as CryptidsController — and Index hands the view one "
            + "random record:\n"
            + "    var featured = await _context.Cryptids.OrderBy(c => Guid.NewGuid()).FirstOrDefaultAsync();\n"
            + "    return View(featured);");

        // The new columns are editable — and the [Bind] guest list lets them in.
        var editPage = await _client.GetAsync($"{Index}/Edit/1");
        Assert.True(editPage.IsSuccessStatusCode,
            "the Edit form isn't loading — checks 2 and 3 cover it; this one builds on them.");

        var editHtml = await editPage.Content.ReadAsStringAsync();
        Assert.True(editHtml.Contains(@"name=""LatinName"""),
            "the Edit form has no LatinName field. Views don't update themselves when the model "
            + "grows — the scaffold generated yours before the column existed, so the two new "
            + "fields get added by hand, same shape as the others.");

        var fields = FormFields(editHtml);
        fields["LatinName"] = "Bovine spiritus emend.";
        var response = await PostForm($"{Index}/Edit/1", fields);
        Assert.True((int)response.StatusCode is 302 or 303,
            $"correcting The Hodag's Latin name returned {(int)response.StatusCode} instead of a "
            + "redirect.");

        var (scope2, context2) = _app.NewContext();
        using (scope2)
        {
            var hodag = context2.Set<Cryptid>().AsNoTracking().First(c => c.Id == 1);
            // Read by reflection: this file has to compile against the starter,
            // where the property doesn't exist yet.
            var latin = typeof(Cryptid).GetProperty("LatinName")?.GetValue(hodag) as string;
            Assert.True(latin == "Bovine spiritus emend.",
                "I corrected The Hodag's Latin name through your form, got the redirect, and my "
                + $"correction never reached the database (it now holds: {latin ?? "null"}). The "
                + "field is on the form — so the culprit is the [Bind] list on your Edit POST: "
                + "it's a guest list, only the names on it are read out of the form, and "
                + "LatinName and ImageUrl aren't on yours yet. Worse than ignored: the unbound "
                + "field arrives null and Update() writes the null. Add both names to the list.");
        }
    }
}
