using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentConnect.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyDocumentRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "LandlordId",
                schema: "dbo",
                table: "Document",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PropertyId",
                schema: "dbo",
                table: "Document",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TenantId",
                schema: "dbo",
                table: "Document",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LandlordId",
                schema: "dbo",
                table: "Document");

            migrationBuilder.DropColumn(
                name: "PropertyId",
                schema: "dbo",
                table: "Document");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "dbo",
                table: "Document");
        }
    }
}
