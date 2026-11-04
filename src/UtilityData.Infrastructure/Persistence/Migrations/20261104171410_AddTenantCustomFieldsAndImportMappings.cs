using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UtilityData.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantCustomFieldsAndImportMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ImportMappingVersionId",
                table: "ImportBatches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ImportMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FileFormat = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportMappings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantCustomFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantCustomFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantCustomFields_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportMappingVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportMappingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    WorksheetName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    HeaderRowNumber = table.Column<int>(type: "int", nullable: false),
                    DateFormat = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    DecimalSeparator = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    ThousandsSeparator = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportMappingVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportMappingVersions_ImportMappings_ImportMappingId",
                        column: x => x.ImportMappingId,
                        principalTable: "ImportMappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImportMappingVersions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerAccountCustomValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_CustomerAccountCustomValues", x => x.Id);
                    table.CheckConstraint("CK_CustomerAccountCustomValues_ExactlyOneValue", "((CASE WHEN [TextValue] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [IntegerValue] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [DecimalValue] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [DateValue] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [BooleanValue] IS NULL THEN 0 ELSE 1 END)) = 1");
                    table.ForeignKey(
                        name: "FK_CustomerAccountCustomValues_CustomerAccounts_CustomerAccountId",
                        column: x => x.CustomerAccountId,
                        principalTable: "CustomerAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerAccountCustomValues_TenantCustomFields_TenantCustomFieldId",
                        column: x => x.TenantCustomFieldId,
                        principalTable: "TenantCustomFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerAccountCustomValues_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportMappingColumns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportMappingVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceColumnName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TargetKind = table.Column<int>(type: "int", nullable: false),
                    TargetFieldKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    TenantCustomFieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportMappingColumns", x => x.Id);
                    table.CheckConstraint("CK_ImportMappingColumns_Target", "(([TargetKind] = 1 AND [TargetFieldKey] IS NOT NULL AND [TenantCustomFieldId] IS NULL) OR ([TargetKind] = 2 AND [TargetFieldKey] IS NULL AND [TenantCustomFieldId] IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_ImportMappingColumns_ImportMappingVersions_ImportMappingVersionId",
                        column: x => x.ImportMappingVersionId,
                        principalTable: "ImportMappingVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImportMappingColumns_TenantCustomFields_TenantCustomFieldId",
                        column: x => x.TenantCustomFieldId,
                        principalTable: "TenantCustomFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImportMappingColumns_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_ImportMappingVersionId",
                table: "ImportBatches",
                column: "ImportMappingVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccountCustomValues_CustomerAccountId",
                table: "CustomerAccountCustomValues",
                column: "CustomerAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccountCustomValues_TenantCustomFieldId",
                table: "CustomerAccountCustomValues",
                column: "TenantCustomFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccountCustomValues_TenantId_CustomerAccountId_TenantCustomFieldId",
                table: "CustomerAccountCustomValues",
                columns: new[] { "TenantId", "CustomerAccountId", "TenantCustomFieldId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccountCustomValues_TenantId_TenantCustomFieldId",
                table: "CustomerAccountCustomValues",
                columns: new[] { "TenantId", "TenantCustomFieldId" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportMappingColumns_ImportMappingVersionId",
                table: "ImportMappingColumns",
                column: "ImportMappingVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportMappingColumns_TenantCustomFieldId",
                table: "ImportMappingColumns",
                column: "TenantCustomFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportMappingColumns_TenantId_ImportMappingVersionId_SourceColumnName",
                table: "ImportMappingColumns",
                columns: new[] { "TenantId", "ImportMappingVersionId", "SourceColumnName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportMappingColumns_TenantId_ImportMappingVersionId_TargetFieldKey",
                table: "ImportMappingColumns",
                columns: new[] { "TenantId", "ImportMappingVersionId", "TargetFieldKey" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportMappings_TenantId_IsActive",
                table: "ImportMappings",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportMappings_TenantId_Name",
                table: "ImportMappings",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportMappingVersions_ImportMappingId",
                table: "ImportMappingVersions",
                column: "ImportMappingId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportMappingVersions_TenantId_ImportMappingId_VersionNumber",
                table: "ImportMappingVersions",
                columns: new[] { "TenantId", "ImportMappingId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantCustomFields_TenantId_IsActive",
                table: "TenantCustomFields",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantCustomFields_TenantId_Name",
                table: "TenantCustomFields",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ImportBatches_ImportMappingVersions_ImportMappingVersionId",
                table: "ImportBatches",
                column: "ImportMappingVersionId",
                principalTable: "ImportMappingVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImportBatches_ImportMappingVersions_ImportMappingVersionId",
                table: "ImportBatches");

            migrationBuilder.DropTable(
                name: "CustomerAccountCustomValues");

            migrationBuilder.DropTable(
                name: "ImportMappingColumns");

            migrationBuilder.DropTable(
                name: "ImportMappingVersions");

            migrationBuilder.DropTable(
                name: "TenantCustomFields");

            migrationBuilder.DropTable(
                name: "ImportMappings");

            migrationBuilder.DropIndex(
                name: "IX_ImportBatches_ImportMappingVersionId",
                table: "ImportBatches");

            migrationBuilder.DropColumn(
                name: "ImportMappingVersionId",
                table: "ImportBatches");
        }
    }
}
