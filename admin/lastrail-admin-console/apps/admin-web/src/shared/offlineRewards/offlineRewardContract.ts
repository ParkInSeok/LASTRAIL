import { adminReadValueLabel } from "../api/errorDisplay";

export type OfflineRewardPreviewStatus = "AVAILABLE" | "EMPTY" | "REJECTED";

export type OfflineRewardPreviewRewardLine = {
  source: "OFFLINE_REWARD";
  rewardId: string;
  quantity: string;
  balanceAfter?: string;
};

export type OfflineRewardPreviewCalculation = {
  storyProgressId: string;
  storyProgressMultiplierLabel: string;
  cappedOfflineSeconds: string;
  capSource: string;
};

export type OfflineRewardPreviewContract = {
  previewStatus: OfflineRewardPreviewStatus | string;
  storyProgressId: string;
  claimableSeconds: string;
  claimableHours: string;
  offlineStorageCapHours: string;
  minimumClaimSeconds: string;
  maxClaimableHours: string;
  rewardLines: OfflineRewardPreviewRewardLine[];
  calculation: OfflineRewardPreviewCalculation;
  narrativeVersion: string;
  serverCalculatedAt: string;
  errorCode: string | null;
  message: string;
};

export type OfflineRewardStateContract = {
  last_offline_reward_claim_at: string;
  last_story_progress_id: string;
  last_balance_config_version: string;
  last_narrative_version: string;
  last_previewed_at: string;
  revision: string;
  updated_at: string;
};

export type OfflineRewardClaimStatus = "CLAIMED" | "DUPLICATE_REPLAY" | "REJECTED";

export type OfflineRewardClaimContract = {
  accepted: boolean | string;
  claimStatus: OfflineRewardClaimStatus | string;
  offlineRewardClaimId: string;
  rewardTransactionId: string;
  economyTransactionId: string;
  accountMutationId: string;
  storyProgressId: string;
  claimableSeconds: string;
  claimableHours: string;
  offlineStorageCapHours: string;
  minimumClaimSeconds: string;
  maxClaimableHours: string;
  rewardLines: OfflineRewardPreviewRewardLine[];
  calculation: OfflineRewardPreviewCalculation;
  serverClaimedAt: string;
  narrativeVersion: string;
  errorCode: string | null;
  message: string | null;
};

export type OfflineRewardSupportContext = {
  readOnly: true;
  claimMutationEnabled: false;
  mutationEndpointsEnabled: false;
};

export type OfflineRewardStateSource = "account_offline_reward_state" | "accounts.created_at_fallback" | string;

export type OfflineRewardReadModelState = {
  source: OfflineRewardStateSource;
  lastOfflineRewardClaimAt: string;
  lastStoryProgressId: string;
  lastBalanceConfigVersion: string | null;
  lastNarrativeVersion: string;
  lastPreviewedAt: string | null;
  revision: number | string;
  updatedAt: string;
};

export type OfflineRewardReadModelLastClaim = {
  offlineRewardClaimId: string;
  claimedAt: string;
};

export type OfflineRewardReadModelItem = {
  accountId: string;
  displayName: string;
  accountStatus: string;
  storyProgressId: string;
  narrativeVersion: string;
  offlineRewardState: OfflineRewardReadModelState;
  lastClaim: OfflineRewardReadModelLastClaim;
  supportContext: OfflineRewardSupportContext;
};

export type OfflineRewardReadModelResponse = {
  permission: "OFFLINE_REWARD_READ" | string;
  permissionMode: "ROLE_PERMISSION" | string;
  items: OfflineRewardReadModelItem[];
  nextCursor: string | null;
};

export type OfflineRewardReadFiltersPreview = {
  accountId: string;
  storyProgressId: string;
  narrativeVersion: string;
  source: string;
  limit: string;
  cursor: string;
};

export type OfflineRewardOperationsPolicyItem = {
  code: string;
  label: string;
  value: "연동 대기";
  note: string;
};

export type OfflineRewardOperationsSeverityMapping = {
  errorCode: string;
  severity: "info" | "needs_review" | "high_risk";
  category: "expected_behavior" | "offline_policy_review" | "offline_state_integrity";
};

export const offlineRewardPreviewContractFields = [
  "previewStatus",
  "storyProgressId",
  "claimableSeconds",
  "claimableHours",
  "offlineStorageCapHours",
  "minimumClaimSeconds",
  "maxClaimableHours",
  "rewardLines.source",
  "calculation.storyProgressId",
  "calculation.storyProgressMultiplierLabel",
  "calculation.cappedOfflineSeconds",
  "calculation.capSource",
  "narrativeVersion",
  "serverCalculatedAt",
  "errorCode",
  "message"
] as const;

export type OfflineRewardPreviewContractField = (typeof offlineRewardPreviewContractFields)[number];

export const offlineRewardClaimContractFields = [
  "accepted",
  "claimStatus",
  "offlineRewardClaimId",
  "rewardTransactionId",
  "economyTransactionId",
  "accountMutationId",
  "storyProgressId",
  "claimableSeconds",
  "claimableHours",
  "offlineStorageCapHours",
  "minimumClaimSeconds",
  "maxClaimableHours",
  "rewardLines.source",
  "rewardLines.balanceAfter",
  "calculation.storyProgressId",
  "calculation.storyProgressMultiplierLabel",
  "calculation.cappedOfflineSeconds",
  "calculation.capSource",
  "serverClaimedAt",
  "narrativeVersion",
  "errorCode",
  "message"
] as const;

export type OfflineRewardClaimContractField = (typeof offlineRewardClaimContractFields)[number];

export const offlineRewardStateContractFields = [
  "last_offline_reward_claim_at",
  "last_story_progress_id",
  "last_balance_config_version",
  "last_narrative_version",
  "last_previewed_at",
  "revision",
  "updated_at"
] as const;

export const offlineRewardReadFilterFields = [
  "accountId",
  "storyProgressId",
  "narrativeVersion",
  "source",
  "limit",
  "cursor"
] as const;

export const offlineRewardOperationsPolicy: OfflineRewardOperationsPolicyItem[] = [
  {
    code: "server_records_authoritative",
    label: "서버 기록 기준",
    value: "연동 대기",
    note: "서버 소유 기록이 오프라인 보상 Source of Truth입니다."
  },
  {
    code: "unity_client_state_not_source_of_truth",
    label: "클라이언트 상태 불신",
    value: "연동 대기",
    note: "Unity local state, screenshots, timers, client-supplied values는 판정 기준이 아닙니다."
  },
  {
    code: "risk_not_direct_multiplier",
    label: "위험도 직접 배율 제외",
    value: "연동 대기",
    note: "Risk I-V는 offline reward에 직접 곱하지 않습니다."
  },
  {
    code: "long_absence_cap_overflow_not_claimable",
    label: "장기 미접속 한도 초과",
    value: "연동 대기",
    note: "cap overflow는 기본 claim 대상이 아닙니다."
  },
  {
    code: "minimum_claim_time_not_claimable",
    label: "최소 수령 시간 미만",
    value: "연동 대기",
    note: "minimum claim time 미만은 expected behavior로 볼 수 있습니다."
  },
  {
    code: "empty_claim_does_not_advance_baseline",
    label: "빈 수령 기준 시각 유지",
    value: "연동 대기",
    note: "empty/not-claimable claim은 last_offline_reward_claim_at을 앞당기면 안 됩니다."
  },
  {
    code: "duplicate_replay_no_revision",
    label: "중복 재생 상태 유지",
    value: "연동 대기",
    note: "duplicate replay는 재지급이나 state revision 증가가 없습니다."
  },
  {
    code: "no_row_fallback_expected_before_first_claim",
    label: "no-row fallback",
    value: "연동 대기",
    note: "legacy account first claim 전 fallback은 expected behavior입니다."
  }
];

export const offlineRewardOperationsSeverityMappings: OfflineRewardOperationsSeverityMapping[] = [
  { errorCode: "offline_reward_not_claimable", severity: "info", category: "expected_behavior" },
  { errorCode: "duplicate_replay", severity: "info", category: "expected_behavior" },
  { errorCode: "offline_story_progress_missing", severity: "needs_review", category: "offline_policy_review" },
  { errorCode: "offline_reward_policy_missing", severity: "needs_review", category: "offline_policy_review" },
  { errorCode: "offline_reward_policy_invalid", severity: "needs_review", category: "offline_policy_review" },
  { errorCode: "offline_reward_state_conflict", severity: "high_risk", category: "offline_state_integrity" },
  { errorCode: "offline_reward_clock_tamper", severity: "high_risk", category: "offline_state_integrity" },
  { errorCode: "offline_reward_claim_conflict", severity: "high_risk", category: "offline_state_integrity" },
  { errorCode: "offline_reward_plan_mismatch", severity: "high_risk", category: "offline_state_integrity" },
  { errorCode: "economy_mutation_conflict", severity: "high_risk", category: "offline_state_integrity" }
];

export type OfflineRewardStateContractField = (typeof offlineRewardStateContractFields)[number];

export function offlineRewardPreviewContractFieldLabel(field: OfflineRewardPreviewContractField | string): string {
  return (
    {
      previewStatus: "미리보기 상태",
      storyProgressId: "스토리 진행도",
      claimableSeconds: "수령 가능 초",
      claimableHours: "수령 가능 시간",
      offlineStorageCapHours: "오프라인 보관 한도",
      minimumClaimSeconds: "최소 수령 초",
      maxClaimableHours: "최대 수령 시간",
      "rewardLines.source": "보상 출처",
      "calculation.storyProgressId": "계산 스토리 진행도",
      "calculation.storyProgressMultiplierLabel": "스토리 진행 배율",
      "calculation.cappedOfflineSeconds": "한도 적용 오프라인 초",
      "calculation.capSource": "한도 출처",
      narrativeVersion: "내러티브 버전",
      serverCalculatedAt: "서버 계산 시각",
      errorCode: "오류 코드",
      message: "메시지"
    }[field] ?? field
  );
}

export function offlineRewardClaimContractFieldLabel(field: OfflineRewardClaimContractField | string): string {
  return (
    {
      accepted: "수락 여부",
      claimStatus: "수령 상태",
      offlineRewardClaimId: "오프라인 보상 수령 ID",
      rewardTransactionId: "보상 트랜잭션 ID",
      economyTransactionId: "경제 트랜잭션 ID",
      accountMutationId: "계정 변경 ID",
      storyProgressId: "스토리 진행도",
      claimableSeconds: "수령 가능 초",
      claimableHours: "수령 가능 시간",
      offlineStorageCapHours: "오프라인 보관 한도",
      minimumClaimSeconds: "최소 수령 초",
      maxClaimableHours: "최대 수령 시간",
      "rewardLines.source": "보상 출처",
      "rewardLines.balanceAfter": "수령 후 잔액",
      "calculation.storyProgressId": "계산 스토리 진행도",
      "calculation.storyProgressMultiplierLabel": "스토리 진행 배율",
      "calculation.cappedOfflineSeconds": "한도 적용 오프라인 초",
      "calculation.capSource": "한도 출처",
      serverClaimedAt: "서버 수령 시각",
      narrativeVersion: "내러티브 버전",
      errorCode: "오류 코드",
      message: "메시지"
    }[field] ?? field
  );
}

export function offlineRewardStateContractFieldLabel(field: OfflineRewardStateContractField | string): string {
  return (
    {
      last_offline_reward_claim_at: "마지막 오프라인 보상 수령 시각",
      last_story_progress_id: "마지막 스토리 진행도",
      last_balance_config_version: "마지막 밸런스 설정 버전",
      last_narrative_version: "마지막 내러티브 버전",
      last_previewed_at: "마지막 미리보기 시각",
      revision: "리비전",
      updated_at: "갱신 시각"
    }[field] ?? field
  );
}

export const offlineRewardPreviewContractPreview: OfflineRewardPreviewContract = {
  previewStatus: "연동 대기",
  storyProgressId: "none",
  claimableSeconds: "연동 대기",
  claimableHours: "연동 대기",
  offlineStorageCapHours: "연동 대기",
  minimumClaimSeconds: "연동 대기",
  maxClaimableHours: "연동 대기",
  rewardLines: [
    {
      source: "OFFLINE_REWARD",
      rewardId: "연동 대기",
      quantity: "연동 대기"
    }
  ],
  calculation: {
    storyProgressId: "none",
    storyProgressMultiplierLabel: "1.00x",
    cappedOfflineSeconds: "연동 대기",
    capSource: "연동 대기"
  },
  narrativeVersion: "연동 대기",
  serverCalculatedAt: "연동 대기",
  errorCode: null,
  message: "연동 대기"
};

export const offlineRewardStateContractPreview: OfflineRewardStateContract = {
  last_offline_reward_claim_at: "연동 대기",
  last_story_progress_id: "연동 대기",
  last_balance_config_version: "연동 대기",
  last_narrative_version: "연동 대기",
  last_previewed_at: "연동 대기",
  revision: "연동 대기",
  updated_at: "연동 대기"
};

export const offlineRewardClaimContractPreview: OfflineRewardClaimContract = {
  accepted: "연동 대기",
  claimStatus: "연동 대기",
  offlineRewardClaimId: "연동 대기",
  rewardTransactionId: "연동 대기",
  economyTransactionId: "연동 대기",
  accountMutationId: "연동 대기",
  storyProgressId: "연동 대기",
  claimableSeconds: "연동 대기",
  claimableHours: "연동 대기",
  offlineStorageCapHours: "연동 대기",
  minimumClaimSeconds: "연동 대기",
  maxClaimableHours: "연동 대기",
  rewardLines: [
    {
      source: "OFFLINE_REWARD",
      rewardId: "연동 대기",
      quantity: "연동 대기",
      balanceAfter: "연동 대기"
    }
  ],
  calculation: {
    storyProgressId: "연동 대기",
    storyProgressMultiplierLabel: "연동 대기",
    cappedOfflineSeconds: "연동 대기",
    capSource: "연동 대기"
  },
  serverClaimedAt: "연동 대기",
  narrativeVersion: "연동 대기",
  errorCode: null,
  message: "연동 대기"
};

const pendingSupportContext: OfflineRewardSupportContext = {
  readOnly: true,
  claimMutationEnabled: false,
  mutationEndpointsEnabled: false
};

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value) ? (value as Record<string, unknown>) : null;
}

function stringValue(value: unknown, fallback: string): string {
  return typeof value === "string" && value.length > 0 ? value : fallback;
}

function readStringValue(value: unknown, emptyLabel = "없음"): string {
  return adminReadValueLabel({ kind: "success", value, emptyLabel });
}

function numberOrEmpty(value: unknown): number | string {
  return typeof value === "number" && Number.isFinite(value) ? value : "없음";
}

function nullableStringValue(value: unknown): string | null {
  return typeof value === "string" && value.length > 0 ? value : null;
}

function mapOfflineRewardState(value: unknown): OfflineRewardReadModelState {
  const record = asRecord(value);
  return {
    source: readStringValue(record?.source),
    lastOfflineRewardClaimAt: readStringValue(record?.lastOfflineRewardClaimAt),
    lastStoryProgressId: readStringValue(record?.lastStoryProgressId),
    lastBalanceConfigVersion: nullableStringValue(record?.lastBalanceConfigVersion),
    lastNarrativeVersion: readStringValue(record?.lastNarrativeVersion),
    lastPreviewedAt: nullableStringValue(record?.lastPreviewedAt),
    revision: numberOrEmpty(record?.revision),
    updatedAt: readStringValue(record?.updatedAt)
  };
}

function mapLastClaim(value: unknown): OfflineRewardReadModelLastClaim {
  const record = asRecord(value);
  return {
    offlineRewardClaimId: readStringValue(record?.offlineRewardClaimId),
    claimedAt: readStringValue(record?.claimedAt)
  };
}

function mapSupportContext(value: unknown): OfflineRewardSupportContext {
  const record = asRecord(value);
  return {
    readOnly: record?.readOnly === false ? true : true,
    claimMutationEnabled: false,
    mutationEndpointsEnabled: false
  };
}

function mapOfflineRewardItem(value: unknown, fallbackAccountId = "연동 대기"): OfflineRewardReadModelItem {
  const record = asRecord(value);
  if (!record) return emptyOfflineRewardDetail(fallbackAccountId);

  return {
    accountId: stringValue(record.accountId, fallbackAccountId),
    displayName: readStringValue(record.displayName),
    accountStatus: readStringValue(record.accountStatus),
    storyProgressId: readStringValue(record.storyProgressId),
    narrativeVersion: readStringValue(record.narrativeVersion),
    offlineRewardState: mapOfflineRewardState(record.offlineRewardState),
    lastClaim: mapLastClaim(record.lastClaim),
    supportContext: mapSupportContext(record.supportContext)
  };
}

export function emptyOfflineRewardDetail(accountId = "연동 대기"): OfflineRewardReadModelItem {
  return {
    accountId,
    displayName: "연동 대기",
    accountStatus: "연동 대기",
    storyProgressId: "연동 대기",
    narrativeVersion: "연동 대기",
    offlineRewardState: {
      source: "연동 대기",
      lastOfflineRewardClaimAt: "연동 대기",
      lastStoryProgressId: "연동 대기",
      lastBalanceConfigVersion: null,
      lastNarrativeVersion: "연동 대기",
      lastPreviewedAt: null,
      revision: "연동 대기",
      updatedAt: "연동 대기"
    },
    lastClaim: {
      offlineRewardClaimId: "연동 대기",
      claimedAt: "연동 대기"
    },
    supportContext: pendingSupportContext
  };
}

export function noDataOfflineRewardDetail(accountId = "데이터 없음"): OfflineRewardReadModelItem {
  return {
    accountId,
    displayName: "없음",
    accountStatus: "없음",
    storyProgressId: "none",
    narrativeVersion: "없음",
    offlineRewardState: {
      source: "없음",
      lastOfflineRewardClaimAt: "없음",
      lastStoryProgressId: "none",
      lastBalanceConfigVersion: null,
      lastNarrativeVersion: "없음",
      lastPreviewedAt: null,
      revision: "없음",
      updatedAt: "없음"
    },
    lastClaim: {
      offlineRewardClaimId: "없음",
      claimedAt: "없음"
    },
    supportContext: pendingSupportContext
  };
}

export function mapOfflineRewardListResponse(response: unknown): OfflineRewardReadModelResponse {
  const record = asRecord(response);
  return {
    permission: stringValue(record?.permission, "OFFLINE_REWARD_READ"),
    permissionMode: stringValue(record?.permissionMode, "ROLE_PERMISSION"),
    items: Array.isArray(record?.items) ? record.items.map((item) => mapOfflineRewardItem(item)) : [],
    nextCursor: typeof record?.nextCursor === "string" ? record.nextCursor : null
  };
}

export function mapOfflineRewardDetailResponse(response: unknown, accountId: string): OfflineRewardReadModelItem {
  return response === null || response === undefined ? emptyOfflineRewardDetail(accountId) : mapOfflineRewardItem(response, accountId);
}

export const offlineRewardReadContractPreview = {
  listItem: emptyOfflineRewardDetail(),
  detail: emptyOfflineRewardDetail(),
  filters: Object.fromEntries(offlineRewardReadFilterFields.map((field) => [field, ""])) as OfflineRewardReadFiltersPreview
};
