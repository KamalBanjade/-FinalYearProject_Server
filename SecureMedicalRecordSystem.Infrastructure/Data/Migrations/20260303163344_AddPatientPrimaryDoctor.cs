using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureMedicalRecordSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientPrimaryDoctor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PrimaryDoctorId",
                table: "Patients",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_PrimaryDoctorId",
                table: "Patients",
                column: "PrimaryDoctorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Doctors_PrimaryDoctorId",
                table: "Patients",
                column: "PrimaryDoctorId",
                principalTable: "Doctors",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Doctors_PrimaryDoctorId",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patients_PrimaryDoctorId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "PrimaryDoctorId",
                table: "Patients");
        }
    }
}
