import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { adminApi } from "../shared/api/client";
import { adminRequestFailureDisplay } from "../shared/api/errorDisplay";
import { overviewMetrics } from "../shared/dashboard/overviewMetrics";
import { DailyCharts } from "../shared/dashboard/DailyCharts";
import "./overviewDashboard.css";

export function OverviewDashboard() {
  const [days, setDays] = useState<7 | 30>(7);
  const query = useQuery({ queryKey: ["dashboard-launch-summary"], queryFn: adminApi.dashboardLaunchSummary, retry: false });
  const groups = overviewMetrics({
    activeSessions: query.data?.activeSessions,
    daily: query.data?.dailySourceMetrics,
    calculatedAt: query.data?.serverCalculatedAt,
    loading: query.isLoading,
    error: query.error ? adminRequestFailureDisplay(query.error, { resourceLabel: "개요", resourceId: "/api/dashboard/launch-summary" }).reason : undefined
  });
  return <main className="content">
    <header><h1>개요</h1></header>
    <div className="dashboard-sections">{groups.map(group => <section className="admin-panel" key={group.title}>
      <h2>{group.title}</h2>
      <div className="metric-grid">{group.metrics.map(metric => <div className="metric-card" key={metric.label}>
        <span>{metric.label}</span><strong>{metric.value}</strong>
      </div>)}</div>
    </section>)}</div>
    <section className="overview-trends" aria-label="일별 추이">
      <header className="overview-trend-header">
        <h2>일별 추이 <small>KST</small></h2>
        <div className="overview-range" role="group" aria-label="조회 기간">
          {([7, 30] as const).map(value => <button key={value} type="button" aria-pressed={days === value} onClick={() => setDays(value)}>최근 {value}일</button>)}
        </div>
      </header>
      <DailyCharts data={query.data?.dailySourceMetrics} days={days} calculatedAt={query.data?.serverCalculatedAt ?? ""} state={query.isLoading ? "조회 중" : query.error ? adminRequestFailureDisplay(query.error, { resourceLabel: "일별 지표", resourceId: "/api/dashboard/launch-summary" }).reason : undefined} />
    </section>
  </main>;
}
