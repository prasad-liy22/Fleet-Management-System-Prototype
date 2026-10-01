using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomerConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "CustomerCompanies",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "CustomerCompanies");
        }
    }
}
