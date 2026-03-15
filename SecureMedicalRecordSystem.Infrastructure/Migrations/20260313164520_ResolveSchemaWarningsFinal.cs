using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureMedicalRecordSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ResolveSchemaWarningsFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PatientHealthRecords_Templates_TemplateId1",
                table: "PatientHealthRecords");

            migrationBuilder.DropIndex(
                name: "IX_PatientHealthRecords_TemplateId1",
                table: "PatientHealthRecords");

            migrationBuilder.DropColumn(
                name: "TemplateId1",
                table: "PatientHealthRecords");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "AppointmentRecords",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "AppointmentRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AppointmentRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "AppointmentRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "AppointmentRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Templates_CreatedFromRecordId",
                table: "Templates",
                column: "CreatedFromRecordId");

            migrationBuilder.AddForeignKey(
                name: "FK_Templates_PatientHealthRecords_CreatedFromRecordId",
                table: "Templates",
                column: "CreatedFromRecordId",
                principalTable: "PatientHealthRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Templates_PatientHealthRecords_CreatedFromRecordId",
                table: "Templates");

            migrationBuilder.DropIndex(
                name: "IX_Templates_CreatedFromRecordId",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AppointmentRecords");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "AppointmentRecords");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AppointmentRecords");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AppointmentRecords");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "AppointmentRecords");

            migrationBuilder.AddColumn<Guid>(
                name: "TemplateId1",
                table: "PatientHealthRecords",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientHealthRecords_TemplateId1",
                table: "PatientHealthRecords",
                column: "TemplateId1",
                unique: true,
                filter: "[TemplateId1] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_PatientHealthRecords_Templates_TemplateId1",
                table: "PatientHealthRecords",
                column: "TemplateId1",
                principalTable: "Templates",
                principalColumn: "Id");
        }
    }
}
