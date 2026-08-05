import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { AppLayout } from "./components/layout/AppLayout";
import { ProtectedRoute } from "./routes/ProtectedRoute";

import Login from "./pages/auth/Login";
import Register from "./pages/auth/Register";
import ForgotPassword from "./pages/auth/ForgotPassword";
import ResetPassword from "./pages/auth/ResetPassword";

import Dashboard from "./pages/learner/Dashboard";
import Practice from "./pages/learner/Practice";
import MockExam from "./pages/learner/MockExam";
import Results from "./pages/learner/Results";
import Progress from "./pages/learner/Progress";
import Bookmarks from "./pages/learner/Bookmarks";
import Flashcards from "./pages/learner/Flashcards";
import Certifications from "./pages/learner/Certifications";
import Leaderboard from "./pages/learner/Leaderboard";
import Premium from "./pages/learner/Premium";
import Settings from "./pages/learner/Settings";
import Help from "./pages/learner/Help";

import AdminDashboard from "./pages/admin/AdminDashboard";
import AdminUsers from "./pages/admin/Users";
import QuestionBanks from "./pages/admin/QuestionBanks";
import ImportReview from "./pages/admin/ImportReview";
import AdminAnalytics from "./pages/admin/Analytics";
import AdminReports from "./pages/admin/Reports";
import Maintenance from "./pages/admin/Maintenance";
import Payments from "./pages/admin/Payments";
import Logs from "./pages/admin/Logs";

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Public auth routes */}
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
        <Route path="/forgot-password" element={<ForgotPassword />} />
        <Route path="/reset-password" element={<ResetPassword />} />

        {/* Learner routes */}
        <Route
          element={
            <ProtectedRoute>
              <AppLayout />
            </ProtectedRoute>
          }
        >
          <Route path="/dashboard" element={<Dashboard />} />
          <Route path="/practice" element={<Practice />} />
          <Route path="/mock-exams" element={<MockExam />} />
          <Route path="/results" element={<Results />} />
          <Route path="/progress" element={<Progress />} />
          <Route path="/bookmarks" element={<Bookmarks />} />
          <Route path="/flashcards" element={<Flashcards />} />
          <Route path="/certifications" element={<Certifications />} />
          <Route path="/leaderboard" element={<Leaderboard />} />
          <Route path="/premium" element={<Premium />} />
          <Route path="/settings" element={<Settings />} />
          <Route path="/help" element={<Help />} />
        </Route>

        {/* Administrator routes */}
        <Route
          element={
            <ProtectedRoute requiredRole="Administrator">
              <AppLayout />
            </ProtectedRoute>
          }
        >
          <Route path="/admin" element={<AdminDashboard />} />
          <Route path="/admin/users" element={<AdminUsers />} />
          <Route path="/admin/question-banks" element={<QuestionBanks />} />
          <Route path="/admin/imports/:jobId" element={<ImportReview />} />
          <Route path="/admin/analytics" element={<AdminAnalytics />} />
          <Route path="/admin/reports" element={<AdminReports />} />
          <Route path="/admin/maintenance" element={<Maintenance />} />
          <Route path="/admin/payments" element={<Payments />} />
          <Route path="/admin/logs" element={<Logs />} />
        </Route>

        <Route path="/" element={<Navigate to="/dashboard" replace />} />
        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
