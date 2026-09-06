import { type RunVersionEvidence as Evidence, versionSummary } from "./runVersions";

export function RunVersionEvidence({ value }: { value: Evidence }) {
  return <section aria-label="출격 버전 기록"><h2>출격 버전 기록</h2><dl>
    {(["start", "finish"] as const).map(key => <div key={key}>
      <dt>{key === "start" ? "시작 승인 시점" : "종료 승인 시점"}</dt>
      <dd>{versionSummary(value[key])}</dd>
      {value[key].snapshot && <dd>콘텐츠 {value[key].snapshot.contentVersion ?? "미기록"} · 기록 시각 {value[key].snapshot.capturedAt ?? "미기록"}</dd>}
    </div>)}
  </dl></section>;
}
