using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentConnect.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantChildren",
                schema: "dbo");

            migrationBuilder.AddColumn<bool>(
                name: "IncludeInEmail",
                schema: "dbo",
                table: "Tenant",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Relationship",
                schema: "dbo",
                table: "Tenant",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ApplicationUserId",
                schema: "dbo",
                table: "Landlord",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IncludeInEmail",
                schema: "dbo",
                table: "Tenant");

            migrationBuilder.DropColumn(
                name: "Relationship",
                schema: "dbo",
                table: "Tenant");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                schema: "dbo",
                table: "Landlord");

            migrationBuilder.CreateTable(
                name: "TenantChildren",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    DOB = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Occupation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantGroupId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantChildren", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantChildren_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "dbo",
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantChildren_TenantId",
                schema: "dbo",
                table: "TenantChildren",
                column: "TenantId");
        }
    }
}
