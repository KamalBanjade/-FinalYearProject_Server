using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureMedicalRecordSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrustedDevicesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrustedDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceToken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeviceFingerprint = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IPAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrustedDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrustedDevices_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrustedDevices_DeviceToken",
                table: "TrustedDevices",
                column: "DeviceToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrustedDevices_ExpiresAt",
                table: "TrustedDevices",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_TrustedDevices_UserId_IsActive",
                table: "TrustedDevices",
                columns: new[] { "UserId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrustedDevices");
        }
    }
}
