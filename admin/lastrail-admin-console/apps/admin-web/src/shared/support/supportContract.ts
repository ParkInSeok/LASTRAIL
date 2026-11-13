import { adminReadValueLabel } from "../api/errorDisplay";

export type CustomerSupportCaseType =
  | "account_auth"
  | "run_completion"
  | "run_reward"
  | "offline_reward"
  | "story_progress"
  | "gacha_recruitment"
  | "integrity_restriction";

export type CustomerSupportSuggestionCaseType =
  | "google_login_link_conflict"
  | "auth_session_expired"
  | "reward_claim_issue"
  | "offline_reward_issue"
  | "gacha_duplicate_guarantee_inquiry"
  | "raid_boss_only_result_inquiry"
  | "start_package_ref_mismatch"
  | "ops_error_observed";

export type CustomerSupportSuggestionCase = {
  caseType: CustomerSupportSuggestionCaseType;
  availability: "연동 대기";
  readOnly: true;
  safeSnippet: string;
  copyActionEnabled: false;
  sendActionEnabled: false;
  ticketCreationEnabled: false;
  escalationWriteEnabled: false;
};

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

export type AccountAuthSessionField =
  | "identityProviders"
  | "providerNames"
  | "linkedProviders"
  | "primaryProvider"
  | "hasGoogleLink"
  | "activeSessionCount"
  | "latestSessionCreatedAt"
  | "latestSessionAt"
  | "latestSessionExpiresAt"
  | "latestAuthAt"
  | "lastLoginAt"
  | "lastLoginAgeSeconds"
  | "lastSeenAt"
  | "lastSeenSource"
  | "concurrentUsers"
  | "concurrentUsersPrecision";

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

export type SupportGachaLaunchEvidenceField =
  | "activeBannerId"
  | "bannerType"
  | "pickupOperatorId"
  | "pullCountVisibility"
  | "pityThresholds"
  | "guaranteeType"
  | "duplicateConversion"
  | "resultFields"
  | "ticketAcquisition"
  | "actOneEliteVisualScope";

export type SupportSummarySectionCode =
  | "account"
  | "auth"
  | "recentRuns"
  | "runRewards"
  | "offlineReward"
  | "storyProgress"
  | "gacha"
  | "loginBonus"
  | "idempotency"
  | "integrity"
  | "opsErrors"
  | "supportNotes";

export type SupportSummarySection = {
  code: SupportSummarySectionCode;
  label: string;
  value: "연동 대기" | "연결됨" | "없음";
  referenceCount: number | "연동 대기";
};

export type SupportAuthSessionItem = {
  field: AccountAuthSessionField;
  label: string;
  value: string;
  note: string;
};

export type SupportOpsErrorCorrelationIds = {
  accountId: string | null;
  runId: string | null;
  rewardPreviewId: string | null;
  rewardClaimId: string | null;
  offlineRewardClaimId: string | null;
  gachaBatchId: string | null;
  gachaBannerId: string | null;
  storyProgressId: string | null;
  integrityEventId: string | null;
  supportTicketId: string | null;
};

export type SupportOpsErrorLink = {
  label: string;
  href: string;
};

export type SupportOpsErrorEvidence = {
  errorLogId: string;
  occurredAt: string;
  environment: string;
  service: string;
  severity: string;
  errorCode: string;
  messageSummary: string;
  status: string;
  source: string;
  requestId: string;
  correlationIds: SupportOpsErrorCorrelationIds;
  links: SupportOpsErrorLink[];
};

export type SupportLoginBonusRewardLine = {
  kind: string;
  id: string;
  quantity: number;
  source: string;
  balanceAfter?: string;
};

export type SupportLoginBonusSummary = {
  bonusId: string;
  claimStatus: "UNCLAIMED" | "CLAIMED" | string;
  claimedAt: string;
  rewardTransactionId: string;
  expectedRewardLines: SupportLoginBonusRewardLine[];
  rewardLineSummary: SupportLoginBonusRewardLine[];
  idempotencyReplayKnown: boolean;
  readOnly: true;
};

export type SupportRecentRunStartMetadata = {
  encounterType: string;
  raidId: string;
  bossId: string;
  bossPhaseRefs: string[];
  startAugmentPackageRef: string;
  startTacticPackageRef: string;
  contentVersion: string;
  balanceVersion: string;
  allowedOperatorSlots: string;
  failureStaminaRefundMarker: {
    status: string;
    resource: string;
    reason: string;
    policyId: string;
    serverOwnedMutation: string;
    fuelLedgerReason: string;
  };
  source: "run_start";
};

export type SupportRecentRunEvidence = {
  runId: string;
  operationId: string;
  risk: string;
  outcome: string;
  rewardPreviewId: string;
  startMetadata: SupportRecentRunStartMetadata;
};

export type SupportAccountSummary = {
  permission: "SUPPORT_READ";
  permissionMode: "ROLE_PERMISSION";
  account: {
    accountId: string;
    displayName: string;
    accountStatus: string;
  };
  authSession: {
    providerOrder: readonly ["GOOGLE", "GUEST"];
    items: SupportAuthSessionItem[];
  };
  recentRuns: SupportRecentRunEvidence[];
  opsErrors: SupportOpsErrorEvidence[];
  loginBonus: SupportLoginBonusSummary;
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
  suggestionCases: readonly CustomerSupportSuggestionCase[];
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
    packageRefPolicy: "server-authored run start package";
    sourceOfTruthExclusion: "reward/economy/story/offline/gacha Source of Truth 아님";
    supportFacingErrorCodes: readonly string[];
    mutationControlsEnabled: false;
  };
  gachaLaunch: {
    readOnly: true;
    scopeStatus: "TESTING_TUNING_DRAFT";
    productionFinal: false;
    seedPromotionApproved: false;
    evidenceFields: readonly SupportGachaLaunchEvidenceField[];
    pullCounts: readonly string[];
    pityThresholds: {
      rare: "10";
      elite: "30";
      pickup: "60";
    };
    guaranteeTypes: readonly string[];
    duplicateConversionFields: readonly string[];
    resultFields: readonly string[];
    ticketAcquisition: {
      totalNonRepeatTickets: 12;
      costItemId: "cur_command_ticket";
      excludedOfflineRepeatRewards: readonly ["cur_command_ticket", "cur_command_ticket_fragment", "cur_special_core"];
      boundary: "launch candidate/draft, not production seed/config promotion";
      firstClearTickets: {
        quantity: 7;
        state: "promoted_local_launch_config";
      };
      loginBonusTickets: {
        quantity: 5;
        state: "support_summary_read_model_available";
      };
      sources: ReadonlyArray<{
        code: string;
        label: string;
        quantity: number;
        repeatable: false;
        state?: "promoted_local_launch_config" | "support_summary_read_model_available";
      }>;
    };
    actOneEliteOperatorIds: readonly string[];
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

export const customerSupportSuggestionCases: CustomerSupportSuggestionCase[] = [
  {
    caseType: "google_login_link_conflict",
    availability: "연동 대기",
    readOnly: true,
    safeSnippet: "서버 기록 기준으로 Google 연동 상태를 확인했습니다.",
    copyActionEnabled: false,
    sendActionEnabled: false,
    ticketCreationEnabled: false,
    escalationWriteEnabled: false
  },
  {
    caseType: "auth_session_expired",
    availability: "연동 대기",
    readOnly: true,
    safeSnippet: "마지막 로그인과 세션 만료 시각을 서버 기록 기준으로 확인했습니다.",
    copyActionEnabled: false,
    sendActionEnabled: false,
    ticketCreationEnabled: false,
    escalationWriteEnabled: false
  },
  {
    caseType: "reward_claim_issue",
    availability: "연동 대기",
    readOnly: true,
    safeSnippet: "보상 미리보기와 수령 기록은 서버 장부 기준으로 확인됩니다.",
    copyActionEnabled: false,
    sendActionEnabled: false,
    ticketCreationEnabled: false,
    escalationWriteEnabled: false
  },
  {
    caseType: "offline_reward_issue",
    availability: "연동 대기",
    readOnly: true,
    safeSnippet: "수령 가능 시간과 보관 한도가 함께 적용됩니다.",
    copyActionEnabled: false,
    sendActionEnabled: false,
    ticketCreationEnabled: false,
    escalationWriteEnabled: false
  },
  {
    caseType: "gacha_duplicate_guarantee_inquiry",
    availability: "연동 대기",
    readOnly: true,
    safeSnippet: "중복 요청은 기존 처리 결과를 다시 보여주는 방식입니다.",
    copyActionEnabled: false,
    sendActionEnabled: false,
    ticketCreationEnabled: false,
    escalationWriteEnabled: false
  },
  {
    caseType: "raid_boss_only_result_inquiry",
    availability: "연동 대기",
    readOnly: true,
    safeSnippet: "레이드는 보스 전용 레이드 결과로 일반 15공세와 분리해 확인합니다.",
    copyActionEnabled: false,
    sendActionEnabled: false,
    ticketCreationEnabled: false,
    escalationWriteEnabled: false
  },
  {
    caseType: "start_package_ref_mismatch",
    availability: "연동 대기",
    readOnly: true,
    safeSnippet: "증강과 지휘전술 패키지는 출격 시작 시점의 서버 기록으로 확인합니다.",
    copyActionEnabled: false,
    sendActionEnabled: false,
    ticketCreationEnabled: false,
    escalationWriteEnabled: false
  },
  {
    caseType: "ops_error_observed",
    availability: "연동 대기",
    readOnly: true,
    safeSnippet: "추가 검토가 필요한 시스템 기록은 읽기 전용 근거로 확인합니다.",
    copyActionEnabled: false,
    sendActionEnabled: false,
    ticketCreationEnabled: false,
    escalationWriteEnabled: false
  }
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
  "loginBonus",
  "idempotency",
  "integrity",
  "opsErrors",
  "supportNotes"
];

export const accountAuthVisibilityFields = [
  "identityProviders",
  "providerNames",
  "linkedProviders",
  "primaryProvider",
  "hasGoogleLink",
  "activeSessionCount",
  "latestSessionCreatedAt",
  "latestSessionAt",
  "latestSessionExpiresAt",
  "latestAuthAt",
  "lastLoginAt",
  "lastLoginAgeSeconds",
  "lastSeenAt",
  "lastSeenSource",
  "concurrentUsers",
  "concurrentUsersPrecision"
] as const;

export const accountAuthProviderDisplayOrder = ["GOOGLE", "GUEST"] as const;

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

export const customerSupportGachaLaunchEvidenceFields: SupportGachaLaunchEvidenceField[] = [
  "activeBannerId",
  "bannerType",
  "pickupOperatorId",
  "pullCountVisibility",
  "pityThresholds",
  "guaranteeType",
  "duplicateConversion",
  "resultFields",
  "ticketAcquisition",
  "actOneEliteVisualScope"
];

export const customerSupportGachaLaunchPolicy = {
  readOnly: true,
  scopeStatus: "TESTING_TUNING_DRAFT",
  productionFinal: false,
  seedPromotionApproved: false,
  evidenceFields: customerSupportGachaLaunchEvidenceFields,
  pullCounts: ["1-pull", "10-pull"],
  pityThresholds: {
    rare: "10",
    elite: "30",
    pickup: "60"
  },
  guaranteeTypes: ["NONE", "RARE_OR_HIGHER_10", "ELITE_30", "PICKUP_ELITE_60"],
  duplicateConversionFields: [
    "displayItemId",
    "persistedTokenPattern",
    "rareConversionQuantityCandidate",
    "eliteConversionQuantityCandidate"
  ],
  resultFields: [
    "pullSequence",
    "resultRarity",
    "resultType",
    "resultId",
    "reward",
    "isPickup",
    "wasDuplicate",
    "duplicateConversion"
  ],
  ticketAcquisition: {
    totalNonRepeatTickets: 12,
    costItemId: "cur_command_ticket",
    excludedOfflineRepeatRewards: ["cur_command_ticket", "cur_command_ticket_fragment", "cur_special_core"],
    boundary: "launch candidate/draft, not production seed/config promotion",
    firstClearTickets: { quantity: 7, state: "promoted_local_launch_config" },
    loginBonusTickets: { quantity: 5, state: "support_summary_read_model_available" },
    sources: [
      {
        code: "launch_login_bonus",
        label: "login bonus",
        quantity: 5,
        repeatable: false,
        state: "support_summary_read_model_available"
      },
      {
        code: "opn_03_water_stop_first_clear",
        label: "opn_03_water_stop first clear",
        quantity: 1,
        repeatable: false,
        state: "promoted_local_launch_config"
      },
      {
        code: "opn_05_night_boarders_first_clear",
        label: "opn_05_night_boarders first clear",
        quantity: 1,
        repeatable: false,
        state: "promoted_local_launch_config"
      },
      {
        code: "opn_06_broken_line_first_clear",
        label: "opn_06_broken_line first clear",
        quantity: 2,
        repeatable: false,
        state: "promoted_local_launch_config"
      },
      {
        code: "raid_raider_king_engine_story_raid_first_clear",
        label: "raid_raider_king_engine story raid first clear",
        quantity: 3,
        repeatable: false,
        state: "promoted_local_launch_config"
      }
    ]
  },
  actOneEliteOperatorIds: [
    "op_vex",
    "op_nyx",
    "op_sable",
    "op_marrow",
    "op_iris",
    "op_rail",
    "op_anchor",
    "op_lumen",
    "op_cinder",
    "op_warden"
  ],
  mutationControlsEnabled: false
} as const;

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
  loginBonus: "로그인 보너스 수령",
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

export function customerSupportSuggestionCaseLabel(caseType: CustomerSupportSuggestionCaseType | string): string {
  return (
    {
      google_login_link_conflict: "Google 로그인/연동 충돌",
      auth_session_expired: "인증/세션 만료",
      reward_claim_issue: "보상 수령 문의",
      offline_reward_issue: "오프라인 보상 문의",
      gacha_duplicate_guarantee_inquiry: "모집 중복/보장 문의",
      raid_boss_only_result_inquiry: "레이드 보스 결과 문의",
      start_package_ref_mismatch: "시작 패키지 ref 불일치",
      ops_error_observed: "시스템 에러 관측"
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
      google: "Google",
      GUEST: "게스트",
      GOOGLE: "Google"
    }[provider] ?? provider
  );
}

export function accountAuthSessionFieldLabel(field: AccountAuthSessionField | string): string {
  return (
    {
      identityProviders: "식별 제공자",
      providerNames: "Google/게스트 제공자",
      linkedProviders: "Google 연동 제공자",
      primaryProvider: "주 제공자",
      hasGoogleLink: "Google 연동 여부",
      activeSessionCount: "활성 세션 수",
      latestSessionCreatedAt: "최신 세션 생성 시각",
      latestSessionAt: "최신 세션 시각",
      latestSessionExpiresAt: "최신 세션 만료 시각",
      latestAuthAt: "최신 인증 시각",
      lastLoginAt: "마지막 로그인 시각",
      lastLoginAgeSeconds: "마지막 로그인 경과 초",
      lastSeenAt: "마지막 활동 시각",
      lastSeenSource: "마지막 활동 소스",
      concurrentUsers: "동접자 집계 상태",
      concurrentUsersPrecision: "동접자 집계 정밀도"
    }[field] ?? field
  );
}

function normalizeProvider(provider: string): "GOOGLE" | "GUEST" | string {
  const normalized = provider.toUpperCase();
  if (normalized === "GOOGLE") return "GOOGLE";
  if (normalized === "GUEST") return "GUEST";
  return provider;
}

function providerListValue(value: unknown, emptyLabel = "없음"): string {
  if (!Array.isArray(value)) return emptyLabel;
  const normalized = value.filter((provider): provider is string => typeof provider === "string").map(normalizeProvider);
  const ordered = [
    ...accountAuthProviderDisplayOrder.filter((provider) => normalized.includes(provider)),
    ...normalized.filter((provider) => !accountAuthProviderDisplayOrder.includes(provider as "GOOGLE" | "GUEST"))
  ];
  return ordered.length > 0 ? ordered.map(accountAuthProviderLabel).join(" / ") : emptyLabel;
}

export function formatConcurrentUsersDisplay(value: unknown, precision: unknown): string {
  if (precision === "UNAVAILABLE" && (value === null || value === undefined)) return "집계 불가 (UNAVAILABLE)";
  if (typeof value !== "number") return "연동 대기";
  return precision === "ESTIMATED"
    ? `launch backlog 지표 (ESTIMATED): 약 ${value}`
    : `launch backlog 지표 (${typeof precision === "string" && precision ? precision : "UNKNOWN"}): ${value}`;
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
  suggestionCases: customerSupportSuggestionCases,
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
    packageRefPolicy: "server-authored run start package",
    sourceOfTruthExclusion: "reward/economy/story/offline/gacha Source of Truth 아님",
    supportFacingErrorCodes: ["package_ref_mismatch", "contract_violation"],
    mutationControlsEnabled: false
  },
  gachaLaunch: customerSupportGachaLaunchPolicy,
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

function readStringValue(value: unknown, emptyLabel = "없음"): string {
  return adminReadValueLabel({ kind: "success", value, emptyLabel });
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

function authSessionValue(field: AccountAuthSessionField, auth: Record<string, unknown> | null): string {
  if (!auth) return "연동 대기";
  const value = auth[field];
  if (field === "identityProviders" || field === "providerNames" || field === "linkedProviders") {
    return providerListValue(value);
  }
  if (field === "primaryProvider") {
    return typeof value === "string" ? accountAuthProviderLabel(value) : "없음";
  }
  if (field === "hasGoogleLink") {
    if (value === true) return "연동됨";
    if (value === false) return "미연동";
    return "없음";
  }
  if (field === "lastLoginAgeSeconds") {
    return typeof value === "number" ? `${value}초` : "없음";
  }
  if (field === "activeSessionCount") {
    return typeof value === "number" ? `${value}` : "없음";
  }
  if (field === "concurrentUsers") {
    return formatConcurrentUsersDisplay(value, auth.concurrentUsersPrecision);
  }
  if (field === "concurrentUsersPrecision") {
    return typeof value === "string" && value ? value : "없음";
  }
  return readStringValue(value);
}

function authSessionItems(auth: Record<string, unknown> | null): SupportAuthSessionItem[] {
  return accountAuthVisibilityFields.map((field) => ({
    field,
    label: accountAuthSessionFieldLabel(field),
    value: authSessionValue(field, auth),
    note:
      field === "concurrentUsers"
        ? "UNAVAILABLE/null은 확정 동접자 수 아님"
        : `${field} read-only auth/session evidence`
  }));
}

function sectionFrom(code: SupportSummarySectionCode, value: unknown): SupportSummarySection {
  const referenceCount = code === "account" ? 1 : countReferences(value);
  return {
    code,
    label: customerSupportSummarySectionLabel(code),
    value: referenceCount > 0 ? "연결됨" : "없음",
    referenceCount
  };
}

function stringOrNull(value: unknown): string | null {
  return typeof value === "string" && value ? value : null;
}

function supportOpsErrorLink(record: unknown): SupportOpsErrorLink | null {
  const link = asRecord(record);
  const label = stringOrNull(link?.label);
  const href = stringOrNull(link?.href);
  return label && href ? { label, href } : null;
}

function supportOpsErrorLinks(value: unknown): SupportOpsErrorLink[] {
  if (!Array.isArray(value)) return [];
  return value.map(supportOpsErrorLink).filter((link): link is SupportOpsErrorLink => Boolean(link));
}

function supportOpsErrorEvidence(value: unknown): SupportOpsErrorEvidence | null {
  const record = asRecord(value);
  if (!record) return null;
  return {
    errorLogId: readStringValue(record.errorLogId),
    occurredAt: readStringValue(record.occurredAt),
    environment: readStringValue(record.environment),
    service: readStringValue(record.service),
    severity: readStringValue(record.severity),
    errorCode: readStringValue(record.errorCode),
    messageSummary: readStringValue(record.messageSummary),
    status: readStringValue(record.status),
    source: readStringValue(record.source),
    requestId: readStringValue(record.requestId),
    correlationIds: {
      accountId: stringOrNull(record.accountId),
      runId: stringOrNull(record.runId),
      rewardPreviewId: stringOrNull(record.rewardPreviewId),
      rewardClaimId: stringOrNull(record.rewardClaimId),
      offlineRewardClaimId: stringOrNull(record.offlineRewardClaimId),
      gachaBatchId: stringOrNull(record.gachaBatchId),
      gachaBannerId: stringOrNull(record.gachaBannerId),
      storyProgressId: stringOrNull(record.storyProgressId),
      integrityEventId: stringOrNull(record.integrityEventId),
      supportTicketId: stringOrNull(record.supportTicketId)
    },
    links: supportOpsErrorLinks(record.links)
  };
}

function supportOpsErrorEvidenceList(value: unknown): SupportOpsErrorEvidence[] {
  if (!Array.isArray(value)) return [];
  return value.map(supportOpsErrorEvidence).filter((item): item is SupportOpsErrorEvidence => Boolean(item));
}

function supportLoginBonusRewardLine(value: unknown): SupportLoginBonusRewardLine | null {
  const record = asRecord(value);
  if (!record) return null;
  const line: SupportLoginBonusRewardLine = {
    kind: stringValue(record.kind),
    id: stringValue(record.id),
    quantity: typeof record.quantity === "number" ? record.quantity : Number(record.quantity) || 0,
    source: stringValue(record.source)
  };
  if (record.balanceAfter !== undefined && record.balanceAfter !== null) {
    line.balanceAfter = nullableStringValue(record.balanceAfter);
  }
  return line;
}

function supportLoginBonusRewardLines(value: unknown): SupportLoginBonusRewardLine[] {
  if (!Array.isArray(value)) return [];
  return value
    .map(supportLoginBonusRewardLine)
    .filter((item): item is SupportLoginBonusRewardLine => Boolean(item));
}

function supportLoginBonusSummary(value: unknown): SupportLoginBonusSummary {
  const record = asRecord(value);
  const fallback = record ? "없음" : "연동 대기";
  return {
    bonusId: stringValue(record?.bonusId, fallback),
    claimStatus: stringValue(record?.claimStatus, fallback),
    claimedAt: stringValue(record?.claimedAt, fallback),
    rewardTransactionId: stringValue(record?.rewardTransactionId, fallback),
    expectedRewardLines: supportLoginBonusRewardLines(record?.expectedRewardLines),
    rewardLineSummary: supportLoginBonusRewardLines(record?.rewardLineSummary),
    idempotencyReplayKnown: record?.idempotencyReplayKnown === true,
    readOnly: true
  };
}

function stringListValue(value: unknown): string[] {
  if (!Array.isArray(value)) return [];
  return value.filter((item): item is string => typeof item === "string" && item.length > 0);
}

function nullableStringValue(value: unknown): string {
  if (typeof value === "number") return String(value);
  return readStringValue(value);
}

function supportRunStartMetadata(record: Record<string, unknown>): SupportRecentRunStartMetadata {
  const metadata = asRecord(record.serverPreviewMetadata) ?? asRecord(record.metadata) ?? {};
  const failureStaminaRefundMarker = asRecord(metadata.failureStaminaRefundMarker);
  return {
    encounterType: readStringValue(metadata.encounterType),
    raidId: readStringValue(metadata.raidId),
    bossId: readStringValue(metadata.bossId),
    bossPhaseRefs: stringListValue(metadata.bossPhaseRefs),
    startAugmentPackageRef: readStringValue(metadata.startAugmentPackageRef),
    startTacticPackageRef: readStringValue(metadata.startTacticPackageRef),
    contentVersion: readStringValue(metadata.contentVersion),
    balanceVersion: readStringValue(metadata.balanceVersion),
    allowedOperatorSlots: nullableStringValue(metadata.allowedOperatorSlots),
    failureStaminaRefundMarker: {
      status: readStringValue(failureStaminaRefundMarker?.status),
      resource: readStringValue(failureStaminaRefundMarker?.resource),
      reason: readStringValue(failureStaminaRefundMarker?.reason),
      policyId: readStringValue(failureStaminaRefundMarker?.policyId),
      serverOwnedMutation: readStringValue(failureStaminaRefundMarker?.serverOwnedMutation),
      fuelLedgerReason: readStringValue(failureStaminaRefundMarker?.fuelLedgerReason)
    },
    source: "run_start"
  };
}

function supportRecentRunEvidence(value: unknown): SupportRecentRunEvidence | null {
  const record = asRecord(value);
  if (!record) return null;
  return {
    runId: readStringValue(record.runId),
    operationId: readStringValue(record.operationId),
    risk: readStringValue(record.risk),
    outcome: readStringValue(record.outcome),
    rewardPreviewId: readStringValue(record.rewardPreviewId),
    startMetadata: supportRunStartMetadata(record)
  };
}

function supportRecentRunEvidenceList(value: unknown): SupportRecentRunEvidence[] {
  if (!Array.isArray(value)) return [];
  return value.map(supportRecentRunEvidence).filter((item): item is SupportRecentRunEvidence => Boolean(item));
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
    authSession: {
      providerOrder: accountAuthProviderDisplayOrder,
      items: authSessionItems(null)
    },
    recentRuns: [],
    opsErrors: [],
    loginBonus: supportLoginBonusSummary(null),
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
  const auth = asRecord(record.auth);
  const supportContext = asRecord(record.supportContext);

  return {
    permission: "SUPPORT_READ",
    permissionMode: "ROLE_PERMISSION",
    account: {
      accountId: stringValue(account?.accountId, fallbackAccountId),
      displayName: readStringValue(account?.displayName),
      accountStatus: readStringValue(account?.accountStatus)
    },
    authSession: {
      providerOrder: accountAuthProviderDisplayOrder,
      items: authSessionItems(auth)
    },
    recentRuns: supportRecentRunEvidenceList(record.recentRuns),
    opsErrors: supportOpsErrorEvidenceList(record.opsErrors),
    loginBonus: supportLoginBonusSummary(record.loginBonus),
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
