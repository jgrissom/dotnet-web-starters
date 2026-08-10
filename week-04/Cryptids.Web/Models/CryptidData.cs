namespace Cryptids.Web.Models;

// The field-report archive, hard-coded for now. In week 7 this becomes a
// database table and this file goes away — the controller barely changes.
public static class CryptidData
{
    public static List<Cryptid> All { get; } = new()
    {
        new Cryptid { Id = 1, Name = "The Hodag",         Region = "Rhinelander, Wisconsin", FirstSighting = 1893, Sightings = 47,   IsDebunked = true  },
        new Cryptid { Id = 2, Name = "Bigfoot",           Region = "Pacific Northwest",      FirstSighting = 1958, Sightings = 1204, IsDebunked = false },
        new Cryptid { Id = 3, Name = "Mothman",           Region = "Point Pleasant, WV",     FirstSighting = 1966, Sightings = 102,  IsDebunked = false },
        new Cryptid { Id = 4, Name = "The Loch Ness Monster", Region = "Loch Ness, Scotland", FirstSighting = 565, Sightings = 1131, IsDebunked = false },
        new Cryptid { Id = 5, Name = "The Jersey Devil",  Region = "Pine Barrens, NJ",       FirstSighting = 1735, Sightings = 287,  IsDebunked = false },
        new Cryptid { Id = 6, Name = "Chupacabra",        Region = "Puerto Rico",            FirstSighting = 1995, Sightings = 214,  IsDebunked = true  },
    };
}
