using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CertMaster.Infrastructure.Migrations;

public partial class AddQuestionMediaAndPbqSupport : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name: "ImportedQuestionId", table: "ImportJobImages", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<string>(name: "ImageKind", table: "ImportJobImages", type: "text", nullable: false, defaultValue: "Question");
        migrationBuilder.AddColumn<int>(name: "SortOrder", table: "ImportJobImages", type: "integer", nullable: false, defaultValue: 0);

        migrationBuilder.AddColumn<string>(name: "QuestionType", table: "ImportedQuestions", type: "text", nullable: false, defaultValue: "Choice");
        migrationBuilder.AddColumn<bool>(name: "RequiresManualReview", table: "ImportedQuestions", type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<int>(name: "SourcePageStart", table: "ImportedQuestions", type: "integer", nullable: true);
        migrationBuilder.AddColumn<int>(name: "SourcePageEnd", table: "ImportedQuestions", type: "integer", nullable: true);

        migrationBuilder.AddColumn<string>(name: "QuestionType", table: "Questions", type: "text", nullable: false, defaultValue: "Choice");
        migrationBuilder.AddColumn<string>(name: "InteractionJson", table: "Questions", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(name: "ImageUrl", table: "QuestionOptions", type: "text", nullable: true);

        migrationBuilder.CreateIndex(name: "IX_ImportJobImages_ImportedQuestionId", table: "ImportJobImages", column: "ImportedQuestionId");
        migrationBuilder.AddForeignKey(
            name: "FK_ImportJobImages_ImportedQuestions_ImportedQuestionId",
            table: "ImportJobImages",
            column: "ImportedQuestionId",
            principalTable: "ImportedQuestions",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_ImportJobImages_ImportedQuestions_ImportedQuestionId", table: "ImportJobImages");
        migrationBuilder.DropIndex(name: "IX_ImportJobImages_ImportedQuestionId", table: "ImportJobImages");
        migrationBuilder.DropColumn(name: "ImportedQuestionId", table: "ImportJobImages");
        migrationBuilder.DropColumn(name: "ImageKind", table: "ImportJobImages");
        migrationBuilder.DropColumn(name: "SortOrder", table: "ImportJobImages");
        migrationBuilder.DropColumn(name: "QuestionType", table: "ImportedQuestions");
        migrationBuilder.DropColumn(name: "RequiresManualReview", table: "ImportedQuestions");
        migrationBuilder.DropColumn(name: "SourcePageStart", table: "ImportedQuestions");
        migrationBuilder.DropColumn(name: "SourcePageEnd", table: "ImportedQuestions");
        migrationBuilder.DropColumn(name: "QuestionType", table: "Questions");
        migrationBuilder.DropColumn(name: "InteractionJson", table: "Questions");
        migrationBuilder.DropColumn(name: "ImageUrl", table: "QuestionOptions");
    }
}
