using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NZWalks.API.Migrations.HrDb
{
    /// <inheritdoc />
    public partial class AddAttendanceApprovalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "Attendances",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Approved");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Attendances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedByEmployeeId",
                table: "Attendances",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Attendances",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_ApprovedByEmployeeId",
                table: "Attendances",
                column: "ApprovedByEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Employees_ApprovedByEmployeeId",
                table: "Attendances",
                column: "ApprovedByEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Employees_ApprovedByEmployeeId",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_ApprovedByEmployeeId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "ApprovedByEmployeeId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Attendances");
        }
    }
}
