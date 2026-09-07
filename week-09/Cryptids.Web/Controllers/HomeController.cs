using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cryptids.Web.Data;
using Cryptids.Web.Models;

namespace Cryptids.Web.Controllers;

public class HomeController : Controller
{
    // Week 8: the home page features a record, so this controller needs the
    // context too. Same move as CryptidsController — ask, and it's handed over.
    private readonly CryptidContext _context;

    public HomeController(CryptidContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // One random record, chosen by the database: this becomes
        // ORDER BY NEWID() — one row comes back, not the whole table.
        // The query lives HERE, not in the view. Views render; controllers ask.
        var featured = await _context.Cryptids
            .OrderBy(c => Guid.NewGuid())
            .FirstOrDefaultAsync();

        return View(featured);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
