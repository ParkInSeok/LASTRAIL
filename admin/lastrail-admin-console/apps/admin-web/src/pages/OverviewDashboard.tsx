import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { adminApi } from "../shared/api/client";
import { adminRequestFailureDisplay } from "../shared/api/errorDisplay";
import { overviewMetrics } from "../shared/dashboard/overviewMetrics";
import { overviewChartDefinitions, overviewDateLabels } from "../shared/dashboard/overviewCharts";
import "./overviewDashboard.css";

export function OverviewDashboard() {
  const [days, setDays] = useState<7 | 30>(7);
  const [from, to] = overviewDateLabels(days);
  const query = useQuery({ queryKey: ["dashboard-launch-summary"], queryFn: adminApi.dashboardLaunchSummary, retry: false });
  const groups = overviewMetrics({
    activeSessions: query.data?.activeSessions,
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
      <div className="overview-chart-grid">{overviewChartDefinitions.map(chart => <figure key={chart.title} className="overview-chart" aria-label={`${chart.title}, 최근 ${days}일, 일별 집계 미연동`}>
        <figcaption><span><i style={{ backgroundColor: chart.color }} aria-hidden="true" />{chart.title}</span><small>{chart.unit}</small></figcaption>
        <div className="overview-plot">
          <div className="overview-gridlines" aria-hidden="true">{[0, 1, 2, 3].map(line => <span key={line} />)}</div>
          <p>일별 집계 미연동</p>
        </div>
        <div className="overview-axis" aria-hidden="true"><span>{from}</span><span>{to}</span></div>
      </figure>)}</div>
    </section>
  </main>;
}
