using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Cryptids.Web.Data;
using Cryptids.Web.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// The database. One registration: which context, which provider, which server.
// The connection string lives in user secrets — never in this file.
builder.Services.AddDbContext<CryptidContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity: who the members are, and the sign-in cookie that remembers them.
// The users live in CryptidContext, in the same database as the creatures.
builder.Services.AddIdentity<Member, IdentityRole>()
    .AddEntityFrameworkStores<CryptidContext>();

var app = builder.Build();

// Week 12: the Curator role, and who holds it. Runs once, at startup.
using (var scope = app.Services.CreateScope())
{
    await RoleSeeder.SeedAsync(scope.ServiceProvider);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
// A wrong id, or a URL nobody recognizes, used to be a blank browser page.
// This re-runs the request through /Home/Missing and keeps the 404 status.
// Week 13: pages only. Under /api, a status is the whole answer, and a page
// of HTML in the body is no use to a program.
app.UseWhen(context => !context.Request.Path.StartsWithSegments("/api"), pages =>
    pages.UseStatusCodePagesWithReExecute("/Home/Missing"));

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Week 13: a minimal API endpoint. The same kind of answer as a controller
// action, in one statement, with no class around it.
app.MapGet("/api/cryptids/count", async (CryptidContext db) =>
    new { count = await db.Cryptids.CountAsync() });

app.Run();

// Makes the app visible to the Cryptids.Checks test project — do not remove.
public partial class Program { }
