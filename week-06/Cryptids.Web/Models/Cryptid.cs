namespace Cryptids.Web.Models;

public class Cryptid
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Region { get; set; } = "";
    public int FirstSighting { get; set; }
    public int Sightings { get; set; }
    public bool IsDebunked { get; set; }
}
