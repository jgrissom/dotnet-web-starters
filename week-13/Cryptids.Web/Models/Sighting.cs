using System.ComponentModel.DataAnnotations;

namespace Cryptids.Web.Models;

// Week 9: the dependent. Week 2's report.html, made real — who saw it, which
// creature, and what they saw. A sighting cannot exist without a cryptid,
// which is exactly what the int (rather than int?) on CryptidId says.
public class Sighting
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Who is reporting this?")]
    [Display(Name = "Reported by")]
    [StringLength(60, MinimumLength = 2)]
    public string Witness { get; set; } = "";

    [Required(ErrorMessage = "Where did you see it?")]
    [StringLength(80)]
    public string Location { get; set; } = "";

    [Display(Name = "Seen on")]
    [DataType(DataType.Date)]
    public DateTime SeenOn { get; set; }

    [Required(ErrorMessage = "Say what you saw.")]
    [Display(Name = "What you saw")]
    [StringLength(400, MinimumLength = 10, ErrorMessage = "Give us at least {2} characters.")]
    public string Account { get; set; } = "";

    // The foreign key — the column that actually exists in the table.
    [Display(Name = "Creature")]
    public int CryptidId { get; set; }

    // The navigation property — not a column. EF Core fills it in when asked,
    // and leaves it null when it isn't.
    public Cryptid? Cryptid { get; set; }
}
