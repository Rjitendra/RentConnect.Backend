using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentConnect.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate345 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedDate",
                schema: "dbo",
                table: "TicketStatus",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<DateTime>(
                name: "AddedDate",
                schema: "dbo",
                table: "TicketStatus",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedDate",
                schema: "dbo",
                table: "TicketComment",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<DateTime>(
                name: "AddedDate",
                schema: "dbo",
                table: "TicketComment",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedDate",
                schema: "dbo",
                table: "TicketAttachment",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "AddedBy",
                schema: "dbo",
                table: "TicketAttachment",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AddedDate",
                schema: "dbo",
                table: "TicketAttachment",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedDate",
                schema: "dbo",
                table: "Ticket",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "AddedBy",
                schema: "dbo",
                table: "Ticket",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AddedDate",
                schema: "dbo",
                table: "Ticket",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedDate",
                schema: "dbo",
                table: "Tenant",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "AddedBy",
                schema: "dbo",
                table: "Tenant",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AddedDate",
                schema: "dbo",
                table: "Tenant",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedDate",
                schema: "dbo",
                table: "Property",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "AddedBy",
                schema: "dbo",
                table: "Property",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AddedDate",
                schema: "dbo",
                table: "Property",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedDate",
                schema: "dbo",
                table: "Landlord",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "AddedBy",
                schema: "dbo",
                table: "Landlord",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AddedDate",
                schema: "dbo",
                table: "Landlord",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedDate",
                schema: "dbo",
                table: "Document",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "AddedBy",
                schema: "dbo",
                table: "Document",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AddedDate",
                schema: "dbo",
                table: "Document",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UploadContext",
                schema: "dbo",
                table: "Document",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddedDate",
                schema: "dbo",
                table: "TicketStatus");

            migrationBuilder.DropColumn(
                name: "AddedDate",
                schema: "dbo",
                table: "TicketComment");

            migrationBuilder.DropColumn(
                name: "AddedBy",
                schema: "dbo",
                table: "TicketAttachment");

            migrationBuilder.DropColumn(
                name: "AddedDate",
                schema: "dbo",
                table: "TicketAttachment");

            migrationBuilder.DropColumn(
                name: "AddedBy",
                schema: "dbo",
                table: "Ticket");

            migrationBuilder.DropColumn(
                name: "AddedDate",
                schema: "dbo",
                table: "Ticket");

            migrationBuilder.DropColumn(
                name: "AddedBy",
                schema: "dbo",
                table: "Tenant");

            migrationBuilder.DropColumn(
                name: "AddedDate",
                schema: "dbo",
                table: "Tenant");

            migrationBuilder.DropColumn(
                name: "AddedBy",
                schema: "dbo",
                table: "Property");

            migrationBuilder.DropColumn(
                name: "AddedDate",
                schema: "dbo",
                table: "Property");

            migrationBuilder.DropColumn(
                name: "AddedBy",
                schema: "dbo",
                table: "Landlord");

            migrationBuilder.DropColumn(
                name: "AddedDate",
                schema: "dbo",
                table: "Landlord");

            migrationBuilder.DropColumn(
                name: "AddedBy",
                schema: "dbo",
                table: "Document");

            migrationBuilder.DropColumn(
                name: "AddedDate",
                schema: "dbo",
                table: "Document");

            migrationBuilder.DropColumn(
                name: "UploadContext",
                schema: "dbo",
                table: "Document");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedDate",
                schema: "dbo",
                table: "TicketStatus",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedDate",
                schema: "dbo",
                table: "TicketComment",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedDate",
                schema: "dbo",
                table: "TicketAttachment",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedDate",
                schema: "dbo",
                table: "Ticket",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedDate",
                schema: "dbo",
                table: "Tenant",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedDate",
                schema: "dbo",
                table: "Property",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedDate",
                schema: "dbo",
                table: "Landlord",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedDate",
                schema: "dbo",
                table: "Document",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
