import { AdminApiError } from "./client";

export type AdminRequestFailureDisplay = {
  title: string;
  resourceLabel: string;
  resourceId: string;
  reason: string;
  statusLabel: string;
  errorCode: string;
  message: string;
  nextStep: string;
};

export type AdminReadStatusState = {
  hasData: boolean;
  isError: boolean;
  isLoading: boolean;
};

export type AdminReadValueState =
  | { kind: "integration_pending"; pendingLabel?: string }
  | { kind: "loading" }
  | { kind: "success"; value: unknown; emptyLabel?: string }
  | { kind: "error"; error: unknown; resourceLabel: string; resourceId: string };

type FailureContext = {
  resourceLabel: string;
  resourceId: string;
};

function stringOrFallback(value: string | undefined, fallback: string): string {
  return value && value.length > 0 ? value : fallback;
}

function reasonFor(error: unknown): Pick<AdminRequestFailureDisplay, "reason" | "nextStep"> {
  if (error instanceof AdminApiError) {
    if (error.errorCode === "ADMIN_ACCOUNT_RESPONSE_MISMATCH") {
      return { reason: "요청 계정과 응답 계정 확인 필요", nextStep: "다른 계정의 데이터는 표시하지 않습니다. 계정 목록과 API 응답을 확인하세요." };
    }
    if (error.path.startsWith("/api/client-releases") && error.status === 404) {
      return { reason: "버전 등록 API 배포 확인 필요", nextStep: "현재 대상 Admin API의 registry 지원 여부를 확인하세요. 다른 서버로 자동 전환하지 않습니다." };
    }
    if (error.errorCode === "registry_target_mismatch") {
      return { reason: "선택 대상과 응답 서버 불일치", nextStep: "DEV/PRD와 연결된 Admin API 환경을 확인한 뒤 다시 조회하세요." };
    }
    if (error.errorCode === "version_registry_managed") {
      return { reason: "버전코드별 등록 정책 사용 중 · 기존 단일 저장 불가", nextStep: "등록 정책을 다시 조회하고 버전코드별 변경을 사용하세요." };
    }
    if (error.errorCode === "version_precondition_required" || error.errorCode === "invalid_version_revision") {
      return { reason: "버전 저장 기준 확인 필요 · 최신 정책을 다시 조회하세요.", nextStep: "Admin UI/API 버전 정렬과 최신 정책 조회를 확인하세요. 자동 재저장하지 않습니다." };
    }
    if (error.status === 409 && error.errorCode === "version_current_pointer_conflict") {
      return {
        reason: "저장 중 다른 변경과 충돌했습니다. 새로고침으로 최신 정책을 조회한 뒤 입력값을 다시 검토하세요.",
        nextStep: "자동 재저장하지 않습니다. 최신 버전·최소 지원 버전·콘텐츠 정책을 확인한 후 다시 입력하세요."
      };
    }
    if (error.errorCode === "version_storage_unavailable") {
      return { reason: "R2 활성화 및 버킷 연결 필요", nextStep: "DEV R2 저장소 연결 후 다시 조회하세요." };
    }
    if (error.errorCode === "invalid_version") {
      return { reason: "버전·버전코드·최소 지원 버전 확인 필요", nextStep: "버전코드는 1~2147483647, 최소 지원 버전코드는 배포할 버전코드 이하로 입력하세요." };
    }
    if (error.errorCode === "ADMIN_API_NON_JSON_RESPONSE") {
      return {
        reason: "Access redirect 또는 프록시 응답 확인 필요",
        nextStep: "Cloudflare Access 로그인 상태와 Pages proxy가 JSON Admin API 응답을 받고 있는지 확인하세요."
      };
    }
    if (error.status === 401 || error.status === 403) {
      return {
        reason: "Access/admin credential 확인 필요",
        nextStep: "Cloudflare Access 로그인과 Admin 권한을 확인한 뒤 다시 시도하세요."
      };
    }
    if (error.status === 404) {
      return {
        reason: "DEV fixture/ID 없음",
        nextStep: "목록/검색에서 실제 DEV fixture ID를 다시 선택하세요."
      };
    }
    if (error.status === 500 && error.errorCode === "ADMIN_API_BASE_URL_REQUIRED") {
      return {
        reason: "Admin API 환경 설정 확인 필요",
        nextStep: "Pages preview의 Admin API base URL 설정을 확인하세요."
      };
    }
    return {
      reason: "Admin API 응답 실패",
      nextStep: "상태 코드와 errorCode를 기준으로 서버/환경 로그를 확인하세요."
    };
  }

  if (error instanceof Error && (error.name === "ZodError" || error.message.includes("ZodError"))) {
    return {
      reason: "응답 형식 확인 필요",
      nextStep: "Admin Web contract와 서버 응답 shape가 같은지 확인하세요."
    };
  }

  return {
    reason: "요청 실패 원인 확인 필요",
    nextStep: "네트워크, Access, Admin API 상태를 순서대로 확인하세요."
  };
}

export function adminRequestFailureDisplay(error: unknown, context: FailureContext): AdminRequestFailureDisplay {
  const classified = reasonFor(error);
  const apiError = error instanceof AdminApiError ? error : null;

  return {
    title: `${context.resourceLabel} 요청 실패`,
    resourceLabel: context.resourceLabel,
    resourceId: stringOrFallback(context.resourceId, "식별자 없음"),
    reason: classified.reason,
    statusLabel: apiError ? String(apiError.status) : "확인 필요",
    errorCode: stringOrFallback(apiError?.errorCode, "없음"),
    message: stringOrFallback(apiError?.upstreamMessage, "민감정보 없이 상태만 표시합니다."),
    nextStep: classified.nextStep
  };
}

export function adminReadStatusLabel(state: AdminReadStatusState): string {
  if (state.hasData) return "조회 가능";
  if (state.isError) return "Access/admin credential 확인 필요";
  if (state.isLoading) return "조회 중";
  return "연동 대기";
}

export function adminReadValueLabel(state: AdminReadValueState): string {
  if (state.kind === "integration_pending") return state.pendingLabel ?? "연동 대기";
  if (state.kind === "loading") return "조회 중";
  if (state.kind === "error") {
    return adminRequestFailureDisplay(state.error, {
      resourceLabel: state.resourceLabel,
      resourceId: state.resourceId
    }).reason;
  }

  const emptyLabel = state.emptyLabel ?? "없음";
  if (state.value === null || state.value === undefined) return emptyLabel;
  if (typeof state.value === "string") return state.value.length > 0 ? state.value : emptyLabel;
  if (typeof state.value === "number") return Number.isFinite(state.value) ? String(state.value) : emptyLabel;
  if (typeof state.value === "boolean") return state.value ? "true" : "false";
  if (Array.isArray(state.value)) {
    const values = state.value.filter((item): item is string | number => {
      if (typeof item === "string") return item.length > 0;
      return typeof item === "number" && Number.isFinite(item);
    });
    return values.length > 0 ? values.join(" / ") : emptyLabel;
  }

  return emptyLabel;
}
