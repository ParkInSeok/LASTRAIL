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

export type CustomerSupportEscalationSeverity = "info" | "needs_review" | "high_risk" | "blocked";

export type CustomerSupportEscalationCategory =
  | "reward_claim_missing_ledger"
  | "reward_preview_without_claim"
  | "offline_state_conflict"
  | "offline_claim_duplicate_mismatch"
  | "story_unlock_missing"
  | "gacha_guarantee_conflict"
  | "integrity_restricted_reward"
  | "account_identity_conflict"
  | "narrative_content_missing"
  | "expected_behavior";

export type CustomerSupportEscalationAction =
  | "respond_with_template"
  | "request_more_player_context"
  | "escalate_to_operator_review"
  | "escalate_to_owner_review"
  | "mark_as_known_issue_candidate"
  | "attach_read_only_evidence";

export type CustomerSupportRepairCategory =
  | "manual_currency_item_grant"
  | "offline_reward_timestamp_reset"
  | "story_progress_unlock_mutation"
  | "gacha_result_reroll"
  | "reward_economy_ledger_edit"
  | "localization_narrative_publish";

export type CustomerSupportRepairLaunchStatus = "Disabled";

export type AccountAuthProvider = "guest" | "google";
export type AccountAuthState =
  | "guest_only"
  | "google_linked"
  | "restore_candidate"
  | "identity_conflict"
  | "provider_unlinked"
  | "session_active"
  | "session_expired";
export type AccountAuthLinkStatus = "LINKED" | "ALREADY_LINKED";
export type AccountAuthErrorCode =
  | "google_identity_already_linked"
  | "provider_identity_conflict"
  | "google_token_invalid"
  | "google_auth_config_missing";

export type SupportRunCombatEvidenceField =
  | "encounterType"
  | "raidId"
  | "bossId"
  | "bossDefeated"
  | "completedPhaseCount"
  | "encounterDurationSeconds"
  | "startAugmentPackageRef"
  | "startTacticPackageRef"
  | "selectedAugmentIds"
  | "selectedTacticIds"
  | "source";

export type SupportRunEncounterType = "ORDINARY_15_WAVE" | "RAID_BOSS";

export type SupportSummarySectionCode =
  | "account"
  | "auth"
  | "recentRuns"
  | "runRewards"
  | "offlineReward"
  | "storyProgress"
  | "gacha"
  | "idempotency"
  | "integrity"
  | "opsErrors"
  | "supportNotes";

export type SupportSummarySection = {
  code: SupportSummarySectionCode;
  label: string;
  value: "연동 대기" | "연결됨";
  referenceCount: number | "연동 대기";
};

export type SupportAccountSummary = {
  permission: "SUPPORT_READ";
  permissionMode: "ROLE_PERMISSION";
  account: {
    accountId: string;
    displayName: string;
    accountStatus: string;
  };
  sections: SupportSummarySection[];
  supportContext: {
    readOnly: true;
    repairEnabled: false;
    mutationEndpointsEnabled: false;
  };
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
  escalation: {
    readOnly: true;
    writeEnabled: false;
    severities: readonly CustomerSupportEscalationSeverity[];
    categories: readonly CustomerSupportEscalationCategory[];
    recommendedActions: readonly CustomerSupportEscalationAction[];
    evidenceFields: readonly string[];
    futurePermissions: readonly string[];
  };
  repairCompensation: {
    readOnly: true;
    launchStatus: CustomerSupportRepairLaunchStatus;
    futurePermissions: readonly string[];
    auditFields: readonly string[];
    categories: Array<{
      code: CustomerSupportRepairCategory;
      label: string;
      status: CustomerSupportRepairLaunchStatus;
    }>;
  };
  accountAuth: {
    readOnly: true;
    providers: readonly AccountAuthProvider[];
    states: readonly AccountAuthState[];
    linkStatuses: readonly AccountAuthLinkStatus[];
    errorCodes: readonly AccountAuthErrorCode[];
    visibilityFields: readonly string[];
  };
  runCombat: {
    readOnly: true;
    encounterTypes: readonly SupportRunEncounterType[];
    evidenceFields: readonly SupportRunCombatEvidenceField[];
    startPackageSource: "run_start";
    mutationControlsEnabled: false;
  };
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

export const customerSupportSummarySectionCodes: SupportSummarySectionCode[] = [
  "account",
  "auth",
  "recentRuns",
  "runRewards",
  "offlineReward",
  "storyProgress",
  "gacha",
  "idempotency",
  "integrity",
  "opsErrors",
  "supportNotes"
];

export const accountAuthVisibilityFields = [
  "providerNames",
  "linkedProviders",
  "accountState",
  "linkStatus",
  "lastLoginAt",
  "lastLoginAge",
  "activeSessionCount",
  "latestSessionAt",
  "sanitizedIdentityRef"
] as const;

export const accountAuthProviders: AccountAuthProvider[] = ["guest", "google"];

export const accountAuthStates: AccountAuthState[] = [
  "guest_only",
  "google_linked",
  "restore_candidate",
  "identity_conflict",
  "provider_unlinked",
  "session_active",
  "session_expired"
];

export const accountAuthLinkStatuses: AccountAuthLinkStatus[] = ["LINKED", "ALREADY_LINKED"];

export const accountAuthErrorCodes: AccountAuthErrorCode[] = [
  "google_identity_already_linked",
  "provider_identity_conflict",
  "google_token_invalid",
  "google_auth_config_missing"
];

export const customerSupportCombatEvidenceFields: SupportRunCombatEvidenceField[] = [
  "encounterType",
  "raidId",
  "bossId",
  "bossDefeated",
  "completedPhaseCount",
  "encounterDurationSeconds",
  "startAugmentPackageRef",
  "startTacticPackageRef",
  "selectedAugmentIds",
  "selectedTacticIds",
  "source"
];

export const customerSupportRunEncounterTypes: SupportRunEncounterType[] = ["ORDINARY_15_WAVE", "RAID_BOSS"];

export const customerSupportCombatForbiddenExamplesOutOfScope = [
  "raw combat timeline",
  "animation frame",
  "VFX",
  "SFX",
  "camera",
  "hit-stop",
  "debug payload"
] as const;

export const customerSupportOpsErrorsEvidencePolicy = {
  field: "opsErrors",
  launchRole: "read-only evidence",
  repairMechanism: false,
  futureLimit: 5,
  ordering: "critical first, latest first"
} as const;

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

export const customerSupportEscalationSeverities: CustomerSupportEscalationSeverity[] = [
  "info",
  "needs_review",
  "high_risk",
  "blocked"
];

export const customerSupportEscalationCategories: CustomerSupportEscalationCategory[] = [
  "reward_claim_missing_ledger",
  "reward_preview_without_claim",
  "offline_state_conflict",
  "offline_claim_duplicate_mismatch",
  "story_unlock_missing",
  "gacha_guarantee_conflict",
  "integrity_restricted_reward",
  "account_identity_conflict",
  "narrative_content_missing",
  "expected_behavior"
];

export const customerSupportEscalationActions: CustomerSupportEscalationAction[] = [
  "respond_with_template",
  "request_more_player_context",
  "escalate_to_operator_review",
  "escalate_to_owner_review",
  "mark_as_known_issue_candidate",
  "attach_read_only_evidence"
];

export const customerSupportEscalationEvidenceFields = [
  "severity",
  "category",
  "recommendedAction",
  "primaryReferenceId",
  "readOnlyEvidencePackage"
] as const;

export const customerSupportEscalationFuturePermissions = [
  "SUPPORT_ESCALATION_READ",
  "SUPPORT_ESCALATION_WRITE",
  "SUPPORT_REPAIR_PREVIEW",
  "SUPPORT_REPAIR_APPROVE",
  "SUPPORT_REPAIR_EXECUTE"
] as const;

export const customerSupportRepairCategories: CustomerSupportRepairCategory[] = [
  "manual_currency_item_grant",
  "offline_reward_timestamp_reset",
  "story_progress_unlock_mutation",
  "gacha_result_reroll",
  "reward_economy_ledger_edit",
  "localization_narrative_publish"
];

export const customerSupportRepairFuturePermissions = [
  "SUPPORT_ESCALATION_READ",
  "SUPPORT_ESCALATION_WRITE",
  "SUPPORT_REPAIR_PREVIEW",
  "SUPPORT_REPAIR_APPROVE",
  "SUPPORT_REPAIR_EXECUTE"
] as const;

export const customerSupportRepairAuditFields = [
  "supportTicketId",
  "accountId",
  "operatorId",
  "approverId",
  "permissionUsed",
  "reasonCode",
  "affectedIds",
  "beforeAfterSanitizedSummary"
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

const summarySectionLabels: Record<SupportSummarySectionCode, string> = {
  account: "계정 상태/프로필",
  auth: "인증 제공자/세션",
  recentRuns: "최근 출격",
  runRewards: "출격 보상 미리보기/수령",
  offlineReward: "오프라인 보상 상태/수령",
  storyProgress: "스토리 진행/해금",
  gacha: "최근 모집 batch/보장",
  idempotency: "Idempotency replay 기록",
  integrity: "무결성 신호/리뷰",
  opsErrors: "시스템 에러 로그",
  supportNotes: "안전 템플릿 힌트"
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

export function customerSupportSummarySectionLabel(section: SupportSummarySectionCode | string): string {
  return summarySectionLabels[section as SupportSummarySectionCode] ?? section;
}

export function customerSupportEscalationSeverityLabel(severity: CustomerSupportEscalationSeverity | string): string {
  return (
    {
      info: "안내",
      needs_review: "검토 필요",
      high_risk: "고위험",
      blocked: "처리 보류"
    }[severity] ?? severity
  );
}

export function customerSupportEscalationCategoryLabel(category: CustomerSupportEscalationCategory | string): string {
  return (
    {
      reward_claim_missing_ledger: "보상 수령 장부 누락",
      reward_preview_without_claim: "보상 미리보기 후 수령 누락",
      offline_state_conflict: "오프라인 보상 상태 충돌",
      offline_claim_duplicate_mismatch: "오프라인 중복 수령 불일치",
      story_unlock_missing: "스토리 해금 누락",
      gacha_guarantee_conflict: "모집 보장 상태 충돌",
      integrity_restricted_reward: "무결성 보상 제한",
      account_identity_conflict: "계정 식별 충돌",
      narrative_content_missing: "내러티브 콘텐츠 누락",
      expected_behavior: "정상 동작 안내"
    }[category] ?? category
  );
}

export function customerSupportEscalationActionLabel(action: CustomerSupportEscalationAction | string): string {
  return (
    {
      respond_with_template: "템플릿으로 응답",
      request_more_player_context: "플레이어 추가 정보 요청",
      escalate_to_operator_review: "운영자 검토 전달",
      escalate_to_owner_review: "OWNER 검토 전달",
      mark_as_known_issue_candidate: "알려진 이슈 후보 표시",
      attach_read_only_evidence: "읽기 전용 근거 첨부"
    }[action] ?? action
  );
}

export function customerSupportRepairCategoryLabel(category: CustomerSupportRepairCategory | string): string {
  return (
    {
      manual_currency_item_grant: "재화/아이템 수동 지급",
      offline_reward_timestamp_reset: "오프라인 보상 시각 초기화",
      story_progress_unlock_mutation: "스토리 진행/해금 변경",
      gacha_result_reroll: "모집 결과 재추첨",
      reward_economy_ledger_edit: "보상/경제 장부 편집",
      localization_narrative_publish: "현지화/내러티브 게시"
    }[category] ?? category
  );
}

export function customerSupportRepairLaunchStatusLabel(status: CustomerSupportRepairLaunchStatus | string): string {
  return (
    {
      Disabled: "비활성"
    }[status] ?? status
  );
}

export function accountAuthProviderLabel(provider: AccountAuthProvider | string): string {
  return (
    {
      guest: "게스트",
      google: "Google"
    }[provider] ?? provider
  );
}

export function accountAuthStateLabel(state: AccountAuthState | string): string {
  return (
    {
      guest_only: "게스트 전용",
      google_linked: "Google 연동",
      restore_candidate: "복구 후보",
      identity_conflict: "계정 식별 충돌",
      provider_unlinked: "제공자 연결 해제",
      session_active: "세션 활성",
      session_expired: "세션 만료"
    }[state] ?? state
  );
}

export function accountAuthLinkStatusLabel(status: AccountAuthLinkStatus | string): string {
  return (
    {
      LINKED: "연동됨",
      ALREADY_LINKED: "이미 연동됨"
    }[status] ?? status
  );
}

export function accountAuthErrorCodeLabel(code: AccountAuthErrorCode | string): string {
  return (
    {
      google_identity_already_linked: "Google 계정 이미 연동됨",
      provider_identity_conflict: "제공자 계정 식별 충돌",
      google_token_invalid: "Google 토큰 유효하지 않음",
      google_auth_config_missing: "Google 인증 설정 누락"
    }[code] ?? code
  );
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
  escalation: {
    readOnly: true,
    writeEnabled: false,
    severities: customerSupportEscalationSeverities,
    categories: customerSupportEscalationCategories,
    recommendedActions: customerSupportEscalationActions,
    evidenceFields: customerSupportEscalationEvidenceFields,
    futurePermissions: customerSupportEscalationFuturePermissions
  },
  repairCompensation: {
    readOnly: true,
    launchStatus: "Disabled",
    futurePermissions: customerSupportRepairFuturePermissions,
    auditFields: customerSupportRepairAuditFields,
    categories: customerSupportRepairCategories.map((category) => ({
      code: category,
      label: customerSupportRepairCategoryLabel(category),
      status: "Disabled"
    }))
  },
  accountAuth: {
    readOnly: true,
    providers: accountAuthProviders,
    states: accountAuthStates,
    linkStatuses: accountAuthLinkStatuses,
    errorCodes: accountAuthErrorCodes.filter((code) => code !== "google_token_invalid"),
    visibilityFields: accountAuthVisibilityFields
  },
  runCombat: {
    readOnly: true,
    encounterTypes: customerSupportRunEncounterTypes,
    evidenceFields: customerSupportCombatEvidenceFields,
    startPackageSource: "run_start",
    mutationControlsEnabled: false
  },
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

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value) ? (value as Record<string, unknown>) : null;
}

function stringValue(value: unknown, fallback = "연동 대기"): string {
  return typeof value === "string" && value ? value : fallback;
}

function countReferences(value: unknown): number {
  if (Array.isArray(value)) return value.length;
  const record = asRecord(value);
  if (!record) return 0;
  return Object.values(record).filter((entry) => {
    if (Array.isArray(entry)) return entry.length > 0;
    return entry !== null && entry !== undefined;
  }).length;
}

function sectionFrom(code: SupportSummarySectionCode, value: unknown): SupportSummarySection {
  const referenceCount = code === "account" ? 1 : countReferences(value);
  return {
    code,
    label: customerSupportSummarySectionLabel(code),
    value: referenceCount > 0 ? "연결됨" : "연동 대기",
    referenceCount
  };
}

export function emptySupportAccountSummary(accountId = "연동 대기"): SupportAccountSummary {
  return {
    permission: "SUPPORT_READ",
    permissionMode: "ROLE_PERMISSION",
    account: {
      accountId,
      displayName: "연동 대기",
      accountStatus: "연동 대기"
    },
    sections: customerSupportSummarySectionCodes.map((code) => ({
      code,
      label: customerSupportSummarySectionLabel(code),
      value: "연동 대기",
      referenceCount: "연동 대기"
    })),
    supportContext: {
      readOnly: true,
      repairEnabled: false,
      mutationEndpointsEnabled: false
    }
  };
}

export function mapSupportAccountSummaryResponse(response: unknown, fallbackAccountId = "연동 대기"): SupportAccountSummary {
  const record = asRecord(response);
  if (!record) return emptySupportAccountSummary(fallbackAccountId);

  const account = asRecord(record.account);
  const supportContext = asRecord(record.supportContext);

  return {
    permission: "SUPPORT_READ",
    permissionMode: "ROLE_PERMISSION",
    account: {
      accountId: stringValue(account?.accountId, fallbackAccountId),
      displayName: stringValue(account?.displayName),
      accountStatus: stringValue(account?.accountStatus)
    },
    sections: customerSupportSummarySectionCodes.map((code) => {
      if (code === "account") return sectionFrom(code, account);
      return sectionFrom(code, record[code]);
    }),
    supportContext: {
      readOnly: true,
      repairEnabled: supportContext?.repairEnabled === true ? false : false,
      mutationEndpointsEnabled: supportContext?.mutationEndpointsEnabled === true ? false : false
    }
  };
}
