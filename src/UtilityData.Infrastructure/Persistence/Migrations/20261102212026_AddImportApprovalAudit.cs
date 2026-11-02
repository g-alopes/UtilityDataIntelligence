using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UtilityData.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImportApprovalAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAtUtc",
                table: "ImportBatches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedByUserId",
                table: "ImportBatches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ApprovedRecordCount",
                table: "ImportBatches",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAtUtc",
                table: "ImportBatches");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "ImportBatches");

            migrationBuilder.DropColumn(
                name: "ApprovedRecordCount",
                table: "ImportBatches");
        }
    }
}
