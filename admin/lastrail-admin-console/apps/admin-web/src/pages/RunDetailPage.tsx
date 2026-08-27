import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { adminApi } from "../shared/api/client";
import { levelSourceLabel } from "../shared/labels/adminLabels";

type RunRiskRewardPlaceholder = {
  label: string;
  value: "연동 대기";
  note: string;
};

export const runRiskRewardPlaceholders: RunRiskRewardPlaceholder[] = [
  { label: "위험도", value: "연동 대기", note: "서버 risk tier 기준" },
  { label: "작전 ID", value: "연동 대기", note: "operation progression 배율 후보" },
  { label: "결과", value: "연동 대기", note: "CLEARED/PARTIAL/ABANDONED 등 outcome" },
  { label: "클리어 공세", value: "연동 대기", note: "waveCompletionMultiplier 후보" },
  { label: "rewardPreviewId / rewardClaimId", value: "연동 대기", note: "보상 미리보기/수령 연결" },
  { label: "적용 배율", value: "연동 대기", note: "sanitized calculation metadata 후보" },
  { label: "first clear", value: "연동 대기", note: "별도 rewardLine 지급 상태" },
  { label: "무결성 판정", value: "연동 대기", note: "reward_integrity_restricted 연계 후보" },
  { label: "계산 메타데이터 상태", value: "연동 대기", note: "reward_plan_json.calculation 후보" }
];

export function RunDetailPage() {
  const { runId = "" } = useParams();
  const query = useQuery({
    queryKey: ["run", runId],
    queryFn: () => adminApi.runDetail(runId),
    enabled: runId.length > 0
  });

  if (query.isLoading) return <main className="content">출격을 불러오는 중</main>;
  if (query.error) return <main className="content error">출격 요청 실패</main>;
  if (!query.data) return <main className="content">선택된 출격이 없습니다</main>;

  const run = query.data;

  return (
    <main className="content">
      <header>
        <h1>{run.runId}</h1>
        <p>{run.accountId}</p>
      </header>
      <section className="summary-grid">
        <div>
          <span>출격 밸런스 버전</span>
          <strong>{run.balanceVersion}</strong>
        </div>
        <div>
          <span>현재 게시 밸런스 버전</span>
          <strong>{run.currentActiveBalanceVersion}</strong>
        </div>
        <div>
          <span>클라이언트 빌드</span>
          <strong>{run.clientBuild}</strong>
        </div>
        <div>
          <span>검증 런타임 리비전</span>
          <strong>{run.validatorRuntimeRevision}</strong>
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>위험도/보상 계산 기준</h2>
          <p>서버 reward preview generation 계약 확정 전까지 계산 구조만 준비합니다</p>
        </header>
        <div className="metric-grid">
          {runRiskRewardPlaceholders.map((placeholder) => (
            <div key={placeholder.label} className="metric-card">
              <span>{placeholder.label}</span>
              <strong>{placeholder.value}</strong>
              <small>{placeholder.note}</small>
            </div>
          ))}
        </div>
      </section>
      <h2>스냅샷</h2>
      <table>
        <thead>
          <tr>
            <th>요원</th>
            <th>적용 레벨</th>
            <th>레벨 출처</th>
            <th>시작 시 동기화</th>
            <th>시작 시 훈련</th>
            <th>스킬</th>
            <th>승급</th>
          </tr>
        </thead>
        <tbody>
          {run.snapshot.operators.map((operator) => (
            <tr key={operator.operatorId}>
              <td>{operator.operatorId}</td>
              <td>{operator.effectiveLevel}</td>
              <td>{levelSourceLabel(operator.levelSource)}</td>
              <td>{operator.syncLevelAtStart}</td>
              <td>{operator.trainedLevelAtStart ?? "-"}</td>
              <td>{operator.skillTraining}</td>
              <td>{operator.promotion}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </main>
  );
}
