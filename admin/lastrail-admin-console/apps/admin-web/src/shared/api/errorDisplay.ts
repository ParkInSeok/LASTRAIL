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
