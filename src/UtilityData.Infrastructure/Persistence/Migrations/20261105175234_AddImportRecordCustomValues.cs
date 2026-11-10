using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UtilityData.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImportRecordCustomValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportRecordCustomValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantCustomFieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TextValue = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IntegerValue = table.Column<long>(type: "bigint", nullable: true),
                    DecimalValue = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    DateValue = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BooleanValue = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportRecordCustomValues", x => x.Id);
                    table.CheckConstraint("CK_ImportRecordCustomValues_ExactlyOneValue", "((CASE WHEN [TextValue] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [IntegerValue] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [DecimalValue] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [DateValue] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [BooleanValue] IS NULL THEN 0 ELSE 1 END)) = 1");
                    table.ForeignKey(
                        name: "FK_ImportRecordCustomValues_ImportRecords_ImportRecordId",
                        column: x => x.ImportRecordId,
                        principalTable: "ImportRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImportRecordCustomValues_TenantCustomFields_TenantCustomFieldId",
                        column: x => x.TenantCustomFieldId,
                        principalTable: "TenantCustomFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImportRecordCustomValues_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportRecordCustomValues_ImportRecordId",
                table: "ImportRecordCustomValues",
                column: "ImportRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportRecordCustomValues_TenantCustomFieldId",
                table: "ImportRecordCustomValues",
                column: "TenantCustomFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportRecordCustomValues_TenantId_ImportRecordId_TenantCustomFieldId",
                table: "ImportRecordCustomValues",
                columns: new[] { "TenantId", "ImportRecordId", "TenantCustomFieldId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportRecordCustomValues");
        }
    }
}
