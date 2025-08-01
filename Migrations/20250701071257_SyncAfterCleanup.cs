using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTOM.Migrations
{
    /// <inheritdoc />
    public partial class SyncAfterCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF OBJECT_ID('[dbo].[ReportDataTransactionFields]', 'U') IS NOT NULL DROP TABLE [dbo].[ReportDataTransactionFields];");

            migrationBuilder.Sql("IF OBJECT_ID('[dbo].[ReportDataTransactions]', 'U') IS NOT NULL DROP TABLE [dbo].[ReportDataTransactions];");

            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM sys.columns WHERE Name = N'MappingJson' AND Object_ID = Object_ID(N'[dbo].[Templates]')) ALTER TABLE [dbo].[Templates] DROP COLUMN [MappingJson];");

            migrationBuilder.CreateTable(
                name: "FormDatas",
                columns: table => new
                {
                    FormDataID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateID = table.Column<int>(type: "int", nullable: false),
                    SoCif = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedByUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreationTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationTimestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedDepartmentID = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FormDataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormDatas", x => x.FormDataID);
                    table.ForeignKey(
                        name: "FK_FormDatas_NguoiSuDung_CreatedByUserName",
                        column: x => x.CreatedByUserName,
                        principalTable: "NguoiSuDung",
                        principalColumn: "UserName",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormDatas_PhongBan_CreatedDepartmentID",
                        column: x => x.CreatedDepartmentID,
                        principalTable: "PhongBan",
                        principalColumn: "MaPhong",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormDatas_Templates_TemplateID",
                        column: x => x.TemplateID,
                        principalTable: "Templates",
                        principalColumn: "TemplateID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FormDatas_CreatedByUserName",
                table: "FormDatas",
                column: "CreatedByUserName");

            migrationBuilder.CreateIndex(
                name: "IX_FormDatas_CreatedDepartmentID",
                table: "FormDatas",
                column: "CreatedDepartmentID");

            migrationBuilder.CreateIndex(
                name: "IX_FormDatas_SoCif",
                table: "FormDatas",
                column: "SoCif");

            migrationBuilder.CreateIndex(
                name: "IX_FormDatas_TemplateID",
                table: "FormDatas",
                column: "TemplateID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FormDatas");

            migrationBuilder.AddColumn<string>(
                name: "MappingJson",
                table: "Templates",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReportDataTransactions",
                columns: table => new
                {
                    TransactionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApprovedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TemplateID = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValue: "Draft"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TransactionCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportDataTransactions", x => x.TransactionID);
                    table.ForeignKey(
                        name: "FK_ReportDataTransactions_NguoiSuDung_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "NguoiSuDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportDataTransactions_NguoiSuDung_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "NguoiSuDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportDataTransactions_NguoiSuDung_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "NguoiSuDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportDataTransactions_Templates_TemplateID",
                        column: x => x.TemplateID,
                        principalTable: "Templates",
                        principalColumn: "TemplateID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReportDataTransactionFields",
                columns: table => new
                {
                    TransactionFieldID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TransactionID = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FieldName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FieldValue = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportDataTransactionFields", x => x.TransactionFieldID);
                    table.ForeignKey(
                        name: "FK_ReportDataTransactionFields_NguoiSuDung_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "NguoiSuDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportDataTransactionFields_NguoiSuDung_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "NguoiSuDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportDataTransactionFields_ReportDataTransactions_TransactionID",
                        column: x => x.TransactionID,
                        principalTable: "ReportDataTransactions",
                        principalColumn: "TransactionID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportDataTransactionFields_CreatedBy",
                table: "ReportDataTransactionFields",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDataTransactionFields_TransactionID",
                table: "ReportDataTransactionFields",
                column: "TransactionID");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDataTransactionFields_UpdatedBy",
                table: "ReportDataTransactionFields",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDataTransactions_ApprovedBy",
                table: "ReportDataTransactions",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDataTransactions_CreatedBy",
                table: "ReportDataTransactions",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDataTransactions_TemplateID",
                table: "ReportDataTransactions",
                column: "TemplateID");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDataTransactions_UpdatedBy",
                table: "ReportDataTransactions",
                column: "UpdatedBy");
        }
    }
}
