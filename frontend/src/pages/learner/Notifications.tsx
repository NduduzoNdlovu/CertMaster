import { CheckCheck, Bell } from "lucide-react";
import { useNotifications, useMarkNotificationRead, useMarkAllNotificationsRead } from "../../hooks/useApiData";
import { Card, SectionHeading } from "../../components/ui/Primitives";
import { Button } from "../../components/ui/Button";

export default function Notifications() {
  const { data = [], isLoading } = useNotifications();
  const markRead = useMarkNotificationRead();
  const markAllRead = useMarkAllNotificationsRead();
  const unread = data.filter(n => !n.isRead).length;

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <SectionHeading title="Notifications" description="Updates about your learning, exams and account." />
        <Button variant="secondary" size="sm" onClick={() => markAllRead.mutate()} disabled={!unread || markAllRead.isPending}>
          <CheckCheck size={16} /> {markAllRead.isPending ? "Marking..." : "Mark all as read"}
        </Button>
      </div>

      <Card className="p-0 overflow-hidden">
        {isLoading && <p className="p-6 text-sm text-text-secondary">Loading notifications...</p>}
        {!isLoading && data.length === 0 && (
          <div className="p-10 text-center">
            <Bell size={30} className="mx-auto mb-3 text-text-secondary" />
            <p className="text-sm text-text-secondary">You're all caught up.</p>
          </div>
        )}
        {data.map(notification => (
          <button
            key={notification.id}
            onClick={() => !notification.isRead && markRead.mutate(notification.id)}
            className={`w-full text-left p-5 border-b border-border-subtle last:border-0 hover:bg-bg-alt ${
              !notification.isRead ? "bg-bg-alt/60" : ""
            }`}
          >
            <div className="flex items-start gap-3">
              <span className={`mt-1.5 h-2 w-2 rounded-full shrink-0 ${notification.isRead ? "bg-border-subtle" : "bg-brand-accent"}`} />
              <div className="min-w-0 flex-1">
                <div className="flex items-center justify-between gap-3">
                  <p className="font-semibold text-text-primary">{notification.title}</p>
                  <time className="text-xs text-text-secondary shrink-0">
                    {new Date(notification.createdAtUtc).toLocaleString()}
                  </time>
                </div>
                <p className="text-sm text-text-secondary mt-1">{notification.body}</p>
              </div>
            </div>
          </button>
        ))}
      </Card>
    </div>
  );
}
