using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cryptids.Web.Data;
using Cryptids.Web.Models;

namespace Cryptids.Web.Controllers;

public class CryptidsController : Controller
{
    // The context arrives in the constructor. Nothing in this class ever
    // creates one, or knows where the database is.
    private readonly CryptidContext _context;

    public CryptidsController(CryptidContext context)
    {
        _context = context;
    }

    // GET /Cryptids
    public IActionResult Index()
    {
        return View(_context.Cryptids.ToList());
    }

    // GET /Cryptids/Details/2  — the 2 lands in `id` via the route's {id?} segment
    public IActionResult Details(int id)
    {
        var cryptid = _context.Cryptids.FirstOrDefault(c => c.Id == id);

        if (cryptid == null)
        {
            return NotFound();              // no such creature → honest 404
        }

        return View(cryptid);               // one creature goes to the view
    }

    // GET /Cryptids/Create — hand the browser an empty form.
    public IActionResult Create()
    {
        return View();
    }

    // POST /Cryptids/Create — unchanged since week 7.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Cryptid cryptid)
    {
        if (!ModelState.IsValid)
        {
            return View(cryptid);           // back to the form — their input, plus the errors
        }

        _context.Cryptids.Add(cryptid);     // remembered, not saved
        _context.SaveChanges();             // nothing reaches the database until this line

        return RedirectToAction(nameof(Index));
    }

    // ── Ported from the scaffold, week 8 ──────────────────────────────────
    // Everything below started life in CryptidsScaffoldController. It's async
    // because the scaffolder writes async, and this is the week that arrives.

    // GET /Cryptids/Edit/3 — the form, pre-filled with what's on file.
    // `int? id` is the scaffolder being defensive: /Cryptids/Edit with no
    // number at all binds null, and null gets an honest 404.
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var cryptid = await _context.Cryptids.FindAsync(id);
        if (cryptid == null)
        {
            return NotFound();
        }
        return View(cryptid);
    }

    // POST /Cryptids/Edit/3 — the corrected record comes back.
    // The [Bind] list is a guest list: only these properties are read out of
    // the form. Add a property to the model and its name goes here too, or
    // the new field is dropped silently.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Region,FirstSighting,Sightings,IsDebunked,LatinName,ImageUrl")] Cryptid cryptid)
    {
        if (id != cryptid.Id)
        {
            return NotFound();              // the URL and the form disagree about which record
        }

        if (!ModelState.IsValid)
        {
            return View(cryptid);           // same guard as Create, same reason
        }

        try
        {
            _context.Update(cryptid);       // mark the whole record modified
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // The UPDATE matched no row: someone closed this file while the
            // form was open. A 404 is the honest answer; anything else rethrows.
            if (!CryptidExists(cryptid.Id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }
        return RedirectToAction(nameof(Index));
    }

    // GET /Cryptids/Delete/5 — show what's about to go, and ask first.
    // A GET must never change data; this one only shows the confirmation page.
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var cryptid = await _context.Cryptids.FirstOrDefaultAsync(c => c.Id == id);
        if (cryptid == null)
        {
            return NotFound();
        }

        return View(cryptid);
    }

    // POST /Cryptids/Delete/5 — the actual deletion.
    // Two actions can't share a name and a signature, so the POST is called
    // DeleteConfirmed — and [ActionName] keeps its URL at /Cryptids/Delete.
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var cryptid = await _context.Cryptids.FindAsync(id);
        if (cryptid != null)
        {
            _context.Cryptids.Remove(cryptid);  // remembered, not deleted
        }

        await _context.SaveChangesAsync();      // the DELETE runs here
        return RedirectToAction(nameof(Index));
    }

    private bool CryptidExists(int id)
    {
        return _context.Cryptids.Any(e => e.Id == id);
    }
}
