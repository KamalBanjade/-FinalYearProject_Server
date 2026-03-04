using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureMedicalRecordSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AccessSessionsAndTotp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TOTPSecret",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AccessSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionToken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    QRTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IPAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessSessions_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AccessSessions_QRTokens_QRTokenId",
                        column: x => x.QRTokenId,
                        principalTable: "QRTokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessSessions_PatientId",
                table: "AccessSessions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessSessions_QRTokenId",
                table: "AccessSessions",
                column: "QRTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessSessions_SessionToken",
                table: "AccessSessions",
                column: "SessionToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessSessions");

            migrationBuilder.DropColumn(
                name: "TOTPSecret",
                table: "Users");
        }
    }
}
