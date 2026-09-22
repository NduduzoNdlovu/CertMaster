import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "../lib/api";
import type {
  ImportJob,
  ImportJobDetail,
  ImportedQuestion,
  QuestionBankVersionInfo,
} from "../types";

const USE_MOCKS = import.meta.env.DEV && import.meta.env.VITE_USE_MOCKS === "true";

// Mock mode gives a light, in-memory simulation of the workflow so the admin UI is
// still navigable without a backend — it does not attempt to replicate real file
// parsing, since that's meaningless without an actual file.
let mockVersionCounter = 0;
const mockVersions: Record<string, QuestionBankVersionInfo[]> = {};
const mockJobs: ImportJob[] = [];
const mockJobDetails: Record<string, ImportJobDetail> = {};

export function useCertificationVersions(certificationId?: string) {
  return useQuery<QuestionBankVersionInfo[]>({
    queryKey: ["admin", "versions", certificationId],
    queryFn: async () => {
      if (USE_MOCKS) return certificationId ? mockVersions[certificationId] ?? [] : [];
      const { data } = await api.get<QuestionBankVersionInfo[]>(`/admin/certifications/${certificationId}/versions`);
      return data;
    },
    enabled: Boolean(certificationId),
  });
}

export function useImportJobs(certificationId?: string) {
  return useQuery<ImportJob[]>({
    queryKey: ["admin", "imports", certificationId ?? "all"],
    queryFn: async () => {
      if (USE_MOCKS) return certificationId ? mockJobs.filter((j) => j.certificationId === certificationId) : mockJobs;
      const { data } = await api.get<ImportJob[]>("/admin/imports", { params: { certificationId } });
      return data;
    },
  });
}

export function useImportJobDetail(jobId?: string) {
  return useQuery<ImportJobDetail>({
    queryKey: ["admin", "imports", "detail", jobId],
    queryFn: async () => {
      if (USE_MOCKS) return mockJobDetails[jobId!];
      const { data } = await api.get<ImportJobDetail>(`/admin/imports/${jobId}`);
      return data;
    },
    enabled: Boolean(jobId),
  });
}

export function useStartImport() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: {
      certificationId: string;
      questionBankVersionId?: string;
      newVersionLabel?: string;
      file: File;
    }) => {
      if (USE_MOCKS) {
        await new Promise((r) => setTimeout(r, 600));
        mockVersionCounter++;
        const versionId = payload.questionBankVersionId ?? `mock-version-${mockVersionCounter}`;
        if (!payload.questionBankVersionId) {
          mockVersions[payload.certificationId] = [
            ...(mockVersions[payload.certificationId] ?? []),
            {
              id: versionId,
              certificationId: payload.certificationId,
              versionLabel: payload.newVersionLabel || `v${mockVersionCounter} - mock`,
              status: "PendingReview",
              questionsExtracted: 2,
              questionsFlagged: 1,
              duplicatesDetected: 0,
              createdAtUtc: new Date().toISOString(),
            },
          ];
        }
        const jobId = `mock-job-${mockVersionCounter}`;
        const job: ImportJob = {
          id: jobId,
          certificationId: payload.certificationId,
          questionBankVersionId: versionId,
          versionLabel: payload.newVersionLabel || `v${mockVersionCounter} - mock`,
          originalFileName: payload.file.name,
          sourceFormat: "Csv",
          status: "ReadyForReview",
          questionsExtracted: 2,
          questionsPendingReview: 2,
          questionsApproved: 0,
          questionsRejected: 0,
          duplicatesDetected: 0,
          createdAtUtc: new Date().toISOString(),
          completedAtUtc: new Date().toISOString(),
        };
        mockJobs.unshift(job);
        mockJobDetails[jobId] = {
          job,
          questions: [
            {
              id: `${jobId}-q1`,
              topic: "General",
              difficulty: "Medium",
              prompt: "Sample imported question — mock mode doesn't parse real files.",
              explanation: "This is a placeholder shown only in mock mode.",
              reviewStatus: "PendingReview",
              validationIssues: "",
              isDuplicate: false,
              options: [
                { id: "o1", text: "Option A", isCorrect: true, sortOrder: 1 },
                { id: "o2", text: "Option B", isCorrect: false, sortOrder: 2 },
              ],
            },
            {
              id: `${jobId}-q2`,
              topic: "General",
              difficulty: "Easy",
              prompt: "A second sample question with a validation issue.",
              explanation: "",
              reviewStatus: "PendingReview",
              validationIssues: "No correct answer could be identified.",
              isDuplicate: false,
              options: [
                { id: "o3", text: "Option A", isCorrect: false, sortOrder: 1 },
                { id: "o4", text: "Option B", isCorrect: false, sortOrder: 2 },
              ],
            },
          ],
          extractedImages: [],
        };
        return job;
      }

      const formData = new FormData();
      formData.append("certificationId", payload.certificationId);
      if (payload.questionBankVersionId) formData.append("questionBankVersionId", payload.questionBankVersionId);
      if (payload.newVersionLabel) formData.append("newVersionLabel", payload.newVersionLabel);
      formData.append("file", payload.file);

      const { data } = await api.post<ImportJob>("/admin/imports", formData, {
        headers: { "Content-Type": "multipart/form-data" },
      });
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin", "imports"] });
      queryClient.invalidateQueries({ queryKey: ["admin", "versions"] });
    },
  });
}

export function useUpdateImportedQuestion() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, question }: { id: string; question: Partial<ImportedQuestion> }) => {
      if (USE_MOCKS) {
        await new Promise((r) => setTimeout(r, 300));
        return question as ImportedQuestion;
      }
      const { data } = await api.put<ImportedQuestion>(`/admin/imports/questions/${id}`, {
        topic: question.topic,
        subtopic: question.subtopic,
        difficulty: question.difficulty,
        prompt: question.prompt,
        explanation: question.explanation,
        reference: question.reference,
        options: question.options?.map((o) => ({ id: o.id, text: o.text, isCorrect: o.isCorrect })),
      });
      return data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["admin", "imports", "detail"] }),
  });
}

export function useApproveImportedQuestions() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (ids: string[]) => {
      if (USE_MOCKS) {
        await new Promise((r) => setTimeout(r, 300));
        return;
      }
      await api.post("/admin/imports/questions/approve", { importedQuestionIds: ids });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin", "imports"] });
    },
  });
}

export function useRejectImportedQuestions() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ ids, reason }: { ids: string[]; reason: string }) => {
      if (USE_MOCKS) {
        await new Promise((r) => setTimeout(r, 300));
        return;
      }
      await api.post("/admin/imports/questions/reject", { importedQuestionIds: ids, reason });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin", "imports"] });
    },
  });
}

export function usePublishVersion() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (versionId: string) => {
      if (USE_MOCKS) {
        await new Promise((r) => setTimeout(r, 400));
        return { versionId, questionsPublished: 2, publishedAtUtc: new Date().toISOString() };
      }
      const { data } = await api.post(`/admin/versions/${versionId}/publish`);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin", "versions"] });
      queryClient.invalidateQueries({ queryKey: ["certifications"] });
    },
  });
}
