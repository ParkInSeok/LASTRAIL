import { Hono, type Context } from "hono";
import { canonicalJson, createRequestId, sha256Hex } from "@lastrail/common";
import { parseVersion, readVersion, saveVersion, type VersionBucket } from "./version-store";
import { opsLogSearch } from "./ops-log-search";

const app = new Hono<{ Bindings: Env & { VERSION_BUCKET?: VersionBucket } }>();

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
  preview_json: string | null;
};

type AdminRunFinalResultRow = {
  outcome: "VICTORY" | "FAILURE" | "ABANDONED";
  cleared_waves: number;
  payload_json: string;
  submitted_at: number;
};

type AdminRunRewardEvidenceRow = {
  reward_preview_id: string;
  preview_status: string;
  reward_claim_id: string | null;
  reward_plan_json: string;
};

type AdminRunFirstClearEvidenceRow = {
  operation_key: string;
  risk: string;
  cleared_at: number;
  run_id: string | null;
};

type AdminRunFuelLedgerEvidenceRow = {
  id: string;
  delta: number;
  reason: string;
};

type AdminRunIntegrityEvidenceRow = {
  open_signals: number | null;
  hard_violations: number | null;
  primary_reason: string | null;
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

type AdminSupportAccountRow = {
  account_id: string;
  account_status: "ACTIVE" | "SUSPENDED" | "BANNED" | "DELETED";
  account_created_at: number;
  account_updated_at: number;
  display_name: string | null;
};

type AdminSupportIdentityRow = {
  provider: string;
};

type AdminSupportAuthRow = {
  active_session_count: number | null;
  latest_session_created_at: number | null;
  latest_session_expires_at: number | null;
  last_login_at: number | null;
};

type AdminSupportRunRow = {
  run_id: string;
  status: string;
  started_at: number;
  finished_at: number | null;
  risk: string | null;
  balance_config_version: number;
  preview_json: string | null;
  outcome: string | null;
  cleared_waves: number | null;
  reward_preview_id: string | null;
};

type AdminSupportRunRewardRow = {
  reward_preview_id: string;
  run_id: string;
  preview_status: string;
  reward_claim_id: string | null;
  claimed_at: number | null;
  balance_config_version: number;
  expires_at: number;
  reward_plan_json: string;
};

type AdminSupportLoginBonusClaimRow = {
  bonus_id: string;
  reward_transaction_id: string;
  claimed_at: number;
  reward_json: string;
};

type AdminSupportTicketRow = {
  id: string;
  account_id: string;
  category: string;
  status: string;
  body_text: string;
  body_preview: string;
  body_length: number;
  app_version: string | null;
  app_version_code: string | null;
  client_composed_at: number | null;
  client_submitted_at: number | null;
  server_received_at: number;
  idempotency_key: string;
  request_hash: string;
  created_at: number;
  updated_at: number;
  idempotency_replay_known?: number | null;
};

type AdminSupportGachaBatchRow = {
  batch_id: string;
  banner_id: string;
  pull_count: number;
  cost_item_id: string;
  cost_quantity: number;
  result_summary_json: string;
  created_at: number;
};

type AdminSupportIdempotencyRow = {
  operation: string;
  result_ref: string;
  created_at: number;
};

type AdminSupportIntegrityRow = {
  open_signals: number;
  hard_violations: number;
  latest_detection_at: number | null;
  primary_reason: string | null;
};

type AdminSupportReviewRow = {
  status: string;
  reason: string | null;
  updated_at: number;
};

type AdminOpsErrorLogRow = {
  id: string;
  occurred_at: number;
  environment: "DEV" | "PRD";
  service: string;
  severity: "info" | "warning" | "error" | "critical";
  error_code: string;
  message_summary: string;
  status: "open" | "acknowledged" | "resolved" | "ignored";
  source: string;
  request_id: string | null;
  account_id: string | null;
  run_id: string | null;
  reward_preview_id: string | null;
  reward_claim_id: string | null;
  offline_reward_claim_id: string | null;
  gacha_batch_id: string | null;
  gacha_banner_id: string | null;
  story_progress_id: string | null;
  integrity_event_id: string | null;
  support_ticket_id: string | null;
  idempotency_operation: string | null;
  idempotency_key_hash: string | null;
  correlation_json: string;
  sanitized_context_json: string;
  retryable: number;
  masked: number;
  payload_ref: string | null;
  trace_ref: string | null;
  resolved_at: number | null;
  resolved_by: string | null;
  resolution_note: string | null;
  created_at: number;
};

type AdminDashboardSessionRow = {
  active_session_count: number | null;
  latest_session_created_at: number | null;
};

type AdminDashboardSupportReviewRow = {
  support_review_needed_count: number | null;
};

type AdminDashboardIntegrityRow = {
  integrity_high_risk_count: number | null;
};

type AdminDashboardIssueRow = {
  reward_claim_failure_count: number | null;
  offline_reward_issue_count: number | null;
  gacha_support_issue_count: number | null;
  critical_open_ops_error_count: number | null;
  unacknowledged_core_failure_count: number | null;
};

type AdminDashboardDuplicateReplayRow = {
  duplicate_replay_count: number | null;
};

type AdminDashboardRecoveryRow = {
  account_recovery_candidate_count: number | null;
};

type AdminDashboardStorySummaryRow = {
  accounts_with_story_progress_count: number | null;
  version_mismatch_count: number | null;
};

type AdminDashboardStoryDistributionRow = {
  highest_story_progress_id: string;
  account_count: number | null;
};

type AdminOpsErrorAggregateRow = {
  recent_error_count: number | null;
  warning_count: number | null;
  critical_count: number | null;
  retryable_count: number | null;
  masked_count: number | null;
  open_count: number | null;
  acknowledged_count: number | null;
  latest_critical_at: number | null;
};

type AdminGachaBannerRow = {
  id: string;
  banner_type: string;
  status: string;
  balance_config_version: number;
  pickup_operator_id: string | null;
  starts_at: number;
  ends_at: number | null;
  pity_config_json: string;
  pool_snapshot_json: string;
  created_at: number;
  updated_at: number;
};

type AdminGachaPityRow = {
  account_id: string;
  banner_id: string;
  total_pull_count: number;
  elite_pity_count: number;
  pickup_elite_pity_count: number;
  rare_or_higher_pity_count: number;
  revision: number;
  last_batch_id: string | null;
  updated_at: number;
};

type AdminGachaBatchRow = {
  batch_id: string;
  account_id: string;
  banner_id: string;
  pull_count: number;
  cost_item_id: string;
  cost_quantity: number;
  cost_ledger_id?: string | null;
  pre_pity_json: string;
  post_pity_json: string;
  result_summary_json: string;
  created_at: number;
  idempotency_replay_known: number | null;
};

type AdminGachaResultRow = {
  result_id: string;
  pull_sequence: number;
  result_rarity: string;
  result_type: string;
  reward_id: string;
  is_pickup: number;
  was_duplicate: number;
  guarantee_type: string;
  duplicate_conversion_json: string | null;
  item_ledger_id: string | null;
  progression_ledger_id: string | null;
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
const SUPPORT_READ_PERMISSION = "SUPPORT_READ";
const OPS_ERROR_LOG_READ_PERMISSION = "OPS_ERROR_LOG_READ";
const LAUNCH_LOGIN_BONUS_ID = "launch_login_bonus_act1_v1";
const LAUNCH_LOGIN_BONUS_EXPECTED_REWARD_LINES = [
  { kind: "ITEM", id: "cur_command_ticket", quantity: 5, source: "LAUNCH_LOGIN_BONUS" },
] as const;
const CONFIRMATION_TTL_MS = 5 * 60 * 1000;
const ACT_ONE_NARRATIVE_VERSION = "act1.2026-08-27.001";
const ACT_ONE_NARRATIVE_COVERAGE_VERSION = "act1.2026-08-30.001";
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
const ACT_ONE_NARRATIVE_COVERAGE = [
  { missionId: "opn_01_last_departure", displayName: "마지막 출발" },
  { missionId: "opn_02_first_signal", displayName: "첫 신호" },
  { missionId: "opn_03_water_stop", displayName: "급수 정차" },
  { missionId: "opn_04_scrapstar_show", displayName: "스크랩스타 쇼" },
  { missionId: "opn_05_night_boarders", displayName: "야간 승선전" },
  { missionId: "opn_06_broken_line", displayName: "끊어진 선로" },
  { missionId: "raid_raider_king_engine", displayName: "약탈왕 기관차" },
] as const;

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

function actOneNarrativeCoverageItems() {
  return ACT_ONE_NARRATIVE_COVERAGE.map((item) => ({
    missionId: item.missionId,
    displayName: item.displayName,
    contentStatus: "draft_exists",
    localizationStatus: "localization_pending",
    fixtureStatus: "fixture_pending",
    missingContentStatus: "version_pending",
    narrativeVersion: ACT_ONE_NARRATIVE_COVERAGE_VERSION,
    updatedAt: null,
    source: "narrative_doc_candidate",
  }));
}

function actOneNarrativeCoverageSummary(versionMismatchCount = 0) {
  const items = actOneNarrativeCoverageItems();
  return {
    contentMissingCount: items.filter((item) => item.contentStatus === "content_missing").length,
    localizationPendingCount: items.filter((item) => item.localizationStatus === "localization_pending").length,
    fixturePendingCount: items.filter((item) => item.fixtureStatus === "fixture_pending").length,
    versionMismatchCount,
    latestNarrativeVersion: ACT_ONE_NARRATIVE_COVERAGE_VERSION,
  };
}

function emptyNarrativeCoverageSummary() {
  return {
    contentMissingCount: 0,
    localizationPendingCount: 0,
    fixturePendingCount: 0,
    versionMismatchCount: 0,
    latestNarrativeVersion: null,
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

function supportOfflineRewardFallback(account: AdminSupportAccountRow): AdminOfflineRewardRow {
  return {
    account_id: account.account_id,
    display_name: account.display_name,
    account_status: account.account_status,
    account_created_at: account.account_created_at,
    highest_story_progress_id: null,
    narrative_version: null,
    last_offline_reward_claim_at: null,
    last_story_progress_id: null,
    last_balance_config_version: null,
    last_narrative_version: null,
    last_previewed_at: null,
    revision: null,
    state_updated_at: null,
    last_claim_id: null,
    last_claim_created_at: null,
  };
}

function jsonObjectString(value: JsonValue, key: string): string | null {
  if (!isJsonObject(value)) return null;
  const field = value[key];
  return typeof field === "string" && field.length > 0 ? field : null;
}

function jsonObjectStringArray(value: JsonValue, key: string): string[] {
  if (!isJsonObject(value) || !Array.isArray(value[key])) return [];
  return value[key].filter((item): item is string => typeof item === "string" && item.length > 0);
}

function jsonObjectPositiveInteger(value: JsonValue, key: string): number | null {
  if (!isJsonObject(value)) return null;
  const field = value[key];
  return typeof field === "number" && Number.isInteger(field) && field > 0 ? field : null;
}

function jsonObjectNonNegativeInteger(value: JsonValue, key: string): number | null {
  if (!isJsonObject(value)) return null;
  const field = value[key];
  return typeof field === "number" && Number.isInteger(field) && field >= 0 ? field : null;
}

function jsonObjectBoolean(value: JsonValue, key: string): boolean | null {
  if (!isJsonObject(value)) return null;
  const field = value[key];
  return typeof field === "boolean" ? field : null;
}

function parseRunPreviewMetadata(previewJson: string | null): {
  operationId: string | null;
  actId: string | null;
  regionId: string | null;
  encounterType: "ORDINARY_15_WAVE" | "RAID_BOSS";
  raidId: string | null;
  bossId: string | null;
  bossPhaseRefs: string[];
  allowedOperatorSlots: number | null;
  startAugmentPackageRef: string | null;
  startTacticPackageRef: string | null;
  contentVersion: string | null;
} {
  if (previewJson === null) {
    return {
      operationId: null,
      actId: null,
      regionId: null,
      encounterType: "ORDINARY_15_WAVE",
      raidId: null,
      bossId: null,
      bossPhaseRefs: [],
      allowedOperatorSlots: null,
      startAugmentPackageRef: null,
      startTacticPackageRef: null,
      contentVersion: null,
    };
  }
  try {
    const parsed = parseJsonValue(previewJson);
    const encounterType = isJsonObject(parsed) && parsed.encounterType === "RAID_BOSS" ? "RAID_BOSS" : "ORDINARY_15_WAVE";
    return {
      operationId: jsonObjectString(parsed, "operationId"),
      actId: jsonObjectString(parsed, "actId"),
      regionId: jsonObjectString(parsed, "regionId"),
      encounterType,
      raidId: encounterType === "RAID_BOSS" ? jsonObjectString(parsed, "raidId") : null,
      bossId: encounterType === "RAID_BOSS" ? jsonObjectString(parsed, "bossId") : null,
      bossPhaseRefs: encounterType === "RAID_BOSS" ? jsonObjectStringArray(parsed, "bossPhaseRefs") : [],
      allowedOperatorSlots: jsonObjectPositiveInteger(parsed, "allowedOperatorSlots"),
      startAugmentPackageRef: jsonObjectString(parsed, "startAugmentPackageRef"),
      startTacticPackageRef: jsonObjectString(parsed, "startTacticPackageRef"),
      contentVersion: jsonObjectString(parsed, "contentVersion"),
    };
  } catch {
    return {
      operationId: null,
      actId: null,
      regionId: null,
      encounterType: "ORDINARY_15_WAVE",
      raidId: null,
      bossId: null,
      bossPhaseRefs: [],
      allowedOperatorSlots: null,
      startAugmentPackageRef: null,
      startTacticPackageRef: null,
      contentVersion: null,
    };
  }
}

function parseRewardLineSummary(rewardPlanJson: string): JsonValue[] {
  try {
    const parsed = parseJsonValue(rewardPlanJson);
    if (!isJsonObject(parsed) || !Array.isArray(parsed.rewardLines)) return [];
    return parsed.rewardLines.filter((line) => {
      if (!isJsonObject(line)) return false;
      return typeof line.kind === "string" && typeof line.id === "string" && typeof line.quantity === "number";
    });
  } catch {
    return [];
  }
}

function parseGachaPostPityState(resultSummaryJson: string): JsonValue | null {
  try {
    const parsed = parseJsonValue(resultSummaryJson);
    if (!isJsonObject(parsed)) return null;
    return isJsonValue(parsed.postPityState) ? parsed.postPityState : null;
  } catch {
    return null;
  }
}

function parseGachaGuaranteeTypes(resultSummaryJson: string): string[] {
  try {
    const parsed = parseJsonValue(resultSummaryJson);
    if (!isJsonObject(parsed) || !Array.isArray(parsed.responseResults)) return [];
    return [...new Set(parsed.responseResults
      .map((result) => isJsonObject(result) && typeof result.guaranteeType === "string" ? result.guaranteeType : null)
      .filter((value): value is string => value !== null))];
  } catch {
    return [];
  }
}

function supportLoginBonusSummary(row: AdminSupportLoginBonusClaimRow | null, idempotencyRows: AdminSupportIdempotencyRow[]) {
  return {
    bonusId: LAUNCH_LOGIN_BONUS_ID,
    claimStatus: row === null ? "UNCLAIMED" : "CLAIMED",
    claimedAt: row === null ? null : toIso(row.claimed_at),
    rewardTransactionId: row?.reward_transaction_id ?? null,
    expectedRewardLines: LAUNCH_LOGIN_BONUS_EXPECTED_REWARD_LINES,
    rewardLineSummary: row === null ? [] : parseRewardLineSummary(row.reward_json),
    idempotencyReplayKnown: row === null
      ? false
      : idempotencyRows.some((idem) => idem.operation === "login_bonus_claim" && idem.result_ref === row.reward_transaction_id),
    readOnly: true,
  };
}

function supportExplanationTemplates() {
  return [
    {
      caseType: "offline_reward",
      playerFacingSummary: "오프라인 보상은 최소 누적 시간이 지나야 수령할 수 있습니다. 서버 기록 기준으로 수령 가능 상태를 확인해 주세요.",
      escalationNeeded: false,
    },
    {
      caseType: "duplicate_replay",
      playerFacingSummary: "서버가 이전 요청을 이미 처리한 기록이 있습니다. 앱을 새로고침하거나 계정 상태 동기화를 시도해 주세요.",
      escalationNeeded: false,
    },
    {
      caseType: "integrity_restriction",
      playerFacingSummary: "해당 기록은 운영 검토가 필요한 상태입니다. 상세 확인 후 별도로 안내드리겠습니다.",
      escalationNeeded: true,
    },
  ];
}

function getSupportPrimaryProvider(providers: string[]): string | null {
  if (providers.includes("GOOGLE")) return "GOOGLE";
  if (providers.includes("GUEST")) return "GUEST";
  return providers[0] ?? null;
}

function toNullableIso(ms: number | null | undefined): string | null {
  return ms === null || ms === undefined ? null : toIso(ms);
}

function getLastLoginAgeSeconds(lastLoginAt: number | null | undefined, now: number): number | null {
  if (lastLoginAt === null || lastLoginAt === undefined) return null;
  return Math.max(0, Math.floor((now - lastLoginAt) / 1000));
}

function countValue(value: number | null | undefined): number {
  return typeof value === "number" && Number.isFinite(value) ? value : 0;
}

function parseOptionalJsonObject(json: string): JsonValue {
  try {
    const parsed = parseJsonValue(json);
    return isJsonObject(parsed) ? parsed : {};
  } catch {
    return {};
  }
}

function opsErrorLogLinks(row: AdminOpsErrorLogRow) {
  return {
    accountId: row.account_id,
    runId: row.run_id,
    rewardPreviewId: row.reward_preview_id,
    rewardClaimId: row.reward_claim_id,
    offlineRewardClaimId: row.offline_reward_claim_id,
    gachaBatchId: row.gacha_batch_id,
    gachaBannerId: row.gacha_banner_id,
    integrityEventId: row.integrity_event_id,
    supportTicketId: row.support_ticket_id,
  };
}

function opsErrorLogSummary(row: AdminOpsErrorLogRow) {
  const context = parseOptionalJsonObject(row.sanitized_context_json);
  const serverVersion = context && typeof context === "object" && !Array.isArray(context) && typeof context.serverVersion === "string" ? context.serverVersion : null;
  return {
    id: row.id,
    occurredAt: toIso(row.occurred_at),
    environment: row.environment,
    service: row.service,
    severity: row.severity,
    errorCode: row.error_code,
    messageSummary: row.message_summary,
    status: row.status,
    source: row.source,
    requestId: row.request_id,
    serverVersion,
    accountId: row.account_id,
    runId: row.run_id,
    rewardPreviewId: row.reward_preview_id,
    rewardClaimId: row.reward_claim_id,
    offlineRewardClaimId: row.offline_reward_claim_id,
    gachaBatchId: row.gacha_batch_id,
    gachaBannerId: row.gacha_banner_id,
    storyProgressId: row.story_progress_id,
    integrityEventId: row.integrity_event_id,
    supportTicketId: row.support_ticket_id,
    idempotencyOperation: row.idempotency_operation,
    idempotencyKeyHash: row.idempotency_key_hash,
    retryable: row.retryable === 1,
    masked: row.masked === 1,
    payloadRef: row.payload_ref,
    traceRef: row.trace_ref,
    resolvedAt: row.resolved_at === null ? null : toIso(row.resolved_at),
    resolvedBy: row.resolved_by,
    resolutionNote: row.resolution_note,
    createdAt: toIso(row.created_at),
    links: opsErrorLogLinks(row),
  };
}

function opsErrorLogDetail(row: AdminOpsErrorLogRow) {
  return {
    ...opsErrorLogSummary(row),
    correlation: parseOptionalJsonObject(row.correlation_json),
    sanitizedContext: parseOptionalJsonObject(row.sanitized_context_json),
  };
}

function supportOpsErrorSummary(row: AdminOpsErrorLogRow) {
  return {
    errorLogId: row.id,
    occurredAt: toIso(row.occurred_at),
    environment: row.environment,
    service: row.service,
    severity: row.severity,
    errorCode: row.error_code,
    messageSummary: row.message_summary,
    status: row.status,
    source: row.source,
    requestId: row.request_id,
    accountId: row.account_id,
    runId: row.run_id,
    rewardPreviewId: row.reward_preview_id,
    rewardClaimId: row.reward_claim_id,
    offlineRewardClaimId: row.offline_reward_claim_id,
    gachaBatchId: row.gacha_batch_id,
    gachaBannerId: row.gacha_banner_id,
    storyProgressId: row.story_progress_id,
    integrityEventId: row.integrity_event_id,
    supportTicketId: row.support_ticket_id,
    idempotencyOperation: row.idempotency_operation,
    idempotencyKeyHash: row.idempotency_key_hash,
    retryable: row.retryable === 1,
    masked: row.masked === 1,
    payloadRef: row.payload_ref,
    traceRef: row.trace_ref,
    links: opsErrorLogLinks(row),
  };
}

function supportTicketSummary(row: AdminSupportTicketRow) {
  return {
    ticketId: row.id,
    accountId: row.account_id,
    category: row.category,
    status: row.status,
    bodyPreview: row.body_preview,
    bodyLength: row.body_length,
    detailAvailable: true,
    appVersion: row.app_version,
    appVersionCode: row.app_version_code,
    clientComposedAt: toNullableIso(row.client_composed_at),
    clientSubmittedAt: toNullableIso(row.client_submitted_at),
    serverReceivedAt: toIso(row.server_received_at),
    createdAt: toIso(row.created_at),
    updatedAt: toIso(row.updated_at),
  };
}

function supportTicketDetail(row: AdminSupportTicketRow) {
  return {
    ...supportTicketSummary(row),
    body: row.body_text,
    idempotencyReplayKnown: row.idempotency_replay_known === 1,
  };
}

function parseJsonObjectOrEmpty(json: string): { readonly [key: string]: JsonValue } {
  try {
    const parsed = parseJsonValue(json);
    return isJsonObject(parsed) ? parsed : {};
  } catch {
    return {};
  }
}

function getJsonNumber(value: { readonly [key: string]: JsonValue }, keys: readonly string[], fallback: number): number {
  for (const key of keys) {
    const field = value[key];
    if (typeof field === "number" && Number.isSafeInteger(field) && field > 0) return field;
  }
  return fallback;
}

function parseAdminGachaPityThresholds(pityConfigJson: string) {
  const parsed = parseJsonObjectOrEmpty(pityConfigJson);
  return {
    rareOrHigher: getJsonNumber(parsed, ["rare", "rareOrHigher", "rareOrHigherGuarantee"], 10),
    elite: getJsonNumber(parsed, ["elite", "eliteGuarantee"], 30),
    pickupElite: getJsonNumber(parsed, ["pickup", "pickupElite", "pickupEliteGuarantee"], 60),
  };
}

function parseAdminGachaPoolDisplay(poolSnapshotJson: string): {
  pullCostItemId: string;
  pullCostQuantity: number;
  pullCountOptions: number[];
} {
  const parsed = parseJsonObjectOrEmpty(poolSnapshotJson);
  const cost = isJsonObject(parsed.cost) ? parsed.cost : {};
  const quantity = cost.quantityPerPull ?? cost.quantity;
  const pullCountOptions = Array.isArray(parsed.allowedPullCounts)
    ? parsed.allowedPullCounts
    : Array.isArray(parsed.pullCountOptions)
      ? parsed.pullCountOptions
      : [1, 10];

  return {
    pullCostItemId: typeof cost.itemId === "string" && cost.itemId.length > 0 ? cost.itemId : "cur_command_ticket",
    pullCostQuantity: typeof quantity === "number" && Number.isSafeInteger(quantity) && quantity > 0 ? quantity : 1,
    pullCountOptions: pullCountOptions
      .filter((value): value is number => typeof value === "number" && (value === 1 || value === 10))
      .sort((left, right) => left - right),
  };
}

function adminGachaBannerSummary(row: AdminGachaBannerRow) {
  const pool = parseAdminGachaPoolDisplay(row.pool_snapshot_json);
  return {
    bannerId: row.id,
    bannerType: row.banner_type,
    status: row.status,
    activeBannerId: row.status === "ACTIVE" ? row.id : null,
    pickupOperatorId: row.pickup_operator_id,
    balanceVersion: String(row.balance_config_version),
    pullCostItemId: pool.pullCostItemId,
    pullCostQuantity: pool.pullCostQuantity,
    pullCountOptions: pool.pullCountOptions.length > 0 ? pool.pullCountOptions : [1, 10],
    pityThresholds: parseAdminGachaPityThresholds(row.pity_config_json),
    guaranteeTypes: ["NONE", "RARE_OR_HIGHER_10", "ELITE_30", "PICKUP_ELITE_60"],
    startsAt: toIso(row.starts_at),
    endsAt: row.ends_at === null ? null : toIso(row.ends_at),
    updatedAt: toIso(row.updated_at),
    detailAvailable: true,
  };
}

function adminGachaPitySummary(row: AdminGachaPityRow | null, accountId: string, bannerId: string | null) {
  return {
    accountId,
    bannerId,
    pityState: {
      totalPullCount: row?.total_pull_count ?? 0,
      elitePityCount: row?.elite_pity_count ?? 0,
      pickupElitePityCount: row?.pickup_elite_pity_count ?? 0,
      rareOrHigherPityCount: row?.rare_or_higher_pity_count ?? 0,
      revision: row?.revision ?? 0,
      lastBatchId: row?.last_batch_id ?? null,
      updatedAt: row === null ? null : toIso(row.updated_at),
    },
    detailAvailable: row !== null,
    supportContext: {
      readOnly: true,
      mutationEndpointsEnabled: false,
      source: "player_gacha_pity",
    },
  };
}

function parseGachaResponseResults(resultSummaryJson: string): JsonValue[] {
  const parsed = parseJsonObjectOrEmpty(resultSummaryJson);
  if (!Array.isArray(parsed.responseResults)) return [];
  return parsed.responseResults.filter((value) => isJsonObject(value));
}

function adminGachaBatchSummary(row: AdminGachaBatchRow) {
  return {
    batchId: row.batch_id,
    accountId: row.account_id,
    bannerId: row.banner_id,
    resultStatus: "ACCEPTED",
    idempotencyReplayKnown: row.idempotency_replay_known === 1,
    pullCount: row.pull_count,
    cost: {
      itemId: row.cost_item_id,
      quantity: row.cost_quantity,
      itemLedgerRef: row.cost_ledger_id ?? null,
    },
    prePityState: parseOptionalJsonObject(row.pre_pity_json),
    postPityState: parseOptionalJsonObject(row.post_pity_json),
    responseResults: parseGachaResponseResults(row.result_summary_json),
    guaranteeTypes: parseGachaGuaranteeTypes(row.result_summary_json),
    createdAt: toIso(row.created_at),
  };
}

function parseDuplicateConversion(value: string | null) {
  if (value === null) return null;
  const parsed = parseJsonObjectOrEmpty(value);
  const itemId = typeof parsed.itemId === "string" ? parsed.itemId : null;
  const operatorId = typeof parsed.operatorId === "string" ? parsed.operatorId : null;
  return {
    displayItemId: itemId,
    operatorId,
    persistedTokenPattern: itemId !== null && operatorId !== null ? `${itemId}:${operatorId}` : null,
    quantity: typeof parsed.quantity === "number" && Number.isFinite(parsed.quantity) ? parsed.quantity : null,
  };
}

function adminGachaResultSummary(row: AdminGachaResultRow) {
  return {
    resultId: row.result_id,
    pullSequence: row.pull_sequence,
    resultRarity: row.result_rarity,
    resultType: row.result_type,
    reward: {
      id: row.reward_id,
    },
    isPickup: row.is_pickup === 1,
    wasDuplicate: row.was_duplicate === 1,
    guaranteeType: row.guarantee_type,
    duplicateConversion: parseDuplicateConversion(row.duplicate_conversion_json),
    itemLedgerRef: row.item_ledger_id,
    operatorLedgerRef: row.progression_ledger_id,
  };
}

function parseRunFinalPayloadMetadata(payloadJson: string | null | undefined): {
  selectedAugmentIds: string[];
  selectedTacticIds: string[];
  raidId: string | null;
  bossId: string | null;
  bossDefeated: boolean | null;
  completedPhaseCount: number | null;
  encounterDurationSeconds: number | null;
  staminaRefund: {
    status: string | null;
    resource: string | null;
    reason: string | null;
    policyId: string | null;
    serverOwnedMutation: boolean;
    fuelLedgerReason: string | null;
  } | null;
} {
  if (typeof payloadJson !== "string") {
    return {
      selectedAugmentIds: [],
      selectedTacticIds: [],
      raidId: null,
      bossId: null,
      bossDefeated: null,
      completedPhaseCount: null,
      encounterDurationSeconds: null,
      staminaRefund: null,
    };
  }

  const parsed = parseJsonObjectOrEmpty(payloadJson);
  const raidResult = isJsonObject(parsed.raidResult) ? parsed.raidResult : {};
  const directTacticIds = Array.isArray(parsed.selectedTacticIds)
    ? parsed.selectedTacticIds.filter((value): value is string => typeof value === "string" && value.length > 0)
    : [];
  const selectedTacticIds = directTacticIds.length > 0
    ? directTacticIds
    : Array.isArray(parsed.tacticSelections)
      ? parsed.tacticSelections
          .map((selection) => isJsonObject(selection) ? jsonObjectString(selection, "selectedTacticId") : null)
          .filter((value): value is string => value !== null)
      : [];
  const selectedAugmentIds = Array.isArray(parsed.selectedAugmentIds)
    ? parsed.selectedAugmentIds.filter((value): value is string => typeof value === "string" && value.length > 0)
    : Array.isArray(parsed.tacticSelections)
      ? parsed.tacticSelections
          .map((selection) => isJsonObject(selection) ? jsonObjectString(selection, "selectedAugmentId") : null)
          .filter((value): value is string => value !== null)
      : [];
  const refund = isJsonObject(parsed.staminaRefund) ? parsed.staminaRefund : null;
  const refundStatus = refund === null ? null : jsonObjectString(refund, "status");
  const refundReason = refund === null ? null : jsonObjectString(refund, "reason");

  return {
    selectedAugmentIds,
    selectedTacticIds,
    raidId: jsonObjectString(raidResult, "raidId"),
    bossId: jsonObjectString(raidResult, "bossId"),
    bossDefeated: jsonObjectBoolean(raidResult, "bossDefeated"),
    completedPhaseCount: jsonObjectNonNegativeInteger(raidResult, "completedPhaseCount"),
    encounterDurationSeconds: jsonObjectNonNegativeInteger(raidResult, "encounterDurationSeconds"),
    staminaRefund: refund === null
      ? null
      : {
          status: refundStatus,
          resource: jsonObjectString(refund, "refundType"),
          reason: refundReason,
          policyId: jsonObjectString(refund, "policyRef"),
          serverOwnedMutation: refundStatus === "COMMITTED",
          fuelLedgerReason: refundReason === "STORY_RAID_FAILURE" ? "STORY_RAID_FAILURE_REFUND" : null,
        },
  };
}

function parseRunRewardCalculationMetadata(rewardPlanJson: string | null | undefined): {
  appliedMultiplierLabel: string | null;
  calculationMetadataStatus: "AVAILABLE" | "MISSING";
} {
  if (typeof rewardPlanJson !== "string") {
    return { appliedMultiplierLabel: null, calculationMetadataStatus: "MISSING" };
  }
  const parsed = parseJsonObjectOrEmpty(rewardPlanJson);
  const calculation = isJsonObject(parsed.calculation) ? parsed.calculation : null;
  if (calculation === null) {
    return { appliedMultiplierLabel: null, calculationMetadataStatus: "MISSING" };
  }
  return {
    appliedMultiplierLabel: jsonObjectString(calculation, "appliedMultiplierLabel"),
    calculationMetadataStatus: "AVAILABLE",
  };
}

function validSupportIdentityRows(rows: AdminSupportIdentityRow[]): AdminSupportIdentityRow[] {
  return rows.filter((row): row is AdminSupportIdentityRow =>
    row !== null && row !== undefined && typeof row.provider === "string"
  );
}

function validOpsErrorRows(rows: AdminOpsErrorLogRow[]): AdminOpsErrorLogRow[] {
  return rows.filter((row): row is AdminOpsErrorLogRow =>
    row !== null && row !== undefined && typeof row.id === "string"
  );
}

function validFuelLedgerRows(rows: AdminRunFuelLedgerEvidenceRow[]): AdminRunFuelLedgerEvidenceRow[] {
  return rows.filter((row): row is AdminRunFuelLedgerEvidenceRow =>
    row !== null && row !== undefined && typeof row.id === "string"
  );
}

function rejectAdminDashboardBody(c: AppContext): Response {
  return c.json({
    error: "contract_violation",
    message: "Admin dashboard read endpoints do not accept request bodies.",
  }, 400);
}

function rejectAdminGachaBody(c: AppContext): Response {
  return c.json({
    error: "contract_violation",
    message: "Admin gacha read endpoints do not accept request bodies.",
  }, 400);
}

function rejectAdminPlayerBody(c: AppContext): Response {
  return c.json({
    error: "contract_violation",
    message: "Admin player read endpoints do not accept request bodies.",
  }, 400);
}

function rejectAdminRunBody(c: AppContext): Response {
  return c.json({
    error: "contract_violation",
    message: "Admin run read endpoints do not accept request bodies.",
  }, 400);
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

function rejectAdminSupportBody(c: AppContext): Response {
  return c.json({
    error: "contract_violation",
    message: "Admin support account summary endpoints do not accept request bodies.",
  }, 400);
}

function rejectAdminOpsErrorLogBody(c: AppContext): Response {
  return c.json({
    error: "contract_violation",
    message: "Admin ops error log read endpoints do not accept request bodies.",
  }, 400);
}

function hasAdminPermission(principal: AdminPrincipalRow, permission: string): boolean {
  if (permission === OPS_ERROR_LOG_READ_PERMISSION) {
    return principal.role === "OWNER" || principal.role === "OPERATOR";
  }
  if (permission === SUPPORT_READ_PERMISSION) {
    return principal.role === "OWNER" || principal.role === "OPERATOR" || principal.role === "VIEWER";
  }
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
app.get("/v1/admin/versions", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  c.header("Cache-Control", "no-store");
  if (c.env.ENVIRONMENT !== "DEV") return c.json({ error: { code: "version_environment_disabled" } }, 403);
  if (!c.env.VERSION_BUCKET) return c.json({ error: { code: "version_storage_unavailable" } }, 503);
  try {
    const version = await readVersion(c.env.VERSION_BUCKET);
    return c.json({ environment: "DEV", server: "dev-admin.lastrail.app", canExport: principal.role === "OWNER", version: version?.version ?? null, versionCode: version?.versionCode ?? null, minimumVersionCode: version?.minimumVersionCode ?? null });
  } catch {
    return c.json({ error: { code: "version_storage_read_failed" } }, 502);
  }
});
app.post("/v1/admin/versions/export", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  c.header("Cache-Control", "no-store");
  if (principal.role !== "OWNER" || c.env.ENVIRONMENT !== "DEV") return c.json({ error: { code: "version_export_forbidden" } }, 403);
  const version = parseVersion(await c.req.json().catch(() => null));
  if (!version || !version.minimumVersionCode) return c.json({ error: { code: "invalid_version" } }, 400);
  if (!c.env.VERSION_BUCKET) return c.json({ error: { code: "version_storage_unavailable" } }, 503);
  try {
    return c.json({ accepted: true, ...await saveVersion(c.env.VERSION_BUCKET, version) });
  } catch {
    return c.json({ error: { code: "version_storage_write_failed" } }, 502);
  }
});
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
app.get("/v1/admin/dashboard/launch-summary", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, SUPPORT_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminDashboardBody(c);

  const now = Date.now();
  const sessionSummary = await c.env.DB.prepare(
    `SELECT
       COALESCE(SUM(CASE WHEN status = 'ACTIVE' AND expires_at > ? THEN 1 ELSE 0 END), 0) AS active_session_count,
       MAX(created_at) AS latest_session_created_at
     FROM auth_sessions`,
  )
    .bind(now)
    .first<AdminDashboardSessionRow>();
  const supportReview = await c.env.DB.prepare(
    `SELECT COUNT(*) AS support_review_needed_count
     FROM suspicious_player_reviews
     WHERE status IN ('REVIEWING','ESCALATED','REWARD_HELD','SCORE_EXCLUDED','ACTIONED')`,
  )
    .bind()
    .first<AdminDashboardSupportReviewRow>();
  const integrityRisk = await c.env.DB.prepare(
    `SELECT COUNT(DISTINCT account_id) AS integrity_high_risk_count
     FROM integrity_events
     WHERE classification = 'HARD_VIOLATION'
        OR evidence_code IN ('CHECKSUM_MISMATCH','IMPOSSIBLE_SCORE','IMPOSSIBLE_DAMAGE','BALANCE_VERSION_REWARD_MISMATCH')`,
  )
    .bind()
    .first<AdminDashboardIntegrityRow>();
  const issueSummary = await c.env.DB.prepare(
    `SELECT
       COALESCE(SUM(CASE WHEN error_code IN ('reward_plan_mismatch','reward_multiplier_missing','reward_plan_unavailable','reward_integrity_restricted') THEN 1 ELSE 0 END), 0) AS reward_claim_failure_count,
       COALESCE(SUM(CASE WHEN error_code IN ('offline_reward_state_conflict','offline_reward_claim_conflict','offline_reward_plan_mismatch','offline_reward_policy_missing','offline_reward_policy_invalid') THEN 1 ELSE 0 END), 0) AS offline_reward_issue_count,
       COALESCE(SUM(CASE WHEN error_code IN ('gacha_state_conflict','gacha_pool_unavailable','gacha_temporarily_unavailable') THEN 1 ELSE 0 END), 0) AS gacha_support_issue_count,
       COALESCE(SUM(CASE WHEN severity = 'critical' AND status = 'open' THEN 1 ELSE 0 END), 0) AS critical_open_ops_error_count,
       COALESCE(SUM(CASE WHEN severity IN ('critical','error') AND status = 'open' THEN 1 ELSE 0 END), 0) AS unacknowledged_core_failure_count
     FROM ops_error_logs
     WHERE status IN ('open','acknowledged')`,
  )
    .bind()
    .first<AdminDashboardIssueRow>();
  const duplicateReplay = await c.env.DB.prepare(
    `SELECT COUNT(*) AS duplicate_replay_count
     FROM idempotency_records
     WHERE operation IN ('run_start','run_finish','run_reward_claim','offline_reward_claim','login_bonus_claim','gacha_pull')`,
  )
    .bind()
    .first<AdminDashboardDuplicateReplayRow>();
  const recoveryCandidates = await c.env.DB.prepare(
    `SELECT COUNT(*) AS account_recovery_candidate_count
     FROM accounts a
     WHERE a.status = 'ACTIVE'
       AND EXISTS (
         SELECT 1 FROM auth_identities guest
         WHERE guest.account_id = a.id AND guest.provider = 'GUEST'
       )
       AND NOT EXISTS (
         SELECT 1 FROM auth_identities google
         WHERE google.account_id = a.id AND google.provider = 'GOOGLE'
       )`,
  )
    .bind()
    .first<AdminDashboardRecoveryRow>();
  const storySummary = await c.env.DB.prepare(
    `SELECT
       COUNT(*) AS accounts_with_story_progress_count,
       COALESCE(SUM(CASE WHEN narrative_version IS NOT NULL AND narrative_version != ? THEN 1 ELSE 0 END), 0) AS version_mismatch_count
     FROM account_story_progress`,
  )
    .bind(ACT_ONE_NARRATIVE_COVERAGE_VERSION)
    .first<AdminDashboardStorySummaryRow>();
  const storyDistribution = await c.env.DB.prepare(
    `SELECT highest_story_progress_id, COUNT(*) AS account_count
     FROM account_story_progress
     GROUP BY highest_story_progress_id
     ORDER BY COUNT(*) DESC, highest_story_progress_id
     LIMIT 20`,
  )
    .bind()
    .all<AdminDashboardStoryDistributionRow>();
  const lastLoginAt = sessionSummary?.latest_session_created_at ?? null;
  const narrativeSummary = actOneNarrativeCoverageSummary(countValue(storySummary?.version_mismatch_count));

  return c.json({
    permission: SUPPORT_READ_PERMISSION,
    permissionMode: "ROLE_PERMISSION",
    activeSessions: countValue(sessionSummary?.active_session_count),
    concurrentUsers: null,
    concurrentUsersPrecision: "UNAVAILABLE",
    recentActiveAccounts5m: null,
    recentActiveAccounts30m: null,
    recentActiveAccountsSource: "not_available_without_activity_source",
    lastLoginAtLatest: toNullableIso(lastLoginAt),
    lastLoginAgeBuckets: {
      latestAgeSeconds: getLastLoginAgeSeconds(lastLoginAt, now),
      exactConcurrentUsersAvailable: false,
    },
    supportReviewNeededCount: countValue(supportReview?.support_review_needed_count),
    accountRecoveryCandidateCount: countValue(recoveryCandidates?.account_recovery_candidate_count),
    integrityHighRiskCount: countValue(integrityRisk?.integrity_high_risk_count),
    rewardClaimFailureCount: countValue(issueSummary?.reward_claim_failure_count),
    offlineRewardIssueCount: countValue(issueSummary?.offline_reward_issue_count),
    gachaSupportIssueCount: countValue(issueSummary?.gacha_support_issue_count),
    duplicateReplayCount: countValue(duplicateReplay?.duplicate_replay_count),
    criticalOpenOpsErrorCount: countValue(issueSummary?.critical_open_ops_error_count),
    unacknowledgedCoreFailureCount: countValue(issueSummary?.unacknowledged_core_failure_count),
    accountsWithStoryProgressCount: countValue(storySummary?.accounts_with_story_progress_count),
    highestProgressDistribution: storyDistribution.results.map((row) => ({
      storyProgressId: row.highest_story_progress_id,
      order: storyProgressOrder(row.highest_story_progress_id),
      accountCount: countValue(row.account_count),
    })),
    contentMissingCount: narrativeSummary.contentMissingCount,
    localizationPendingCount: narrativeSummary.localizationPendingCount,
    fixturePendingCount: narrativeSummary.fixturePendingCount,
    versionMismatchCount: narrativeSummary.versionMismatchCount,
    latestNarrativeVersion: narrativeSummary.latestNarrativeVersion,
    serverCalculatedAt: toIso(now),
    supportContext: {
      readOnly: true,
      mutationEndpointsEnabled: false,
      exactConcurrentUsersEnabled: false,
    },
  });
});
app.get("/v1/admin/ops/error-logs", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, OPS_ERROR_LOG_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminOpsErrorLogBody(c);

  const whereClauses: string[] = [];
  const values: Array<string | number> = [];
  try {
    const search = opsLogSearch(c.req.query("category"), c.req.query("q"), c.req.query("serverVersion"));
    whereClauses.push(...search.clauses);
    values.push(...search.values);
  } catch {
    return c.json({ error: { code: "invalid_log_filter" } }, 400);
  }
  const filters = [
    ["environment", "environment"],
    ["service", "service"],
    ["severity", "severity"],
    ["status", "status"],
    ["source", "source"],
    ["errorCode", "error_code"],
    ["requestId", "request_id"],
    ["accountId", "account_id"],
    ["runId", "run_id"],
    ["rewardPreviewId", "reward_preview_id"],
    ["rewardClaimId", "reward_claim_id"],
    ["offlineRewardClaimId", "offline_reward_claim_id"],
    ["gachaBatchId", "gacha_batch_id"],
    ["gachaBannerId", "gacha_banner_id"],
    ["storyProgressId", "story_progress_id"],
    ["integrityEventId", "integrity_event_id"],
    ["supportTicketId", "support_ticket_id"],
  ] as const;
  for (const [queryName, columnName] of filters) {
    const value = c.req.query(queryName)?.trim();
    if (value !== undefined && value !== "") {
      whereClauses.push(`${columnName} = ?`);
      values.push(value);
    }
  }

  const occurredFrom = c.req.query("occurredFrom");
  if (occurredFrom !== undefined) {
    const parsed = Date.parse(occurredFrom);
    if (Number.isFinite(parsed)) {
      whereClauses.push("occurred_at >= ?");
      values.push(parsed);
    }
  }
  const occurredTo = c.req.query("occurredTo");
  if (occurredTo !== undefined) {
    const parsed = Date.parse(occurredTo);
    if (Number.isFinite(parsed)) {
      whereClauses.push("occurred_at <= ?");
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
       id, occurred_at, environment, service, severity, error_code, message_summary,
       status, source, request_id, account_id, run_id, reward_preview_id,
       reward_claim_id, offline_reward_claim_id, gacha_batch_id, gacha_banner_id,
       story_progress_id, integrity_event_id, support_ticket_id,
       idempotency_operation, idempotency_key_hash, correlation_json,
       sanitized_context_json, retryable, masked, payload_ref, trace_ref,
       resolved_at, resolved_by, resolution_note, created_at
     FROM ops_error_logs
     ${whereClauses.length === 0 ? "" : `WHERE ${whereClauses.join(" AND ")}`}
     ORDER BY
       CASE severity
         WHEN 'critical' THEN 0
         WHEN 'error' THEN 1
         WHEN 'warning' THEN 2
         ELSE 3
       END,
       occurred_at DESC,
       id
     LIMIT ? OFFSET ?`,
  )
    .bind(...values)
    .all<AdminOpsErrorLogRow>();

  const visibleRows = rows.results.slice(0, limit);
  return c.json({
    permission: OPS_ERROR_LOG_READ_PERMISSION,
    permissionMode: "ROLE_PERMISSION",
    items: visibleRows.map(opsErrorLogSummary),
    collectionAvailable: c.req.query("category") !== "frontend",
    nextCursor: rows.results.length > limit ? String(offset + limit) : null,
  });
});
app.get("/v1/admin/ops/error-logs/summary", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, OPS_ERROR_LOG_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminOpsErrorLogBody(c);

  const row = await c.env.DB.prepare(
    `SELECT
       COUNT(*) AS recent_error_count,
       COALESCE(SUM(CASE WHEN severity = 'warning' THEN 1 ELSE 0 END), 0) AS warning_count,
       COALESCE(SUM(CASE WHEN severity = 'critical' THEN 1 ELSE 0 END), 0) AS critical_count,
       COALESCE(SUM(CASE WHEN retryable = 1 THEN 1 ELSE 0 END), 0) AS retryable_count,
       COALESCE(SUM(CASE WHEN masked = 1 THEN 1 ELSE 0 END), 0) AS masked_count,
       COALESCE(SUM(CASE WHEN status = 'open' THEN 1 ELSE 0 END), 0) AS open_count,
       COALESCE(SUM(CASE WHEN status = 'acknowledged' THEN 1 ELSE 0 END), 0) AS acknowledged_count,
       MAX(CASE WHEN severity = 'critical' THEN occurred_at ELSE NULL END) AS latest_critical_at
     FROM ops_error_logs`,
  )
    .bind()
    .first<AdminOpsErrorAggregateRow>();

  return c.json({
    permission: OPS_ERROR_LOG_READ_PERMISSION,
    permissionMode: "ROLE_PERMISSION",
    recentErrorCount: countValue(row?.recent_error_count),
    warningCount: countValue(row?.warning_count),
    criticalCount: countValue(row?.critical_count),
    retryableCount: countValue(row?.retryable_count),
    maskedCount: countValue(row?.masked_count),
    openCount: countValue(row?.open_count),
    acknowledgedCount: countValue(row?.acknowledged_count),
    latestCriticalAt: toNullableIso(row?.latest_critical_at),
    serverCalculatedAt: toIso(Date.now()),
    detailAvailable: false,
  });
});
app.get("/v1/admin/ops/error-logs/:errorLogId", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, OPS_ERROR_LOG_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminOpsErrorLogBody(c);

  const row = await c.env.DB.prepare(
    `SELECT
       id, occurred_at, environment, service, severity, error_code, message_summary,
       status, source, request_id, account_id, run_id, reward_preview_id,
       reward_claim_id, offline_reward_claim_id, gacha_batch_id, gacha_banner_id,
       story_progress_id, integrity_event_id, support_ticket_id,
       idempotency_operation, idempotency_key_hash, correlation_json,
       sanitized_context_json, retryable, masked, payload_ref, trace_ref,
       resolved_at, resolved_by, resolution_note, created_at
     FROM ops_error_logs
     WHERE id = ?`,
  )
    .bind(c.req.param("errorLogId"))
    .first<AdminOpsErrorLogRow>();

  if (row === null) {
    return c.json({ error: "ops_error_log_not_found", message: "Ops error log was not found." }, 404);
  }

  return c.json({
    item: opsErrorLogDetail(row),
    detail: null,
  });
});
app.get("/v1/admin/support/tickets", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, SUPPORT_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminSupportBody(c);

  const accountId = c.req.query("accountId")?.trim() || null;
  const category = c.req.query("category")?.trim() || null;
  const status = c.req.query("status")?.trim() || null;
  const limitQuery = c.req.query("limit");
  const parsedLimit = limitQuery === undefined ? 50 : Number(limitQuery);
  const limit = Number.isSafeInteger(parsedLimit) && parsedLimit > 0 ? Math.min(parsedLimit, 50) : 50;
  const cursorQuery = c.req.query("cursor");
  const parsedCursor = cursorQuery === undefined ? 0 : Number(cursorQuery);
  const offset = Number.isSafeInteger(parsedCursor) && parsedCursor >= 0 ? parsedCursor : 0;

  const rows = await c.env.DB.prepare(
    `SELECT id, account_id, category, status, body_text, body_preview, body_length,
            app_version, app_version_code, client_composed_at, client_submitted_at,
            server_received_at, idempotency_key, request_hash, created_at, updated_at
     FROM support_tickets
     WHERE (? IS NULL OR account_id = ?)
       AND (? IS NULL OR category = ?)
       AND (? IS NULL OR status = ?)
     ORDER BY server_received_at DESC, id
     LIMIT ? OFFSET ?`,
  )
    .bind(accountId, accountId, category, category, status, status, limit + 1, offset)
    .all<AdminSupportTicketRow>();
  const visibleRows = rows.results.slice(0, limit);

  return c.json({
    permission: SUPPORT_READ_PERMISSION,
    permissionMode: "ROLE_PERMISSION",
    items: visibleRows.map(supportTicketSummary),
    nextCursor: rows.results.length > limit ? String(offset + limit) : null,
    supportContext: {
      readOnly: true,
      mutationEndpointsEnabled: false,
      repairEndpointsEnabled: false,
      source: "admin_support_tickets",
    },
    errorCode: null,
    message: null,
  });
});
app.get("/v1/admin/support/tickets/:ticketId", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, SUPPORT_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminSupportBody(c);

  const row = await c.env.DB.prepare(
    `SELECT ticket.id, ticket.account_id, ticket.category, ticket.status, ticket.body_text,
            ticket.body_preview, ticket.body_length, ticket.app_version, ticket.app_version_code,
            ticket.client_composed_at, ticket.client_submitted_at, ticket.server_received_at,
            ticket.idempotency_key, ticket.request_hash, ticket.created_at, ticket.updated_at,
            CASE WHEN idem.id IS NULL THEN 0 ELSE 1 END AS idempotency_replay_known
     FROM support_tickets ticket
     LEFT JOIN idempotency_records idem
       ON idem.account_id = ticket.account_id
      AND idem.operation = 'support_ticket_create'
      AND idem.result_ref = ticket.id
     WHERE ticket.id = ?`,
  )
    .bind(c.req.param("ticketId"))
    .first<AdminSupportTicketRow>();

  if (row === null) {
    return c.json({ error: "support_ticket_not_found", message: "Support ticket was not found." }, 404);
  }

  return c.json({
    permission: SUPPORT_READ_PERMISSION,
    permissionMode: "ROLE_PERMISSION",
    ticket: supportTicketDetail(row),
    supportContext: {
      readOnly: true,
      mutationEndpointsEnabled: false,
      repairEndpointsEnabled: false,
      source: "admin_support_ticket_detail",
    },
    errorCode: null,
    message: null,
  });
});
app.get("/v1/admin/support/accounts/:accountId", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, SUPPORT_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminSupportBody(c);

  const accountId = c.req.param("accountId");
  const account = await c.env.DB.prepare(
    `SELECT
       a.id AS account_id,
       a.status AS account_status,
       a.created_at AS account_created_at,
       a.updated_at AS account_updated_at,
       pp.display_name AS display_name
     FROM accounts a
     LEFT JOIN player_profiles pp ON pp.account_id = a.id
     WHERE a.id = ?`,
  )
    .bind(accountId)
    .first<AdminSupportAccountRow>();

  if (account === null) {
    return c.json({ error: "account_not_found", message: "Account was not found." }, 404);
  }

  const identities = await c.env.DB.prepare(
    `SELECT provider
     FROM auth_identities
     WHERE account_id = ?
     ORDER BY provider`,
  )
    .bind(accountId)
    .all<AdminSupportIdentityRow>();
  const authStatus = await c.env.DB.prepare(
    `SELECT
       COALESCE(SUM(CASE WHEN status = 'ACTIVE' AND expires_at > ? THEN 1 ELSE 0 END), 0) AS active_session_count,
       MAX(created_at) AS latest_session_created_at,
       MAX(CASE WHEN status = 'ACTIVE' AND expires_at > ? THEN expires_at ELSE NULL END) AS latest_session_expires_at,
       MAX(created_at) AS last_login_at
     FROM auth_sessions
     WHERE account_id = ?`,
  )
    .bind(Date.now(), Date.now(), accountId)
    .first<AdminSupportAuthRow>();
  const recentRuns = await c.env.DB.prepare(
    `SELECT
       r.id AS run_id,
       r.status AS status,
       r.started_at AS started_at,
       r.finished_at AS finished_at,
       op.risk AS risk,
       r.balance_config_version AS balance_config_version,
       op.preview_json AS preview_json,
       final.outcome AS outcome,
       final.cleared_waves AS cleared_waves,
       preview.id AS reward_preview_id
     FROM runs r
     LEFT JOIN operation_previews op ON op.id = r.preview_id
     LEFT JOIN run_final_results final ON final.run_id = r.id
     LEFT JOIN run_reward_previews preview ON preview.run_id = r.id
     WHERE r.account_id = ?
     ORDER BY r.started_at DESC, r.id
     LIMIT 10`,
  )
    .bind(accountId)
    .all<AdminSupportRunRow>();
  const runRewards = await c.env.DB.prepare(
    `SELECT
       preview.id AS reward_preview_id,
       preview.run_id AS run_id,
       preview.status AS preview_status,
       preview.reward_claim_id AS reward_claim_id,
       preview.claimed_at AS claimed_at,
       preview.balance_config_version AS balance_config_version,
       preview.expires_at AS expires_at,
       preview.reward_plan_json AS reward_plan_json
     FROM run_reward_previews preview
     WHERE preview.account_id = ?
     ORDER BY preview.created_at DESC, preview.id
     LIMIT 10`,
  )
    .bind(accountId)
    .all<AdminSupportRunRewardRow>();
  const offlineRewardRow = await c.env.DB.prepare(
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
  const storyRow = await c.env.DB.prepare(
    `SELECT account_id, act_id, highest_cleared_operation_id, highest_story_progress_id,
            cleared_operations_json, story_flags_json, narrative_version,
            last_progress_run_id, updated_at
     FROM account_story_progress
     WHERE account_id = ?`,
  )
    .bind(accountId)
    .first<AdminStoryProgressRow>();
  const unlockRows = await c.env.DB.prepare(
    `SELECT unlock_id
     FROM account_unlocks
     WHERE account_id = ? AND unlock_id LIKE 'operation:%'
     ORDER BY unlocked_at, unlock_id`,
  )
    .bind(accountId)
    .all<AdminStoryProgressUnlockRow>();
  const gachaBatches = await c.env.DB.prepare(
    `SELECT
       id AS batch_id,
       banner_id AS banner_id,
       pull_count AS pull_count,
       cost_item_id AS cost_item_id,
       cost_quantity AS cost_quantity,
       result_summary_json AS result_summary_json,
       created_at AS created_at
     FROM gacha_pull_batches
     WHERE account_id = ?
     ORDER BY created_at DESC, id
     LIMIT 10`,
  )
    .bind(accountId)
    .all<AdminSupportGachaBatchRow>();
  const idempotencyRows = await c.env.DB.prepare(
    `SELECT operation, result_ref, created_at
     FROM idempotency_records
     WHERE account_id = ?
       AND operation IN ('run_finish','run_reward_claim','offline_reward_claim','login_bonus_claim','gacha_pull')
     ORDER BY created_at DESC, id
     LIMIT 20`,
  )
    .bind(accountId)
    .all<AdminSupportIdempotencyRow>();
  const integrity = await c.env.DB.prepare(
    `SELECT
       COUNT(event.id) AS open_signals,
       COALESCE(SUM(CASE WHEN event.classification = 'HARD_VIOLATION' THEN 1 ELSE 0 END), 0) AS hard_violations,
       MAX(event.created_at) AS latest_detection_at,
       (
         SELECT latest.evidence_code
         FROM integrity_events latest
         WHERE latest.account_id = ?
         ORDER BY latest.created_at DESC, latest.id DESC
         LIMIT 1
       ) AS primary_reason
     FROM integrity_events event
     LEFT JOIN integrity_event_review_states review ON review.event_id = event.id
     WHERE event.account_id = ?
       AND COALESCE(review.state, 'OPEN') != 'RESOLVED'`,
  )
    .bind(accountId, accountId)
    .first<AdminSupportIntegrityRow>();
  const suspiciousReview = await c.env.DB.prepare(
    `SELECT status, reason, updated_at
     FROM suspicious_player_reviews
     WHERE account_id = ?`,
  )
    .bind(accountId)
    .first<AdminSupportReviewRow>();
  const opsErrors = await c.env.DB.prepare(
    `SELECT
       id, occurred_at, environment, service, severity, error_code, message_summary,
       status, source, request_id, account_id, run_id, reward_preview_id,
       reward_claim_id, offline_reward_claim_id, gacha_batch_id, gacha_banner_id,
       story_progress_id, integrity_event_id, support_ticket_id,
       idempotency_operation, idempotency_key_hash, correlation_json,
       sanitized_context_json, retryable, masked, payload_ref, trace_ref,
       resolved_at, resolved_by, resolution_note, created_at
     FROM ops_error_logs
     WHERE account_id = ?
       AND error_code IN (
         'google_auth_config_missing',
         'offline_reward_state_conflict',
         'gacha_state_conflict',
         'story_progress_state_conflict'
       )
       AND status IN ('open','acknowledged')
     ORDER BY
       CASE severity
         WHEN 'critical' THEN 0
         WHEN 'error' THEN 1
         WHEN 'warning' THEN 2
         ELSE 3
       END,
       occurred_at DESC,
       id
     LIMIT 5`,
  )
    .bind(accountId)
    .all<AdminOpsErrorLogRow>();
  const loginBonusClaim = await c.env.DB.prepare(
    `SELECT bonus_id, reward_transaction_id, claimed_at, reward_json
     FROM account_login_bonus_claims
     WHERE account_id = ? AND bonus_id = ?`,
  )
    .bind(accountId, LAUNCH_LOGIN_BONUS_ID)
    .first<AdminSupportLoginBonusClaimRow>();

  const providers = identities.results.map((row) => row.provider);
  const lastLoginAt = authStatus?.last_login_at ?? null;

  return c.json({
    accountId: account.account_id,
    generatedAt: new Date().toISOString(),
    permission: SUPPORT_READ_PERMISSION,
    permissionMode: "ROLE_PERMISSION",
    account: {
      accountId: account.account_id,
      accountStatus: account.account_status,
      displayName: account.display_name,
      createdAt: toIso(account.account_created_at),
      updatedAt: toIso(account.account_updated_at),
    },
    auth: {
      identityProviders: providers,
      providerNames: providers,
      linkedProviders: providers,
      primaryProvider: getSupportPrimaryProvider(providers),
      hasGoogleLink: providers.includes("GOOGLE"),
      activeSessionCount: authStatus?.active_session_count ?? 0,
      latestSessionCreatedAt: toNullableIso(authStatus?.latest_session_created_at),
      latestSessionAt: toNullableIso(authStatus?.latest_session_created_at),
      latestSessionExpiresAt: toNullableIso(authStatus?.latest_session_expires_at),
      latestAuthAt: toNullableIso(lastLoginAt),
      lastLoginAt: toNullableIso(lastLoginAt),
      lastLoginAgeSeconds: getLastLoginAgeSeconds(lastLoginAt, Date.now()),
      lastSeenAt: null,
      lastSeenSource: "not_available_without_activity_source",
      concurrentUsers: null,
      concurrentUsersPrecision: "UNAVAILABLE",
    },
    recentRuns: recentRuns.results.map((run) => {
      const previewMetadata = parseRunPreviewMetadata(run.preview_json);
      return {
        runId: run.run_id,
        status: run.status,
        operationId: previewMetadata.operationId,
        actId: previewMetadata.actId,
        regionId: previewMetadata.regionId,
        encounterType: previewMetadata.encounterType,
        raidId: previewMetadata.raidId,
        bossId: previewMetadata.bossId,
        bossPhaseRefs: previewMetadata.bossPhaseRefs,
        allowedOperatorSlots: previewMetadata.allowedOperatorSlots,
        startAugmentPackageRef: previewMetadata.startAugmentPackageRef,
        startTacticPackageRef: previewMetadata.startTacticPackageRef,
        contentVersion: previewMetadata.contentVersion,
        risk: normalizeRiskTier(run.risk),
        balanceVersion: String(run.balance_config_version),
        startedAt: toIso(run.started_at),
        finishedAt: run.finished_at === null ? null : toIso(run.finished_at),
        outcome: run.outcome,
        clearedWaves: run.cleared_waves,
        rewardPreviewId: run.reward_preview_id,
      };
    }),
    runRewards: runRewards.results.map((reward) => ({
      rewardPreviewId: reward.reward_preview_id,
      runId: reward.run_id,
      previewStatus: reward.preview_status,
      claimability: reward.preview_status === "CLAIMED" ? "ALREADY_CLAIMED" : reward.preview_status,
      rewardClaimId: reward.reward_claim_id,
      claimedAt: reward.claimed_at === null ? null : toIso(reward.claimed_at),
      balanceVersion: String(reward.balance_config_version),
      expiresAt: toIso(reward.expires_at),
      rewardLineSummary: parseRewardLineSummary(reward.reward_plan_json),
      idempotencyReplayKnown: idempotencyRows.results.some((row) => row.operation === "run_reward_claim" && row.result_ref === reward.reward_claim_id),
    })),
    offlineRewards: offlineRewardSummary(offlineRewardRow ?? supportOfflineRewardFallback(account)),
    storyProgress: storyRow === null ? defaultStoryProgressDetail(accountId) : storyProgressDetail(storyRow, unlockRows.results),
    gacha: {
      recentBatches: gachaBatches.results.map((batch) => ({
        batchId: batch.batch_id,
        bannerId: batch.banner_id,
        pullCount: batch.pull_count,
        cost: {
          itemId: batch.cost_item_id,
          quantity: batch.cost_quantity,
        },
        guaranteeTypes: parseGachaGuaranteeTypes(batch.result_summary_json),
        postPityState: parseGachaPostPityState(batch.result_summary_json),
        createdAt: toIso(batch.created_at),
      })),
    },
    loginBonus: supportLoginBonusSummary(loginBonusClaim, idempotencyRows.results),
    idempotency: idempotencyRows.results.map((row) => ({
      operation: row.operation,
      resultRef: row.result_ref,
      createdAt: toIso(row.created_at),
      replaySafe: true,
      linkedResourceType: row.operation === "gacha_pull"
        ? "gacha_batch"
        : row.operation === "login_bonus_claim"
          ? "login_bonus_claim"
        : row.operation === "offline_reward_claim" || row.operation === "run_reward_claim"
          ? "reward_transaction"
          : "reward_preview",
    })),
    integrity: {
      openSignals: integrity?.open_signals ?? 0,
      hardViolations: integrity?.hard_violations ?? 0,
      latestDetectionAt: integrity?.latest_detection_at === null || integrity?.latest_detection_at === undefined ? null : toIso(integrity.latest_detection_at),
      accountReviewStatus: suspiciousReview === null ? "NEW" : suspiciousReviewStatus(suspiciousReview.status),
      reviewReason: suspiciousReview?.reason ?? null,
      reviewUpdatedAt: suspiciousReview === null ? null : toIso(suspiciousReview.updated_at),
      primaryReason: integrity?.primary_reason ?? null,
    },
    opsErrors: opsErrors.results.map(supportOpsErrorSummary),
    supportNotes: supportExplanationTemplates(),
    supportContext: {
      readOnly: true,
      mutationEndpointsEnabled: false,
      repairEndpointsEnabled: false,
      source: "support_account_summary",
    },
    errorCode: null,
    message: null,
  });
});
app.get("/v1/admin/gacha/banners", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, SUPPORT_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminGachaBody(c);

  const status = c.req.query("status")?.trim() || null;
  const rows = await c.env.DB.prepare(
    `SELECT
       id, banner_type, status, balance_config_version, pickup_operator_id,
       starts_at, ends_at, pity_config_json, pool_snapshot_json, created_at, updated_at
     FROM gacha_banners
     WHERE (? IS NULL OR status = ?)
     ORDER BY
       CASE status WHEN 'ACTIVE' THEN 0 ELSE 1 END,
       updated_at DESC,
       id
     LIMIT 50`,
  )
    .bind(status, status)
    .all<AdminGachaBannerRow>();
  const items = rows.results.map(adminGachaBannerSummary);
  return c.json({
    permission: SUPPORT_READ_PERMISSION,
    permissionMode: "ROLE_PERMISSION",
    activeBannerId: items.find((item) => item.status === "ACTIVE")?.bannerId ?? null,
    items,
    nextCursor: null,
  });
});
app.get("/v1/admin/gacha/accounts/:accountId/pity", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, SUPPORT_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminGachaBody(c);

  const accountId = c.req.param("accountId");
  const bannerId = c.req.query("bannerId")?.trim() || null;
  const row = await c.env.DB.prepare(
    `SELECT
       account_id, banner_id, total_pull_count, elite_pity_count,
       pickup_elite_pity_count, rare_or_higher_pity_count,
       revision, last_batch_id, updated_at
     FROM player_gacha_pity
     WHERE account_id = ?
       AND (? IS NULL OR banner_id = ?)
     ORDER BY updated_at DESC, banner_id
     LIMIT 1`,
  )
    .bind(accountId, bannerId, bannerId)
    .first<AdminGachaPityRow>();

  return c.json({
    permission: SUPPORT_READ_PERMISSION,
    permissionMode: "ROLE_PERMISSION",
    ...adminGachaPitySummary(row, accountId, bannerId ?? row?.banner_id ?? null),
  });
});
app.get("/v1/admin/gacha/pull-batches", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, SUPPORT_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminGachaBody(c);

  const accountId = c.req.query("accountId")?.trim() || null;
  const bannerId = c.req.query("bannerId")?.trim() || null;
  const limitQuery = c.req.query("limit");
  const parsedLimit = limitQuery === undefined ? 50 : Number(limitQuery);
  const limit = Number.isSafeInteger(parsedLimit) && parsedLimit > 0 ? Math.min(parsedLimit, 50) : 50;
  const cursorQuery = c.req.query("cursor");
  const parsedCursor = cursorQuery === undefined ? 0 : Number(cursorQuery);
  const offset = Number.isSafeInteger(parsedCursor) && parsedCursor >= 0 ? parsedCursor : 0;

  const rows = await c.env.DB.prepare(
    `SELECT
       batch.id AS batch_id,
       batch.account_id AS account_id,
       batch.banner_id AS banner_id,
       batch.pull_count AS pull_count,
       batch.cost_item_id AS cost_item_id,
       batch.cost_quantity AS cost_quantity,
       batch.cost_ledger_id AS cost_ledger_id,
       batch.pre_pity_json AS pre_pity_json,
       batch.post_pity_json AS post_pity_json,
       batch.result_summary_json AS result_summary_json,
       batch.created_at AS created_at,
       CASE WHEN idem.id IS NULL THEN 0 ELSE 1 END AS idempotency_replay_known
     FROM gacha_pull_batches batch
     LEFT JOIN idempotency_records idem
       ON idem.account_id = batch.account_id
      AND idem.operation = 'gacha_pull'
      AND idem.result_ref = batch.id
     WHERE (? IS NULL OR batch.account_id = ?)
       AND (? IS NULL OR batch.banner_id = ?)
     ORDER BY batch.created_at DESC, batch.id
     LIMIT ? OFFSET ?`,
  )
    .bind(accountId, accountId, bannerId, bannerId, limit + 1, offset)
    .all<AdminGachaBatchRow>();

  const visibleRows = rows.results.slice(0, limit);
  return c.json({
    permission: SUPPORT_READ_PERMISSION,
    permissionMode: "ROLE_PERMISSION",
    items: visibleRows.map(adminGachaBatchSummary),
    nextCursor: rows.results.length > limit ? String(offset + limit) : null,
  });
});
app.get("/v1/admin/gacha/pull-batches/:batchId", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, SUPPORT_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminGachaBody(c);

  const batchId = c.req.param("batchId");
  const batch = await c.env.DB.prepare(
    `SELECT
       batch.id AS batch_id,
       batch.account_id AS account_id,
       batch.banner_id AS banner_id,
       batch.pull_count AS pull_count,
       batch.cost_item_id AS cost_item_id,
       batch.cost_quantity AS cost_quantity,
       batch.cost_ledger_id AS cost_ledger_id,
       batch.pre_pity_json AS pre_pity_json,
       batch.post_pity_json AS post_pity_json,
       batch.result_summary_json AS result_summary_json,
       batch.created_at AS created_at,
       CASE WHEN idem.id IS NULL THEN 0 ELSE 1 END AS idempotency_replay_known
     FROM gacha_pull_batches batch
     LEFT JOIN idempotency_records idem
       ON idem.account_id = batch.account_id
      AND idem.operation = 'gacha_pull'
      AND idem.result_ref = batch.id
     WHERE batch.id = ?`,
  )
    .bind(batchId)
    .first<AdminGachaBatchRow>();

  if (batch === null) {
    return c.json({ error: "gacha_batch_not_found", message: "Gacha pull batch was not found." }, 404);
  }

  const results = await c.env.DB.prepare(
    `SELECT
       id AS result_id,
       pull_sequence AS pull_sequence,
       result_rarity AS result_rarity,
       result_type AS result_type,
       result_id AS reward_id,
       is_pickup AS is_pickup,
       was_duplicate AS was_duplicate,
       guarantee_type AS guarantee_type,
       duplicate_conversion_json AS duplicate_conversion_json,
       item_ledger_id AS item_ledger_id,
       progression_ledger_id AS progression_ledger_id
     FROM gacha_pull_results
     WHERE batch_id = ?
     ORDER BY pull_sequence ASC`,
  )
    .bind(batchId)
    .all<AdminGachaResultRow>();
  const itemLedgerRefs = [
    batch.cost_ledger_id ?? null,
    ...results.results.map((result) => result.item_ledger_id),
  ].filter((value): value is string => value !== null);
  const operatorLedgerRefs = results.results
    .map((result) => result.progression_ledger_id)
    .filter((value): value is string => value !== null);

  return c.json({
    item: {
      ...adminGachaBatchSummary(batch),
      itemLedgerRefs,
      operatorLedgerRefs,
      results: results.results.map(adminGachaResultSummary),
    },
    detailAvailable: true,
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
app.get("/v1/admin/narrative/coverage", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, STORY_PROGRESS_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminStoryProgressBody(c);

  const actId = c.req.query("actId")?.trim() || "act_01_wasteland_railway";
  if (actId !== "act_01_wasteland_railway") {
    return c.json({
      permission: STORY_PROGRESS_READ_PERMISSION,
      permissionMode: "ROLE_PERMISSION",
      actId,
      narrativeVersion: null,
      items: [],
      summary: emptyNarrativeCoverageSummary(),
      supportContext: {
        readOnly: true,
        mutationEndpointsEnabled: false,
        narrativeEditingEnabled: false,
        localizationPublishingEnabled: false,
        source: "narrative_coverage_static_candidates",
      },
    });
  }

  return c.json({
    permission: STORY_PROGRESS_READ_PERMISSION,
    permissionMode: "ROLE_PERMISSION",
    actId,
    narrativeVersion: ACT_ONE_NARRATIVE_COVERAGE_VERSION,
    items: actOneNarrativeCoverageItems(),
    summary: actOneNarrativeCoverageSummary(),
    supportContext: {
      readOnly: true,
      mutationEndpointsEnabled: false,
      narrativeEditingEnabled: false,
      localizationPublishingEnabled: false,
      source: "narrative_coverage_static_candidates",
    },
  });
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
  if (hasRequestBody(c)) return rejectAdminPlayerBody(c);

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
  const identities = await c.env.DB.prepare(
    `SELECT provider
     FROM auth_identities
     WHERE account_id = ?
     ORDER BY provider`,
  )
    .bind(accountId)
    .all<AdminSupportIdentityRow>();
  const now = Date.now();
  const authStatus = await c.env.DB.prepare(
    `SELECT
       COALESCE(SUM(CASE WHEN status = 'ACTIVE' AND expires_at > ? THEN 1 ELSE 0 END), 0) AS active_session_count,
       MAX(created_at) AS latest_session_created_at,
       MAX(CASE WHEN status = 'ACTIVE' AND expires_at > ? THEN expires_at ELSE NULL END) AS latest_session_expires_at,
       MAX(created_at) AS last_login_at
     FROM auth_sessions
     WHERE account_id = ?`,
  )
    .bind(now, now, accountId)
    .first<AdminSupportAuthRow>();
  const opsErrors = await c.env.DB.prepare(
    `SELECT
       id, occurred_at, environment, service, severity, error_code, message_summary,
       status, source, request_id, account_id, run_id, reward_preview_id,
       reward_claim_id, offline_reward_claim_id, gacha_batch_id, gacha_banner_id,
       story_progress_id, integrity_event_id, support_ticket_id,
       idempotency_operation, idempotency_key_hash, correlation_json,
       sanitized_context_json, retryable, masked, payload_ref, trace_ref,
       resolved_at, resolved_by, resolution_note, created_at
     FROM ops_error_logs
     WHERE account_id = ?
       AND error_code IN (
         'google_auth_config_missing',
         'offline_reward_state_conflict',
         'gacha_state_conflict',
         'story_progress_state_conflict'
       )
       AND status IN ('open','acknowledged')
     ORDER BY
       CASE severity
         WHEN 'critical' THEN 0
         WHEN 'error' THEN 1
         WHEN 'warning' THEN 2
         ELSE 3
       END,
       occurred_at DESC,
       id
     LIMIT 5`,
  )
    .bind(accountId)
    .all<AdminOpsErrorLogRow>();

  const coreRows = core.results;
  const syncLevel = syncLevelFrom(coreRows);
  const coreByOperator = new Map(coreRows.map((row) => [row.operator_id, row]));
  const providers = validSupportIdentityRows(identities.results).map((row) => row.provider);
  const lastLoginAt = authStatus?.last_login_at ?? null;

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
    auth: {
      identityProviders: providers,
      providerNames: providers,
      linkedProviders: providers,
      primaryProvider: getSupportPrimaryProvider(providers),
      hasGoogleLink: providers.includes("GOOGLE"),
      activeSessionCount: authStatus?.active_session_count ?? 0,
      latestSessionCreatedAt: toNullableIso(authStatus?.latest_session_created_at),
      latestSessionAt: toNullableIso(authStatus?.latest_session_created_at),
      latestSessionExpiresAt: toNullableIso(authStatus?.latest_session_expires_at),
      latestAuthAt: toNullableIso(lastLoginAt),
      lastLoginAt: toNullableIso(lastLoginAt),
      lastLoginAgeSeconds: getLastLoginAgeSeconds(lastLoginAt, now),
      lastSeenAt: null,
      lastSeenSource: "not_available_without_activity_source",
      concurrentUsers: null,
      concurrentUsersPrecision: "UNAVAILABLE",
    },
    opsErrors: validOpsErrorRows(opsErrors.results).map(supportOpsErrorSummary),
    supportContext: {
      readOnly: true,
      mutationEndpointsEnabled: false,
      repairEndpointsEnabled: false,
      source: "admin_player_detail",
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
  if (hasRequestBody(c)) return rejectAdminRunBody(c);

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
       rs.snapshot_json AS snapshot_json,
       op.preview_json AS preview_json
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

  const finalResult = await c.env.DB.prepare(
    `SELECT outcome, cleared_waves, payload_json, submitted_at
     FROM run_final_results
     WHERE run_id = ?`,
  )
    .bind(row.run_id)
    .first<AdminRunFinalResultRow>();
  const rewardPreview = await c.env.DB.prepare(
    `SELECT id AS reward_preview_id,
            status AS preview_status,
            reward_claim_id AS reward_claim_id,
            reward_plan_json AS reward_plan_json
     FROM run_reward_previews
     WHERE run_id = ?`,
  )
    .bind(row.run_id)
    .first<AdminRunRewardEvidenceRow>();
  const previewMetadata = parseRunPreviewMetadata(row.preview_json ?? null);
  const firstClear = previewMetadata.operationId === null
    ? null
    : await c.env.DB.prepare(
        `SELECT operation_key, risk, cleared_at, run_id
         FROM account_first_clears
         WHERE account_id = ? AND operation_key = ? AND risk = ?`,
      )
        .bind(row.account_id, previewMetadata.operationId, normalizeRiskTier(row.risk))
        .first<AdminRunFirstClearEvidenceRow>();
  const finalMetadata = parseRunFinalPayloadMetadata(finalResult?.payload_json);
  const fuelLedgerRows = finalMetadata.staminaRefund === null
    ? { results: [] as AdminRunFuelLedgerEvidenceRow[] }
    : await c.env.DB.prepare(
        `SELECT id, delta, reason
         FROM fuel_ledger
         WHERE account_id = ? AND reason = 'STORY_RAID_FAILURE_REFUND'
         ORDER BY created_at DESC, id
         LIMIT 5`,
      )
        .bind(row.account_id)
        .all<AdminRunFuelLedgerEvidenceRow>();
  const integrity = await c.env.DB.prepare(
    `SELECT
       COUNT(event.id) AS open_signals,
       COALESCE(SUM(CASE WHEN event.classification = 'HARD_VIOLATION' THEN 1 ELSE 0 END), 0) AS hard_violations,
       (
         SELECT latest.evidence_code
         FROM integrity_events latest
         WHERE latest.run_id = ?
         ORDER BY latest.created_at DESC, latest.id DESC
         LIMIT 1
       ) AS primary_reason
     FROM integrity_events event
     LEFT JOIN integrity_event_review_states review ON review.event_id = event.id
     WHERE event.run_id = ?
       AND COALESCE(review.state, 'OPEN') != 'RESOLVED'`,
  )
    .bind(row.run_id, row.run_id)
    .first<AdminRunIntegrityEvidenceRow>();
  const rewardCalculation = parseRunRewardCalculationMetadata(rewardPreview?.reward_plan_json);
  const fuelLedgerRefs = validFuelLedgerRows(fuelLedgerRows.results).map((ledger) => ledger.id);
  const staminaRefund = finalMetadata.staminaRefund === null
    ? null
    : {
        ...finalMetadata.staminaRefund,
        fuelLedgerRefs,
      };
  const openSignals = integrity?.open_signals ?? 0;
  const hardViolations = integrity?.hard_violations ?? 0;

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
    operationId: previewMetadata.operationId,
    outcome: finalResult?.outcome ?? null,
    clearedWaves: finalResult?.cleared_waves ?? null,
    rewardPreviewId: rewardPreview?.reward_preview_id ?? null,
    rewardClaimId: rewardPreview?.reward_claim_id ?? null,
    appliedMultiplierLabel: rewardCalculation.appliedMultiplierLabel,
    calculationMetadataStatus: rewardCalculation.calculationMetadataStatus,
    firstClearConsumed: firstClear !== null && firstClear !== undefined,
    integrityDisposition: {
      status: hardViolations > 0 ? "HIGH_RISK" : openSignals > 0 ? "NEEDS_REVIEW" : "CLEAR",
      openSignals,
      hardViolations,
      primaryReason: integrity?.primary_reason ?? null,
    },
    encounterType: previewMetadata.encounterType,
    raidId: finalMetadata.raidId ?? previewMetadata.raidId,
    bossId: finalMetadata.bossId ?? previewMetadata.bossId,
    bossPhaseRefs: previewMetadata.bossPhaseRefs,
    bossDefeated: finalMetadata.bossDefeated,
    completedPhaseCount: finalMetadata.completedPhaseCount,
    encounterDurationSeconds: finalMetadata.encounterDurationSeconds,
    allowedOperatorSlots: previewMetadata.allowedOperatorSlots,
    startAugmentPackageRef: previewMetadata.startAugmentPackageRef,
    startTacticPackageRef: previewMetadata.startTacticPackageRef,
    selectedAugmentIds: finalMetadata.selectedAugmentIds,
    selectedTacticIds: finalMetadata.selectedTacticIds,
    staminaRefund,
    supportContext: {
      readOnly: true,
      mutationEndpointsEnabled: false,
      repairEndpointsEnabled: false,
      source: "admin_run_detail",
    },
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


