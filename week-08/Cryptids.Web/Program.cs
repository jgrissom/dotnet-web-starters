using Microsoft.EntityFrameworkCore;
using Cryptids.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// The database. One registration: which context, which provider, which server.
// The connection string itself lives in appsettings.json — never in this file.
builder.Services.AddDbContext<CryptidContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

// Makes the app visible to the Cryptids.Checks test project — do not remove.
public partial class Program { }
