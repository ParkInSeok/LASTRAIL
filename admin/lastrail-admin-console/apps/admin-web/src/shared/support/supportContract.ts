export type CustomerSupportCaseType =
  | "account_auth"
  | "run_completion"
  | "run_reward"
  | "offline_reward"
  | "story_progress"
  | "gacha_recruitment"
  | "integrity_restriction";

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
  noteFormatFields: readonly string[];
  responseTemplateStatuses: readonly string[];
  safeSnippetPlaceholders: readonly string[];
  context: CustomerSupportContextItem[];
  supportContext: {
    readOnly: true;
    mutationEndpointsEnabled: false;
  };
};

export const customerSupportCaseTypes: CustomerSupportCaseType[] = [
  "account_auth",
  "run_completion",
  "run_reward",
  "offline_reward",
  "story_progress",
  "gacha_recruitment",
  "integrity_restriction"
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

export const customerSupportNoteFormatFields = [
  "caseType",
  "accountId",
  "primaryReferenceId",
  "observedStatus",
  "serverEvidence",
  "playerFacingSummary",
  "escalationNeeded"
] as const;

export const customerSupportTemplateStatuses = [
  "internalChecks",
  "playerFacingTemplate",
  "escalateWhen"
] as const;

export const customerSupportSafeSnippetPlaceholders = [
  "server_record_checked",
  "duplicate_replay_existing_result",
  "offline_claimable_cap_applied",
  "first_clear_finish_time_basis",
  "progress_and_text_are_separate",
  "rng_probability_not_disclosed",
  "escalated_for_ops_review"
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
      account_auth: "계정·인증",
      run_completion: "출격 완료",
      run_reward: "출격 보상",
      offline_reward: "오프라인 보상",
      story_progress: "스토리 진행",
      gacha_recruitment: "모집·합류",
      integrity_restriction: "무결성 제한"
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
  noteFormatFields: customerSupportNoteFormatFields,
  responseTemplateStatuses: customerSupportTemplateStatuses,
  safeSnippetPlaceholders: customerSupportSafeSnippetPlaceholders,
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
