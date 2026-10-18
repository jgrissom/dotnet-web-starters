using Microsoft.EntityFrameworkCore;
using Cryptids.Web.Models;

namespace Cryptids.Web.Data;

// The database, as far as the rest of the app is concerned.
// One DbSet per table; the class itself is the connection to SQL Server.
public class CryptidContext : DbContext
{
    // The options — including which database and where — are handed in by
    // Program.cs. The context never decides that for itself.
    public CryptidContext(DbContextOptions<CryptidContext> options) : base(options)
    {
    }

    // This one property is the Cryptids table. Querying it is querying SQL Server.
    public DbSet<Cryptid> Cryptids => Set<Cryptid>();

    // Week 9: the second table. The relationship lives in the model classes,
    // not here — this line only says "there is a Sightings table."
    public DbSet<Sighting> Sightings => Set<Sighting>();

    // The seed data. It used to be a static List<Cryptid> in CryptidData.cs;
    // now it belongs to the model, so a migration carries it into the table.
    // Week 8: the six archive records gain Latin names and plate images. The
    // migration that follows this edit is an UPDATE, not an insert — EF Core
    // works out the difference from the last snapshot.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cryptid>().HasData(
            new Cryptid { Id = 1, Name = "The Hodag", Region = "Rhinelander, Wisconsin", FirstSighting = 1893, IsDebunked = true, LatinName = "Bovine spiritus", ImageUrl = "/img/cryptids/hodag.webp" },
            new Cryptid { Id = 2, Name = "Bigfoot", Region = "Pacific Northwest", FirstSighting = 1958, IsDebunked = false, LatinName = "Gigantopithecus canadensis", ImageUrl = "/img/cryptids/bigfoot.webp" },
            new Cryptid { Id = 3, Name = "Mothman", Region = "Point Pleasant, WV", FirstSighting = 1966, IsDebunked = false, LatinName = "Noctua pontiensis", ImageUrl = "/img/cryptids/mothman.webp" },
            new Cryptid { Id = 4, Name = "The Loch Ness Monster", Region = "Loch Ness, Scotland", FirstSighting = 565, IsDebunked = false, LatinName = "Nessiteras rhombopteryx", ImageUrl = "/img/cryptids/lochness.webp" },
            new Cryptid { Id = 5, Name = "The Jersey Devil", Region = "Pine Barrens, NJ", FirstSighting = 1735, IsDebunked = false, LatinName = "Diabolus pinorum", ImageUrl = "/img/cryptids/jerseydevil.webp" },
            new Cryptid { Id = 6, Name = "Chupacabra", Region = "Puerto Rico", FirstSighting = 1995, IsDebunked = true, LatinName = "Caprivorus portoricensis", ImageUrl = "/img/cryptids/chupacabra.webp" }
        );

        // Week 9: the accounts themselves. "Reports on file" used to say 47 for
        // the Hodag because someone typed 47. Fourteen rows is what the archive
        // can actually show you, and that is the honest number.
        modelBuilder.Entity<Sighting>().HasData(
            new Sighting { Id = 1, CryptidId = 1, Witness = "Eugene Shepard", Location = "Rhinelander, Wisconsin", SeenOn = new DateTime(1893, 10, 3), Account = "Horned, thick with muscle, and it smelled of green wood smoke. It went into the tamarack swamp and did not come out." },
            new Sighting { Id = 2, CryptidId = 1, Witness = "M. Krueger", Location = "Pelican Lake, Wisconsin", SeenOn = new DateTime(1952, 7, 19), Account = "Tracks in the mud at the boat landing, too wide for a bear and too deep for a deer." },
            new Sighting { Id = 3, CryptidId = 1, Witness = "Dolores Ambrose", Location = "Hodag Park, Wisconsin", SeenOn = new DateTime(2011, 6, 2), Account = "Something moved along the treeline while we packed the car. My brother saw it too and will not talk about it." },
            new Sighting { Id = 4, CryptidId = 2, Witness = "Jerry Crew", Location = "Bluff Creek, California", SeenOn = new DateTime(1958, 8, 27), Account = "Prints around the equipment, sixteen inches, walking a straight line across ground no man had crossed." },
            new Sighting { Id = 5, CryptidId = 2, Witness = "R. Patterson", Location = "Six Rivers, California", SeenOn = new DateTime(1967, 10, 20), Account = "It turned to look at us as it walked. It did not hurry, and that is the part I think about." },
            new Sighting { Id = 6, CryptidId = 2, Witness = "Anonymous", Location = "Chequamegon Forest, Wisconsin", SeenOn = new DateTime(2019, 11, 4), Account = "Bow season, first light. Something upright crossed the ridge two hundred yards out and I stopped hunting for the day." },
            new Sighting { Id = 7, CryptidId = 2, Witness = "T. Nakamura", Location = "Olympic Peninsula, Washington", SeenOn = new DateTime(2023, 3, 15), Account = "Two knocks on wood, spaced evenly, answered from further up the valley. No wind that night." },
            new Sighting { Id = 8, CryptidId = 3, Witness = "Linda Scarberry", Location = "Point Pleasant, West Virginia", SeenOn = new DateTime(1966, 11, 15), Account = "Grey, tall as a man, with wings folded behind. The eyes threw back the headlights like a deer's, but red." },
            new Sighting { Id = 9, CryptidId = 3, Witness = "Newell Partridge", Location = "Salem, West Virginia", SeenOn = new DateTime(1966, 11, 12), Account = "The television went to herringbone and the dog would not stop barking at the hay barn. The dog was gone by morning." },
            new Sighting { Id = 10, CryptidId = 4, Witness = "Aldie Mackay", Location = "Loch Ness, Scotland", SeenOn = new DateTime(1933, 5, 2), Account = "A great disturbance in the water off Abriachan, and something rolling and plunging in it for a full minute." },
            new Sighting { Id = 11, CryptidId = 4, Witness = "Arthur Grant", Location = "Abriachan, Scotland", SeenOn = new DateTime(1934, 1, 5), Account = "It crossed the road in front of the motorcycle in two bounds and went down the bank into the water." },
            new Sighting { Id = 12, CryptidId = 4, Witness = "H. Lindqvist", Location = "Fort Augustus, Scotland", SeenOn = new DateTime(2018, 8, 11), Account = "A wake with no boat in front of it, tracking north for perhaps thirty seconds before it flattened out." },
            new Sighting { Id = 13, CryptidId = 5, Witness = "Nelson Evans", Location = "Gloucester, New Jersey", SeenOn = new DateTime(1909, 1, 19), Account = "It stood on the shed roof, dog-faced and long-legged, then went off over the fence without touching it." },
            new Sighting { Id = 14, CryptidId = 6, Witness = "Madelyne Tolentino", Location = "Canóvanas, Puerto Rico", SeenOn = new DateTime(1995, 8, 11), Account = "Upright, spines down the back, and it hopped rather than ran. The goats were found the following morning." }
        );
    }
}
