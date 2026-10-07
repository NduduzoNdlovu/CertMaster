import { NavLink } from "react-router-dom";
import {
  LayoutDashboard,
  Dumbbell,
  FileClock,
  BarChart3,
  TrendingUp,
  Bookmark,
  Award,
  Trophy,
  Crown,
  Layers,
  Settings,
  Bell,
  HelpCircle,
  ChevronsLeft,
  ChevronsRight,
  Users,
  Database,
  Wrench,
  CreditCard,
  ScrollText,
  Flag,
} from "lucide-react";
import { useAuth } from "../../context/AuthContext";

const learnerNav = [
  { to: "/dashboard", label: "Dashboard", icon: LayoutDashboard },
  { to: "/practice", label: "Practice", icon: Dumbbell },
  { to: "/mock-exams", label: "Mock Exams", icon: FileClock },
  { to: "/results", label: "My Results", icon: BarChart3 },
  { to: "/progress", label: "Progress", icon: TrendingUp },
  { to: "/bookmarks", label: "Bookmarks", icon: Bookmark },
  { to: "/flashcards", label: "Flashcards", icon: Layers },
  { to: "/certifications", label: "Certifications", icon: Award },
  { to: "/leaderboard", label: "Leaderboard", icon: Trophy },
  { to: "/premium", label: "Premium", icon: Crown },
  { to: "/notifications", label: "Notifications", icon: Bell },
];

const learnerFooterNav = [
  { to: "/settings", label: "Settings", icon: Settings },
  { to: "/help", label: "Help", icon: HelpCircle },
];

const adminNav = [
  { to: "/admin", label: "Overview", icon: LayoutDashboard },
  { to: "/admin/users", label: "Users", icon: Users },
  { to: "/admin/question-banks", label: "Question Banks", icon: Database },
  { to: "/admin/reports", label: "Reported Questions", icon: Flag },
  { to: "/admin/analytics", label: "Analytics", icon: BarChart3 },
  { to: "/admin/maintenance", label: "Maintenance", icon: Wrench },
  { to: "/admin/payments", label: "Payments", icon: CreditCard },
  { to: "/admin/logs", label: "Logs", icon: ScrollText },
];

export function Sidebar({ collapsed, onToggle }: { collapsed: boolean; onToggle: () => void }) {
  const { user } = useAuth();
  const isAdmin = user?.role === "Administrator";
  const navItems = isAdmin ? adminNav : learnerNav;

  return (
    <aside
      className={`hidden lg:flex flex-col fixed top-[70px] left-0 bottom-0 bg-brand-primary text-white transition-all duration-200 ${collapsed ? "w-[76px]" : "w-64"}`}
    >
      <nav className="flex-1 overflow-y-auto no-scrollbar py-4 px-2 space-y-1">
        {navItems.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.to === "/admin"}
            className={({ isActive }) =>
              `flex items-center gap-3 h-11 px-3 rounded-md text-sm font-medium transition-colors ${
                isActive ? "bg-white/15 text-white" : "text-white/80 hover:bg-white/10 hover:text-white"
              }`
            }
            title={collapsed ? item.label : undefined}
          >
            <item.icon size={20} className="shrink-0" />
            {!collapsed && <span>{item.label}</span>}
          </NavLink>
        ))}
      </nav>

      {!isAdmin && (
        <div className="px-2 py-2 space-y-1 border-t border-white/10">
          {learnerFooterNav.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                `flex items-center gap-3 h-11 px-3 rounded-md text-sm font-medium transition-colors ${
                  isActive ? "bg-white/15 text-white" : "text-white/80 hover:bg-white/10 hover:text-white"
                }`
              }
              title={collapsed ? item.label : undefined}
            >
              <item.icon size={20} className="shrink-0" />
              {!collapsed && <span>{item.label}</span>}
            </NavLink>
          ))}
        </div>
      )}

      <button
        onClick={onToggle}
        className="h-12 flex items-center justify-center text-white/70 hover:text-white hover:bg-white/10 border-t border-white/10"
        aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"}
      >
        {collapsed ? <ChevronsRight size={20} /> : <ChevronsLeft size={20} />}
      </button>
    </aside>
  );
}
