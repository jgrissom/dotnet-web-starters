using Microsoft.AspNetCore.Mvc.Rendering;
using Cryptids.Web.Models;

namespace Cryptids.Web.ViewModels;

// Week 9. Not an entity: it has no table and it is never in the DbContext,
// which is why it lives in its own folder. It exists because this one view
// needs two things at once — the report being written, and the list of
// creatures to attach it to.
public class SightingFormViewModel
{
    public Sighting Sighting { get; set; } = new();

    // ⚠️ The "?" is load-bearing, and week 6 explains why: ASP.NET treats a
    // non-nullable property as a required field. This list is never posted
    // back — it is the choices, not the answer — so without the "?" every
    // report is rejected with "The Cryptids field is required", pointing at
    // a dropdown that plainly has a creature selected.
    public SelectList? Cryptids { get; set; }
}
