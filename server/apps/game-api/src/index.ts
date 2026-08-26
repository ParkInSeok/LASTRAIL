import { Hono } from "hono";
import type { Context } from "hono";
import { createRequestId } from "@lastrail/common";
import { createAccessToken, hashRefreshToken, verifyAccessToken } from "./auth";

const app = new Hono<{ Bindings: Env }>();

type GameApiContext = Context<{ Bindings: Env }>;

type GameApiEnv = Env & {
  AUTH_SIGNING_KEY_CURRENT?: string;
};

const apiBaseUrls = {
  LOCAL: "http://localhost:8787",
  DEV: "https://dev-api.lastrail.app",
  PRD: "https://api.lastrail.app",
} as const;

type BalanceConfigRow = {
  version: number;
  schema_version: number;
  required_content_version: number;
  checksum: string;
  config_json: string;
  published_at: number;
};

type RewardPreviewRow = {
  id: string;
  account_id: string;
  run_id: string;
  status: "ACTIVE" | "CLAIMED" | "EXPIRED" | "INVALIDATED";
  balance_config_version: number;
  reward_plan_json: string;
  expires_at: number;
  created_at: number;
  claimed_at: number | null;
  reward_claim_id: string | null;
};

type CurrencyRewardLine = {
  kind: "CURRENCY";
  id: string;
  quantity: number;
  source?: string;
  adBonusEligible?: boolean;
};

type AccountMutationSequenceRow = {
  sequence: number | null;
};

type AccountCurrencyRow = {
  balance: number;
};

type IdempotencyRecordRow = {
  result_ref: string;
  created_at: number;
  reward_created_at: number | null;
};

type GachaBannerRow = {
  id: string;
  banner_type: "STANDARD" | "PICKUP";
  status: "DRAFT" | "ACTIVE" | "ENDED" | "DISABLED";
  balance_config_version: number;
  pickup_operator_id: string | null;
  starts_at: number;
  ends_at: number | null;
  pity_config_json: string;
  pool_snapshot_json: string;
};

type AuthSessionAccountRow = {
  account_id: string;
  session_id: string;
  session_status: "ACTIVE" | "ROTATED" | "REUSED" | "REVOKED" | "EXPIRED";
  session_expires_at: number;
  account_status: "ACTIVE" | "SUSPENDED" | "BANNED" | "DELETED";
};

type RefreshSessionAccountRow = AuthSessionAccountRow & {
  refresh_token_hash: string;
  family_id: string;
  generation: number;
};

type AuthContext = {
  accountId: string;
  sessionId: string;
};

type GachaPityState = {
  totalPullCount: number;
  elitePityCount: number;
  pickupElitePityCount: number;
  rareOrHigherPityCount: number;
  revision: number;
};

type GachaPityRow = {
  total_pull_count: number;
  elite_pity_count: number;
  pickup_elite_pity_count: number;
  rare_or_higher_pity_count: number;
  revision: number;
};

type GachaPityConfig = {
  elite: number;
  pickup: number;
  rare: number;
};

type GachaPoolResult = {
  resultRarity: "COMMON" | "RARE" | "ELITE";
  resultType: "ELITE_OPERATOR" | "RARE_OPERATOR" | "ELITE_OPERATOR_SHARD" | "RARE_OPERATOR_SHARD" | "OPERATOR_TOKEN" | "TRAINING_MATERIAL" | "WORK_SKILL_MATERIAL";
  resultId: string;
  weight?: number;
  isPickup?: boolean;
  quantity?: number;
};

type GachaCommittedResult = {
  id: string;
  pullSequence: number;
  resultRarity: GachaPoolResult["resultRarity"];
  resultType: GachaPoolResult["resultType"];
  resultId: string;
  reward: Record<string, unknown>;
  isPickup: boolean;
  guaranteeType: "NONE" | "RARE_OR_HIGHER_10" | "ELITE_30" | "PICKUP_ELITE_60";
  wasDuplicate: boolean;
  duplicateConversion: Record<string, unknown> | null;
  operatorId: string | null;
  itemLedgerId: string | null;
  progressionLedgerId: string | null;
};

type AccountItemRow = {
  quantity: number;
};

type ExistingOperatorRow = {
  operator_id: string;
};

type GachaGuaranteeType = GachaCommittedResult["guaranteeType"];

type GachaReplayBatchRow = {
  id: string;
  banner_id: string;
  balance_config_version: number;
  pull_count: number;
  cost_item_id: string;
  cost_quantity: number;
  post_pity_json: string;
  result_summary_json: string;
  created_at: number;
};

type GachaReplayResultRow = {
  pull_sequence: number;
  result_rarity: GachaPoolResult["resultRarity"];
  result_type: GachaPoolResult["resultType"];
  result_id: string;
  is_pickup: number;
  guarantee_type: GachaCommittedResult["guaranteeType"];
  was_duplicate: number;
  duplicate_conversion_json: string | null;
};

type AuthFailureStatus = 401 | 403 | 500;

type AuthFailure = {
  status: AuthFailureStatus;
  errorCode: string;
  message: string;
};

const currencyIdToD1Currency = {
  cur_credits: "CREDITS",
  cur_machine_parts: "MACHINE_PARTS",
  cur_tactical_data: "TACTICAL_DATA",
  cur_special_core: "SPECIAL_CORE",
  cur_operator_exp: "OPERATOR_EXP",
} as const;

function parsePositiveInteger(value: string): number | null {
  if (!/^[1-9]\d*$/u.test(value)) return null;
  const parsed = Number(value);
  return Number.isSafeInteger(parsed) ? parsed : null;
}

function toIso(ms: number): string {
  return new Date(ms).toISOString();
}

function createDomainId(prefix: string): string {
  return `${prefix}_${createRequestId().slice(4)}`;
}

function createRefreshToken(): string {
  return `refresh_${createRequestId().slice(4)}${createRequestId().slice(4)}`;
}

function getRewardLines(rewardPlanJson: string): unknown[] {
  const parsed = JSON.parse(rewardPlanJson) as { rewardLines?: unknown };
  return Array.isArray(parsed.rewardLines) ? parsed.rewardLines : [];
}

function getCurrencyRewardLines(rewardPlanJson: string): CurrencyRewardLine[] {
  return getRewardLines(rewardPlanJson).filter((line): line is CurrencyRewardLine => {
    if (typeof line !== "object" || line === null) return false;
    const candidate = line as Record<string, unknown>;
    return candidate.kind === "CURRENCY"
      && typeof candidate.id === "string"
      && Object.hasOwn(currencyIdToD1Currency, candidate.id)
      && typeof candidate.quantity === "number"
      && Number.isSafeInteger(candidate.quantity)
      && candidate.quantity > 0;
  });
}

function getClaimability(row: RewardPreviewRow): "CLAIMABLE" | "ALREADY_CLAIMED" | "EXPIRED" | "INVALIDATED" {
  if (row.status === "CLAIMED") return "ALREADY_CLAIMED";
  if (row.status === "EXPIRED") return "EXPIRED";
  if (row.status === "INVALIDATED") return "INVALIDATED";
  if (row.expires_at <= Date.now()) return "EXPIRED";
  return "CLAIMABLE";
}

function createGachaRejectedResponse(options: {
  bannerId: string;
  pullCount: number | null;
  errorCode: string;
  message: string;
}) {
  return {
    accepted: false,
    resultStatus: "REJECTED",
    batchId: null,
    bannerId: options.bannerId,
    pullCount: options.pullCount,
    cost: options.pullCount === 1 || options.pullCount === 10
      ? { itemId: "cur_command_ticket", quantity: options.pullCount }
      : null,
    results: [],
    postPityState: null,
    createdAt: null,
    errorCode: options.errorCode,
    message: options.message,
  };
}

function createGachaAcceptedResponse(options: {
  batchId: string;
  bannerId: string;
  balanceVersion: string;
  pullCount: 1 | 10;
  results: Array<Omit<GachaCommittedResult, "id" | "operatorId" | "itemLedgerId" | "progressionLedgerId">>;
  postPityState: GachaPityState;
  createdAt: number;
}) {
  return {
    accepted: true,
    resultStatus: "ACCEPTED",
    batchId: options.batchId,
    bannerId: options.bannerId,
    balanceVersion: options.balanceVersion,
    pullCount: options.pullCount,
    cost: {
      itemId: "cur_command_ticket",
      quantity: options.pullCount,
    },
    results: options.results,
    postPityState: options.postPityState,
    createdAt: toIso(options.createdAt),
    errorCode: null,
    message: null,
  };
}

function createGachaDuplicateReplayResponse(options: {
  batchId: string;
  bannerId: string;
  balanceVersion: string;
  pullCount: 1 | 10;
  results: Array<Omit<GachaCommittedResult, "id" | "operatorId" | "itemLedgerId" | "progressionLedgerId">>;
  postPityState: GachaPityState;
  createdAt: number;
}) {
  return {
    accepted: false,
    resultStatus: "DUPLICATE_REPLAY",
    batchId: options.batchId,
    bannerId: options.bannerId,
    balanceVersion: options.balanceVersion,
    pullCount: options.pullCount,
    cost: {
      itemId: "cur_command_ticket",
      quantity: options.pullCount,
    },
    results: options.results,
    postPityState: options.postPityState,
    createdAt: toIso(options.createdAt),
    errorCode: "duplicate_replay",
    message: "Gacha pull was already committed.",
  };
}

function isActiveGachaBanner(row: GachaBannerRow, now: number): boolean {
  return row.status === "ACTIVE" && row.starts_at <= now && (row.ends_at === null || row.ends_at > now);
}

function parseGachaPoolResults(poolSnapshotJson: string): GachaPoolResult[] {
  const parsed = JSON.parse(poolSnapshotJson) as { results?: unknown };
  if (!Array.isArray(parsed.results)) return [];

  return parsed.results.filter((result): result is GachaPoolResult => {
    if (typeof result !== "object" || result === null) return false;
    const candidate = result as Record<string, unknown>;
    return (candidate.resultRarity === "COMMON" || candidate.resultRarity === "RARE" || candidate.resultRarity === "ELITE")
      && (candidate.resultType === "ELITE_OPERATOR"
        || candidate.resultType === "RARE_OPERATOR"
        || candidate.resultType === "ELITE_OPERATOR_SHARD"
        || candidate.resultType === "RARE_OPERATOR_SHARD"
        || candidate.resultType === "OPERATOR_TOKEN"
        || candidate.resultType === "TRAINING_MATERIAL"
        || candidate.resultType === "WORK_SKILL_MATERIAL")
      && typeof candidate.resultId === "string";
  });
}

function parseGachaPityConfig(pityConfigJson: string): GachaPityConfig {
  const parsed = JSON.parse(pityConfigJson) as Record<string, unknown>;
  return {
    elite: typeof parsed.elite === "number" && Number.isSafeInteger(parsed.elite) && parsed.elite > 0 ? parsed.elite : 30,
    pickup: typeof parsed.pickup === "number" && Number.isSafeInteger(parsed.pickup) && parsed.pickup > 0 ? parsed.pickup : 60,
    rare: typeof parsed.rare === "number" && Number.isSafeInteger(parsed.rare) && parsed.rare > 0 ? parsed.rare : 10,
  };
}

function getInitialPityState(row: GachaPityRow | null): GachaPityState {
  if (row === null) {
    return {
      totalPullCount: 0,
      elitePityCount: 0,
      pickupElitePityCount: 0,
      rareOrHigherPityCount: 0,
      revision: 0,
    };
  }

  return {
    totalPullCount: row.total_pull_count,
    elitePityCount: row.elite_pity_count,
    pickupElitePityCount: row.pickup_elite_pity_count,
    rareOrHigherPityCount: row.rare_or_higher_pity_count,
    revision: row.revision,
  };
}

function applyGachaPityResult(state: GachaPityState, result: Pick<GachaCommittedResult, "resultRarity" | "isPickup">): GachaPityState {
  const isElite = result.resultRarity === "ELITE";
  const isRareOrHigher = result.resultRarity === "RARE" || isElite;
  return {
    totalPullCount: state.totalPullCount + 1,
    elitePityCount: isElite ? 0 : state.elitePityCount + 1,
    pickupElitePityCount: result.isPickup && isElite ? 0 : state.pickupElitePityCount + 1,
    rareOrHigherPityCount: isRareOrHigher ? 0 : state.rareOrHigherPityCount + 1,
    revision: state.revision,
  };
}

function finalizeGachaPityState(state: GachaPityState): GachaPityState {
  return {
    ...state,
    revision: state.revision + 1,
  };
}

function getNextGachaGuaranteeType(options: {
  banner: GachaBannerRow;
  pityState: GachaPityState;
  pityConfig: GachaPityConfig;
}): GachaGuaranteeType {
  if (
    options.banner.banner_type === "PICKUP"
    && options.banner.pickup_operator_id !== null
    && options.pityState.pickupElitePityCount + 1 >= options.pityConfig.pickup
  ) {
    return "PICKUP_ELITE_60";
  }
  if (options.pityState.elitePityCount + 1 >= options.pityConfig.elite) {
    return "ELITE_30";
  }
  if (options.pityState.rareOrHigherPityCount + 1 >= options.pityConfig.rare) {
    return "RARE_OR_HIGHER_10";
  }
  return "NONE";
}

function isGachaPoolResultEligibleForGuarantee(
  result: GachaPoolResult,
  guaranteeType: GachaGuaranteeType,
  banner: GachaBannerRow,
): boolean {
  if (guaranteeType === "PICKUP_ELITE_60") {
    return result.resultRarity === "ELITE"
      && (result.isPickup === true || result.resultId === banner.pickup_operator_id);
  }
  if (guaranteeType === "ELITE_30") {
    return result.resultRarity === "ELITE";
  }
  if (guaranteeType === "RARE_OR_HIGHER_10") {
    return result.resultRarity === "RARE" || result.resultRarity === "ELITE";
  }
  return true;
}

function hashStringToUint32(input: string): number {
  let hash = 2166136261;
  for (let index = 0; index < input.length; index += 1) {
    hash ^= input.charCodeAt(index);
    hash = Math.imul(hash, 16777619);
  }
  return hash >>> 0;
}

function getGachaResultWeight(result: GachaPoolResult, guaranteeType: GachaGuaranteeType): number {
  if (guaranteeType !== "NONE") {
    return typeof result.weight === "number" && Number.isFinite(result.weight) && result.weight > 0
      ? result.weight
      : 1;
  }
  return typeof result.weight === "number" && Number.isFinite(result.weight) && result.weight > 0
    ? result.weight
    : 0;
}

function selectGachaPoolResult(options: {
  poolResults: GachaPoolResult[];
  guaranteeType: GachaGuaranteeType;
  banner: GachaBannerRow;
  seed: string;
}): GachaPoolResult | null {
  const candidates = options.poolResults
    .filter((result) => isGachaPoolResultEligibleForGuarantee(result, options.guaranteeType, options.banner))
    .map((result) => ({ result, weight: getGachaResultWeight(result, options.guaranteeType) }))
    .filter((candidate) => candidate.weight > 0);
  const totalWeight = candidates.reduce((sum, candidate) => sum + candidate.weight, 0);
  if (totalWeight <= 0) return null;

  let roll = hashStringToUint32(options.seed) % totalWeight;
  for (const candidate of candidates) {
    if (roll < candidate.weight) return candidate.result;
    roll -= candidate.weight;
  }
  return candidates[candidates.length - 1]?.result ?? null;
}

function getOperatorShardItemId(operatorId: string): string {
  return `cur_operator_token:${operatorId}`;
}

function getDuplicateConversionQuantity(resultType: GachaPoolResult["resultType"]): number {
  return resultType === "ELITE_OPERATOR" ? 50 : 30;
}

function toGachaResponseResult(result: GachaCommittedResult): Omit<GachaCommittedResult, "id" | "operatorId" | "itemLedgerId" | "progressionLedgerId"> {
  return {
    pullSequence: result.pullSequence,
    resultRarity: result.resultRarity,
    resultType: result.resultType,
    resultId: result.resultId,
    reward: result.reward,
    isPickup: result.isPickup,
    guaranteeType: result.guaranteeType,
    wasDuplicate: result.wasDuplicate,
    duplicateConversion: result.duplicateConversion,
  };
}

function toGachaReplayResult(row: GachaReplayResultRow): Omit<GachaCommittedResult, "id" | "operatorId" | "itemLedgerId" | "progressionLedgerId"> {
  const duplicateConversion = row.duplicate_conversion_json === null
    ? null
    : JSON.parse(row.duplicate_conversion_json) as Record<string, unknown>;
  const reward = duplicateConversion === null
    ? {
      kind: row.result_type === "ELITE_OPERATOR" || row.result_type === "RARE_OPERATOR" ? "OPERATOR" : "ITEM",
      id: row.result_id,
      quantity: 1,
    }
    : {
      kind: "ITEM",
      id: duplicateConversion.itemId,
      operatorId: duplicateConversion.operatorId,
      quantity: duplicateConversion.quantity,
    };

  return {
    pullSequence: row.pull_sequence,
    resultRarity: row.result_rarity,
    resultType: row.result_type,
    resultId: row.result_id,
    reward,
    isPickup: row.is_pickup === 1,
    guaranteeType: row.guarantee_type,
    wasDuplicate: row.was_duplicate === 1,
    duplicateConversion,
  };
}

function getGachaReplayResponseResults(options: {
  resultSummaryJson: string;
  resultRows: GachaReplayResultRow[];
}): Array<Omit<GachaCommittedResult, "id" | "operatorId" | "itemLedgerId" | "progressionLedgerId">> {
  try {
    const parsed = JSON.parse(options.resultSummaryJson) as { responseResults?: unknown };
    if (Array.isArray(parsed.responseResults)) {
      return parsed.responseResults as Array<Omit<GachaCommittedResult, "id" | "operatorId" | "itemLedgerId" | "progressionLedgerId">>;
    }
  } catch {
    return options.resultRows.map(toGachaReplayResult);
  }
  return options.resultRows.map(toGachaReplayResult);
}

function getAuthSecret(env: Env): string | null {
  return (env as GameApiEnv).AUTH_SIGNING_KEY_CURRENT ?? null;
}

function createAuthError(errorCode: string, message: string) {
  return {
    errorCode,
    message,
  };
}

function createAuthFailure(status: AuthFailureStatus, errorCode: string, message: string): AuthFailure {
  return {
    status,
    errorCode,
    message,
  };
}

function isAuthFailure(value: AuthContext | AuthFailure): value is AuthFailure {
  return "errorCode" in value;
}

async function resolveAuthContext(c: GameApiContext): Promise<AuthContext | AuthFailure> {
  const authorization = c.req.header("Authorization");
  if (authorization === undefined || !authorization.startsWith("Bearer ")) {
    return createAuthFailure(401, "auth_required", "Authorization bearer token is required.");
  }

  const secret = getAuthSecret(c.env);
  if (secret === null || secret.trim() === "") {
    return createAuthFailure(500, "auth_not_configured", "Auth signing key is not configured.");
  }

  const token = authorization.slice("Bearer ".length).trim();
  const verified = await verifyAccessToken({ secret, token, nowMs: Date.now() });
  if (verified === null) {
    return createAuthFailure(401, "access_token_invalid", "Access token is invalid or expired.");
  }

  const row = await c.env.DB.prepare(
    `SELECT sessions.account_id, sessions.id AS session_id, sessions.status AS session_status,
            sessions.expires_at AS session_expires_at, accounts.status AS account_status
     FROM auth_sessions sessions
     JOIN accounts ON accounts.id = sessions.account_id
     WHERE sessions.id = ? AND sessions.account_id = ?`,
  )
    .bind(verified.sessionId, verified.accountId)
    .first<AuthSessionAccountRow>();

  if (row === null || row.session_status !== "ACTIVE" || row.session_expires_at <= Date.now()) {
    return createAuthFailure(401, "auth_session_invalid", "Auth session is invalid or expired.");
  }

  if (row.account_status !== "ACTIVE") {
    return createAuthFailure(403, "account_not_active", "Account is not active.");
  }

  return {
    accountId: row.account_id,
    sessionId: row.session_id,
  };
}

app.get("/status", (c) => c.json({ ok: true, service: "game-api", requestId: createRequestId() }));
app.post("/v1/auth/guest", async (c) => {
  const secret = getAuthSecret(c.env);
  if (secret === null || secret.trim() === "") {
    return c.json(createAuthError("auth_not_configured", "Auth signing key is not configured."), 500);
  }

  const now = Date.now();
  const accountId = createDomainId("account");
  const identityId = createDomainId("identity");
  const sessionId = createDomainId("session");
  const familyId = createDomainId("family");
  const providerSubject = createDomainId("guest");
  const refreshToken = createRefreshToken();
  const refreshTokenHash = await hashRefreshToken(refreshToken);
  const accessTokenTtlMs = 15 * 60 * 1000;
  const refreshTokenTtlMs = 30 * 24 * 60 * 60 * 1000;
  const accessTokenExpiresAt = now + accessTokenTtlMs;
  const refreshTokenExpiresAt = now + refreshTokenTtlMs;
  const accessToken = await createAccessToken({
    secret,
    accountId,
    sessionId,
    nowMs: now,
    ttlMs: accessTokenTtlMs,
  });

  await c.env.DB.batch([
    c.env.DB.prepare(
      `INSERT INTO accounts (id, status, created_at, updated_at)
       VALUES (?, 'ACTIVE', ?, ?)`,
    ).bind(accountId, now, now),
    c.env.DB.prepare(
      `INSERT INTO auth_identities (id, account_id, provider, provider_subject, linked_at)
       VALUES (?, ?, 'GUEST', ?, ?)`,
    ).bind(identityId, accountId, providerSubject, now),
    c.env.DB.prepare(
      `INSERT INTO auth_sessions (id, account_id, refresh_token_hash, family_id, generation, status, created_at, expires_at, rotated_at)
       VALUES (?, ?, ?, ?, 0, 'ACTIVE', ?, ?, NULL)`,
    ).bind(sessionId, accountId, refreshTokenHash, familyId, now, refreshTokenExpiresAt),
    c.env.DB.prepare(
      `INSERT INTO player_profiles (account_id, display_name, commander_xp, created_at, updated_at)
       VALUES (?, NULL, 0, ?, ?)`,
    ).bind(accountId, now, now),
    c.env.DB.prepare(
      `INSERT INTO account_runtime_state (account_id, protection_mode, maintenance_until, updated_at)
       VALUES (?, 'NORMAL', NULL, ?)`,
    ).bind(accountId, now),
    c.env.DB.prepare(
      `INSERT INTO account_tutorial_state (account_id, status, completed_at, updated_at)
       VALUES (?, 'NOT_STARTED', NULL, ?)`,
    ).bind(accountId, now),
  ]);

  return c.json({
    tokenType: "Bearer",
    account: {
      id: accountId,
      status: "ACTIVE",
    },
    accessToken,
    accessTokenExpiresAt: toIso(accessTokenExpiresAt),
    refreshToken,
    refreshTokenExpiresAt: toIso(refreshTokenExpiresAt),
  });
});
app.post("/v1/auth/refresh", async (c) => {
  const secret = getAuthSecret(c.env);
  if (secret === null || secret.trim() === "") {
    return c.json(createAuthError("auth_not_configured", "Auth signing key is not configured."), 500);
  }

  let body: { refreshToken?: unknown };
  try {
    body = await c.req.json();
  } catch {
    body = {};
  }

  if (typeof body.refreshToken !== "string" || body.refreshToken.trim() === "") {
    return c.json(createAuthError("contract_violation", "refreshToken is required."), 400);
  }

  const refreshTokenHash = await hashRefreshToken(body.refreshToken);
  const row = await c.env.DB.prepare(
    `SELECT sessions.id AS session_id, sessions.account_id, sessions.refresh_token_hash,
            sessions.family_id, sessions.generation, sessions.status AS session_status,
            sessions.expires_at AS session_expires_at, accounts.status AS account_status
     FROM auth_sessions sessions
     JOIN accounts ON accounts.id = sessions.account_id
     WHERE sessions.refresh_token_hash = ?`,
  )
    .bind(refreshTokenHash)
    .first<RefreshSessionAccountRow>();

  if (row === null) {
    return c.json(createAuthError("refresh_token_invalid", "Refresh token is invalid."), 401);
  }

  const now = Date.now();
  if (row.session_status === "ROTATED") {
    await c.env.DB.batch([
      c.env.DB.prepare(
        `UPDATE auth_sessions
         SET status = 'REUSED', rotated_at = ?
         WHERE family_id = ? AND status IN ('ACTIVE','ROTATED')`,
      ).bind(now, row.family_id),
    ]);
    return c.json(createAuthError("refresh_token_reused", "Refresh token was already rotated."), 401);
  }

  if (row.session_status !== "ACTIVE" || row.session_expires_at <= now) {
    return c.json(createAuthError("refresh_session_invalid", "Refresh session is invalid or expired."), 401);
  }

  if (row.account_status !== "ACTIVE") {
    return c.json(createAuthError("account_not_active", "Account is not active."), 403);
  }

  const newSessionId = createDomainId("session");
  const newRefreshToken = createRefreshToken();
  const newRefreshTokenHash = await hashRefreshToken(newRefreshToken);
  const accessTokenTtlMs = 15 * 60 * 1000;
  const refreshTokenTtlMs = 30 * 24 * 60 * 60 * 1000;
  const accessTokenExpiresAt = now + accessTokenTtlMs;
  const refreshTokenExpiresAt = now + refreshTokenTtlMs;
  const accessToken = await createAccessToken({
    secret,
    accountId: row.account_id,
    sessionId: newSessionId,
    nowMs: now,
    ttlMs: accessTokenTtlMs,
  });

  await c.env.DB.batch([
    c.env.DB.prepare(
      `UPDATE auth_sessions
       SET status = 'ROTATED', rotated_at = ?
       WHERE id = ? AND status = 'ACTIVE'`,
    ).bind(now, row.session_id),
    c.env.DB.prepare(
      `INSERT INTO auth_sessions (id, account_id, refresh_token_hash, family_id, generation, status, created_at, expires_at, rotated_at)
       VALUES (?, ?, ?, ?, ?, 'ACTIVE', ?, ?, NULL)`,
    ).bind(newSessionId, row.account_id, newRefreshTokenHash, row.family_id, row.generation + 1, now, refreshTokenExpiresAt),
  ]);

  return c.json({
    tokenType: "Bearer",
    account: {
      id: row.account_id,
      status: "ACTIVE",
    },
    accessToken,
    accessTokenExpiresAt: toIso(accessTokenExpiresAt),
    refreshToken: newRefreshToken,
    refreshTokenExpiresAt: toIso(refreshTokenExpiresAt),
  });
});
app.get("/v1/client-config", (c) =>
  c.json({
    environment: c.env.ENVIRONMENT,
    apiBaseUrl: apiBaseUrls[c.env.ENVIRONMENT],
    balanceConfigEndpointTemplate: "/v1/balance-config/:version",
    minimumClientVersion: "0.1.0",
    maintenance: { enabled: false },
  }),
);
app.get("/v1/balance-config/:version", async (c) => {
  const rawVersion = c.req.param("version");
  const version = parsePositiveInteger(rawVersion);
  if (version === null) {
    return c.json({ error: "INVALID_BALANCE_CONFIG_VERSION", version: rawVersion }, 400);
  }

  const row = await c.env.DB.prepare(
    `SELECT version, schema_version, required_content_version, checksum, config_json, published_at
     FROM balance_config_versions
     WHERE version = ?`,
  )
    .bind(version)
    .first<BalanceConfigRow>();

  if (row === null) {
    return c.json({ error: "BALANCE_CONFIG_NOT_PUBLISHED", version }, 404);
  }

  let payload: unknown;
  try {
    payload = JSON.parse(row.config_json) as unknown;
  } catch {
    return c.json({ error: "BALANCE_CONFIG_PAYLOAD_INVALID", version }, 500);
  }

  return c.json({
    version: row.version,
    schemaVersion: row.schema_version,
    requiredContentVersion: row.required_content_version,
    checksum: row.checksum,
    publishedAt: row.published_at,
    payload,
  });
});

app.post("/v1/gacha/banners/:bannerId/pulls", async (c) => {
  const bannerId = c.req.param("bannerId");
  const idempotencyKey = c.req.header("Idempotency-Key");

  if (idempotencyKey === undefined || idempotencyKey.trim() === "") {
    return c.json(createGachaRejectedResponse({
      bannerId,
      pullCount: null,
      errorCode: "contract_violation",
      message: "Idempotency-Key header is required.",
    }), 400);
  }

  let body: { bannerId?: unknown; pullCount?: unknown };
  try {
    body = await c.req.json();
  } catch {
    body = {};
  }

  const pullCount = typeof body.pullCount === "number" && Number.isSafeInteger(body.pullCount)
    ? body.pullCount
    : null;

  if (body.bannerId !== undefined) {
    return c.json(createGachaRejectedResponse({
      bannerId,
      pullCount,
      errorCode: "contract_violation",
      message: "Request body must not include bannerId.",
    }), 400);
  }

  if (pullCount !== 1 && pullCount !== 10) {
    return c.json(createGachaRejectedResponse({
      bannerId,
      pullCount,
      errorCode: "invalid_pull_count",
      message: "pullCount must be 1 or 10.",
    }), 400);
  }

  const authContext = await resolveAuthContext(c);
  if (isAuthFailure(authContext)) {
    return c.json(createGachaRejectedResponse({
      bannerId,
      pullCount,
      errorCode: authContext.errorCode,
      message: authContext.message,
    }), authContext.status);
  }

  const replayBatch = await c.env.DB.prepare(
    `SELECT batch.id, batch.banner_id, banner.balance_config_version, batch.pull_count, batch.cost_item_id, batch.cost_quantity,
            batch.post_pity_json, batch.result_summary_json, batch.created_at
     FROM idempotency_records idem
     JOIN gacha_pull_batches batch ON batch.id = idem.result_ref
     JOIN gacha_banners banner ON banner.id = batch.banner_id
     WHERE idem.account_id = ? AND idem.operation = 'gacha_pull' AND idem.idempotency_key = ? AND batch.banner_id = ?`,
  )
    .bind(authContext.accountId, idempotencyKey, bannerId)
    .first<GachaReplayBatchRow>();

  if (replayBatch !== null) {
    const replayResults = await c.env.DB.prepare(
      `SELECT pull_sequence, result_rarity, result_type, result_id, is_pickup,
              guarantee_type, was_duplicate, duplicate_conversion_json
       FROM gacha_pull_results
       WHERE batch_id = ? AND account_id = ?
       ORDER BY pull_sequence`,
    )
      .bind(replayBatch.id, authContext.accountId)
      .all<GachaReplayResultRow>();

    return c.json(createGachaDuplicateReplayResponse({
      batchId: replayBatch.id,
      bannerId: replayBatch.banner_id,
      balanceVersion: String(replayBatch.balance_config_version),
      pullCount: replayBatch.pull_count as 1 | 10,
      results: getGachaReplayResponseResults({
        resultSummaryJson: replayBatch.result_summary_json,
        resultRows: replayResults.results,
      }),
      postPityState: JSON.parse(replayBatch.post_pity_json) as GachaPityState,
      createdAt: replayBatch.created_at,
    }));
  }

  const row = await c.env.DB.prepare(
    `SELECT id, banner_type, status, balance_config_version, pickup_operator_id, starts_at, ends_at, pity_config_json, pool_snapshot_json
     FROM gacha_banners
     WHERE id = ?`,
  )
    .bind(bannerId)
    .first<GachaBannerRow>();

  if (row === null) {
    return c.json(createGachaRejectedResponse({
      bannerId,
      pullCount,
      errorCode: "banner_not_found",
      message: "Gacha banner was not found.",
    }), 404);
  }

  if (!isActiveGachaBanner(row, Date.now())) {
    return c.json(createGachaRejectedResponse({
      bannerId,
      pullCount,
      errorCode: "banner_inactive",
      message: "Gacha banner is not active.",
    }), 409);
  }

  const ticketRow = await c.env.DB.prepare(
    `SELECT quantity
     FROM account_items
     WHERE account_id = ? AND item_id = 'cur_command_ticket'`,
  )
    .bind(authContext.accountId)
    .first<AccountItemRow>();
  const currentTickets = ticketRow?.quantity ?? 0;
  if (currentTickets < pullCount) {
    return c.json(createGachaRejectedResponse({
      bannerId,
      pullCount,
      errorCode: "insufficient_command_ticket",
      message: "Not enough command tickets.",
    }), 409);
  }

  const poolResults = parseGachaPoolResults(row.pool_snapshot_json);
  if (poolResults.length === 0) {
    return c.json(createGachaRejectedResponse({
      bannerId,
      pullCount,
      errorCode: "gacha_pool_unavailable",
      message: "Gacha pool is unavailable.",
    }), 503);
  }

  const pityRow = await c.env.DB.prepare(
    `SELECT total_pull_count, elite_pity_count, pickup_elite_pity_count, rare_or_higher_pity_count, revision
     FROM player_gacha_pity
     WHERE account_id = ? AND banner_id = ?`,
  )
    .bind(authContext.accountId, bannerId)
    .first<GachaPityRow>();
  const prePityState = getInitialPityState(pityRow);
  const pityConfig = parseGachaPityConfig(row.pity_config_json);
  let nextPityState = prePityState;
  const now = Date.now();
  const batchId = createDomainId("gacha_batch");
  const mutationId = createDomainId("mutation");
  const costLedgerId = createDomainId("item_ledger");
  const latestMutation = await c.env.DB.prepare(
    `SELECT MAX(sequence) AS sequence
     FROM account_mutations
     WHERE account_id = ?`,
  )
    .bind(authContext.accountId)
    .first<AccountMutationSequenceRow>();
  const nextSequence = (latestMutation?.sequence ?? 0) + 1;
  const statements = [
    c.env.DB.prepare(
      `INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at)
       VALUES (?, ?, ?, 'GACHA_PULL', ?, ?)`,
    ).bind(mutationId, authContext.accountId, nextSequence, idempotencyKey, now),
    c.env.DB.prepare(
      `UPDATE account_items
       SET quantity = ?, updated_at = ?
       WHERE account_id = ? AND item_id = 'cur_command_ticket'`,
    ).bind(currentTickets - pullCount, now, authContext.accountId),
    c.env.DB.prepare(
      `INSERT INTO item_ledger (id, account_id, mutation_id, item_id, delta, quantity_after, reason, source_type, source_id, created_at)
       VALUES (?, ?, ?, 'cur_command_ticket', ?, ?, 'GACHA_PULL_COST', 'GACHA_PULL', ?, ?)`,
    ).bind(costLedgerId, authContext.accountId, mutationId, -pullCount, currentTickets - pullCount, batchId, now),
  ];
  const committedResults: GachaCommittedResult[] = [];
  const ownedOperatorIds = new Set<string>();
  const existingOperators = await c.env.DB.prepare(
    `SELECT operator_id
     FROM account_operators
     WHERE account_id = ?`,
  )
    .bind(authContext.accountId)
    .all<ExistingOperatorRow>();
  for (const operator of existingOperators.results) {
    ownedOperatorIds.add(operator.operator_id);
  }
  const itemBalanceCache = new Map<string, number>();

  const getCachedItemBalance = async (itemId: string): Promise<number> => {
    const cachedBalance = itemBalanceCache.get(itemId);
    if (cachedBalance !== undefined) return cachedBalance;
    const row = await c.env.DB.prepare(
      `SELECT quantity
       FROM account_items
       WHERE account_id = ? AND item_id = ?`,
    )
      .bind(authContext.accountId, itemId)
      .first<AccountItemRow>();
    const balance = row?.quantity ?? 0;
    itemBalanceCache.set(itemId, balance);
    return balance;
  };

  for (let index = 0; index < pullCount; index += 1) {
    const guaranteeType = getNextGachaGuaranteeType({
      banner: row,
      pityState: nextPityState,
      pityConfig,
    });
    const poolResult = selectGachaPoolResult({
      poolResults,
      guaranteeType,
      banner: row,
      seed: `${authContext.accountId}:${bannerId}:${idempotencyKey}:${index + 1}`,
    });
    if (poolResult === null) {
      return c.json(createGachaRejectedResponse({
        bannerId,
        pullCount,
        errorCode: "gacha_pool_unavailable",
        message: "Gacha pool is unavailable.",
      }), 503);
    }
    const isOperator = poolResult.resultType === "ELITE_OPERATOR" || poolResult.resultType === "RARE_OPERATOR";
    const isDuplicate = isOperator && ownedOperatorIds.has(poolResult.resultId);
    const isPickup = Boolean(poolResult.isPickup) || poolResult.resultId === row.pickup_operator_id;
    const resultId = createDomainId("gacha_result");
    let itemLedgerId: string | null = null;
    let progressionLedgerId: string | null = null;
    let duplicateConversion: Record<string, unknown> | null = null;
    let reward: Record<string, unknown>;

    if (isOperator && isDuplicate) {
      const conversionQuantity = getDuplicateConversionQuantity(poolResult.resultType);
      const shardItemId = getOperatorShardItemId(poolResult.resultId);
      const shardBalanceAfter = await getCachedItemBalance(shardItemId) + conversionQuantity;
      itemBalanceCache.set(shardItemId, shardBalanceAfter);
      itemLedgerId = createDomainId("item_ledger");
      duplicateConversion = {
        sourceResultType: poolResult.resultType,
        sourceResultId: poolResult.resultId,
        itemId: "cur_operator_token",
        operatorId: poolResult.resultId,
        quantity: conversionQuantity,
      };
      reward = {
        kind: "ITEM",
        id: "cur_operator_token",
        operatorId: poolResult.resultId,
        quantity: conversionQuantity,
      };
      statements.push(
        c.env.DB.prepare(
          `INSERT INTO account_items (account_id, item_id, quantity, updated_at)
           VALUES (?, ?, ?, ?)
           ON CONFLICT(account_id, item_id) DO UPDATE SET quantity = excluded.quantity, updated_at = excluded.updated_at`,
        ).bind(authContext.accountId, shardItemId, shardBalanceAfter, now),
        c.env.DB.prepare(
          `INSERT INTO item_ledger (id, account_id, mutation_id, item_id, delta, quantity_after, reason, source_type, source_id, created_at)
           VALUES (?, ?, ?, ?, ?, ?, 'GACHA_DUPLICATE_CONVERSION', 'GACHA_PULL', ?, ?)`,
        ).bind(itemLedgerId, authContext.accountId, mutationId, shardItemId, conversionQuantity, shardBalanceAfter, batchId, now),
      );
    } else if (isOperator) {
      progressionLedgerId = createDomainId("progression");
      reward = {
        kind: "OPERATOR",
        id: poolResult.resultId,
        quantity: 1,
      };
      statements.push(
        c.env.DB.prepare(
          `INSERT INTO account_operators (account_id, operator_id, skill_training, promotion, acquired_at, revision, last_mutation_id)
           VALUES (?, ?, 1, 0, ?, 1, ?)`,
        ).bind(authContext.accountId, poolResult.resultId, now, mutationId),
        c.env.DB.prepare(
          `INSERT INTO progression_ledger (id, account_id, mutation_id, progression_type, target_id, delta_json, created_at)
           VALUES (?, ?, ?, 'OPERATOR_ACQUIRED', ?, ?, ?)`,
        ).bind(progressionLedgerId, authContext.accountId, mutationId, poolResult.resultId, JSON.stringify({ acquired: true }), now),
      );
      ownedOperatorIds.add(poolResult.resultId);
    } else {
      const quantity = poolResult.quantity ?? 1;
      const itemBalanceAfter = await getCachedItemBalance(poolResult.resultId) + quantity;
      itemBalanceCache.set(poolResult.resultId, itemBalanceAfter);
      itemLedgerId = createDomainId("item_ledger");
      reward = {
        kind: "ITEM",
        id: poolResult.resultId,
        quantity,
      };
      statements.push(
        c.env.DB.prepare(
          `INSERT INTO account_items (account_id, item_id, quantity, updated_at)
           VALUES (?, ?, ?, ?)
           ON CONFLICT(account_id, item_id) DO UPDATE SET quantity = excluded.quantity, updated_at = excluded.updated_at`,
        ).bind(authContext.accountId, poolResult.resultId, itemBalanceAfter, now),
        c.env.DB.prepare(
          `INSERT INTO item_ledger (id, account_id, mutation_id, item_id, delta, quantity_after, reason, source_type, source_id, created_at)
           VALUES (?, ?, ?, ?, ?, ?, 'GACHA_ITEM_GRANT', 'GACHA_PULL', ?, ?)`,
        ).bind(itemLedgerId, authContext.accountId, mutationId, poolResult.resultId, quantity, itemBalanceAfter, batchId, now),
      );
    }

    const committedResult: GachaCommittedResult = {
      id: resultId,
      pullSequence: index + 1,
      resultRarity: poolResult.resultRarity,
      resultType: poolResult.resultType,
      resultId: poolResult.resultId,
      reward,
      isPickup,
      guaranteeType,
      wasDuplicate: isDuplicate,
      duplicateConversion,
      operatorId: isOperator ? poolResult.resultId : null,
      itemLedgerId,
      progressionLedgerId,
    };
    nextPityState = applyGachaPityResult(nextPityState, committedResult);
    committedResults.push(committedResult);
  }

  const postPityState = finalizeGachaPityState(nextPityState);
  const responseResults = committedResults.map(toGachaResponseResult);
  statements.push(
    c.env.DB.prepare(
      `INSERT INTO gacha_pull_batches (
         id, account_id, banner_id, mutation_id, idempotency_key, pull_count, cost_item_id, cost_quantity,
         cost_ledger_id, pre_pity_json, post_pity_json, rng_seed_hash, result_summary_json, created_at
       ) VALUES (?, ?, ?, ?, ?, ?, 'cur_command_ticket', ?, ?, ?, ?, ?, ?, ?)`,
    ).bind(
      batchId,
      authContext.accountId,
      bannerId,
      mutationId,
      idempotencyKey,
      pullCount,
      pullCount,
      costLedgerId,
      JSON.stringify(prePityState),
      JSON.stringify(postPityState),
      `rng_hash_${createRequestId().slice(4)}`,
      JSON.stringify({
        responseResults,
        results: committedResults.map((result) => ({
          resultRarity: result.resultRarity,
          resultType: result.resultType,
          resultId: result.resultId,
          wasDuplicate: result.wasDuplicate,
        })),
      }),
      now,
    ),
  );
  for (const result of committedResults) {
    statements.push(
      c.env.DB.prepare(
        `INSERT INTO gacha_pull_results (
           id, batch_id, account_id, banner_id, pull_sequence, result_rarity, result_type, result_id, is_pickup,
           guarantee_type, was_duplicate, duplicate_conversion_json, operator_id, item_ledger_id, progression_ledger_id, created_at
         ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
      ).bind(
        result.id,
        batchId,
        authContext.accountId,
        bannerId,
        result.pullSequence,
        result.resultRarity,
        result.resultType,
        result.resultId,
        result.isPickup ? 1 : 0,
        result.guaranteeType,
        result.wasDuplicate ? 1 : 0,
        result.duplicateConversion === null ? null : JSON.stringify(result.duplicateConversion),
        result.operatorId,
        result.itemLedgerId,
        result.progressionLedgerId,
        now,
      ),
    );
  }
  statements.push(
    c.env.DB.prepare(
      `INSERT INTO player_gacha_pity (
         account_id, banner_id, total_pull_count, elite_pity_count, pickup_elite_pity_count,
         rare_or_higher_pity_count, revision, last_batch_id, updated_at
       ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
       ON CONFLICT(account_id, banner_id) DO UPDATE SET
         total_pull_count = excluded.total_pull_count,
         elite_pity_count = excluded.elite_pity_count,
         pickup_elite_pity_count = excluded.pickup_elite_pity_count,
         rare_or_higher_pity_count = excluded.rare_or_higher_pity_count,
         revision = excluded.revision,
         last_batch_id = excluded.last_batch_id,
         updated_at = excluded.updated_at`,
    ).bind(
      authContext.accountId,
      bannerId,
      postPityState.totalPullCount,
      postPityState.elitePityCount,
      postPityState.pickupElitePityCount,
      postPityState.rareOrHigherPityCount,
      postPityState.revision,
      batchId,
      now,
    ),
    c.env.DB.prepare(
      `INSERT INTO idempotency_records (id, account_id, idempotency_key, operation, result_ref, created_at)
       VALUES (?, ?, ?, 'gacha_pull', ?, ?)`,
    ).bind(createDomainId("idem"), authContext.accountId, idempotencyKey, batchId, now),
  );

  await c.env.DB.batch(statements);

  return c.json(createGachaAcceptedResponse({
    batchId,
    bannerId,
    balanceVersion: String(row.balance_config_version),
    pullCount,
    results: responseResults,
    postPityState,
    createdAt: now,
  }));
});

app.get("/v1/runs/:runId/reward-preview/:rewardPreviewId", async (c) => {
  const runId = c.req.param("runId");
  const rewardPreviewId = c.req.param("rewardPreviewId");
  const row = await c.env.DB.prepare(
    `SELECT id, account_id, run_id, status, balance_config_version, reward_plan_json, expires_at, created_at, claimed_at, reward_claim_id
     FROM run_reward_previews
     WHERE run_id = ? AND id = ?`,
  )
    .bind(runId, rewardPreviewId)
    .first<RewardPreviewRow>();

  if (row === null) {
    return c.json({ errorCode: "reward_preview_not_found", message: "Reward preview was not found." }, 404);
  }

  const claimability = getClaimability(row);
  return c.json({
    runId: row.run_id,
    rewardPreviewId: row.id,
    status: row.status,
    claimability,
    balanceVersion: String(row.balance_config_version),
    expiresAt: toIso(row.expires_at),
    serverCalculatedAt: toIso(row.created_at),
    rewardLines: getRewardLines(row.reward_plan_json),
    claim: {
      claimable: claimability === "CLAIMABLE",
      rewardClaimId: row.reward_claim_id,
      claimedAt: row.claimed_at === null ? null : toIso(row.claimed_at),
      errorCode: claimability === "CLAIMABLE" || claimability === "ALREADY_CLAIMED" ? null : `reward_preview_${claimability.toLowerCase()}`,
    },
  });
});

app.post("/v1/runs/:runId/reward-preview/:rewardPreviewId/claim", async (c) => {
  const runId = c.req.param("runId");
  const rewardPreviewId = c.req.param("rewardPreviewId");
  const idempotencyKey = c.req.header("Idempotency-Key");

  if (idempotencyKey === undefined || idempotencyKey.trim() === "") {
    return c.json({
      runId,
      rewardPreviewId,
      rewardClaimId: null,
      status: "REJECTED",
      serverClaimedAt: null,
      balanceVersion: null,
      rewardLines: [],
      errorCode: "contract_violation",
      message: "Idempotency-Key header is required.",
    }, 400);
  }

  let body: { runId?: unknown; rewardPreviewId?: unknown };
  try {
    body = await c.req.json();
  } catch {
    body = {};
  }

  if (body.runId !== runId || body.rewardPreviewId !== rewardPreviewId) {
    return c.json({
      runId,
      rewardPreviewId,
      rewardClaimId: null,
      status: "REJECTED",
      serverClaimedAt: null,
      balanceVersion: null,
      rewardLines: [],
      errorCode: "contract_violation",
      message: "Path and body runId/rewardPreviewId must match.",
    }, 400);
  }

  const row = await c.env.DB.prepare(
    `SELECT id, account_id, run_id, status, balance_config_version, reward_plan_json, expires_at, created_at, claimed_at, reward_claim_id
     FROM run_reward_previews
     WHERE run_id = ? AND id = ?`,
  )
    .bind(runId, rewardPreviewId)
    .first<RewardPreviewRow>();

  if (row !== null && row.status === "CLAIMED" && row.reward_claim_id !== null && row.claimed_at !== null) {
    return c.json({
      runId: row.run_id,
      rewardPreviewId: row.id,
      rewardClaimId: row.reward_claim_id,
      status: "DUPLICATE_REPLAY",
      serverClaimedAt: toIso(row.claimed_at),
      balanceVersion: String(row.balance_config_version),
      rewardLines: [],
      errorCode: "duplicate_replay",
      message: "Reward claim was already committed.",
    });
  }

  if (row === null) {
    return c.json({
      runId,
      rewardPreviewId,
      rewardClaimId: null,
      status: "REJECTED",
      serverClaimedAt: null,
      balanceVersion: null,
      rewardLines: [],
      errorCode: "reward_preview_not_found",
      message: "Reward preview was not found.",
    }, 404);
  }

  if (getClaimability(row) !== "CLAIMABLE") {
    return c.json({
      runId,
      rewardPreviewId,
      rewardClaimId: null,
      status: "REJECTED",
      serverClaimedAt: null,
      balanceVersion: String(row.balance_config_version),
      rewardLines: [],
      errorCode: `reward_preview_${getClaimability(row).toLowerCase()}`,
      message: "Reward preview is not claimable.",
    });
  }

  const idempotencyRecord = await c.env.DB.prepare(
    `SELECT idem.result_ref, idem.created_at, reward.created_at AS reward_created_at
     FROM idempotency_records idem
     LEFT JOIN reward_transactions reward ON reward.id = idem.result_ref
     WHERE idem.account_id = ? AND idem.operation = 'run_reward_claim' AND idem.idempotency_key = ?`,
  )
    .bind(row.account_id, idempotencyKey)
    .first<IdempotencyRecordRow>();

  if (idempotencyRecord !== null) {
    return c.json({
      runId: row.run_id,
      rewardPreviewId: row.id,
      rewardClaimId: idempotencyRecord.result_ref,
      status: "DUPLICATE_REPLAY",
      serverClaimedAt: toIso(idempotencyRecord.reward_created_at ?? idempotencyRecord.created_at),
      balanceVersion: String(row.balance_config_version),
      rewardLines: [],
      errorCode: "duplicate_replay",
      message: "Reward claim was already committed.",
    });
  }

  const now = Date.now();
  const accountId = row.account_id;
  const rewardClaimId = createDomainId("reward_claim");
  const mutationId = createDomainId("mutation");
  const economyTransactionId = createDomainId("economy");
  const rewardLines = getCurrencyRewardLines(row.reward_plan_json);
  const latestMutation = await c.env.DB.prepare(
    `SELECT MAX(sequence) AS sequence
     FROM account_mutations
     WHERE account_id = ?`,
  )
    .bind(accountId)
    .first<AccountMutationSequenceRow>();
  const nextSequence = (latestMutation?.sequence ?? 0) + 1;
  const claimRewardLines = [];
  const statements = [
    c.env.DB.prepare(
      `INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at)
       VALUES (?, ?, ?, 'RUN_REWARD_CLAIM', ?, ?)`,
    ).bind(mutationId, accountId, nextSequence, idempotencyKey, now),
    c.env.DB.prepare(
      `INSERT INTO reward_transactions (id, account_id, run_id, mutation_id, balance_config_version, reward_json, created_at)
       VALUES (?, ?, ?, ?, ?, ?, ?)`,
    ).bind(rewardClaimId, accountId, runId, mutationId, row.balance_config_version, row.reward_plan_json, now),
    c.env.DB.prepare(
      `INSERT INTO economy_transactions (id, account_id, mutation_id, reason, source_type, source_id, created_at)
       VALUES (?, ?, ?, 'RUN_REWARD_CLAIM', 'RUN_REWARD_PREVIEW', ?, ?)`,
    ).bind(economyTransactionId, accountId, mutationId, rewardPreviewId, now),
  ];

  for (const rewardLine of rewardLines) {
    const currency = currencyIdToD1Currency[rewardLine.id as keyof typeof currencyIdToD1Currency];
    const current = await c.env.DB.prepare(
      `SELECT balance
       FROM account_currencies
       WHERE account_id = ? AND currency = ?`,
    )
      .bind(accountId, currency)
      .first<AccountCurrencyRow>();
    const balanceAfter = (current?.balance ?? 0) + rewardLine.quantity;
    const ledgerId = createDomainId("ledger");
    statements.push(
      c.env.DB.prepare(
        `INSERT INTO account_currencies (account_id, currency, balance, updated_at)
         VALUES (?, ?, ?, ?)
         ON CONFLICT(account_id, currency) DO UPDATE SET balance = excluded.balance, updated_at = excluded.updated_at`,
      ).bind(accountId, currency, balanceAfter, now),
      c.env.DB.prepare(
        `INSERT INTO currency_ledger (id, account_id, transaction_id, currency, delta, balance_after, created_at)
         VALUES (?, ?, ?, ?, ?, ?, ?)`,
      ).bind(ledgerId, accountId, economyTransactionId, currency, rewardLine.quantity, balanceAfter, now),
    );
    claimRewardLines.push({
      kind: rewardLine.kind,
      id: rewardLine.id,
      quantity: rewardLine.quantity,
      source: rewardLine.source,
      balanceAfter,
    });
  }

  statements.push(
    c.env.DB.prepare(
      `UPDATE run_reward_previews
       SET status = 'CLAIMED', claimed_at = ?, reward_claim_id = ?
       WHERE id = ? AND run_id = ? AND status = 'ACTIVE'`,
    ).bind(now, rewardClaimId, rewardPreviewId, runId),
    c.env.DB.prepare(
      `INSERT INTO idempotency_records (id, account_id, idempotency_key, operation, result_ref, created_at)
       VALUES (?, ?, ?, 'run_reward_claim', ?, ?)`,
    ).bind(createDomainId("idem"), accountId, idempotencyKey, rewardClaimId, now),
  );

  await c.env.DB.batch(statements);

  return c.json({
    runId,
    rewardPreviewId,
    rewardClaimId,
    status: "CLAIMED",
    serverClaimedAt: toIso(now),
    balanceVersion: String(row.balance_config_version),
    rewardLines: claimRewardLines,
    errorCode: null,
    message: null,
  });
});

export default app;


