import { NavLink, Route, Routes } from "react-router-dom";
import type { AdminPermission, BootstrapResponse } from "@lastrail/admin-contracts";
import { AdminProvider, useAdmin } from "../shared/auth/AdminContext";
import { adminRequestFailureDisplay } from "../shared/api/errorDisplay";
import { navigationItemUsesExactActiveMatch, visibleNavigation } from "../shared/navigation/navigation";
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

const fallbackShellPermissions: AdminPermission[] = [
  "ACCOUNT_READ",
  "RUN_READ",
  "ECONOMY_READ",
  "OFFLINE_REWARD_READ",
  "AUDIT_READ",
  "INTEGRITY_READ",
  "INCIDENT_READ",
  "ENFORCEMENT_SUSPEND",
  "ANALYTICS_READ",
  "BALANCE_READ"
];

type ShellStateInput = {
  bootstrap?: BootstrapResponse;
  isLoading: boolean;
  error: Error | null;
};

type ShellState = {
  shouldRenderShell: boolean;
  permissions: readonly AdminPermission[];
  identityDisplayName: string;
  environmentLabel: string;
  bootstrapStatus?: string;
  bootstrapNextStep?: string;
};

export function shellStateFromAdminContext({ bootstrap, isLoading, error }: ShellStateInput): ShellState {
  if (isLoading) {
    return {
      shouldRenderShell: false,
      permissions: [],
      identityDisplayName: "Admin 확인 중",
      environmentLabel: "dev"
    };
  }

  if (bootstrap) {
    return {
      shouldRenderShell: true,
      permissions: bootstrap.admin.permissions,
      identityDisplayName: bootstrap.admin.displayName,
      environmentLabel: bootstrap.environment
    };
  }

  if (error) {
    const failure = adminRequestFailureDisplay(error, {
      resourceLabel: "운영 콘솔 초기 정보",
      resourceId: "/api/bootstrap"
    });

    return {
      shouldRenderShell: true,
      permissions: fallbackShellPermissions,
      identityDisplayName: "Admin 확인 필요",
      environmentLabel: "dev",
      bootstrapStatus: failure.reason,
      bootstrapNextStep: failure.nextStep
    };
  }

  return {
    shouldRenderShell: true,
    permissions: fallbackShellPermissions,
    identityDisplayName: "Admin 확인 필요",
    environmentLabel: "dev",
    bootstrapStatus: "연동 대기",
    bootstrapNextStep: "Admin bootstrap 응답을 기다리는 동안 읽기 전용 셸을 표시합니다."
  };
}

function Shell() {
  const { bootstrap, isLoading, error } = useAdmin();
  const shellState = shellStateFromAdminContext({ bootstrap, isLoading, error });
  const permissions = shellState.permissions;
  const groups = visibleNavigation(permissions);
  const navigationItems = groups.flatMap((group) => group.items);

  if (!shellState.shouldRenderShell) return <main className="center">운영 콘솔 초기 정보를 불러오는 중</main>;

  return (
    <div className="shell">
      <aside className="sidebar">
        <div className="brand">LAST RAIL 운영 콘솔</div>
        <div className="identity">
          <strong>{shellState.identityDisplayName}</strong>
          <span>{shellState.environmentLabel}</span>
        </div>
        <nav>
          {groups.map((group) => (
            <section key={group.label} className="nav-group">
              <h2>{group.label}</h2>
              {group.items.map((item) => (
                <NavLink
                  key={item.path}
                  to={item.path}
                  end={navigationItemUsesExactActiveMatch(item.path, navigationItems)}
                >
                  {item.label}
                </NavLink>
              ))}
            </section>
          ))}
        </nav>
      </aside>
      <div className="main-area">
        {shellState.bootstrapStatus ? (
          <section className="admin-panel shell-status-banner" role="status" aria-label="운영 콘솔 초기화 상태">
            <span>초기 정보</span>
            <strong>{shellState.bootstrapStatus}</strong>
            <small>{shellState.bootstrapNextStep}</small>
          </section>
        ) : null}
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
