import { dailySeries, type DailyMetrics, type DailyKey } from "./dailyMetrics";

const charts: Array<{ key: DailyKey; title: string; color: string }> = [
  { key: "signups", title: "신규 가입자", color: "#4267b2" },
  { key: "sessionCreatedAccounts", title: "접속 세션 생성 계정 (활동 DAU 아님)", color: "#087e8b" },
  { key: "adRewardsVerified", title: "광고 보상 검증 건수", color: "#a36812" },
  { key: "adRewardsGranted", title: "광고 보상 지급 건수", color: "#278050" }
];
export function DailyCharts({ data, days, calculatedAt, state }: { data?: DailyMetrics | null; days: 7 | 30; calculatedAt: string; state?: string }) {
  return <div className="overview-chart-grid">{charts.map(chart => {
    const points = dailySeries(data, chart.key, days, calculatedAt);
    const message = state ?? (!data ? "일별 집계 미연동" : !data.available[chart.key] ? "집계 원천 없음" : points.length === 0 ? "데이터 없음" : null);
    const max = Math.max(1, ...points.map(point => point.value));
    return <figure className="overview-chart" key={chart.key} aria-label={chart.title}>
      <figcaption><span><i style={{ backgroundColor: chart.color }} aria-hidden="true" />{chart.title}</span></figcaption>
      <div className="overview-plot">
        {message ? <p>{message}</p> : <div className="daily-bars">{points.map(point => <div className="daily-bar-slot" key={point.date} title={`${point.date} KST: ${point.value}`} aria-label={`${point.date}: ${point.value}`}>
          <span className="daily-bar" style={{ height: `${point.value / max * 100}%`, backgroundColor: chart.color }} />
          <span className="daily-value">{point.value}</span>
        </div>)}</div>}
      </div>
      {!message && <><div className="overview-axis"><span>{points[0]?.date}</span><span>{points.at(-1)?.date}</span></div>
        <details><summary>일별 값</summary><table><thead><tr><th>날짜 (KST)</th><th>값</th></tr></thead><tbody>{points.map(point => <tr key={point.date}><td>{point.date}</td><td>{point.value}</td></tr>)}</tbody></table></details></>}
    </figure>;
  })}</div>;
}
