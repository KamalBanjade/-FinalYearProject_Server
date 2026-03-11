using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureMedicalRecordSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStructuredDataEntrySystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedFromRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BasedOnTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Visibility = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TemplateSchema = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UsageCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AverageEntryTimeSeconds = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Templates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Templates_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Templates_Templates_BasedOnTemplateId",
                        column: x => x.BasedOnTemplateId,
                        principalTable: "Templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Templates_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PatientHealthRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecordDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BloodPressureSystolic = table.Column<int>(type: "int", nullable: true),
                    BloodPressureDiastolic = table.Column<int>(type: "int", nullable: true),
                    HeartRate = table.Column<int>(type: "int", nullable: true),
                    Temperature = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Height = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BMI = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SpO2 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ChiefComplaint = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DoctorNotes = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    Diagnosis = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TreatmentPlan = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TemplateSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedFromScratch = table.Column<bool>(type: "bit", nullable: false),
                    IsStructured = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    GeneratedPdfPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TemplateId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientHealthRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientHealthRecords_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PatientHealthRecords_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientHealthRecords_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientHealthRecords_Templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "Templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PatientHealthRecords_Templates_TemplateId1",
                        column: x => x.TemplateId1,
                        principalTable: "Templates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TemplateVersionHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    ChangeType = table.Column<int>(type: "int", nullable: false),
                    ChangeDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousSchema = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewSchema = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateVersionHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateVersionHistory_Templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "Templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TemplateVersionHistory_Users_ModifierId",
                        column: x => x.ModifierId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HealthAttributes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FieldName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FieldLabel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FieldType = table.Column<int>(type: "int", nullable: false),
                    FieldValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FieldUnit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NormalRangeMin = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NormalRangeMax = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsAbnormal = table.Column<bool>(type: "bit", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsFromTemplate = table.Column<bool>(type: "bit", nullable: false),
                    AddedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthAttributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthAttributes_PatientHealthRecords_RecordId",
                        column: x => x.RecordId,
                        principalTable: "PatientHealthRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateUsageHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FieldsAdded = table.Column<int>(type: "int", nullable: false),
                    FieldsRemoved = table.Column<int>(type: "int", nullable: false),
                    AddedFieldsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WasTemplateUpdated = table.Column<bool>(type: "bit", nullable: false),
                    EntryTimeSeconds = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateUsageHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateUsageHistory_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TemplateUsageHistory_PatientHealthRecords_RecordId",
                        column: x => x.RecordId,
                        principalTable: "PatientHealthRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateUsageHistory_Templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "Templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HealthAttributes_FieldName",
                table: "HealthAttributes",
                column: "FieldName");

            migrationBuilder.CreateIndex(
                name: "IX_HealthAttributes_RecordId",
                table: "HealthAttributes",
                column: "RecordId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthAttributes_RecordId_DisplayOrder",
                table: "HealthAttributes",
                columns: new[] { "RecordId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_HealthAttributes_SectionName",
                table: "HealthAttributes",
                column: "SectionName");

            migrationBuilder.CreateIndex(
                name: "IX_PatientHealthRecords_AppointmentId",
                table: "PatientHealthRecords",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientHealthRecords_DoctorId",
                table: "PatientHealthRecords",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientHealthRecords_PatientId",
                table: "PatientHealthRecords",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientHealthRecords_RecordDate",
                table: "PatientHealthRecords",
                column: "RecordDate");

            migrationBuilder.CreateIndex(
                name: "IX_PatientHealthRecords_TemplateId",
                table: "PatientHealthRecords",
                column: "TemplateId",
                unique: true,
                filter: "[TemplateId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PatientHealthRecords_TemplateId1",
                table: "PatientHealthRecords",
                column: "TemplateId1",
                unique: true,
                filter: "[TemplateId1] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_BasedOnTemplateId",
                table: "Templates",
                column: "BasedOnTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_CreatedBy",
                table: "Templates",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_CreatedBy_TemplateName",
                table: "Templates",
                columns: new[] { "CreatedBy", "TemplateName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Templates_DepartmentId",
                table: "Templates",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_UsageCount",
                table: "Templates",
                column: "UsageCount");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_Visibility",
                table: "Templates",
                column: "Visibility");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateUsageHistory_DoctorId",
                table: "TemplateUsageHistory",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateUsageHistory_RecordId",
                table: "TemplateUsageHistory",
                column: "RecordId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateUsageHistory_TemplateId",
                table: "TemplateUsageHistory",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateUsageHistory_UsedAt",
                table: "TemplateUsageHistory",
                column: "UsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateVersionHistory_ChangedAt",
                table: "TemplateVersionHistory",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateVersionHistory_ModifierId",
                table: "TemplateVersionHistory",
                column: "ModifierId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateVersionHistory_TemplateId_Version",
                table: "TemplateVersionHistory",
                columns: new[] { "TemplateId", "Version" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HealthAttributes");

            migrationBuilder.DropTable(
                name: "TemplateUsageHistory");

            migrationBuilder.DropTable(
                name: "TemplateVersionHistory");

            migrationBuilder.DropTable(
                name: "PatientHealthRecords");

            migrationBuilder.DropTable(
                name: "Templates");
        }
    }
}
