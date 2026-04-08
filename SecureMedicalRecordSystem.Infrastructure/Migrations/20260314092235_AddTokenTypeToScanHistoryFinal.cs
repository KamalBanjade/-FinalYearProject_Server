using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureMedicalRecordSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTokenTypeToScanHistoryFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The column was already added in a previous (now removed) migration attempt.
            // We skip AddColumn here to avoid 'column already exists' errors, but run the backfill.
            
            migrationBuilder.AddColumn<int>(
                name: "TokenType",
                table: "ScanHistories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Backfill TokenType: 1 = Emergency, 0 = Normal
            migrationBuilder.Sql("UPDATE ScanHistories SET TokenType = 1 WHERE TOTPVerified = 1 AND AccessGranted = 1 AND TOTPVerifiedAt IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TokenType",
                table: "ScanHistories");
        }
    }
}
