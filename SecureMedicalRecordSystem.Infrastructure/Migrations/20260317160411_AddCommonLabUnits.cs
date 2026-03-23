using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureMedicalRecordSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommonLabUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommonLabUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeasurementType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CommonUnits = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DefaultUnit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NormalRangeLow = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NormalRangeHigh = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NormalRangeUnit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Aliases = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommonLabUnits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommonLabUnits_Category",
                table: "CommonLabUnits",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_CommonLabUnits_MeasurementType",
                table: "CommonLabUnits",
                column: "MeasurementType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommonLabUnits");
        }
    }
}
