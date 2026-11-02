using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Cryptids.Web.Data;
using Cryptids.Web.Models;
using Cryptids.Web.ViewModels;

namespace Cryptids.Web.Controllers;

public class SightingsController : Controller
{
    private readonly CryptidContext _context;

    public SightingsController(CryptidContext context)
    {
        _context = context;
    }

    // GET /Sightings/Create?cryptidId=3
    // Arriving from a creature's page pre-picks it; arriving from the nav
    // leaves the dropdown to the reporter.
    public IActionResult Create(int? cryptidId)
    {
        var form = new SightingFormViewModel
        {
            Sighting = new Sighting { CryptidId = cryptidId ?? 0, SeenOn = DateTime.Today },
            Cryptids = CryptidChoices(cryptidId)
        };

        return View(form);
    }

    // POST /Sightings/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SightingFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            // The dropdown was never form data, so nothing posted it back.
            // Rebuild it or the redisplayed form comes back with an empty list.
            form.Cryptids = CryptidChoices(form.Sighting.CryptidId);
            return View(form);
        }

        _context.Sightings.Add(form.Sighting);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", "Cryptids", new { id = form.Sighting.CryptidId });
    }

    // A SelectList is three things: the rows, the property that becomes the
    // value, and the property a human reads.
    private SelectList CryptidChoices(int? selected) =>
        new SelectList(_context.Cryptids.OrderBy(c => c.Name).ToList(), "Id", "Name", selected);
}
