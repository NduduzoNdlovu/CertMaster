START TRANSACTION;
CREATE TABLE "AchievementCertificates" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "CertificationId" uuid NOT NULL,
    "ExamAttemptId" uuid NOT NULL,
    "ScorePercent" integer NOT NULL,
    "Tier" character varying(32) NOT NULL,
    "VerificationCode" character varying(64) NOT NULL,
    "IssuedAtUtc" timestamp with time zone NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "ModifiedAtUtc" timestamp with time zone,
    CONSTRAINT "PK_AchievementCertificates" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AchievementCertificates_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AchievementCertificates_Certifications_CertificationId" FOREIGN KEY ("CertificationId") REFERENCES "Certifications" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_AchievementCertificates_ExamAttempts_ExamAttemptId" FOREIGN KEY ("ExamAttemptId") REFERENCES "ExamAttempts" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AchievementCertificates_CertificationId" ON "AchievementCertificates" ("CertificationId");

CREATE UNIQUE INDEX "IX_AchievementCertificates_ExamAttemptId" ON "AchievementCertificates" ("ExamAttemptId");

CREATE INDEX "IX_AchievementCertificates_UserId_IssuedAtUtc" ON "AchievementCertificates" ("UserId", "IssuedAtUtc");

CREATE UNIQUE INDEX "IX_AchievementCertificates_VerificationCode" ON "AchievementCertificates" ("VerificationCode");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261013181118_AddAchievementCertificates', '9.0.1');

COMMIT;

