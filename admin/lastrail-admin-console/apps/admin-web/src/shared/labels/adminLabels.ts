import type {
  AdminRole,
  PlayerDetail,
  PlayerSearchResponse,
  RunDetail,
  RunSearchResponse,
  SuspiciousPlayerDetail,
  SuspiciousPlayersResponse
} from "@lastrail/admin-contracts";

type AccountStatus = PlayerDetail["status"] | PlayerSearchResponse["items"][number]["status"];
type AuditResult = SuspiciousPlayerDetail["decisions"][number]["result"];
type Classification = SuspiciousPlayerDetail["riskSignals"][number]["classification"];
type LevelSource =
  | PlayerDetail["progression"]["operators"][number]["levelSource"]
  | RunDetail["snapshot"]["operators"][number]["levelSource"];
type Priority =
  | NonNullable<PlayerSearchResponse["items"][number]["risk"]>
  | SuspiciousPlayersResponse["items"][number]["priority"]
  | SuspiciousPlayerDetail["summary"]["priority"];
type ReviewState = "OPEN" | "IN_REVIEW" | "RESOLVED";
type ReviewStatus =
  | NonNullable<PlayerSearchResponse["items"][number]["reviewStatus"]>
  | SuspiciousPlayersResponse["items"][number]["reviewStatus"]
  | SuspiciousPlayerDetail["summary"]["reviewStatus"];
type RunResult =
  | RunSearchResponse["items"][number]["result"]
  | SuspiciousPlayerDetail["suspiciousRuns"][number]["result"];

function labelFor<T extends string>(labels: Record<T, string>, value: T | string): string {
  return labels[value as T] ?? value;
}

export function accountStatusLabel(status: AccountStatus): string {
  return labelFor(
    {
      ACTIVE: "활성",
      SUSPENDED: "정지",
      BANNED: "차단",
      DELETED: "삭제"
    },
    status
  );
}

export function auditResultLabel(result: AuditResult): string {
  return labelFor({ SUCCESS: "성공", FAILURE: "실패" }, result);
}

export function auditActionLabel(action: string): string {
  return labelFor(
    {
      PLAYER_NOTE_CREATE: "플레이어 노트 작성",
      BALANCE_CONFIG_PUBLISH: "밸런스 설정 게시",
      INTEGRITY_REVIEW_MARK: "무결성 판정 기록",
      INTEGRITY_REVIEW_STATUS_CHANGE: "무결성 검토 상태 변경"
    },
    action
  );
}

export function categoryLabel(category: string): string {
  return labelFor(
    {
      DAMAGE_ANALYSIS: "피해량 분석",
      CLEAR_TIME_ANALYSIS: "클리어 시간 분석",
      ULTIMATE_USAGE_ANALYSIS: "얼티밋 전술 사용 분석",
      CURRENCY_ANALYSIS: "재화 분석",
      BALANCE_VERSION_ANALYSIS: "밸런스 버전 분석",
      SCORE_PATTERN_ANALYSIS: "점수 패턴 분석"
    },
    category
  );
}

export function classificationLabel(classification: Classification): string {
  return labelFor(
    {
      SOFT_ANOMALY: "경미 이상",
      HARD_VIOLATION: "중대 위반"
    },
    classification
  );
}

export function evidenceCodeLabel(code: string | null): string {
  if (!code) return "신호 없음";
  return labelFor(
    {
      CHECKSUM_MISMATCH: "체크섬 불일치",
      IMPOSSIBLE_SCORE: "불가능한 점수",
      IMPOSSIBLE_DAMAGE: "불가능한 피해량",
      BALANCE_VERSION_REWARD_MISMATCH: "밸런스 버전 보상 불일치",
      ABNORMAL_CLEAR_TIME: "비정상 클리어 시간",
      ABNORMAL_ULTIMATE_USAGE: "비정상 얼티밋 사용",
      EXCESSIVE_RAID_SCORE_FOR_PROGRESSION: "성장 대비 과도한 레이드 점수",
      CURRENCY_SPIKE: "비정상 재화 증가",
      BOUNDARY_SCORE_PATTERN: "경계값 점수 반복",
      DAMAGE_SPIKE: "피해량 급증"
    },
    code
  );
}

export function enforcementLabel(enforcement: string): string {
  return labelFor(
    {
      NONE: "조치 없음",
      SCORE_EXCLUDED: "순위 제외",
      REWARD_HELD: "보상 보류",
      ACTIONED: "조치 완료"
    },
    enforcement
  );
}

export function levelSourceLabel(source: LevelSource): string {
  return labelFor({ TRAINED: "훈련", SYNC: "동기화" }, source);
}

export function permissionLabel(permission: string): string {
  return labelFor(
    {
      ACCOUNT_READ: "계정 조회",
      ACCOUNT_NOTE_WRITE: "계정 노트 작성",
      RUN_READ: "출격 조회",
      INTEGRITY_READ: "무결성 조회",
      INCIDENT_READ: "사건 조회",
      ENFORCEMENT_SUSPEND: "제재 처리",
      ANALYTICS_READ: "지표 조회",
      AUDIT_READ: "감사 로그 조회",
      BALANCE_READ: "밸런스 조회",
      BALANCE_DRAFT_CREATE: "밸런스 초안 생성",
      BALANCE_DRAFT_EDIT: "밸런스 초안 수정",
      BALANCE_DRAFT_DISCARD: "밸런스 초안 폐기",
      BALANCE_VALIDATE: "밸런스 검증",
      BALANCE_PUBLISH: "밸런스 게시",
      BALANCE_ROLLBACK: "밸런스 롤백",
      BALANCE_SIMULATION_READ: "밸런스 시뮬레이션 조회",
      BALANCE_SIMULATION_RUN: "밸런스 시뮬레이션 실행",
      BALANCE_SIMULATION_CANCEL: "밸런스 시뮬레이션 취소"
    },
    permission
  );
}

export function priorityLabel(priority: Priority): string {
  return labelFor(
    {
      LOW: "낮음",
      MEDIUM: "보통",
      HIGH: "높음",
      CRITICAL: "긴급"
    },
    priority
  );
}

export function resourceTypeLabel(resourceType: string): string {
  return labelFor(
    {
      account: "계정",
      run: "출격",
      incident: "사건",
      balance: "밸런스"
    },
    resourceType
  );
}

export function reviewStateLabel(state: ReviewState): string {
  return labelFor({ OPEN: "신규", IN_REVIEW: "검토 중", RESOLVED: "처리 완료" }, state);
}

export function reviewStatusLabel(status: ReviewStatus): string {
  return labelFor(
    {
      UNREVIEWED: "미검토",
      IN_REVIEW: "검토 중",
      NEEDS_MORE_EVIDENCE: "추가 증거 필요",
      CONFIRMED_VIOLATION: "위반 확인",
      LINKED_INCIDENT: "사건 연결",
      NEW: "신규",
      REVIEWING: "검토 중",
      CLEARED: "정상 처리",
      SCORE_EXCLUDED: "순위 제외",
      REWARD_HELD: "보상 보류",
      ESCALATED: "제재 검토",
      ACTIONED: "조치 완료",
      FALSE_POSITIVE: "오탐"
    },
    status
  );
}

export function roleLabel(role: AdminRole): string {
  return labelFor({ VIEWER: "조회자", OPERATOR: "운영자", OWNER: "소유자" }, role);
}

export function runResultLabel(result: RunResult): string {
  return labelFor(
    {
      IN_PROGRESS: "진행 중",
      CLEARED: "완료",
      FAILED: "실패",
      INVALIDATED: "무효 처리"
    },
    result
  );
}
