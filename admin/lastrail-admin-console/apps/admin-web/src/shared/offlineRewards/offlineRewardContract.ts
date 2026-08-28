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
