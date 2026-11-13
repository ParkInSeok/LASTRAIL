export const overviewChartDefinitions = [
  { title: "접속자 추이", unit: "명", color: "#087e8b" },
  { title: "신규 가입자 추이", unit: "명", color: "#4267b2" },
  { title: "광고 시청 추이", unit: "회", color: "#a36812" },
  { title: "광고 수익 추이", unit: "통화 미확정", color: "#278050" },
  { title: "패스 결제율 추이", unit: "%", color: "#a54868" },
  { title: "가입자 대비 접속률 추이", unit: "%", color: "#626c78" }
] as const;

export function overviewDateLabels(days: 7 | 30, now = new Date()): [string, string] {
  const kst = new Date(now.getTime() + 9 * 60 * 60 * 1000);
  const end = Date.UTC(kst.getUTCFullYear(), kst.getUTCMonth(), kst.getUTCDate());
  const label = (time: number) => new Date(time).toISOString().slice(5, 10).replace("-", ".");
  return [label(end - (days - 1) * 86400000), label(end)];
}
