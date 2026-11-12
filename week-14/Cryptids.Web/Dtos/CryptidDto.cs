namespace Cryptids.Web.Dtos;

// What /api/cryptids says about each creature: the facts, and how many reports
// are on file. The reports themselves are on the creature's own address.
public class CryptidDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Region { get; set; } = "";
    public int FirstSighting { get; set; }
    public bool IsDebunked { get; set; }
    public string? LatinName { get; set; }
    public int ReportCount { get; set; }
}
