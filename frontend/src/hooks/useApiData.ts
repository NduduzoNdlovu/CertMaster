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
} from "../types";

const USE_MOCKS = import.meta.env.VITE_USE_MOCKS === "true";

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
