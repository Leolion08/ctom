using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTOM.Migrations
{
    /// <inheritdoc />
    public partial class AddMappingJsonToTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MappingJson",
                table: "Templates",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MappingJson",
                table: "Templates");
        }
    }
}
