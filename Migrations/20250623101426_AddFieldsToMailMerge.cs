using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTOM.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldsToMailMerge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Thêm cột mới vào bảng AvailableCifFields
            migrationBuilder.AddColumn<string>(
                name: "FieldTagPrefix",
                table: "AvailableCifFields",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true,
                defaultValue: "CIF");

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "AvailableCifFields",
                type: "int",
                nullable: true,
                defaultValue: 0);


            // Thêm cột mới vào bảng TemplateFields
            migrationBuilder.AddColumn<string>(
                name: "DataSourceType",
                table: "TemplateFields",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true,
                comment: "Loại nguồn dữ liệu của trường ('CIF', 'INPUT', 'CALC')");

            migrationBuilder.AddColumn<string>(
                name: "CalculationFormula",
                table: "TemplateFields",
                type: "nvarchar(max)",
                nullable: true,
                comment: "Công thức tính toán cho trường loại CALC");
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Xóa các cột đã thêm nếu rollback
            migrationBuilder.DropColumn(
                name: "FieldTagPrefix",
                table: "AvailableCifFields");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "AvailableCifFields");

            migrationBuilder.DropColumn(
                name: "DataSourceType",
                table: "TemplateFields");


            migrationBuilder.DropColumn(
                name: "CalculationFormula",
                table: "TemplateFields");
        }
    }
}
