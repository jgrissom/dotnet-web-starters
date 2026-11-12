using System.ComponentModel.DataAnnotations;

namespace Cryptids.Web.Dtos;

// What a curator may change through the API. No Id, no reports, and no plate:
// a PUT leaves the creature's image exactly where it is.
public class CryptidInput
{
    [Required(ErrorMessage = "Every creature needs a name.")]
    [StringLength(60, MinimumLength = 2)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Where was it seen?")]
    [StringLength(80)]
    public string Region { get; set; } = "";

    [Range(500, 2026, ErrorMessage = "First sighted has to be a year between {1} and {2}.")]
    public int FirstSighting { get; set; }

    public bool IsDebunked { get; set; }

    [StringLength(80)]
    public string? LatinName { get; set; }
}
