// ═══════════════════════════════════════════════════════════════════
//  READ-ONLY — plumbing for the checks. Nothing here is part of the lab.
//
//  Same trick as week 7: the checks run your app in memory, swapping its
//  SQL Server provider for an in-memory one just for the test run, seeded
//  from the HasData in your context. Your Program.cs is untouched; the
//  swap happens after it runs. Nothing here talks to SQL Server, which is
//  why `dotnet test` works on the bus with no wifi.
//
//  That also means 6/6 does not prove your connection string works —
//  your browser proves that. Both matter.
// ═══════════════════════════════════════════════════════════════════
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cryptids.Checks;

public class RegistryApp : WebApplicationFactory<Program>
{
    // A fresh in-memory database per run, so one test's writes can't leak
    // into the next run's expectations.
    private readonly string _dbName = "registry-checks-" + Guid.NewGuid();

    /// <summary>The DbContext class in Cryptids.Web, or null if there isn't one.</summary>
    public static Type? ContextType =>
        typeof(Program).Assembly
            .GetTypes()
            .FirstOrDefault(t => typeof(DbContext).IsAssignableFrom(t) && !t.IsAbstract);

    /// <summary>
    /// True once CryptidsScaffoldController has been deleted. The scaffold is
    /// the reference you port FROM — the finished Registry doesn't keep it.
    /// </summary>
    public static bool ScaffoldIsGone =>
        typeof(Program).Assembly.GetTypes().All(t => t.Name != "CryptidsScaffoldController");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var contextType = ContextType;
        if (contextType == null)
        {
            return;         // no context — something drastic happened to the starter
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
                options.UseInMemoryDatabase(_dbName);

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
