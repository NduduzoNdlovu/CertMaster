import { useState } from "react";
import { Bell, Menu, MessageSquare, Search, Settings, LogOut, GraduationCap, FileQuestion, Award, BookOpen } from "lucide-react";
import { useAuth } from "../../context/AuthContext";
import { useNavigate } from "react-router-dom";
import { useNotifications, useMarkNotificationRead, useSearch } from "../../hooks/useApiData";

export function Header({ onMenuClick }: { onMenuClick: () => void }) {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [profileOpen, setProfileOpen] = useState(false);
  const [notifOpen, setNotifOpen] = useState(false);
  const [searchOpen, setSearchOpen] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");

  const { data: notifications } = useNotifications();
  const markRead = useMarkNotificationRead();
  const { data: searchResults, isFetching: isSearching } = useSearch(searchTerm);
  const unreadCount = notifications?.filter((n) => !n.isRead).length ?? 0;

  const iconForResultType = (type: string) => {
    if (type === "Certification") return <Award size={16} className="text-brand-primary shrink-0" />;
    if (type === "Topic") return <BookOpen size={16} className="text-brand-primary shrink-0" />;
    return <FileQuestion size={16} className="text-brand-primary shrink-0" />;
  };

  const goToResult = (result: NonNullable<typeof searchResults>[number]) => {
    setSearchOpen(false);
    setSearchTerm("");
    if (result.type === "Certification") navigate(`/practice?cert=${result.certificationId}`);
    else navigate(`/practice?cert=${result.certificationId ?? ""}`);
  };

  return (
    <header className="h-[70px] fixed top-0 left-0 right-0 z-30 bg-bg-card border-b border-border-subtle flex items-center px-4 md:px-6 gap-4">
      <button
        onClick={onMenuClick}
        className="lg:hidden h-11 w-11 flex items-center justify-center rounded-md text-text-primary hover:bg-bg-alt"
        aria-label="Open navigation menu"
      >
        <Menu size={22} />
      </button>

      <div className="flex items-center gap-2 shrink-0">
        <div className="h-9 w-9 rounded-md bg-brand-primary flex items-center justify-center">
          <GraduationCap size={20} className="text-white" />
        </div>
        <span className="font-extrabold text-lg text-text-primary tracking-tight hidden sm:block">
          CertMaster
        </span>
      </div>

      <div className="flex-1 max-w-xl hidden md:flex items-center relative">
        <Search size={18} className="absolute left-3 text-text-secondary z-10" />
        <input
          type="search"
          placeholder="Search questions, topics, certifications..."
          value={searchTerm}
          onChange={(e) => { setSearchTerm(e.target.value); setSearchOpen(true); }}
          onFocus={() => setSearchOpen(true)}
          onBlur={() => setTimeout(() => setSearchOpen(false), 150)}
          className="w-full h-11 pl-10 pr-4 rounded-md bg-bg-alt border border-border-subtle text-sm text-text-primary placeholder:text-text-secondary focus:outline-none focus:ring-2 focus:ring-brand-primary/40"
        />

        {searchOpen && searchTerm.trim().length >= 2 && (
          <div className="absolute top-12 left-0 right-0 bg-bg-card border border-border-subtle rounded-md shadow-lg py-1 max-h-96 overflow-y-auto">
            {isSearching && <p className="px-4 py-3 text-sm text-text-secondary">Searching...</p>}
            {!isSearching && searchResults?.length === 0 && (
              <p className="px-4 py-3 text-sm text-text-secondary">No results for "{searchTerm}"</p>
            )}
            {searchResults?.map((r) => (
              <button
                key={`${r.type}-${r.id}`}
                onMouseDown={() => goToResult(r)}
                className="w-full flex items-start gap-2 px-4 py-2.5 text-left hover:bg-bg-alt"
              >
                {iconForResultType(r.type)}
                <span className="min-w-0">
                  <span className="block text-sm text-text-primary truncate">{r.title}</span>
                  <span className="block text-xs text-text-secondary truncate">{r.subtitle}</span>
                </span>
              </button>
            ))}
          </div>
        )}
      </div>

      <div className="flex items-center gap-1 ml-auto">
        <button className="h-11 w-11 hidden sm:flex items-center justify-center rounded-md text-text-primary hover:bg-bg-alt relative" aria-label="Messages">
          <MessageSquare size={20} />
        </button>

        <div className="relative">
          <button
            onClick={() => setNotifOpen((v) => !v)}
            className="h-11 w-11 flex items-center justify-center rounded-md text-text-primary hover:bg-bg-alt relative"
            aria-label="Notifications"
          >
            <Bell size={20} />
            {unreadCount > 0 && (
              <span className="absolute top-2 right-2 h-2 w-2 rounded-full bg-brand-accent" />
            )}
          </button>

          {notifOpen && (
            <div className="absolute right-0 mt-2 w-80 bg-bg-card border border-border-subtle rounded-md shadow-lg py-1 max-h-96 overflow-y-auto">
              <div className="px-4 py-2 flex items-center justify-between border-b border-border-subtle">
                <span className="text-sm font-semibold text-text-primary">Notifications</span>
                {unreadCount > 0 && <span className="text-xs text-brand-primary font-medium">{unreadCount} new</span>}
              </div>
              {notifications?.length === 0 && (
                <p className="px-4 py-4 text-sm text-text-secondary">You're all caught up.</p>
              )}
              {notifications?.map((n) => (
                <button
                  key={n.id}
                  onClick={() => markRead.mutate(n.id)}
                  className={`w-full text-left px-4 py-3 border-b border-border-subtle last:border-0 hover:bg-bg-alt ${!n.isRead ? "bg-bg-alt/60" : ""}`}
                >
                  <div className="flex items-center justify-between gap-2">
                    <span className="text-sm font-medium text-text-primary">{n.title}</span>
                    {!n.isRead && <span className="h-1.5 w-1.5 rounded-full bg-brand-accent shrink-0" />}
                  </div>
                  <p className="text-xs text-text-secondary mt-0.5">{n.body}</p>
                </button>
              ))}
            </div>
          )}
        </div>

        <div className="relative ml-1">
          <button
            onClick={() => setProfileOpen((v) => !v)}
            className="flex items-center gap-2 h-11 pl-1 pr-2 rounded-md hover:bg-bg-alt"
          >
            <div className="h-8 w-8 rounded-full bg-brand-secondary text-white flex items-center justify-center text-sm font-semibold">
              {user?.fullName?.charAt(0) ?? "U"}
            </div>
            <span className="text-sm font-medium text-text-primary hidden md:block">
              {user?.fullName ?? "User"}
            </span>
          </button>

          {profileOpen && (
            <div className="absolute right-0 mt-2 w-52 bg-bg-card border border-border-subtle rounded-md shadow-lg py-1">
              <button
                onClick={() => { setProfileOpen(false); navigate("/settings"); }}
                className="w-full flex items-center gap-2 px-4 py-2.5 text-sm text-text-primary hover:bg-bg-alt text-left"
              >
                <Settings size={16} /> Settings
              </button>
              <button
                onClick={() => { logout(); navigate("/login"); }}
                className="w-full flex items-center gap-2 px-4 py-2.5 text-sm text-state-error hover:bg-bg-alt text-left"
              >
                <LogOut size={16} /> Log out
              </button>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}
