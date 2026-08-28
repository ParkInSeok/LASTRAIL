import { NavLink, Route, Routes } from "react-router-dom";
import { AdminProvider, useAdmin } from "../shared/auth/AdminContext";
import { visibleNavigation } from "../shared/navigation/navigation";
import { AuditPage } from "../pages/AuditPage";
import { DashboardPage } from "../pages/DashboardPage";
import { ErrorLogsPage } from "../pages/ErrorLogsPage";
import { GachaPage } from "../pages/GachaPage";
import { IntegrityPage } from "../pages/IntegrityPage";
import { OfflineRewardsPage } from "../pages/OfflineRewardsPage";
import { PlayerDetailPage } from "../pages/PlayerDetailPage";
import { PlayersPage } from "../pages/PlayersPage";
import { RunDetailPage } from "../pages/RunDetailPage";
import { RunsPage } from "../pages/RunsPage";
import { SupportAccountPage } from "../pages/SupportAccountPage";
import { SuspiciousPlayerDetailPage } from "../pages/SuspiciousPlayerDetailPage";
import { SuspiciousPlayersPage } from "../pages/SuspiciousPlayersPage";

function Shell() {
  const { bootstrap, isLoading, error } = useAdmin();
  const permissions = bootstrap?.admin.permissions ?? [];
  const groups = visibleNavigation(permissions);

  if (isLoading) return <main className="center">운영 콘솔 초기 정보를 불러오는 중</main>;
  if (error) return <main className="center error">운영 콘솔 초기화 실패</main>;

  return (
    <div className="shell">
      <aside className="sidebar">
        <div className="brand">LAST RAIL 운영 콘솔</div>
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
        <Route path="/support/accounts/:accountId" element={<SupportAccountPage />} />
        <Route path="/gacha" element={<GachaPage />} />
        <Route path="/offline-rewards" element={<OfflineRewardsPage />} />
        <Route path="/ops/error-logs" element={<ErrorLogsPage />} />
        <Route path="/integrity" element={<IntegrityPage />} />
        <Route path="/integrity/suspicious-players" element={<SuspiciousPlayersPage />} />
        <Route path="/integrity/suspicious-players/:accountId" element={<SuspiciousPlayerDetailPage />} />
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
