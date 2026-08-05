import { useEffect, useState } from "react";
import { useAuth } from "../../context/AuthContext";
import { Card, SectionHeading } from "../../components/ui/Primitives";
import { Button } from "../../components/ui/Button";
import {
  useUpdateProfile,
  useChangePassword,
  useNotificationPreferences,
  useUpdateNotificationPreferences,
  type NotificationPreferences,
} from "../../hooks/useApiData";

const notificationLabels: { key: keyof NotificationPreferences; label: string }[] = [
  { key: "dailyReminders", label: "Daily study reminders" },
  { key: "weeklySummary", label: "Weekly progress summary" },
  { key: "productUpdates", label: "Product updates" },
];

export default function Settings() {
  const { user } = useAuth();
  const updateProfile = useUpdateProfile();
  const changePassword = useChangePassword();
  const { data: preferences } = useNotificationPreferences();
  const updatePreferences = useUpdateNotificationPreferences();

  const [fullName, setFullName] = useState(user?.fullName ?? "");
  const [profileMessage, setProfileMessage] = useState<string | null>(null);

  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [passwordMessage, setPasswordMessage] = useState<{ type: "success" | "error"; text: string } | null>(null);

  const [localPrefs, setLocalPrefs] = useState<NotificationPreferences | null>(null);
  const [prefsSaved, setPrefsSaved] = useState(false);

  useEffect(() => {
    if (preferences) setLocalPrefs(preferences);
  }, [preferences]);

  const handleSaveProfile = () => {
    setProfileMessage(null);
    updateProfile.mutate(fullName, {
      onSuccess: () => setProfileMessage("Profile updated."),
      onError: () => setProfileMessage("Couldn't save changes. Please try again."),
    });
  };

  const handleUpdatePassword = () => {
    setPasswordMessage(null);
    if (newPassword.length < 8) {
      setPasswordMessage({ type: "error", text: "New password must be at least 8 characters." });
      return;
    }
    changePassword.mutate(
      { currentPassword, newPassword },
      {
        onSuccess: () => {
          setPasswordMessage({ type: "success", text: "Password updated. Other logged-in devices have been signed out." });
          setCurrentPassword("");
          setNewPassword("");
        },
        onError: (err: any) => {
          setPasswordMessage({
            type: "error",
            text: err?.response?.data?.error ?? "Couldn't update password. Check your current password and try again.",
          });
        },
      }
    );
  };

  const togglePreference = (key: keyof NotificationPreferences) => {
    setLocalPrefs((prev) => (prev ? { ...prev, [key]: !prev[key] } : prev));
    setPrefsSaved(false);
  };

  const handleSavePreferences = () => {
    if (!localPrefs) return;
    updatePreferences.mutate(localPrefs, { onSuccess: () => setPrefsSaved(true) });
  };

  return (
    <div className="space-y-6 max-w-2xl">
      <SectionHeading title="Settings" description="Manage your account and preferences" />

      <Card className="p-6 space-y-4">
        <h3 className="font-semibold text-text-primary">Profile</h3>
        <div>
          <label className="block text-sm font-medium text-text-primary mb-1.5">Full name</label>
          <input
            value={fullName}
            onChange={(e) => setFullName(e.target.value)}
            className="w-full h-11 px-3 rounded-md border border-border-subtle text-sm"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-text-primary mb-1.5">Email</label>
          <input defaultValue={user?.email} disabled className="w-full h-11 px-3 rounded-md border border-border-subtle text-sm bg-bg-alt text-text-secondary" />
        </div>
        {profileMessage && <p className="text-sm text-text-secondary">{profileMessage}</p>}
        <Button onClick={handleSaveProfile} disabled={updateProfile.isPending}>
          {updateProfile.isPending ? "Saving..." : "Save changes"}
        </Button>
      </Card>

      <Card className="p-6 space-y-4">
        <h3 className="font-semibold text-text-primary">Notifications</h3>
        {notificationLabels.map(({ key, label }) => (
          <label key={key} className="flex items-center justify-between text-sm text-text-primary">
            {label}
            <input
              type="checkbox"
              checked={localPrefs?.[key] ?? true}
              onChange={() => togglePreference(key)}
              className="h-5 w-5 rounded border-border-subtle"
            />
          </label>
        ))}
        {prefsSaved && <p className="text-sm text-state-success">Preferences saved.</p>}
        <Button variant="secondary" onClick={handleSavePreferences} disabled={updatePreferences.isPending || !localPrefs}>
          {updatePreferences.isPending ? "Saving..." : "Save preferences"}
        </Button>
      </Card>

      <Card className="p-6 space-y-3">
        <h3 className="font-semibold text-text-primary">Password</h3>
        <input
          type="password"
          placeholder="Current password"
          value={currentPassword}
          onChange={(e) => setCurrentPassword(e.target.value)}
          className="w-full h-11 px-3 rounded-md border border-border-subtle text-sm"
        />
        <input
          type="password"
          placeholder="New password"
          value={newPassword}
          onChange={(e) => setNewPassword(e.target.value)}
          className="w-full h-11 px-3 rounded-md border border-border-subtle text-sm"
        />
        {passwordMessage && (
          <p className={`text-sm ${passwordMessage.type === "success" ? "text-state-success" : "text-state-error"}`}>
            {passwordMessage.text}
          </p>
        )}
        <Button variant="secondary" onClick={handleUpdatePassword} disabled={changePassword.isPending}>
          {changePassword.isPending ? "Updating..." : "Update password"}
        </Button>
      </Card>
    </div>
  );
}
