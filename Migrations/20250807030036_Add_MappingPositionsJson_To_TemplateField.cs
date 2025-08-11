using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTOM.Migrations
{
    /// <inheritdoc />
    public partial class Add_MappingPositionsJson_To_TemplateField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MappingPositionsJson",
                table: "TemplateFields",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MappingPositionsJson",
                table: "TemplateFields");
        }
    }
}
