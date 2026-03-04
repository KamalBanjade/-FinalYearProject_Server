using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureMedicalRecordSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignedDoctorToMedicalRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedDoctorId",
                table: "MedicalRecords",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_AssignedDoctorId",
                table: "MedicalRecords",
                column: "AssignedDoctorId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalRecords_Doctors_AssignedDoctorId",
                table: "MedicalRecords",
                column: "AssignedDoctorId",
                principalTable: "Doctors",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalRecords_Doctors_AssignedDoctorId",
                table: "MedicalRecords");

            migrationBuilder.DropIndex(
                name: "IX_MedicalRecords_AssignedDoctorId",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "AssignedDoctorId",
                table: "MedicalRecords");
        }
    }
}
