import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "../lib/api";
import {
  mockAdminOverview,
  mockBookmarkedIds,
  mockCertifications,
  mockLeaderboard,
  mockMaintenanceWindows,
  mockNotifications,
  mockOpenReports,
  mockQuestions,
  mockSearch,
} from "../lib/mockData";
import type {
  AdminOverview,
  AppNotification,
  BookmarkedQuestion,
  Certification,
  ExamSubmitResult,
  Flashcard,
  LeaderboardEntry,
  MaintenanceWindow,
  Question,
  QuestionReport,
  SearchResult,
  ExamAttempt, ExamAttemptDetail, PagedResult, AdminUser, AdminUserDetail, AdminCertification, AdminResult, AdminResultDetail, AdminQuestion, AuditLog, Payment, ExamSession,
} from "../types";

const USE_MOCKS = import.meta.env.DEV && import.meta.env.VITE_USE_MOCKS === "true";

export function useMyResults() { return useQuery<ExamAttempt[]>({ queryKey: ["exam-results"], queryFn: async () => (await api.get("/exams/my-results")).data }); }
export function useMyResult(id?: string) { return useQuery<ExamAttemptDetail>({ queryKey: ["exam-results", id], queryFn: async () => (await api.get(`/exams/my-results/${id}`)).data, enabled: Boolean(id) }); }
export function useAdminUsers(params: { search?: string; role?: string; plan?: string; status?: string; page: number; pageSize: number }) {
  return useQuery<PagedResult<AdminUser>>({ queryKey: ["admin", "users", params], queryFn: async () => (await api.get("/admin/users", { params })).data });
}
export function useSetUserSuspended() { const qc = useQueryClient(); return useMutation({ mutationFn: async ({ id, suspended }: { id: string; suspended: boolean }) => api.post(`/admin/users/${id}/${suspended ? "suspend" : "reactivate"}`), onSuccess: () => qc.invalidateQueries({ queryKey: ["admin", "users"] }) }); }
export function useAdminLogs(page = 1, search = "", level = "") { return useQuery<PagedResult<AuditLog>>({ queryKey: ["admin", "logs", page, search, level], queryFn: async () => (await api.get("/admin/logs", { params: { page, search: search || undefined, level: level || undefined } })).data }); }
export function useAdminPayments() { return useQuery<Payment[]>({ queryKey: ["admin", "payments"], queryFn: async () => (await api.get("/admin/payments")).data }); }
export function usePaymentSummary() { return useQuery<{ monthlyRevenue: number; activeSubscriptions: number; monthlyPlans: number; yearlyPlans: number }>({ queryKey: ["admin", "payment-summary"], queryFn: async () => (await api.get("/admin/payment-summary")).data }); }
export function useAdminAnalytics() { return useQuery<{ questionAccuracyPercent: number; averageMockExamDurationMinutes: number; mostFailedTopic: string }>({ queryKey: ["admin", "analytics"], queryFn: async () => (await api.get("/admin/analytics")).data }); }
export function useStartExam() { return useMutation({ mutationFn: async (payload: { certificationId: string; mode: "Practice" | "MockExam" }) => (await api.post<ExamSession>("/exams/start", payload)).data }); }
export function useSaveExamAnswer() { return useMutation({ mutationFn: async (p: { attemptId: string; questionId: string; selectedOptionId: string | null; wasFlaggedForReview: boolean }) => api.put(`/exams/${p.attemptId}/answers/${p.questionId}`, { selectedOptionId: p.selectedOptionId, wasFlaggedForReview: p.wasFlaggedForReview }) }); }
export function useSubmitPracticeAnswer() {
  return useMutation({
    mutationFn: async (p: { attemptId: string; questionId: string; selectedOptionId: string | null; wasFlaggedForReview: boolean }) =>
      (await api.post(`/exams/${p.attemptId}/practice-answers/${p.questionId}`, {
        selectedOptionId: p.selectedOptionId,
        wasFlaggedForReview: p.wasFlaggedForReview,
      })).data as {
        questionId: string;
        isCorrect: boolean;
        correctOptionId?: string;
        correctOption?: string;
        explanation: string;
      },
  });
}
export function useCompleteExam() { const qc = useQueryClient(); return useMutation({ mutationFn: async (attemptId: string) => (await api.post<ExamSubmitResult>(`/exams/${attemptId}/complete`)).data, onSuccess: () => { qc.invalidateQueries({ queryKey: ["exam-results"] }); qc.invalidateQueries({ queryKey: ["dashboard"] }); } }); }

export function useCertifications() {
  return useQuery<Certification[]>({
    queryKey: ["certifications"],
    queryFn: async () => {
      if (USE_MOCKS) return mockCertifications;
      const { data } = await api.get<Certification[]>("/certifications");
      return data;
    },
  });
}

export function useAdminUser(id?: string) {
  return useQuery<AdminUserDetail>({ queryKey: ["admin", "user", id], queryFn: async () => (await api.get(`/admin/users/${id}`)).data, enabled: Boolean(id) });
}
export function useAdminCertifications() {
  return useQuery<AdminCertification[]>({ queryKey: ["admin", "certifications"], queryFn: async () => (await api.get("/admin/certifications")).data });
}
export function useUpdateAdminCertification() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: async ({ id, data }: { id: string; data: Omit<AdminCertification, "id" | "topicCount" | "questionCount"> }) => api.put(`/admin/certifications/${id}`, data), onSuccess: () => { qc.invalidateQueries({ queryKey: ["admin", "certifications"] }); qc.invalidateQueries({ queryKey: ["certifications"] }); } });
}
export function useAdminResults(params: { search?: string; certificationId?: string; mode?: string; passed?: string; page: number; pageSize: number }) {
  return useQuery<PagedResult<AdminResult>>({ queryKey: ["admin", "results", params], queryFn: async () => (await api.get("/admin/results", { params: { ...params, passed: params.passed === "" ? undefined : params.passed } })).data });
}
export function useAdminResult(id?: string) {
  return useQuery<AdminResultDetail>({ queryKey: ["admin", "result", id], queryFn: async () => (await api.get(`/admin/results/${id}`)).data, enabled: Boolean(id) });
}
export function useAdminQuestions(params: { search?: string; certificationId?: string; status?: string; difficulty?: string; page: number; pageSize: number }) {
  return useQuery<PagedResult<AdminQuestion>>({ queryKey: ["admin", "questions", params], queryFn: async () => (await api.get("/admin/questions", { params })).data });
}
export function useAdminQuestion(id?: string) {
  return useQuery<AdminQuestion>({ queryKey: ["admin", "question", id], queryFn: async () => (await api.get(`/admin/questions/${id}`)).data, enabled: Boolean(id) });
}
export function useUpdateAdminQuestion() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: async ({ id, data }: { id: string; data: Omit<AdminQuestion, "id" | "certificationId" | "certificationName" | "questionBankVersionId"> }) => api.put(`/admin/questions/${id}`, data), onSuccess: (_, vars) => { qc.invalidateQueries({ queryKey: ["admin", "questions"] }); qc.invalidateQueries({ queryKey: ["admin", "question", vars.id] }); qc.invalidateQueries({ queryKey: ["questions"] }); } });
}
export function useBroadcastNotification() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: async (data: { title: string; body: string; type: string; userIds?: string[] }) => (await api.post("/admin/notifications/broadcast", data)).data as { recipients: number }, onSuccess: () => qc.invalidateQueries({ queryKey: ["admin", "overview"] }) });
}
export function useScheduleMaintenanceWindow() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: async (data: { startsAtUtc: string; endsAtUtc: string; reason: string }) => api.post("/admin/maintenance", data), onSuccess: () => qc.invalidateQueries({ queryKey: ["admin", "maintenance"] }) });
}
export function useCancelMaintenanceWindow() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: async (id: string) => api.post(`/admin/maintenance/${id}/cancel`), onSuccess: () => qc.invalidateQueries({ queryKey: ["admin", "maintenance"] }) });
}

export function useAdminOverview() {
  return useQuery<AdminOverview>({
    queryKey: ["admin", "overview"],
    queryFn: async () => {
      if (USE_MOCKS) return mockAdminOverview;
      const { data } = await api.get<AdminOverview>("/admin/overview");
      return data;
    },
  });
}

export function useMaintenanceWindows() {
  return useQuery<MaintenanceWindow[]>({
    queryKey: ["admin", "maintenance"],
    queryFn: async () => {
      if (USE_MOCKS) return mockMaintenanceWindows;
      const { data } = await api.get<MaintenanceWindow[]>("/admin/maintenance");
      return data;
    },
  });
}

export function usePracticeQuestions(certificationId?: string) {
  return useQuery<Question[]>({
    queryKey: ["questions", "practice", certificationId],
    queryFn: async () => {
      if (USE_MOCKS) {
        return certificationId
          ? mockQuestions.filter((q) => q.certificationId === certificationId)
          : mockQuestions;
      }
      const { data } = await api.get<Question[]>("/questions/practice", { params: { certificationId } });
      return data;
    },
  });
}

export function useNotifications() {
  return useQuery<AppNotification[]>({
    queryKey: ["notifications"],
    queryFn: async () => {
      if (USE_MOCKS) return mockNotifications;
      const { data } = await api.get<AppNotification[]>("/notifications");
      return data;
    },
    refetchInterval: USE_MOCKS ? false : 60_000,
  });
}

export function useMarkNotificationRead() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      if (USE_MOCKS) return;
      await api.post(`/notifications/${id}/read`);
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["notifications"] }),
  });
}

export function useMarkAllNotificationsRead() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async () => {
      if (USE_MOCKS) return;
      await api.post("/notifications/read-all");
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["notifications"] }),
  });
}

export function useFlashcards(certificationId?: string) {
  return useQuery<Flashcard[]>({
    queryKey: ["flashcards", certificationId],
    queryFn: async () => {
      if (USE_MOCKS) {
        const source = certificationId
          ? mockQuestions.filter((q) => q.certificationId === certificationId)
          : mockQuestions;
        return source.map((q) => ({ id: q.id, topic: q.topic, front: q.prompt, back: q.explanation, reference: q.reference }));
      }
      const { data } = await api.get<Flashcard[]>("/flashcards", { params: { certificationId } });
      return data;
    },
    enabled: !USE_MOCKS ? true : true,
  });
}

export function useSearch(term: string) {
  return useQuery<SearchResult[]>({
    queryKey: ["search", term],
    queryFn: async () => {
      if (USE_MOCKS) return mockSearch(term);
      const { data } = await api.get<SearchResult[]>("/search", { params: { q: term } });
      return data;
    },
    enabled: term.trim().length >= 2,
    staleTime: 10_000,
  });
}

export function useBookmarkedIds() {
  return useQuery<Set<string>>({
    queryKey: ["bookmarks", "ids"],
    queryFn: async () => {
      if (USE_MOCKS) return new Set(mockBookmarkedIds);
      const { data } = await api.get<string[]>("/bookmarks/ids");
      return new Set(data);
    },
  });
}

export function useToggleBookmark() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (questionId: string) => {
      if (USE_MOCKS) {
        if (mockBookmarkedIds.has(questionId)) mockBookmarkedIds.delete(questionId);
        else mockBookmarkedIds.add(questionId);
        return { isBookmarked: mockBookmarkedIds.has(questionId) };
      }
      const { data } = await api.post<{ isBookmarked: boolean }>(`/bookmarks/${questionId}/toggle`);
      return data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["bookmarks"] }),
  });
}

export function useMyBookmarks() {
  return useQuery<BookmarkedQuestion[]>({
    queryKey: ["bookmarks", "list"],
    queryFn: async () => {
      if (USE_MOCKS) {
        return mockQuestions
          .filter((q) => mockBookmarkedIds.has(q.id))
          .map((q) => ({
            questionId: q.id,
            certificationId: q.certificationId,
            topic: q.topic,
            difficulty: q.difficulty,
            prompt: q.prompt,
            bookmarkedAtUtc: new Date().toISOString(),
          }));
      }
      const { data } = await api.get<BookmarkedQuestion[]>("/bookmarks");
      return data;
    },
  });
}

export function useReportQuestion() {
  return useMutation({
    mutationFn: async ({ questionId, reason }: { questionId: string; reason: string }) => {
      if (USE_MOCKS) {
        await new Promise((r) => setTimeout(r, 300));
        return { message: "Thanks — our content team will review this question." };
      }
      const { data } = await api.post("/question-reports", { questionId, reason });
      return data;
    },
  });
}

export function useLeaderboard(take = 10, period: "AllTime" | "Monthly" = "AllTime") {
  return useQuery<LeaderboardEntry[]>({
    queryKey: ["leaderboard", take, period],
    queryFn: async () => {
      if (USE_MOCKS) return mockLeaderboard.filter((e) => !e.isCurrentUser).slice(0, take);
      const { data } = await api.get<LeaderboardEntry[]>("/leaderboard", { params: { take, period } });
      return data;
    },
  });
}

export function useMyLeaderboardRank(period: "AllTime" | "Monthly" = "AllTime") {
  return useQuery<LeaderboardEntry | null>({
    queryKey: ["leaderboard", "me", period],
    queryFn: async () => {
      if (USE_MOCKS) return mockLeaderboard.find((e) => e.isCurrentUser) ?? null;
      const { data } = await api.get<LeaderboardEntry | null>("/leaderboard/me", { params: { period } });
      return data;
    },
  });
}

export function useUpdateProfile() {
  return useMutation({
    mutationFn: async (fullName: string) => {
      if (USE_MOCKS) {
        await new Promise((r) => setTimeout(r, 300));
        return { message: "Profile updated." };
      }
      const { data } = await api.put("/profile", { fullName });
      return data;
    },
  });
}

export function useChangePassword() {
  return useMutation({
    mutationFn: async (payload: { currentPassword: string; newPassword: string }) => {
      if (USE_MOCKS) {
        await new Promise((r) => setTimeout(r, 300));
        return { message: "Password updated." };
      }
      const { data } = await api.post("/profile/change-password", payload);
      return data;
    },
  });
}

export interface NotificationPreferences {
  dailyReminders: boolean;
  weeklySummary: boolean;
  productUpdates: boolean;
}

const DEFAULT_NOTIFICATION_PREFERENCES: NotificationPreferences = {
  dailyReminders: true,
  weeklySummary: true,
  productUpdates: true,
};

export function useNotificationPreferences() {
  return useQuery<NotificationPreferences>({
    queryKey: ["profile", "notification-preferences"],
    queryFn: async () => {
      if (USE_MOCKS) return DEFAULT_NOTIFICATION_PREFERENCES;
      const { data } = await api.get<NotificationPreferences>("/profile/notification-preferences");
      return data;
    },
  });
}

export function useUpdateNotificationPreferences() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (prefs: NotificationPreferences) => {
      if (USE_MOCKS) {
        await new Promise((r) => setTimeout(r, 300));
        return prefs;
      }
      const { data } = await api.put("/profile/notification-preferences", prefs);
      return data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["profile", "notification-preferences"] }),
  });
}

export function useOpenQuestionReports() {
  return useQuery<QuestionReport[]>({
    queryKey: ["admin", "question-reports"],
    queryFn: async () => {
      if (USE_MOCKS) return mockOpenReports.filter((r) => !r.resolved);
      const { data } = await api.get<QuestionReport[]>("/question-reports/open");
      return data;
    },
  });
}

export function useResolveQuestionReport() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, action }: { id: string; action: "Republish" | "KeepFlagged" }) => {
      if (USE_MOCKS) {
        const report = mockOpenReports.find((r) => r.id === id);
        if (report) report.resolved = true;
        await new Promise((r) => setTimeout(r, 300));
        return;
      }
      await api.post(`/question-reports/${id}/resolve`, { action });
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["admin", "question-reports"] }),
  });
}

export interface SubmitExamPayload {
  certificationId: string;
  mode: "Practice" | "MockExam";
  durationSeconds: number;
  answers: { questionId: string; selectedOptionId: string | null; wasFlaggedForReview: boolean }[];
}

/**
 * Persists a practice or mock exam attempt to the backend so it counts toward the
 * dashboard's stats, weak/strong topics, study streak, and the leaderboard. In mock
 * mode, scores the answers against mockQuestions locally so the UI behaves the same
 * way without a real backend.
 */
export function useSubmitExam() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: SubmitExamPayload): Promise<ExamSubmitResult> => {
      if (USE_MOCKS) {
        let correctCount = 0;
        for (const answer of payload.answers) {
          const question = mockQuestions.find((q) => q.id === answer.questionId);
          const option = question?.options.find((o) => o.id === answer.selectedOptionId);
          if (option?.isCorrect) correctCount++;
        }
        const totalQuestions = payload.answers.length;
        const score = totalQuestions === 0 ? 0 : Math.round((correctCount / totalQuestions) * 100);
        return {
          attemptId: `mock-${Date.now()}`,
          totalQuestions,
          correctCount,
          score,
          passed: score >= 65,
        };
      }

      const { data } = await api.post<ExamSubmitResult>("/exams/submit", payload);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
      queryClient.invalidateQueries({ queryKey: ["leaderboard"] });
      queryClient.invalidateQueries({ queryKey: ["notifications"] });
    },
  });
}
