using CertMaster.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace CertMaster.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260914000200_ProtectActiveMockExams")]
public partial class ProtectActiveMockExams : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.CreateIndex(
        name: "IX_ExamAttempts_UserId_OneActiveMock", table: "ExamAttempts", column: "UserId", unique: true,
        filter: "\"CompletedAtUtc\" IS NULL AND \"Mode\" = 1");
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropIndex(
        name: "IX_ExamAttempts_UserId_OneActiveMock", table: "ExamAttempts");
}
