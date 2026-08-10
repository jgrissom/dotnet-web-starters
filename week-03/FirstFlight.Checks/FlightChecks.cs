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

    [Fact] // passes out of the box — proves the harness works
    public async Task Check1_HomePageLoads()
    {
        var response = await _client.GetAsync("/");
        Assert.True(response.IsSuccessStatusCode, "GET / should return 200");
    }

    [Fact] // Task 2: make the site yours — brand and heading say "First Flight"
    public async Task Check2_SiteIsBranded()
    {
        var html = await _client.GetStringAsync("/");
        Assert.Contains("First Flight", html);
    }

    [Fact] // Task 3: add an About action + view to HomeController
    public async Task Check3_AboutPageExists()
    {
        var response = await _client.GetAsync("/Home/About");
        Assert.True(response.IsSuccessStatusCode, "GET /Home/About should return 200 — add the action AND the view");
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("About", html);
    }

    [Fact] // Task 4: put About in the navbar
    public async Task Check4_AboutIsInTheNav()
    {
        var html = await _client.GetStringAsync("/");
        // URLs are case-insensitive, so /home/about is just as correct
        Assert.Contains("/Home/About", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact] // Task 5: a Hello action that reads a query parameter
    public async Task Check5_HelloGreetsByName()
    {
        var text = await _client.GetStringAsync("/Home/Hello?name=Ada");
        Assert.Contains("Hello, Ada!", text);
    }

    [Fact] // Task 6: ...and has a sensible default when no name is given
    public async Task Check6_HelloHasADefault()
    {
        var text = await _client.GetStringAsync("/Home/Hello");
        Assert.Contains("Hello, stranger!", text);
    }
}
