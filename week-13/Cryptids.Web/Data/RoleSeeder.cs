using Microsoft.AspNetCore.Identity;
using Cryptids.Web.Models;

namespace Cryptids.Web.Data;

// Runs once, when the app starts. Makes sure the Curator role exists, then
// gives it to the member whose email is in configuration under Admin:Email —
// a user secret on your machine, an app setting on Azure. No email set, or
// nobody has joined with it yet: the role exists, and nobody holds it.
public static class RoleSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<Member>>();
        var config = services.GetRequiredService<IConfiguration>();

        if (!await roleManager.RoleExistsAsync("Curator"))
        {
            await roleManager.CreateAsync(new IdentityRole("Curator"));
        }

        var email = config["Admin:Email"];
        if (string.IsNullOrEmpty(email))
        {
            return;
        }

        var member = await userManager.FindByEmailAsync(email);
        if (member != null && !await userManager.IsInRoleAsync(member, "Curator"))
        {
            await userManager.AddToRoleAsync(member, "Curator");
        }
    }
}
