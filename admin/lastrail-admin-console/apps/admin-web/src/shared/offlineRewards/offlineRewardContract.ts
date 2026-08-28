export type OfflineRewardPreviewStatus = "AVAILABLE" | "EMPTY" | "REJECTED";

export type OfflineRewardPreviewRewardLine = {
  source: "OFFLINE_REWARD";
  rewardId: string;
  quantity: string;
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

export const offlineRewardStateContractFields = [
  "last_offline_reward_claim_at",
  "last_story_progress_id",
  "last_balance_config_version",
  "last_narrative_version",
  "last_previewed_at",
  "revision",
  "updated_at"
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
