namespace Cryptids.Web.Dtos;

// One report, as the API says it. It names its creature with a number, so
// there is no Cryptid object here to lead back to the creature's reports.
public class SightingDto
{
    public int Id { get; set; }
    public int CryptidId { get; set; }
    public string Witness { get; set; } = "";
    public string Location { get; set; } = "";
    public DateTime SeenOn { get; set; }
    public string Account { get; set; } = "";
}
