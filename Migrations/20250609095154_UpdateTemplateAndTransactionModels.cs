using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTOM.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTemplateAndTransactionModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TemplateCode",
                table: "Templates");

            migrationBuilder.RenameColumn(
                name: "FileName",
                table: "Templates",
                newName: "OriginalUploadFileName");

            migrationBuilder.AlterColumn<string>(
                name: "TemplateName",
                table: "Templates",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Templates",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BusinessOperationID",
                table: "Templates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserName",
                table: "Templates",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatedDepartmentID",
                table: "Templates",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreationTimestamp",
                table: "Templates",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModificationTimestamp",
                table: "Templates",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedByUserName",
                table: "Templates",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MappedDocxFilePath",
                table: "Templates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OriginalDocxFilePath",
                table: "Templates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SharingType",
                table: "Templates",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Private");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Templates",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.CreateTable(
                name: "AvailableCifFields",
                columns: table => new
                {
                    FieldID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FieldName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvailableCifFields", x => x.FieldID);
                });

            migrationBuilder.CreateTable(
                name: "BusinessOperations",
                columns: table => new
                {
                    OperationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OperationName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OperationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessOperations", x => x.OperationID);
                });

            migrationBuilder.CreateTable(
                name: "ReportDataTransactions",
                columns: table => new
                {
                    TransactionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateID = table.Column<int>(type: "int", nullable: false),
                    TransactionCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValue: "Draft"),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                name: "TemplateFields",
                columns: table => new
                {
                    TemplateFieldID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateID = table.Column<int>(type: "int", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DefaultValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateFields", x => x.TemplateFieldID);
                    table.ForeignKey(
                        name: "FK_TemplateFields_Templates_TemplateID",
                        column: x => x.TemplateID,
                        principalTable: "Templates",
                        principalColumn: "TemplateID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateSharingRules",
                columns: table => new
                {
                    SharingRuleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateID = table.Column<int>(type: "int", nullable: false),
                    SharingType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TargetID = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TargetName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PermissionLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateSharingRules", x => x.SharingRuleID);
                    table.ForeignKey(
                        name: "FK_TemplateSharingRules_Templates_TemplateID",
                        column: x => x.TemplateID,
                        principalTable: "Templates",
                        principalColumn: "TemplateID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserFavoriteTemplates",
                columns: table => new
                {
                    FavoriteID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TemplateID = table.Column<int>(type: "int", nullable: false),
                    AddedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TemplatesTemplateID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFavoriteTemplates", x => x.FavoriteID);
                    table.ForeignKey(
                        name: "FK_UserFavoriteTemplates_NguoiSuDung_UserID",
                        column: x => x.UserID,
                        principalTable: "NguoiSuDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserFavoriteTemplates_Templates_TemplateID",
                        column: x => x.TemplateID,
                        principalTable: "Templates",
                        principalColumn: "TemplateID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserFavoriteTemplates_Templates_TemplatesTemplateID",
                        column: x => x.TemplatesTemplateID,
                        principalTable: "Templates",
                        principalColumn: "TemplateID");
                });

            migrationBuilder.CreateTable(
                name: "ReportDataTransactionFields",
                columns: table => new
                {
                    TransactionFieldID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionID = table.Column<int>(type: "int", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FieldValue = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
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
                name: "IX_Templates_BusinessOperationID",
                table: "Templates",
                column: "BusinessOperationID");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_CreatedByUserName",
                table: "Templates",
                column: "CreatedByUserName");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_CreatedDepartmentID",
                table: "Templates",
                column: "CreatedDepartmentID");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_LastModifiedByUserName",
                table: "Templates",
                column: "LastModifiedByUserName");

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

            migrationBuilder.CreateIndex(
                name: "IX_TemplateFields_TemplateID",
                table: "TemplateFields",
                column: "TemplateID");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSharingRules_TemplateID",
                table: "TemplateSharingRules",
                column: "TemplateID");

            migrationBuilder.CreateIndex(
                name: "IX_UserFavoriteTemplates_TemplateID",
                table: "UserFavoriteTemplates",
                column: "TemplateID");

            migrationBuilder.CreateIndex(
                name: "IX_UserFavoriteTemplates_TemplatesTemplateID",
                table: "UserFavoriteTemplates",
                column: "TemplatesTemplateID");

            migrationBuilder.CreateIndex(
                name: "IX_UserFavoriteTemplates_UserID",
                table: "UserFavoriteTemplates",
                column: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Templates_BusinessOperations_BusinessOperationID",
                table: "Templates",
                column: "BusinessOperationID",
                principalTable: "BusinessOperations",
                principalColumn: "OperationID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Templates_NguoiSuDung_CreatedByUserName",
                table: "Templates",
                column: "CreatedByUserName",
                principalTable: "NguoiSuDung",
                principalColumn: "UserName",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Templates_NguoiSuDung_LastModifiedByUserName",
                table: "Templates",
                column: "LastModifiedByUserName",
                principalTable: "NguoiSuDung",
                principalColumn: "UserName",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Templates_PhongBan_CreatedDepartmentID",
                table: "Templates",
                column: "CreatedDepartmentID",
                principalTable: "PhongBan",
                principalColumn: "MaPhong",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Templates_BusinessOperations_BusinessOperationID",
                table: "Templates");

            migrationBuilder.DropForeignKey(
                name: "FK_Templates_NguoiSuDung_CreatedByUserName",
                table: "Templates");

            migrationBuilder.DropForeignKey(
                name: "FK_Templates_NguoiSuDung_LastModifiedByUserName",
                table: "Templates");

            migrationBuilder.DropForeignKey(
                name: "FK_Templates_PhongBan_CreatedDepartmentID",
                table: "Templates");

            migrationBuilder.DropTable(
                name: "AvailableCifFields");

            migrationBuilder.DropTable(
                name: "BusinessOperations");

            migrationBuilder.DropTable(
                name: "ReportDataTransactionFields");

            migrationBuilder.DropTable(
                name: "TemplateFields");

            migrationBuilder.DropTable(
                name: "TemplateSharingRules");

            migrationBuilder.DropTable(
                name: "UserFavoriteTemplates");

            migrationBuilder.DropTable(
                name: "ReportDataTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Templates_BusinessOperationID",
                table: "Templates");

            migrationBuilder.DropIndex(
                name: "IX_Templates_CreatedByUserName",
                table: "Templates");

            migrationBuilder.DropIndex(
                name: "IX_Templates_CreatedDepartmentID",
                table: "Templates");

            migrationBuilder.DropIndex(
                name: "IX_Templates_LastModifiedByUserName",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "BusinessOperationID",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "CreatedByUserName",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "CreatedDepartmentID",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "CreationTimestamp",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "LastModificationTimestamp",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "LastModifiedByUserName",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "MappedDocxFilePath",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "OriginalDocxFilePath",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "SharingType",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Templates");

            migrationBuilder.RenameColumn(
                name: "OriginalUploadFileName",
                table: "Templates",
                newName: "FileName");

            migrationBuilder.AlterColumn<string>(
                name: "TemplateName",
                table: "Templates",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Templates",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TemplateCode",
                table: "Templates",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);
        }
    }
}
