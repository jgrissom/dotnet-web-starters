// ═══════════════════════════════════════════════════════════════════
//  READ-ONLY — these checks are how you know the lab is done. They are not
//  your grade: the points come from your DEPLOYED app (see homework.md).
//  Run them with:  dotnet test Cryptids.Checks   (from the parent folder)
//  Your job is turning ❌ into ✅ by editing Cryptids.Web — never this file.
//
//  This week the Registry takes input. The checks fill your form in and post
//  it, the same way a browser would — once with good data and once with bad —
//  and then look at what your app did about it.
// ═══════════════════════════════════════════════════════════════════
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.RegularExpressions;
using Cryptids.Web.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Cryptids.Checks;

public class FormChecks : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    private const string Home = "/";
    private const string Index = "/Cryptids";
    private const string Details1 = "/Cryptids/Details/1";      // The Hodag
    private const string Create = "/Cryptids/Create";

    // The six creatures the archive ships with. Anything else on the index
    // got there through your form.
    private static readonly int[] SeededIds = { 1, 2, 3, 4, 5, 6 };

    public FormChecks(WebApplicationFactory<Program> factory)
    {
        // AllowAutoRedirect: false — check 4 needs to SEE the redirect your POST
        // returns, not silently follow it. HandleCookies stays on so the
        // antiforgery cookie survives from the GET to the POST, like a browser.
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
    }

    private async Task<string> Html(string url) => await _client.GetStringAsync(url);

    // Every /Cryptids/Details/N id currently linked from the index page.
    // hrefs are URLs, and case never matters in a URL.
    private static List<int> DetailsIds(string html) =>
        Regex.Matches(html, @"/Cryptids/Details/(\d+)", RegexOptions.IgnoreCase)
             .Select(m => int.Parse(m.Groups[1].Value))
             .Distinct()
             .ToList();

    // The hidden field the <form asp-action="..."> tag helper writes for you.
    private static string AntiForgeryToken(string html)
    {
        var m = Regex.Match(html,
            @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
        return m.Success ? m.Groups[1].Value : "";
    }

    private static PropertyInfo Prop(string name) =>
        typeof(Cryptid).GetProperty(name)
        ?? throw new Xunit.Sdk.XunitException(
            $"the Cryptid model has no {name} property — did you rename it? "
            + "The lab needs the six properties it shipped with.");

    private static bool Has<T>(string prop) where T : Attribute =>
        Prop(prop).GetCustomAttribute<T>() != null;

    // Fills your form in and submits it, carrying the antiforgery token the way
    // a browser does. Fails with a pointer to task 3 if the page isn't there yet.
    private async Task<HttpResponseMessage> SubmitForm(Dictionary<string, string> fields)
    {
        var page = await _client.GetAsync(Create);
        Assert.True(page.IsSuccessStatusCode,
            $"GET {Create} returned {(int)page.StatusCode} — I can't submit a form that isn't "
            + "there yet. Task 3 builds the page; this check is task 4 or later.");

        var html = await page.Content.ReadAsStringAsync();
        var token = AntiForgeryToken(html);
        if (token != "") fields["__RequestVerificationToken"] = token;

        return await _client.PostAsync(Create, new FormUrlEncodedContent(fields));
    }

    [Fact] // passes out of the box — the app you were handed already works
    public async Task Check1_TheSiteYouWereGivenWorks()
    {
        foreach (var url in new[] { Home, Index, Details1 })
        {
            var response = await _client.GetAsync(url);
            Assert.True(response.IsSuccessStatusCode,
                $"GET {url} should return 200 — this one passes before you touch anything. "
                + "If it's red, something in Cryptids.Web got broken; undo it before starting the lab.");
        }
    }

    [Fact] // Task 2: the rules live on the model, as data annotations
    public async Task Check2_TheModelCarriesItsRules()
    {
        await Task.CompletedTask;

        Assert.True(Has<RequiredAttribute>("Name"),
            "Cryptid.Name has no [Required] attribute. The rules for a field belong on the "
            + "model, in Models/Cryptid.cs — that's the one place both the form and the "
            + "controller can see them.");

        Assert.True(Has<StringLengthAttribute>("Name"),
            "Cryptid.Name has no [StringLength] attribute. Add [StringLength(60, MinimumLength = 2)] "
            + "so a name can't be one stray character or a paragraph.");

        Assert.True(Has<RequiredAttribute>("Region"),
            "Cryptid.Region has no [Required] attribute. A report with no region isn't a report.");

        Assert.True(Has<RangeAttribute>("FirstSighting"),
            "Cryptid.FirstSighting has no [Range] attribute. Add [Range(500, 2026)] — a first "
            + "sighting in the year 40000 is a typo, and the model is where you say so.");

        Assert.True(Has<RangeAttribute>("Sightings"),
            "Cryptid.Sightings has no [Range] attribute. Add [Range(0, 100000)] — a negative "
            + "number of reports is not a thing.");

        var display = Prop("FirstSighting").GetCustomAttribute<DisplayAttribute>();
        Assert.True(display?.Name == "First sighted",
            "Cryptid.FirstSighting needs [Display(Name = \"First sighted\")]. Without it your form "
            + $"labels the field \"FirstSighting\", which is a property name, not English. "
            + $"(I found: {(display?.Name == null ? "no [Display] attribute" : $"\"{display.Name}\"")})");
    }

    [Fact] // Task 3: a real form, reachable from the registry
    public async Task Check3_TheFormPageExists()
    {
        var response = await _client.GetAsync(Create);
        Assert.True(response.IsSuccessStatusCode,
            $"GET {Create} returned {(int)response.StatusCode}. You need a Create() action on "
            + "CryptidsController and a Views/Cryptids/Create.cshtml to go with it.");

        var html = await response.Content.ReadAsStringAsync();

        Assert.True(Regex.IsMatch(html, @"<form[^>]*method=""post""", RegexOptions.IgnoreCase),
            "the Create page has no <form method=\"post\"> on it. A form that doesn't post "
            + "can't send anything anywhere.");

        // Razor adds this to EVERY <form method="post"> — hand-written or not — so its
        // absence means it was actively suppressed, and the POST will come back a 400.
        Assert.True(html.Contains("__RequestVerificationToken"),
            "your form has no antiforgery token. Razor puts one in every post form for free, so "
            + "something turned it off — asp-antiforgery=\"false\", or a <form> that isn't "
            + "method=\"post\". Without it, a POST action marked [ValidateAntiForgeryToken] "
            + "rejects the submission with a 400 before your code ever runs.");

        foreach (var field in new[] { "Name", "Region", "FirstSighting", "Sightings", "IsDebunked" })
        {
            Assert.True(Regex.IsMatch(html, $@"name=""{field}""", RegexOptions.IgnoreCase),
                $"the form has no input named \"{field}\". Model binding matches an input's name to "
                + $"a property name — no input called {field}, no {field} on the other end. "
                + $"asp-for=\"{field}\" writes that name for you.");
        }

        Assert.True(html.Contains("First sighted"),
            "the form doesn't show the label \"First sighted\" anywhere. That text comes from "
            + "[Display(Name = \"First sighted\")] on the model, via <label asp-for=\"FirstSighting\">.");

        var index = await Html(Index);
        Assert.True(Regex.IsMatch(index, @"/Cryptids/Create", RegexOptions.IgnoreCase),
            "nothing on /Cryptids links to the form. Add a link or button to the top of "
            + "Views/Cryptids/Index.cshtml — a page nobody can reach is a page nobody uses.");
    }

    [Fact] // Task 4: a good report gets filed, and the browser is sent somewhere sensible
    public async Task Check4_AGoodReportGetsFiled()
    {
        var before = DetailsIds(await Html(Index));

        var response = await SubmitForm(new Dictionary<string, string>
        {
            ["Name"] = "The Beast of Bray Road",
            ["Region"] = "Elkhorn, Wisconsin",
            ["FirstSighting"] = "1936",
            ["Sightings"] = "42",
            ["IsDebunked"] = "false",
        });

        var body = await response.Content.ReadAsStringAsync();

        // A 200 has two very different causes, and the giveaway is whether the name
        // I posted came back with the page. If it didn't, nothing ever bound it —
        // an action with NO verb attribute answers POST as happily as GET, so a lone
        // Create() quietly serves the blank form back and looks like a rejection.
        var sawMyData = body.Contains("The Beast of Bray Road");

        var hint = (int)response.StatusCode switch
        {
            405 => " A 405 means the page exists but nothing is listening for the POST: you need a "
                 + "SECOND Create action, marked [HttpPost], that takes a Cryptid parameter.",
            400 => " A 400 usually means the antiforgery token didn't check out — your action has "
                 + "[ValidateAntiForgeryToken] but the form isn't rendering the hidden token. "
                 + "Use <form asp-action=\"Create\" method=\"post\">, not a plain <form>.",
            200 when !sawMyData
                => " I got a blank form back, without the name I just typed in it — so nothing "
                 + "received the post. You almost certainly have only the GET Create(), and an "
                 + "action with no verb attribute answers EVERY verb, so it served the empty form "
                 + "again. Add the [HttpPost] Create(Cryptid cryptid) overload.",
            200 => " I got the form back with my data in it, which means ModelState.IsValid was "
                 + "false for a report that should be fine. Compare what I sent — first sighted "
                 + "1936, 42 reports — against the [Range] limits you set in task 2.",
            _ => "",
        };

        Assert.True((int)response.StatusCode is 302 or 303,
            $"posting a perfectly good report returned {(int)response.StatusCode}, and I expected a "
            + "redirect. After a successful POST, send the browser somewhere with "
            + "RedirectToAction(nameof(Index)) — returning View() leaves them sitting on the form, "
            + "and a refresh files the report all over again." + hint);

        var location = response.Headers.Location?.ToString() ?? "";
        Assert.True(location.Contains("/Cryptids", StringComparison.OrdinalIgnoreCase),
            $"the redirect points at \"{location}\" — it should go back to the registry so they can "
            + "see what they just filed: return RedirectToAction(nameof(Index));");

        var indexHtml = await Html(Index);
        Assert.True(indexHtml.Contains("The Beast of Bray Road"),
            "the redirect happened, but the new creature isn't on /Cryptids. Your POST action has "
            + "to actually add it: CryptidData.All.Add(cryptid);");

        var added = DetailsIds(indexHtml).Except(before).ToList();
        Assert.True(added.Count == 1,
            $"I expected exactly one new Details link on the registry and found {added.Count}. "
            + "Every card links to /Cryptids/Details/{Id}, so this usually means the new creature "
            + "has no Id of its own.");

        // Id 0 "works" for exactly one creature and then collides with the next one.
        Assert.True(added[0] > 0,
            "the new creature went into the list with Id 0, because nothing assigned it one. It "
            + "looks fine right now — but file a second report and both will answer to "
            + "/Cryptids/Details/0, and only the first will ever be found. Give it an id before "
            + "you add it: cryptid.Id = CryptidData.All.Max(c => c.Id) + 1;");

        var detailsPage = await _client.GetAsync($"/Cryptids/Details/{added[0]}");
        Assert.True(detailsPage.IsSuccessStatusCode,
            $"/Cryptids/Details/{added[0]} returned {(int)detailsPage.StatusCode}. The new creature "
            + "needs an Id before it goes in the list, or its own details page 404s: "
            + "cryptid.Id = CryptidData.All.Max(c => c.Id) + 1;");

        Assert.Contains("The Beast of Bray Road", await detailsPage.Content.ReadAsStringAsync());
    }

    [Fact] // Task 5: a bad report is refused, out loud, without losing what they typed
    public async Task Check5_ABadReportIsRefused()
    {
        var before = DetailsIds(await Html(Index));

        var response = await SubmitForm(new Dictionary<string, string>
        {
            ["Name"] = "",                          // fails [Required]
            ["Region"] = "Loch Ness, Scotland",     // perfectly fine — and must survive
            ["FirstSighting"] = "99999",            // fails [Range]
            ["Sightings"] = "3",
            ["IsDebunked"] = "false",
        });

        Assert.True((int)response.StatusCode == 200,
            $"posting a report with no name and a first sighting in the year 99999 returned "
            + $"{(int)response.StatusCode}. A redirect here means it was accepted. Guard the POST "
            + "action with: if (!ModelState.IsValid) { return View(cryptid); }");

        var html = await response.Content.ReadAsStringAsync();

        // Ask this one FIRST: a blank form means nothing bound the post at all, which
        // would otherwise look identical to "you forgot the error messages."
        Assert.True(html.Contains("Loch Ness, Scotland"),
            "the form came back empty — the region I typed wasn't in it. Either nothing received "
            + "the post (an action with no verb attribute answers every verb, so a lone GET "
            + "Create() serves the blank form straight back — add the [HttpPost] overload), or "
            + "you're returning View() with no argument. One bad field shouldn't cost them "
            + "everything they typed: return View(cryptid).");

        Assert.True(html.Contains("field-validation-error") || html.Contains("validation-summary-errors"),
            "the form came back with their input intact, but no error messages on it — so the "
            + "person filling it in has no idea what went wrong. Add "
            + "<span asp-validation-for=\"Name\"></span> next to each field, and a "
            + "<div asp-validation-summary=\"ModelOnly\"></div> at the top.");


        var after = DetailsIds(await Html(Index));
        Assert.True(after.Count == before.Count,
            $"the registry grew from {before.Count} creatures to {after.Count} — a report that "
            + "failed validation still got added. The ModelState.IsValid guard has to come "
            + "BEFORE CryptidData.All.Add(...), and it has to return.");
    }

    [Fact] // Task 6: client-side validation, delivered through week 5's Scripts slot
    public async Task Check6_ValidationRunsInTheBrowserToo()
    {
        var page = await _client.GetAsync(Create);
        Assert.True(page.IsSuccessStatusCode,
            $"GET {Create} returned {(int)page.StatusCode} — there's no form to validate yet. "
            + "Task 3 builds the page; this check is the last one.");

        var html = await page.Content.ReadAsStringAsync();

        Assert.True(html.Contains("jquery.validate", StringComparison.OrdinalIgnoreCase),
            "the Create page isn't loading the validation scripts, so the form only complains "
            + "after a round trip to the server. Add this at the bottom of Create.cshtml:\n"
            + "    @section Scripts { <partial name=\"_ValidationScriptsPartial\" /> }");

        // Same proof as week 5's section check: a section lands where the LAYOUT
        // puts it — below the footer — not where you typed it.
        var script = html.IndexOf("jquery.validate", StringComparison.OrdinalIgnoreCase);
        var footer = html.IndexOf("<footer", StringComparison.OrdinalIgnoreCase);
        Assert.True(footer >= 0 && script > footer,
            "the validation scripts are rendering in the middle of the page instead of down with "
            + "the other scripts. That means the <partial> is sitting in the view's markup rather "
            + "than inside a @section Scripts block — and loaded there it runs before jQuery "
            + "exists. Wrap it in the section.");

        Assert.True(Regex.IsMatch(html, @"data-val-required", RegexOptions.IgnoreCase),
            "your inputs have no data-val-required attributes, which is what the browser-side "
            + "validator reads. They appear automatically when asp-for renders a property that "
            + "has a [Required] attribute — so this usually means task 2 or task 3 isn't done.");
    }
}
