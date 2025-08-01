using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTOM.Migrations
{
    /// <inheritdoc />
    public partial class FormDataAndCleanupSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create table FormDatas
            migrationBuilder.CreateTable(
                name: "FormDatas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateID = table.Column<int>(type: "int", nullable: false),
                    SoCif = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FormDataJSON = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormDatas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormDatas_Templates_TemplateID",
                        column: x => x.TemplateID,
                        principalTable: "Templates",
                        principalColumn: "TemplateID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FormDatas_TemplateID",
                table: "FormDatas",
                column: "TemplateID");

            migrationBuilder.CreateIndex(
                name: "IX_FormDatas_SoCif",
                table: "FormDatas",
                column: "SoCif");

            // 2. Drop old tables
            migrationBuilder.DropTable(name: "ReportDataTransactionFields");
            migrationBuilder.DropTable(name: "ReportDataTransactions");

            // 3. Drop obsolete column
            migrationBuilder.DropColumn(name: "MappingJson", table: "Templates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Re-add MappingJson column
            migrationBuilder.AddColumn<string>(
                name: "MappingJson",
                table: "Templates",
                type: "nvarchar(max)",
                nullable: true);

            // 2. Recreate ReportDataTransactions table
            migrationBuilder.CreateTable(
                name: "ReportDataTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateID = table.Column<int>(type: "int", nullable: false),
                    SoCif = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportDataTransactions", x => x.Id);
                });

            // 3. Recreate ReportDataTransactionFields table
            migrationBuilder.CreateTable(
                name: "ReportDataTransactionFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FieldValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportDataTransactionFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportDataTransactionFields_ReportDataTransactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "ReportDataTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportDataTransactionFields_TransactionId",
                table: "ReportDataTransactionFields",
                column: "TransactionId");

            // 4. Drop FormDatas
            migrationBuilder.DropTable(name: "FormDatas");
        }
    }
}
