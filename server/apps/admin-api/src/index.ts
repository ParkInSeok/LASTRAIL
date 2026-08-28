import { Hono, type Context } from "hono";
import { canonicalJson, createRequestId, sha256Hex } from "@lastrail/common";

const app = new Hono<{ Bindings: Env }>();

type JsonValue = null | boolean | number | string | JsonValue[] | { readonly [key: string]: JsonValue };

type AdminPrincipalRow = {
  id: string;
  access_subject: string;
  role: "OWNER" | "OPERATOR" | "VIEWER";
  status: "ACTIVE" | "DISABLED";
  display_name: string | null;
};

type BalanceDraftRow = {
  id: string;
  base_version: number | null;
  schema_version: number;
  required_content_version: number;
  config_json: string;
  checksum: string;
  revision: number;
  status: "DRAFT" | "PUBLISHED" | "DISCARDED";
};

type ActiveBalanceRow = {
  version: number;
};

type AdminPlayerSearchRow = {
  account_id: string;
  display_name: string | null;
  status: "ACTIVE" | "SUSPENDED" | "BANNED" | "DELETED";
  updated_at: number;
  sync_level: number | null;
};

type AdminPlayerDetailRow = {
  account_id: string;
  display_name: string | null;
  status: "ACTIVE" | "SUSPENDED" | "BANNED" | "DELETED";
  created_at: number;
  operator_exp: number | null;
};

type AdminTrainingCoreRow = {
  slot_index: number;
  operator_id: string;
  trained_level: number;
};

type AdminOperatorRow = {
  operator_id: string;
  skill_training: number;
  promotion: number;
};

type AdminPlayerNoteRow = {
  id: string;
  principal_id: string;
  display_name: string | null;
  resource_id: string;
  request_id: string;
  change_summary_json: string;
  created_at: number;
};

type AdminAuditLogRow = {
  id: string;
  principal_id: string | null;
  display_name: string | null;
  role: "OWNER" | "OPERATOR" | "VIEWER";
  permission: string;
  action: string;
  resource_type: string;
  resource_id: string | null;
  request_id: string;
  confirmation_id: string | null;
  payload_hash: string | null;
  change_summary_json: string;
  result: "SUCCESS" | "FAILURE";
  error_code: string | null;
  created_at: number;
};

type AdminIntegrityEventRow = {
  id: string;
  account_id: string;
  run_id: string | null;
  classification: "SOFT_ANOMALY" | "HARD_VIOLATION";
  category: string;
  evidence_code: string;
  evidence_json: string;
  review_state: "OPEN" | "IN_REVIEW" | "RESOLVED";
  assigned_to: string | null;
  created_at: number;
};

type AdminSuspiciousPlayerRow = {
  account_id: string;
  display_name: string | null;
  open_signals: number;
  hard_violations: number;
  suspicious_runs: number;
  last_detection_at: number;
  primary_reason: string;
  review_status: string;
  current_enforcement: string | null;
  review_owner: string | null;
};

type AdminSuspiciousPlayerAccountRow = {
  account_id: string;
  display_name: string | null;
  status: "ACTIVE" | "SUSPENDED" | "BANNED" | "DELETED";
  created_at: number;
};

type AdminSuspiciousPlayerRunRow = {
  run_id: string;
  status: string;
  risk: string | null;
  balance_config_version: number;
  started_at: number;
  finished_at: number | null;
};

type AdminSuspiciousPlayerDecisionRow = {
  id: string;
  display_name: string | null;
  from_status: string | null;
  to_status: string;
  reason: string | null;
  created_at: number;
};

type AdminSuspiciousPlayerReviewRow = {
  status: string;
  assigned_to: string | null;
  reason: string | null;
  updated_at: number;
};

type AdminRunSearchRow = {
  run_id: string;
  account_id: string;
  status: string;
  started_at: number;
  finished_at: number | null;
  risk: string | null;
  balance_config_version: number;
  validator_version: string | null;
};

type AdminRunDetailRow = AdminRunSearchRow & {
  current_active_balance_version: number | null;
  build_id: string | null;
  content_version: number | null;
  validator_runtime_revision: number | null;
  snapshot_json: string;
};

type AdminStoryProgressRow = {
  account_id: string;
  act_id: string;
  highest_cleared_operation_id: string;
  highest_story_progress_id: string;
  cleared_operations_json: string;
  story_flags_json: string;
  narrative_version: string | null;
  last_progress_run_id: string | null;
  updated_at: number;
};

type AdminStoryProgressUnlockRow = {
  unlock_id: string;
};

type AdminOfflineRewardRow = {
  account_id: string;
  display_name: string | null;
  account_status: "ACTIVE" | "SUSPENDED" | "BANNED" | "DELETED";
  account_created_at: number;
  highest_story_progress_id: string | null;
  narrative_version: string | null;
  last_offline_reward_claim_at: number | null;
  last_story_progress_id: string | null;
  last_balance_config_version: number | null;
  last_narrative_version: string | null;
  last_previewed_at: number | null;
  revision: number | null;
  state_updated_at: number | null;
  last_claim_id: string | null;
  last_claim_created_at: number | null;
};

type NextVersionRow = {
  next_version: number;
};

type AdminConfirmationRow = {
  id: string;
  payload_hash: string;
};

type AppContext = Context<{ Bindings: Env }>;

const BALANCE_PUBLISH_PERMISSION = "BALANCE_PUBLISH";
const ACCOUNT_NOTE_WRITE_PERMISSION = "ACCOUNT_NOTE_WRITE";
const STORY_PROGRESS_READ_PERMISSION = "STORY_PROGRESS_READ";
const OFFLINE_REWARD_READ_PERMISSION = "OFFLINE_REWARD_READ";
const CONFIRMATION_TTL_MS = 5 * 60 * 1000;
const ACT_ONE_NARRATIVE_VERSION = "act1.2026-08-27.001";
const ACT_ONE_STORY_PROGRESS_ORDER = [
  "none",
  "opn_01_last_departure",
  "opn_02_first_signal",
  "opn_03_water_stop",
  "opn_04_scrapstar_show",
  "opn_05_night_boarders",
  "opn_06_broken_line",
  "raid_raider_king_engine",
] as const;
const ACT_ONE_OPERATION_IDS = ACT_ONE_STORY_PROGRESS_ORDER.filter((id) => id !== "none");

type AccessJwtPayload = {
  iss?: unknown;
  aud?: unknown;
  sub?: unknown;
  email?: unknown;
  common_name?: unknown;
  exp?: unknown;
  nbf?: unknown;
};

type AccessIdentity = {
  subject: string;
  email: string | null;
};

type VerifiedAccessJwt =
  | {
      kind: "user";
      subject: string;
      email: string | null;
    }
  | {
      kind: "service";
      commonName: string;
    };

type AccessJwk = JsonWebKey & {
  kid?: string;
  alg?: string;
  use?: string;
};

type AccessJwks = {
  keys?: AccessJwk[];
};

function decodeBase64Url(value: string): Uint8Array {
  const padded = value.replaceAll("-", "+").replaceAll("_", "/").padEnd(Math.ceil(value.length / 4) * 4, "=");
  const binary = atob(padded);
  return Uint8Array.from(binary, (char) => char.charCodeAt(0));
}

function decodeJson<T>(value: string): T {
  return JSON.parse(new TextDecoder().decode(decodeBase64Url(value))) as T;
}

function hasAudience(payloadAudience: unknown, expectedAudience: string): boolean {
  if (typeof payloadAudience === "string") return payloadAudience === expectedAudience;
  if (Array.isArray(payloadAudience)) return payloadAudience.includes(expectedAudience);
  return false;
}

async function verifyAccessJwt(
  token: string,
  issuer: string,
  audience: string,
  options: { allowServiceToken?: boolean; nowSeconds?: number } = {},
): Promise<VerifiedAccessJwt | null> {
  const nowSeconds = options.nowSeconds ?? Math.floor(Date.now() / 1000);
  const [encodedHeader, encodedPayload, encodedSignature] = token.split(".");
  if (encodedHeader === undefined || encodedPayload === undefined || encodedSignature === undefined) return null;

  const header = decodeJson<{ alg?: unknown; kid?: unknown }>(encodedHeader);
  if (header.alg !== "RS256" || typeof header.kid !== "string") return null;

  const jwksResponse = await fetch(`${issuer}/cdn-cgi/access/certs`);
  if (!jwksResponse.ok) return null;

  const jwks = (await jwksResponse.json()) as AccessJwks;
  const jwk = jwks.keys?.find((key) => key.kid === header.kid && key.kty === "RSA");
  if (jwk === undefined) return null;

  const publicKey = await crypto.subtle.importKey(
    "jwk",
    jwk,
    { name: "RSASSA-PKCS1-v1_5", hash: "SHA-256" },
    false,
    ["verify"],
  );
  const signingInput = `${encodedHeader}.${encodedPayload}`;
  const verified = await crypto.subtle.verify(
    "RSASSA-PKCS1-v1_5",
    publicKey,
    decodeBase64Url(encodedSignature),
    new TextEncoder().encode(signingInput),
  );
  if (!verified) return null;

  const payload = decodeJson<AccessJwtPayload>(encodedPayload);
  if (payload.iss !== issuer) return null;
  if (!hasAudience(payload.aud, audience)) return null;
  if (typeof payload.exp !== "number" || payload.exp <= nowSeconds) return null;
  if (typeof payload.nbf === "number" && payload.nbf > nowSeconds) return null;

  if (typeof payload.sub === "string" && payload.sub.length > 0) {
    return {
      kind: "user",
      subject: payload.sub,
      email: typeof payload.email === "string" && payload.email.length > 0 ? payload.email : null,
    };
  }

  if (options.allowServiceToken && typeof payload.common_name === "string" && payload.common_name.length > 0) {
    return {
      kind: "service",
      commonName: payload.common_name,
    };
  }

  return null;
}

async function extractDelegatedAccessIdentity(c: AppContext): Promise<AccessIdentity | Response> {
  if (c.env.ADMIN_DELEGATED_ACCESS_ISSUER.length === 0 || c.env.ADMIN_DELEGATED_ACCESS_AUD.length === 0) {
    return c.json({ error: "ADMIN_DELEGATED_ACCESS_JWT_VERIFICATION_NOT_CONFIGURED" }, 501);
  }

  const delegatedToken = c.req.header("X-Lastrail-User-Access-Jwt");
  if (!delegatedToken) {
    return c.json({ error: "ADMIN_DELEGATED_ACCESS_JWT_REQUIRED" }, 401);
  }

  const delegatedIdentity = await verifyAccessJwt(
    delegatedToken,
    c.env.ADMIN_DELEGATED_ACCESS_ISSUER,
    c.env.ADMIN_DELEGATED_ACCESS_AUD,
  );
  if (delegatedIdentity === null || delegatedIdentity.kind !== "user") {
    return c.json({ error: "ADMIN_DELEGATED_ACCESS_JWT_INVALID" }, 403);
  }

  return {
    subject: delegatedIdentity.subject,
    email: delegatedIdentity.email,
  };
}

async function extractAccessIdentity(c: AppContext): Promise<AccessIdentity | Response> {
  if (c.env.ADMIN_AUTH_MODE === "LOCAL_HEADER") {
    const accessSubject = c.req.header("Cf-Access-Subject") ?? null;
    if (!accessSubject) {
      return c.json({ error: "ADMIN_ACCESS_SUBJECT_REQUIRED" }, 401);
    }
    return { subject: accessSubject, email: null };
  } else if (c.env.ADMIN_ACCESS_ISSUER.length > 0 && c.env.ADMIN_ACCESS_AUD.length > 0) {
    const token = c.req.header("Cf-Access-Jwt-Assertion");
    if (!token) {
      return c.json({ error: "ADMIN_ACCESS_JWT_REQUIRED" }, 401);
    }

    const accessIdentity = await verifyAccessJwt(token, c.env.ADMIN_ACCESS_ISSUER, c.env.ADMIN_ACCESS_AUD, {
      allowServiceToken: true,
    });
    if (accessIdentity === null) {
      return c.json({ error: "ADMIN_ACCESS_JWT_INVALID" }, 403);
    }
    if (accessIdentity.kind === "service") {
      return extractDelegatedAccessIdentity(c);
    }
    return {
      subject: accessIdentity.subject,
      email: accessIdentity.email,
    };
  } else {
    return c.json({ error: "ADMIN_ACCESS_JWT_VERIFICATION_NOT_CONFIGURED" }, 501);
  }
}

async function authenticateAdmin(c: AppContext): Promise<AdminPrincipalRow | Response> {
  const accessIdentity = await extractAccessIdentity(c);
  if (accessIdentity instanceof Response) return accessIdentity;

  const principal = await c.env.DB.prepare(
    `SELECT id, access_subject, role, status, display_name
     FROM admin_principals
     WHERE access_subject = ?
       AND status = 'ACTIVE'`,
  )
    .bind(accessIdentity.subject)
    .first<AdminPrincipalRow>();

  if (principal === null) {
    return c.json({ error: "ADMIN_PRINCIPAL_NOT_AUTHORIZED" }, 403);
  }

  return principal;
}

function isJsonValue(value: unknown): value is JsonValue {
  if (value === null) return true;
  if (typeof value === "boolean" || typeof value === "string") return true;
  if (typeof value === "number") return Number.isFinite(value);
  if (Array.isArray(value)) return value.every((item) => isJsonValue(item));
  if (typeof value === "object") return Object.values(value).every((item) => isJsonValue(item));
  return false;
}

function isJsonObject(value: unknown): value is { readonly [key: string]: JsonValue } {
  return value !== null && typeof value === "object" && !Array.isArray(value) && isJsonValue(value);
}

function isPositiveInteger(value: unknown): value is number {
  return typeof value === "number" && Number.isSafeInteger(value) && value > 0;
}

function isNonNegativeInteger(value: unknown): value is number {
  return typeof value === "number" && Number.isSafeInteger(value) && value >= 0;
}

function d1Changes(result: unknown): number | null {
  if (result === null || typeof result !== "object") return null;
  const meta = "meta" in result ? result.meta : null;
  if (meta === null || typeof meta !== "object") return null;
  const changes = "changes" in meta ? meta.changes : null;
  return typeof changes === "number" ? changes : null;
}

function toIso(timestampMs: number): string {
  return new Date(timestampMs).toISOString();
}

function mapRunResult(status: string): "IN_PROGRESS" | "CLEARED" | "FAILED" | "INVALIDATED" {
  if (status === "ACTIVE" || status === "FINISH_PENDING") return "IN_PROGRESS";
  if (status === "COMPLETED") return "CLEARED";
  if (status === "QUARANTINED") return "INVALIDATED";
  return "FAILED";
}

function normalizeRiskTier(risk: string | null): "I" | "II" | "III" | "IV" | "V" {
  if (risk === "II" || risk === "III" || risk === "IV" || risk === "V") return risk;
  return "I";
}

type SuspiciousPlayerReviewStatus =
  | "NEW"
  | "REVIEWING"
  | "CLEARED"
  | "SCORE_EXCLUDED"
  | "REWARD_HELD"
  | "ESCALATED"
  | "ACTIONED"
  | "FALSE_POSITIVE";

function suspiciousPriority(
  openSignals: number,
  hardViolations: number,
  evidenceCodes: readonly string[] = [],
): "LOW" | "MEDIUM" | "HIGH" | "CRITICAL" {
  if (
    evidenceCodes.some((code) =>
      code === "CHECKSUM_MISMATCH" ||
      code === "IMPOSSIBLE_SCORE" ||
      code === "IMPOSSIBLE_DAMAGE" ||
      code === "BALANCE_VERSION_REWARD_MISMATCH"
    )
  ) {
    return "CRITICAL";
  }
  if (
    evidenceCodes.some((code) =>
      code === "ABNORMAL_CLEAR_TIME" ||
      code === "ABNORMAL_ULTIMATE_USAGE" ||
      code === "RAID_SCORE_REPEAT"
    )
  ) {
    return "HIGH";
  }
  if (hardViolations >= 3 || openSignals >= 10) return "CRITICAL";
  if (hardViolations > 0 || openSignals >= 3) return "HIGH";
  if (openSignals >= 2) return "MEDIUM";
  return "LOW";
}

function suspiciousReviewStatus(value: string): SuspiciousPlayerReviewStatus {
  if (value === "REVIEWING" || value === "IN_REVIEW") return "REVIEWING";
  if (value === "CLEARED") return "CLEARED";
  if (value === "SCORE_EXCLUDED") return "SCORE_EXCLUDED";
  if (value === "REWARD_HELD") return "REWARD_HELD";
  if (value === "ESCALATED") return "ESCALATED";
  if (value === "ACTIONED" || value === "RESOLVED") return "ACTIONED";
  if (value === "FALSE_POSITIVE") return "FALSE_POSITIVE";
  return "NEW";
}

function currentEnforcementStatus(type: string | null): "NONE" | "REWARD_HELD" | "SCORE_EXCLUDED" | "ACTIONED" {
  if (type === "REWARD_RESTRICTED") return "REWARD_HELD";
  if (type === "TEMP_SUSPENDED" || type === "PERMANENT_BAN") return "ACTIONED";
  return "NONE";
}

function currentMarkOnlyStatus(status: SuspiciousPlayerReviewStatus | null): "NONE" | "REWARD_HELD" | "SCORE_EXCLUDED" | "ACTIONED" {
  if (status === "REWARD_HELD") return "REWARD_HELD";
  if (status === "SCORE_EXCLUDED") return "SCORE_EXCLUDED";
  if (status === "ACTIONED") return "ACTIONED";
  return "NONE";
}

function suspiciousRunType(runId: string): "RUN" | "RAID_RUN" {
  return runId.includes("raid") ? "RAID_RUN" : "RUN";
}

function syncLevelFrom(coreRows: AdminTrainingCoreRow[]): number {
  if (coreRows.length !== 5) return 1;
  return Math.min(...coreRows.map((row) => row.trained_level));
}

function coreLevelSpreadFrom(coreRows: AdminTrainingCoreRow[]): number {
  if (coreRows.length !== 5) return 0;
  const levels = coreRows.map((row) => row.trained_level);
  return Math.max(...levels) - Math.min(...levels);
}

function parseRunSnapshot(snapshotJson: string): JsonValue {
  const parsed = JSON.parse(snapshotJson) as unknown;
  if (!isJsonValue(parsed)) throw new Error("RUN_SNAPSHOT_INVALID_JSON");
  return parsed;
}

function parseJsonValue(json: string): JsonValue {
  const parsed = JSON.parse(json) as unknown;
  if (!isJsonValue(parsed)) throw new Error("JSON_VALUE_INVALID");
  return parsed;
}

function parseJsonStringArray(value: string): string[] {
  try {
    const parsed = JSON.parse(value) as unknown;
    if (!Array.isArray(parsed)) return [];
    return parsed.filter((item): item is string => typeof item === "string");
  } catch {
    return [];
  }
}

function storyProgressOrder(storyProgressId: string): number {
  const index = ACT_ONE_STORY_PROGRESS_ORDER.indexOf(storyProgressId as typeof ACT_ONE_STORY_PROGRESS_ORDER[number]);
  return index < 0 ? 0 : index;
}

function sortActOneOperationIds(values: string[]): string[] {
  return [...new Set(values)]
    .filter((value) => ACT_ONE_OPERATION_IDS.includes(value as typeof ACT_ONE_OPERATION_IDS[number]))
    .sort((left, right) => storyProgressOrder(left) - storyProgressOrder(right));
}

function narrativeMetadata(narrativeVersion: string) {
  return {
    narrativeVersion,
    contentStatus: "CANDIDATE",
    localizationStatus: "PENDING",
    fixtureStatus: "PARTIAL",
    missingContentStatus: "UNKNOWN",
  };
}

function storyProgressSummary(row: AdminStoryProgressRow) {
  const narrativeVersion = row.narrative_version ?? ACT_ONE_NARRATIVE_VERSION;
  const storyFlags = parseJsonStringArray(row.story_flags_json);

  return {
    accountId: row.account_id,
    actId: row.act_id,
    operationId: row.highest_story_progress_id,
    storyProgress: {
      highestStoryProgressId: row.highest_story_progress_id,
      highestClearedOperationId: row.highest_cleared_operation_id,
      order: storyProgressOrder(row.highest_story_progress_id),
    },
    firstClear: null,
    clearedOperationIds: sortActOneOperationIds(parseJsonStringArray(row.cleared_operations_json)),
    narrativeFlags: storyFlags,
    storyFlags,
    narrativeVersion,
    narrative: narrativeMetadata(narrativeVersion),
    lastProgressRunId: row.last_progress_run_id,
    updatedAt: toIso(row.updated_at),
    supportContext: {
      source: "account_story_progress",
      readOnly: true,
      mutationEndpointsEnabled: false,
    },
  };
}

function defaultStoryProgressDetail(accountId: string) {
  return {
    accountId,
    actId: "act_01_wasteland_railway",
    operationId: null,
    storyProgress: {
      highestStoryProgressId: "none",
      highestClearedOperationId: null,
      order: 0,
    },
    firstClear: null,
    clearedOperationIds: [],
    narrativeFlags: [],
    storyFlags: [],
    unlockedOperations: ["opn_01_last_departure"],
    narrativeVersion: ACT_ONE_NARRATIVE_VERSION,
    narrative: narrativeMetadata(ACT_ONE_NARRATIVE_VERSION),
    lastProgressRunId: null,
    updatedAt: null,
    supportContext: {
      source: "default_none_progress",
      readOnly: true,
      mutationEndpointsEnabled: false,
    },
  };
}

function storyProgressDetail(row: AdminStoryProgressRow, unlockRows: AdminStoryProgressUnlockRow[]) {
  const unlockedOperations = sortActOneOperationIds([
    "opn_01_last_departure",
    ...unlockRows
      .map((unlockRow) => unlockRow.unlock_id.startsWith("operation:") ? unlockRow.unlock_id.slice("operation:".length) : null)
      .filter((value): value is string => value !== null),
  ]);

  return {
    ...storyProgressSummary(row),
    unlockedOperations,
  };
}

function offlineRewardSummary(row: AdminOfflineRewardRow) {
  const storyProgressId = row.highest_story_progress_id ?? row.last_story_progress_id ?? "none";
  const narrativeVersion = row.narrative_version ?? row.last_narrative_version ?? ACT_ONE_NARRATIVE_VERSION;
  const hasOfflineState = row.last_offline_reward_claim_at !== null;
  return {
    accountId: row.account_id,
    displayName: row.display_name,
    accountStatus: row.account_status,
    storyProgressId,
    narrativeVersion,
    offlineRewardState: {
      source: hasOfflineState ? "account_offline_reward_state" : "accounts.created_at_fallback",
      lastOfflineRewardClaimAt: toIso(row.last_offline_reward_claim_at ?? row.account_created_at),
      lastStoryProgressId: row.last_story_progress_id ?? "none",
      lastBalanceConfigVersion: row.last_balance_config_version,
      lastNarrativeVersion: row.last_narrative_version ?? ACT_ONE_NARRATIVE_VERSION,
      lastPreviewedAt: row.last_previewed_at === null ? null : toIso(row.last_previewed_at),
      revision: row.revision ?? 0,
      updatedAt: row.state_updated_at === null ? null : toIso(row.state_updated_at),
    },
    lastClaim: row.last_claim_id === null
      ? null
      : {
          offlineRewardClaimId: row.last_claim_id,
          claimedAt: row.last_claim_created_at === null ? null : toIso(row.last_claim_created_at),
        },
    supportContext: {
      readOnly: true,
      claimMutationEnabled: false,
      mutationEndpointsEnabled: false,
    },
  };
}

function hasRequestBody(c: AppContext): boolean {
  const contentLength = c.req.header("content-length");
  return contentLength !== undefined && contentLength !== "0";
}

function rejectAdminStoryProgressBody(c: AppContext): Response {
  return c.json({
    error: "contract_violation",
    message: "Admin story progress read endpoints do not accept request bodies.",
  }, 400);
}

function rejectAdminOfflineRewardBody(c: AppContext): Response {
  return c.json({
    error: "contract_violation",
    message: "Admin offline reward read endpoints do not accept request bodies.",
  }, 400);
}

function hasAdminPermission(principal: AdminPrincipalRow, permission: string): boolean {
  if (permission === OFFLINE_REWARD_READ_PERMISSION) {
    return principal.role === "OWNER" || principal.role === "OPERATOR" || principal.role === "VIEWER";
  }
  if (permission === STORY_PROGRESS_READ_PERMISSION) {
    return principal.role === "OWNER" || principal.role === "OPERATOR" || principal.role === "VIEWER";
  }
  if (permission === ACCOUNT_NOTE_WRITE_PERMISSION) {
    return principal.role === "OWNER" || principal.role === "OPERATOR";
  }
  if (permission === BALANCE_PUBLISH_PERMISSION) {
    return principal.role === "OWNER";
  }
  return false;
}

function adminPermissionDenied(): Response {
  return new Response(JSON.stringify({ error: "ADMIN_PERMISSION_DENIED" }), {
    status: 403,
    headers: { "Content-Type": "application/json" },
  });
}

function parsePlayerNoteSummary(summaryJson: string): string {
  const parsed = parseJsonValue(summaryJson);
  if (!isJsonObject(parsed) || typeof parsed.note !== "string" || parsed.note.length === 0) {
    throw new Error("PLAYER_NOTE_INVALID_JSON");
  }
  return parsed.note;
}

async function createPayloadHash(payload: JsonValue): Promise<string> {
  return `sha256:${await sha256Hex(canonicalJson(payload))}`;
}

async function createTokenHash(token: string): Promise<string> {
  return `sha256:${await sha256Hex(token)}`;
}

async function createBalancePublishPayloadHash(draftId: string, expectedRevision: number, expectedChecksum: string): Promise<string> {
  return createPayloadHash({
    permission: BALANCE_PUBLISH_PERMISSION,
    draftId,
    expectedRevision,
    expectedChecksum,
  });
}

async function writeBalancePublishFailureAudit(
  c: AppContext,
  principal: AdminPrincipalRow,
  resourceId: string,
  errorCode: string,
  payloadHash: string | null,
  changeSummary: JsonValue,
): Promise<void> {
  await c.env.DB.prepare(
    `INSERT INTO admin_audit_logs (
       id,
       principal_id,
       role,
       permission,
       action,
       resource_type,
       resource_id,
       request_id,
       confirmation_id,
       payload_hash,
       change_summary_json,
       account_mutation_id,
       result,
       error_code,
       created_at
     ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, NULL, ?, ?, NULL, ?, ?, ?)`,
  )
    .bind(
      `audit_${createRequestId().slice(4)}`,
      principal.id,
      principal.role,
      BALANCE_PUBLISH_PERMISSION,
      "BALANCE_CONFIG_PUBLISH",
      "balance_config_draft",
      resourceId,
      createRequestId(),
      payloadHash,
      canonicalJson(changeSummary),
      "FAILURE",
      errorCode,
      Date.now(),
    )
    .run();
}

app.get("/status", (c) => c.json({ ok: true, service: "admin-api", requestId: createRequestId() }));
app.get("/v1/admin/bootstrap/access-subject", async (c) => {
  if (c.env.ENVIRONMENT !== "DEV") {
    return c.json({ error: "NOT_FOUND" }, 404);
  }

  const accessIdentity = await extractAccessIdentity(c);
  if (accessIdentity instanceof Response) return accessIdentity;

  return c.json({
    accessSubject: accessIdentity.subject,
    email: accessIdentity.email,
  });
});
app.get("/v1/admin/me", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;

  return c.json({
    principal: {
      id: principal.id,
      accessSubject: principal.access_subject,
      role: principal.role,
      displayName: principal.display_name,
    },
  });
});
app.get("/v1/admin/offline-rewards", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, OFFLINE_REWARD_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminOfflineRewardBody(c);

  const whereClauses: string[] = [];
  const values: Array<string | number> = [];
  const filters = [
    ["accountId", "a.id"],
    ["accountStatus", "a.status"],
    ["storyProgressId", "COALESCE(sp.highest_story_progress_id, ors.last_story_progress_id, 'none')"],
    ["lastStoryProgressId", "COALESCE(ors.last_story_progress_id, 'none')"],
    ["narrativeVersion", "COALESCE(sp.narrative_version, ors.last_narrative_version, ?)"],
  ] as const;
  for (const [queryName, columnName] of filters) {
    const value = c.req.query(queryName)?.trim();
    if (value !== undefined && value !== "") {
      whereClauses.push(`${columnName} = ?`);
      if (queryName === "narrativeVersion") values.push(ACT_ONE_NARRATIVE_VERSION);
      values.push(value);
    }
  }

  const updatedFrom = c.req.query("updatedFrom");
  if (updatedFrom !== undefined) {
    const parsed = Date.parse(updatedFrom);
    if (Number.isFinite(parsed)) {
      whereClauses.push("COALESCE(ors.updated_at, a.updated_at) >= ?");
      values.push(parsed);
    }
  }
  const updatedTo = c.req.query("updatedTo");
  if (updatedTo !== undefined) {
    const parsed = Date.parse(updatedTo);
    if (Number.isFinite(parsed)) {
      whereClauses.push("COALESCE(ors.updated_at, a.updated_at) <= ?");
      values.push(parsed);
    }
  }

  const limitQuery = c.req.query("limit");
  const parsedLimit = limitQuery === undefined ? 50 : Number(limitQuery);
  const limit = Number.isSafeInteger(parsedLimit) && parsedLimit > 0 ? Math.min(parsedLimit, 50) : 50;
  const cursorQuery = c.req.query("cursor");
  const parsedCursor = cursorQuery === undefined ? 0 : Number(cursorQuery);
  const offset = Number.isSafeInteger(parsedCursor) && parsedCursor >= 0 ? parsedCursor : 0;
  values.push(limit + 1, offset);

  const rows = await c.env.DB.prepare(
    `SELECT
       a.id AS account_id,
       pp.display_name AS display_name,
       a.status AS account_status,
       a.created_at AS account_created_at,
       sp.highest_story_progress_id AS highest_story_progress_id,
       sp.narrative_version AS narrative_version,
       ors.last_offline_reward_claim_at AS last_offline_reward_claim_at,
       ors.last_story_progress_id AS last_story_progress_id,
       ors.last_balance_config_version AS last_balance_config_version,
       ors.last_narrative_version AS last_narrative_version,
       ors.last_previewed_at AS last_previewed_at,
       ors.revision AS revision,
       ors.updated_at AS state_updated_at,
       last_claim.id AS last_claim_id,
       last_claim.created_at AS last_claim_created_at
     FROM accounts a
     LEFT JOIN player_profiles pp ON pp.account_id = a.id
     LEFT JOIN account_story_progress sp ON sp.account_id = a.id
     LEFT JOIN account_offline_reward_state ors ON ors.account_id = a.id
     LEFT JOIN reward_transactions last_claim
       ON last_claim.id = (
         SELECT rt.id
         FROM reward_transactions rt
         WHERE rt.account_id = a.id AND rt.run_id IS NULL
         ORDER BY rt.created_at DESC, rt.id DESC
         LIMIT 1
       )
     ${whereClauses.length === 0 ? "" : `WHERE ${whereClauses.join(" AND ")}`}
     ORDER BY COALESCE(ors.updated_at, a.updated_at) DESC, a.id
     LIMIT ? OFFSET ?`,
  )
    .bind(...values)
    .all<AdminOfflineRewardRow>();

  const visibleRows = rows.results.slice(0, limit);
  return c.json({
    permission: OFFLINE_REWARD_READ_PERMISSION,
    permissionMode: "ROLE_PERMISSION",
    items: visibleRows.map(offlineRewardSummary),
    nextCursor: rows.results.length > limit ? String(offset + limit) : null,
  });
});
app.get("/v1/admin/offline-rewards/:accountId", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, OFFLINE_REWARD_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminOfflineRewardBody(c);

  const accountId = c.req.param("accountId");
  const row = await c.env.DB.prepare(
    `SELECT
       a.id AS account_id,
       pp.display_name AS display_name,
       a.status AS account_status,
       a.created_at AS account_created_at,
       sp.highest_story_progress_id AS highest_story_progress_id,
       sp.narrative_version AS narrative_version,
       ors.last_offline_reward_claim_at AS last_offline_reward_claim_at,
       ors.last_story_progress_id AS last_story_progress_id,
       ors.last_balance_config_version AS last_balance_config_version,
       ors.last_narrative_version AS last_narrative_version,
       ors.last_previewed_at AS last_previewed_at,
       ors.revision AS revision,
       ors.updated_at AS state_updated_at,
       last_claim.id AS last_claim_id,
       last_claim.created_at AS last_claim_created_at
     FROM accounts a
     LEFT JOIN player_profiles pp ON pp.account_id = a.id
     LEFT JOIN account_story_progress sp ON sp.account_id = a.id
     LEFT JOIN account_offline_reward_state ors ON ors.account_id = a.id
     LEFT JOIN reward_transactions last_claim
       ON last_claim.id = (
         SELECT rt.id
         FROM reward_transactions rt
         WHERE rt.account_id = a.id AND rt.run_id IS NULL
         ORDER BY rt.created_at DESC, rt.id DESC
         LIMIT 1
       )
     WHERE a.id = ?`,
  )
    .bind(accountId)
    .first<AdminOfflineRewardRow>();

  if (row === null) {
    return c.json({ error: "account_not_found", message: "Account was not found." }, 404);
  }

  return c.json(offlineRewardSummary(row));
});
app.get("/v1/admin/story-progress", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, STORY_PROGRESS_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminStoryProgressBody(c);

  const constantFilters = [
    ["contentStatus", "CANDIDATE"],
    ["localizationStatus", "PENDING"],
    ["fixtureStatus", "PARTIAL"],
    ["missingContentStatus", "UNKNOWN"],
  ] as const;
  if (constantFilters.some(([queryName, expected]) => {
    const actual = c.req.query(queryName);
    return actual !== undefined && actual !== expected;
  })) {
    return c.json({
      permission: STORY_PROGRESS_READ_PERMISSION,
      permissionMode: "ROLE_PERMISSION",
      items: [],
      nextCursor: null,
    });
  }

  const whereClauses: string[] = [];
  const values: Array<string | number> = [];
  const filters = [
    ["accountId", "account_id"],
    ["actId", "act_id"],
    ["highestStoryProgressId", "highest_story_progress_id"],
    ["highestClearedOperationId", "highest_cleared_operation_id"],
    ["narrativeVersion", "narrative_version"],
  ] as const;
  for (const [queryName, columnName] of filters) {
    const value = c.req.query(queryName)?.trim();
    if (value !== undefined && value !== "") {
      whereClauses.push(`${columnName} = ?`);
      values.push(value);
    }
  }

  const updatedFrom = c.req.query("updatedFrom");
  if (updatedFrom !== undefined) {
    const parsed = Date.parse(updatedFrom);
    if (Number.isFinite(parsed)) {
      whereClauses.push("updated_at >= ?");
      values.push(parsed);
    }
  }
  const updatedTo = c.req.query("updatedTo");
  if (updatedTo !== undefined) {
    const parsed = Date.parse(updatedTo);
    if (Number.isFinite(parsed)) {
      whereClauses.push("updated_at <= ?");
      values.push(parsed);
    }
  }

  const limitQuery = c.req.query("limit");
  const parsedLimit = limitQuery === undefined ? 50 : Number(limitQuery);
  const limit = Number.isSafeInteger(parsedLimit) && parsedLimit > 0 ? Math.min(parsedLimit, 50) : 50;
  const cursorQuery = c.req.query("cursor");
  const parsedCursor = cursorQuery === undefined ? 0 : Number(cursorQuery);
  const offset = Number.isSafeInteger(parsedCursor) && parsedCursor >= 0 ? parsedCursor : 0;
  values.push(limit + 1, offset);

  const rows = await c.env.DB.prepare(
    `SELECT account_id, act_id, highest_cleared_operation_id, highest_story_progress_id,
            cleared_operations_json, story_flags_json, narrative_version,
            last_progress_run_id, updated_at
     FROM account_story_progress
     ${whereClauses.length === 0 ? "" : `WHERE ${whereClauses.join(" AND ")}`}
     ORDER BY updated_at DESC, account_id
     LIMIT ? OFFSET ?`,
  )
    .bind(...values)
    .all<AdminStoryProgressRow>();

  const visibleRows = rows.results.slice(0, limit);
  return c.json({
    permission: STORY_PROGRESS_READ_PERMISSION,
    permissionMode: "ROLE_PERMISSION",
    items: visibleRows.map(storyProgressSummary),
    nextCursor: rows.results.length > limit ? String(offset + limit) : null,
  });
});
app.get("/v1/admin/story-progress/:accountId", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, STORY_PROGRESS_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminStoryProgressBody(c);

  const accountId = c.req.param("accountId");
  const row = await c.env.DB.prepare(
    `SELECT account_id, act_id, highest_cleared_operation_id, highest_story_progress_id,
            cleared_operations_json, story_flags_json, narrative_version,
            last_progress_run_id, updated_at
     FROM account_story_progress
     WHERE account_id = ?`,
  )
    .bind(accountId)
    .first<AdminStoryProgressRow>();

  if (row === null) {
    return c.json(defaultStoryProgressDetail(accountId));
  }

  const unlockRows = await c.env.DB.prepare(
    `SELECT unlock_id
     FROM account_unlocks
     WHERE account_id = ? AND unlock_id LIKE 'operation:%'
     ORDER BY unlocked_at, unlock_id`,
  )
    .bind(accountId)
    .all<AdminStoryProgressUnlockRow>();

  return c.json(storyProgressDetail(row, unlockRows.results));
});
app.get("/v1/admin/players", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;

  const rawQuery = c.req.query("q") ?? "";
  const query = rawQuery.trim();
  const likeQuery = `%${query}%`;
  const rows = await c.env.DB.prepare(
    `SELECT
       a.id AS account_id,
       pp.display_name AS display_name,
       a.status AS status,
       a.updated_at AS updated_at,
       CASE
         WHEN (SELECT COUNT(*) FROM operator_training_core_slots core WHERE core.account_id = a.id) = 5
         THEN (SELECT MIN(trained_level) FROM operator_training_core_slots core WHERE core.account_id = a.id)
         ELSE 1
       END AS sync_level
     FROM accounts a
     LEFT JOIN player_profiles pp ON pp.account_id = a.id
     WHERE (? = '' OR a.id LIKE ? OR pp.display_name LIKE ?)
     ORDER BY a.updated_at DESC
     LIMIT 50`,
  )
    .bind(query, likeQuery, likeQuery)
    .all<AdminPlayerSearchRow>();

  return c.json({
    items: rows.results.map((row) => ({
      accountId: row.account_id,
      displayName: row.display_name ?? row.account_id,
      status: row.status,
      syncLevel: row.sync_level ?? 1,
      lastSeenAt: toIso(row.updated_at),
    })),
  });
});
app.get("/v1/admin/players/:accountId", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;

  const accountId = c.req.param("accountId");
  const account = await c.env.DB.prepare(
    `SELECT
       a.id AS account_id,
       pp.display_name AS display_name,
       a.status AS status,
       a.created_at AS created_at,
       ac.balance AS operator_exp
     FROM accounts a
     LEFT JOIN player_profiles pp ON pp.account_id = a.id
     LEFT JOIN account_currencies ac
       ON ac.account_id = a.id
      AND ac.currency = 'OPERATOR_EXP'
     WHERE a.id = ?`,
  )
    .bind(accountId)
    .first<AdminPlayerDetailRow>();
  if (account === null) {
    return c.json({ error: "ADMIN_PLAYER_NOT_FOUND" }, 404);
  }

  const core = await c.env.DB.prepare(
    `SELECT slot_index, operator_id, trained_level
     FROM operator_training_core_slots
     WHERE account_id = ?
     ORDER BY slot_index ASC`,
  )
    .bind(accountId)
    .all<AdminTrainingCoreRow>();
  const operators = await c.env.DB.prepare(
    `SELECT operator_id, skill_training, promotion
     FROM account_operators
     WHERE account_id = ?
     ORDER BY operator_id ASC`,
  )
    .bind(accountId)
    .all<AdminOperatorRow>();

  const coreRows = core.results;
  const syncLevel = syncLevelFrom(coreRows);
  const coreByOperator = new Map(coreRows.map((row) => [row.operator_id, row]));

  return c.json({
    accountId: account.account_id,
    displayName: account.display_name ?? account.account_id,
    status: account.status,
    createdAt: toIso(account.created_at),
    progression: {
      operatorExp: account.operator_exp ?? 0,
      syncLevel,
      coreLevelSpread: coreLevelSpreadFrom(coreRows),
      trainingCore: coreRows.map((row) => ({
        slot: row.slot_index + 1,
        operatorId: row.operator_id,
        name: row.operator_id,
        trainedLevel: row.trained_level,
      })),
      operators: operators.results.map((operator) => {
        const coreSlot = coreByOperator.get(operator.operator_id);
        const isTrainingCore = coreSlot !== undefined;
        return {
          operatorId: operator.operator_id,
          name: operator.operator_id,
          effectiveLevel: isTrainingCore ? coreSlot.trained_level : syncLevel,
          levelSource: isTrainingCore ? "TRAINED" : "SYNC",
          trainedLevel: coreSlot?.trained_level ?? 1,
          skillTraining: operator.skill_training,
          promotion: String(operator.promotion),
          ...(coreSlot ? { coreSlot: coreSlot.slot_index + 1 } : {}),
        };
      }),
    },
  });
});
app.post("/v1/admin/players/:accountId/notes", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (principal.role === "VIEWER") {
    return c.json({ error: "ADMIN_PERMISSION_DENIED" }, 403);
  }

  const accountId = c.req.param("accountId");
  const body = (await c.req.json()) as { note?: unknown };
  const note = typeof body.note === "string" ? body.note.trim() : "";
  if (accountId.length === 0 || note.length === 0 || note.length > 1000) {
    return c.json({ error: "PLAYER_NOTE_INVALID" }, 400);
  }

  const account = await c.env.DB.prepare("SELECT id FROM accounts WHERE id = ?")
    .bind(accountId)
    .first<{ id: string }>();
  if (account === null) {
    return c.json({ error: "ADMIN_PLAYER_NOT_FOUND" }, 404);
  }

  const requestId = c.req.header("x-request-id") ?? createRequestId();
  const changeSummary = { note };
  const payloadHash = await createPayloadHash({ accountId, ...changeSummary });
  await c.env.DB.prepare(
    `INSERT INTO admin_audit_logs (
       id,
       principal_id,
       role,
       permission,
       action,
       resource_type,
       resource_id,
       request_id,
       confirmation_id,
       payload_hash,
       change_summary_json,
       account_mutation_id,
       result,
       error_code,
       created_at
     ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, NULL, ?, ?, NULL, ?, NULL, ?)`,
  )
    .bind(
      `audit_${createRequestId().slice(4)}`,
      principal.id,
      principal.role,
      ACCOUNT_NOTE_WRITE_PERMISSION,
      "PLAYER_NOTE_CREATE",
      "account",
      account.id,
      requestId,
      payloadHash,
      canonicalJson(changeSummary),
      "SUCCESS",
      Date.now(),
    )
    .run();

  return c.json(
    {
      note: {
        accountId: account.id,
        action: "PLAYER_NOTE_CREATE",
        result: "SUCCESS",
        requestId,
      },
    },
    201,
  );
});
app.get("/v1/admin/players/:accountId/notes", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;

  const accountId = c.req.param("accountId");
  const rows = await c.env.DB.prepare(
    `SELECT
       audit.id AS id,
       audit.principal_id AS principal_id,
       principal.display_name AS display_name,
       audit.resource_id AS resource_id,
       audit.request_id AS request_id,
       audit.change_summary_json AS change_summary_json,
       audit.created_at AS created_at
     FROM admin_audit_logs audit
     LEFT JOIN admin_principals principal ON principal.id = audit.principal_id
     WHERE audit.action = 'PLAYER_NOTE_CREATE'
       AND audit.resource_type = 'account'
       AND audit.resource_id = ?
       AND audit.result = 'SUCCESS'
     ORDER BY audit.created_at DESC
     LIMIT 20`,
  )
    .bind(accountId)
    .all<AdminPlayerNoteRow>();

  return c.json({
    items: rows.results.map((row) => ({
      id: row.id,
      accountId: row.resource_id,
      note: parsePlayerNoteSummary(row.change_summary_json),
      authorId: row.principal_id,
      authorDisplayName: row.display_name ?? row.principal_id,
      requestId: row.request_id,
      createdAt: toIso(row.created_at),
    })),
  });
});
app.get("/v1/admin/audit", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;

  const action = c.req.query("action")?.trim() || null;
  const resourceId = c.req.query("resourceId")?.trim() || null;
  const result = c.req.query("result")?.trim() || null;
  if (result !== null && result !== "SUCCESS" && result !== "FAILURE") {
    return c.json({ error: "AUDIT_FILTER_INVALID" }, 400);
  }

  const rows = await c.env.DB.prepare(
    `SELECT
       audit.id AS id,
       audit.principal_id AS principal_id,
       principal.display_name AS display_name,
       audit.role AS role,
       audit.permission AS permission,
       audit.action AS action,
       audit.resource_type AS resource_type,
       audit.resource_id AS resource_id,
       audit.request_id AS request_id,
       audit.confirmation_id AS confirmation_id,
       audit.payload_hash AS payload_hash,
       audit.change_summary_json AS change_summary_json,
       audit.result AS result,
       audit.error_code AS error_code,
       audit.created_at AS created_at
     FROM admin_audit_logs audit
     LEFT JOIN admin_principals principal ON principal.id = audit.principal_id
     WHERE (? IS NULL OR audit.action = ?)
       AND (? IS NULL OR audit.resource_id = ?)
       AND (? IS NULL OR audit.result = ?)
     ORDER BY audit.created_at DESC
     LIMIT 100`,
  )
    .bind(action, action, resourceId, resourceId, result, result)
    .all<AdminAuditLogRow>();

  return c.json({
    items: rows.results.map((row) => ({
      id: row.id,
      principalId: row.principal_id,
      principalDisplayName: row.display_name ?? row.principal_id ?? "Unknown",
      role: row.role,
      permission: row.permission,
      action: row.action,
      resourceType: row.resource_type,
      resourceId: row.resource_id,
      requestId: row.request_id,
      confirmationId: row.confirmation_id,
      payloadHash: row.payload_hash,
      changeSummary: parseJsonValue(row.change_summary_json),
      result: row.result,
      errorCode: row.error_code,
      createdAt: toIso(row.created_at),
    })),
  });
});
app.get("/v1/admin/integrity/events", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;

  const accountId = c.req.query("accountId")?.trim() || null;
  const classification = c.req.query("classification")?.trim() || null;
  const reviewState = c.req.query("reviewState")?.trim() || null;
  if (classification !== null && classification !== "SOFT_ANOMALY" && classification !== "HARD_VIOLATION") {
    return c.json({ error: "INTEGRITY_FILTER_INVALID" }, 400);
  }
  if (reviewState !== null && reviewState !== "OPEN" && reviewState !== "IN_REVIEW" && reviewState !== "RESOLVED") {
    return c.json({ error: "INTEGRITY_FILTER_INVALID" }, 400);
  }

  const rows = await c.env.DB.prepare(
    `SELECT
       event.id AS id,
       event.account_id AS account_id,
       event.run_id AS run_id,
       event.classification AS classification,
       event.category AS category,
       event.evidence_code AS evidence_code,
       event.evidence_json AS evidence_json,
       COALESCE(review.state, 'OPEN') AS review_state,
       review.assigned_to AS assigned_to,
       event.created_at AS created_at
     FROM integrity_events event
     LEFT JOIN integrity_event_review_states review ON review.event_id = event.id
     WHERE (? IS NULL OR event.account_id = ?)
       AND (? IS NULL OR event.classification = ?)
       AND (? IS NULL OR COALESCE(review.state, 'OPEN') = ?)
     ORDER BY event.created_at DESC
     LIMIT 100`,
  )
    .bind(accountId, accountId, classification, classification, reviewState, reviewState)
    .all<AdminIntegrityEventRow>();

  return c.json({
    items: rows.results.map((row) => ({
      id: row.id,
      accountId: row.account_id,
      runId: row.run_id,
      classification: row.classification,
      category: row.category,
      evidenceCode: row.evidence_code,
      evidence: parseJsonValue(row.evidence_json),
      reviewState: row.review_state,
      assignedTo: row.assigned_to,
      createdAt: toIso(row.created_at),
    })),
  });
});
app.get("/v1/admin/integrity/suspicious-players", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;

  const reviewStatus = c.req.query("reviewStatus")?.trim() || null;
  if (
    reviewStatus !== null &&
    reviewStatus !== "NEW" &&
    reviewStatus !== "REVIEWING" &&
    reviewStatus !== "CLEARED" &&
    reviewStatus !== "SCORE_EXCLUDED" &&
    reviewStatus !== "REWARD_HELD" &&
    reviewStatus !== "ESCALATED" &&
    reviewStatus !== "ACTIONED" &&
    reviewStatus !== "FALSE_POSITIVE"
  ) {
    return c.json({ error: "SUSPICIOUS_PLAYERS_FILTER_INVALID" }, 400);
  }
  const rows = await c.env.DB.prepare(
    `SELECT
       event.account_id AS account_id,
       profile.display_name AS display_name,
       COUNT(*) AS open_signals,
       SUM(CASE WHEN event.classification = 'HARD_VIOLATION' THEN 1 ELSE 0 END) AS hard_violations,
       COUNT(DISTINCT event.run_id) AS suspicious_runs,
       MAX(event.created_at) AS last_detection_at,
       latest.evidence_code AS primary_reason,
       COALESCE(
         MAX(account_review.status),
         CASE MAX(CASE WHEN COALESCE(review.state, 'OPEN') = 'IN_REVIEW' THEN 1 ELSE 0 END)
           WHEN 1 THEN 'REVIEWING'
           ELSE 'NEW'
         END
       ) AS review_status,
       enforcement.type AS current_enforcement,
       COALESCE(MAX(account_review.assigned_to), MAX(review.assigned_to)) AS review_owner
     FROM integrity_events event
     LEFT JOIN player_profiles profile ON profile.account_id = event.account_id
     LEFT JOIN integrity_event_review_states review ON review.event_id = event.id
     LEFT JOIN suspicious_player_reviews account_review ON account_review.account_id = event.account_id
     LEFT JOIN account_enforcements enforcement
       ON enforcement.account_id = event.account_id
      AND enforcement.status = 'ACTIVE'
     LEFT JOIN integrity_events latest
       ON latest.id = (
         SELECT inner_event.id
         FROM integrity_events inner_event
         WHERE inner_event.account_id = event.account_id
         ORDER BY inner_event.created_at DESC
         LIMIT 1
       )
     WHERE COALESCE(review.state, 'OPEN') IN ('OPEN', 'IN_REVIEW')
     GROUP BY event.account_id
     HAVING (? IS NULL OR
       COALESCE(
         MAX(account_review.status),
         CASE MAX(CASE WHEN COALESCE(review.state, 'OPEN') = 'IN_REVIEW' THEN 1 ELSE 0 END)
           WHEN 1 THEN 'REVIEWING'
           ELSE 'NEW'
         END
       ) = ?)
     ORDER BY last_detection_at DESC
     LIMIT 100`,
  )
    .bind(reviewStatus, reviewStatus)
    .all<AdminSuspiciousPlayerRow>();

  return c.json({
    items: rows.results.map((row) => {
      const review = suspiciousReviewStatus(row.review_status);
      const markOnlyStatus = currentMarkOnlyStatus(review);
      return {
        accountId: row.account_id,
        displayName: row.display_name ?? row.account_id,
        priority: suspiciousPriority(row.open_signals, row.hard_violations, [row.primary_reason]),
        openSignals: row.open_signals,
        hardViolations: row.hard_violations,
        suspiciousRuns: row.suspicious_runs,
        lastDetectionAt: toIso(row.last_detection_at),
        primaryReason: row.primary_reason,
        reviewStatus: review,
        currentEnforcement: markOnlyStatus === "NONE" ? currentEnforcementStatus(row.current_enforcement) : markOnlyStatus,
        reviewOwner: row.review_owner,
      };
    }),
  });
});
app.get("/v1/admin/integrity/suspicious-players/:accountId", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;

  const accountId = c.req.param("accountId");
  const account = await c.env.DB.prepare(
    `SELECT account.id AS account_id,
            profile.display_name AS display_name,
            account.status AS status,
            account.created_at AS created_at
     FROM accounts account
     LEFT JOIN player_profiles profile ON profile.account_id = account.id
     WHERE account.id = ?`,
  )
    .bind(accountId)
    .first<AdminSuspiciousPlayerAccountRow>();

  if (account === null) {
    return c.json({ error: "ACCOUNT_NOT_FOUND" }, 404);
  }

  const review = await c.env.DB.prepare(
    `SELECT status,
            assigned_to,
            reason,
            updated_at
     FROM suspicious_player_reviews
     WHERE account_id = ?`,
  )
    .bind(accountId)
    .first<AdminSuspiciousPlayerReviewRow>();

  const signals = await c.env.DB.prepare(
    `SELECT event.id AS id,
            event.account_id AS account_id,
            event.run_id AS run_id,
            event.classification AS classification,
            event.category AS category,
            event.evidence_code AS evidence_code,
            event.evidence_json AS evidence_json,
            COALESCE(review.state, 'OPEN') AS review_state,
            review.assigned_to AS assigned_to,
            event.created_at AS created_at
     FROM integrity_events event
     LEFT JOIN integrity_event_review_states review ON review.event_id = event.id
     WHERE event.account_id = ?
     ORDER BY event.created_at DESC
     LIMIT 100`,
  )
    .bind(accountId)
    .all<AdminIntegrityEventRow>();

  const runIds = [...new Set(signals.results.map((signal) => signal.run_id).filter((runId): runId is string => runId !== null))];
  const runs = runIds.length > 0
    ? await c.env.DB.prepare(
        `SELECT run.id AS run_id,
                run.status AS status,
                preview.risk AS risk,
                run.balance_config_version AS balance_config_version,
                run.started_at AS started_at,
                run.finished_at AS finished_at
         FROM runs run
         LEFT JOIN operation_previews preview ON preview.id = run.preview_id
         WHERE run.id IN (${runIds.map(() => "?").join(", ")})
         ORDER BY run.started_at DESC`,
      )
        .bind(...runIds)
        .all<AdminSuspiciousPlayerRunRow>()
    : { results: [] };

  const notes = await c.env.DB.prepare(
    `SELECT audit.id AS id,
            audit.principal_id AS principal_id,
            principal.display_name AS display_name,
            audit.resource_id AS resource_id,
            audit.request_id AS request_id,
            audit.change_summary_json AS change_summary_json,
            audit.created_at AS created_at
     FROM admin_audit_logs audit
     LEFT JOIN admin_principals principal ON principal.id = audit.principal_id
     WHERE audit.action = 'PLAYER_NOTE_CREATE'
       AND audit.resource_id = ?
     ORDER BY audit.created_at DESC
     LIMIT 25`,
  )
    .bind(accountId)
    .all<AdminPlayerNoteRow>();

  const decisions = await c.env.DB.prepare(
    `SELECT history.id AS id,
            principal.display_name AS display_name,
            history.from_status AS from_status,
            history.to_status AS to_status,
            history.reason AS reason,
            history.created_at AS created_at
     FROM suspicious_player_review_history history
     LEFT JOIN admin_principals principal ON principal.id = history.created_by
     WHERE history.account_id = ?
     ORDER BY history.created_at DESC
     LIMIT 25`,
  )
    .bind(accountId)
    .all<AdminSuspiciousPlayerDecisionRow>();

  const hardViolations = signals.results.filter((signal) => signal.classification === "HARD_VIOLATION").length;
  const derivedReviewOwner = signals.results.find((signal) => signal.assigned_to !== null)?.assigned_to ?? null;
  const hasReviewingSignal = signals.results.some((signal) => suspiciousReviewStatus(signal.review_state) === "REVIEWING");
  const evidenceCodes = signals.results.map((signal) => signal.evidence_code);
  const accountReviewStatus = review ? suspiciousReviewStatus(review.status) : null;
  const derivedReviewStatus = hasReviewingSignal ? "REVIEWING" : "NEW";

  return c.json({
    account: {
      accountId: account.account_id,
      displayName: account.display_name ?? account.account_id,
      status: account.status,
      createdAt: toIso(account.created_at),
    },
    summary: {
      priority: suspiciousPriority(signals.results.length, hardViolations, evidenceCodes),
      openSignals: signals.results.length,
      hardViolations,
      suspiciousRuns: runIds.length,
      lastDetectionAt: signals.results[0] ? toIso(signals.results[0].created_at) : null,
      primaryReason: signals.results[0]?.evidence_code ?? null,
      reviewStatus: accountReviewStatus ?? derivedReviewStatus,
      reviewReason: review?.reason ?? null,
      reviewUpdatedAt: review ? toIso(review.updated_at) : null,
      currentEnforcement: currentMarkOnlyStatus(accountReviewStatus),
      reviewOwner: review?.assigned_to ?? derivedReviewOwner,
    },
    riskSignals: signals.results.map((signal) => ({
      id: signal.id,
      runId: signal.run_id,
      classification: signal.classification,
      category: signal.category,
      evidenceCode: signal.evidence_code,
      evidence: parseJsonValue(signal.evidence_json),
      reviewStatus: suspiciousReviewStatus(signal.review_state),
      reviewOwner: signal.assigned_to,
      createdAt: toIso(signal.created_at),
    })),
    suspiciousRuns: runs.results.map((run) => ({
      runId: run.run_id,
      runType: suspiciousRunType(run.run_id),
      result: mapRunResult(run.status),
      riskTier: normalizeRiskTier(run.risk),
      balanceVersion: String(run.balance_config_version),
      startedAt: toIso(run.started_at),
      completedAt: run.finished_at === null ? null : toIso(run.finished_at),
    })),
    notes: notes.results.map((row) => ({
      id: row.id,
      accountId: row.resource_id,
      note: parsePlayerNoteSummary(row.change_summary_json),
      authorId: row.principal_id,
      authorDisplayName: row.display_name ?? row.principal_id,
      requestId: row.request_id,
      createdAt: toIso(row.created_at),
    })),
    decisions: decisions.results.map((row) => ({
      id: row.id,
      action: "SUSPICIOUS_PLAYER_REVIEW_MARK",
      result: "SUCCESS",
      principalDisplayName: row.display_name ?? "Unknown Admin",
      changeSummary: {
        fromStatus: row.from_status,
        toStatus: row.to_status,
        reason: row.reason,
      },
      createdAt: toIso(row.created_at),
    })),
  });
});
app.get("/v1/admin/runs", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;

  const accountId = c.req.query("accountId")?.trim();
  const rows = await c.env.DB.prepare(
    `SELECT
       r.id AS run_id,
       r.account_id AS account_id,
       r.status AS status,
       r.started_at AS started_at,
       r.finished_at AS finished_at,
       op.risk AS risk,
       r.balance_config_version AS balance_config_version,
       vv.version AS validator_version
     FROM runs r
     LEFT JOIN operation_previews op ON op.id = r.preview_id
     LEFT JOIN validator_runtime_active vra ON vra.singleton_id = 1
     LEFT JOIN validator_runtime_configs vrc ON vrc.id = vra.runtime_config_id
     LEFT JOIN validator_versions vv ON vv.id = vrc.validator_version_id
     WHERE (? IS NULL OR r.account_id = ?)
     ORDER BY r.started_at DESC
     LIMIT 50`,
  )
    .bind(accountId ?? null, accountId ?? null)
    .all<AdminRunSearchRow>();

  return c.json({
    items: rows.results.map((row) => ({
      runId: row.run_id,
      accountId: row.account_id,
      startedAt: toIso(row.started_at),
      ...(row.finished_at ? { completedAt: toIso(row.finished_at) } : {}),
      result: mapRunResult(row.status),
      riskTier: normalizeRiskTier(row.risk),
      balanceVersion: String(row.balance_config_version),
      ...(row.validator_version ? { validatorVersion: row.validator_version } : {}),
    })),
  });
});
app.get("/v1/admin/runs/:runId", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;

  const row = await c.env.DB.prepare(
    `SELECT
       r.id AS run_id,
       r.account_id AS account_id,
       r.status AS status,
       r.started_at AS started_at,
       r.finished_at AS finished_at,
       op.risk AS risk,
       r.balance_config_version AS balance_config_version,
       active.version AS current_active_balance_version,
       r.build_id AS build_id,
       cb.content_version AS content_version,
       vv.version AS validator_version,
       vrc.runtime_revision AS validator_runtime_revision,
       rs.snapshot_json AS snapshot_json
     FROM runs r
     INNER JOIN run_snapshots rs ON rs.run_id = r.id
     LEFT JOIN operation_previews op ON op.id = r.preview_id
     LEFT JOIN client_builds cb ON cb.id = r.build_id
     LEFT JOIN balance_config_active active ON active.singleton_id = 1
     LEFT JOIN validator_runtime_active vra ON vra.singleton_id = 1
     LEFT JOIN validator_runtime_configs vrc ON vrc.id = vra.runtime_config_id
     LEFT JOIN validator_versions vv ON vv.id = vrc.validator_version_id
     WHERE r.id = ?`,
  )
    .bind(c.req.param("runId"))
    .first<AdminRunDetailRow>();
  if (row === null) {
    return c.json({ error: "ADMIN_RUN_NOT_FOUND" }, 404);
  }

  return c.json({
    runId: row.run_id,
    accountId: row.account_id,
    startedAt: toIso(row.started_at),
    ...(row.finished_at ? { completedAt: toIso(row.finished_at) } : {}),
    result: mapRunResult(row.status),
    riskTier: normalizeRiskTier(row.risk),
    balanceVersion: String(row.balance_config_version),
    currentActiveBalanceVersion: String(row.current_active_balance_version ?? row.balance_config_version),
    clientBuild: row.build_id ?? "UNKNOWN",
    contentVersion: String(row.content_version ?? 0),
    validatorVersion: row.validator_version ?? "UNKNOWN",
    validatorRuntimeRevision: String(row.validator_runtime_revision ?? 0),
    snapshot: parseRunSnapshot(row.snapshot_json),
  });
});
app.post("/v1/admin/balance-config/drafts", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (principal.role === "VIEWER") {
    return c.json({ error: "ADMIN_PERMISSION_DENIED" }, 403);
  }

  const body = (await c.req.json()) as {
    baseVersion?: unknown;
    schemaVersion?: unknown;
    requiredContentVersion?: unknown;
    config?: unknown;
  };
  if (
    !(body.baseVersion === undefined || body.baseVersion === null || isPositiveInteger(body.baseVersion)) ||
    !isPositiveInteger(body.schemaVersion) ||
    !isNonNegativeInteger(body.requiredContentVersion) ||
    !isJsonObject(body.config)
  ) {
    return c.json({ error: "BALANCE_DRAFT_INVALID" }, 400);
  }

  const now = Date.now();
  const draftId = `draft_${createRequestId().slice(4)}`;
  const configJson = canonicalJson(body.config);
  const checksum = `sha256:${await sha256Hex(configJson)}`;
  const baseVersion = body.baseVersion ?? null;

  await c.env.DB.prepare(
    `INSERT INTO balance_config_drafts (
       id,
       base_version,
       schema_version,
       required_content_version,
       config_json,
       checksum,
       revision,
       status,
       created_by,
       created_at,
       updated_at
     ) VALUES (?, ?, ?, ?, ?, ?, 1, 'DRAFT', ?, ?, ?)`,
  )
    .bind(draftId, baseVersion, body.schemaVersion, body.requiredContentVersion, configJson, checksum, principal.id, now, now)
    .run();

  return c.json(
    {
      draft: {
        id: draftId,
        baseVersion,
        schemaVersion: body.schemaVersion,
        requiredContentVersion: body.requiredContentVersion,
        checksum,
        revision: 1,
        status: "DRAFT",
      },
    },
    201,
  );
});
app.post("/v1/admin/balance-config/drafts/:draftId/publish/confirmations", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (principal.role !== "OWNER") {
    await writeBalancePublishFailureAudit(c, principal, c.req.param("draftId"), "ADMIN_PERMISSION_DENIED", null, { reason: "role_not_owner" });
    return c.json({ error: "ADMIN_PERMISSION_DENIED" }, 403);
  }

  const draftId = c.req.param("draftId");
  const body = (await c.req.json()) as {
    expectedRevision?: unknown;
    expectedChecksum?: unknown;
  };
  if (draftId.length === 0 || !isPositiveInteger(body.expectedRevision) || typeof body.expectedChecksum !== "string" || body.expectedChecksum.length === 0) {
    return c.json({ error: "BALANCE_PUBLISH_CONFIRMATION_INVALID" }, 400);
  }

  const draft = await c.env.DB.prepare(
    `SELECT id, base_version, schema_version, required_content_version, config_json, checksum, revision, status
     FROM balance_config_drafts
     WHERE id = ?
       AND status = 'DRAFT'`,
  )
    .bind(draftId)
    .first<BalanceDraftRow>();
  if (draft === null) {
    return c.json({ error: "BALANCE_DRAFT_NOT_FOUND" }, 404);
  }
  if (draft.revision !== body.expectedRevision || draft.checksum !== body.expectedChecksum) {
    await writeBalancePublishFailureAudit(c, principal, draft.id, "BALANCE_DRAFT_PRECONDITION_FAILED", null, {
      expectedRevision: body.expectedRevision,
      actualRevision: draft.revision,
      expectedChecksum: body.expectedChecksum,
      actualChecksum: draft.checksum,
    });
    return c.json({ error: "BALANCE_DRAFT_PRECONDITION_FAILED" }, 409);
  }

  const active = await c.env.DB.prepare(
    `SELECT version
     FROM balance_config_active
     WHERE singleton_id = 1`,
  )
    .bind()
    .first<ActiveBalanceRow>();
  const activeVersion = active?.version ?? null;
  if (draft.base_version !== activeVersion) {
    await writeBalancePublishFailureAudit(c, principal, draft.id, "BALANCE_DRAFT_STALE_BASE", null, {
      draftBaseVersion: draft.base_version,
      activeVersion,
    });
    return c.json({ error: "BALANCE_DRAFT_STALE_BASE" }, 409);
  }

  const now = Date.now();
  const expiresAt = now + CONFIRMATION_TTL_MS;
  const confirmationId = `confirmation_${createRequestId().slice(4)}`;
  const token = `confirm_${createRequestId().slice(4)}`;
  const payloadHash = await createBalancePublishPayloadHash(draft.id, body.expectedRevision, body.expectedChecksum);
  const tokenHash = await createTokenHash(token);

  await c.env.DB.batch([
    c.env.DB.prepare(
      `INSERT INTO admin_confirmations (
         id,
         principal_id,
         permission,
         payload_hash,
         token_hash,
         expires_at,
         consumed_at,
         created_at
       ) VALUES (?, ?, ?, ?, ?, ?, NULL, ?)`,
    ).bind(confirmationId, principal.id, BALANCE_PUBLISH_PERMISSION, payloadHash, tokenHash, expiresAt, now),
  ]);

  return c.json(
    {
      confirmation: {
        id: confirmationId,
        permission: BALANCE_PUBLISH_PERMISSION,
        payloadHash,
        expiresAt,
        token,
      },
    },
    201,
  );
});
app.post("/v1/admin/balance-config/drafts/:draftId/publish", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (principal.role !== "OWNER") {
    await writeBalancePublishFailureAudit(c, principal, c.req.param("draftId"), "ADMIN_PERMISSION_DENIED", null, { reason: "role_not_owner" });
    return c.json({ error: "ADMIN_PERMISSION_DENIED" }, 403);
  }

  const draftId = c.req.param("draftId");
  const body = (await c.req.json()) as {
    expectedRevision?: unknown;
    expectedChecksum?: unknown;
    confirmationToken?: unknown;
  };
  if (
    draftId.length === 0 ||
    !isPositiveInteger(body.expectedRevision) ||
    typeof body.expectedChecksum !== "string" ||
    body.expectedChecksum.length === 0 ||
    typeof body.confirmationToken !== "string" ||
    body.confirmationToken.length === 0
  ) {
    return c.json({ error: "BALANCE_PUBLISH_INVALID" }, 400);
  }
  const payloadHash = await createBalancePublishPayloadHash(draftId, body.expectedRevision, body.expectedChecksum);

  const draft = await c.env.DB.prepare(
    `SELECT id, base_version, schema_version, required_content_version, config_json, checksum, revision, status
     FROM balance_config_drafts
     WHERE id = ?
       AND status = 'DRAFT'`,
  )
    .bind(draftId)
    .first<BalanceDraftRow>();
  if (draft === null) {
    await writeBalancePublishFailureAudit(c, principal, draftId, "BALANCE_DRAFT_NOT_FOUND", payloadHash, { draftId });
    return c.json({ error: "BALANCE_DRAFT_NOT_FOUND" }, 404);
  }
  if (draft.revision !== body.expectedRevision || draft.checksum !== body.expectedChecksum) {
    await writeBalancePublishFailureAudit(c, principal, draft.id, "BALANCE_DRAFT_PRECONDITION_FAILED", payloadHash, {
      expectedRevision: body.expectedRevision,
      actualRevision: draft.revision,
      expectedChecksum: body.expectedChecksum,
      actualChecksum: draft.checksum,
    });
    return c.json({ error: "BALANCE_DRAFT_PRECONDITION_FAILED" }, 409);
  }

  const active = await c.env.DB.prepare(
    `SELECT version
     FROM balance_config_active
     WHERE singleton_id = 1`,
  )
    .bind()
    .first<ActiveBalanceRow>();
  const activeVersion = active?.version ?? null;
  if (draft.base_version !== activeVersion) {
    await writeBalancePublishFailureAudit(c, principal, draft.id, "BALANCE_DRAFT_STALE_BASE", payloadHash, {
      draftBaseVersion: draft.base_version,
      activeVersion,
    });
    return c.json({ error: "BALANCE_DRAFT_STALE_BASE" }, 409);
  }

  const nextVersionRow = await c.env.DB.prepare(
    `SELECT COALESCE(MAX(version), 0) + 1 AS next_version
     FROM balance_config_versions`,
  )
    .bind()
    .first<NextVersionRow>();
  const nextVersion = nextVersionRow?.next_version ?? 1;
  const now = Date.now();
  const tokenHash = await createTokenHash(body.confirmationToken);
  const confirmation = await c.env.DB.prepare(
    `SELECT id, payload_hash
     FROM admin_confirmations
     WHERE principal_id = ?
       AND permission = ?
       AND payload_hash = ?
       AND token_hash = ?
       AND consumed_at IS NULL
       AND expires_at > ?`,
  )
    .bind(principal.id, BALANCE_PUBLISH_PERMISSION, payloadHash, tokenHash, now)
    .first<AdminConfirmationRow>();
  if (confirmation === null) {
    await writeBalancePublishFailureAudit(c, principal, draft.id, "ADMIN_CONFIRMATION_INVALID", payloadHash, {
      expectedRevision: body.expectedRevision,
      expectedChecksum: body.expectedChecksum,
    });
    return c.json({ error: "ADMIN_CONFIRMATION_INVALID" }, 409);
  }

  const publishClaimId = `publish_${createRequestId().slice(4)}`;
  const activationId = `activation_${createRequestId().slice(4)}`;
  const requestId = createRequestId();

  const batchResults = await c.env.DB.batch([
    c.env.DB.prepare(
      `UPDATE admin_confirmations
       SET consumed_at = ?
       WHERE id = ?
         AND consumed_at IS NULL
         AND expires_at > ?`,
    ).bind(now, confirmation.id, now),
    c.env.DB.prepare(
      `INSERT INTO balance_config_versions (
         version,
         schema_version,
         required_content_version,
         checksum,
         config_json,
         published_at,
         published_by,
         draft_id
       )
       SELECT ?, ?, ?, ?, ?, ?, ?, ?
       WHERE EXISTS (
         SELECT 1
         FROM admin_confirmations
         WHERE id = ?
           AND consumed_at = ?
       )`,
    ).bind(nextVersion, draft.schema_version, draft.required_content_version, draft.checksum, draft.config_json, now, principal.id, draft.id, confirmation.id, now),
    c.env.DB.prepare(
      `INSERT INTO balance_config_publish_claims (
         id,
         draft_id,
         expected_revision,
         expected_checksum,
         published_version,
         created_at
       )
       SELECT ?, ?, ?, ?, ?, ?
       WHERE EXISTS (
         SELECT 1
         FROM admin_confirmations
         WHERE id = ?
           AND consumed_at = ?
       )`,
    ).bind(publishClaimId, draft.id, body.expectedRevision, body.expectedChecksum, nextVersion, now, confirmation.id, now),
    c.env.DB.prepare(
      `UPDATE balance_config_drafts
       SET status = 'PUBLISHED',
           updated_at = ?
       WHERE id = ?
         AND status = 'DRAFT'
         AND EXISTS (
           SELECT 1
           FROM admin_confirmations
           WHERE id = ?
             AND consumed_at = ?
         )`,
    ).bind(now, draft.id, confirmation.id, now),
    c.env.DB.prepare(
      `INSERT INTO balance_config_activations (
         id,
         version,
         activation_type,
         activated_by,
         activated_at,
         reason
       )
       SELECT ?, ?, 'PUBLISH', ?, ?, ?
       WHERE EXISTS (
         SELECT 1
         FROM admin_confirmations
         WHERE id = ?
           AND consumed_at = ?
       )`,
    ).bind(activationId, nextVersion, principal.id, now, null, confirmation.id, now),
    c.env.DB.prepare(
      `INSERT INTO balance_config_active (
         singleton_id,
         version,
         activation_id,
         updated_at
       )
       SELECT 1, ?, ?, ?
       WHERE EXISTS (
         SELECT 1
         FROM admin_confirmations
         WHERE id = ?
           AND consumed_at = ?
       )
       ON CONFLICT(singleton_id) DO UPDATE SET
         version = excluded.version,
         activation_id = excluded.activation_id,
         updated_at = excluded.updated_at`,
    ).bind(nextVersion, activationId, now, confirmation.id, now),
    c.env.DB.prepare(
      `INSERT INTO admin_audit_logs (
         id,
         principal_id,
         role,
         permission,
         action,
         resource_type,
         resource_id,
         request_id,
         confirmation_id,
         payload_hash,
         change_summary_json,
         account_mutation_id,
         result,
         error_code,
         created_at
       )
       SELECT ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, NULL, 'SUCCESS', NULL, ?
       WHERE EXISTS (
         SELECT 1
         FROM admin_confirmations
         WHERE id = ?
           AND consumed_at = ?
       )`,
    ).bind(
      `audit_${createRequestId().slice(4)}`,
      principal.id,
      principal.role,
      BALANCE_PUBLISH_PERMISSION,
      "BALANCE_CONFIG_PUBLISH",
      "balance_config_draft",
      draft.id,
      requestId,
      confirmation.id,
      confirmation.payload_hash,
      canonicalJson({ publishedVersion: nextVersion, activationId, checksum: draft.checksum }),
      now,
      confirmation.id,
      now,
    ),
  ]);
  if (d1Changes(batchResults[0]) !== 1) {
    await writeBalancePublishFailureAudit(c, principal, draft.id, "ADMIN_CONFIRMATION_CONSUME_FAILED", payloadHash, {
      confirmationId: confirmation.id,
      expectedRevision: body.expectedRevision,
      expectedChecksum: body.expectedChecksum,
    });
    return c.json({ error: "ADMIN_CONFIRMATION_CONSUME_FAILED" }, 409);
  }

  return c.json(
    {
      publish: {
        draftId: draft.id,
        version: nextVersion,
        activationId,
        checksum: draft.checksum,
        confirmationId: confirmation.id,
        requestId,
      },
    },
    201,
  );
});

export default app;


