using System.ComponentModel.DataAnnotations;

namespace Cryptids.Web.Dtos;

// What anyone may send to file a report. The same rules as the Sighting class,
// and no Id: the database picks that.
public class SightingInput
{
    public int CryptidId { get; set; }

    [Required(ErrorMessage = "Who is reporting this?")]
    [StringLength(60, MinimumLength = 2)]
    public string Witness { get; set; } = "";

    [Required(ErrorMessage = "Where did you see it?")]
    [StringLength(80)]
    public string Location { get; set; } = "";

    // Nullable so that [Required] can tell a date nobody sent from a real one.
    // A plain DateTime would quietly become the year 1.
    [Required(ErrorMessage = "When did you see it?")]
    public DateTime? SeenOn { get; set; }

    [Required(ErrorMessage = "Say what you saw.")]
    [StringLength(400, MinimumLength = 10, ErrorMessage = "Give us at least {2} characters.")]
    public string Account { get; set; } = "";
}
