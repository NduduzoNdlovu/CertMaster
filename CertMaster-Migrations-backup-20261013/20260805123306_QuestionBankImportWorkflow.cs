using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CertMaster.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class QuestionBankImportWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookmarks_UserId",
                table: "Bookmarks");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "QuestionBankVersions");

            migrationBuilder.DropColumn(
                name: "SourceFileName",
                table: "QuestionBankVersions");

            migrationBuilder.RenameColumn(
                name: "UploadedByUserId",
                table: "QuestionBankVersions",
                newName: "CreatedByUserId");

            migrationBuilder.RenameColumn(
                name: "SourceFormat",
                table: "QuestionBankVersions",
                newName: "Status");

            migrationBuilder.AddColumn<bool>(
                name: "DailyRemindersEnabled",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ProductUpdatesEnabled",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WeeklySummaryEnabled",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAtUtc",
                table: "QuestionBankVersions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublishedByUserId",
                table: "QuestionBankVersions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ImportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CertificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionBankVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedByUserId = table.Column<string>(type: "text", nullable: false),
                    OriginalFileName = table.Column<string>(type: "text", nullable: false),
                    StoredFilePath = table.Column<string>(type: "text", nullable: false),
                    FileSha256 = table.Column<string>(type: "text", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    SourceFormat = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    QuestionsExtracted = table.Column<int>(type: "integer", nullable: false),
                    QuestionsPendingReview = table.Column<int>(type: "integer", nullable: false),
                    QuestionsApproved = table.Column<int>(type: "integer", nullable: false),
                    QuestionsRejected = table.Column<int>(type: "integer", nullable: false),
                    DuplicatesDetected = table.Column<int>(type: "integer", nullable: false),
                    ValidationIssuesCount = table.Column<int>(type: "integer", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportJobs_Certifications_CertificationId",
                        column: x => x.CertificationId,
                        principalTable: "Certifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImportJobs_QuestionBankVersions_QuestionBankVersionId",
                        column: x => x.QuestionBankVersionId,
                        principalTable: "QuestionBankVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImportedQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionBankVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CertificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Topic = table.Column<string>(type: "text", nullable: false),
                    Subtopic = table.Column<string>(type: "text", nullable: true),
                    Difficulty = table.Column<int>(type: "integer", nullable: false),
                    Prompt = table.Column<string>(type: "text", nullable: false),
                    Explanation = table.Column<string>(type: "text", nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: true),
                    ReviewStatus = table.Column<int>(type: "integer", nullable: false),
                    ValidationIssues = table.Column<string>(type: "text", nullable: false),
                    IsDuplicate = table.Column<bool>(type: "boolean", nullable: false),
                    DuplicateOfQuestionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNotes = table.Column<string>(type: "text", nullable: true),
                    PublishedQuestionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportedQuestions_Certifications_CertificationId",
                        column: x => x.CertificationId,
                        principalTable: "Certifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImportedQuestions_ImportJobs_ImportJobId",
                        column: x => x.ImportJobId,
                        principalTable: "ImportJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImportedQuestions_QuestionBankVersions_QuestionBankVersionId",
                        column: x => x.QuestionBankVersionId,
                        principalTable: "QuestionBankVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportJobImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoredFilePath = table.Column<string>(type: "text", nullable: false),
                    PageNumber = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportJobImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportJobImages_ImportJobs_ImportJobId",
                        column: x => x.ImportJobId,
                        principalTable: "ImportJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImportedQuestionOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportedQuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedQuestionOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportedQuestionOptions_ImportedQuestions_ImportedQuestionId",
                        column: x => x.ImportedQuestionId,
                        principalTable: "ImportedQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_UserId_QuestionId",
                table: "Bookmarks",
                columns: new[] { "UserId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportedQuestionOptions_ImportedQuestionId",
                table: "ImportedQuestionOptions",
                column: "ImportedQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedQuestions_CertificationId",
                table: "ImportedQuestions",
                column: "CertificationId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedQuestions_ImportJobId",
                table: "ImportedQuestions",
                column: "ImportJobId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedQuestions_QuestionBankVersionId_ReviewStatus",
                table: "ImportedQuestions",
                columns: new[] { "QuestionBankVersionId", "ReviewStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobImages_ImportJobId",
                table: "ImportJobImages",
                column: "ImportJobId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_CertificationId_Status",
                table: "ImportJobs",
                columns: new[] { "CertificationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_QuestionBankVersionId",
                table: "ImportJobs",
                column: "QuestionBankVersionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportedQuestionOptions");

            migrationBuilder.DropTable(
                name: "ImportJobImages");

            migrationBuilder.DropTable(
                name: "ImportedQuestions");

            migrationBuilder.DropTable(
                name: "ImportJobs");

            migrationBuilder.DropIndex(
                name: "IX_Bookmarks_UserId_QuestionId",
                table: "Bookmarks");

            migrationBuilder.DropColumn(
                name: "DailyRemindersEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProductUpdatesEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WeeklySummaryEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PublishedAtUtc",
                table: "QuestionBankVersions");

            migrationBuilder.DropColumn(
                name: "PublishedByUserId",
                table: "QuestionBankVersions");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "QuestionBankVersions",
                newName: "SourceFormat");

            migrationBuilder.RenameColumn(
                name: "CreatedByUserId",
                table: "QuestionBankVersions",
                newName: "UploadedByUserId");

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "QuestionBankVersions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SourceFileName",
                table: "QuestionBankVersions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_UserId",
                table: "Bookmarks",
                column: "UserId");
        }
    }
}
