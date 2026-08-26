import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { adminApi } from "../shared/api/client";
import { levelSourceLabel } from "../shared/labels/adminLabels";

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
