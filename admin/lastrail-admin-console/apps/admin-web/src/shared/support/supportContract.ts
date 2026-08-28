export type CustomerSupportCaseType =
  | "Account/Auth"
  | "Run Completion"
  | "Run Reward"
  | "Offline Reward"
  | "Story Progress/Unlock"
  | "Gacha/Recruitment"
  | "Integrity-restricted Reward";

export type CustomerSupportContextField =
  | "accountStatus"
  | "recentRuns"
  | "runRewardPreviewClaimIds"
  | "offlineRewardStateClaimIds"
  | "storyProgressUnlocks"
  | "gachaBatchGuaranteeType"
  | "idempotencyReplayStatus"
  | "integrityDisposition"
  | "recentOpsErrorLogs";

export type CustomerSupportContextItem = {
  field: CustomerSupportContextField;
  label: string;
  value: "연동 대기";
  note: string;
};

export type CustomerSupportContractPreview = {
  accountId: "연동 대기";
  status: "연동 대기";
  permission: "SUPPORT_READ";
  permissionMode: "ROLE_PERMISSION";
  endpointCandidate: "/v1/admin/support/accounts/:accountId";
  context: CustomerSupportContextItem[];
  supportContext: {
    readOnly: true;
    mutationEndpointsEnabled: false;
  };
};

export const customerSupportCaseTypes: CustomerSupportCaseType[] = [
  "Account/Auth",
  "Run Completion",
  "Run Reward",
  "Offline Reward",
  "Story Progress/Unlock",
  "Gacha/Recruitment",
  "Integrity-restricted Reward"
];

export const customerSupportContextFields: CustomerSupportContextField[] = [
  "accountStatus",
  "recentRuns",
  "runRewardPreviewClaimIds",
  "offlineRewardStateClaimIds",
  "storyProgressUnlocks",
  "gachaBatchGuaranteeType",
  "idempotencyReplayStatus",
  "integrityDisposition",
  "recentOpsErrorLogs"
];

export const customerSupportMutationControlsOutOfScope = [
  "manual currency grants",
  "reward repair buttons",
  "offline reward timestamp reset",
  "story progress mutation",
  "unlock mutation",
  "gacha reroll/repair",
  "localization publishing",
  "narrative editing",
  "economy correction"
] as const;

export const customerSupportSensitiveExamplesOutOfScope = [
  "token",
  "raw auth header",
  "raw body",
  "raw RNG",
  "seed preimage",
  "per-candidate probability",
  "full weight list"
] as const;

const contextLabels: Record<CustomerSupportContextField, string> = {
  accountStatus: "계정/인증 상태",
  recentRuns: "최근 출격",
  runRewardPreviewClaimIds: "출격 보상 미리보기/수령 ID",
  offlineRewardStateClaimIds: "오프라인 보상 상태/수령 ID",
  storyProgressUnlocks: "스토리 진행/해금",
  gachaBatchGuaranteeType: "요원 모집 batch/보장 발동",
  idempotencyReplayStatus: "Idempotency replay 상태",
  integrityDisposition: "무결성 판정",
  recentOpsErrorLogs: "최근 시스템 에러 로그"
};

export function customerSupportCaseTypeLabel(caseType: CustomerSupportCaseType | string): string {
  return (
    {
      "Account/Auth": "계정/인증",
      "Run Completion": "출격 완료",
      "Run Reward": "출격 보상",
      "Offline Reward": "오프라인 보상",
      "Story Progress/Unlock": "스토리 진행/해금",
      "Gacha/Recruitment": "요원 모집",
      "Integrity-restricted Reward": "무결성 제한 보상"
    }[caseType] ?? caseType
  );
}

export function customerSupportContextFieldLabel(field: CustomerSupportContextField | string): string {
  return contextLabels[field as CustomerSupportContextField] ?? field;
}

export const customerSupportContractPreview: CustomerSupportContractPreview = {
  accountId: "연동 대기",
  status: "연동 대기",
  permission: "SUPPORT_READ",
  permissionMode: "ROLE_PERMISSION",
  endpointCandidate: "/v1/admin/support/accounts/:accountId",
  context: customerSupportContextFields.map((field) => ({
    field,
    label: customerSupportContextFieldLabel(field),
    value: "연동 대기",
    note: `${field} read-only reference`
  })),
  supportContext: {
    readOnly: true,
    mutationEndpointsEnabled: false
  }
};
