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

function numberOrPending(value: unknown): number | string {
  return typeof value === "number" && Number.isFinite(value) ? value : "연동 대기";
}

function nullableStringValue(value: unknown): string | null {
  return typeof value === "string" && value.length > 0 ? value : null;
}

function mapOfflineRewardState(value: unknown): OfflineRewardReadModelState {
  const record = asRecord(value);
  return {
    source: stringValue(record?.source, "연동 대기"),
    lastOfflineRewardClaimAt: stringValue(record?.lastOfflineRewardClaimAt, "연동 대기"),
    lastStoryProgressId: stringValue(record?.lastStoryProgressId, "연동 대기"),
    lastBalanceConfigVersion: nullableStringValue(record?.lastBalanceConfigVersion),
    lastNarrativeVersion: stringValue(record?.lastNarrativeVersion, "연동 대기"),
    lastPreviewedAt: nullableStringValue(record?.lastPreviewedAt),
    revision: numberOrPending(record?.revision),
    updatedAt: stringValue(record?.updatedAt, "연동 대기")
  };
}

function mapLastClaim(value: unknown): OfflineRewardReadModelLastClaim {
  const record = asRecord(value);
  return {
    offlineRewardClaimId: stringValue(record?.offlineRewardClaimId, "연동 대기"),
    claimedAt: stringValue(record?.claimedAt, "연동 대기")
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
    displayName: stringValue(record.displayName, "연동 대기"),
    accountStatus: stringValue(record.accountStatus, "연동 대기"),
    storyProgressId: stringValue(record.storyProgressId, "연동 대기"),
    narrativeVersion: stringValue(record.narrativeVersion, "연동 대기"),
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
