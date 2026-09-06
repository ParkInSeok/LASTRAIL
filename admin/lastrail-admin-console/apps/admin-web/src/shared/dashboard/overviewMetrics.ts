import { dailySeries, type DailyMetrics } from "./dailyMetrics";

export function overviewMetrics(state: { activeSessions?: string; loading?: boolean; error?: string; daily?: DailyMetrics | null; calculatedAt?: string }) {
  const pending = "집계 미연동";
  const sessionValue = state.error ?? (state.loading ? "조회 중" : state.activeSessions ?? "없음");
  const endDay = state.calculatedAt && Number.isFinite(Date.parse(state.calculatedAt)) ? new Date(Date.parse(state.calculatedAt) + 9 * 3600000).toISOString().slice(0, 10) : null;
  const signup = dailySeries(state.daily, "signups", 7, state.calculatedAt ?? "").find(row => row.date === endDay);
  const signupValue = state.error ?? (state.loading ? "조회 중" : !state.daily ? pending : !state.daily.available.signups ? "집계 원천 없음" : signup ? String(signup.value) : "데이터 없음");
  return [
    { title: "접속자", metrics: [
      { label: "오늘 활성 이용자", value: pending },
      { label: "활성 세션", value: sessionValue }
    ] },
    { title: "가입자", metrics: [
      { label: "오늘 신규 가입자", value: signupValue },
      { label: "전체 가입자", value: pending }
    ] },
    { title: "광고", metrics: [
      { label: "오늘 시청 수", value: pending },
      { label: "오늘 시청 이용자", value: pending },
      { label: "광고 수익", value: state.daily ? "수익 집계 원천 없음" : pending }
    ] },
    { title: "패스 결제율", metrics: [
      { label: "오늘 패스 구매자 / 오늘 활성 이용자", value: pending }
    ] },
    { title: "가입자 대비 접속률", metrics: [
      { label: "오늘 활성 이용자 / 전체 가입자", value: pending }
    ] }
  ];
}
