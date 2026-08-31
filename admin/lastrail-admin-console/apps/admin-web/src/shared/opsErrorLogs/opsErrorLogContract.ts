import { adminReadValueLabel } from "../api/errorDisplay";

export type OpsErrorLogReadFiltersPreview = {
  environment: string;
  severity: string;
  status: string;
  source: string;
  service: string;
  errorCode: string;
  requestId: string;
  accountId: string;
  runId: string;
  rewardPreviewId: string;
  rewardClaimId: string;
  offlineRewardClaimId: string;
  gachaBatchId: string;
  gachaBannerId: string;
  storyProgressId: string;
  integrityEventId: string;
  supportTicketId: string;
  limit: string;
  cursor: string;
};

export type OpsErrorLogListItem = {
  id: string;
  occurredAt: string;
  environment: string;
  severity: string;
  status: string;
  source: string;
  service: string;
  errorCode: string;
  messageSummary: string;
  requestId: string;
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
  idempotencyKeyHash: string | null;
  payloadRef: string | null;
  traceRef: string | null;
  retryable: boolean | "연동 대기" | "없음";
  masked: boolean | "연동 대기" | "없음";
};

export type OpsErrorLogListResponse = {
  permission: "OPS_ERROR_LOG_READ" | string;
  permissionMode: "ROLE_PERMISSION" | string;
  items: OpsErrorLogListItem[];
  nextCursor: string | null;
};

export type OpsErrorLogDetailResponse = {
  permission: "OPS_ERROR_LOG_READ" | string;
  permissionMode: "ROLE_PERMISSION" | string;
  item: OpsErrorLogListItem;
  detail: unknown | null;
};

export const opsErrorLogReadFilterFields = [
  "environment",
  "severity",
  "status",
  "source",
  "service",
  "errorCode",
  "requestId",
  "accountId",
  "runId",
  "rewardPreviewId",
  "rewardClaimId",
  "offlineRewardClaimId",
  "gachaBatchId",
  "gachaBannerId",
  "storyProgressId",
  "integrityEventId",
  "supportTicketId",
  "limit",
  "cursor"
] as const;

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value) ? (value as Record<string, unknown>) : null;
}

function stringValue(value: unknown, fallback: string): string {
  return typeof value === "string" && value.length > 0 ? value : fallback;
}

function readStringValue(value: unknown, fallback: string): string {
  return adminReadValueLabel({ kind: "success", value, emptyLabel: fallback });
}

function nullableStringValue(value: unknown): string | null {
  return typeof value === "string" && value.length > 0 ? value : null;
}

function booleanOrReadState(value: unknown, fallback: "연동 대기" | "없음"): boolean | "연동 대기" | "없음" {
  return typeof value === "boolean" ? value : fallback;
}

function pick(record: Record<string, unknown> | null, camel: string, snake?: string): unknown {
  if (!record) return undefined;
  return record[camel] ?? (snake ? record[snake] : undefined);
}

function correlationString(record: Record<string, unknown> | null, key: string): string | null {
  const direct = nullableStringValue(record?.[key]);
  if (direct) return direct;

  const correlation = asRecord(record?.correlationJson ?? record?.correlation_json);
  return nullableStringValue(correlation?.[key]);
}

function mapOpsErrorLogItem(value: unknown, fallbackId = "연동 대기"): OpsErrorLogListItem {
  const record = asRecord(value);
  const fieldFallback = record ? "없음" : "연동 대기";

  return {
    id: stringValue(record?.id, fallbackId),
    occurredAt: readStringValue(pick(record, "occurredAt", "occurred_at"), fieldFallback),
    environment: readStringValue(record?.environment, fieldFallback),
    severity: readStringValue(record?.severity, fieldFallback),
    status: readStringValue(record?.status, fieldFallback),
    source: readStringValue(record?.source, fieldFallback),
    service: readStringValue(record?.service, fieldFallback),
    errorCode: readStringValue(pick(record, "errorCode", "error_code"), fieldFallback),
    messageSummary: readStringValue(pick(record, "messageSummary", "message_summary"), fieldFallback),
    requestId: readStringValue(pick(record, "requestId", "request_id"), fieldFallback),
    accountId: correlationString(record, "accountId"),
    runId: correlationString(record, "runId"),
    rewardPreviewId: correlationString(record, "rewardPreviewId"),
    rewardClaimId: correlationString(record, "rewardClaimId"),
    offlineRewardClaimId: correlationString(record, "offlineRewardClaimId"),
    gachaBatchId: correlationString(record, "gachaBatchId"),
    gachaBannerId: correlationString(record, "gachaBannerId"),
    storyProgressId: correlationString(record, "storyProgressId"),
    integrityEventId: correlationString(record, "integrityEventId"),
    supportTicketId: correlationString(record, "supportTicketId"),
    idempotencyKeyHash: nullableStringValue(pick(record, "idempotencyKeyHash", "idempotency_key_hash")),
    payloadRef: nullableStringValue(pick(record, "payloadRef", "payload_ref")),
    traceRef: nullableStringValue(pick(record, "traceRef", "trace_ref")),
    retryable: booleanOrReadState(record?.retryable, fieldFallback),
    masked: booleanOrReadState(record?.masked, fieldFallback)
  };
}

export function emptyOpsErrorLogListItem(id = "연동 대기"): OpsErrorLogListItem {
  return mapOpsErrorLogItem(null, id);
}

export function noDataOpsErrorLogListItem(id = "데이터 없음"): OpsErrorLogListItem {
  return mapOpsErrorLogItem({ id });
}

export function emptyOpsErrorLogDetail(errorLogId = "연동 대기"): OpsErrorLogDetailResponse {
  return {
    permission: "OPS_ERROR_LOG_READ",
    permissionMode: "ROLE_PERMISSION",
    item: emptyOpsErrorLogListItem(errorLogId),
    detail: null
  };
}

export function mapOpsErrorLogListResponse(response: unknown): OpsErrorLogListResponse {
  const record = asRecord(response);
  return {
    permission: stringValue(record?.permission, "OPS_ERROR_LOG_READ"),
    permissionMode: stringValue(record?.permissionMode, "ROLE_PERMISSION"),
    items: Array.isArray(record?.items) ? record.items.map((item) => mapOpsErrorLogItem(item)) : [],
    nextCursor: typeof record?.nextCursor === "string" ? record.nextCursor : null
  };
}

export function mapOpsErrorLogDetailResponse(response: unknown, fallbackId = "연동 대기"): OpsErrorLogDetailResponse {
  const record = asRecord(response);
  if (!record) return emptyOpsErrorLogDetail(fallbackId);

  return {
    permission: stringValue(record.permission, "OPS_ERROR_LOG_READ"),
    permissionMode: stringValue(record.permissionMode, "ROLE_PERMISSION"),
    item: mapOpsErrorLogItem(record.item ?? response, fallbackId),
    detail: record.detail ?? null
  };
}

export const opsErrorLogReadContractPreview = {
  listItem: emptyOpsErrorLogListItem(),
  detail: emptyOpsErrorLogDetail(),
  filters: Object.fromEntries(opsErrorLogReadFilterFields.map((field) => [field, ""])) as OpsErrorLogReadFiltersPreview
};
