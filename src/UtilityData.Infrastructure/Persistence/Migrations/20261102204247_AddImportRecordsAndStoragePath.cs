using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UtilityData.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImportRecordsAndStoragePath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAtUtc",
                table: "ImportBatches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "ImportBatches",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "ImportBatches",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoredFilePath",
                table: "ImportBatches",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ImportRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowNumber = table.Column<long>(type: "bigint", nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    ServiceAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    Errors = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportRecords_ImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "ImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImportRecords_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_Status_CreatedAtUtc",
                table: "ImportBatches",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportRecords_ImportBatchId",
                table: "ImportRecords",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportRecords_TenantId_ImportBatchId_IsValid",
                table: "ImportRecords",
                columns: new[] { "TenantId", "ImportBatchId", "IsValid" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportRecords_TenantId_ImportBatchId_RowNumber",
                table: "ImportRecords",
                columns: new[] { "TenantId", "ImportBatchId", "RowNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportRecords");

            migrationBuilder.DropIndex(
                name: "IX_ImportBatches_Status_CreatedAtUtc",
                table: "ImportBatches");

            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                table: "ImportBatches");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ImportBatches");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "ImportBatches");

            migrationBuilder.DropColumn(
                name: "StoredFilePath",
                table: "ImportBatches");
        }
    }
}
