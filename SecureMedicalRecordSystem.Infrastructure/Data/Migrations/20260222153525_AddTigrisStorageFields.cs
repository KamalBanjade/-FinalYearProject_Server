using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureMedicalRecordSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTigrisStorageFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedFilePath",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "FileType",
                table: "MedicalRecords");

            migrationBuilder.RenameColumn(
                name: "FileSize",
                table: "MedicalRecords",
                newName: "FileSizeBytes");

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                table: "MedicalRecords",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "MedicalRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "MedicalRecords",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptionAlgorithm",
                table: "MedicalRecords",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "AES-256-CBC");

            migrationBuilder.AddColumn<string>(
                name: "FileHash",
                table: "MedicalRecords",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsEncrypted",
                table: "MedicalRecords",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLatestVersion",
                table: "MedicalRecords",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "MimeType",
                table: "MedicalRecords",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                table: "MedicalRecords",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "PreviousVersionId",
                table: "MedicalRecords",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecordDate",
                table: "MedicalRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "S3ObjectKey",
                table: "MedicalRecords",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "MedicalRecords",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "EncryptionAlgorithm",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "FileHash",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "IsEncrypted",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "IsLatestVersion",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "MimeType",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "PreviousVersionId",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "RecordDate",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "S3ObjectKey",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "MedicalRecords");

            migrationBuilder.RenameColumn(
                name: "FileSizeBytes",
                table: "MedicalRecords",
                newName: "FileSize");

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                table: "MedicalRecords",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedFilePath",
                table: "MedicalRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "MedicalRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FileType",
                table: "MedicalRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
