using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureMedicalRecordSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ModelUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BloodType",
                table: "Patients",
                newName: "BloodGroup");

            migrationBuilder.AddColumn<string>(
                name: "ChronicConditions",
                table: "Patients",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactNumber",
                table: "Doctors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HospitalAffiliation",
                table: "Doctors",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChronicConditions",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "ContactNumber",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "HospitalAffiliation",
                table: "Doctors");

            migrationBuilder.RenameColumn(
                name: "BloodGroup",
                table: "Patients",
                newName: "BloodType");
        }
    }
}
