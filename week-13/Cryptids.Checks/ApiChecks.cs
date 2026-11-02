// ═══════════════════════════════════════════════════════════════════
//  READ-ONLY — these checks are how you know the lab is done. They are not
//  your grade: the points come from your DEPLOYED app (see homework.md).
//  Run them with:  dotnet test Cryptids.Checks   (from the parent folder)
//  Your job is turning ❌ into ✅ by editing Cryptids.Web — never this file.
//
//  This week the Registry learns to answer programs as well as people. The
//  archive answers in JSON, a creature comes with its reports, anybody can
//  file a report with a POST, and only a curator can change the archive.
//  Same rules as weeks 7–12: everything runs against an in-memory database,
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

public class ApiChecks : IClassFixture<RegistryApp>
{
    private readonly RegistryApp _app;

    // A password Identity's default rules accept: upper, lower, a digit, a
    // symbol, and more than six characters.
    private const string GoodPassword = "Hodag-1893!";

    // What every form that asks for anything else is given.
    private const string MemberName = "Agent Kestrel";

    private const string Role = "Curator";

    private const string GoodCreature =
        """{ "name": "The Beast of Bray Road", "region": "Elkhorn, Wisconsin", "firstSighting": 1989, "isDebunked": false, "latinName": "Canis elkhornensis" }""";

    public ApiChecks(RegistryApp app)
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
                + "week 12's finished Registry: the list, a creature's page, the report form, the "
                + "Join and Sign in pages, and the API tester. If it's red, something in Cryptids.Web "
                + "got broken; undo it.");
        }

        var missing = await client.GetAsync("/Cryptids/Details/999999");
        Assert.True(missing.StatusCode == HttpStatusCode.NotFound
                && (await missing.Content.ReadAsStringAsync()).Contains("<html", StringComparison.OrdinalIgnoreCase),
            $"a person who opens a creature that isn't on file should still get the Registry's Not on "
            + $"file page, and /Cryptids/Details/999999 came back {(int)missing.StatusCode}. That page "
            + "is for people, so it stays when task 5 takes it away from /api.");
    }

    // ── 2 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Check2_TheArchiveAnswersInJson()
    {
        var stranger = _app.NewClient();

        var list = await stranger.GetAsync("/api/cryptids");
        Assert.True(list.StatusCode == HttpStatusCode.OK,
            $"GET /api/cryptids came back {(int)list.StatusCode}"
            + (IsRedirect(list) ? ", a redirect to " + list.Headers.Location : "")
            + (list.StatusCode == HttpStatusCode.InternalServerError
                ? ". A 500 with \"A possible object cycle was detected\" means the action is returning "
                  + "Cryptid objects with their Sightings loaded: each report points back at its creature. "
                  + "Task 2 returns a CryptidDto, built by hand."
                : ". Task 2 creates it: a CryptidsApiController with [ApiController] and "
                  + "[Route(\"api/cryptids\")] on the class, and an [HttpGet] action. Reading is public."));
        Assert.True(IsJson(list),
            $"GET /api/cryptids answered with {list.Content.Headers.ContentType?.MediaType}, not JSON. "
            + "Return the data from the action. Return View() is for pages.");

        var creatures = await Parse(list);
        Assert.True(creatures.ValueKind == JsonValueKind.Array && creatures.GetArrayLength() == CountCryptids(),
            $"GET /api/cryptids should be a JSON array of all {CountCryptids()} creatures, and it's "
            + $"{Short(creatures)}.");

        var hodag = creatures.EnumerateArray().First(c => c.GetProperty("id").GetInt32() == 1);
        Assert.True(hodag.TryGetProperty("name", out var name) && name.GetString() == "The Hodag",
            "creature 1 in /api/cryptids has no \"name\" of The Hodag. The JSON's names start with a "
            + "lowercase letter: that's the serializer's default, not something you type.");
        Assert.False(hodag.TryGetProperty("sightings", out _),
            "/api/cryptids still has a \"sightings\" property, which is what the Cryptid class looks "
            + "like, not what the list should say. Task 2 returns a CryptidDto, with the number of "
            + "reports instead of the reports.");
        Assert.True(hodag.TryGetProperty("reportCount", out var count)
                && count.ValueKind == JsonValueKind.Number && count.GetInt32() == 3,
            "The Hodag has 3 reports on file, and its \"reportCount\" in /api/cryptids is "
            + (hodag.TryGetProperty("reportCount", out var seen) ? seen.ToString() : "missing")
            + ". A count of 0 means the query never loaded them: Include(c => c.Sightings).");

        var total = await stranger.GetAsync("/api/cryptids/count");
        Assert.True(total.StatusCode == HttpStatusCode.OK && IsJson(total),
            $"GET /api/cryptids/count came back {(int)total.StatusCode}. Task 2's other half: a minimal "
            + "API endpoint, app.MapGet(\"/api/cryptids/count\", …) in Program.cs, above app.Run().");
        var counted = await Parse(total);
        Assert.True(counted.ValueKind == JsonValueKind.Object
                && counted.TryGetProperty("count", out var c) && c.ValueKind == JsonValueKind.Number
                && c.GetInt32() == CountCryptids(),
            $"GET /api/cryptids/count should be {{\"count\":{CountCryptids()}}}, and it's {Short(counted)}.");
    }

    // ── 3 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Check3_ACreatureComesWithItsReports()
    {
        var stranger = _app.NewClient();

        var one = await stranger.GetAsync("/api/cryptids/1");
        Assert.True(one.StatusCode == HttpStatusCode.OK && IsJson(one),
            $"GET /api/cryptids/1 came back {(int)one.StatusCode}"
            + (one.StatusCode == HttpStatusCode.InternalServerError
                ? ". A 500 with \"A possible object cycle was detected\" is the Cryptid class: its "
                  + "reports point at the creature, and the creature points at its reports. Task 3 "
                  + "returns a CryptidDetailsDto, built by hand, which can't loop."
                : ". Task 3 creates it: an [HttpGet(\"{id:int}\")] action on CryptidsApiController."));

        var hodag = await Parse(one);
        Assert.True(hodag.TryGetProperty("name", out var name) && name.GetString() == "The Hodag",
            $"GET /api/cryptids/1 should be The Hodag, and it's {Short(hodag)}.");
        Assert.True(hodag.TryGetProperty("sightings", out var sightings)
                && sightings.ValueKind == JsonValueKind.Array && sightings.GetArrayLength() == 3,
            "The Hodag has 3 reports on file, and /api/cryptids/1 has "
            + (hodag.TryGetProperty("sightings", out var seen) ? Short(seen) : "no \"sightings\"")
            + " in \"sightings\". An empty list means the query never loaded them: "
            + "Include(c => c.Sightings).");

        var report = sightings[0];
        Assert.True(report.TryGetProperty("witness", out _) && report.TryGetProperty("account", out _),
            "each report in \"sightings\" needs its \"witness\" and its \"account\" — that's a "
            + $"SightingDto. The first one is {Short(report)}.");
        Assert.False(report.TryGetProperty("cryptid", out _),
            "a report in \"sightings\" still has a \"cryptid\" in it, which leads straight back to the "
            + "creature it's inside. Build SightingDto by hand, with a CryptidId and no Cryptid.");

        var missing = await stranger.GetAsync("/api/cryptids/999999");
        Assert.True(missing.StatusCode == HttpStatusCode.NotFound,
            $"GET /api/cryptids/999999 came back {(int)missing.StatusCode}. A creature that isn't on "
            + "file is a 404: return NotFound() when the query finds nothing.");
        Assert.True(IsJson(missing),
            $"the 404 from /api/cryptids/999999 came back as {missing.Content.Headers.ContentType?.MediaType ?? "an empty body"}. "
            + "With [ApiController] on the class, NotFound() answers in JSON by itself.");
    }

    // ── 4 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Check4_AnyoneCanFileAReportWithJson()
    {
        var stranger = _app.NewClient();
        var before = CountSightings();

        var filed = await stranger.PostAsync("/api/sightings", Json(Report(cryptidId: 2)));
        Assert.True(filed.StatusCode == HttpStatusCode.Created,
            $"a visitor who isn't signed in POSTed a good report to /api/sightings and got "
            + $"{(int)filed.StatusCode}"
            + (IsRedirect(filed) ? ", a redirect to " + filed.Headers.Location : "")
            + ". Task 4 creates it: a SightingsApiController, with no [Authorize], whose [HttpPost] "
            + "saves the report and returns Created(…). Reports are the public half.");
        Assert.True(CountSightings() == before + 1,
            "POST /api/sightings said 201 and no row landed in Sightings. Add it and SaveChangesAsync.");
        Assert.True((filed.Headers.Location?.ToString() ?? "").Contains("/api/cryptids/2", StringComparison.OrdinalIgnoreCase),
            $"a 201 says where the new thing can be read. The Location header was "
            + $"\"{filed.Headers.Location}\", and it should be /api/cryptids/2, the creature it was filed under.");

        var body = await Parse(filed);
        Assert.True(body.ValueKind == JsonValueKind.Object && body.TryGetProperty("id", out var id) && id.GetInt32() > 0,
            $"POST /api/sightings should answer with the new report, its id included, and it said {Short(body)}.");

        var count = CountSightings();
        var empty = await stranger.PostAsync("/api/sightings", Json("{ \"cryptidId\": 2 }"));
        Assert.True(empty.StatusCode == HttpStatusCode.BadRequest && IsJson(empty) && CountSightings() == count,
            $"a report with nothing in it came back {(int)empty.StatusCode}"
            + (CountSightings() > count ? " and a row LANDED" : "")
            + ". [ApiController] refuses it with a 400 by itself, as long as the action takes a "
            + "SightingInput that carries the rules: [Required] on Witness, Location, SeenOn and Account.");

        var noDate = await stranger.PostAsync("/api/sightings", Json(Report(cryptidId: 2, seenOn: null)));
        Assert.True(noDate.StatusCode == HttpStatusCode.BadRequest && CountSightings() == count,
            $"a report with no date came back {(int)noDate.StatusCode}. SeenOn on SightingInput is "
            + "DateTime? with [Required]: a plain DateTime quietly becomes the year 1.");

        var nowhere = await stranger.PostAsync("/api/sightings", Json(Report(cryptidId: 999999)));
        Assert.True(nowhere.StatusCode == HttpStatusCode.BadRequest && CountSightings() == count,
            $"a report on creature 999999, which doesn't exist, came back {(int)nowhere.StatusCode}. "
            + "On SQL Server that's a 500 from the foreign key. Look the creature up first, and "
            + "answer with ModelState.AddModelError and ValidationProblem(ModelState).");
        var problem = await Parse(nowhere);
        Assert.True(problem.TryGetProperty("errors", out var errors) && errors.EnumerateObject().Any(),
            "that 400 has no \"errors\" in it, so a program can't tell what was wrong. Task 4 says "
            + "ValidationProblem(ModelState), not a bare BadRequest().");
    }

    // ── 5 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Check5_OnlyCuratorsCanChangeTheArchiveByApi()
    {
        var stranger = _app.NewClient();
        var member = _app.NewClient();
        await Register(member, Email());
        var creatures = CountCryptids();

        foreach (var (who, client, status) in new[]
        {
            ("a visitor who isn't signed in", stranger, HttpStatusCode.Unauthorized),
            ("a member who isn't a curator", member, HttpStatusCode.Forbidden),
        })
        {
            var put = await client.PutAsync("/api/cryptids/6", Json(GoodCreature));
            var delete = await client.DeleteAsync("/api/cryptids/6");
            foreach (var (verb, response) in new[] { ("PUT", put), ("DELETE", delete) })
            {
                Assert.True(response.StatusCode == status,
                    $"{who} sent a {verb} to /api/cryptids/6 and got {(int)response.StatusCode}, not "
                    + $"{(int)status}"
                    + (response.StatusCode == HttpStatusCode.MethodNotAllowed
                        ? ": there's no such action yet. Task 5 adds [HttpPut(\"{id:int}\")] and [HttpDelete(\"{id:int}\")]."
                        : response.IsSuccessStatusCode
                            ? ". It got through. Task 5 puts [Authorize(Roles = \"Curator\")] on the "
                              + "CryptidsApiController class, with [AllowAnonymous] on the two GETs."
                            : "."));

                var text = await response.Content.ReadAsStringAsync();
                Assert.False(text.Contains("<html", StringComparison.OrdinalIgnoreCase),
                    $"{who} got the right status from a {verb}, with a page of HTML behind it. That's "
                    + "week 10's Missing page, answering every status. Task 5 keeps it away from /api, "
                    + "with app.UseWhen around UseStatusCodePagesWithReExecute.");
            }
        }
        Assert.True(CountCryptids() == creatures, "a creature is GONE. A visitor without the role deleted it.");

        var nowhere = await stranger.GetAsync("/api/nothing-here");
        Assert.False((await nowhere.Content.ReadAsStringAsync()).Contains("<html", StringComparison.OrdinalIgnoreCase),
            "GET /api/nothing-here came back as a page of HTML. A program asked for that. Task 5's "
            + "app.UseWhen keeps the Missing page away from everything under /api.");

        await using var curator = await Curator();
        Assert.True(curator.HoldsRole,
            "there's no curator to test with. Week 12's seeder came with the starter: leave it alone.");

        var changed = await curator.Client.PutAsync("/api/cryptids/6", Json(GoodCreature));
        Assert.True(changed.StatusCode == HttpStatusCode.NoContent,
            $"the curator's PUT to /api/cryptids/6 came back {(int)changed.StatusCode}"
            + (IsRedirect(changed) || changed.StatusCode == HttpStatusCode.Forbidden
                ? ". The role in the attribute must be spelled exactly as the seeder spells it: \"Curator\"."
                : ". A PUT that worked answers 204 with NoContent(): the client already has what it sent."));

        var read = await Parse(await curator.Client.GetAsync("/api/cryptids/6"));
        Assert.True(read.GetProperty("name").GetString() == "The Beast of Bray Road",
            "the curator's PUT said 204, and /api/cryptids/6 still has the old name. Copy the input "
            + "onto the record you loaded, then SaveChangesAsync.");
        Assert.True(curator.ImageUrl(6) == "/img/cryptids/chupacabra.webp",
            $"the PUT changed creature 6's plate to \"{curator.ImageUrl(6)}\". CryptidInput has no "
            + "ImageUrl, so nothing should touch it. Load the record and copy the five properties "
            + "across; _context.Update with a new Cryptid blanks whatever the input didn't carry. "
            + "Same trap as week 8's [Bind] list.");

        var gone = await curator.Client.DeleteAsync("/api/cryptids/6");
        Assert.True(gone.StatusCode == HttpStatusCode.NoContent,
            $"the curator's DELETE to /api/cryptids/6 came back {(int)gone.StatusCode}, not 204.");
        var after = await curator.Client.GetAsync("/api/cryptids/6");
        Assert.True(after.StatusCode == HttpStatusCode.NotFound,
            $"the curator deleted creature 6 and GET /api/cryptids/6 still answers {(int)after.StatusCode}.");

        var again = await curator.Client.DeleteAsync("/api/cryptids/6");
        Assert.True(again.StatusCode == HttpStatusCode.NotFound,
            $"deleting a creature that's already gone came back {(int)again.StatusCode}. Return NotFound().");
    }

    // ── a curator, the way the Registry gets one ──────────────────────────
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

        public string? ImageUrl(int id)
        {
            var (scope, context) = After.NewContext();
            using (scope)
            {
                return context.Set<Cryptid>().AsNoTracking().First(c => c.Id == id).ImageUrl;
            }
        }

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

    private static string Report(int cryptidId, string? seenOn = "2026-10-31")
    {
        var date = seenOn == null ? "" : $"\"seenOn\": \"{seenOn}\", ";
        return $$"""{ "cryptidId": {{cryptidId}}, "witness": "A passing stranger", "location": "Rhinelander, Wisconsin", {{date}}"account": "Green wood smoke and a sound like something heavy turning over." }""";
    }

    private static StringContent Json(string json) =>
        new StringContent(json, Encoding.UTF8, "application/json");

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
        return text.Length > 160 ? text[..160] + "…" : text;
    }

    private static string Email() => $"check-{Guid.NewGuid():N}@example.com";

    private static async Task<string> Html(HttpClient client, string url) =>
        await (await client.GetAsync(url)).Content.ReadAsStringAsync();

    private static bool IsRedirect(HttpResponseMessage response) =>
        (int)response.StatusCode is 301 or 302 or 303;

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

    private int CountCryptids()
    {
        var (scope, context) = _app.NewContext();
        using (scope)
        {
            return context.Set<Cryptid>().Count();
        }
    }

    private int CountSightings()
    {
        var (scope, context) = _app.NewContext();
        using (scope)
        {
            return context.Set<Sighting>().Count();
        }
    }
}
