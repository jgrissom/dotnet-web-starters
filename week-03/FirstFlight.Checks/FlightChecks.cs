// ═══════════════════════════════════════════════════════════════════
//  READ-ONLY — these checks are how you know the lab is done. They are not
//  your grade: the points come from your DEPLOYED app (see homework.md).
//  Run them with:  dotnet test FirstFlight.Checks   (from the parent folder)
//  Your job is turning ❌ into ✅ by editing FirstFlight.Web — never this file.
// ═══════════════════════════════════════════════════════════════════
using Microsoft.AspNetCore.Mvc.Testing;

namespace FirstFlight.Checks;

public class FlightChecks : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public FlightChecks(WebApplicationFactory<Program> factory)
        => _client = factory.CreateClient();

    // Keeps an unexpected response readable. A whole HTML page inside an
    // assertion message buries the sentence that tells you what to fix.
    private static string Short(string s)
    {
        var t = s.Trim().ReplaceLineEndings(" ");
        return t.Length <= 70 ? t : t[..70] + "…";
    }

    [Fact] // passes out of the box — proves the harness works
    public async Task Check1_HomePageLoads()
    {
        var response = await _client.GetAsync("/");
        Assert.True(response.IsSuccessStatusCode,
            $"GET / returned {(int)response.StatusCode} — this one passes before you touch anything. "
            + "If it's red, something in FirstFlight.Web got broken; undo it before starting the lab.");
    }

    [Fact] // Task 2: make the site yours — brand and heading say "First Flight"
    public async Task Check2_SiteIsBranded()
    {
        var html = await _client.GetStringAsync("/");
        Assert.True(html.Contains("First Flight"),
            "the home page doesn't say \"First Flight\" anywhere. That's task 2, and it's two edits: "
            + "the navbar brand in Views/Shared/_Layout.cshtml, and the heading in "
            + "Views/Home/Index.cshtml. Both currently say the project name instead.");
    }

    [Fact] // Task 3: add an About action + view to HomeController
    public async Task Check3_AboutPageExists()
    {
        var response = await _client.GetAsync("/Home/About");
        Assert.True(response.IsSuccessStatusCode,
            $"GET /Home/About returned {(int)response.StatusCode}. You need BOTH an About() action on "
            + "HomeController and a Views/Home/About.cshtml to go with it — an action with no view "
            + "throws, and a view with no action is never reached. (Just added the .cshtml? "
            + "dotnet watch can't hot-reload a new view — answer its restart prompt, or press Ctrl+R.)");

        var html = await response.Content.ReadAsStringAsync();
        Assert.True(html.Contains("About"),
            "your About page loads, but the word \"About\" isn't on it. It wants a heading — "
            + "<h2>About</h2> — and a sentence about you.");
    }

    [Fact] // Task 4: put About in the navbar
    public async Task Check4_AboutIsInTheNav()
    {
        var html = await _client.GetStringAsync("/");
        // URLs are case-insensitive, so /home/about is just as correct
        Assert.True(html.Contains("/Home/About", StringComparison.OrdinalIgnoreCase),
            "nothing in your navbar links to /Home/About. Copy the Privacy <li> in "
            + "Views/Shared/_Layout.cshtml and point it at About — the page can exist and still be "
            + "unreachable, which is what this check is for.");
    }

    [Fact] // Task 5: a Hello action that reads a query parameter
    public async Task Check5_HelloGreetsByName()
    {
        var response = await _client.GetAsync("/Home/Hello?name=Ada");
        Assert.True(response.IsSuccessStatusCode,
            $"GET /Home/Hello?name=Ada returned {(int)response.StatusCode} — there's no Hello action "
            + "yet. Add one to HomeController that takes a string name and returns "
            + "Content($\"Hello, {name}!\"). No view needed for this one.");

        var text = await response.Content.ReadAsStringAsync();
        Assert.True(text.Contains("Hello, Ada!"),
            $"/Home/Hello?name=Ada answered, but it didn't say \"Hello, Ada!\" — I got \"{Short(text)}\". "
            + "The name arrives as the action's parameter: ASP.NET matches ?name=Ada to a parameter "
            + "called name, so the spelling has to agree.");
    }

    [Fact] // Task 6: ...and has a sensible default when no name is given
    public async Task Check6_HelloHasADefault()
    {
        var response = await _client.GetAsync("/Home/Hello");
        Assert.True(response.IsSuccessStatusCode,
            $"GET /Home/Hello with no name returned {(int)response.StatusCode}. Check 5 covers getting "
            + "the action there at all; this one is about what it does when nobody gives it a name.");

        var text = await response.Content.ReadAsStringAsync();
        Assert.True(text.Contains("Hello, stranger!"),
            $"/Home/Hello with no name should say \"Hello, stranger!\" — I got \"{Short(text)}\". "
            + "Make the parameter nullable and default it: string? name, then name ?? \"stranger\".");
    }
}
