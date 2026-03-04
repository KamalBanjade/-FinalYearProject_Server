using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureMedicalRecordSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSetupTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "MedicalQRGeneratedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TOTPSetupCompleted",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TOTPSetupCompletedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MedicalQRGeneratedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TOTPSetupCompleted",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TOTPSetupCompletedAt",
                table: "Users");
        }
    }
}
