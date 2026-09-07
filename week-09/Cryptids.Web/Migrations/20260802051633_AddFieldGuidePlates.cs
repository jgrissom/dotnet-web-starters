using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cryptids.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldGuidePlates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Cryptids",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LatinName",
                table: "Cryptids",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Cryptids",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ImageUrl", "LatinName" },
                values: new object[] { "/img/cryptids/hodag.webp", "Bovine spiritus" });

            migrationBuilder.UpdateData(
                table: "Cryptids",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ImageUrl", "LatinName" },
                values: new object[] { "/img/cryptids/bigfoot.webp", "Gigantopithecus canadensis" });

            migrationBuilder.UpdateData(
                table: "Cryptids",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ImageUrl", "LatinName" },
                values: new object[] { "/img/cryptids/mothman.webp", "Noctua pontiensis" });

            migrationBuilder.UpdateData(
                table: "Cryptids",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "ImageUrl", "LatinName" },
                values: new object[] { "/img/cryptids/lochness.webp", "Nessiteras rhombopteryx" });

            migrationBuilder.UpdateData(
                table: "Cryptids",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "ImageUrl", "LatinName" },
                values: new object[] { "/img/cryptids/jerseydevil.webp", "Diabolus pinorum" });

            migrationBuilder.UpdateData(
                table: "Cryptids",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "ImageUrl", "LatinName" },
                values: new object[] { "/img/cryptids/chupacabra.webp", "Caprivorus portoricensis" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Cryptids");

            migrationBuilder.DropColumn(
                name: "LatinName",
                table: "Cryptids");
        }
    }
}
