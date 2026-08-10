// ═══════════════════════════════════════════════════════════════════
//  READ-ONLY — these checks are how you know the lab is done. They are not
//  your grade: the points come from your DEPLOYED app (see homework.md).
//  Run them with:  dotnet test Cryptids.Checks   (from the parent folder)
//  Your job is turning ❌ into ✅ by editing Cryptids.Web — never this file.
// ═══════════════════════════════════════════════════════════════════
using Microsoft.AspNetCore.Mvc.Testing;
using Cryptids.Web.Models;

namespace Cryptids.Checks;

public class CryptidChecks : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CryptidChecks(WebApplicationFactory<Program> factory)
        => _client = factory.CreateClient();

    [Fact] // passes out of the box — proves the harness works
    public async Task Check1_HomePageLoads()
    {
        var response = await _client.GetAsync("/");
        Assert.True(response.IsSuccessStatusCode, "GET / should return 200");
    }

    [Fact] // Task 2: add a CryptidsController with an Index action + view
    public async Task Check2_CryptidsPageExists()
    {
        var response = await _client.GetAsync("/Cryptids");
        Assert.True(response.IsSuccessStatusCode,
            "GET /Cryptids should return 200 — you need Controllers/CryptidsController.cs AND Views/Cryptids/Index.cshtml");
    }

    [Fact] // Task 3: the Index view lists every creature in the archive
    public async Task Check3_IndexListsEveryCryptid()
    {
        var html = await _client.GetStringAsync("/Cryptids");
        foreach (var cryptid in CryptidData.All)
        {
            Assert.True(html.Contains(cryptid.Name),
                $"/Cryptids is missing \"{cryptid.Name}\" — loop over the whole list with @foreach");
        }
    }

    [Fact] // Task 4: Details shows the ONE creature whose id is in the URL
    public async Task Check4_DetailsShowsOneCryptid()
    {
        var response = await _client.GetAsync("/Cryptids/Details/2");
        Assert.True(response.IsSuccessStatusCode, "GET /Cryptids/Details/2 should return 200");

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Bigfoot", html);              // cryptid 2's name
        Assert.Contains("Pacific Northwest", html);    // ...and its region
        Assert.DoesNotContain("Mothman", html);        // but NOT the whole archive
    }

    [Fact] // Task 5: an id nobody has must 404 — not crash, not show a blank page
    public async Task Check5_BadIdIsNotFound()
    {
        // a real id has to work first, or "404" would just mean "no controller yet"
        var good = await _client.GetAsync("/Cryptids/Details/1");
        Assert.True(good.IsSuccessStatusCode, "GET /Cryptids/Details/1 should return 200 (check 4 first)");

        var bad = await _client.GetAsync("/Cryptids/Details/999");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, bad.StatusCode);
    }

    [Fact] // Task 6: each row on Index links to its own Details page
    public async Task Check6_IndexLinksToDetails()
    {
        var html = await _client.GetStringAsync("/Cryptids");
        // URLs are case-insensitive, so /cryptids/details/1 is just as correct
        Assert.Contains("/Cryptids/Details/1", html, StringComparison.OrdinalIgnoreCase);
    }
}
