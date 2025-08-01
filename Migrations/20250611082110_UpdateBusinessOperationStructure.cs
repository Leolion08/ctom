using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTOM.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBusinessOperationStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OperationID",
                table: "BusinessOperations",
                newName: "BusinessOperationID");

            migrationBuilder.RenameColumn(
                name: "OperationCode",
                table: "BusinessOperations",
                newName: "ModifiedBy");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "BusinessOperations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "BusinessOperations",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<string>(
                name: "CustomerType",
                table: "BusinessOperations",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "DN");

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "BusinessOperations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentOperationID",
                table: "BusinessOperations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessOperations_OperationName_ParentOperationID_CustomerType",
                table: "BusinessOperations",
                columns: new[] { "OperationName", "ParentOperationID", "CustomerType" },
                unique: true,
                filter: "[ParentOperationID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessOperations_ParentOperationID",
                table: "BusinessOperations",
                column: "ParentOperationID");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessOperations_BusinessOperations_ParentOperationID",
                table: "BusinessOperations",
                column: "ParentOperationID",
                principalTable: "BusinessOperations",
                principalColumn: "BusinessOperationID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusinessOperations_BusinessOperations_ParentOperationID",
                table: "BusinessOperations");

            migrationBuilder.DropIndex(
                name: "IX_BusinessOperations_OperationName_ParentOperationID_CustomerType",
                table: "BusinessOperations");

            migrationBuilder.DropIndex(
                name: "IX_BusinessOperations_ParentOperationID",
                table: "BusinessOperations");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "BusinessOperations");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "BusinessOperations");

            migrationBuilder.DropColumn(
                name: "CustomerType",
                table: "BusinessOperations");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "BusinessOperations");

            migrationBuilder.DropColumn(
                name: "ParentOperationID",
                table: "BusinessOperations");

            migrationBuilder.RenameColumn(
                name: "BusinessOperationID",
                table: "BusinessOperations",
                newName: "OperationID");

            migrationBuilder.RenameColumn(
                name: "ModifiedBy",
                table: "BusinessOperations",
                newName: "OperationCode");
        }
    }
}
