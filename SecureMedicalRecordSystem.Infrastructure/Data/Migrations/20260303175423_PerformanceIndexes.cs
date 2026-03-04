using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureMedicalRecordSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RecordCertifications_DoctorId",
                table: "RecordCertifications");

            migrationBuilder.CreateIndex(
                name: "IX_RecordCertifications_DoctorId_IsValid",
                table: "RecordCertifications",
                columns: new[] { "DoctorId", "IsValid" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_AssignedDoctor_State_Deleted",
                table: "MedicalRecords",
                columns: new[] { "AssignedDoctorId", "State", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_PatientId_Deleted",
                table: "MedicalRecords",
                columns: new[] { "PatientId", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RecordCertifications_DoctorId_IsValid",
                table: "RecordCertifications");

            migrationBuilder.DropIndex(
                name: "IX_MedicalRecords_AssignedDoctor_State_Deleted",
                table: "MedicalRecords");

            migrationBuilder.DropIndex(
                name: "IX_MedicalRecords_PatientId_Deleted",
                table: "MedicalRecords");

            migrationBuilder.CreateIndex(
                name: "IX_RecordCertifications_DoctorId",
                table: "RecordCertifications",
                column: "DoctorId");
        }
    }
}
