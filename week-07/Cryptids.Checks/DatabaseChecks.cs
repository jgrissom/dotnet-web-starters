// ═══════════════════════════════════════════════════════════════════
//  READ-ONLY — these checks are how you know the lab is done. They are not
//  your grade: the points come from your DEPLOYED app (see homework.md).
//  Run them with:  dotnet test Cryptids.Checks   (from the parent folder)
//  Your job is turning ❌ into ✅ by editing Cryptids.Web — never this file.
//
//  This week the Registry gets a real database. These checks never touch
//  SQL Server: they run your app against an in-memory database seeded from
//  the HasData you wrote, so they work with no wifi and can't be broken by
//  fourteen people connecting at once. Proving you reached the SCHOOL's
//  server is your deployed app's job, and that's what the homework checks.
// ═══════════════════════════════════════════════════════════════════
using System.Reflection;
using System.Text.RegularExpressions;
using Cryptids.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cryptids.Checks;

public class DatabaseChecks : IClassFixture<RegistryApp>
{
    private readonly RegistryApp _app;
    private readonly HttpClient _client;

    private const string Home = "/";
    private const string Index = "/Cryptids";
    private const string Details1 = "/Cryptids/Details/1";
    private const string Create = "/Cryptids/Create";

    // The six the registry has shipped with since week 4.
    private static readonly string[] Seeded =
    {
        "The Hodag", "Bigfoot", "Mothman",
        "The Loch Ness Monster", "The Jersey Devil", "Chupacabra",
    };

    public DatabaseChecks(RegistryApp app)
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

    private static string AntiForgeryToken(string html)
    {
        var m = Regex.Match(html, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
        return m.Success ? m.Groups[1].Value : "";
    }

    private async Task<HttpResponseMessage> SubmitForm(Dictionary<string, string> fields)
    {
        var page = await _client.GetAsync(Create);
        Assert.True(page.IsSuccessStatusCode,
            $"GET {Create} returned {(int)page.StatusCode} — the form your app has had since "
            + "week 6 isn't loading, so something earlier in the lab is broken.");

        var token = AntiForgeryToken(await page.Content.ReadAsStringAsync());
        if (token != "") fields["__RequestVerificationToken"] = token;

        return await _client.PostAsync(Create, new FormUrlEncodedContent(fields));
    }

    // The context class, or a failure that says which task builds it.
    private static Type RequireContext() =>
        RegistryApp.ContextType
        ?? throw new Xunit.Sdk.XunitException(
            "there's no DbContext anywhere in Cryptids.Web. A DbContext is the class that "
            + "represents your database — task 2 creates Data/CryptidContext.cs:\n"
            + "    public class CryptidContext : DbContext\n"
            + "    {\n"
            + "        public CryptidContext(DbContextOptions<CryptidContext> options) : base(options) { }\n"
            + "        public DbSet<Cryptid> Cryptids => Set<Cryptid>();\n"
            + "    }");

    [Fact] // passes out of the box — the app you were handed already works
    public async Task Check1_TheSiteYouWereGivenWorks()
    {
        foreach (var url in new[] { Home, Index, Details1, Create })
        {
            var response = await _client.GetAsync(url);
            Assert.True(response.IsSuccessStatusCode,
                $"GET {url} should return 200 — this one passes before you touch anything. "
                + "It's week 6's finished Registry, form and all. If it's red, something in "
                + "Cryptids.Web got broken; undo it before starting the lab.");
        }
    }

    [Fact] // Task 2: the context describes the database, seed data and all
    public async Task Check2_TheContextDescribesTheDatabase()
    {
        await Task.CompletedTask;
        var contextType = RequireContext();

        // A DbSet<Cryptid> is the table. Without one, the context knows no Cryptids.
        var set = contextType.GetProperties()
            .FirstOrDefault(p => p.PropertyType.IsGenericType
                              && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)
                              && p.PropertyType.GetGenericArguments()[0] == typeof(Cryptid));

        Assert.True(set != null,
            $"{contextType.Name} has no DbSet<Cryptid> property. That property IS the table — it's "
            + "how the rest of your app queries it, and it's what tells EF Core there should be a "
            + "Cryptids table at all. Add:\n"
            + "    public DbSet<Cryptid> Cryptids => Set<Cryptid>();");

        // The six creatures now belong to the MODEL, not to a static list.
        var (scope, context) = _app.NewContext();
        using (scope)
        {
            // The design-time model is the one that still remembers HasData; the
            // runtime model drops it once the database exists.
            var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Cryptid));
            Assert.True(entity != null,
                "EF Core's model has no Cryptid entity, even though the DbSet is there. That "
                + "usually means OnModelCreating throws before it finishes.");

            var seed = entity!.GetSeedData().ToList();
            Assert.True(seed.Count >= 6,
                $"your model seeds {seed.Count} creature(s); the registry has always had 6. The seed "
                + "data moves out of CryptidData.cs and into the context, so a migration can carry "
                + "it into the table:\n"
                + "    protected override void OnModelCreating(ModelBuilder modelBuilder)\n"
                + "    {\n"
                + "        modelBuilder.Entity<Cryptid>().HasData(\n"
                + "            new Cryptid { Id = 1, Name = \"The Hodag\", ... },\n"
                + "            ...\n"
                + "        );\n"
                + "    }");

            var names = seed.Select(row => row["Name"] as string).ToList();
            foreach (var expected in Seeded)
            {
                Assert.True(names.Contains(expected),
                    $"\"{expected}\" isn't in your seed data. All six creatures come across, with the "
                    + "same Ids they had in CryptidData.cs — the details pages people already "
                    + $"bookmarked are /Cryptids/Details/1..6. I found: {string.Join(", ", names)}");
            }

            var ids = seed.Select(row => Convert.ToInt32(row["Id"])).OrderBy(i => i).ToList();
            Assert.True(ids.SequenceEqual(new[] { 1, 2, 3, 4, 5, 6 }),
                $"your seeded Ids are {string.Join(", ", ids)} — they should be 1 through 6. HasData "
                + "needs an explicit Id for every row: it has to know which row is which in order to "
                + "work out what changed next time you add a migration.");
        }
    }

    [Fact] // Task 3: Program.cs points the context at SQL Server, via user secrets
    public async Task Check3_TheAppIsWiredToSqlServer()
    {
        await Task.CompletedTask;
        var contextType = RequireContext();

        // A plain factory — no in-memory swap — so this sees YOUR registration.
        // Resolving the options doesn't open a connection, so no server is needed.
        using var plain = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>();

        object? options;
        using var probe = plain.Services.CreateScope();
        try
        {
            var optionsType = typeof(DbContextOptions<>).MakeGenericType(contextType);
            options = probe.ServiceProvider.GetService(optionsType);
        }
        catch (Exception e)
        {
            throw new Xunit.Sdk.XunitException(
                "your app won't start. The usual cause at this point in the lab is a missing "
                + "connection string: builder.Configuration.GetConnectionString(\"DefaultConnection\") "
                + "returned nothing, and UseSqlServer(null) throws.\n"
                + $"The actual error was: {e.InnerException?.Message ?? e.Message}");
        }

        Assert.True(options != null,
            $"nothing registered your {contextType.Name} with the app. A context has to be handed to "
            + "the dependency-injection container before a controller can ask for one. In Program.cs, "
            + "above var app = builder.Build():\n"
            + $"    builder.Services.AddDbContext<{contextType.Name}>(options =>\n"
            + "        options.UseSqlServer(builder.Configuration.GetConnectionString(\"DefaultConnection\")));");

        var extensions = ((IDbContextOptions)options!).Extensions;
        var usesSqlServer = extensions.Any(e => e.GetType().Name.Contains("SqlServer"));

        Assert.True(usesSqlServer,
            "your context is registered, but not against SQL Server. The provider is chosen by the "
            + "Use... call inside AddDbContext, and this course uses the school's SQL Server:\n"
            + "    options.UseSqlServer(builder.Configuration.GetConnectionString(\"DefaultConnection\"))\n"
            + $"(I found: {string.Join(", ", extensions.Select(e => e.GetType().Name))})");

        // The connection string has to come from configuration — and it lives in user
        // secrets, which is a file in YOUR user profile, not a file in this project.
        var config = plain.Services.GetRequiredService<IConfiguration>();
        var connectionString = config.GetConnectionString("DefaultConnection");

        Assert.False(string.IsNullOrWhiteSpace(connectionString),
            "no connection string reached the app. It doesn't live in a file in this project — it "
            + "lives in user secrets, in your own user profile. From inside Cryptids.Web:\n"
            + "    dotnet user-secrets init\n"
            + "    dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"Server=...;Database=...;User ID=...;Password=...;TrustServerCertificate=True\"\n"
            + "If you already set this on another machine, set it again here — secrets don't travel "
            + "with your repo, and a lab PC that reboots clean loses them.");

        Assert.False(connectionString!.Contains('<') || connectionString.Contains('>'),
            "your connection string still has the angle-bracket placeholders in it. Replace "
            + "<SCHOOL-SQL-SERVER>, <YOUR-DATABASE>, <YOUR-USERNAME> and <YOUR-PASSWORD> with the "
            + "details on the class handout, then set it again:\n"
            + "    dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"...\"");
    }

    [Fact] // Task 4: a migration exists, and it builds the table
    public async Task Check4_AMigrationDescribesTheTable()
    {
        await Task.CompletedTask;
        RequireContext();

        var migrations = typeof(Program).Assembly
            .GetTypes()
            .Where(t => typeof(Migration).IsAssignableFrom(t)
                     && !t.IsAbstract
                     && t.GetCustomAttribute<MigrationAttribute>() != null)
            .ToList();

        Assert.True(migrations.Count > 0,
            "there are no migrations in your project. A migration is the C# file that turns your "
            + "model into actual tables — writing the context doesn't create anything on its own. "
            + "From inside the Cryptids.Web folder:\n"
            + "    dotnet ef migrations add InitialCreate\n"
            + "    dotnet ef database update");

        var snapshot = typeof(Program).Assembly.GetTypes()
            .Any(t => typeof(ModelSnapshot).IsAssignableFrom(t) && !t.IsAbstract);
        Assert.True(snapshot,
            "your project has a migration but no model snapshot, which means the Migrations folder "
            + "is only half there. Delete the Migrations folder and run dotnet ef migrations add "
            + "InitialCreate again.");

        // Read the operations the migration would run. This needs no database.
        var migration = (Migration)Activator.CreateInstance(migrations[0])!;
        var operations = migration.UpOperations;

        var createdTables = operations.OfType<CreateTableOperation>().Select(o => o.Name).ToList();
        Assert.True(createdTables.Any(t => t.Equals("Cryptids", StringComparison.OrdinalIgnoreCase)),
            "your first migration doesn't create a Cryptids table. It should have been generated "
            + $"from your DbSet<Cryptid>. (It creates: {(createdTables.Count == 0 ? "nothing" : string.Join(", ", createdTables))}) "
            + "If you added the migration before writing the DbSet, delete the Migrations folder "
            + "and add it again.");

        var seededRows = operations.OfType<InsertDataOperation>().Sum(o => o.Values.GetLength(0));
        Assert.True(seededRows >= 6,
            $"your migration inserts {seededRows} row(s) of seed data, and the registry has 6 "
            + "creatures. The HasData from task 2 has to be in place BEFORE you add the migration — "
            + "a migration is a snapshot of the model at the moment you generated it. Delete the "
            + "Migrations folder and run dotnet ef migrations add InitialCreate again.");
    }

    [Fact] // Task 5: the read pages come from the database
    //
    // Deliberately does NOT require CryptidData.cs to be gone. Deleting it breaks
    // the build until the POST action is rewritten, and a broken build means no
    // check runs at all — so demanding it here made check 5 impossible to pass
    // without also finishing task 6, and made the in-class target of 1–5
    // unreachable. Task 6 owns the deletion.
    public async Task Check5_TheRegistryReadsFromTheDatabase()
    {
        RequireContext();

        var indexHtml = await Html(Index);
        foreach (var name in Seeded)
        {
            Assert.True(indexHtml.Contains(name),
                $"\"{name}\" isn't on /Cryptids. The index action should hand the view everything in "
                + "the table:\n"
                + "    return View(_context.Cryptids.ToList());");
        }

        // The strongest available proof that the page really reads the context:
        // put a creature in the database by hand and see whether the page notices.
        var (scope, context) = _app.NewContext();
        using (scope)
        {
            context.Add(new Cryptid
            {
                Name = "Test Subject Alpha",
                Region = "The Checks Project",
                FirstSighting = 2026,
                Sightings = 1,
                IsDebunked = false,
            });
            context.SaveChanges();
        }

        var afterInsert = await Html(Index);
        Assert.True(afterInsert.Contains("Test Subject Alpha"),
            "I added a creature straight to the database and /Cryptids didn't show it. Your index "
            + "action is still reading from somewhere else — it needs to go through the context "
            + "your controller was handed:\n"
            + "    private readonly CryptidContext _context;\n"
            + "    public CryptidsController(CryptidContext context) { _context = context; }\n"
            + "    public IActionResult Index() => View(_context.Cryptids.ToList());");

        var details = await _client.GetAsync(Details1);
        Assert.True(details.IsSuccessStatusCode,
            $"{Details1} returned {(int)details.StatusCode}. Details has to look the creature up in "
            + "the table too:\n"
            + "    var cryptid = _context.Cryptids.FirstOrDefault(c => c.Id == id);");

        Assert.Contains("The Hodag", await details.Content.ReadAsStringAsync());
    }

    [Fact] // Task 6: a filed report is written to the database, with an Id from SQL Server
    public async Task Check6_AFiledReportIsSaved()
    {
        RequireContext();

        // The deletion lives here, not in check 5: removing the file breaks the
        // build until this action stops using it, so the two go together.
        Assert.True(RegistryApp.CryptidDataIsGone,
            "Models/CryptidData.cs is still in your project. The whole point of tonight is that the "
            + "list moved into SQL Server — the static List<Cryptid> is now a duplicate copy of the "
            + "data that nothing should be reading. Delete the file. The only thing that will stop "
            + "compiling is the POST action below, which is exactly the code this task asks you to "
            + "rewrite.");

        var before = DetailsIds(await Html(Index));

        var response = await SubmitForm(new Dictionary<string, string>
        {
            ["Name"] = "The Beast of Bray Road",
            ["Region"] = "Elkhorn, Wisconsin",
            ["FirstSighting"] = "1936",
            ["Sightings"] = "42",
            ["IsDebunked"] = "false",
        });

        Assert.True((int)response.StatusCode is 302 or 303,
            $"filing a good report returned {(int)response.StatusCode} instead of a redirect. Week 6's "
            + "POST action already did this correctly; if it broke tonight, the likely cause is that "
            + "removing CryptidData left the action half-rewritten.");

        var indexHtml = await Html(Index);
        Assert.True(indexHtml.Contains("The Beast of Bray Road"),
            "the redirect happened, but the new creature isn't on /Cryptids. Adding it to the context "
            + "only writes it down in memory — nothing reaches the database until you save:\n"
            + "    _context.Cryptids.Add(cryptid);\n"
            + "    _context.SaveChanges();");

        var added = DetailsIds(indexHtml).Except(before).ToList();
        Assert.True(added.Count == 1,
            $"I expected exactly one new Details link and found {added.Count}.");

        // Id is an IDENTITY column now. The database picks the number, and EF
        // Core reads it back onto the object — which is why the old
        // Max(c => c.Id) + 1 line has to go.
        Assert.True(added[0] > 0,
            "the new creature has Id 0, so nothing gave it one. If you kept week 6's "
            + "cryptid.Id = ... Max(c => c.Id) + 1 line, delete it: Id is an IDENTITY column now, "
            + "SQL Server assigns it, and EF Core copies the real value back onto your object "
            + "during SaveChanges().");

        // And it's genuinely in the table, not just in a list somewhere.
        var (scope, context) = _app.NewContext();
        using (scope)
        {
            var saved = context.Set<Cryptid>().FirstOrDefault(c => c.Name == "The Beast of Bray Road");
            Assert.True(saved != null,
                "the creature is on the page but not in the database. Something is still keeping a "
                + "list of its own — check that the POST action goes through the context.");

            Assert.True(saved!.Id == added[0],
                $"the database gave the new creature Id {saved.Id}, but the registry links to "
                + $"/Cryptids/Details/{added[0]}. The Id on the object after SaveChanges() is the "
                + "real one; don't overwrite it.");
        }

        var detailsPage = await _client.GetAsync($"/Cryptids/Details/{added[0]}");
        Assert.True(detailsPage.IsSuccessStatusCode,
            $"/Cryptids/Details/{added[0]} returned {(int)detailsPage.StatusCode} for a creature that "
            + "is definitely in the table.");
    }
}
