using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureMedicalRecordSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmergencyDataFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BloodGroup",
                table: "Patients",
                newName: "BloodType");

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactRelationship",
                table: "Patients",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmergencyDataLastUpdated",
                table: "Patients",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyNotesToResponders",
                table: "Patients",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmergencyContactRelationship",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "EmergencyDataLastUpdated",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "EmergencyNotesToResponders",
                table: "Patients");

            migrationBuilder.RenameColumn(
                name: "BloodType",
                table: "Patients",
                newName: "BloodGroup");
        }
    }
}
