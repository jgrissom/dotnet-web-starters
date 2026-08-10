using Microsoft.AspNetCore.Mvc;
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
        // Was CryptidData.All. ToList() is where the SELECT actually runs.
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
    // No model passed: the inputs come up blank instead of pre-filled with 0.
    public IActionResult Create()
    {
        return View();
    }

    // POST /Cryptids/Create — the filled-in form arrives back here.
    // Everything above the Add is exactly what week 6 wrote.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Cryptid cryptid)
    {
        if (!ModelState.IsValid)
        {
            return View(cryptid);           // back to the form — their input, plus the errors
        }

        // No id assignment any more: Id is an IDENTITY column, so SQL Server picks
        // the next number and EF Core reads it back onto the object.
        _context.Cryptids.Add(cryptid);     // remembered, not saved
        _context.SaveChanges();             // nothing reaches the database until this line

        // Redirect, don't render: a rendered POST re-submits when they refresh.
        return RedirectToAction(nameof(Index));
    }
}
