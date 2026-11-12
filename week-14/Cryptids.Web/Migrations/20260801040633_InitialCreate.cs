using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Cryptids.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cryptids",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Region = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    FirstSighting = table.Column<int>(type: "int", nullable: false),
                    Sightings = table.Column<int>(type: "int", nullable: false),
                    IsDebunked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cryptids", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Cryptids",
                columns: new[] { "Id", "FirstSighting", "IsDebunked", "Name", "Region", "Sightings" },
                values: new object[,]
                {
                    { 1, 1893, true, "The Hodag", "Rhinelander, Wisconsin", 47 },
                    { 2, 1958, false, "Bigfoot", "Pacific Northwest", 1204 },
                    { 3, 1966, false, "Mothman", "Point Pleasant, WV", 102 },
                    { 4, 565, false, "The Loch Ness Monster", "Loch Ness, Scotland", 1131 },
                    { 5, 1735, false, "The Jersey Devil", "Pine Barrens, NJ", 287 },
                    { 6, 1995, true, "Chupacabra", "Puerto Rico", 214 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cryptids");
        }
    }
}
