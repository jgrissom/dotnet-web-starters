// ═══════════════════════════════════════════════════════════════════
//  READ-ONLY — these checks are how you know the lab is done. They are not
//  your grade: the points come from your DEPLOYED app (see homework.md).
//  Run them with:  dotnet test Cryptids.Checks   (from the parent folder)
//  Your job is turning ❌ into ✅ by editing Cryptids.Web — never this file.
//
//  This week the Registry's pages call its own API with JavaScript. These
//  checks can't click a button: nothing here runs your JavaScript. What they
//  CAN check is that your page and your script agree — the script is served,
//  everything it looks for is on the page, and the form's box names are the
//  names the API reads. Your browser is where you watch it work. Do both.
//
//  Same rules as weeks 7–13: everything runs against an in-memory database,
//  so no wifi and no SQL Server, and 5/5 still doesn't prove your connection
//  string works.
// ═══════════════════════════════════════════════════════════════════
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Cryptids.Web.Models;

namespace Cryptids.Checks;

public class JsChecks : IClassFixture<RegistryApp>
{
    private readonly RegistryApp _app;

    // A password Identity's default rules accept: upper, lower, a digit, a
    // symbol, and more than six characters.
    private const string GoodPassword = "Hodag-1893!";

    // What every form that asks for anything else is given.
    private const string MemberName = "Agent Kestrel";

    private const string Role = "Curator";

    // What check 3 types into every text box: long enough for an account,
    // short enough for a witness's name.
    private const string Typed = "Seen by the checks, low over the marsh";

    public JsChecks(RegistryApp app)
    {
        _app = app;
        _app.EnsureSeeded();
    }

    // ── 1 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Check1_TheRegistryYouWereHandedStillWorks()
    {
        var client = _app.NewClient();
        foreach (var url in new[] { "/", "/Cryptids", "/Cryptids/Details/1", "/Sightings/Create", "/Account/Register", "/Account/Login", "/api-tester.html" })
        {
            var response = await client.GetAsync(url);
            Assert.True(response.IsSuccessStatusCode,
                $"GET {url} should return 200 — this one passes before you touch anything. It's "
                + "week 13's finished Registry: the pages, the Join and Sign in pages, and the API "
                + "tester. If it's red, something in Cryptids.Web got broken; undo it.");
        }

        var list = await client.GetAsync("/api/cryptids");
        Assert.True(list.StatusCode == HttpStatusCode.OK && IsJson(list),
            $"GET /api/cryptids came back {(int)list.StatusCode}. Tonight's JavaScript calls week 13's "
            + "API, so it has to keep answering. If it's red, undo what changed in "
            + "Controllers/CryptidsApiController.cs or Program.cs.");

        var one = await Parse(await client.GetAsync("/api/cryptids/1"));
        Assert.True(one.ValueKind == JsonValueKind.Object
                && one.TryGetProperty("sightings", out var sightings) && sightings.GetArrayLength() == 3,
            "GET /api/cryptids/1 should be The Hodag with its 3 reports, the way week 13 left it, and "
            + $"it's {Short(one)}.");

        var missing = await client.GetAsync("/Cryptids/Details/999999");
        Assert.True(missing.StatusCode == HttpStatusCode.NotFound
                && (await missing.Content.ReadAsStringAsync()).Contains("<html", StringComparison.OrdinalIgnoreCase),
            $"a person who opens a creature that isn't on file should still get the Registry's Not on "
            + $"file page, and /Cryptids/Details/999999 came back {(int)missing.StatusCode}.");
    }

    // ── 2 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Check2_AQuickLookNeedsNoReload()
    {
        // As a curator, so a button only a curator gets (task 4) is on the page too.
        await using var curator = await Curator();
        var html = await Html(curator.Client, "/Cryptids");

        var scripts = await ScriptsOn(curator.Client, html, "/Cryptids", "Task 2's last step puts "
            + "<script src=\"~/js/archive.js\"></script> inside a @section Scripts { … } at the bottom of "
            + "Views/Cryptids/Index.cshtml.");

        var asks = scripts.FirstOrDefault(s => Regex.IsMatch(s.Source, @"fetch\s*\(\s*[`'""]/api/cryptids/", RegexOptions.IgnoreCase));
        Assert.True(asks != null,
            $"/Cryptids loads {string.Join(" and ", scripts.Select(s => s.Path))}, and none of it asks the API "
            + "for one creature. Task 2's quickLook does: fetch(`/api/cryptids/${card.dataset.id}`).");

        var ids = CryptidIds();
        var unmarked = ids.Where(id => !Regex.IsMatch(html, $@"\bdata-id\s*=\s*""{id}""")).ToList();
        Assert.True(unmarked.Count == 0,
            $"the script reads each card's id from data-id, and creature{(unmarked.Count == 1 ? "" : "s")} "
            + $"{string.Join(", ", unmarked)} on /Cryptids {(unmarked.Count == 1 ? "has" : "have")} none. "
            + "Task 2's grid puts data-id=\"@cryptid.Id\" on each card's <div class=\"col …\">.");

        var missing = MissingSelectors(asks!.Source, html);
        Assert.True(missing.Count == 0,
            $"{asks.Path} looks for {string.Join(", ", missing)}, and /Cryptids has nothing that matches. "
            + "querySelector finds nothing, and the script stops at the first thing it tries to use. "
            + "Check the spelling on both sides: an id is #name in the script and id=\"name\" on the page; "
            + "a class is .name and class=\"name\".");
    }

    // ── 3 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Check3_AReportFilesWithoutLeavingThePage()
    {
        var stranger = _app.NewClient();
        var html = await Html(stranger, "/Cryptids/Details/2");

        var scripts = await ScriptsOn(stranger, html, "/Cryptids/Details/2", "Task 3's last step puts "
            + "<script src=\"~/js/report.js\"></script> inside the @section Scripts { … } at the bottom of "
            + "Views/Cryptids/Details.cshtml, next to the console.log that's already there.");

        var files = scripts.FirstOrDefault(s => s.Source.Contains("/api/sightings", StringComparison.OrdinalIgnoreCase));
        Assert.True(files != null,
            $"/Cryptids/Details/2 loads {string.Join(" and ", scripts.Select(s => s.Path))}, and none of it "
            + "sends anything to /api/sightings. Task 3's report.js does.");

        var source = files!.Source;
        Assert.True(Regex.IsMatch(source, @"method\s*:\s*[`'""]POST[`'""]", RegexOptions.IgnoreCase),
            $"{files.Path} never says method: \"POST\". Without it, fetch sends a GET, and /api/sightings "
            + "has nothing to answer a GET with.");
        Assert.True(source.Contains("application/json", StringComparison.OrdinalIgnoreCase),
            $"{files.Path} sends to /api/sightings with no \"Content-Type\": \"application/json\". The API "
            + "answers that with a 415 and never reads the body.");
        Assert.True(source.Contains("JSON.stringify", StringComparison.Ordinal),
            $"{files.Path} never calls JSON.stringify, so what it sends isn't JSON.");
        Assert.True(source.Contains("preventDefault", StringComparison.Ordinal),
            $"{files.Path} never calls event.preventDefault(). Without it the browser sends the form "
            + "the old way and reloads the page, and your fetch never gets to finish.");

        var missing = MissingSelectors(source, html);
        Assert.True(missing.Count == 0,
            $"{files.Path} looks for {string.Join(", ", missing)}, and /Cryptids/Details/2 has nothing "
            + "that matches. Check the spelling on both sides: #name in the script is id=\"name\" on the "
            + "page.");

        // The form the script looks for, by its id.
        var formIds = Selectors(source).SelectMany(Tokens)
            .Select(t => Regex.Match(t, @"#([\w-]+)").Groups[1].Value).Where(i => i != "").ToHashSet();
        var form = Regex.Matches(html, @"<form\b[^>]*>[\s\S]*?</form>", RegexOptions.IgnoreCase)
            .Select(m => m.Value)
            .FirstOrDefault(f => formIds.Contains(Regex.Match(f, @"^<form\b[^>]*\bid\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase).Groups[1].Value));
        Assert.True(form != null,
            $"{files.Path} looks for a form by its id, and /Cryptids/Details/2 has no <form> with any id "
            + "the script names. Task 3's form is <form id=\"report-form\">.");

        // Fill it in the way a person would, and send it with the names it has.
        var body = new Dictionary<string, object?>();
        foreach (Match field in Regex.Matches(form!, @"<(input|textarea|select)\b([^>]*)>", RegexOptions.IgnoreCase))
        {
            var attrs = field.Groups[2].Value;
            var name = Regex.Match(attrs, @"\bname\s*=\s*""([^""]+)""").Groups[1].Value;
            if (name == "" || name.StartsWith("__") || body.ContainsKey(name))
            {
                continue;
            }
            var type = Regex.Match(attrs, @"\btype\s*=\s*""([^""]+)""").Groups[1].Value.ToLowerInvariant();
            var value = Regex.Match(attrs, @"\bvalue\s*=\s*""([^""]*)""").Groups[1].Value;
            body[name] = type switch
            {
                "hidden" => int.TryParse(value, out var n) ? n : value,
                "date" => "2026-11-17",
                "number" => 1,
                "checkbox" => false,
                _ => Typed,
            };
        }

        var before = CountSightings(2);
        var sent = await stranger.PostAsync("/api/sightings",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
        Assert.True(sent.StatusCode == HttpStatusCode.Created,
            $"I filled in the form on /Cryptids/Details/2 the way a person would — "
            + $"{(body.Count == 0 ? "it has no named boxes" : string.Join(", ", body.Keys))} — and sent "
            + $"those names to /api/sightings as JSON. It answered {(int)sent.StatusCode}: "
            + $"{Short(await Parse(sent))}. Each box's name has to be the API's name for it: witness, "
            + "location, seenOn and account, plus a hidden cryptidId holding @Model.Id.");

        var after = await Parse(await stranger.GetAsync("/api/cryptids/2"));
        var landed = after.TryGetProperty("sightings", out var reports)
            && reports.EnumerateArray().Any(r => r.TryGetProperty("witness", out var w) && w.GetString() == Typed);
        Assert.True(landed && CountSightings(2) == before + 1,
            "the API said 201, and reading creature 2 back at /api/cryptids/2 doesn't show the report. "
            + "Look at the hidden cryptidId: it has to be the creature this page is about.");
    }

    // ── 4 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Check4_OnlyACuratorCanCloseAFileFromTheList()
    {
        await using var curator = await Curator();
        Assert.True(curator.HoldsRole,
            "a member whose email is in Admin:Email should hold the Curator role after a restart, and "
            + "doesn't. That's week 12's seeder: Data/RoleSeeder.cs, called in Program.cs.");

        var curatorHtml = await Html(curator.Client, "/Cryptids");
        var strangerHtml = await Html(curator.After.NewClient(), "/Cryptids");

        var scripts = await ScriptsOn(curator.Client, curatorHtml, "/Cryptids", "Task 2 adds it.");
        var deletes = scripts.FirstOrDefault(s =>
            Regex.IsMatch(s.Source, @"method\s*:\s*[`'""]DELETE[`'""]", RegexOptions.IgnoreCase)
            && s.Source.Contains("/api/cryptids/", StringComparison.OrdinalIgnoreCase));
        Assert.True(deletes != null,
            "the script /Cryptids loads never sends a DELETE to /api/cryptids/…. Task 4's closeFile "
            + "does: fetch(`/api/cryptids/${card.dataset.id}`, { method: \"DELETE\" }).");

        // A button on every card that a curator gets and a visitor doesn't, with
        // a class the script looks for. Whatever it's called.
        var creatures = CryptidIds().Count;
        var wanted = Selectors(deletes!.Source).SelectMany(Tokens)
            .SelectMany(t => Regex.Matches(t, @"\.([\w-]+)").Select(m => m.Groups[1].Value)).ToHashSet();
        var forCurator = ButtonClasses(curatorHtml);
        var forStranger = ButtonClasses(strangerHtml);
        var everyCard = wanted.Where(c => forCurator.GetValueOrDefault(c) >= creatures).ToList();
        var curatorOnly = everyCard.Where(c => forStranger.GetValueOrDefault(c) == 0).ToList();

        // Two buttons on every card that a visitor sees too: the quick look,
        // and a close button that isn't hidden from them.
        Assert.True(curatorOnly.Count > 0,
            everyCard.Count > 1
                ? "a visitor who isn't signed in sees the close button too. Put it inside "
                  + "@if (User.IsInRole(\"Curator\")) { … }, the way the ＋ Open a new file button is."
                : $"as a curator, every card on /Cryptids should have one more button than a visitor "
                  + $"sees — the one closeFile answers to — and none of the {creatures} cards has one. "
                  + "Task 4 adds <button type=\"button\" class=\"btn btn-link text-danger close-file\"> "
                  + "inside @if (User.IsInRole(\"Curator\")), and the click listener checks for "
                  + "event.target.matches(\".close-file\").");

        var missing = MissingSelectors(deletes.Source, curatorHtml);
        Assert.True(missing.Count == 0,
            $"{deletes.Path} looks for {string.Join(", ", missing)}, and the curator's /Cryptids has "
            + "nothing that matches.");

        // The API the button calls: a 204, and reading it back is a 404.
        int doomed;
        var (scope, context) = curator.After.NewContext();
        using (scope)
        {
            var test = new Cryptid { Name = "Check Four's Test File", Region = "Nowhere", FirstSighting = 2026 };
            context.Add(test);
            await context.SaveChangesAsync();
            doomed = test.Id;
        }
        var gone = await curator.Client.DeleteAsync($"/api/cryptids/{doomed}");
        var after = await curator.Client.GetAsync($"/api/cryptids/{doomed}");
        Assert.True(gone.StatusCode == HttpStatusCode.NoContent && after.StatusCode == HttpStatusCode.NotFound,
            $"as a curator, DELETE /api/cryptids/{doomed} came back {(int)gone.StatusCode} and reading it "
            + $"back came back {(int)after.StatusCode}. Week 13's API should answer 204, then 404.");
    }

    // ── 5 ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Check5_TheChecksRunOnEveryPush()
    {
        var root = LabRoot();
        Assert.True(root != null, "I couldn't find the folder holding both Cryptids.Web and Cryptids.Checks.");

        var folder = Path.Combine(root!, ".github", "workflows");
        var files = Directory.Exists(folder)
            ? Directory.GetFiles(folder).Where(f => f.EndsWith(".yml") || f.EndsWith(".yaml")).ToList()
            : new List<string>();
        Assert.True(files.Count > 0,
            $"there's no workflow in {Path.GetFileName(root)}/.github/workflows/. Task 5 creates "
            + ".github/workflows/checks.yml in the folder that holds both projects — the same folder "
            + "you run dotnet test from. Watch the dot at the start of .github.");

        var problems = new List<string>();
        foreach (var file in files)
        {
            var yaml = File.ReadAllText(file);
            var name = Path.GetFileName(file);
            var inline = Regex.IsMatch(yaml, @"^\s*on\s*:\s*\[[^\]]*\bpush\b[^\]]*\bpull_request\b|^\s*on\s*:\s*\[[^\]]*\bpull_request\b[^\]]*\bpush\b", RegexOptions.Multiline);
            if (!inline && (!Regex.IsMatch(yaml, @"^\s*push\s*:", RegexOptions.Multiline)
                || !Regex.IsMatch(yaml, @"^\s*pull_request\s*:", RegexOptions.Multiline)))
            {
                problems.Add($"{name} doesn't run on both push and pull_request");
                continue;
            }
            if (!Regex.IsMatch(yaml, @"actions/setup-dotnet@") || !Regex.IsMatch(yaml, @"dotnet-version\s*:\s*['""]?10\."))
            {
                problems.Add($"{name} doesn't install .NET 10 with actions/setup-dotnet");
                continue;
            }
            if (!Regex.IsMatch(yaml, @"dotnet\s+test\s+Cryptids\.Checks"))
            {
                problems.Add($"{name} never runs dotnet test Cryptids.Checks");
                continue;
            }
            return;     // one good workflow is enough
        }

        Assert.Fail(string.Join("; and ", problems) + ". Task 5's checks.yml has all three. A tab "
            + "instead of spaces, or a step indented differently from the one above it, also stops "
            + "GitHub reading the file.");
    }

    // ── reading a page and its scripts ────────────────────────────────────

    private sealed record Script(string Path, string Source);

    // Every script the page loads from /js/, except the template's own site.js.
    private static async Task<List<Script>> ScriptsOn(HttpClient client, string html, string page, string howToAdd)
    {
        var paths = Regex.Matches(html, @"<script\b[^>]*\bsrc\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase)
            .Select(m => m.Groups[1].Value.Split('?')[0])
            .Where(p => p.StartsWith("/js/", StringComparison.OrdinalIgnoreCase)
                     && !p.Equals("/js/site.js", StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList();
        Assert.True(paths.Count > 0, $"{page} loads no script of yours yet. {howToAdd}");

        var scripts = new List<Script>();
        foreach (var path in paths)
        {
            var response = await client.GetAsync(path);
            Assert.True(response.StatusCode == HttpStatusCode.OK,
                $"{page} asks for {path}, and it came back {(int)response.StatusCode}. The file belongs in "
                + $"Cryptids.Web/wwwroot{path} — wwwroot is the folder the browser can see.");
            scripts.Add(new Script(path, await response.Content.ReadAsStringAsync()));
        }
        return scripts;
    }

    // Every selector the script hands to querySelector, querySelectorAll,
    // closest or matches — except one followed by ?. which the script itself
    // says might not be there.
    private static IEnumerable<string> Selectors(string js) =>
        Regex.Matches(js, @"\.(?:querySelector(?:All)?|closest|matches)\s*\(\s*([""'`])((?:(?!\1).)+)\1\s*\)(?!\s*\?\.)")
            .Select(m => m.Groups[2].Value)
            .Where(s => !s.Contains("${"));

    // "#archive .reports, li" → "#archive", ".reports", "li"
    private static IEnumerable<string> Tokens(string selector) =>
        selector.Split(',').SelectMany(part => Regex.Split(part.Trim(), @"\s*[\s>+~]\s*"))
            .Select(t => Regex.Replace(t, @"::?[\w-]+(\([^)]*\))?", ""))
            .Where(t => t != "");

    // A token is there if some element on the page, or in the script's own
    // markup, has its tag, its id, every class and every attribute.
    private static List<string> MissingSelectors(string js, string html)
    {
        var tags = Regex.Matches(html + js, @"<([a-zA-Z][\w-]*)\b([^>]*)>").Select(m => (tag: m.Groups[1].Value.ToLowerInvariant(), attrs: m.Groups[2].Value)).ToList();
        var missing = new List<string>();
        foreach (var token in Selectors(js).SelectMany(Tokens).Distinct())
        {
            var tag = Regex.Match(token, @"^[a-zA-Z][\w-]*").Value.ToLowerInvariant();
            var id = Regex.Match(token, @"#([\w-]+)").Groups[1].Value;
            var classes = Regex.Matches(token, @"\.([\w-]+)").Select(m => m.Groups[1].Value).ToList();
            var attrs = Regex.Matches(token, @"\[\s*([\w-]+)").Select(m => m.Groups[1].Value).ToList();
            var found = tags.Any(t =>
                (tag == "" || t.tag == tag)
                && (id == "" || Regex.IsMatch(t.attrs, $@"\bid\s*=\s*""{Regex.Escape(id)}"""))
                && classes.All(c => Regex.IsMatch(t.attrs, $@"\bclass\s*=\s*""[^""]*(?<![\w-]){Regex.Escape(c)}(?![\w-])"))
                && attrs.All(a => Regex.IsMatch(t.attrs, $@"(?<![\w-]){Regex.Escape(a)}\s*=")));
            if (!found)
            {
                missing.Add(token);
            }
        }
        return missing;
    }

    // How many buttons carry each class.
    private static Dictionary<string, int> ButtonClasses(string html)
    {
        var counts = new Dictionary<string, int>();
        foreach (Match button in Regex.Matches(html, @"<button\b[^>]*\bclass\s*=\s*""([^""]*)""", RegexOptions.IgnoreCase))
        {
            foreach (var c in button.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                counts[c] = counts.GetValueOrDefault(c) + 1;
            }
        }
        return counts;
    }

    // The folder holding both projects: where dotnet test runs, and where a
    // repository pushed to GitHub has its root.
    private static string? LabRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "Cryptids.Web"))
                && Directory.Exists(Path.Combine(dir.FullName, "Cryptids.Checks")))
            {
                return dir.FullName;
            }
        }
        return null;
    }

    // ── a curator, on an app restarted after they joined ──────────────────
    //
    // Join with an email, then start the app AGAIN with Admin:Email set to it —
    // the seeder runs once, at startup, so that's the only moment it can hand
    // the role out. Then sign in on the restarted app, so the new cookie
    // carries the role.

    private sealed class CuratorSession : IAsyncDisposable
    {
        public required RegistryApp Before { get; init; }
        public required RegistryApp After { get; init; }
        public required HttpClient Client { get; init; }
        public required bool HoldsRole { get; init; }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await After.DisposeAsync();
            await Before.DisposeAsync();
        }
    }

    private static async Task<CuratorSession> Curator()
    {
        var name = "registry-curator-" + Guid.NewGuid();
        var root = new InMemoryDatabaseRoot();
        var email = Email();

        var before = new RegistryApp(name, root, null);
        before.EnsureSeeded();
        await Register(before.NewClient(), email);

        var after = new RegistryApp(name, root, email);
        after.EnsureSeeded();
        var client = after.NewClient();
        await SignIn(client, email, GoodPassword);

        bool holds;
        using (var scope = after.Services.CreateScope())
        {
            var members = scope.ServiceProvider.GetRequiredService<UserManager<Member>>();
            var member = await members.FindByEmailAsync(email);
            holds = member != null && await members.IsInRoleAsync(member, Role);
        }

        return new CuratorSession { Before = before, After = after, Client = client, HoldsRole = holds };
    }

    // ── plumbing ──────────────────────────────────────────────────────────

    private static bool IsJson(HttpResponseMessage response) =>
        (response.Content.Headers.ContentType?.MediaType ?? "").Contains("json", StringComparison.OrdinalIgnoreCase);

    private static async Task<JsonElement> Parse(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        try
        {
            return JsonDocument.Parse(text).RootElement.Clone();
        }
        catch (JsonException)
        {
            return JsonDocument.Parse(JsonSerializer.Serialize(text)).RootElement.Clone();
        }
    }

    private static string Short(JsonElement element)
    {
        var text = element.ToString();
        return text.Length > 220 ? text[..220] + "…" : text;
    }

    private static string Email() => $"check-{Guid.NewGuid():N}@example.com";

    private static async Task<string> Html(HttpClient client, string url) =>
        await (await client.GetAsync(url)).Content.ReadAsStringAsync();

    // The page's antiforgery token, plus whatever fields the caller wants to send.
    private static FormUrlEncodedContent WithToken(string html, Dictionary<string, string> fields)
    {
        var token = Regex.Match(html, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
        if (token.Success)
        {
            fields["__RequestVerificationToken"] = token.Groups[1].Value;
        }
        return new FormUrlEncodedContent(fields);
    }

    // Fills in whatever the registration form asks for, the way a person
    // would: the email box, both password boxes, and MemberName in anything else.
    private static async Task<HttpResponseMessage> Register(HttpClient client, string email)
    {
        var html = await Html(client, "/Account/Register");
        var form = Regex.Match(html, @"<form[^>]*method=""post""[\s\S]*?</form>", RegexOptions.IgnoreCase);
        var fields = new Dictionary<string, string>();

        foreach (Match input in Regex.Matches(form.Success ? form.Value : html, @"<input\b[^>]*>", RegexOptions.IgnoreCase))
        {
            var name = Regex.Match(input.Value, @"\bname=""([^""]+)""").Groups[1].Value;
            var type = Regex.Match(input.Value, @"\btype=""([^""]+)""").Groups[1].Value.ToLowerInvariant();
            if (name == "" || name.StartsWith("__") || fields.ContainsKey(name))
            {
                continue;
            }

            fields[name] = type == "password" ? GoodPassword
                : type == "email" || name.Contains("Email", StringComparison.OrdinalIgnoreCase) ? email
                : type == "checkbox" ? "false"
                : MemberName;
        }

        return await client.PostAsync("/Account/Register", WithToken(html, fields));
    }

    private static async Task<HttpResponseMessage> SignIn(HttpClient client, string email, string password)
    {
        var html = await Html(client, "/Account/Login");
        return await client.PostAsync("/Account/Login", WithToken(html, new()
        {
            ["Email"] = email,
            ["Password"] = password,
            ["RememberMe"] = "false",
        }));
    }

    private List<int> CryptidIds()
    {
        var (scope, context) = _app.NewContext();
        using (scope)
        {
            return context.Set<Cryptid>().Select(c => c.Id).ToList();
        }
    }

    private int CountSightings(int cryptidId)
    {
        var (scope, context) = _app.NewContext();
        using (scope)
        {
            return context.Set<Sighting>().Count(s => s.CryptidId == cryptidId);
        }
    }
}
