// ═══════════════════════════════════════════════════════════════════
//  READ-ONLY — these checks are how you know the lab is done. They are not
//  your grade: the points come from your DEPLOYED app (see homework.md).
//  Run them with:  dotnet test Cryptids.Checks   (from the parent folder)
//  Your job is turning ❌ into ✅ by editing Cryptids.Web — never this file.
//
//  This week the Registry learns WHICH members may change it. Signing in is
//  no longer enough: the archive's writes need the Curator role, a member who
//  isn't a curator is told so on a page of the Registry's own, the curator
//  comes from configuration, the buttons nobody else can use disappear — and
//  the sign-in cookie turns out to carry the role. Same rules as weeks 7–11:
//  everything runs against an in-memory database, so no wifi and no SQL
//  Server, and 6/6 still doesn't prove your connection string works.
// ═══════════════════════════════════════════════════════════════════
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Cryptids.Web.Models;

namespace Cryptids.Checks;

public class RoleChecks : IClassFixture<RegistryApp>
{
    private readonly RegistryApp _app;

    // A password Identity's default rules accept: upper, lower, a digit, a
    // symbol, and more than six characters.
    private const string GoodPassword = "Hodag-1893!";

    // What every form that asks for anything else is given.
    private const string MemberName = "Agent Kestrel";

    private const string Role = "Curator";

    public RoleChecks(RegistryApp app)
    {
        _app = app;
        _app.EnsureSeeded();
    }

    private const string TheSeeder =
        "Task 4 hands it out: Data/RoleSeeder.cs, which makes the Curator role and gives it to "
        + "the member whose email is in Admin:Email, and the three lines in Program.cs that run it "
        + "once, just after builder.Build().";

    // ── 1 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Check1_TheRegistryYouWereHandedStillWorks()
    {
        var client = _app.NewClient();
        foreach (var url in new[] { "/", "/Cryptids", "/Cryptids/Details/1", "/Sightings/Create", "/Account/Register", "/Account/Login" })
        {
            var response = await client.GetAsync(url);
            Assert.True(response.IsSuccessStatusCode,
                $"GET {url} should return 200 — this one passes before you touch anything. It's "
                + "week 11's finished Registry: the list, a creature's page, the report form, and the "
                + "Join and Sign in pages. If it's red, something in Cryptids.Web got broken; undo it.");
        }

        var member = _app.NewClient();
        var joined = await Register(member, Email());
        Assert.True(IsRedirect(joined),
            $"joining the Registry came back {(int)joined.StatusCode} instead of a redirect. That's "
            + "week 11's register page, and it came with the starter. Undo whatever changed it.");
    }

    // ── 2 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Check2_OnlyCuratorsCanChangeTheArchive()
    {
        // Signed out: still sent to sign in, the same as week 11.
        var stranger = _app.NewClient();
        foreach (var url in new[] { "/Cryptids/Create", "/Cryptids/Edit/1", "/Cryptids/Delete/1" })
        {
            var response = await stranger.GetAsync(url);
            Assert.True(SentToSignIn(response),
                $"a visitor who isn't signed in opened {url} — it came back {(int)response.StatusCode}. "
                + "Keep [Authorize] on the CryptidsController CLASS; task 2 only adds a role to it.");
        }

        // Signed in, and NOT a curator: the week-11 lock lets this member in.
        var member = _app.NewClient();
        await Register(member, Email());

        foreach (var url in new[] { "/Cryptids/Create", "/Cryptids/Edit/1", "/Cryptids/Delete/1" })
        {
            var response = await member.GetAsync(url);
            Assert.True(Refused(response),
                $"a member who is signed in but ISN'T a curator opened {url} — it came back "
                + $"{(int)response.StatusCode}"
                + (SentToSignIn(response) ? ", to the sign-in page" : "")
                + ". Signed in is not trusted: anyone can join. Task 2 changes the attribute on the "
                + "CryptidsController class to [Authorize(Roles = \"Curator\")]. "
                + "⚠️ An attribute is the kind of edit hot reload can report as applied when it "
                + "isn't — if your browser still opens the form, press Ctrl+R in terminal 1.");
        }

        // The POSTs are what change the database.
        var create = await member.PostAsync("/Cryptids/Create",
            WithToken(await Html(member, "/Sightings/Create"), new()));
        Assert.True(Refused(create),
            $"a member who isn't a curator POSTed to /Cryptids/Create and got {(int)create.StatusCode}. "
            + "Locking the GET hides the form; it doesn't lock the POST. The role goes on the class.");

        var before = CountCryptids();
        var delete = await member.PostAsync("/Cryptids/Delete/6",
            WithToken(await Html(member, "/Sightings/Create"), new() { ["Id"] = "6" }));
        Assert.True(Refused(delete) && CountCryptids() == before,
            $"a member who isn't a curator POSTed to /Cryptids/Delete/6 and got {(int)delete.StatusCode}"
            + (CountCryptids() < before ? " — and the creature is GONE" : "")
            + ". [Authorize(Roles = \"Curator\")] on the class covers DeleteConfirmed too.");

        // Reading is still public, and so is reporting.
        foreach (var url in new[] { "/Cryptids", "/Cryptids/Details/1" })
        {
            var response = await stranger.GetAsync(url);
            Assert.True(response.IsSuccessStatusCode,
                $"{url} sent a visitor away with {(int)response.StatusCode}. Reading the archive is "
                + "public — keep [AllowAnonymous] on Index and Details.");
        }

        var form = await Html(stranger, "/Sightings/Create");
        var reports = CountSightings();
        var filed = await stranger.PostAsync("/Sightings/Create", WithToken(form, new()
        {
            ["Sighting.CryptidId"] = "1",
            ["Sighting.Witness"] = "A passing stranger",
            ["Sighting.Location"] = "Rhinelander, Wisconsin",
            ["Sighting.SeenOn"] = "2026-10-01",
            ["Sighting.Account"] = "Green wood smoke and a sound like something heavy turning over.",
        }));
        Assert.True(IsRedirect(filed) && CountSightings() == reports + 1,
            $"a visitor who isn't signed in tried to file a report and got {(int)filed.StatusCode}. "
            + "Reports are the PUBLIC half of the Registry — anyone can file one, signed in or not. "
            + "SightingsController gets no [Authorize], with or without a role.");
    }

    // ── 3 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Check3_AMemberWhoIsNotACuratorIsToldSo()
    {
        var member = _app.NewClient();
        await Register(member, Email());

        var refused = await member.GetAsync("/Cryptids/Create");
        Assert.True(IsRedirect(refused),
            $"a member who isn't a curator opened /Cryptids/Create and got {(int)refused.StatusCode}, "
            + "not a redirect. Check 2 comes first: [Authorize(Roles = \"Curator\")] on the class.");

        var where = refused.Headers.Location?.ToString() ?? "";
        Assert.True(where.Contains("/Account/AccessDenied", StringComparison.OrdinalIgnoreCase),
            $"a member who isn't a curator was sent to {where}. A signed-in member who lacks the "
            + "role goes to /Account/AccessDenied — Identity's default, the same way a stranger goes "
            + "to /Account/Login. If it's the sign-in page, the member isn't signed in: read check 1.");

        var page = await member.GetAsync(where);
        Assert.True(page.StatusCode == HttpStatusCode.OK,
            $"/Account/AccessDenied came back {(int)page.StatusCode}"
            + (page.StatusCode == HttpStatusCode.NotFound ? " — the Registry's Not on file page" : "")
            + ". Identity sends the member there, and nothing answers. Task 3 writes it: an "
            + "AccessDenied action on AccountController that returns View(), and "
            + "Views/Account/AccessDenied.cshtml.");

        var html = await page.Content.ReadAsStringAsync();
        Assert.True(html.Contains(Role, StringComparison.Ordinal),
            "/Account/AccessDenied opens, but it never says why: nothing on it mentions curators. "
            + "Say who this is for — \"Curators only\" — so a member knows it isn't broken.");
    }

    // ── 4 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Check4_TheCuratorComesFromConfiguration()
    {
        using (var scope = _app.Services.CreateScope())
        {
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            Assert.True(await roles.RoleExistsAsync(Role),
                "the app started and there's still no Curator role in AspNetRoles. " + TheSeeder);
        }

        await using var curator = await Curator();
        Assert.True(curator.HoldsRole,
            $"I set Admin:Email to {curator.Email}, joined with that email, and restarted the app — and "
            + $"{curator.Email} still isn't a curator. The seeder reads the email from configuration, "
            + "finds the member with FindByEmailAsync, and calls AddToRoleAsync(member, \"Curator\"). "
            + "Spell the key exactly Admin:Email. " + TheSeeder);

        foreach (var url in new[] { "/Cryptids/Create", "/Cryptids/Edit/1", "/Cryptids/Delete/1" })
        {
            var response = await curator.Client.GetAsync(url);
            Assert.True(response.StatusCode == HttpStatusCode.OK,
                $"the curator, signed in after the restart, opened {url} and got "
                + $"{(int)response.StatusCode} {response.Headers.Location}. The role on the class "
                + "must be spelled exactly as the seeder spells it: \"Curator\".");
        }
    }

    // ── 5 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Check5_OnlyCuratorsSeeTheButtons()
    {
        var stranger = _app.NewClient();
        var member = _app.NewClient();
        await Register(member, Email());

        foreach (var (who, client) in new[] { ("a visitor who isn't signed in", stranger), ("a member who isn't a curator", member) })
        {
            var list = await Html(client, "/Cryptids");
            Assert.False(Links(list, "/Cryptids/Create"),
                $"{who} sees the ＋ Open a new file button on /Cryptids. It leads to a page they can't "
                + "open. Task 5 wraps it in @if (User.IsInRole(\"Curator\")) { … }.");

            var details = await Html(client, "/Cryptids/Details/1");
            Assert.False(Links(details, "/Cryptids/Edit/1") || Links(details, "/Cryptids/Delete/1"),
                $"{who} sees ✏️ Correct the record or 🗑️ Close the file on a creature's page. Task 5 "
                + "wraps both in one @if (User.IsInRole(\"Curator\")) { … }.");

            Assert.True(Links(details, "/Sightings/Create"),
                $"{who} can't see 👁️ Report a sighting on a creature's page any more. That one stays: "
                + "reports are the public half. Only the archive's buttons go inside the @if.");
        }

        await using var curator = await Curator();
        Assert.True(curator.HoldsRole, "there's no curator to look with yet — check 4 comes first.");
        Assert.True(Links(await Html(curator.Client, "/Cryptids"), "/Cryptids/Create"),
            "the curator can't see ＋ Open a new file on /Cryptids. The @if hides it from everyone else, "
            + "not from the curator — check the role name inside IsInRole is \"Curator\".");
        var mine = await Html(curator.Client, "/Cryptids/Details/1");
        Assert.True(Links(mine, "/Cryptids/Edit/1") && Links(mine, "/Cryptids/Delete/1"),
            "the curator can't see ✏️ Correct the record and 🗑️ Close the file on a creature's page.");
    }

    // ── 6 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Check6_TheCookieCarriesTheRole()
    {
        var stranger = _app.NewClient();
        var away = await stranger.GetAsync("/Account/Claims");
        Assert.True(away.StatusCode != HttpStatusCode.NotFound,
            "GET /Account/Claims came back 404. Task 6 writes it: a Claims action on "
            + "AccountController, and Views/Account/Claims.cshtml listing User.Claims.");
        Assert.True(SentToSignIn(away),
            $"a visitor who isn't signed in opened /Account/Claims and got {(int)away.StatusCode}. "
            + "There's nobody to describe — put [Authorize] on the Claims action.");

        await using var curator = await Curator();
        Assert.True(curator.HoldsRole, "there's no curator to look with yet — check 4 comes first.");
        var page = await curator.Client.GetAsync("/Account/Claims");
        var html = await page.Content.ReadAsStringAsync();
        Assert.True(page.StatusCode == HttpStatusCode.OK,
            $"the curator opened /Account/Claims and got {(int)page.StatusCode}.");
        Assert.True(html.Contains(curator.Email, StringComparison.OrdinalIgnoreCase),
            "the curator's /Account/Claims doesn't show their email. Loop over every claim — "
            + "@foreach (var claim in User.Claims) — and show each one's Type and Value.");
        Assert.True(html.Contains("/claims/role", StringComparison.Ordinal) && html.Contains(Role, StringComparison.Ordinal),
            "the curator's /Account/Claims doesn't show a role claim with Curator in it. Show both "
            + "claim.Type and claim.Value — the role is one claim among the others.");
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
        public required string Email { get; init; }
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

        return new CuratorSession { Before = before, After = after, Client = client, Email = email, HoldsRole = holds };
    }

    // ── plumbing ──────────────────────────────────────────────────────────

    private static string Email() => $"check-{Guid.NewGuid():N}@example.com";

    private static async Task<string> Html(HttpClient client, string url) =>
        await (await client.GetAsync(url)).Content.ReadAsStringAsync();

    private static bool IsRedirect(HttpResponseMessage response) =>
        (int)response.StatusCode is 301 or 302 or 303;

    private static bool SentToSignIn(HttpResponseMessage response) =>
        IsRedirect(response)
        && (response.Headers.Location?.ToString() ?? "").Contains("/Account/Login", StringComparison.OrdinalIgnoreCase);

    // Turned away, but not to the sign-in page: a redirect to AccessDenied, or a 403.
    private static bool Refused(HttpResponseMessage response) =>
        response.StatusCode == HttpStatusCode.Forbidden
        || (IsRedirect(response) && !SentToSignIn(response)
            && (response.Headers.Location?.ToString() ?? "").Contains("AccessDenied", StringComparison.OrdinalIgnoreCase));

    // Any link to this path. Routing ignores case, so the check does too.
    private static bool Links(string html, string path) =>
        Regex.IsMatch(html, $@"href=""{Regex.Escape(path)}(?:[""?/])", RegexOptions.IgnoreCase);

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
