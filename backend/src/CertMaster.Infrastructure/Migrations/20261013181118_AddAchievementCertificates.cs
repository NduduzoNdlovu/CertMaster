
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CertMaster.Infrastructure.Migrations
{
    public partial class AddAchievementCertificates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AchievementCertificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),

                    UserId = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),

                    CertificationId = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),

                    ExamAttemptId = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),

                    ScorePercent = table.Column<int>(
                        type: "integer",
                        nullable: false),

                    Tier = table.Column<string>(
                        type: "character varying(32)",
                        maxLength: 32,
                        nullable: false),

                    VerificationCode = table.Column<string>(
                        type: "character varying(64)",
                        maxLength: 64,
                        nullable: false),

                    IssuedAtUtc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false),

                    CreatedAtUtc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false),

                    ModifiedAtUtc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_AchievementCertificates",
                        x => x.Id);

                    table.ForeignKey(
                        name: "FK_AchievementCertificates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_AchievementCertificates_Certifications_CertificationId",
                        column: x => x.CertificationId,
                        principalTable: "Certifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);

                    table.ForeignKey(
                        name: "FK_AchievementCertificates_ExamAttempts_ExamAttemptId",
                        column: x => x.ExamAttemptId,
                        principalTable: "ExamAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AchievementCertificates_CertificationId",
                table: "AchievementCertificates",
                column: "CertificationId");

            migrationBuilder.CreateIndex(
                name: "IX_AchievementCertificates_ExamAttemptId",
                table: "AchievementCertificates",
                column: "ExamAttemptId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AchievementCertificates_UserId_IssuedAtUtc",
                table: "AchievementCertificates",
                columns: new[] { "UserId", "IssuedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AchievementCertificates_VerificationCode",
                table: "AchievementCertificates",
                column: "VerificationCode",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AchievementCertificates");
        }
    }
}
