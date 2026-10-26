// ═══════════════════════════════════════════════════════════════════
//  READ-ONLY — plumbing for the checks. Nothing here is part of the lab.
//
//  Same trick as weeks 7–11: the checks run your app in memory, swapping its
//  SQL Server provider for an in-memory one just for the test run, seeded
//  from the HasData in your context. The members, the roles and who holds
//  them all live in that same in-memory database, so everything these checks
//  create vanishes when the run ends. Your Program.cs is untouched; the swap
//  happens after it runs. Nothing here talks to SQL Server, which is why
//  `dotnet test` works on the bus with no wifi.
//
//  That also means 6/6 does not prove your connection string works —
//  your browser proves that. Both matter.
//
//  New this week: two copies of your app can share one in-memory database.
//  That's how check 4 restarts your app — the role is handed out once, at
//  startup, so a member who joined after the app started needs a second one.
// ═══════════════════════════════════════════════════════════════════
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cryptids.Checks;

public class RegistryApp : WebApplicationFactory<Program>
{
    // A fresh in-memory database per app, unless two apps are asked to share one.
    private readonly string _dbName;
    private readonly InMemoryDatabaseRoot _root;
    private readonly string? _adminEmail;

    public RegistryApp() : this(null, null, null)
    {
    }

    /// <summary>
    /// An app that shares its database with any other app given the same name and
    /// root, and that starts with Admin:Email set to <paramref name="adminEmail"/>.
    /// </summary>
    internal RegistryApp(string? dbName, InMemoryDatabaseRoot? root, string? adminEmail)
    {
        _dbName = dbName ?? "registry-checks-" + Guid.NewGuid();
        _root = root ?? new InMemoryDatabaseRoot();
        _adminEmail = adminEmail;
    }

    /// <summary>The DbContext class in Cryptids.Web, or null if there isn't one.</summary>
    public static Type? ContextType =>
        typeof(Program).Assembly
            .GetTypes()
            .FirstOrDefault(t => typeof(DbContext).IsAssignableFrom(t) && !t.IsAbstract);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Admin:Email for this run only. It is added last, so it beats your own
        // user secret — and when it isn't set, it is set to nothing, so your
        // secret's email never reaches the checks either.
        builder.ConfigureAppConfiguration(config =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:Email"] = _adminEmail ?? "",
            }));

        var contextType = ContextType;
        if (contextType == null)
        {
            return;         // no context — something drastic happened to the starter
        }

        // Create and seed the database BEFORE your app starts. From task 4 on,
        // your app writes a role the moment it starts — and an in-memory
        // database that already exists never gets your HasData creatures.
        var optionsBuilder = (DbContextOptionsBuilder)Activator.CreateInstance(
            typeof(DbContextOptionsBuilder<>).MakeGenericType(contextType))!;
        optionsBuilder.UseInMemoryDatabase(_dbName, _root);
        using (var seeding = (DbContext)Activator.CreateInstance(contextType, optionsBuilder.Options)!)
        {
            seeding.Database.EnsureCreated();
        }

        builder.ConfigureTestServices(services =>
        {
            // Drop every registration that mentions their context: the context
            // itself, DbContextOptions<T>, and whatever else AddDbContext added.
            var doomed = services
                .Where(d => d.ServiceType == contextType
                         || d.ServiceType == typeof(DbContextOptions)
                         || (d.ServiceType.IsGenericType
                             && d.ServiceType.GetGenericArguments().Contains(contextType)))
                .ToList();

            foreach (var descriptor in doomed)
            {
                services.Remove(descriptor);
            }

            // ...and register the same context against an in-memory provider.
            // AddDbContext is generic, and we only know the type at runtime.
            var addDbContext = typeof(EntityFrameworkServiceCollectionExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "AddDbContext"
                         && m.GetGenericArguments().Length == 1
                         && m.GetParameters().Length == 4
                         && m.GetParameters()[1].ParameterType == typeof(Action<DbContextOptionsBuilder>));

            Action<DbContextOptionsBuilder> useInMemory = options =>
                options.UseInMemoryDatabase(_dbName, _root);

            addDbContext
                .MakeGenericMethod(contextType)
                .Invoke(null, new object?[]
                {
                    services, useInMemory, ServiceLifetime.Scoped, ServiceLifetime.Scoped,
                });
        });
    }

    /// <summary>
    /// A scope plus their context, for the checks that need to look at the data
    /// directly rather than through a page.
    /// </summary>
    public (IServiceScope scope, DbContext context) NewContext()
    {
        var scope = Services.CreateScope();
        var context = (DbContext)scope.ServiceProvider.GetRequiredService(ContextType!);
        context.Database.EnsureCreated();       // applies the HasData in the context
        return (scope, context);
    }

    /// <summary>Creates and seeds the in-memory database, if there is a context.</summary>
    public void EnsureSeeded()
    {
        if (ContextType == null)
        {
            return;
        }

        var (scope, _) = NewContext();
        scope.Dispose();
    }

    public HttpClient NewClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,          // the redirect after a POST is the thing being checked
        HandleCookies = true,               // so the antiforgery cookie survives, like a browser
    });
}
