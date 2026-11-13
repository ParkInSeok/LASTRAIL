import { accountAuthVisibilityFields, accountAuthSessionFieldLabel } from "../support/supportContract";
import type { SupportAuthSessionItem, SupportOpsErrorEvidence } from "../support/supportContract";
import { adminReadValueLabel } from "../api/errorDisplay";

export type PlayerLaunchEvidence = {
  authSession: {
    items: SupportAuthSessionItem[];
  };
  opsErrors: SupportOpsErrorEvidence[];
  supportContext: {
    readOnly: true;
    mutationEndpointsEnabled: false;
    source: string | null;
  };
};

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value) ? (value as Record<string, unknown>) : null;
}

function stringOrNull(value: unknown): string | null {
  return typeof value === "string" && value.length > 0 ? value : null;
}

function readValue(value: unknown, emptyLabel = "없음"): string {
  return adminReadValueLabel({ kind: "success", value, emptyLabel });
}

function providerList(value: unknown): string {
  if (!Array.isArray(value)) return "없음";
  const providers = value.filter((item): item is string => typeof item === "string" && item.length > 0);
  return providers.length > 0 ? providers.join(" / ") : "없음";
}

function authValue(field: string, auth: Record<string, unknown>): string {
  if (field === "identityProviders" || field === "providerNames" || field === "linkedProviders") {
    return providerList(auth[field]);
  }
  if (field === "hasGoogleLink") {
    if (auth[field] === true) return "연동됨";
    if (auth[field] === false) return "미연동";
    return "없음";
  }
  if (field === "lastLoginAgeSeconds") {
    return typeof auth[field] === "number" ? `${auth[field]}초` : "없음";
  }
  if (field === "concurrentUsers" && auth.concurrentUsersPrecision === "UNAVAILABLE") {
    return "정밀 동접자 백로그 (UNAVAILABLE)";
  }
  return readValue(auth[field]);
}

function authSessionItems(auth: Record<string, unknown> | null): SupportAuthSessionItem[] {
  return accountAuthVisibilityFields.map((field) => ({
    field,
    label: accountAuthSessionFieldLabel(field),
    value: auth ? authValue(field, auth) : "없음",
    note:
      field === "concurrentUsers"
        ? "UNAVAILABLE/null은 확정 동접자 수 아님"
        : `${field} read-only auth/session evidence`
  }));
}

function opsErrorEvidence(value: unknown): SupportOpsErrorEvidence | null {
  const record = asRecord(value);
  if (!record) return null;
  return {
    errorLogId: readValue(record.errorLogId),
    occurredAt: readValue(record.occurredAt),
    environment: readValue(record.environment),
    service: readValue(record.service),
    severity: readValue(record.severity),
    errorCode: readValue(record.errorCode),
    messageSummary: readValue(record.messageSummary),
    status: readValue(record.status),
    source: readValue(record.source),
    requestId: readValue(record.requestId),
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
    links: []
  };
}

export function mapPlayerLaunchEvidence(response: unknown): PlayerLaunchEvidence {
  const record = asRecord(response) ?? {};
  const supportContext = asRecord(record.supportContext);
  return {
    authSession: {
      items: authSessionItems(asRecord(record.auth))
    },
    opsErrors: Array.isArray(record.opsErrors)
      ? record.opsErrors.map(opsErrorEvidence).filter((item): item is SupportOpsErrorEvidence => Boolean(item))
      : [],
    supportContext: {
      readOnly: true,
      mutationEndpointsEnabled: false,
      source: stringOrNull(supportContext?.source)
    }
  };
}
