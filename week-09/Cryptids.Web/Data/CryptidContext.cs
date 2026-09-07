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

    // The seed data. It used to be a static List<Cryptid> in CryptidData.cs;
    // now it belongs to the model, so a migration carries it into the table.
    // Week 8: the six archive records gain Latin names and plate images. The
    // migration that follows this edit is an UPDATE, not an insert — EF Core
    // works out the difference from the last snapshot.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cryptid>().HasData(
            new Cryptid { Id = 1, Name = "The Hodag", Region = "Rhinelander, Wisconsin", FirstSighting = 1893, Sightings = 47, IsDebunked = true, LatinName = "Bovine spiritus", ImageUrl = "/img/cryptids/hodag.webp" },
            new Cryptid { Id = 2, Name = "Bigfoot", Region = "Pacific Northwest", FirstSighting = 1958, Sightings = 1204, IsDebunked = false, LatinName = "Gigantopithecus canadensis", ImageUrl = "/img/cryptids/bigfoot.webp" },
            new Cryptid { Id = 3, Name = "Mothman", Region = "Point Pleasant, WV", FirstSighting = 1966, Sightings = 102, IsDebunked = false, LatinName = "Noctua pontiensis", ImageUrl = "/img/cryptids/mothman.webp" },
            new Cryptid { Id = 4, Name = "The Loch Ness Monster", Region = "Loch Ness, Scotland", FirstSighting = 565, Sightings = 1131, IsDebunked = false, LatinName = "Nessiteras rhombopteryx", ImageUrl = "/img/cryptids/lochness.webp" },
            new Cryptid { Id = 5, Name = "The Jersey Devil", Region = "Pine Barrens, NJ", FirstSighting = 1735, Sightings = 287, IsDebunked = false, LatinName = "Diabolus pinorum", ImageUrl = "/img/cryptids/jerseydevil.webp" },
            new Cryptid { Id = 6, Name = "Chupacabra", Region = "Puerto Rico", FirstSighting = 1995, Sightings = 214, IsDebunked = true, LatinName = "Caprivorus portoricensis", ImageUrl = "/img/cryptids/chupacabra.webp" }
        );
    }
}
