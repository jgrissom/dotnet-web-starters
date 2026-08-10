// ═══════════════════════════════════════════════════════════════════
//  READ-ONLY — these checks are how you know the lab is done. They are not
//  your grade: the points come from your DEPLOYED app (see homework.md).
//  Run them with:  dotnet test Cryptids.Checks   (from the parent folder)
//  Your job is turning ❌ into ✅ by editing Cryptids.Web — never this file.
//
//  This week the app already works. Every check below is about the SHELL —
//  the layout, the partial, the section and the theme. Almost all of your
//  edits happen in Views/Shared/_Layout.cshtml.
// ═══════════════════════════════════════════════════════════════════
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Cryptids.Checks;

public class ShellChecks : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    // The three pages the shell has to wrap. If it's really in the layout,
    // it shows up on all three without you editing any of them.
    private const string Home = "/";
    private const string Index = "/Cryptids";
    private const string Details = "/Cryptids/Details/1";      // The Hodag

    public ShellChecks(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> Html(string url) => await _client.GetStringAsync(url);

    // The text between <title> and </title>, trimmed. "" if there isn't one.
    private static string PageTitle(string html)
    {
        var open = html.IndexOf("<title", StringComparison.OrdinalIgnoreCase);
        if (open < 0) return "";
        var start = html.IndexOf('>', open);
        var end = html.IndexOf("</title>", start < 0 ? open : start, StringComparison.OrdinalIgnoreCase);
        return (start < 0 || end < 0) ? "" : html[(start + 1)..end].Trim();
    }

    // The text inside <a class="navbar-brand" ...>HERE</a>. "" if there isn't one.
    private static string NavbarBrand(string html)
    {
        var i = html.IndexOf("navbar-brand", StringComparison.OrdinalIgnoreCase);
        if (i < 0) return "";
        var start = html.IndexOf('>', i);
        var end = html.IndexOf("</a>", start < 0 ? i : start, StringComparison.OrdinalIgnoreCase);
        return (start < 0 || end < 0) ? "" : html[(start + 1)..end].Trim();
    }

    [Fact] // passes out of the box — the app you were handed already works
    public async Task Check1_TheSiteYouWereGivenWorks()
    {
        foreach (var url in new[] { Home, Index, Details })
        {
            var response = await _client.GetAsync(url);
            Assert.True(response.IsSuccessStatusCode,
                $"GET {url} should return 200 — this one passes before you touch anything. "
                + "If it's red, something in Cryptids.Web got broken; undo it before starting the lab.");
        }
    }

    [Fact] // Task 2: brand the layout — navbar, <title> suffix, and the footer
    public async Task Check2_ShellIsBranded()
    {
        foreach (var url in new[] { Home, Index, Details })
        {
            var html = await Html(url);

            Assert.True(NavbarBrand(html).Contains("Cryptid Registry"),
                $"the navbar brand on {url} should say \"Cryptid Registry\" — it currently says "
                + $"\"{NavbarBrand(html)}\". Edit the <a class=\"navbar-brand\"> text in "
                + "Views/Shared/_Layout.cshtml once, and all three pages change together.");

            Assert.True(PageTitle(html).Contains("Cryptid Registry"),
                $"the browser-tab title on {url} should end with \"Cryptid Registry\" — it is currently "
                + $"\"{PageTitle(html)}\". That's the <title> line in Views/Shared/_Layout.cshtml.");

            Assert.True(html.Contains("Field Reports Since 1893"),
                $"the footer on {url} should include \"Field Reports Since 1893\". It's the third and last "
                + "edit in Views/Shared/_Layout.cshtml — and the reason all three pages change at once "
                + "is that there is only one of it.");
        }
    }

    [Fact] // Task 3: each page sets its own ViewData["Title"], and Details' is data-driven
    public async Task Check3_EveryPageHasItsOwnTitle()
    {
        var indexTitle = PageTitle(await Html(Index));
        var detailsTitle = PageTitle(await Html(Details));

        Assert.True(indexTitle != detailsTitle,
            $"/Cryptids and /Cryptids/Details/1 both show the title \"{indexTitle}\". Set "
            + "ViewData[\"Title\"] at the top of each view — the layout is already reading it.");

        Assert.True(detailsTitle.Contains("The Hodag"),
            $"the title on /Cryptids/Details/1 should name that creature, but it reads \"{detailsTitle}\". "
            + "A details page title should come from the data: ViewData[\"Title\"] = Model.Name;");
    }

    [Fact] // Task 4: one card file, rendered from two different views
    public async Task Check4_CardIsAPartialUsedTwice()
    {
        var contentRoot = _factory.Services
            .GetRequiredService<IWebHostEnvironment>().ContentRootPath;
        var partial = Path.Combine(contentRoot, "Views", "Shared", "_CryptidCard.cshtml");

        Assert.True(File.Exists(partial),
            "Views/Shared/_CryptidCard.cshtml doesn't exist yet. Create it with `@model Cryptid` on "
            + "the first line — the lab README has the markup to paste.");

        var index = await Html(Index);
        var cardsOnIndex = index.Split("cryptid-card").Length - 1;
        Assert.True(cardsOnIndex >= 5,
            $"/Cryptids should render one card per creature, but I count {cardsOnIndex}. Replace the "
            + "<table> with a card grid and render the partial inside the loop: "
            + "<partial name=\"_CryptidCard\" model=\"cryptid\" />");

        // The whole point of a partial is that it works in more than one place.
        var home = await Html(Home);
        Assert.True(home.Contains("cryptid-card"),
            "the home page isn't using your card. One file rendered from one place isn't reuse — "
            + "add a featured creature to Views/Home/Index.cshtml and render the same partial there. "
            + "CryptidData is already available in views, so no controller change is needed.");
    }

    [Fact] // Task 5: a page-specific script, delivered through the layout's Scripts section
    public async Task Check5_DetailsAddsAScript()
    {
        var detailsHtml = await Html(Details);
        var indexHtml = await Html(Index);

        Assert.True(detailsHtml.Contains("Cryptid file loaded"),
            "/Cryptids/Details/1 should contain the text \"Cryptid file loaded\". Add a "
            + "@section Scripts { ... } block to Views/Cryptids/Details.cshtml.");

        Assert.False(indexHtml.Contains("Cryptid file loaded"),
            "/Cryptids is showing \"Cryptid file loaded\" too — that script belongs to the details "
            + "page only. It's in the layout or in Index.cshtml; move it into a @section Scripts "
            + "block in Details.cshtml.");

        // A section renders where the LAYOUT puts it — down with the other scripts,
        // below the footer. Pasted into the view body it would land above the footer.
        var script = detailsHtml.IndexOf("Cryptid file loaded", StringComparison.Ordinal);
        var footer = detailsHtml.IndexOf("<footer", StringComparison.OrdinalIgnoreCase);
        Assert.True(footer >= 0 && script > footer,
            "\"Cryptid file loaded\" is rendering in the middle of the page instead of down with the "
            + "other scripts. That means it's sitting in the view's markup rather than in a "
            + "@section Scripts block — the layout decides where a section lands.");
    }

    [Fact] // Task 6: swap the stock Bootstrap stylesheet for a Bootswatch theme
    public async Task Check6_ThemeIsNotTheDefault()
    {
        foreach (var url in new[] { Home, Index, Details })
        {
            var html = await Html(url);

            // hrefs are URLs — case never matters in one
            Assert.True(html.Contains("bootswatch", StringComparison.OrdinalIgnoreCase),
                $"{url} isn't loading a Bootswatch theme. Replace the Bootstrap <link> in "
                + "Views/Shared/_Layout.cshtml with a Bootswatch one from https://bootswatch.com.");

            Assert.False(html.Contains("lib/bootstrap/dist/css/bootstrap.min.css", StringComparison.OrdinalIgnoreCase),
                $"{url} is still loading the stock bootstrap.min.css as well as the theme. "
                + "Bootswatch REPLACES that stylesheet — delete the original <link>, or the two "
                + "will fight and the theme will only half apply.");
        }
    }
}
