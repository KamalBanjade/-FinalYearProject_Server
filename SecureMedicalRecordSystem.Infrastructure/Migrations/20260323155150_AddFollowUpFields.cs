using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureMedicalRecordSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFollowUpFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FollowUpDate",
                table: "PatientHealthRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FollowUpDays",
                table: "PatientHealthRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FollowUpScheduled",
                table: "PatientHealthRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentAppointmentId",
                table: "Appointments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ParentAppointmentId",
                table: "Appointments",
                column: "ParentAppointmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Appointments_ParentAppointmentId",
                table: "Appointments",
                column: "ParentAppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Appointments_ParentAppointmentId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ParentAppointmentId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "FollowUpDate",
                table: "PatientHealthRecords");

            migrationBuilder.DropColumn(
                name: "FollowUpDays",
                table: "PatientHealthRecords");

            migrationBuilder.DropColumn(
                name: "FollowUpScheduled",
                table: "PatientHealthRecords");

            migrationBuilder.DropColumn(
                name: "ParentAppointmentId",
                table: "Appointments");
        }
    }
}
