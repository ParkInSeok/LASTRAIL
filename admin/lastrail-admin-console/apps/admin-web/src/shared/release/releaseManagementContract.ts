export type ReleaseManagementTargetAxis = "targetId" | "environment" | "platform" | "channel";

export type ReleaseManagementReadState =
  | "unset"
  | "no_permission"
  | "loading"
  | "empty"
  | "conflict"
  | "publish_failed"
  | "verification_in_progress"
  | "target_unavailable"
  | "r2_unconfigured";

export type ReleaseManagementCandidateField = {
  key:
    | "releaseId"
    | "appMinimumBuild"
    | "appRecommendedBuild"
    | "contentVersion"
    | "contentCatalogId"
    | "contentCatalogHash"
    | "protocolVersion"
    | "updateNotice"
    | "storeUpdateUrl"
    | "platform"
    | "channel";
  label: string;
  value: "서버 계약 대기";
  input: "text" | "textarea" | "select";
  note: string;
};

export type ReleaseManagementActionCandidate = {
  code:
    | "save_draft"
    | "apply_internal_testing"
    | "approve_candidate"
    | "prestage_production"
    | "promote_pointer"
    | "open_release"
    | "rollback_release";
  label: string;
  enabled: false;
  permissionStatus: "서버 계약 대기";
  requiresTargetConfirmation: true;
  requiresReason: true;
  note: string;
};

export type ReleaseManagementValidationCheck = {
  code:
    | "integer_version_compare"
    | "compatibility_range"
    | "catalog_hash_exists"
    | "allowed_target_paths"
    | "store_update_availability"
    | "approved_content_hash"
    | "public_json_redaction"
    | "etag_revision_match"
    | "target_permission_match"
    | "dev_to_prd_artifact_revalidation";
  label: string;
  status: "서버 계약 대기";
  note: string;
};

export type ReleaseManagementTargetRegistryPolicy = {
  serverOwned: true;
  targetIsolation: string;
  browserSubmittedInfrastructureAllowed: false;
  globalCurrentJsonShared: false;
  conditionalPointerUpdateRequired: true;
  remoteR2WritesApproved: false;
  productionExecutionApproved: false;
  publicTestFlagGrantsInternalAccess: false;
};

export type ReleaseManagementSnapshot = {
  serverContractStatus: "서버 계약 대기";
  spreadsheetIntegrationEnabled: false;
  targetAxes: readonly ReleaseManagementTargetAxis[];
  readStates: readonly ReleaseManagementReadState[];
  targetRegistry: ReleaseManagementTargetRegistryPolicy;
  candidateFields: readonly ReleaseManagementCandidateField[];
  actions: readonly ReleaseManagementActionCandidate[];
  validationChecks: readonly ReleaseManagementValidationCheck[];
  safetyChecks: readonly string[];
  publicJsonBoundary: {
    internalApprovalsIncluded: false;
    diagnosticsIncluded: false;
    auditHistoryIncluded: false;
  };
};

export const releaseManagementTargetAxes: ReleaseManagementTargetAxis[] = [
  "targetId",
  "environment",
  "platform",
  "channel"
];

export const releaseManagementReadStates: ReleaseManagementReadState[] = [
  "unset",
  "no_permission",
  "loading",
  "empty",
  "conflict",
  "publish_failed",
  "verification_in_progress",
  "target_unavailable",
  "r2_unconfigured"
];

export const releaseManagementTargetRegistryPolicy: ReleaseManagementTargetRegistryPolicy = {
  serverOwned: true,
  targetIsolation: "targetId + environment + platform + channel",
  browserSubmittedInfrastructureAllowed: false,
  globalCurrentJsonShared: false,
  conditionalPointerUpdateRequired: true,
  remoteR2WritesApproved: false,
  productionExecutionApproved: false,
  publicTestFlagGrantsInternalAccess: false
};

export const releaseManagementCandidateFields: ReleaseManagementCandidateField[] = [
  {
    key: "releaseId",
    label: "Release ID",
    value: "서버 계약 대기",
    input: "text",
    note: "release id는 app/content/protocol id와 분리합니다"
  },
  {
    key: "appMinimumBuild",
    label: "앱 최소 빌드",
    value: "서버 계약 대기",
    input: "text",
    note: "정수 비교 가능한 minimum app build"
  },
  {
    key: "appRecommendedBuild",
    label: "앱 권장 빌드",
    value: "서버 계약 대기",
    input: "text",
    note: "권장 업데이트 안내용 app build"
  },
  {
    key: "contentVersion",
    label: "콘텐츠 버전",
    value: "서버 계약 대기",
    input: "text",
    note: "client-config와 호환되는 콘텐츠 버전"
  },
  {
    key: "contentCatalogId",
    label: "콘텐츠 catalog ID",
    value: "서버 계약 대기",
    input: "text",
    note: "대상 registry가 허용한 catalog path만 서버가 검증"
  },
  {
    key: "contentCatalogHash",
    label: "콘텐츠 catalog hash",
    value: "서버 계약 대기",
    input: "text",
    note: "승인된 artifact hash와 대상별 접근성을 확인"
  },
  {
    key: "protocolVersion",
    label: "프로토콜 버전",
    value: "서버 계약 대기",
    input: "text",
    note: "서버 API/client protocol compatibility range"
  },
  {
    key: "updateNotice",
    label: "업데이트 안내",
    value: "서버 계약 대기",
    input: "textarea",
    note: "public JSON에는 내부 승인/진단 정보를 포함하지 않습니다"
  },
  {
    key: "storeUpdateUrl",
    label: "스토어 업데이트 URL",
    value: "서버 계약 대기",
    input: "text",
    note: "플랫폼별 허용 URL만 서버가 검증"
  },
  {
    key: "platform",
    label: "플랫폼",
    value: "서버 계약 대기",
    input: "select",
    note: "Android/Windows 등 플랫폼 축"
  },
  {
    key: "channel",
    label: "채널",
    value: "서버 계약 대기",
    input: "select",
    note: "internal/production 등 배포 채널 축"
  }
];

export const releaseManagementActionCandidates: ReleaseManagementActionCandidate[] = [
  {
    code: "save_draft",
    label: "초안 저장",
    enabled: false,
    permissionStatus: "서버 계약 대기",
    requiresTargetConfirmation: true,
    requiresReason: true,
    note: "targetId/platform/channel/revision 확인 후 draft save"
  },
  {
    code: "apply_internal_testing",
    label: "내부 테스트 적용",
    enabled: false,
    permissionStatus: "서버 계약 대기",
    requiresTargetConfirmation: true,
    requiresReason: true,
    note: "targetId internal channel 전용 적용"
  },
  {
    code: "approve_candidate",
    label: "QA 승인",
    enabled: false,
    permissionStatus: "서버 계약 대기",
    requiresTargetConfirmation: true,
    requiresReason: true,
    note: "targetId 후보 artifact hash 승인"
  },
  {
    code: "prestage_production",
    label: "프로덕션 사전 준비",
    enabled: false,
    permissionStatus: "서버 계약 대기",
    requiresTargetConfirmation: true,
    requiresReason: true,
    note: "source targetId와 destination targetId를 분리 검증"
  },
  {
    code: "promote_pointer",
    label: "활성 포인터 승급",
    enabled: false,
    permissionStatus: "서버 계약 대기",
    requiresTargetConfirmation: true,
    requiresReason: true,
    note: "targetId current pointer revision/ETag 조건부 갱신"
  },
  {
    code: "open_release",
    label: "개시",
    enabled: false,
    permissionStatus: "서버 계약 대기",
    requiresTargetConfirmation: true,
    requiresReason: true,
    note: "targetId별 promote와 open/go-live를 분리"
  },
  {
    code: "rollback_release",
    label: "롤백",
    enabled: false,
    permissionStatus: "서버 계약 대기",
    requiresTargetConfirmation: true,
    requiresReason: true,
    note: "targetId compatible previous release만 rollback 후보"
  }
];

export const releaseManagementValidationChecks: ReleaseManagementValidationCheck[] = [
  {
    code: "integer_version_compare",
    label: "정수 버전 비교",
    status: "서버 계약 대기",
    note: "문자열 사전순 비교 금지"
  },
  {
    code: "compatibility_range",
    label: "호환 범위",
    status: "서버 계약 대기",
    note: "minimum/recommended/protocol/content compatibility"
  },
  {
    code: "catalog_hash_exists",
    label: "catalog/hash 존재",
    status: "서버 계약 대기",
    note: "대상 콘텐츠 host에서 접근 가능한 approved hash"
  },
  {
    code: "allowed_target_paths",
    label: "대상 경로 허용",
    status: "서버 계약 대기",
    note: "target registry prefix 밖 R2 key/host 거절"
  },
  {
    code: "store_update_availability",
    label: "스토어 업데이트 URL",
    status: "서버 계약 대기",
    note: "플랫폼별 허용 업데이트 경로"
  },
  {
    code: "approved_content_hash",
    label: "승인된 콘텐츠 hash",
    status: "서버 계약 대기",
    note: "QA 승인 후보와 실제 artifact hash 일치"
  },
  {
    code: "public_json_redaction",
    label: "public JSON 분리",
    status: "서버 계약 대기",
    note: "내부 승인/감사/진단 데이터 미포함"
  },
  {
    code: "etag_revision_match",
    label: "ETag/revision 조건부 갱신",
    status: "서버 계약 대기",
    note: "동시 편집과 duplicate request 차단"
  },
  {
    code: "target_permission_match",
    label: "대상 권한 검사",
    status: "서버 계약 대기",
    note: "targetId별 read/edit/promote/open/rollback 권한 분리"
  },
  {
    code: "dev_to_prd_artifact_revalidation",
    label: "DEV to PRD 재검증",
    status: "서버 계약 대기",
    note: "목적 대상 catalog URL/번들 접근성 기준으로 재검증"
  }
];

export const releaseManagementSafetyChecks = [
  "다른 target 위조 거절",
  "same releaseId target별 분리",
  "target별 approval 재사용 차단",
  "target별 idempotency key 충돌 차단",
  "target 전환 중 늦은 응답 무시",
  "target 전환 후 이전 대상 submit 차단",
  "잘못된 catalog host 거절",
  "프로덕션 target registry 미설정 fail-closed"
] as const;

export function buildReleaseManagementSnapshot(): ReleaseManagementSnapshot {
  return {
    serverContractStatus: "서버 계약 대기",
    spreadsheetIntegrationEnabled: false,
    targetAxes: releaseManagementTargetAxes,
    readStates: releaseManagementReadStates,
    targetRegistry: releaseManagementTargetRegistryPolicy,
    candidateFields: releaseManagementCandidateFields,
    actions: releaseManagementActionCandidates,
    validationChecks: releaseManagementValidationChecks,
    safetyChecks: releaseManagementSafetyChecks,
    publicJsonBoundary: {
      internalApprovalsIncluded: false,
      diagnosticsIncluded: false,
      auditHistoryIncluded: false
    }
  };
}

export type SimpleVersionManagementCard = {
  code: "currentServer" | "currentVersion";
  label: string;
  value: string;
  note: string;
};

export type SimpleVersionInputField = {
  key: "targetVersion";
  label: string;
  value: string;
  placeholder: string;
};

export type SimpleVersionExportAction = {
  code: "export_version_to_r2";
  label: string;
  enabled: false;
  status: "R2 내보내기 API 대기";
  note: string;
};

export type SimpleVersionManagementSnapshot = {
  serverCards: readonly SimpleVersionManagementCard[];
  inputFields: readonly SimpleVersionInputField[];
  exportAction: SimpleVersionExportAction;
};

export const simpleVersionServerCards: SimpleVersionManagementCard[] = [
  {
    code: "currentServer",
    label: "현재 서버",
    value: "DEV Admin API",
    note: "preview 환경은 dev-admin.lastrail.app을 바라봅니다."
  },
  {
    code: "currentVersion",
    label: "현재 버전",
    value: "조회 API 대기",
    note: "현재 버전을 읽는 서버 endpoint가 열리면 실제 값으로 교체합니다."
  }
];

export const simpleVersionInputFields: SimpleVersionInputField[] = [
  {
    key: "targetVersion",
    label: "변경할 버전",
    value: "",
    placeholder: "예: act1.2026-09-05.001"
  }
];

export const simpleVersionExportAction: SimpleVersionExportAction = {
  code: "export_version_to_r2",
  label: "내보내기",
  enabled: false,
  status: "R2 내보내기 API 대기",
  note: "입력한 버전을 서버 내보내기 API가 검증한 뒤 R2 current version JSON에 갱신합니다."
};

export function buildSimpleVersionManagementSnapshot(): SimpleVersionManagementSnapshot {
  return {
    serverCards: simpleVersionServerCards,
    inputFields: simpleVersionInputFields,
    exportAction: simpleVersionExportAction
  };
}
