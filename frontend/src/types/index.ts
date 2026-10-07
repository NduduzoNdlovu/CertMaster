export type UserRole = "Learner" | "Administrator";

export interface AuthUser {
  id: string;
  fullName: string;
  email: string;
  role: UserRole;
  isPremium: boolean;
  avatarUrl?: string;
}

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
}

export interface Certification {
  id: string;
  code: string; // e.g. "220-1201"
  name: string; // e.g. "CompTIA A+ Core 1"
  version: string;
  description: string;
  topicCount: number;
  questionCount: number;
  examDurationMinutes?: number;
  passingScorePercent?: number;
  mockExamQuestionCount?: number;
}

export interface Topic {
  id: string;
  certificationId: string;
  name: string;
  masteryPercent: number;
}

export type Difficulty = "Easy" | "Medium" | "Hard";

export interface QuestionOption {
  id: string;
  text: string;
  isCorrect?: boolean;
}

export interface Question {
  id: string;
  certificationId: string;
  topic: string;
  subtopic?: string;
  difficulty: Difficulty;
  prompt: string;
  options: QuestionOption[];
  explanation: string;
  reference?: string;
  imageUrl?: string;
  status: "Draft" | "Published" | "Flagged";
}

export interface ExamAttempt {
  id: string;
  certificationId: string;
  certificationName?: string;
  mode: "Practice" | "MockExam";
  startedAt: string;
  completedAt?: string;
  durationSeconds: number;
  score: number;
  passed: boolean;
  totalQuestions: number;
  correctCount: number;
}

export interface ExamAnswerResult {
  questionId: string; prompt: string; selectedOptionId?: string; selectedOption?: string;
  correctOptionId?: string; correctOption?: string; isCorrect: boolean; wasFlaggedForReview: boolean; explanation: string;
}
export interface ExamAttemptDetail { attempt: ExamAttempt & { certificationName: string }; answers: ExamAnswerResult[]; }
export interface ExamSessionQuestion { id: string; topic: string; prompt: string; options: { id: string; text: string }[]; selectedOptionId?: string; wasFlaggedForReview: boolean; }
export interface ExamSession { attemptId: string; certificationId: string; certificationName: string; mode: "Practice" | "MockExam"; startedAtUtc: string; expiresAtUtc: string; serverTimeUtc: string; passingScorePercent: number; questions: ExamSessionQuestion[]; }
export interface PagedResult<T> { items: T[]; page: number; pageSize: number; totalCount: number; totalPages: number; }
export interface AdminUser { id: string; fullName: string; email: string; role: string; plan: string; isSuspended: boolean; createdAtUtc: string; }
export interface AuditLog { id: string; actorEmail: string; action: string; target?: string; level: string; createdAtUtc: string; }
export interface Payment { id: string; userEmail?: string; plan: string; amountZar: number; status: string; createdAtUtc: string; }

export interface DashboardSummary {
  studyStreakDays: number;
  questionsAnswered: number;
  averageScore: number;
  passRate: number;
  studyTimeMinutesThisWeek: number;
  leaderboardPosition: number;
  examReadinessScore: number;
  weakTopics: { topic: string; masteryPercent: number }[];
  strongTopics: { topic: string; masteryPercent: number }[];
  recentAttempts: ExamAttempt[];
  dailyActivity: { date: string; minutes: number }[];
}

export interface Flashcard {
  id: string;
  topic: string;
  front: string;
  back: string;
  reference?: string;
}

export interface AppNotification {
  id: string;
  title: string;
  body: string;
  type: "General" | "ExamResult" | "Maintenance" | "Billing";
  isRead: boolean;
  createdAtUtc: string;
}

export interface SearchResult {
  type: "Certification" | "Topic" | "Question";
  title: string;
  subtitle: string;
  id: string;
  certificationId?: string;
}

export interface AdminOverview {
  totalUsers: number;
  premiumUsers: number;
  activeUsersToday: number;
  usersOnlineNow: number;
  usersTakingExamsNow: number;
  practiceSessionsRunning: number;
  monthlyRevenue: number;
  monthlyGrowthPercent: number;
  mostPopularCertification: string;
  registrationsByDay: { date: string; count: number }[];
  systemStatus: { database: "Online" | "Degraded" | "Offline"; api: "Online" | "Degraded" | "Offline"; backgroundJobs: "Running" | "Paused" };
}

export interface MaintenanceWindow {
  id: string;
  startsAt: string;
  endsAt: string;
  reason: string;
  status: "Scheduled" | "Active" | "Completed" | "Cancelled";
}

export interface BookmarkedQuestion {
  questionId: string;
  certificationId: string;
  topic: string;
  difficulty: string;
  prompt: string;
  bookmarkedAtUtc: string;
}

export interface LeaderboardEntry {
  rank: number;
  fullName: string;
  points: number;
  isPremium: boolean;
  isCurrentUser: boolean;
}

export interface ExamSubmitResult {
  attemptId: string;
  totalQuestions: number;
  correctCount: number;
  score: number;
  passed: boolean;
}

export interface QuestionReport {
  id: string;
  questionId: string;
  questionPrompt: string;
  questionStatus: "Draft" | "Published" | "Flagged";
  reportedByEmail: string;
  reason: string;
  resolved: boolean;
  createdAtUtc: string;
}

export type QuestionBankVersionStatus = "Draft" | "ImportInProgress" | "PendingReview" | "Published" | "Archived";

export interface QuestionBankVersionInfo {
  id: string;
  certificationId: string;
  versionLabel: string;
  status: QuestionBankVersionStatus;
  questionsExtracted: number;
  questionsFlagged: number;
  duplicatesDetected: number;
  createdAtUtc: string;
  publishedAtUtc?: string;
}

export type ImportJobStatusValue =
  | "Uploaded" | "Validating" | "Extracting" | "DetectingQuestions"
  | "Normalizing" | "DetectingDuplicates" | "ReadyForReview" | "Failed";

export interface ImportJob {
  id: string;
  certificationId: string;
  questionBankVersionId: string;
  versionLabel: string;
  originalFileName: string;
  sourceFormat: string;
  status: ImportJobStatusValue;
  errorMessage?: string;
  questionsExtracted: number;
  questionsPendingReview: number;
  questionsApproved: number;
  questionsRejected: number;
  duplicatesDetected: number;
  createdAtUtc: string;
  completedAtUtc?: string;
}

export type ImportedQuestionReviewStatus = "PendingReview" | "Approved" | "Rejected";

export interface ImportedQuestionOption {
  id: string;
  text: string;
  isCorrect: boolean;
  sortOrder: number;
}

export interface ImportedQuestion {
  id: string;
  topic: string;
  subtopic?: string;
  difficulty: string;
  prompt: string;
  explanation: string;
  reference?: string;
  questionType: "Choice" | "MultipleResponse" | "Simulation" | string;
  requiresManualReview: boolean;
  sourcePageStart?: number;
  sourcePageEnd?: number;
  reviewStatus: ImportedQuestionReviewStatus;
  validationIssues: string;
  isDuplicate: boolean;
  duplicateOfQuestionId?: string;
  publishedQuestionId?: string;
  options: ImportedQuestionOption[];
  images: ImportJobImageInfo[];
}

export interface ImportJobImageInfo {
  id: string;
  pageNumber: number;
  imageKind: string;
  importedQuestionId?: string;
}

export interface ImportJobDetail {
  job: ImportJob;
  questions: ImportedQuestion[];
  extractedImages: ImportJobImageInfo[];
}
