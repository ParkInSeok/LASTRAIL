import {
  buildReleaseManagementSnapshot,
  releaseManagementActionCandidates,
  releaseManagementCandidateFields,
  releaseManagementReadStates,
  releaseManagementSafetyChecks,
  releaseManagementTargetAxes,
  releaseManagementTargetRegistryPolicy,
  releaseManagementValidationChecks
} from "../shared/release/releaseManagementContract";

type VersionManagementCard = {
  code: string;
  label: string;
  value: string;
  note: string;
};

export const versionManagementTargetCards: VersionManagementCard[] = [
  {
    code: "targetId",
    label: "대상 서버",
    value: "서버 계약 대기",
    note: "서버 registry가 허용한 targetId만 선택"
  },
  {
    code: "environment",
    label: "환경",
    value: "서버 계약 대기",
    note: "DEV/PRD는 환경 축이며 Unity define STAGE/TEST를 새로 만들지 않음"
  },
  {
    code: "platform",
    label: "플랫폼",
    value: "서버 계약 대기",
    note: "Android/Windows 등 플랫폼별 release"
  },
  {
    code: "channel",
    label: "채널",
    value: "서버 계약 대기",
    note: "internal/production 등 배포 채널"
  },
  {
    code: "activeReleaseRevision",
    label: "활성 release/revision",
    value: "서버 계약 대기",
    note: "target/platform/channel별 current pointer"
  },
  {
    code: "targetRegistry",
    label: "대상 registry",
    value: "서버 계약 대기",
    note: releaseManagementTargetRegistryPolicy.targetIsolation
  }
];

export const versionManagementCandidateFormFields = [...releaseManagementCandidateFields];

export const versionManagementActionCards = releaseManagementActionCandidates.map((action) => ({
  code: action.code,
  label: action.label,
  status: "비활성" as const,
  note: `${action.note} / targetId 확인 / reason 필수 / ${action.permissionStatus}`
}));

export const versionManagementStatusCards: VersionManagementCard[] = releaseManagementReadStates.map((state) => ({
  code: state,
  label:
    {
      unset: "미설정",
      no_permission: "권한 없음",
      loading: "조회 중",
      empty: "데이터 없음",
      conflict: "충돌",
      publish_failed: "게시 실패",
      verification_in_progress: "검증 진행 중",
      target_unavailable: "대상 접근 불가",
      r2_unconfigured: "R2 미설정"
    }[state] ?? state,
  value: "표시 상태",
  note: `${state} 상태를 fake success 없이 분리`
}));

export const versionManagementValidationCards = releaseManagementValidationChecks.map((check) => ({
  code: check.code,
  label: check.label,
  value: check.status,
  note: check.note
}));

export const versionManagementBoundaryCards: VersionManagementCard[] = [
  {
    code: "spreadsheet",
    label: "스프레드시트",
    value: "스프레드시트 제외",
    note: "버전 편집 surface는 Admin으로 통합하고 spreadsheet sync/create/delete 없음"
  },
  {
    code: "remoteExecution",
    label: "원격 실행",
    value: "원격 실행 보류",
    note: "이번 slice는 local 구현/테스트만, R2 upload/promotion/open/rollback 실행 없음"
  },
  {
    code: "publicJson",
    label: "Public JSON",
    value: "내부 데이터 제외",
    note: "internal approval, audit history, diagnostic data는 public JSON에 포함하지 않음"
  },
  {
    code: "testFlag",
    label: "test=true",
    value: "내부 접근 불가",
    note: "public request test=true로 internal access를 열지 않음"
  },
  {
    code: "permission",
    label: "권한",
    value: "권한 계약 대기",
    note: "release read/edit/approve/promote/open/rollback 권한은 서버 계약 후 확정"
  }
];

export const versionManagementSafetyCards: VersionManagementCard[] = releaseManagementSafetyChecks.map((check) => ({
  code: check,
  label: "검증 후보",
  value: "서버 계약 대기",
  note: check
}));

export function VersionManagementPage() {
  const snapshot = buildReleaseManagementSnapshot();

  return (
    <main className="content">
      <header>
        <h1>버전 관리</h1>
        <p>대상 서버별 앱/콘텐츠/프로토콜/release 버전을 관리하는 운영 화면 준비 상태</p>
      </header>
      <section className="summary-grid" aria-label="버전 관리 요약">
        <div>
          <span>서버 계약</span>
          <strong>{snapshot.serverContractStatus}</strong>
          <small>API URL, 필수 필드, permission enum은 서버 계약 후 확정</small>
        </div>
        <div>
          <span>쓰기 실행</span>
          <strong>비활성</strong>
          <small>저장/승급/개시/롤백 remote execution 없음</small>
        </div>
        <div>
          <span>대상 축</span>
          <strong>{releaseManagementTargetAxes.join(" / ")}</strong>
          <small>환경/채널/플랫폼/targetId 분리</small>
        </div>
        <div>
          <span>스프레드시트</span>
          <strong>제외</strong>
          <small>기존 Drive 파일 삭제가 아니라 버전관리 surface에서 제외</small>
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>대상 서버</h2>
          <p>브라우저가 API URL, bucket, R2 key, host를 임의 제출하지 않는 서버 소유 registry 기준입니다</p>
        </header>
        <div className="metric-grid">
          {versionManagementTargetCards.map((card) => (
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
          <h2>후보 편집 폼</h2>
          <p>raw JSON editor 대신 필드별 입력 구조만 준비합니다</p>
        </header>
        <div className="metric-grid">
          {versionManagementCandidateFormFields.map((field) => (
            <label key={field.key} className="metric-card">
              <span>{field.label}</span>
              {field.input === "textarea" ? (
                <textarea value={field.value} readOnly aria-label={field.label} />
              ) : (
                <input value={field.value} readOnly aria-label={field.label} />
              )}
              <small>
                {field.key} / {field.note}
              </small>
            </label>
          ))}
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>명시적 확인 흐름</h2>
          <p>대상 서버명, 환경, 플랫폼, 채널, 현재에서 목표 버전, 사유를 확인하기 전까지 실행하지 않습니다</p>
        </header>
        <div className="metric-grid">
          {versionManagementActionCards.map((card) => (
            <div key={card.code} className="metric-card">
              <span>{card.label}</span>
              <strong>{card.status}</strong>
              <button type="button" disabled>
                서버 계약 대기
              </button>
              <small>{card.note}</small>
            </div>
          ))}
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>검증/상태</h2>
          <p>성공/공백/권한 없음/충돌/게시 실패/검증 중/R2 미설정을 분리해 표시합니다</p>
        </header>
        <div className="metric-grid">
          {versionManagementStatusCards.map((card) => (
            <div key={card.code} className="metric-card">
              <span>{card.label}</span>
              <strong>{card.value}</strong>
              <small>{card.note}</small>
            </div>
          ))}
        </div>
        <div className="metric-grid">
          {versionManagementValidationCards.map((card) => (
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
          <h2>안전 경계</h2>
          <p>target 전환, 동시 편집, DEV to PRD 승급, rollback 호환성은 서버 계약 후 fail-closed로 검증합니다</p>
        </header>
        <div className="metric-grid">
          {versionManagementBoundaryCards.map((card) => (
            <div key={card.code} className="metric-card">
              <span>{card.label}</span>
              <strong>{card.value}</strong>
              <small>{card.note}</small>
            </div>
          ))}
          {versionManagementSafetyCards.map((card) => (
            <div key={card.code} className="metric-card">
              <span>{card.label}</span>
              <strong>{card.value}</strong>
              <small>{card.note}</small>
            </div>
          ))}
        </div>
      </section>
    </main>
  );
}
