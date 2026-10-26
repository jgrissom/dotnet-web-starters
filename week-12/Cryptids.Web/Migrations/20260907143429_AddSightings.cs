using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Cryptids.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSightings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Sightings",
                table: "Cryptids");

            migrationBuilder.CreateTable(
                name: "Sightings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Witness = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SeenOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Account = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    CryptidId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sightings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sightings_Cryptids_CryptidId",
                        column: x => x.CryptidId,
                        principalTable: "Cryptids",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Sightings",
                columns: new[] { "Id", "Account", "CryptidId", "Location", "SeenOn", "Witness" },
                values: new object[,]
                {
                    { 1, "Horned, thick with muscle, and it smelled of green wood smoke. It went into the tamarack swamp and did not come out.", 1, "Rhinelander, Wisconsin", new DateTime(1893, 10, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "Eugene Shepard" },
                    { 2, "Tracks in the mud at the boat landing, too wide for a bear and too deep for a deer.", 1, "Pelican Lake, Wisconsin", new DateTime(1952, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), "M. Krueger" },
                    { 3, "Something moved along the treeline while we packed the car. My brother saw it too and will not talk about it.", 1, "Hodag Park, Wisconsin", new DateTime(2011, 6, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), "Dolores Ambrose" },
                    { 4, "Prints around the equipment, sixteen inches, walking a straight line across ground no man had crossed.", 2, "Bluff Creek, California", new DateTime(1958, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), "Jerry Crew" },
                    { 5, "It turned to look at us as it walked. It did not hurry, and that is the part I think about.", 2, "Six Rivers, California", new DateTime(1967, 10, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "R. Patterson" },
                    { 6, "Bow season, first light. Something upright crossed the ridge two hundred yards out and I stopped hunting for the day.", 2, "Chequamegon Forest, Wisconsin", new DateTime(2019, 11, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), "Anonymous" },
                    { 7, "Two knocks on wood, spaced evenly, answered from further up the valley. No wind that night.", 2, "Olympic Peninsula, Washington", new DateTime(2023, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "T. Nakamura" },
                    { 8, "Grey, tall as a man, with wings folded behind. The eyes threw back the headlights like a deer's, but red.", 3, "Point Pleasant, West Virginia", new DateTime(1966, 11, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Linda Scarberry" },
                    { 9, "The television went to herringbone and the dog would not stop barking at the hay barn. The dog was gone by morning.", 3, "Salem, West Virginia", new DateTime(1966, 11, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "Newell Partridge" },
                    { 10, "A great disturbance in the water off Abriachan, and something rolling and plunging in it for a full minute.", 4, "Loch Ness, Scotland", new DateTime(1933, 5, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), "Aldie Mackay" },
                    { 11, "It crossed the road in front of the motorcycle in two bounds and went down the bank into the water.", 4, "Abriachan, Scotland", new DateTime(1934, 1, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), "Arthur Grant" },
                    { 12, "A wake with no boat in front of it, tracking north for perhaps thirty seconds before it flattened out.", 4, "Fort Augustus, Scotland", new DateTime(2018, 8, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), "H. Lindqvist" },
                    { 13, "It stood on the shed roof, dog-faced and long-legged, then went off over the fence without touching it.", 5, "Gloucester, New Jersey", new DateTime(1909, 1, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), "Nelson Evans" },
                    { 14, "Upright, spines down the back, and it hopped rather than ran. The goats were found the following morning.", 6, "Canóvanas, Puerto Rico", new DateTime(1995, 8, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), "Madelyne Tolentino" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sightings_CryptidId",
                table: "Sightings",
                column: "CryptidId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Sightings");

            migrationBuilder.AddColumn<int>(
                name: "Sightings",
                table: "Cryptids",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Cryptids",
                keyColumn: "Id",
                keyValue: 1,
                column: "Sightings",
                value: 47);

            migrationBuilder.UpdateData(
                table: "Cryptids",
                keyColumn: "Id",
                keyValue: 2,
                column: "Sightings",
                value: 1204);

            migrationBuilder.UpdateData(
                table: "Cryptids",
                keyColumn: "Id",
                keyValue: 3,
                column: "Sightings",
                value: 102);

            migrationBuilder.UpdateData(
                table: "Cryptids",
                keyColumn: "Id",
                keyValue: 4,
                column: "Sightings",
                value: 1131);

            migrationBuilder.UpdateData(
                table: "Cryptids",
                keyColumn: "Id",
                keyValue: 5,
                column: "Sightings",
                value: 287);

            migrationBuilder.UpdateData(
                table: "Cryptids",
                keyColumn: "Id",
                keyValue: 6,
                column: "Sightings",
                value: 214);
        }
    }
}
