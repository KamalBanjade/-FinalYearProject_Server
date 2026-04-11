using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureMedicalRecordSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMasterMedication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MasterMedications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Aliases = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DrugCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PrimaryMarkers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecondaryMarkers = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterMedications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MasterMedications_Name",
                table: "MasterMedications",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MasterMedications");
        }
    }
}
