using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureMedicalRecordSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixDecimalPrecisionAndAddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ScanHistories_DoctorId_ScannedAt",
                table: "ScanHistories",
                columns: new[] { "DoctorId", "ScannedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_AssignedDoctorId_CreatedAt_State",
                table: "MedicalRecords",
                columns: new[] { "AssignedDoctorId", "CreatedAt", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId_AppointmentDate_Status",
                table: "Appointments",
                columns: new[] { "DoctorId", "AppointmentDate", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ScanHistories_DoctorId_ScannedAt",
                table: "ScanHistories");

            migrationBuilder.DropIndex(
                name: "IX_MedicalRecords_AssignedDoctorId_CreatedAt_State",
                table: "MedicalRecords");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_DoctorId_AppointmentDate_Status",
                table: "Appointments");
        }
    }
}
