import { NavLink, Route, Routes } from "react-router-dom";
import { AdminProvider, useAdmin } from "../shared/auth/AdminContext";
import { visibleNavigation } from "../shared/navigation/navigation";
import { AuditPage } from "../pages/AuditPage";
import { DashboardPage } from "../pages/DashboardPage";
import { IntegrityPage } from "../pages/IntegrityPage";
import { PlayerDetailPage } from "../pages/PlayerDetailPage";
import { PlayersPage } from "../pages/PlayersPage";
import { RunDetailPage } from "../pages/RunDetailPage";
import { RunsPage } from "../pages/RunsPage";

function Shell() {
  const { bootstrap, isLoading, error } = useAdmin();
  const permissions = bootstrap?.admin.permissions ?? [];
  const groups = visibleNavigation(permissions);

  if (isLoading) return <main className="center">Loading admin bootstrap</main>;
  if (error) return <main className="center error">Admin bootstrap failed</main>;

  return (
    <div className="shell">
      <aside className="sidebar">
        <div className="brand">LAST RAIL Admin</div>
        <div className="identity">
          <strong>{bootstrap?.admin.displayName}</strong>
          <span>{bootstrap?.environment}</span>
        </div>
        <nav>
          {groups.map((group) => (
            <section key={group.label} className="nav-group">
              <h2>{group.label}</h2>
              {group.items.map((item) => (
                <NavLink key={item.path} to={item.path}>
                  {item.label}
                </NavLink>
              ))}
            </section>
          ))}
        </nav>
      </aside>
      <Routes>
        <Route path="/" element={<DashboardPage />} />
        <Route path="/players" element={<PlayersPage />} />
        <Route path="/players/:accountId" element={<PlayerDetailPage />} />
        <Route path="/integrity" element={<IntegrityPage />} />
        <Route path="/runs" element={<RunsPage />} />
        <Route path="/runs/:runId" element={<RunDetailPage />} />
        <Route path="/audit" element={<AuditPage />} />
      </Routes>
    </div>
  );
}

export function App() {
  return (
    <AdminProvider>
      <Shell />
    </AdminProvider>
  );
}
