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
}
