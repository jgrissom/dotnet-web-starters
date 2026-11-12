namespace Cryptids.Web.Dtos;

// What /api/cryptids/1 says: the same facts as the list, and the reports.
public class CryptidDetailsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Region { get; set; } = "";
    public int FirstSighting { get; set; }
    public bool IsDebunked { get; set; }
    public string? LatinName { get; set; }
    public List<SightingDto> Sightings { get; set; } = new();
}
