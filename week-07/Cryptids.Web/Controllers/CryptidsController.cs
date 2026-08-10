using Microsoft.AspNetCore.Mvc;
using Cryptids.Web.Models;

namespace Cryptids.Web.Controllers;

public class CryptidsController : Controller
{
    // GET /Cryptids
    public IActionResult Index()
    {
        return View(CryptidData.All);       // the whole archive goes to the view
    }

    // GET /Cryptids/Details/2  — the 2 lands in `id` via the route's {id?} segment
    public IActionResult Details(int id)
    {
        var cryptid = CryptidData.All.FirstOrDefault(c => c.Id == id);

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
    // Same action name, different verb; [HttpPost] is what tells them apart.
    // `cryptid` is assembled by model binding, from the inputs' name attributes.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Cryptid cryptid)
    {
        if (!ModelState.IsValid)
        {
            return View(cryptid);           // back to the form — their input, plus the errors
        }

        cryptid.Id = CryptidData.All.Max(c => c.Id) + 1;
        CryptidData.All.Add(cryptid);

        // Redirect, don't render: a rendered POST re-submits when they refresh.
        return RedirectToAction(nameof(Index));
    }
}
