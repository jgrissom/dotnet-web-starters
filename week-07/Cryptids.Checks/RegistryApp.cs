// ═══════════════════════════════════════════════════════════════════
//  READ-ONLY — plumbing for the checks. Nothing here is part of the lab.
//
//  The checks run your app in memory, the same way week 6's did. The one
//  new problem is that your app now wants SQL Server, and a test run must
//  not depend on the school's server being reachable — or on fourteen
//  people hitting it at once during lab.
//
//  So: if your app has a DbContext, this swaps its provider for an
//  in-memory one just for the test run, and seeds it from the HasData you
//  wrote. Your Program.cs is untouched; the swap happens after it runs.
//  Nothing here talks to SQL Server, which is why `dotnet test` works on
//  the bus with no wifi.
//
//  It finds your context by looking for a DbContext in your project, so
//  it doesn't matter whether you called it CryptidContext, CryptidsContext
//  or RegistryContext.
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

    /// <summary>The DbContext class in Cryptids.Web, or null if there isn't one yet.</summary>
    public static Type? ContextType =>
        typeof(Program).Assembly
            .GetTypes()
            .FirstOrDefault(t => typeof(DbContext).IsAssignableFrom(t) && !t.IsAbstract);

    /// <summary>True once Models/CryptidData.cs has been deleted.</summary>
    public static bool CryptidDataIsGone =>
        typeof(Program).Assembly.GetTypes().All(t => t.Name != "CryptidData");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var contextType = ContextType;
        if (contextType == null)
        {
            return;         // no database yet — the app still uses the static list
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
        context.Database.EnsureCreated();       // applies the HasData they wrote
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
