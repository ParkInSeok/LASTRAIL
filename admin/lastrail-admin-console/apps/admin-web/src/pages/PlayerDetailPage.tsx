import { Link, useParams } from "react-router-dom";
import { FormEvent, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { hasPermission } from "@lastrail/admin-contracts";
import { adminApi } from "../shared/api/client";
import { adminRequestFailureDisplay } from "../shared/api/errorDisplay";
import { useAdmin } from "../shared/auth/AdminContext";
import {
  accountStatusLabel,
  levelSourceLabel,
  opsErrorLogSourceLabel,
  opsErrorLogStatusLabel,
  opsSeverityLabel
} from "../shared/labels/adminLabels";
import type { PlayerLaunchEvidence } from "../shared/players/playerLaunchEvidenceContract";
import { accountAuthSessionFieldLabel, accountAuthVisibilityFields } from "../shared/support/supportContract";

type PlayerAuthEvidenceCard = {
  code: string;
  label: string;
  value: string;
  note: string;
};

type PlayerRecentOpsErrorEvidenceCard = {
  code: string;
  label: string;
  value: string;
  note: string;
};

export const playerAuthEvidenceCards: PlayerAuthEvidenceCard[] = [
  ...accountAuthVisibilityFields.map((field) => ({
    code: field,
    label: accountAuthSessionFieldLabel(field),
    value: "연동 대기",
    note:
      field === "concurrentUsers"
        ? "UNAVAILABLE/null은 확정 동접자 수 아님"
        : `${field} read-only auth/session evidence 후보`
  }))
];

export const playerRecentOpsErrorEvidenceCards: PlayerRecentOpsErrorEvidenceCard[] = [
  {
    code: "recentOpsErrorLogs",
    label: "최근 시스템 에러 로그",
    value: "연동 대기",
    note: "recent open/acknowledged latest 5, critical first read-only compose 후보"
  },
  {
    code: "opsErrorSeverity",
    label: "심각도",
    value: "연동 대기",
    note: "info / warning / error / critical"
  },
  {
    code: "opsErrorStatus",
    label: "상태",
    value: "연동 대기",
    note: "open / acknowledged / resolved / ignored"
  },
  {
    code: "opsErrorSource",
    label: "소스",
    value: "연동 대기",
    note: "game_api / admin_api / support_read_model / integrity_pipeline"
  },
  {
    code: "opsErrorCorrelationIds",
    label: "관련 식별자",
    value: "연동 대기",
    note: "accountId / runId / gachaBannerId / rewardPreviewId / supportTicketId"
  }
] as const;

function labelWithCode(label: string, code: string): string {
  return label === code ? code : `${label} / ${code}`;
}

export function playerAuthEvidenceCardsForLaunchEvidence(
  evidence: PlayerLaunchEvidence | null | undefined
): PlayerAuthEvidenceCard[] {
  if (!evidence) return playerAuthEvidenceCards;
  const itemByField = new Map(evidence.authSession.items.map((item) => [item.field, item]));

  return playerAuthEvidenceCards.map((card) => {
    const item = itemByField.get(card.code as (typeof accountAuthVisibilityFields)[number]);
    return {
      ...card,
      value: item?.value ?? "없음",
      note: item?.note ?? card.note
    };
  });
}

function correlationSummary(correlationIds: PlayerLaunchEvidence["opsErrors"][number]["correlationIds"]): string {
  const entries = Object.entries(correlationIds).filter((entry): entry is [string, string] => {
    return typeof entry[1] === "string" && entry[1].length > 0;
  });
  return entries.length > 0 ? entries.map(([key, value]) => `${key}=${value}`).join(" / ") : "없음";
}

export function playerRecentOpsErrorEvidenceCardsForLaunchEvidence(
  evidence: PlayerLaunchEvidence | null | undefined
): PlayerRecentOpsErrorEvidenceCard[] {
  if (!evidence) return playerRecentOpsErrorEvidenceCards;
  const first = evidence.opsErrors[0];
  if (!first) {
    return playerRecentOpsErrorEvidenceCards.map((card) => ({ ...card, value: "데이터 없음" }));
  }

  const values: Record<string, string> = {
    recentOpsErrorLogs: `${evidence.opsErrors.length}건`,
    opsErrorSeverity: labelWithCode(opsSeverityLabel(first.severity), first.severity),
    opsErrorStatus: labelWithCode(opsErrorLogStatusLabel(first.status), first.status),
    opsErrorSource: labelWithCode(opsErrorLogSourceLabel(first.source), first.source),
    opsErrorCorrelationIds: correlationSummary(first.correlationIds)
  };

  return playerRecentOpsErrorEvidenceCards.map((card) => ({
    ...card,
    value: values[card.code] ?? "없음"
  }));
}

function PlayerDetailFailure({ error, accountId }: { error: unknown; accountId: string }) {
  const failure = adminRequestFailureDisplay(error, { resourceLabel: "플레이어", resourceId: accountId });

  return (
    <main className="content error">
      <header>
        <h1>{failure.title}</h1>
        <p>{failure.resourceId}</p>
      </header>
      <section className="summary-grid" aria-label="플레이어 요청 실패 분류">
        <div>
          <span>분류</span>
          <strong>{failure.reason}</strong>
          <small>{failure.nextStep}</small>
        </div>
        <div>
          <span>상태 코드</span>
          <strong>{failure.statusLabel}</strong>
          <small>Admin API response status</small>
        </div>
        <div>
          <span>errorCode</span>
          <strong>{failure.errorCode}</strong>
          <small>{failure.message}</small>
        </div>
        <div>
          <span>경계</span>
          <strong>읽기 전용</strong>
          <small>복구/병합/세션 revoke action 없음</small>
        </div>
      </section>
      <Link to="/players">플레이어 검색으로 돌아가기</Link>
    </main>
  );
}

export function PlayerDetailPage() {
  const { accountId = "" } = useParams();
  const { bootstrap } = useAdmin();
  const canWriteNote = bootstrap ? hasPermission(bootstrap.admin, "ACCOUNT_NOTE_WRITE") : false;
  const queryClient = useQueryClient();
  const [note, setNote] = useState("");
  const [lastNoteRequestId, setLastNoteRequestId] = useState<string | null>(null);
  const query = useQuery({
    queryKey: ["player", accountId],
    queryFn: () => adminApi.playerDetail(accountId),
    enabled: accountId.length > 0
  });
  const notesQuery = useQuery({
    queryKey: ["player-notes", accountId],
    queryFn: () => adminApi.playerNotes(accountId),
    enabled: accountId.length > 0
  });
  const noteMutation = useMutation({
    mutationFn: () => adminApi.createPlayerNote(accountId, note),
    onSuccess: (result) => {
      setNote("");
      setLastNoteRequestId(result.note.requestId);
      void queryClient.invalidateQueries({ queryKey: ["player-notes", accountId] });
    }
  });

  function submitNote(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setLastNoteRequestId(null);
    noteMutation.mutate();
  }

  if (query.isLoading) return <main className="content">플레이어를 불러오는 중</main>;
  if (query.error) return <PlayerDetailFailure error={query.error} accountId={accountId} />;
  if (!query.data) return <main className="content">선택된 플레이어가 없습니다</main>;

  const player = query.data;
  const authEvidenceCards = playerAuthEvidenceCardsForLaunchEvidence(player.launchEvidence);
  const recentOpsErrorEvidenceCards = playerRecentOpsErrorEvidenceCardsForLaunchEvidence(player.launchEvidence);

  return (
    <main className="content">
      <header>
        <h1>{player.displayName}</h1>
        <p>{player.accountId}</p>
      </header>
      <section className="summary-grid">
        <div>
          <span>상태</span>
          <strong>{accountStatusLabel(player.status)}</strong>
        </div>
        <div>
          <span>요원 경험치</span>
          <strong>{player.progression.operatorExp}</strong>
        </div>
        <div>
          <span>동기화 레벨</span>
          <strong>{player.progression.syncLevel}</strong>
        </div>
        <div>
          <span>코어 레벨 편차</span>
          <strong>{player.progression.coreLevelSpread}</strong>
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>최근 시스템 에러 로그</h2>
          <p>계정 문의와 연결 가능한 ops error evidence 후보를 읽기 전용으로 표시합니다</p>
        </header>
        <div className="metric-grid">
          {recentOpsErrorEvidenceCards.map((card) => (
            <div key={card.code} className="metric-card">
              <span>{card.label}</span>
              <strong>{card.value}</strong>
              <small>{card.note}</small>
            </div>
          ))}
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>로그인/연동 근거</h2>
          <p>Google provider/link/account recovery 상태를 token 없이 읽기 전용으로 표시합니다</p>
        </header>
        <div className="metric-grid">
          {authEvidenceCards.map((card) => (
            <div key={card.code} className="metric-card">
              <span>{card.label}</span>
              <strong>{card.value}</strong>
              <small>{card.note}</small>
            </div>
          ))}
        </div>
      </section>
      <section className="admin-panel">
        <h2>운영 노트</h2>
        {canWriteNote ? (
          <form className="note-form" onSubmit={submitNote}>
            <label htmlFor="player-note">노트</label>
            <textarea
              id="player-note"
              maxLength={1000}
              minLength={1}
              required
              value={note}
              onChange={(event) => setNote(event.target.value)}
            />
            <div className="form-row">
              <small>{note.length}/1000</small>
              <button type="submit" disabled={noteMutation.isPending || note.trim().length === 0}>
                {noteMutation.isPending ? "저장 중" : "노트 저장"}
              </button>
            </div>
            {lastNoteRequestId ? <p className="success">노트 저장 완료. 요청 {lastNoteRequestId}</p> : null}
            {noteMutation.error ? <p className="error">노트 저장 실패</p> : null}
          </form>
        ) : null}
        <div className="note-history">
          <h3>최근 노트</h3>
          {notesQuery.isLoading ? <p>노트를 불러오는 중</p> : null}
          {notesQuery.error ? <p className="error">노트 이력 요청 실패</p> : null}
          {notesQuery.data?.items.length === 0 ? <p>아직 노트가 없습니다</p> : null}
          {notesQuery.data?.items.map((item) => (
            <article key={item.id}>
              <header>
                <strong>{item.authorDisplayName}</strong>
                <span>{new Date(item.createdAt).toLocaleString()}</span>
              </header>
              <p>{item.note}</p>
            </article>
          ))}
        </div>
      </section>
      <h2>훈련 코어</h2>
      <table>
        <thead>
          <tr>
            <th>슬롯</th>
            <th>요원</th>
            <th>훈련 레벨</th>
          </tr>
        </thead>
        <tbody>
          {player.progression.trainingCore.map((core) => (
            <tr key={core.slot}>
              <td>{core.slot}</td>
              <td>{core.name}</td>
              <td>{core.trainedLevel}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <h2>요원</h2>
      <table>
        <thead>
          <tr>
            <th>요원</th>
            <th>적용 레벨</th>
            <th>레벨 출처</th>
            <th>훈련 레벨</th>
            <th>스킬</th>
            <th>승급</th>
            <th>코어 슬롯</th>
          </tr>
        </thead>
        <tbody>
          {player.progression.operators.map((operator) => (
            <tr key={operator.operatorId}>
              <td>{operator.name}</td>
              <td>{operator.effectiveLevel}</td>
              <td>{levelSourceLabel(operator.levelSource)}</td>
              <td>{operator.trainedLevel}</td>
              <td>{operator.skillTraining}</td>
              <td>{operator.promotion}</td>
              <td>{operator.coreSlot ?? "-"}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <Link to={`/runs?accountId=${player.accountId}`}>출격 보기</Link>
    </main>
  );
}
