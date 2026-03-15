using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureMedicalRecordSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UnifyScanHistoryAndTrendFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "DoctorId",
                table: "ScanHistories",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "IPAddress",
                table: "ScanHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "ScanHistories",
                type: "nvarchar(max)",
                nullable: true);

            // Backfill from AuditLogs where possible to populate the trend graph
            // 1. Emergency QR Scans
            migrationBuilder.Sql(@"
                INSERT INTO ScanHistories (Id, PatientId, ScannedAt, TokenType, AccessGranted, TOTPVerified, CreatedAt, CreatedBy, IsDeleted, IPAddress, UserAgent)
                SELECT NEWID(), p.Id, a.Timestamp, 1, 1, 1, GETUTCDATE(), 'System-Backfill', 0, a.IpAddress, a.UserAgent
                FROM AuditLogs a
                JOIN Patients p ON a.UserId = p.UserId
                WHERE a.Action = 'EMERGENCY ACCESS - QR code scanned'
                AND NOT EXISTS (SELECT 1 FROM ScanHistories s WHERE s.PatientId = p.Id AND ABS(DATEDIFF(second, s.ScannedAt, a.Timestamp)) < 5)
            ");

            // 2. Standard QR Scans
            migrationBuilder.Sql(@"
                INSERT INTO ScanHistories (Id, PatientId, ScannedAt, TokenType, AccessGranted, TOTPVerified, CreatedAt, CreatedBy, IsDeleted, IPAddress, UserAgent)
                SELECT NEWID(), p.Id, a.Timestamp, 0, 1, 1, GETUTCDATE(), 'System-Backfill', 0, a.IpAddress, a.UserAgent
                FROM AuditLogs a
                JOIN Patients p ON a.UserId = p.UserId
                WHERE a.Action = 'DESKTOP SCAN VERIFIED'
                AND NOT EXISTS (SELECT 1 FROM ScanHistories s WHERE s.PatientId = p.Id AND ABS(DATEDIFF(second, s.ScannedAt, a.Timestamp)) < 5)
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IPAddress",
                table: "ScanHistories");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "ScanHistories");

            migrationBuilder.AlterColumn<Guid>(
                name: "DoctorId",
                table: "ScanHistories",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
