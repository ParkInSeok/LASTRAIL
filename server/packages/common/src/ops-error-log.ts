import { canonicalJson, sha256Hex } from "./canonical-json";

export type OpsErrorLogEnvironment = "DEV" | "PRD";
export type OpsErrorLogService =
  | "admin_api"
  | "game_api"
  | "worker_cron"
  | "migration_verify"
  | "support_read_model"
  | "integrity_pipeline";
export type OpsErrorLogSeverity = "info" | "warning" | "error" | "critical";
export type OpsErrorLogStatus = "open" | "acknowledged" | "resolved" | "ignored";

type JsonValue = null | boolean | number | string | JsonValue[] | { readonly [key: string]: JsonValue };

export type OpsErrorLogInsertInput = {
  id: string;
  occurredAtMs: number;
  environment: OpsErrorLogEnvironment;
  service: OpsErrorLogService;
  severity: OpsErrorLogSeverity;
  errorCode: string;
  messageSummary: string;
  status?: OpsErrorLogStatus;
  source: OpsErrorLogService;
  requestId?: string | null;
  accountId?: string | null;
  runId?: string | null;
  rewardPreviewId?: string | null;
  rewardClaimId?: string | null;
  offlineRewardClaimId?: string | null;
  gachaBatchId?: string | null;
  gachaBannerId?: string | null;
  storyProgressId?: string | null;
  integrityEventId?: string | null;
  supportTicketId?: string | null;
  idempotencyOperation?: string | null;
  idempotencyKey?: string | null;
  correlation?: JsonValue;
  context?: JsonValue;
  retryable?: boolean;
  payloadRef?: string | null;
  traceRef?: string | null;
  createdAtMs?: number;
};

export type OpsErrorLogInsertEnvelope = {
  id: string;
  occurredAt: number;
  environment: OpsErrorLogEnvironment;
  service: OpsErrorLogService;
  severity: OpsErrorLogSeverity;
  errorCode: string;
  messageSummary: string;
  status: OpsErrorLogStatus;
  source: OpsErrorLogService;
  requestId: string | null;
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
  idempotencyOperation: string | null;
  idempotencyKeyHash: string | null;
  correlationJson: string;
  sanitizedContextJson: string;
  retryable: boolean;
  masked: true;
  payloadRef: string | null;
  traceRef: string | null;
  createdAt: number;
};

const FORBIDDEN_CONTEXT_KEYS = new Set([
  "authorization",
  "accesstoken",
  "refreshtoken",
  "cf-access-jwt-assertion",
  "serviceclientsecret",
  "rawbody",
  "rawrequestbody",
  "rawrng",
  "rngseed",
  "seedpreimage",
  "probability",
  "fullweight",
  "fullweights",
  "localizeddialogue",
  "storyprogressmultiplierbps",
  "rewardcalculation",
  "x-account-id",
  "x-lastrail-account-id",
]);

function normalizedKey(key: string): string {
  return key.replaceAll("_", "").replaceAll("-", "").toLowerCase();
}

function scrubJson(value: JsonValue): JsonValue | undefined {
  if (Array.isArray(value)) {
    return value.map((item) => scrubJson(item)).filter((item): item is JsonValue => item !== undefined);
  }
  if (value !== null && typeof value === "object") {
    const output: Record<string, JsonValue> = {};
    for (const [key, item] of Object.entries(value)) {
      if (FORBIDDEN_CONTEXT_KEYS.has(normalizedKey(key))) continue;
      const scrubbed = scrubJson(item);
      if (scrubbed !== undefined) output[key] = scrubbed;
    }
    return output;
  }
  return value;
}

export async function buildOpsErrorLogInsertEnvelope(input: OpsErrorLogInsertInput): Promise<OpsErrorLogInsertEnvelope> {
  return {
    id: input.id,
    occurredAt: input.occurredAtMs,
    environment: input.environment,
    service: input.service,
    severity: input.severity,
    errorCode: input.errorCode,
    messageSummary: input.messageSummary,
    status: input.status ?? "open",
    source: input.source,
    requestId: input.requestId ?? null,
    accountId: input.accountId ?? null,
    runId: input.runId ?? null,
    rewardPreviewId: input.rewardPreviewId ?? null,
    rewardClaimId: input.rewardClaimId ?? null,
    offlineRewardClaimId: input.offlineRewardClaimId ?? null,
    gachaBatchId: input.gachaBatchId ?? null,
    gachaBannerId: input.gachaBannerId ?? null,
    storyProgressId: input.storyProgressId ?? null,
    integrityEventId: input.integrityEventId ?? null,
    supportTicketId: input.supportTicketId ?? null,
    idempotencyOperation: input.idempotencyOperation ?? null,
    idempotencyKeyHash: input.idempotencyKey === undefined || input.idempotencyKey === null
      ? null
      : `sha256:${await sha256Hex(input.idempotencyKey)}`,
    correlationJson: canonicalJson(scrubJson(input.correlation ?? {}) ?? {}),
    sanitizedContextJson: canonicalJson(scrubJson(input.context ?? {}) ?? {}),
    retryable: input.retryable ?? false,
    masked: true,
    payloadRef: input.payloadRef ?? null,
    traceRef: input.traceRef ?? null,
    createdAt: input.createdAtMs ?? input.occurredAtMs,
  };
}
