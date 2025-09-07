using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentConnect.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ticket_Property_PropertyId",
                schema: "dbo",
                table: "Ticket");

            migrationBuilder.DropForeignKey(
                name: "FK_Ticket_Tenant_TenantId",
                schema: "dbo",
                table: "Ticket");

            migrationBuilder.DropColumn(
                name: "DateModified",
                schema: "dbo",
                table: "TicketStatus");

            migrationBuilder.DropColumn(
                name: "IsFurnished",
                schema: "dbo",
                table: "Property");

            migrationBuilder.AlterColumn<long>(
                name: "TicketId",
                schema: "dbo",
                table: "TicketStatus",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                schema: "dbo",
                table: "TicketStatus",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateCreated",
                schema: "dbo",
                table: "TicketStatus",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                schema: "dbo",
                table: "TicketStatus",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<long>(
                name: "AddedBy",
                schema: "dbo",
                table: "TicketStatus",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<string>(
                name: "AddedByName",
                schema: "dbo",
                table: "TicketStatus",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AddedByType",
                schema: "dbo",
                table: "TicketStatus",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "TenantId",
                schema: "dbo",
                table: "Ticket",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<string>(
                name: "TenantGroupId",
                schema: "dbo",
                table: "Ticket",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "PropertyId",
                schema: "dbo",
                table: "Ticket",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "LandlordId",
                schema: "dbo",
                table: "Ticket",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "dbo",
                table: "Ticket",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateCreated",
                schema: "dbo",
                table: "Ticket",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "Category",
                schema: "dbo",
                table: "Ticket",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<long>(
                name: "AssignedTo",
                schema: "dbo",
                table: "Ticket",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CreatedBy",
                schema: "dbo",
                table: "Ticket",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByType",
                schema: "dbo",
                table: "Ticket",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentStatus",
                schema: "dbo",
                table: "Ticket",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateModified",
                schema: "dbo",
                table: "Ticket",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateResolved",
                schema: "dbo",
                table: "Ticket",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                schema: "dbo",
                table: "Ticket",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TicketNumber",
                schema: "dbo",
                table: "Ticket",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                schema: "dbo",
                table: "Ticket",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TicketComment",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TicketId = table.Column<long>(type: "bigint", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AddedBy = table.Column<long>(type: "bigint", nullable: true),
                    AddedByName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AddedByType = table.Column<int>(type: "int", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    BaseVersionId = table.Column<int>(type: "int", nullable: true),
                    VersionId = table.Column<int>(type: "int", nullable: true),
                    IsLatestVersion = table.Column<bool>(type: "bit", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketComment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketComment_Ticket_TicketId",
                        column: x => x.TicketId,
                        principalSchema: "dbo",
                        principalTable: "Ticket",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TicketAttachment",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommentId = table.Column<long>(type: "bigint", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    FileType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UploadedBy = table.Column<long>(type: "bigint", nullable: true),
                    DateUploaded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    BaseVersionId = table.Column<int>(type: "int", nullable: true),
                    VersionId = table.Column<int>(type: "int", nullable: true),
                    IsLatestVersion = table.Column<bool>(type: "bit", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketAttachment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketAttachment_TicketComment_CommentId",
                        column: x => x.CommentId,
                        principalSchema: "dbo",
                        principalTable: "TicketComment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketAttachment_CommentId",
                schema: "dbo",
                table: "TicketAttachment",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketComment_TicketId",
                schema: "dbo",
                table: "TicketComment",
                column: "TicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ticket_Property_PropertyId",
                schema: "dbo",
                table: "Ticket",
                column: "PropertyId",
                principalSchema: "dbo",
                principalTable: "Property",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ticket_Tenant_TenantId",
                schema: "dbo",
                table: "Ticket",
                column: "TenantId",
                principalSchema: "dbo",
                principalTable: "Tenant",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ticket_Property_PropertyId",
                schema: "dbo",
                table: "Ticket");

            migrationBuilder.DropForeignKey(
                name: "FK_Ticket_Tenant_TenantId",
                schema: "dbo",
                table: "Ticket");

            migrationBuilder.DropTable(
                name: "TicketAttachment",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "TicketComment",
                schema: "dbo");

            migrationBuilder.DropColumn(
                name: "AddedByName",
                schema: "dbo",
                table: "TicketStatus");

            migrationBuilder.DropColumn(
                name: "AddedByType",
                schema: "dbo",
                table: "TicketStatus");

            migrationBuilder.DropColumn(
                name: "AssignedTo",
                schema: "dbo",
                table: "Ticket");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "dbo",
                table: "Ticket");

            migrationBuilder.DropColumn(
                name: "CreatedByType",
                schema: "dbo",
                table: "Ticket");

            migrationBuilder.DropColumn(
                name: "CurrentStatus",
                schema: "dbo",
                table: "Ticket");

            migrationBuilder.DropColumn(
                name: "DateModified",
                schema: "dbo",
                table: "Ticket");

            migrationBuilder.DropColumn(
                name: "DateResolved",
                schema: "dbo",
                table: "Ticket");

            migrationBuilder.DropColumn(
                name: "Priority",
                schema: "dbo",
                table: "Ticket");

            migrationBuilder.DropColumn(
                name: "TicketNumber",
                schema: "dbo",
                table: "Ticket");

            migrationBuilder.DropColumn(
                name: "Title",
                schema: "dbo",
                table: "Ticket");

            migrationBuilder.AlterColumn<long>(
                name: "TicketId",
                schema: "dbo",
                table: "TicketStatus",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "dbo",
                table: "TicketStatus",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateCreated",
                schema: "dbo",
                table: "TicketStatus",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                schema: "dbo",
                table: "TicketStatus",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "AddedBy",
                schema: "dbo",
                table: "TicketStatus",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateModified",
                schema: "dbo",
                table: "TicketStatus",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "TenantId",
                schema: "dbo",
                table: "Ticket",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "TenantGroupId",
                schema: "dbo",
                table: "Ticket",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "PropertyId",
                schema: "dbo",
                table: "Ticket",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "LandlordId",
                schema: "dbo",
                table: "Ticket",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "dbo",
                table: "Ticket",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateCreated",
                schema: "dbo",
                table: "Ticket",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                schema: "dbo",
                table: "Ticket",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFurnished",
                schema: "dbo",
                table: "Property",
                type: "bit",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Ticket_Property_PropertyId",
                schema: "dbo",
                table: "Ticket",
                column: "PropertyId",
                principalSchema: "dbo",
                principalTable: "Property",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Ticket_Tenant_TenantId",
                schema: "dbo",
                table: "Ticket",
                column: "TenantId",
                principalSchema: "dbo",
                principalTable: "Tenant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
