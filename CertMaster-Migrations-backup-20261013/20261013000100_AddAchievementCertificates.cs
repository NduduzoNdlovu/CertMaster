using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CertMaster.Infrastructure.Migrations;

[Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(CertMaster.Infrastructure.Persistence.AppDbContext))]
[Migration("20261013000100_AddAchievementCertificates")]
public partial class AddAchievementCertificates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AchievementCertificates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                CertificationId = table.Column<Guid>(type: "uuid", nullable: false),
                ExamAttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                ScorePercent = table.Column<int>(type: "integer", nullable: false),
                Tier = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                VerificationCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                IssuedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AchievementCertificates", x => x.Id);
                table.ForeignKey("FK_AchievementCertificates_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_AchievementCertificates_Certifications_CertificationId", x => x.CertificationId, "Certifications", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AchievementCertificates_ExamAttempts_ExamAttemptId", x => x.ExamAttemptId, "ExamAttempts", "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex("IX_AchievementCertificates_UserId_IssuedAtUtc", "AchievementCertificates", new[] { "UserId", "IssuedAtUtc" });
        migrationBuilder.CreateIndex("IX_AchievementCertificates_ExamAttemptId", "AchievementCertificates", "ExamAttemptId", unique: true);
        migrationBuilder.CreateIndex("IX_AchievementCertificates_VerificationCode", "AchievementCertificates", "VerificationCode", unique: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("AchievementCertificates");
}
