using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentConnect.API.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "TenantId",
                schema: "dbo",
                table: "TenantChildren",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_TenantChildren_TenantId",
                schema: "dbo",
                table: "TenantChildren",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_TenantChildren_Tenant_TenantId",
                schema: "dbo",
                table: "TenantChildren",
                column: "TenantId",
                principalSchema: "dbo",
                principalTable: "Tenant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TenantChildren_Tenant_TenantId",
                schema: "dbo",
                table: "TenantChildren");

            migrationBuilder.DropIndex(
                name: "IX_TenantChildren_TenantId",
                schema: "dbo",
                table: "TenantChildren");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "dbo",
                table: "TenantChildren");
        }
    }
}
