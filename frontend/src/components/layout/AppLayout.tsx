import { useState } from "react";
import { Outlet } from "react-router-dom";
import { Header } from "./Header";
import { Sidebar } from "./Sidebar";
import { BottomNav, MobileDrawer } from "./MobileNav";

export function AppLayout() {
  const [collapsed, setCollapsed] = useState(false);
  const [drawerOpen, setDrawerOpen] = useState(false);

  return (
    <div className="min-h-screen bg-bg-base">
      <Header onMenuClick={() => setDrawerOpen(true)} />
      <Sidebar collapsed={collapsed} onToggle={() => setCollapsed((v) => !v)} />
      <MobileDrawer open={drawerOpen} onClose={() => setDrawerOpen(false)} />

      <main
        className={`pt-[70px] pb-20 lg:pb-6 transition-all duration-200 ${collapsed ? "lg:ml-[76px]" : "lg:ml-64"}`}
      >
        <div className="p-4 md:p-6 max-w-7xl mx-auto">
          <Outlet />
        </div>
      </main>

      <BottomNav />
    </div>
  );
}
