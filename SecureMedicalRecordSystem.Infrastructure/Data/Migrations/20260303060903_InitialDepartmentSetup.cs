using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureMedicalRecordSystem.Infrastructure.Data.Migrations
{
    public partial class InitialDepartmentSetup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. CLEANUP (Defensive)
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Doctors_Departments_DepartmentId')
                    ALTER TABLE Doctors DROP CONSTRAINT FK_Doctors_Departments_DepartmentId;
                
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Doctors_DepartmentId' AND object_id = OBJECT_ID('Doctors'))
                    DROP INDEX IX_Doctors_DepartmentId ON Doctors;

                IF EXISTS (SELECT * FROM sys.columns WHERE name = 'DepartmentId' AND object_id = OBJECT_ID('Doctors'))
                    ALTER TABLE Doctors DROP COLUMN DepartmentId;

                IF OBJECT_ID('Departments', 'U') IS NOT NULL
                    DROP TABLE Departments;
            ");

            // 2. CREATE DEPARTMENTS TABLE
            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            // 3. SEED INITIAL DATA
            var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Description", "IsActive", "IsDeleted", "Name", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), seedDate, "System", "Heart and blood vessel diseases", true, false, "Cardiology", null, null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), seedDate, "System", "Disorders of the nervous system", true, false, "Neurology", null, null },
                    { new Guid("33333333-3333-3333-3333-333333333333"), seedDate, "System", "Bones, joints, ligaments, tendons, and muscles", true, false, "Orthopedics", null, null },
                    { new Guid("44444444-4444-4444-4444-444444444444"), seedDate, "System", "Medical care of infants, children, and adolescents", true, false, "Pediatrics", null, null },
                    { new Guid("55555555-5555-5555-5555-555555555555"), seedDate, "System", "Prevention, diagnosis, and treatment of cancer", true, false, "Oncology", null, null }
                });

            // 4. ADD DEPARTMENT ID TO DOCTORS (Nullable first)
            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "Doctors",
                type: "uniqueidentifier",
                nullable: true);

            // 5. DATA MIGRATION Logic
            migrationBuilder.Sql("UPDATE Doctors SET DepartmentId = '11111111-1111-1111-1111-111111111111' WHERE Department = 'Cardiology'");
            migrationBuilder.Sql("UPDATE Doctors SET DepartmentId = '22222222-2222-2222-2222-222222222222' WHERE Department = 'Neurology'");
            migrationBuilder.Sql("UPDATE Doctors SET DepartmentId = '33333333-3333-3333-3333-333333333333' WHERE Department = 'Orthopedics'");
            migrationBuilder.Sql("UPDATE Doctors SET DepartmentId = '44444444-4444-4444-4444-444444444444' WHERE Department = 'Pediatrics'");
            migrationBuilder.Sql("UPDATE Doctors SET DepartmentId = '55555555-5555-5555-5555-555555555555' WHERE Department = 'Oncology'");
            
            // Fallback for any other strings or previously null values
            migrationBuilder.Sql("UPDATE Doctors SET DepartmentId = '11111111-1111-1111-1111-111111111111' WHERE DepartmentId IS NULL");

            // 6. ENFORCE CONSTRAINTS
            migrationBuilder.AlterColumn<Guid>(
                name: "DepartmentId",
                table: "Doctors",
                type: "uniqueidentifier",
                nullable: false);

            migrationBuilder.DropColumn(
                name: "Department",
                table: "Doctors");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_DepartmentId",
                table: "Doctors",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Doctors_Departments_DepartmentId",
                table: "Doctors",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Doctors_Departments_DepartmentId",
                table: "Doctors");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Doctors_DepartmentId",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Doctors");

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "Doctors",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
