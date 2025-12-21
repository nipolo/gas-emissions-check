using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GEC.InspectionService.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GasInspections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistrationNumber = table.Column<string>(type: "text", nullable: true),
                    StartedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CO = table.Column<decimal>(type: "numeric", nullable: false),
                    CO2 = table.Column<decimal>(type: "numeric", nullable: false),
                    O2 = table.Column<decimal>(type: "numeric", nullable: false),
                    HC = table.Column<int>(type: "integer", nullable: false),
                    NO = table.Column<int>(type: "integer", nullable: false),
                    Lambda = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GasInspections", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GasInspections");
        }
    }
}
