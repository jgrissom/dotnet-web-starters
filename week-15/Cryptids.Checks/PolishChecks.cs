// ═══════════════════════════════════════════════════════════════════
//  READ-ONLY — these checks are how you know the lab is done. Nothing this
//  week is graded.
//  Run them with:  dotnet test Cryptids.Checks   (from the parent folder)
//  Your job is turning ❌ into ✅ by editing Cryptids.Web — never this file.
//
//  This week the Registry gets easier to use: a message after every change,
//  a search box and a filter, a list that comes in pages, and a service that
//  holds the queries two controllers need.
//  Same rules as weeks 7–12: everything runs against an in-memory database,
//  so no wifi and no SQL Server, and 5/5 still doesn't prove your connection
//  string works.
//
//  ⚠️ One difference from your browser: the in-memory database matches
//  case exactly, and SQL Server doesn't. These checks search with the right
//  capitals; `hodag` finds The Hodag in your browser, not here.
// ═══════════════════════════════════════════════════════════════════
using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Cryptids.Web.Models;

namespace Cryptids.Checks;

public class PolishChecks
{
    // A password Identity's default rules accept: upper, lower, a digit, a
    // symbol, and more than six characters.
    private const string GoodPassword = "Hodag-1893!";

    // What every form that asks for anything else is given.
    private const string MemberName = "Agent Kestrel";

    private const string Role = "Curator";

    // ── 1 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Check1_TheRegistryYouWereHandedStillWorks()
    {
        await using var app = Fresh();
        var client = app.NewClient();
        foreach (var url in new[] { "/", "/Cryptids", "/Cryptids/Details/1", "/Sightings/Create", "/Account/Register", "/Account/Login" })
        {
            var response = await client.GetAsync(url);
            Assert.True(response.IsSuccessStatusCode,
                $"GET {url} should return 200 — this one passes before you touch anything. It's "
                + "week 12's finished Registry: the list, a creature's page, the report form, and the "
                + "Join and Sign in pages. If it's red, something in Cryptids.Web got broken; undo it. "
                + "On task 5 and every check went red at once? The app can't build ICryptidService: "
                + "read check 5's message.");
        }

        await using var curator = await Curator();
        Assert.True(curator.HoldsRole,
            "the seeder didn't make a curator. That's week 12's Data/RoleSeeder.cs, and it came with "
            + "the starter. Undo whatever changed it.");
        var form = await curator.Client.GetAsync("/Cryptids/Create");
        Assert.True(form.StatusCode == HttpStatusCode.OK,
            $"the curator opened /Cryptids/Create and got {(int)form.StatusCode}. That's week 12's "
            + "[Authorize(Roles = \"Curator\")], and it came with the starter. Undo whatever changed it.");
    }

    // ── 2 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Check2_AConfirmationSurvivesTheRedirect()
    {
        await using var curator = await Curator();
        Assert.True(curator.HoldsRole, "there's no curator to test with — check 1 comes first.");
        var client = curator.Client;

        // Create: the message has to name the creature, on the page after the redirect.
        var form = await Html(client, "/Cryptids/Create");
        var created = await client.PostAsync("/Cryptids/Create", WithToken(form, new()
        {
            ["Name"] = "Beast of Bray Road",
            ["Region"] = "Elkhorn, Wisconsin",
            ["FirstSighting"] = "1936",
            ["IsDebunked"] = "false",
        }));
        Assert.True(IsRedirect(created),
            $"filing a new creature came back {(int)created.StatusCode}, not a redirect. Week 6's "
            + "RedirectToAction stays; the message has to survive it.");
        var landed = await Follow(client, created);
        Assert.True(Alert(landed)?.Contains("Beast of Bray Road") == true,
            "a curator filed Beast of Bray Road and the page after the redirect shows no message "
            + "naming it. Task 2: TempData[\"Message\"] = … in the POST, and the alert block in "
            + "_Layout.cshtml. ViewData can't do this one: it lives for one request, and a "
            + "redirect is a second request.");

        // Read once, then gone.
        var reloaded = await Html(client, "/Cryptids");
        Assert.True(Alert(reloaded) == null,
            "the message is still there on the next page load. TempData is read once and "
            + "removed; TempData.Keep() or a Peek would keep it around.");

        // Edit.
        form = await Html(client, "/Cryptids/Edit/2");
        var edited = await client.PostAsync("/Cryptids/Edit/2", WithToken(form, new()
        {
            ["Id"] = "2",
            ["Name"] = "Bigfoot",
            ["Region"] = "Pacific Northwest",
            ["FirstSighting"] = "1958",
            ["IsDebunked"] = "false",
        }));
        Assert.True(Alert(await Follow(client, edited))?.Contains("Bigfoot") == true,
            "a curator corrected Bigfoot's record and the next page shows no message naming it. "
            + "Task 2 puts TempData on all three writes: Create, Edit and Delete.");

        // Delete.
        form = await Html(client, "/Cryptids/Delete/6");
        var deleted = await client.PostAsync("/Cryptids/Delete/6", WithToken(form, new() { ["Id"] = "6" }));
        Assert.True(Alert(await Follow(client, deleted))?.Contains("Chupacabra") == true,
            "a curator closed the Chupacabra's file and the next page shows no message naming it. "
            + "The creature has to be found before it's removed, so the name is there to use.");

        // The public half: a visitor, nobody signed in, and a form that only posts an id.
        var visitor = curator.After.NewClient();
        form = await Html(visitor, "/Sightings/Create?cryptidId=3");
        var filed = await visitor.PostAsync("/Sightings/Create", WithToken(form, new()
        {
            ["Sighting.CryptidId"] = "3",
            ["Sighting.Witness"] = MemberName,
            ["Sighting.Location"] = "Point Pleasant, WV",
            ["Sighting.SeenOn"] = "2024-05-01",
            ["Sighting.Account"] = "Red eyes over the river, well after dark.",
        }));
        Assert.True(IsRedirect(filed),
            $"a visitor's report came back {(int)filed.StatusCode}, not a redirect. Reports are "
            + "still public: leave SightingsController open.");
        Assert.True(Alert(await Follow(visitor, filed))?.Contains("Mothman") == true,
            "a visitor filed a report on Mothman and the creature's page shows no message naming "
            + "it. The form only posts CryptidId, so the POST in SightingsController has to "
            + "look the name up. The demo never did this one.");
    }

    // ── 3 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Check3_TheArchiveCanBeSearched()
    {
        await using var app = Fresh();
        var client = app.NewClient();

        var list = await Html(client, "/Cryptids");
        var form = Regex.Matches(list, @"<form\b[^>]*>[\s\S]*?</form>", RegexOptions.IgnoreCase)
            .Select(m => m.Value)
            .FirstOrDefault(f => Regex.IsMatch(f, @"name=""search""", RegexOptions.IgnoreCase));
        Assert.True(form != null,
            "/Cryptids has no form with a box named search. Task 3 adds it, above the cards.");
        Assert.False(Regex.IsMatch(form!.Substring(0, form.IndexOf('>')), @"method=""post""", RegexOptions.IgnoreCase),
            "the search form is a POST. A search changes nothing and should be bookmarkable, so "
            + "it's method=\"get\".");

        foreach (var (word, id, field) in new[] { ("Hodag", 1, "the name"), ("Scotland", 4, "the region"), ("Noctua", 3, "the Latin name") })
        {
            var ids = Ids(await Html(client, $"/Cryptids?search={word}"));
            Assert.True(ids.SetEquals(new[] { id }),
                $"/Cryptids?search={word} should find creature {id} alone — that word is in {field} — "
                + $"and it found [{string.Join(", ", ids)}]. Task 3's Where looks at the name, the "
                + "region and the Latin name.");
        }

        var none = await Html(client, "/Cryptids?search=zzzzzz");
        Assert.True(Ids(none).Count == 0,
            "a search for zzzzzz still lists creatures. The Where isn't being applied.");

        Assert.Contains("value=\"Hodag\"", await Html(client, "/Cryptids?search=Hodag"));

        var debunked = Ids(await Html(client, "/Cryptids?status=debunked"));
        Assert.True(debunked.SetEquals(new[] { 1, 6 }),
            $"/Cryptids?status=debunked should list the two debunked creatures, 1 and 6, and it "
            + $"listed [{string.Join(", ", debunked)}]. Task 3 adds a dropdown named status.");

        var unconfirmed = Ids(await Html(client, "/Cryptids?status=unconfirmed"));
        Assert.True(unconfirmed.Count > 0 && !unconfirmed.Overlaps(new[] { 1, 6 }),
            $"/Cryptids?status=unconfirmed listed [{string.Join(", ", unconfirmed)}]. 1 and 6 are "
            + "debunked, and unconfirmed should leave them out.");

        var both = Ids(await Html(client, "/Cryptids?search=Hodag&status=unconfirmed"));
        Assert.True(both.Count == 0,
            "The Hodag is debunked, so searching for it among the unconfirmed should find nothing, "
            + $"and it found [{string.Join(", ", both)}]. The two Wheres have to add up.");
    }

    // ── 4 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Check4_TheListComesInPages()
    {
        await using var app = Fresh();
        var client = app.NewClient();

        var first = await Html(client, "/Cryptids");
        var one = Ids(first);
        Assert.True(one.Count == 3,
            $"/Cryptids lists {one.Count} creatures. Task 4 shows three to a page.");
        Assert.True(one.SetEquals(new[] { 2, 6, 3 }),
            $"page 1 lists [{string.Join(", ", one)}], and in Name order it should be Bigfoot, "
            + "Chupacabra and Mothman (2, 6 and 3). Sort with OrderBy before Skip and Take: "
            + "a page is only a page if the order holds still.");

        var two = Ids(await Html(client, "/Cryptids?page=2"));
        Assert.True(two.SetEquals(new[] { 1, 5, 4 }),
            $"/Cryptids?page=2 lists [{string.Join(", ", two)}], and should list the other three: "
            + "1, 5 and 4. Skip((page - 1) * PageSize), then Take(PageSize).");

        Assert.True(first.Contains("6 creatures on file"),
            "page 1 of 2 should still say 6 creatures on file. Count what matched before Skip and "
            + "Take cut it down, and show that number.");

        foreach (var (url, want) in new[] { ("/Cryptids?page=0", one), ("/Cryptids?page=-3", one), ("/Cryptids?page=99", two) })
        {
            var response = await client.GetAsync(url);
            Assert.True(response.IsSuccessStatusCode,
                $"{url} came back {(int)response.StatusCode}. A page number is typed by a visitor; "
                + "Math.Clamp keeps it between 1 and the last page.");
            var got = Ids(await response.Content.ReadAsStringAsync());
            Assert.True(got.SetEquals(want),
                $"{url} lists [{string.Join(", ", got)}]. Clamp the page number, so it lands on "
                + "the nearest real page rather than an empty one.");
        }

        var links = PageLinks(first);
        Assert.True(links.Contains("1") && links.Contains("2"),
            $"page 1 needs a link to every page, 1 and 2, and it links to [{string.Join(", ", links)}]. "
            + "Task 4's pager is a numbered list, built with a for loop. The demo only had Previous and Next.");

        var filtered = await Html(client, "/Cryptids?search=e&status=unconfirmed");
        var hrefs = Regex.Matches(filtered, @"href=""([^""]*page=2[^""]*)""", RegexOptions.IgnoreCase)
            .Select(m => WebUtility.HtmlDecode(m.Groups[1].Value)).ToList();
        Assert.True(hrefs.Count > 0,
            "/Cryptids?search=e&status=unconfirmed matches four creatures, so it needs a link to page 2.");
        Assert.True(hrefs.All(h => h.Contains("search=e", StringComparison.OrdinalIgnoreCase)
                                && h.Contains("status=unconfirmed", StringComparison.OrdinalIgnoreCase)),
            $"the link to page 2 is {hrefs[0]}, which forgets what was searched for. Every page link "
            + "carries asp-route-search and asp-route-status.");
    }

    // ── 5 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Check5_TheQueriesLiveInAService()
    {
        var web = typeof(Program).Assembly;
        var service = web.GetType("Cryptids.Web.Services.ICryptidService");
        Assert.True(service is { IsInterface: true },
            "there's no interface called ICryptidService in Cryptids.Web.Services. Task 5 creates "
            + "it, in Services/ICryptidService.cs.");

        var implementation = web.GetTypes().FirstOrDefault(t => service!.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);
        Assert.True(implementation != null,
            "nothing implements ICryptidService yet. Task 5's CryptidService does.");

        foreach (var name in new[] { "CryptidsController", "HomeController" })
        {
            var parameters = web.GetType("Cryptids.Web.Controllers." + name)!
                .GetConstructors().SelectMany(c => c.GetParameters()).Select(p => p.ParameterType).ToList();
            Assert.True(parameters.Contains(service!),
                $"{name}'s constructor doesn't ask for ICryptidService. Ask for the interface, not "
                + "the class: that's what lets Program.cs decide which class it gets.");
        }

        var home = web.GetType("Cryptids.Web.Controllers.HomeController")!.GetConstructors()
            .SelectMany(c => c.GetParameters()).Select(p => p.ParameterType);
        Assert.False(home.Any(t => typeof(DbContext).IsAssignableFrom(t)),
            "HomeController still asks for the context. Its one query moves into the service, and "
            + "then the context has no business being there. The demo never did this one.");

        // Reading the code proves nothing, so watch it: wrap whatever is registered
        // in a recorder, load the two pages, and see whether the service was called.
        ServiceLifetime? lifetime = null;
        Recorder.Calls.Clear();
        await using var app = Fresh();
        await using var watched = app.WithWebHostBuilder(host => host.ConfigureTestServices(services =>
        {
            var registered = services.LastOrDefault(d => d.ServiceType == service);
            lifetime = registered?.Lifetime;
            if (registered?.ImplementationType == null)
            {
                return;
            }

            var real = registered.ImplementationType;
            services.Remove(registered);
            services.AddScoped(real);
            services.Add(new ServiceDescriptor(service!, provider => Recorder.Wrap(service!, provider.GetRequiredService(real)), registered.Lifetime));
        }));

        var client = watched.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var list = await client.GetAsync("/Cryptids");

        Assert.True(lifetime != null,
            "nothing is registered for ICryptidService. Program.cs needs "
            + "builder.Services.AddScoped<ICryptidService, CryptidService>(), above builder.Build().");
        Assert.True(lifetime == ServiceLifetime.Scoped,
            $"ICryptidService is registered as {lifetime}. It holds the context, and the context "
            + "lives for one request, so AddScoped is the one that fits.");
        Assert.True(list.IsSuccessStatusCode, $"/Cryptids came back {(int)list.StatusCode}.");
        Assert.True(Recorder.Calls.Count > 0,
            "/Cryptids loaded and never called ICryptidService. The action should ask the service "
            + "for its list, with the query gone from the controller.");

        var before = Recorder.Calls.Count;
        var page = await client.GetAsync("/");
        Assert.True(page.IsSuccessStatusCode, $"/ came back {(int)page.StatusCode}.");
        Assert.True(Recorder.Calls.Count > before,
            "the home page loaded and never called ICryptidService. The featured creature comes "
            + "from the service too.");
    }

    // Stands in for the student's service, calls the real one, and writes down
    // that it was called.
    public class Recorder : DispatchProxy
    {
        public static readonly ConcurrentBag<string> Calls = new();
        private object _target = null!;

        public static object Wrap(Type service, object target)
        {
            var proxy = typeof(DispatchProxy)
                .GetMethods().First(m => m.Name == nameof(DispatchProxy.Create) && m.IsGenericMethod)
                .MakeGenericMethod(service, typeof(Recorder))
                .Invoke(null, null)!;
            ((Recorder)proxy)._target = target;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            Calls.Add(method!.Name);
            try
            {
                return method.Invoke(_target, args);
            }
            catch (TargetInvocationException e) when (e.InnerException != null)
            {
                throw e.InnerException;
            }
        }
    }

    // ── a curator, the way the Registry gets one ──────────────────────────
    //
    // Join with an email, then start the app AGAIN with Admin:Email set to it,
    // then sign in on the restarted app — week 12's order.

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
        var email = $"check-{Guid.NewGuid():N}@example.com";

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

    private static RegistryApp Fresh()
    {
        var app = new RegistryApp();
        app.EnsureSeeded();
        return app;
    }

    private static async Task<string> Html(HttpClient client, string url) =>
        await (await client.GetAsync(url)).Content.ReadAsStringAsync();

    private static async Task<string> Follow(HttpClient client, HttpResponseMessage redirect) =>
        await Html(client, redirect.Headers.Location?.ToString() ?? "/");

    private static bool IsRedirect(HttpResponseMessage response) =>
        (int)response.StatusCode is 301 or 302 or 303;

    // The text inside a Bootstrap alert, or null if the page has none.
    private static string? Alert(string html)
    {
        var match = Regex.Match(html, @"<div[^>]*\bclass=""[^""]*\balert\b[^""]*""[^>]*>([\s\S]*?)</div>", RegexOptions.IgnoreCase);
        return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value) : null;
    }

    // Which creatures a page links to. Routing ignores case, so this does too.
    private static HashSet<int> Ids(string html) =>
        Regex.Matches(html, @"/Cryptids/Details/(\d+)", RegexOptions.IgnoreCase)
            .Select(m => int.Parse(m.Groups[1].Value)).ToHashSet();

    // The page numbers this page links to.
    private static HashSet<string> PageLinks(string html) =>
        Regex.Matches(html, @"href=""[^""]*[?&](?:amp;)?page=(\d+)", RegexOptions.IgnoreCase)
            .Select(m => m.Groups[1].Value).ToHashSet();

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
}
