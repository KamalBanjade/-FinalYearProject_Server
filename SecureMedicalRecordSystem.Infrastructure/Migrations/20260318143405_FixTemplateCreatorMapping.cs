using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureMedicalRecordSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixTemplateCreatorMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Templates_Users_CreatedBy",
                table: "Templates");

            migrationBuilder.DropIndex(
                name: "IX_Templates_CreatedBy",
                table: "Templates");

            migrationBuilder.DropIndex(
                name: "IX_Templates_CreatedBy_TemplateName",
                table: "Templates");

            // Rename existing Guid column to CreatorId
            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "Templates",
                newName: "CreatorId");

            // Add back the string CreatedBy column for audit purposes (from BaseEntity)
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Templates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            // Pre-populate the audit column with the creator's ID
            migrationBuilder.Sql("UPDATE Templates SET CreatedBy = CAST(CreatorId AS nvarchar(max))");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_CreatorId",
                table: "Templates",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_CreatorId_TemplateName",
                table: "Templates",
                columns: new[] { "CreatorId", "TemplateName" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Templates_Users_CreatorId",
                table: "Templates",
                column: "CreatorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Templates_Users_CreatorId",
                table: "Templates");

            migrationBuilder.DropIndex(
                name: "IX_Templates_CreatorId",
                table: "Templates");

            migrationBuilder.DropIndex(
                name: "IX_Templates_CreatorId_TemplateName",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Templates");

            migrationBuilder.RenameColumn(
                name: "CreatorId",
                table: "Templates",
                newName: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_CreatedBy",
                table: "Templates",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_CreatedBy_TemplateName",
                table: "Templates",
                columns: new[] { "CreatedBy", "TemplateName" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Templates_Users_CreatedBy",
                table: "Templates",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
