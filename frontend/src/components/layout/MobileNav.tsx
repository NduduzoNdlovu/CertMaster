import { NavLink } from "react-router-dom";
import { LayoutDashboard, Dumbbell, FileClock, TrendingUp, X, Bookmark, Award, Trophy, Crown, Settings, HelpCircle, Layers } from "lucide-react";
import { useAuth } from "../../context/AuthContext";

const bottomItems = [
  { to: "/dashboard", label: "Home", icon: LayoutDashboard },
  { to: "/practice", label: "Practice", icon: Dumbbell },
  { to: "/mock-exams", label: "Exams", icon: FileClock },
  { to: "/progress", label: "Progress", icon: TrendingUp },
];

export function BottomNav() {
  return (
    <nav className="lg:hidden fixed bottom-0 left-0 right-0 z-30 bg-bg-card border-t border-border-subtle flex items-stretch h-16">
      {bottomItems.map((item) => (
        <NavLink
          key={item.to}
          to={item.to}
          className={({ isActive }) =>
            `flex-1 flex flex-col items-center justify-center gap-0.5 text-xs font-medium ${
              isActive ? "text-brand-primary" : "text-text-secondary"
            }`
          }
        >
          <item.icon size={22} />
          {item.label}
        </NavLink>
      ))}
    </nav>
  );
}

const drawerExtraItems = [
  { to: "/bookmarks", label: "Bookmarks", icon: Bookmark },
  { to: "/flashcards", label: "Flashcards", icon: Layers },
  { to: "/certifications", label: "Certifications", icon: Award },
  { to: "/leaderboard", label: "Leaderboard", icon: Trophy },
  { to: "/premium", label: "Premium", icon: Crown },
  { to: "/settings", label: "Settings", icon: Settings },
  { to: "/help", label: "Help", icon: HelpCircle },
];

export function MobileDrawer({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { user } = useAuth();
  if (!open) return null;

  return (
    <div className="lg:hidden fixed inset-0 z-40">
      <div className="absolute inset-0 bg-black/40" onClick={onClose} />
      <div className="absolute top-0 left-0 bottom-0 w-72 bg-brand-primary text-white flex flex-col shadow-xl animate-in">
        <div className="h-[70px] flex items-center justify-between px-4 border-b border-white/10">
          <div>
            <p className="font-semibold">{user?.fullName}</p>
            <p className="text-xs text-white/70">{user?.role}</p>
          </div>
          <button onClick={onClose} className="h-11 w-11 flex items-center justify-center" aria-label="Close menu">
            <X size={22} />
          </button>
        </div>
        <nav className="flex-1 overflow-y-auto py-3 px-2 space-y-1">
          {drawerExtraItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              onClick={onClose}
              className={({ isActive }) =>
                `flex items-center gap-3 h-12 px-3 rounded-md text-sm font-medium ${
                  isActive ? "bg-white/15" : "text-white/85 hover:bg-white/10"
                }`
              }
            >
              <item.icon size={20} />
              {item.label}
            </NavLink>
          ))}
        </nav>
      </div>
    </div>
  );
}
