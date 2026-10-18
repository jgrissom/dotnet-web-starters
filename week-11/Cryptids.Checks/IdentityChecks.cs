// ═══════════════════════════════════════════════════════════════════
//  READ-ONLY — these checks are how you know the lab is done. They are not
//  your grade: the points come from your DEPLOYED app (see homework.md).
//  Run them with:  dotnet test Cryptids.Checks   (from the parent folder)
//  Your job is turning ❌ into ✅ by editing Cryptids.Web — never this file.
//
//  This week the Registry learns who its visitors are. It gets a users
//  table, a way to create an account, a way to sign in and out, a lock on
//  the archive's writes — and a name for every member. Same rules as weeks
//  7–9: everything runs against an in-memory database, so no wifi and no
//  SQL Server, and 6/6 still doesn't prove your connection string works.
//
//  Nothing below names your user class as a type, because on the day you
//  start it doesn't exist yet — and nothing names Identity's own types
//  either, because the starter doesn't have the package. The checks find
//  your user the way the database does: the entity whose class is built
//  on IdentityUser.
// ═══════════════════════════════════════════════════════════════════
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;

namespace Cryptids.Checks;

public class IdentityChecks : IClassFixture<RegistryApp>
{
    private readonly RegistryApp _app;

    // A password Identity's default rules accept: upper, lower, a digit, a
    // symbol, and more than six characters.
    private const string GoodPassword = "Hodag-1893!";

    // The name every account in these checks is given, wherever a form asks
    // for one. Check 6 looks for it in the navbar.
    private const string MemberName = "Agent Kestrel";

    public IdentityChecks(RegistryApp app)
    {
        _app = app;
        _app.EnsureSeeded();
    }

    // ── Finding Identity without naming it ────────────────────────────────

    private static bool BuiltOn(Type? type, string baseName)
    {
        for (var t = type?.BaseType; t != null; t = t.BaseType)
        {
            if (t.Name == baseName || t.Name.StartsWith(baseName + "`", StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    // Your user class: in the model, built on IdentityUser, and not
    // IdentityUser itself.
    private static Type? UserType(DbContext context) =>
        context.Model.GetEntityTypes()
            .Select(e => e.ClrType)
            .FirstOrDefault(t => BuiltOn(t, "IdentityUser"));

    private (IServiceScope scope, DbContext context) Db() => _app.NewContext();

    private const string MakeTheUsers =
        "Task 2 sets it up: the Identity package, a Member class in Models/ that inherits "
        + "IdentityUser, CryptidContext inheriting IdentityDbContext<Member>, and AddIdentity in "
        + "Program.cs.";

    // ── 1 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Check1_TheRegistryYouWereHandedStillWorks()
    {
        var client = _app.NewClient();
        foreach (var url in new[] { "/", "/Cryptids", "/Cryptids/Details/1", "/Sightings/Create" })
        {
            var response = await client.GetAsync(url);
            Assert.True(response.IsSuccessStatusCode,
                $"GET {url} should return 200 — this one passes before you touch anything. It's "
                + "week 9's finished Registry: the list, a creature's page and the report form. "
                + "Tonight none of those should ever ask anyone to sign in. If it's red, something "
                + "in Cryptids.Web got broken; undo it.");
        }

        var missing = await client.GetAsync("/Cryptids/Details/999");
        Assert.True(missing.StatusCode == HttpStatusCode.NotFound
                && (await missing.Content.ReadAsStringAsync()).Length > 0,
            "a creature that doesn't exist should still come back 404 with the Registry's own "
            + "Not-on-file page in it. That came with the starter: the Missing view, the Missing "
            + "action on HomeController, and the UseStatusCodePagesWithReExecute line in Program.cs.");
    }

    // ── 2 ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Check2_TheRegistryHasAPlaceToKeepItsMembers()
    {
        Assert.True(BuiltOn(RegistryApp.ContextType, "IdentityDbContext"),
            "CryptidContext still inherits plain DbContext, so the database has nowhere to keep a "
            + "user. " + MakeTheUsers);

        var (scope, context) = Db();
        using (scope)
        {
            var user = UserType(context);
            Assert.True(user != null,
                "CryptidContext inherits IdentityDbContext, but not with a user class of your own. "
                + "Make Models/Member.cs — public class Member : IdentityUser — even with nothing in "
                + "it yet, and use IdentityDbContext<Member>. Changing the user class later means "
                + "changing every file that names it, so it's yours from the start.");

            var userManager = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("Microsoft.AspNetCore.Identity.UserManager`1"))
                .FirstOrDefault(t => t != null)?
                .MakeGenericType(user!);
            Assert.True(userManager != null && scope.ServiceProvider.GetService(userManager) != null,
                $"your {user!.Name} class is in the model, but nothing in Program.cs turns Identity on, so "
                + $"there is no UserManager<{user.Name}> to create an account with. Add, above "
                + $"builder.Build():\n"
                + $"    builder.Services.AddIdentity<{user.Name}, IdentityRole>()\n"
                + $"        .AddEntityFrameworkStores<CryptidContext>();");
        }

        var creates = MigrationsCreating("AspNetUsers");
        Assert.True(creates.Count > 0,
            "the model has Identity's tables in it, but no migration creates them yet. In "
            + "terminal 2: dotnet ef migrations add AddIdentity — then read it before you apply it.");
    }

    // ── 3 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Check3_AVisitorCanCreateAnAccount()
    {
        var client = _app.NewClient();
        var page = await client.GetAsync("/Account/Register");
        Assert.True(page.IsSuccessStatusCode,
            $"GET /Account/Register returned {(int)page.StatusCode}. Task 3 builds it: an "
            + "AccountController with a Register pair, a RegisterViewModel, and "
            + "Views/Account/Register.cshtml.");

        var before = CountUsers();

        // A password Identity refuses. The form should come back, and say why.
        var weak = await Register(client, Email(), "password", "password");
        var weakHtml = await weak.Content.ReadAsStringAsync();
        Assert.True((int)weak.StatusCode == 200 && CountUsers() == before,
            $"an account with the password \"password\" came back {(int)weak.StatusCode}"
            + (CountUsers() == before ? "" : " and was CREATED")
            + ". Identity refuses that password, and a refused registration comes back as the form "
            + "again — return View(form) — not a redirect.");
        Assert.True(weakHtml.Contains("Passwords must", StringComparison.Ordinal),
            "the password \"password\" was refused, but the page doesn't say why. CreateAsync hands "
            + "back an IdentityResult with the reasons in it, and they only reach the page if you "
            + "copy them into ModelState:\n"
            + "    foreach (var error in result.Errors)\n"
            + "    {\n"
            + "        ModelState.AddModelError(\"\", error.Description);\n"
            + "    }");

        // And one it accepts.
        var good = await Register(client, Email(), GoodPassword, GoodPassword);
        Assert.True((int)good.StatusCode is 301 or 302 or 303,
            $"a valid registration returned {(int)good.StatusCode} instead of a redirect. After "
            + "CreateAsync succeeds, sign the new member in and send them somewhere: "
            + "RedirectToAction(\"Index\", \"Home\").");
        Assert.True(CountUsers() == before + 1,
            $"a valid registration redirected, but no account was saved ({before} members, still "
            + $"{CountUsers()}). _userManager.CreateAsync(member, form.Password) is what writes the row.");
        Assert.True(SetsSignInCookie(good),
            "the account was created, but the new member isn't signed in — the response sets no "
            + ".AspNetCore.Identity.Application cookie. After CreateAsync succeeds:\n"
            + "    await _signInManager.SignInAsync(member, isPersistent: false);");
    }

    // ── 4 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Check4_AMemberCanSignOutAndBackIn()
    {
        var client = _app.NewClient();
        var email = Email();
        var registered = await Register(client, email, GoodPassword, GoodPassword);
        Assert.True((int)registered.StatusCode is 301 or 302 or 303,
            "couldn't create an account to sign in with — check 3 has to pass first.");

        // Signed in: the navbar offers a way out, and it's a form.
        var home = await Html(client, "/");
        if (!HasSignOutForm(home))
        {
            Assert.False(Regex.IsMatch(home, @"<a\b[^>]*href=""/Account/Logout""", RegexOptions.IgnoreCase),
                "the navbar's Sign out is a link. A link is a GET, and any page on the internet can "
                + "make your browser send a GET — an <img> tag is enough — so a GET must never sign "
                + "anybody out. Make it a form: <form asp-controller=\"Account\" asp-action=\"Logout\" "
                + "method=\"post\"> with a button inside, and mark the Logout action [HttpPost].");
            Assert.Fail(
                "you're signed in, and the navbar on the home page doesn't offer a way out. Task 4 "
                + "adds Views/Shared/_LoginPartial.cshtml — a Sign out form when "
                + "User.Identity.IsAuthenticated, Join and Sign in links when not — and "
                + "<partial name=\"_LoginPartial\" /> in _Layout.cshtml, after the navbar's </ul>.");
        }

        var signedOut = await client.PostAsync("/Account/Logout", WithToken(home, new()));
        Assert.True((int)signedOut.StatusCode is 301 or 302 or 303,
            $"posting the Sign out form returned {(int)signedOut.StatusCode}. The Logout action is "
            + "[HttpPost], calls await _signInManager.SignOutAsync(), and redirects home.");

        home = await Html(client, "/");
        Assert.True(!HasSignOutForm(home)
                && Regex.IsMatch(home, @"href=""/Account/Login""", RegexOptions.IgnoreCase),
            "after signing out, the navbar still looks signed in — or offers no Sign in link. The "
            + "partial's else branch has the Join and Sign in links.");

        // A wrong password signs nobody in.
        var wrong = await SignIn(client, email, "Not-the-password-9");
        Assert.True((int)wrong.StatusCode == 200 && !SetsSignInCookie(wrong),
            $"a WRONG password came back {(int)wrong.StatusCode}"
            + (SetsSignInCookie(wrong) ? " and SIGNED THE MEMBER IN" : "")
            + ". When PasswordSignInAsync doesn't succeed, add one message to ModelState and "
            + "return View(form).");

        // The right one does.
        var right = await SignIn(client, email, GoodPassword);
        Assert.True((int)right.StatusCode is 301 or 302 or 303 && SetsSignInCookie(right),
            $"the right password came back {(int)right.StatusCode} and "
            + (SetsSignInCookie(right) ? "set" : "set no")
            + " sign-in cookie. The Login POST calls\n"
            + "    await _signInManager.PasswordSignInAsync(form.Email, form.Password, "
            + "form.RememberMe, lockoutOnFailure: false);\n"
            + "...and redirects when result.Succeeded.");

        Assert.True(HasSignOutForm(await Html(client, "/")),
            "signed back in, but the navbar doesn't show the Sign out form.");
    }

    // ── 5 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Check5_OnlyMembersCanChangeTheArchive()
    {
        var stranger = _app.NewClient();

        foreach (var url in new[] { "/Cryptids/Create", "/Cryptids/Edit/1", "/Cryptids/Delete/1" })
        {
            var response = await stranger.GetAsync(url);
            Assert.True(SentToSignIn(response),
                $"a visitor who isn't signed in opened {url} — it came back "
                + $"{(int)response.StatusCode}. Task 5 locks the archive: [Authorize] on the "
                + "CryptidsController class, so every action in it needs a signed-in member, then "
                + "[AllowAnonymous] on Index and Details to open the two that only read.");
        }

        // The POSTs are what actually change the database. A form is not the
        // only way to send one.
        var token = WithToken(await Html(stranger, "/Sightings/Create"), new());
        var create = await stranger.PostAsync("/Cryptids/Create", token);
        Assert.True(SentToSignIn(create),
            $"a visitor who isn't signed in POSTed to /Cryptids/Create and got "
            + $"{(int)create.StatusCode}, not the sign-in page. Locking the GET hides the form; it "
            + "doesn't lock the POST. Put [Authorize] on the class, not on the GET actions.");

        var delete = await stranger.PostAsync("/Cryptids/Delete/6",
            WithToken(await Html(stranger, "/Sightings/Create"), new() { ["Id"] = "6" }));
        Assert.True(SentToSignIn(delete),
            $"a visitor who isn't signed in POSTed to /Cryptids/Delete/6 and got "
            + $"{(int)delete.StatusCode}. That is a stranger deleting a creature. [Authorize] on "
            + "the class covers DeleteConfirmed too.");

        foreach (var url in new[] { "/Cryptids", "/Cryptids/Details/1" })
        {
            var response = await stranger.GetAsync(url);
            Assert.True(response.IsSuccessStatusCode,
                $"{url} sent a visitor to the sign-in page. Reading the archive is public — "
                + "[AllowAnonymous] goes on Index and Details.");
        }

        // The public half: anyone can still file a report.
        var form = await Html(stranger, "/Sightings/Create");
        var before = CountSightings();
        var filed = await stranger.PostAsync("/Sightings/Create", WithToken(form, new()
        {
            ["Sighting.CryptidId"] = "1",
            ["Sighting.Witness"] = "A passing stranger",
            ["Sighting.Location"] = "Rhinelander, Wisconsin",
            ["Sighting.SeenOn"] = "2026-10-01",
            ["Sighting.Account"] = "Green wood smoke and a sound like something heavy turning over.",
        }));
        Assert.True((int)filed.StatusCode is 301 or 302 or 303 && CountSightings() == before + 1,
            $"a visitor who isn't signed in tried to file a report and got {(int)filed.StatusCode}. "
            + "Reports are the PUBLIC half of the Registry — anyone can file one, signed in or not. "
            + "SightingsController gets no [Authorize].");

        // And a member gets in.
        var member = _app.NewClient();
        await Register(member, Email(), GoodPassword, GoodPassword);
        var open = await member.GetAsync("/Cryptids/Create");
        Assert.True(open.IsSuccessStatusCode,
            $"a signed-in member opened /Cryptids/Create and got {(int)open.StatusCode}. Signed in "
            + "is all tonight's lock asks for.");
    }

    // ── 6 ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Check6_TheRegistryKnowsItsMembersByName()
    {
        string? property;
        var (scope, context) = Db();
        using (scope)
        {
            var user = UserType(context);
            Assert.True(user != null, "no user class yet. " + MakeTheUsers);

            property = user!.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .FirstOrDefault(p => p.PropertyType == typeof(string))?.Name;
            Assert.True(property != null,
                $"{user.Name} has nothing of its own yet — every property on it came from "
                + "IdentityUser. Task 6 adds one: public string FieldName { get; set; } = \"\"; "
                + "— the name a member signs their reports with. Then a migration, because "
                + "AspNetUsers just grew a column.");
        }

        Assert.True(MigrationsAdding("AspNetUsers", property!),
            $"{property} is on your user class, but no migration adds it to AspNetUsers. In "
            + "terminal 2: dotnet ef migrations add AddFieldName — then database update, then restart.");

        var client = _app.NewClient();
        var form = await Html(client, "/Account/Register");
        Assert.True(Regex.IsMatch(form, $@"name=""(?:[\w.]*\.)?{property}""", RegexOptions.IgnoreCase),
            $"the registration form doesn't ask for {property}. It goes on RegisterViewModel, then "
            + $"on Register.cshtml as a label and an input, then on the new member in the Register "
            + $"POST: new Member {{ ..., {property} = form.{property} }}.");

        var registered = await Register(client, Email(), GoodPassword, GoodPassword);
        Assert.True((int)registered.StatusCode is 301 or 302 or 303,
            "registering with a field name filled in didn't redirect — read check 3's message.");

        var home = await Html(client, "/");
        Assert.True(home.Contains(MemberName, StringComparison.Ordinal),
            $"I registered as \"{MemberName}\" and the navbar doesn't say so. In _LoginPartial, look "
            + "the member up — @inject UserManager<Member> UserManager, then "
            + "var member = await UserManager.GetUserAsync(User); — and show member.{property} "
            + "where the email was.");
    }

    // ── plumbing ──────────────────────────────────────────────────────────

    private static string Email() => $"check-{Guid.NewGuid():N}@example.com";

    private static async Task<string> Html(HttpClient client, string url) =>
        await (await client.GetAsync(url)).Content.ReadAsStringAsync();

    private static bool HasSignOutForm(string html) =>
        Regex.Matches(html, @"<form\b[^>]*>", RegexOptions.IgnoreCase)
            .Any(m => Regex.IsMatch(m.Value, @"action=""/Account/Logout""", RegexOptions.IgnoreCase)
                   && Regex.IsMatch(m.Value, @"method=""post""", RegexOptions.IgnoreCase));

    private static bool SetsSignInCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var cookies)
        && cookies.Any(c => Regex.IsMatch(c, @"^\.AspNetCore\.Identity\.Application=[^;]+"));

    private static bool SentToSignIn(HttpResponseMessage response) =>
        (int)response.StatusCode is 301 or 302 or 303
        && (response.Headers.Location?.ToString() ?? "").Contains("/Account/Login", StringComparison.OrdinalIgnoreCase);

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
    // would: the email box, the first password box, the second password box,
    // and MemberName in anything else.
    private static async Task<HttpResponseMessage> Register(HttpClient client, string email, string password, string confirm)
    {
        var html = await Html(client, "/Account/Register");
        var form = Regex.Match(html, @"<form[^>]*method=""post""[\s\S]*?</form>", RegexOptions.IgnoreCase);
        var fields = new Dictionary<string, string>();
        var passwords = 0;

        foreach (Match input in Regex.Matches(form.Success ? form.Value : html, @"<input\b[^>]*>", RegexOptions.IgnoreCase))
        {
            var name = Regex.Match(input.Value, @"\bname=""([^""]+)""").Groups[1].Value;
            var type = Regex.Match(input.Value, @"\btype=""([^""]+)""").Groups[1].Value.ToLowerInvariant();
            if (name == "" || name.StartsWith("__") || fields.ContainsKey(name))
            {
                continue;
            }

            if (type == "password")
            {
                fields[name] = passwords++ == 0 ? password : confirm;
            }
            else if (type == "email" || name.Contains("Email", StringComparison.OrdinalIgnoreCase))
            {
                fields[name] = email;
            }
            else if (type == "checkbox")
            {
                fields[name] = "false";
            }
            else
            {
                fields[name] = MemberName;
            }
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

    private int CountUsers()
    {
        var (scope, context) = Db();
        using (scope)
        {
            var user = UserType(context);
            return user == null ? 0 : Rows(context, user).Count();
        }
    }

    private int CountSightings()
    {
        var (scope, context) = Db();
        using (scope)
        {
            return context.Set<Cryptids.Web.Models.Sighting>().Count();
        }
    }

    private static IEnumerable<object> Rows(DbContext context, Type entity) =>
        ((IEnumerable<object>)context.GetType()
            .GetMethod(nameof(DbContext.Set), 1, Type.EmptyTypes)!
            .MakeGenericMethod(entity)
            .Invoke(context, null)!).Cast<object>();

    private static IEnumerable<Migration> Migrations() =>
        typeof(Program).Assembly.GetTypes()
            .Where(t => typeof(Migration).IsAssignableFrom(t) && !t.IsAbstract)
            .Select(t => (Migration)Activator.CreateInstance(t)!);

    private static List<Migration> MigrationsCreating(string table) =>
        Migrations().Where(m => m.UpOperations.OfType<CreateTableOperation>().Any(o => o.Name == table)).ToList();

    // True if any migration puts this column on the table — either in the
    // CreateTable that made it, or in a later AddColumn.
    private static bool MigrationsAdding(string table, string column) =>
        Migrations().Any(m => m.UpOperations.Any(o =>
            (o is CreateTableOperation c && c.Name == table && c.Columns.Any(col => col.Name == column))
            || (o is AddColumnOperation a && a.Table == table && a.Name == column)));
}
