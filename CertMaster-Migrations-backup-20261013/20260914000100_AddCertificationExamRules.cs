using CertMaster.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CertMaster.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260914000100_AddCertificationExamRules")]
public partial class AddCertificationExamRules : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "ExamDurationMinutes", table: "Certifications", type: "integer", nullable: false, defaultValue: 90);
        migrationBuilder.AddColumn<int>(name: "MockExamQuestionCount", table: "Certifications", type: "integer", nullable: false, defaultValue: 90);
        migrationBuilder.AddColumn<int>(name: "PassingScorePercent", table: "Certifications", type: "integer", nullable: false, defaultValue: 65);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ExamDurationMinutes", table: "Certifications");
        migrationBuilder.DropColumn(name: "MockExamQuestionCount", table: "Certifications");
        migrationBuilder.DropColumn(name: "PassingScorePercent", table: "Certifications");
    }
}
