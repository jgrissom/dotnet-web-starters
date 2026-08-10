using System.ComponentModel.DataAnnotations;

namespace Cryptids.Web.Models;

public class Cryptid
{
    // Not on the form — the controller assigns it. Nothing to validate.
    public int Id { get; set; }

    [Required(ErrorMessage = "Every creature needs a name.")]
    [StringLength(60, MinimumLength = 2)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Where was it seen?")]
    [StringLength(80)]
    public string Region { get; set; } = "";

    [Display(Name = "First sighted")]
    [Range(500, 2026, ErrorMessage = "First sighted has to be a year between {1} and {2}.")]
    public int FirstSighting { get; set; }

    [Display(Name = "Reports on file")]
    [Range(0, 100000)]
    public int Sightings { get; set; }

    [Display(Name = "Already debunked?")]
    public bool IsDebunked { get; set; }
}
