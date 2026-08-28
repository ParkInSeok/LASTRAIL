import { Hono } from "hono";
import type { Context } from "hono";
import { buildOpsErrorLogInsertEnvelope, createRequestId } from "@lastrail/common";
import {
  calculateOfflineRewardPreview,
  calculateRunRewardPreview,
  launchOfflineRewardsConfig,
  parseRunRewardsConfig,
  type IntegrityDisposition,
  type RunOutcome,
  type RunRiskTier,
} from "../../../packages/balance/src/index";
import { createAccessToken, hashRefreshToken, verifyAccessToken } from "./auth";

const app = new Hono<{ Bindings: Env }>();

type GameApiContext = Context<{ Bindings: Env }>;

type GameApiEnv = Env & {
  AUTH_SIGNING_KEY_CURRENT?: string;
  GOOGLE_AUTH_AUDIENCES?: string;
  GOOGLE_ID_TOKEN_VERIFIER?: GoogleIdTokenVerifier;
};

type GoogleIdTokenVerifier = {
  verifyIdToken: (idToken: string, audiences: string[]) => Promise<GoogleIdentityClaims>;
};

type GoogleIdentityClaims = {
  subject: string;
  email?: string;
  emailVerified?: boolean;
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

type RunFinishRow = {
  id: string;
  account_id: string;
  status: "ACTIVE" | "FINISH_PENDING" | "COMPLETED" | "FAILED" | "QUARANTINED" | "EXPIRED" | "ABANDONED";
  balance_config_version: number;
  objective: string;
  risk: string;
  preview_json: string;
  config_json: string;
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
  balance_config_version?: number | null;
  preview_run_id?: string | null;
};

type OfflineRewardReplayRow = {
  result_ref: string;
  created_at: number;
  reward_created_at: number | null;
  reward_json: string | null;
};

type FirstClearRow = {
  cleared_at: number;
};

type AccountStoryProgressRow = {
  act_id?: string;
  highest_cleared_operation_id?: string;
  highest_story_progress_id: string;
  cleared_operations_json: string;
  story_flags_json: string;
  narrative_version?: string | null;
  last_progress_run_id?: string | null;
  updated_at?: number;
};

type AccountOfflineRewardStateRow = {
  last_offline_reward_claim_at: number;
  last_story_progress_id: string;
  last_balance_config_version: number | null;
  last_narrative_version: string | null;
  last_previewed_at: number | null;
  revision: number;
  updated_at: number;
};

type AccountUnlockRow = {
  unlock_id: string;
};

type RunFinishBody = {
  runId: string;
  currentStage: "Complete";
  currentWaveNumber: number;
  completedWaveCount: number;
  encounterType?: "ORDINARY_15_WAVE" | "RAID_BOSS";
  raidResult?: {
    raidId?: string;
    bossId?: string;
    bossDefeated: boolean;
    completedPhaseCount?: number;
    encounterDurationSeconds?: number;
  };
  outcome: RunOutcome;
  runtimeStateJson: string;
  tacticSelections: unknown[];
  clientCompletedAt: string;
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
  account_created_at?: number;
};

type RefreshSessionAccountRow = AuthSessionAccountRow & {
  refresh_token_hash: string;
  family_id: string;
  generation: number;
};

type AuthIdentityAccountRow = {
  account_id: string;
  account_status: "ACTIVE" | "SUSPENDED" | "BANNED" | "DELETED";
};

type AuthContext = {
  accountId: string;
  sessionId: string;
  accountCreatedAt: number;
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

type AuthFailureStatus = 400 | 401 | 403 | 409 | 500;

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

const runRewardPreviewTtlMs = 24 * 60 * 60 * 1000;
const runFinishForbiddenRewardFields = new Set([
  "rewardAmount",
  "rewardAmounts",
  "rewardLines",
  "rewardClaimId",
  "rewardPreviewId",
  "currencyGrants",
  "itemGrants",
  "ledgerIds",
  "firstClearEligible",
  "firstClearApplied",
  "storyProgress",
  "highestStoryProgressId",
  "unlockedOperations",
  "narrativeFlags",
  "storyFlags",
  "railChoiceResults",
  "railChoices",
  "choiceResults",
  "optionalFlags",
  "startAugmentPackageRef",
  "startTacticPackageRef",
  "rawAnimationTimeline",
  "animationTimeline",
  "frameData",
  "vfxEvents",
  "sfxEvents",
  "cameraEvents",
  "hitStopEvents",
  "rawRng",
  "rawRngMaterial",
  "probability",
  "validatorDetails",
]);
const runFinishNestedForbiddenFields = new Set([
  ...runFinishForbiddenRewardFields,
  "rewardAmount",
  "rewardAmounts",
  "currencyGrants",
  "itemGrants",
  "economyGrants",
  "ledgerIds",
]);

const actOneStoryProgressOrder = [
  "none",
  "opn_01_last_departure",
  "opn_02_first_signal",
  "opn_03_water_stop",
  "opn_04_scrapstar_show",
  "opn_05_night_boarders",
  "opn_06_broken_line",
  "raid_raider_king_engine",
] as const;

const actOneOperationIds = actOneStoryProgressOrder.filter((id) => id !== "none");
type ActOneStoryProgressId = typeof actOneStoryProgressOrder[number];
const actOneNarrativeVersion = "act1.2026-08-27.001";
const offlineRewardForbiddenQueryKeys = [
  "accountId",
  "account_id",
  "storyProgress",
  "story_progress",
  "storyProgressId",
  "story_progress_id",
  "highestStoryProgressId",
  "risk",
  "offlineSeconds",
  "offline_seconds",
  "actualOfflineSeconds",
  "rewardAmount",
  "rewardAmounts",
  "rewardLines",
  "currencyGrants",
  "itemGrants",
  "economyGrants",
];

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

function getOpsLogEnvironment(env: Env): "DEV" | "PRD" {
  return env.ENVIRONMENT === "PRD" ? "PRD" : "DEV";
}

async function writeGameApiOpsErrorLog(c: GameApiContext, input: {
  severity: "error" | "critical";
  errorCode: string;
  messageSummary: string;
  requestId?: string | null;
  accountId?: string | null;
  runId?: string | null;
  gachaBatchId?: string | null;
  gachaBannerId?: string | null;
  storyProgressId?: string | null;
  idempotencyOperation?: string | null;
  idempotencyKey?: string | null;
  correlation?: Record<string, string | number | boolean | null>;
  context?: Record<string, string | number | boolean | null>;
  retryable?: boolean;
}): Promise<void> {
  try {
    const now = Date.now();
    const envelope = await buildOpsErrorLogInsertEnvelope({
      id: createDomainId("ops_error"),
      occurredAtMs: now,
      environment: getOpsLogEnvironment(c.env),
      service: "game_api",
      severity: input.severity,
      errorCode: input.errorCode,
      messageSummary: input.messageSummary,
      source: "game_api",
      requestId: input.requestId ?? c.req.header("X-Request-Id") ?? null,
      accountId: input.accountId ?? null,
      runId: input.runId ?? null,
      gachaBatchId: input.gachaBatchId ?? null,
      gachaBannerId: input.gachaBannerId ?? null,
      storyProgressId: input.storyProgressId ?? null,
      idempotencyOperation: input.idempotencyOperation ?? null,
      idempotencyKey: input.idempotencyKey ?? null,
      correlation: input.correlation ?? {},
      context: input.context ?? {},
      retryable: input.retryable ?? false,
      createdAtMs: now,
    });
    await c.env.DB.prepare(
      `INSERT INTO ops_error_logs (
         id, occurred_at, environment, service, severity, error_code, message_summary,
         status, source, request_id, account_id, run_id, reward_preview_id, reward_claim_id,
         offline_reward_claim_id, gacha_batch_id, gacha_banner_id, story_progress_id,
         integrity_event_id, support_ticket_id, idempotency_operation, idempotency_key_hash,
         correlation_json, sanitized_context_json, retryable, masked, payload_ref, trace_ref,
         resolved_at, resolved_by, resolution_note, created_at
       ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, NULL, NULL, NULL, ?, ?, ?, NULL, NULL, ?, ?, ?, ?, ?, ?, NULL, NULL, NULL, NULL, NULL, ?)`,
    ).bind(
      envelope.id,
      envelope.occurredAt,
      envelope.environment,
      envelope.service,
      envelope.severity,
      envelope.errorCode,
      envelope.messageSummary,
      envelope.status,
      envelope.source,
      envelope.requestId,
      envelope.accountId,
      envelope.runId,
      envelope.gachaBatchId,
      envelope.gachaBannerId,
      envelope.storyProgressId,
      envelope.idempotencyOperation,
      envelope.idempotencyKeyHash,
      envelope.correlationJson,
      envelope.sanitizedContextJson,
      envelope.retryable ? 1 : 0,
      envelope.masked ? 1 : 0,
      envelope.createdAt,
    ).run();
  } catch {
    // Ops logging is observational only; never worsen the player-facing API path.
  }
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

function createRunFinishRejectedResponse(options: {
  runId: string;
  balanceVersion?: string | null;
  errorCode: string;
  message: string;
}) {
  return {
    runId: options.runId,
    accepted: false,
    resultStatus: "REJECTED",
    rewardClaimId: null,
    rewardPreviewId: null,
    serverReceivedAt: null,
    balanceVersion: options.balanceVersion ?? null,
    message: options.message,
    errorCode: options.errorCode,
  };
}

function createRunFinishAcceptedResponse(options: {
  runId: string;
  rewardPreviewId: string;
  balanceVersion: string;
  serverReceivedAt: number;
}) {
  return {
    runId: options.runId,
    accepted: true,
    resultStatus: "ACCEPTED",
    rewardClaimId: null,
    rewardPreviewId: options.rewardPreviewId,
    serverReceivedAt: toIso(options.serverReceivedAt),
    balanceVersion: options.balanceVersion,
    message: null,
    errorCode: null,
  };
}

function createRunFinishDuplicateReplayResponse(options: {
  runId: string;
  rewardPreviewId: string;
  balanceVersion?: string | null;
  serverReceivedAt: number;
}) {
  return {
    runId: options.runId,
    accepted: false,
    resultStatus: "DUPLICATE_REPLAY",
    rewardClaimId: null,
    rewardPreviewId: options.rewardPreviewId,
    serverReceivedAt: toIso(options.serverReceivedAt),
    balanceVersion: options.balanceVersion ?? null,
    message: "Run finish was already accepted.",
    errorCode: "duplicate_replay",
  };
}

function parseRunFinishBody(value: unknown, pathRunId: string): RunFinishBody | null {
  if (typeof value !== "object" || value === null || Array.isArray(value)) return null;
  const body = value as Record<string, unknown>;
  for (const forbiddenField of runFinishForbiddenRewardFields) {
    if (Object.hasOwn(body, forbiddenField)) return null;
  }
  if (body.runId !== pathRunId) return null;
  const completedWaveCount = body.completedWaveCount;
  if (body.currentStage !== "Complete") return null;
  if (body.currentWaveNumber !== 0) return null;
  if (
    typeof completedWaveCount !== "number" ||
    !Number.isInteger(completedWaveCount) ||
    completedWaveCount < 0 ||
    completedWaveCount > 15
  ) {
    return null;
  }
  if (body.outcome !== "VICTORY" && body.outcome !== "FAILURE" && body.outcome !== "ABANDONED") {
    return null;
  }
  if (typeof body.runtimeStateJson !== "string" || typeof body.clientCompletedAt !== "string") {
    return null;
  }
  if (!Array.isArray(body.tacticSelections)) return null;
  if (containsForbiddenRunFinishField(body.tacticSelections, runFinishNestedForbiddenFields)) return null;
  let encounterType: "ORDINARY_15_WAVE" | "RAID_BOSS" | undefined;
  if (body.encounterType !== undefined) {
    if (body.encounterType !== "ORDINARY_15_WAVE" && body.encounterType !== "RAID_BOSS") return null;
    encounterType = body.encounterType;
  }
  let raidResult: RunFinishBody["raidResult"];
  if (body.raidResult !== undefined) {
    if (typeof body.raidResult !== "object" || body.raidResult === null || Array.isArray(body.raidResult)) return null;
    const candidate = body.raidResult as Record<string, unknown>;
    if (typeof candidate.bossDefeated !== "boolean") return null;
    if (candidate.raidId !== undefined && typeof candidate.raidId !== "string") return null;
    if (candidate.bossId !== undefined && typeof candidate.bossId !== "string") return null;
    if (
      candidate.completedPhaseCount !== undefined &&
      (
        typeof candidate.completedPhaseCount !== "number" ||
        !Number.isInteger(candidate.completedPhaseCount) ||
        candidate.completedPhaseCount < 0
      )
    ) {
      return null;
    }
    if (
      candidate.encounterDurationSeconds !== undefined &&
      (
        typeof candidate.encounterDurationSeconds !== "number" ||
        !Number.isFinite(candidate.encounterDurationSeconds) ||
        candidate.encounterDurationSeconds < 0
      )
    ) {
      return null;
    }
    raidResult = {
      bossDefeated: candidate.bossDefeated,
      ...(typeof candidate.raidId === "string" ? { raidId: candidate.raidId } : {}),
      ...(typeof candidate.bossId === "string" ? { bossId: candidate.bossId } : {}),
      ...(typeof candidate.completedPhaseCount === "number" ? { completedPhaseCount: candidate.completedPhaseCount } : {}),
      ...(typeof candidate.encounterDurationSeconds === "number" ? { encounterDurationSeconds: candidate.encounterDurationSeconds } : {}),
    };
  }
  if (encounterType === "RAID_BOSS" && raidResult === undefined) return null;
  if (encounterType !== "RAID_BOSS" && raidResult !== undefined) return null;
  return {
    runId: body.runId,
    currentStage: body.currentStage,
    currentWaveNumber: body.currentWaveNumber,
    completedWaveCount,
    ...(encounterType === undefined ? {} : { encounterType }),
    ...(raidResult === undefined ? {} : { raidResult }),
    outcome: body.outcome,
    runtimeStateJson: body.runtimeStateJson,
    tacticSelections: body.tacticSelections,
    clientCompletedAt: body.clientCompletedAt,
  };
}

function containsForbiddenRunFinishField(value: unknown, forbiddenFields: Set<string>): boolean {
  if (Array.isArray(value)) {
    return value.some((item) => containsForbiddenRunFinishField(item, forbiddenFields));
  }
  if (typeof value !== "object" || value === null) return false;
  for (const [key, nestedValue] of Object.entries(value as Record<string, unknown>)) {
    if (forbiddenFields.has(key)) return true;
    if (containsForbiddenRunFinishField(nestedValue, forbiddenFields)) return true;
  }
  return false;
}

function getTacticSelectionPackageRefs(tacticSelections: unknown[]): string[] {
  const packageRefs: string[] = [];
  for (const selection of tacticSelections) {
    if (typeof selection !== "object" || selection === null || Array.isArray(selection)) continue;
    const candidate = selection as Record<string, unknown>;
    if (typeof candidate.packageRef === "string" && candidate.packageRef.trim() !== "") {
      packageRefs.push(candidate.packageRef);
    }
  }
  return packageRefs;
}

function parseRunPreviewMetadata(previewJson: string, objective: string): {
  operationId: string;
  actId: string;
  regionId: string;
  encounterType: "ORDINARY_15_WAVE" | "RAID_BOSS";
  raidId: string | null;
  bossId: string | null;
  startAugmentPackageRef: string | null;
  startTacticPackageRef: string | null;
} {
  try {
    const parsed = JSON.parse(previewJson) as Record<string, unknown>;
    return {
      operationId: typeof parsed.operationId === "string" ? parsed.operationId : objective,
      actId: typeof parsed.actId === "string" ? parsed.actId : "act_01_wasteland_railway",
      regionId: typeof parsed.regionId === "string" ? parsed.regionId : "region_wasteland_railway",
      encounterType: parsed.encounterType === "RAID_BOSS" ? "RAID_BOSS" : "ORDINARY_15_WAVE",
      raidId: typeof parsed.raidId === "string" ? parsed.raidId : null,
      bossId: typeof parsed.bossId === "string" ? parsed.bossId : null,
      startAugmentPackageRef: typeof parsed.startAugmentPackageRef === "string" ? parsed.startAugmentPackageRef : null,
      startTacticPackageRef: typeof parsed.startTacticPackageRef === "string" ? parsed.startTacticPackageRef : null,
    };
  } catch {
    return {
      operationId: objective,
      actId: "act_01_wasteland_railway",
      regionId: "region_wasteland_railway",
      encounterType: "ORDINARY_15_WAVE",
      raidId: null,
      bossId: null,
      startAugmentPackageRef: null,
      startTacticPackageRef: null,
    };
  }
}

function isRunFullCompletion(body: RunFinishBody, previewMetadata: { encounterType: "ORDINARY_15_WAVE" | "RAID_BOSS" }): boolean {
  if (body.outcome !== "VICTORY") return false;
  if (previewMetadata.encounterType === "RAID_BOSS") {
    return body.encounterType === "RAID_BOSS" && body.raidResult?.bossDefeated === true;
  }
  return body.completedWaveCount === 15;
}

function parseRunRisk(value: string): RunRiskTier | null {
  if (value === "I" || value === "II" || value === "III" || value === "IV" || value === "V") {
    return value;
  }
  return null;
}

function getActOneStoryProgressOrder(operationId: string): number | null {
  const index = actOneStoryProgressOrder.indexOf(operationId as ActOneStoryProgressId);
  return index < 0 ? null : index;
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

function uniqueStrings(values: string[]): string[] {
  return [...new Set(values)];
}

function sortActOneOperationIds(values: string[]): string[] {
  return uniqueStrings(values)
    .filter((value) => actOneOperationIds.includes(value as typeof actOneOperationIds[number]))
    .sort((left, right) => (getActOneStoryProgressOrder(left) ?? 999) - (getActOneStoryProgressOrder(right) ?? 999));
}

function createStoryProgressRejectedResponse(options: {
  errorCode: string;
  message: string;
}) {
  return {
    accepted: false,
    actId: "act_01_wasteland_railway",
    operationId: null,
    storyProgress: null,
    firstClear: null,
    clearedOperationIds: [],
    narrativeFlags: [],
    storyFlags: [],
    unlockedOperations: [],
    narrativeVersion: actOneNarrativeVersion,
    narrative: null,
    lastProgressRunId: null,
    updatedAt: null,
    errorCode: options.errorCode,
    message: options.message,
  };
}

function createStoryProgressAcceptedResponse(options: {
  row: (AccountStoryProgressRow & {
    act_id: string;
    highest_cleared_operation_id: string;
    narrative_version: string | null;
    last_progress_run_id: string | null;
    updated_at: number;
  }) | null;
  unlockedOperationIds: string[];
}) {
  const narrativeVersion = options.row?.narrative_version ?? actOneNarrativeVersion;
  if (options.row === null) {
    return {
      accepted: true,
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
      narrativeVersion,
      narrative: {
        narrativeVersion,
        contentStatus: "CANDIDATE",
        localizationStatus: "PENDING",
        fixtureStatus: "PARTIAL",
        missingContentStatus: "UNKNOWN",
      },
      lastProgressRunId: null,
      updatedAt: null,
      errorCode: null,
      message: null,
    };
  }

  const storyFlags = parseJsonStringArray(options.row.story_flags_json);
  const unlockedOperations = sortActOneOperationIds([
    "opn_01_last_departure",
    ...options.unlockedOperationIds,
  ]);

  return {
    accepted: true,
    actId: options.row.act_id,
    operationId: options.row.highest_story_progress_id,
    storyProgress: {
      highestStoryProgressId: options.row.highest_story_progress_id,
      highestClearedOperationId: options.row.highest_cleared_operation_id,
      order: getActOneStoryProgressOrder(options.row.highest_story_progress_id) ?? 0,
    },
    firstClear: null,
    clearedOperationIds: sortActOneOperationIds(parseJsonStringArray(options.row.cleared_operations_json)),
    narrativeFlags: storyFlags,
    storyFlags,
    unlockedOperations,
    narrativeVersion,
    narrative: {
      narrativeVersion,
      contentStatus: "CANDIDATE",
      localizationStatus: "PENDING",
      fixtureStatus: "PARTIAL",
      missingContentStatus: "UNKNOWN",
    },
    lastProgressRunId: options.row.last_progress_run_id,
    updatedAt: toIso(options.row.updated_at),
    errorCode: null,
    message: null,
  };
}

function createOfflineRewardRejectedResponse(options: {
  errorCode: string;
  message: string;
}) {
  return {
    accepted: false,
    previewStatus: "REJECTED",
    storyProgressId: null,
    claimableSeconds: 0,
    claimableHours: 0,
    offlineStorageCapHours: null,
    minimumClaimSeconds: launchOfflineRewardsConfig.minimumOfflineMinutes * 60,
    maxClaimableHours: launchOfflineRewardsConfig.maxClaimableHours,
    rewardLines: [],
    calculation: null,
    narrativeVersion: actOneNarrativeVersion,
    serverCalculatedAt: null,
    errorCode: options.errorCode,
    message: options.message,
  };
}

function createOfflineRewardAcceptedResponse(options: {
  storyProgressId: string;
  rewardLines: ReturnType<typeof calculateOfflineRewardPreview>["rewardLines"];
  claimableSeconds: number;
  offlineStorageCapHours: number;
  minimumOfflineMinutes: number;
  maxClaimableHours: number;
  storyProgressMultiplierBps: number;
  narrativeVersion: string;
  serverCalculatedAt: number;
}) {
  return {
    accepted: true,
    previewStatus: options.rewardLines.length === 0 ? "EMPTY" : "AVAILABLE",
    storyProgressId: options.storyProgressId,
    claimableSeconds: options.claimableSeconds,
    claimableHours: options.claimableSeconds / 3600,
    offlineStorageCapHours: options.offlineStorageCapHours,
    minimumClaimSeconds: options.minimumOfflineMinutes * 60,
    maxClaimableHours: options.maxClaimableHours,
    rewardLines: options.rewardLines,
    calculation: {
      storyProgressId: options.storyProgressId,
      storyProgressMultiplierLabel: `${(options.storyProgressMultiplierBps / 10000).toFixed(2)}x`,
      cappedOfflineSeconds: options.claimableSeconds,
      capSource: options.claimableSeconds >= options.offlineStorageCapHours * 3600
        ? "STORY_PROGRESS_STORAGE_CAP"
        : "ACTUAL_OFFLINE_DURATION",
    },
    narrativeVersion: options.narrativeVersion,
    serverCalculatedAt: toIso(options.serverCalculatedAt),
    errorCode: null,
    message: null,
  };
}

function createOfflineRewardClaimRejectedResponse(options: {
  storyProgressId?: string | null;
  claimableSeconds?: number;
  offlineStorageCapHours?: number | null;
  serverClaimedAt?: number | null;
  errorCode: string;
  message: string;
}) {
  const claimableSeconds = options.claimableSeconds ?? 0;
  return {
    accepted: false,
    claimStatus: "REJECTED",
    offlineRewardClaimId: null,
    rewardTransactionId: null,
    economyTransactionId: null,
    accountMutationId: null,
    storyProgressId: options.storyProgressId ?? null,
    claimableSeconds,
    claimableHours: claimableSeconds / 3600,
    offlineStorageCapHours: options.offlineStorageCapHours ?? null,
    minimumClaimSeconds: launchOfflineRewardsConfig.minimumOfflineMinutes * 60,
    maxClaimableHours: launchOfflineRewardsConfig.maxClaimableHours,
    rewardLines: [],
    calculation: null,
    serverClaimedAt: options.serverClaimedAt === null || options.serverClaimedAt === undefined
      ? null
      : toIso(options.serverClaimedAt),
    narrativeVersion: actOneNarrativeVersion,
    errorCode: options.errorCode,
    message: options.message,
  };
}

function createOfflineRewardClaimDuplicateReplayResponse(options: {
  offlineRewardClaimId: string;
  rewardJson: string | null;
  serverClaimedAt: number;
}) {
  let parsed: Record<string, unknown> = {};
  try {
    parsed = options.rewardJson === null ? {} : JSON.parse(options.rewardJson) as Record<string, unknown>;
  } catch {
    parsed = {};
  }

  return {
    accepted: false,
    claimStatus: "DUPLICATE_REPLAY",
    offlineRewardClaimId: options.offlineRewardClaimId,
    rewardTransactionId: parsed.rewardTransactionId ?? options.offlineRewardClaimId,
    economyTransactionId: parsed.economyTransactionId ?? null,
    accountMutationId: parsed.accountMutationId ?? null,
    storyProgressId: parsed.storyProgressId ?? null,
    claimableSeconds: null,
    claimableHours: null,
    offlineStorageCapHours: parsed.offlineStorageCapHours ?? null,
    minimumClaimSeconds: launchOfflineRewardsConfig.minimumOfflineMinutes * 60,
    maxClaimableHours: launchOfflineRewardsConfig.maxClaimableHours,
    rewardLines: [],
    calculation: null,
    serverClaimedAt: toIso(options.serverClaimedAt),
    narrativeVersion: parsed.narrativeVersion ?? actOneNarrativeVersion,
    errorCode: "duplicate_replay",
    message: "Offline reward claim was already committed.",
  };
}

function createStoryProgressPlan(options: {
  accountId: string;
  runId: string;
  operationId: string;
  actId: string;
  existing: AccountStoryProgressRow | null;
  now: number;
}): null | {
  highestStoryProgressId: string;
  highestClearedOperationId: string;
  clearedOperations: string[];
  storyFlags: string[];
  nextOperationUnlockId: string | null;
} {
  const newOrder = getActOneStoryProgressOrder(options.operationId);
  if (newOrder === null || newOrder === 0) return null;
  const existingOrder = options.existing === null
    ? 0
    : getActOneStoryProgressOrder(options.existing.highest_story_progress_id) ?? 0;
  if (newOrder <= existingOrder) return null;

  const previousCleared = options.existing === null
    ? []
    : parseJsonStringArray(options.existing.cleared_operations_json);
  const previousFlags = options.existing === null
    ? []
    : parseJsonStringArray(options.existing.story_flags_json);
  const clearedOperations = uniqueStrings([
    ...previousCleared,
    ...actOneOperationIds.slice(0, newOrder),
  ]);
  const storyFlags = uniqueStrings([
    ...previousFlags,
    `operation_cleared:${options.operationId}`,
  ]);
  const nextOperationId = actOneOperationIds[newOrder] ?? null;

  return {
    highestStoryProgressId: options.operationId,
    highestClearedOperationId: options.operationId === "raid_raider_king_engine"
      ? "opn_06_broken_line"
      : options.operationId,
    clearedOperations,
    storyFlags,
    nextOperationUnlockId: nextOperationId === null ? null : `operation:${nextOperationId}`,
  };
}

function getFinishedRunStatus(outcome: RunOutcome): "COMPLETED" | "FAILED" | "ABANDONED" {
  if (outcome === "VICTORY") return "COMPLETED";
  if (outcome === "ABANDONED") return "ABANDONED";
  return "FAILED";
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

function getGoogleAudiences(env: Env): string[] {
  return ((env as GameApiEnv).GOOGLE_AUTH_AUDIENCES ?? "")
    .split(",")
    .map((audience) => audience.trim())
    .filter((audience) => audience.length > 0);
}

function getGoogleVerifier(env: Env): GoogleIdTokenVerifier | null {
  return (env as GameApiEnv).GOOGLE_ID_TOKEN_VERIFIER ?? null;
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

function isAuthFailure(value: unknown): value is AuthFailure {
  return typeof value === "object" && value !== null && "errorCode" in value;
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
            sessions.expires_at AS session_expires_at, accounts.status AS account_status,
            accounts.created_at AS account_created_at
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
    accountCreatedAt: typeof row.account_created_at === "number" ? row.account_created_at : Date.now(),
  };
}

async function createAuthTokenBundle(options: {
  env: Env;
  secret: string;
  accountId: string;
  now: number;
}) {
  const sessionId = createDomainId("session");
  const familyId = createDomainId("family");
  const refreshToken = createRefreshToken();
  const refreshTokenHash = await hashRefreshToken(refreshToken);
  const accessTokenTtlMs = 15 * 60 * 1000;
  const refreshTokenTtlMs = 30 * 24 * 60 * 60 * 1000;
  const accessTokenExpiresAt = options.now + accessTokenTtlMs;
  const refreshTokenExpiresAt = options.now + refreshTokenTtlMs;
  const accessToken = await createAccessToken({
    secret: options.secret,
    accountId: options.accountId,
    sessionId,
    nowMs: options.now,
    ttlMs: accessTokenTtlMs,
  });

  return {
    sessionId,
    familyId,
    refreshToken,
    refreshTokenHash,
    refreshTokenExpiresAt,
    response: {
      tokenType: "Bearer",
      account: {
        id: options.accountId,
        status: "ACTIVE",
      },
      accessToken,
      accessTokenExpiresAt: toIso(accessTokenExpiresAt),
      refreshToken,
      refreshTokenExpiresAt: toIso(refreshTokenExpiresAt),
    },
  };
}

async function verifyGoogleIdentity(c: GameApiContext, idToken: string): Promise<{ subject: string } | AuthFailure> {
  const audiences = getGoogleAudiences(c.env);
  const verifier = getGoogleVerifier(c.env);
  if (audiences.length === 0 || verifier === null) {
    await writeGameApiOpsErrorLog(c, {
      severity: "critical",
      errorCode: "google_auth_config_missing",
      messageSummary: "Google auth verifier or audience configuration is missing.",
      context: {
        hasAudiences: audiences.length > 0,
        hasVerifier: verifier !== null,
      },
      retryable: false,
    });
    return createAuthFailure(500, "google_auth_not_configured", "Google auth verifier is not configured.");
  }

  let claims: GoogleIdentityClaims;
  try {
    claims = await verifier.verifyIdToken(idToken, audiences);
  } catch {
    return createAuthFailure(401, "google_id_token_invalid", "Google ID token is invalid.");
  }

  if (typeof claims.subject !== "string" || claims.subject.trim() === "") {
    return createAuthFailure(401, "google_id_token_invalid", "Google ID token subject is invalid.");
  }

  return { subject: claims.subject };
}

async function parseGoogleAuthBody(c: GameApiContext): Promise<{ idToken: string } | AuthFailure> {
  if (Object.keys(c.req.query()).length > 0) {
    return createAuthFailure(400, "contract_violation", "Query parameters are not accepted.");
  }

  let body: Record<string, unknown>;
  try {
    const parsed = await c.req.json();
    body = typeof parsed === "object" && parsed !== null ? parsed as Record<string, unknown> : {};
  } catch {
    body = {};
  }

  if (Object.hasOwn(body, "accountId") || Object.hasOwn(body, "account_id")) {
    return createAuthFailure(400, "contract_violation", "accountId must not be supplied by the client.");
  }

  if (typeof body.idToken !== "string" || body.idToken.trim() === "") {
    return createAuthFailure(400, "contract_violation", "idToken is required.");
  }

  return { idToken: body.idToken };
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
    c.env.DB.prepare(
      `INSERT INTO account_offline_reward_state (
         account_id, last_offline_reward_claim_at, last_story_progress_id,
         last_balance_config_version, last_narrative_version, last_previewed_at, revision, updated_at
       ) VALUES (?, ?, ?, NULL, ?, NULL, 0, ?)`,
    ).bind(accountId, now, "none", actOneNarrativeVersion, now),
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
app.post("/v1/auth/google", async (c) => {
  const secret = getAuthSecret(c.env);
  if (secret === null || secret.trim() === "") {
    return c.json(createAuthError("auth_not_configured", "Auth signing key is not configured."), 500);
  }

  const body = await parseGoogleAuthBody(c);
  if (isAuthFailure(body)) {
    return c.json(createAuthError(body.errorCode, body.message), body.status);
  }

  const identity = await verifyGoogleIdentity(c, body.idToken);
  if (isAuthFailure(identity)) {
    return c.json(createAuthError(identity.errorCode, identity.message), identity.status);
  }

  const identityRow = await c.env.DB.prepare(
    `SELECT identities.account_id, accounts.status AS account_status
     FROM auth_identities identities
     JOIN accounts ON accounts.id = identities.account_id
     WHERE identities.provider = 'GOOGLE' AND identities.provider_subject = ?`,
  )
    .bind(identity.subject)
    .first<AuthIdentityAccountRow>();

  const now = Date.now();
  if (identityRow !== null) {
    if (identityRow.account_status !== "ACTIVE") {
      return c.json(createAuthError("account_not_active", "Account is not active."), 403);
    }

    const bundle = await createAuthTokenBundle({
      env: c.env,
      secret,
      accountId: identityRow.account_id,
      now,
    });

    await c.env.DB.batch([
      c.env.DB.prepare(
        `INSERT INTO auth_sessions (id, account_id, refresh_token_hash, family_id, generation, status, created_at, expires_at, rotated_at)
         VALUES (?, ?, ?, ?, 0, 'ACTIVE', ?, ?, NULL)`,
      ).bind(bundle.sessionId, identityRow.account_id, bundle.refreshTokenHash, bundle.familyId, now, bundle.refreshTokenExpiresAt),
    ]);

    return c.json(bundle.response);
  }

  const accountId = createDomainId("account");
  const identityId = createDomainId("identity");
  const bundle = await createAuthTokenBundle({
    env: c.env,
    secret,
    accountId,
    now,
  });

  await c.env.DB.batch([
    c.env.DB.prepare(
      `INSERT INTO accounts (id, status, created_at, updated_at)
       VALUES (?, 'ACTIVE', ?, ?)`,
    ).bind(accountId, now, now),
    c.env.DB.prepare(
      `INSERT INTO auth_identities (id, account_id, provider, provider_subject, linked_at)
       VALUES (?, ?, 'GOOGLE', ?, ?)`,
    ).bind(identityId, accountId, identity.subject, now),
    c.env.DB.prepare(
      `INSERT INTO auth_sessions (id, account_id, refresh_token_hash, family_id, generation, status, created_at, expires_at, rotated_at)
       VALUES (?, ?, ?, ?, 0, 'ACTIVE', ?, ?, NULL)`,
    ).bind(bundle.sessionId, accountId, bundle.refreshTokenHash, bundle.familyId, now, bundle.refreshTokenExpiresAt),
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
    c.env.DB.prepare(
      `INSERT INTO account_offline_reward_state (
         account_id, last_offline_reward_claim_at, last_story_progress_id,
         last_balance_config_version, last_narrative_version, last_previewed_at, revision, updated_at
       ) VALUES (?, ?, ?, NULL, ?, NULL, 0, ?)`,
    ).bind(accountId, now, "none", actOneNarrativeVersion, now),
  ]);

  return c.json(bundle.response);
});
app.post("/v1/auth/google/link", async (c) => {
  const secret = getAuthSecret(c.env);
  if (secret === null || secret.trim() === "") {
    return c.json(createAuthError("auth_not_configured", "Auth signing key is not configured."), 500);
  }

  const body = await parseGoogleAuthBody(c);
  if (isAuthFailure(body)) {
    return c.json(createAuthError(body.errorCode, body.message), body.status);
  }

  const auth = await resolveAuthContext(c);
  if (isAuthFailure(auth)) {
    return c.json(createAuthError(auth.errorCode, auth.message), auth.status);
  }

  const identity = await verifyGoogleIdentity(c, body.idToken);
  if (isAuthFailure(identity)) {
    return c.json(createAuthError(identity.errorCode, identity.message), identity.status);
  }

  const identityRow = await c.env.DB.prepare(
    `SELECT identities.account_id, accounts.status AS account_status
     FROM auth_identities identities
     JOIN accounts ON accounts.id = identities.account_id
     WHERE identities.provider = 'GOOGLE' AND identities.provider_subject = ?`,
  )
    .bind(identity.subject)
    .first<AuthIdentityAccountRow>();

  if (identityRow !== null && identityRow.account_id !== auth.accountId) {
    return c.json(createAuthError("google_identity_already_linked", "Google identity is already linked to another account."), 409);
  }

  const now = Date.now();
  const bundle = await createAuthTokenBundle({
    env: c.env,
    secret,
    accountId: auth.accountId,
    now,
  });
  const linkStatus = identityRow === null ? "LINKED" : "ALREADY_LINKED";
  const statements = [
    c.env.DB.prepare(
      `INSERT INTO auth_sessions (id, account_id, refresh_token_hash, family_id, generation, status, created_at, expires_at, rotated_at)
       VALUES (?, ?, ?, ?, 0, 'ACTIVE', ?, ?, NULL)`,
    ).bind(bundle.sessionId, auth.accountId, bundle.refreshTokenHash, bundle.familyId, now, bundle.refreshTokenExpiresAt),
  ];

  if (identityRow === null) {
    statements.unshift(
      c.env.DB.prepare(
        `INSERT INTO auth_identities (id, account_id, provider, provider_subject, linked_at)
         VALUES (?, ?, 'GOOGLE', ?, ?)`,
      ).bind(createDomainId("identity"), auth.accountId, identity.subject, now),
    );
  }

  await c.env.DB.batch(statements);

  return c.json({
    ...bundle.response,
    linkStatus,
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
app.get("/v1/story-progress", async (c) => {
  const hasForbiddenAccountQuery = c.req.query("accountId") !== undefined || c.req.query("account_id") !== undefined;
  const contentLength = c.req.header("content-length");
  if (hasForbiddenAccountQuery || (contentLength !== undefined && contentLength !== "0")) {
    return c.json(createStoryProgressRejectedResponse({
      errorCode: "contract_violation",
      message: "GET /v1/story-progress must not include account id query parameters or a request body.",
    }), 400);
  }

  const authContext = await resolveAuthContext(c);
  if (isAuthFailure(authContext)) {
    return c.json(createStoryProgressRejectedResponse({
      errorCode: authContext.errorCode,
      message: authContext.message,
    }), authContext.status);
  }

  const storyProgress = await c.env.DB.prepare(
    `SELECT act_id, highest_cleared_operation_id, highest_story_progress_id,
            cleared_operations_json, story_flags_json, narrative_version,
            last_progress_run_id, updated_at
     FROM account_story_progress
     WHERE account_id = ?`,
  )
    .bind(authContext.accountId)
    .first<AccountStoryProgressRow & {
      act_id: string;
      highest_cleared_operation_id: string;
      narrative_version: string | null;
      last_progress_run_id: string | null;
      updated_at: number;
    }>();

  if (storyProgress === null) {
    return c.json(createStoryProgressAcceptedResponse({
      row: null,
      unlockedOperationIds: [],
    }));
  }

  const unlockRows = await c.env.DB.prepare(
    `SELECT unlock_id
     FROM account_unlocks
     WHERE account_id = ? AND unlock_id LIKE 'operation:%'
     ORDER BY unlocked_at, unlock_id`,
  )
    .bind(authContext.accountId)
    .all<AccountUnlockRow>();
  const unlockedOperationIds = unlockRows.results
    .map((row) => row.unlock_id.startsWith("operation:") ? row.unlock_id.slice("operation:".length) : null)
    .filter((value): value is string => value !== null);

  return c.json(createStoryProgressAcceptedResponse({
    row: storyProgress,
    unlockedOperationIds,
  }));
});
app.get("/v1/offline-rewards/preview", async (c) => {
  const requestUrl = new URL(c.req.url);
  const hasForbiddenQuery = offlineRewardForbiddenQueryKeys.some((key) => requestUrl.searchParams.has(key));
  const hasAnyQuery = [...requestUrl.searchParams.keys()].length > 0;
  const contentLength = c.req.header("content-length");
  if (hasForbiddenQuery || hasAnyQuery || (contentLength !== undefined && contentLength !== "0")) {
    return c.json(createOfflineRewardRejectedResponse({
      errorCode: "contract_violation",
      message: "GET /v1/offline-rewards/preview must not include query parameters or a request body.",
    }), 400);
  }

  const authContext = await resolveAuthContext(c);
  if (isAuthFailure(authContext)) {
    return c.json(createOfflineRewardRejectedResponse({
      errorCode: authContext.errorCode,
      message: authContext.message,
    }), authContext.status);
  }

  const storyProgress = await c.env.DB.prepare(
    `SELECT highest_story_progress_id, narrative_version
     FROM account_story_progress
     WHERE account_id = ?`,
  )
    .bind(authContext.accountId)
    .first<Pick<AccountStoryProgressRow, "highest_story_progress_id" | "narrative_version">>();
  const offlineState = await c.env.DB.prepare(
    `SELECT last_offline_reward_claim_at, last_story_progress_id, last_balance_config_version,
            last_narrative_version, last_previewed_at, revision, updated_at
     FROM account_offline_reward_state
     WHERE account_id = ?`,
  )
    .bind(authContext.accountId)
    .first<AccountOfflineRewardStateRow>();

  const now = Date.now();
  const storyProgressId = storyProgress?.highest_story_progress_id ?? "none";
  const baselineMs = offlineState?.last_offline_reward_claim_at ?? authContext.accountCreatedAt;
  const actualOfflineSeconds = Math.floor((now - baselineMs) / 1000);

  try {
    const preview = calculateOfflineRewardPreview({
      config: launchOfflineRewardsConfig,
      storyProgressKey: storyProgressId,
      actualOfflineSeconds,
    });
    return c.json(createOfflineRewardAcceptedResponse({
      storyProgressId: preview.calculation.storyProgressKey,
      rewardLines: preview.rewardLines,
      claimableSeconds: preview.calculation.claimableSeconds,
      offlineStorageCapHours: preview.calculation.offlineStorageCapHours,
      minimumOfflineMinutes: preview.calculation.minimumOfflineMinutes,
      maxClaimableHours: launchOfflineRewardsConfig.maxClaimableHours,
      storyProgressMultiplierBps: preview.calculation.storyProgressMultiplierBps,
      narrativeVersion: storyProgress?.narrative_version ?? actOneNarrativeVersion,
      serverCalculatedAt: now,
    }));
  } catch (error) {
    const errorCode = error instanceof Error && error.message !== "" ? error.message : "offline_reward_policy_missing";
    const status = errorCode === "contract_violation" ? 400 : 409;
    return c.json(createOfflineRewardRejectedResponse({
      errorCode,
      message: "Offline reward preview is unavailable.",
    }), status);
  }
});

app.post("/v1/offline-rewards/claim", async (c) => {
  const requestUrl = new URL(c.req.url);
  const hasAnyQuery = [...requestUrl.searchParams.keys()].length > 0;
  const contentLength = c.req.header("content-length");
  const idempotencyKey = c.req.header("Idempotency-Key");

  if (idempotencyKey === undefined || idempotencyKey.trim() === "") {
    return c.json(createOfflineRewardClaimRejectedResponse({
      errorCode: "contract_violation",
      message: "Idempotency-Key header is required.",
    }), 400);
  }

  if (hasAnyQuery || (contentLength !== undefined && contentLength !== "0")) {
    return c.json(createOfflineRewardClaimRejectedResponse({
      errorCode: "contract_violation",
      message: "POST /v1/offline-rewards/claim must not include query parameters or a request body.",
    }), 400);
  }

  const authContext = await resolveAuthContext(c);
  if (isAuthFailure(authContext)) {
    return c.json(createOfflineRewardClaimRejectedResponse({
      errorCode: authContext.errorCode,
      message: authContext.message,
    }), authContext.status);
  }

  const replay = await c.env.DB.prepare(
    `SELECT idem.result_ref, idem.created_at, reward.created_at AS reward_created_at,
            reward.reward_json AS reward_json
     FROM idempotency_records idem
     LEFT JOIN reward_transactions reward ON reward.id = idem.result_ref
     WHERE idem.account_id = ? AND idem.operation = 'offline_reward_claim' AND idem.idempotency_key = ?`,
  )
    .bind(authContext.accountId, idempotencyKey)
    .first<OfflineRewardReplayRow>();

  if (replay !== null) {
    return c.json(createOfflineRewardClaimDuplicateReplayResponse({
      offlineRewardClaimId: replay.result_ref,
      rewardJson: replay.reward_json,
      serverClaimedAt: replay.reward_created_at ?? replay.created_at,
    }));
  }

  const storyProgress = await c.env.DB.prepare(
    `SELECT highest_story_progress_id, narrative_version
     FROM account_story_progress
     WHERE account_id = ?`,
  )
    .bind(authContext.accountId)
    .first<Pick<AccountStoryProgressRow, "highest_story_progress_id" | "narrative_version">>();
  const offlineState = await c.env.DB.prepare(
    `SELECT last_offline_reward_claim_at, last_story_progress_id, last_balance_config_version,
            last_narrative_version, last_previewed_at, revision, updated_at
     FROM account_offline_reward_state
     WHERE account_id = ?`,
  )
    .bind(authContext.accountId)
    .first<AccountOfflineRewardStateRow>();

  const now = Date.now();
  const baselineMs = offlineState?.last_offline_reward_claim_at ?? authContext.accountCreatedAt;
  if (baselineMs > now) {
    await writeGameApiOpsErrorLog(c, {
      severity: "error",
      errorCode: "offline_reward_state_conflict",
      messageSummary: "Offline reward state is ahead of server time.",
      accountId: authContext.accountId,
      storyProgressId: storyProgress?.highest_story_progress_id ?? "none",
      idempotencyOperation: "offline_reward_claim",
      idempotencyKey,
      correlation: {
        accountId: authContext.accountId,
        storyProgressId: storyProgress?.highest_story_progress_id ?? "none",
      },
      context: {
        component: "offline_rewards",
        stateAheadOfServerTime: true,
        hasOfflineRewardState: offlineState !== null,
      },
      retryable: false,
    });
    return c.json(createOfflineRewardClaimRejectedResponse({
      storyProgressId: storyProgress?.highest_story_progress_id ?? "none",
      errorCode: "offline_reward_state_conflict",
      message: "Offline reward state is ahead of server time.",
    }), 409);
  }

  const storyProgressId = storyProgress?.highest_story_progress_id ?? "none";
  const narrativeVersion = storyProgress?.narrative_version ?? actOneNarrativeVersion;
  const actualOfflineSeconds = Math.floor((now - baselineMs) / 1000);
  let preview;
  try {
    preview = calculateOfflineRewardPreview({
      config: launchOfflineRewardsConfig,
      storyProgressKey: storyProgressId,
      actualOfflineSeconds,
    });
  } catch (error) {
    const errorCode = error instanceof Error && error.message !== "" ? error.message : "offline_reward_policy_missing";
    const status = errorCode === "contract_violation" ? 400 : 409;
    return c.json(createOfflineRewardClaimRejectedResponse({
      storyProgressId,
      errorCode,
      message: "Offline reward claim is unavailable.",
    }), status);
  }

  if (preview.calculation.claimableSeconds === 0 || preview.rewardLines.length === 0) {
    return c.json(createOfflineRewardClaimRejectedResponse({
      storyProgressId: preview.calculation.storyProgressKey,
      claimableSeconds: preview.calculation.claimableSeconds,
      offlineStorageCapHours: preview.calculation.offlineStorageCapHours,
      errorCode: "offline_reward_not_claimable",
      message: "Offline reward is not claimable yet.",
    }), 409);
  }

  const offlineRewardClaimId = createDomainId("offline_claim");
  const mutationId = createDomainId("mutation");
  const economyTransactionId = createDomainId("economy");
  const latestMutation = await c.env.DB.prepare(
    `SELECT MAX(sequence) AS sequence
     FROM account_mutations
     WHERE account_id = ?`,
  )
    .bind(authContext.accountId)
    .first<AccountMutationSequenceRow>();
  const nextSequence = (latestMutation?.sequence ?? 0) + 1;
  const claimRewardLines = [];
  const statements = [
    c.env.DB.prepare(
      `INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at)
       VALUES (?, ?, ?, 'OFFLINE_REWARD_CLAIM', ?, ?)`,
    ).bind(mutationId, authContext.accountId, nextSequence, idempotencyKey, now),
    c.env.DB.prepare(
      `INSERT INTO economy_transactions (id, account_id, mutation_id, reason, source_type, source_id, created_at)
       VALUES (?, ?, ?, 'OFFLINE_REWARD_CLAIM', 'OFFLINE_REWARD_CLAIM', ?, ?)`,
    ).bind(economyTransactionId, authContext.accountId, mutationId, offlineRewardClaimId, now),
  ];

  for (const rewardLine of preview.rewardLines) {
    const currency = currencyIdToD1Currency[rewardLine.id as keyof typeof currencyIdToD1Currency];
    const current = await c.env.DB.prepare(
      `SELECT balance
       FROM account_currencies
       WHERE account_id = ? AND currency = ?`,
    )
      .bind(authContext.accountId, currency)
      .first<AccountCurrencyRow>();
    const balanceAfter = (current?.balance ?? 0) + rewardLine.quantity;
    const ledgerId = createDomainId("ledger");
    statements.push(
      c.env.DB.prepare(
        `INSERT INTO account_currencies (account_id, currency, balance, updated_at)
         VALUES (?, ?, ?, ?)
         ON CONFLICT(account_id, currency) DO UPDATE SET balance = excluded.balance, updated_at = excluded.updated_at`,
      ).bind(authContext.accountId, currency, balanceAfter, now),
      c.env.DB.prepare(
        `INSERT INTO currency_ledger (id, account_id, transaction_id, currency, delta, balance_after, created_at)
         VALUES (?, ?, ?, ?, ?, ?, ?)`,
      ).bind(ledgerId, authContext.accountId, economyTransactionId, currency, rewardLine.quantity, balanceAfter, now),
    );
    claimRewardLines.push({
      kind: rewardLine.kind,
      id: rewardLine.id,
      quantity: rewardLine.quantity,
      source: rewardLine.source,
      balanceAfter,
    });
  }

  const calculation = {
    storyProgressId: preview.calculation.storyProgressKey,
    storyProgressMultiplierLabel: `${(preview.calculation.storyProgressMultiplierBps / 10000).toFixed(2)}x`,
    cappedOfflineSeconds: preview.calculation.claimableSeconds,
    capSource: preview.calculation.claimableSeconds >= preview.calculation.offlineStorageCapHours * 3600
      ? "STORY_PROGRESS_STORAGE_CAP"
      : "ACTUAL_OFFLINE_DURATION",
  };
  const responsePayload = {
    accepted: true,
    claimStatus: "CLAIMED",
    offlineRewardClaimId,
    rewardTransactionId: offlineRewardClaimId,
    economyTransactionId,
    accountMutationId: mutationId,
    storyProgressId: preview.calculation.storyProgressKey,
    claimableSeconds: preview.calculation.claimableSeconds,
    claimableHours: preview.calculation.claimableSeconds / 3600,
    offlineStorageCapHours: preview.calculation.offlineStorageCapHours,
    minimumClaimSeconds: preview.calculation.minimumOfflineMinutes * 60,
    maxClaimableHours: launchOfflineRewardsConfig.maxClaimableHours,
    rewardLines: claimRewardLines,
    calculation,
    serverClaimedAt: toIso(now),
    narrativeVersion,
    errorCode: null,
    message: null,
  };
  const rewardJson = JSON.stringify({
    rewardLines: claimRewardLines,
    calculation,
    storyProgressId: preview.calculation.storyProgressKey,
    claimableSeconds: preview.calculation.claimableSeconds,
    claimableHours: preview.calculation.claimableSeconds / 3600,
    offlineStorageCapHours: preview.calculation.offlineStorageCapHours,
    minimumClaimSeconds: preview.calculation.minimumOfflineMinutes * 60,
    maxClaimableHours: launchOfflineRewardsConfig.maxClaimableHours,
    narrativeVersion,
    rewardTransactionId: offlineRewardClaimId,
    economyTransactionId,
    accountMutationId: mutationId,
  });

  statements.push(
    c.env.DB.prepare(
      `INSERT INTO reward_transactions (id, account_id, run_id, mutation_id, balance_config_version, reward_json, created_at)
       VALUES (?, ?, NULL, ?, NULL, ?, ?)`,
    ).bind(offlineRewardClaimId, authContext.accountId, mutationId, rewardJson, now),
    c.env.DB.prepare(
      `INSERT INTO account_offline_reward_state (
         account_id, last_offline_reward_claim_at, last_story_progress_id,
         last_balance_config_version, last_narrative_version, last_previewed_at, revision, updated_at
       ) VALUES (?, ?, ?, NULL, ?, NULL, 1, ?)
       ON CONFLICT(account_id) DO UPDATE SET
         last_offline_reward_claim_at = excluded.last_offline_reward_claim_at,
         last_story_progress_id = excluded.last_story_progress_id,
         last_balance_config_version = excluded.last_balance_config_version,
         last_narrative_version = excluded.last_narrative_version,
         revision = account_offline_reward_state.revision + 1,
         updated_at = excluded.updated_at`,
    ).bind(authContext.accountId, now, preview.calculation.storyProgressKey, narrativeVersion, now),
    c.env.DB.prepare(
      `INSERT INTO idempotency_records (id, account_id, idempotency_key, operation, result_ref, created_at)
       VALUES (?, ?, ?, 'offline_reward_claim', ?, ?)`,
    ).bind(createDomainId("idem"), authContext.accountId, idempotencyKey, offlineRewardClaimId, now),
  );

  await c.env.DB.batch(statements);

  return c.json(responsePayload);
});
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

  try {
    await c.env.DB.batch(statements);
  } catch {
    await writeGameApiOpsErrorLog(c, {
      severity: "error",
      errorCode: "gacha_state_conflict",
      messageSummary: "Gacha pull commit transaction failed.",
      accountId: authContext.accountId,
      gachaBannerId: bannerId,
      idempotencyOperation: "gacha_pull",
      idempotencyKey,
      correlation: {
        bannerId,
        batchId,
        pullCount,
      },
      context: {
        phase: "gacha_pull_commit",
        responseShape: "gacha_rejected",
      },
      retryable: true,
    });
    return c.json(createGachaRejectedResponse({
      bannerId,
      pullCount,
      errorCode: "gacha_state_conflict",
      message: "Gacha pull could not be committed safely.",
    }), 409);
  }

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

app.post("/v1/runs/:runId/finish", async (c) => {
  const runId = c.req.param("runId");
  const idempotencyKey = c.req.header("Idempotency-Key");

  if (idempotencyKey === undefined || idempotencyKey.trim() === "") {
    return c.json(createRunFinishRejectedResponse({
      runId,
      errorCode: "contract_violation",
      message: "Idempotency-Key header is required.",
    }), 400);
  }

  let requestJson: unknown;
  try {
    requestJson = await c.req.json();
  } catch {
    requestJson = null;
  }

  const body = parseRunFinishBody(requestJson, runId);
  if (body === null) {
    return c.json(createRunFinishRejectedResponse({
      runId,
      errorCode: "contract_violation",
      message: "Run finish request body is invalid.",
    }), 400);
  }

  const authContext = await resolveAuthContext(c);
  if (isAuthFailure(authContext)) {
    return c.json(createRunFinishRejectedResponse({
      runId,
      errorCode: authContext.errorCode,
      message: authContext.message,
    }), authContext.status);
  }

  const idempotencyRecord = await c.env.DB.prepare(
    `SELECT idem.result_ref, idem.created_at, NULL AS reward_created_at,
            preview.balance_config_version AS balance_config_version,
            preview.run_id AS preview_run_id
     FROM idempotency_records idem
     LEFT JOIN run_reward_previews preview ON preview.id = idem.result_ref
     WHERE idem.account_id = ? AND idem.operation = 'run_finish' AND idem.idempotency_key = ?`,
  )
    .bind(authContext.accountId, idempotencyKey)
    .first<IdempotencyRecordRow>();

  if (idempotencyRecord !== null) {
    if (idempotencyRecord.preview_run_id !== runId) {
      return c.json(createRunFinishRejectedResponse({
        runId,
        errorCode: "contract_violation",
        message: "Idempotency-Key was already used for another run finish.",
      }), 400);
    }
    return c.json(createRunFinishDuplicateReplayResponse({
      runId,
      rewardPreviewId: idempotencyRecord.result_ref,
      balanceVersion: idempotencyRecord.balance_config_version === null || idempotencyRecord.balance_config_version === undefined
        ? null
        : String(idempotencyRecord.balance_config_version),
      serverReceivedAt: idempotencyRecord.created_at,
    }));
  }

  const row = await c.env.DB.prepare(
    `SELECT runs.id, runs.account_id, runs.status, runs.balance_config_version,
            preview.objective, preview.risk, preview.preview_json,
            balance.config_json
     FROM runs
     JOIN operation_previews preview ON preview.id = runs.preview_id
     JOIN balance_config_versions balance ON balance.version = runs.balance_config_version
     WHERE runs.id = ?`,
  )
    .bind(runId)
    .first<RunFinishRow>();

  if (row === null) {
    return c.json(createRunFinishRejectedResponse({
      runId,
      errorCode: "run_not_found",
      message: "Run was not found.",
    }));
  }

  if (row.account_id !== authContext.accountId) {
    return c.json(createRunFinishRejectedResponse({
      runId,
      balanceVersion: String(row.balance_config_version),
      errorCode: "run_account_mismatch",
      message: "Run does not belong to the authenticated account.",
    }));
  }

  if (row.status !== "ACTIVE" && row.status !== "FINISH_PENDING") {
    return c.json(createRunFinishRejectedResponse({
      runId,
      balanceVersion: String(row.balance_config_version),
      errorCode: "run_state_conflict",
      message: "Run is not finishable.",
    }));
  }

  const risk = parseRunRisk(row.risk);
  if (risk === null) {
    return c.json(createRunFinishRejectedResponse({
      runId,
      balanceVersion: String(row.balance_config_version),
      errorCode: "risk_reward_policy_missing",
      message: "Run risk policy is invalid.",
    }));
  }

  let runRewardsConfig;
  try {
    const balanceConfig = JSON.parse(row.config_json) as { runRewards?: unknown };
    runRewardsConfig = parseRunRewardsConfig(balanceConfig.runRewards);
  } catch (error) {
    return c.json(createRunFinishRejectedResponse({
      runId,
      balanceVersion: String(row.balance_config_version),
      errorCode: error instanceof Error ? error.message : "reward_plan_unavailable",
      message: "Run reward policy is unavailable.",
    }));
  }

  const previewMetadata = parseRunPreviewMetadata(row.preview_json, row.objective);
  const allowedStartPackageRefs = [
    previewMetadata.startAugmentPackageRef,
    previewMetadata.startTacticPackageRef,
  ].filter((value): value is string => value !== null && value.trim() !== "");
  const submittedPackageRefs = getTacticSelectionPackageRefs(body.tacticSelections);
  if (
    submittedPackageRefs.length > 0 &&
    submittedPackageRefs.some((packageRef) => !allowedStartPackageRefs.includes(packageRef))
  ) {
    return c.json(createRunFinishRejectedResponse({
      runId,
      balanceVersion: String(row.balance_config_version),
      errorCode: "contract_violation",
      message: "Tactic selections must reference the server-authored start package.",
    }), 400);
  }
  if (previewMetadata.encounterType === "RAID_BOSS" && body.encounterType !== "RAID_BOSS") {
    return c.json(createRunFinishRejectedResponse({
      runId,
      balanceVersion: String(row.balance_config_version),
      errorCode: "contract_violation",
      message: "Raid boss run finish requires raid encounter metadata.",
    }), 400);
  }
  if (previewMetadata.encounterType === "ORDINARY_15_WAVE" && body.encounterType === "RAID_BOSS") {
    return c.json(createRunFinishRejectedResponse({
      runId,
      balanceVersion: String(row.balance_config_version),
      errorCode: "contract_violation",
      message: "Ordinary run finish must not submit raid encounter metadata.",
    }), 400);
  }
  const fullCompletion = isRunFullCompletion(body, previewMetadata);
  const rewardClearedWaves = previewMetadata.encounterType === "RAID_BOSS" && fullCompletion
    ? 15
    : body.completedWaveCount;
  const firstClearRecord = await c.env.DB.prepare(
    `SELECT cleared_at
     FROM account_first_clears
     WHERE account_id = ? AND operation_key = ? AND risk = ?`,
  )
    .bind(authContext.accountId, previewMetadata.operationId, risk)
    .first<FirstClearRow>();
  const firstClearEligible =
    firstClearRecord === null &&
    fullCompletion;
  const existingStoryProgress = fullCompletion
    ? await c.env.DB.prepare(
        `SELECT highest_story_progress_id, cleared_operations_json, story_flags_json
         FROM account_story_progress
         WHERE account_id = ?`,
      )
        .bind(authContext.accountId)
        .first<AccountStoryProgressRow>()
    : null;
  const rewardPreview = calculateRunRewardPreview({
    config: runRewardsConfig,
    operationId: previewMetadata.operationId,
    actId: previewMetadata.actId,
    regionId: previewMetadata.regionId,
    risk,
    outcome: body.outcome,
    clearedWaves: rewardClearedWaves,
    firstClearEligible,
    integrityDisposition: "ALLOW" satisfies IntegrityDisposition,
  });
  const now = Date.now();
  const rewardPreviewId = createDomainId("reward_preview");
  const storyProgressPlan = fullCompletion
    ? createStoryProgressPlan({
        accountId: authContext.accountId,
        runId,
        operationId: previewMetadata.operationId,
        actId: previewMetadata.actId,
        existing: existingStoryProgress,
        now,
      })
    : null;
  const storyProgressMutationId = storyProgressPlan === null ? null : createDomainId("mutation");
  const storyProgressLedgerId = storyProgressPlan === null ? null : createDomainId("progression");
  const latestMutation = storyProgressPlan === null
    ? null
    : await c.env.DB.prepare(
        `SELECT MAX(sequence) AS sequence
         FROM account_mutations
         WHERE account_id = ?`,
      )
        .bind(authContext.accountId)
        .first<AccountMutationSequenceRow>();
  const nextMutationSequence = (latestMutation?.sequence ?? 0) + 1;
  const rewardPlanJson = JSON.stringify({
    rewardLines: rewardPreview.rewardLines,
    calculation: {
      ...rewardPreview.calculation,
      balanceVersion: String(row.balance_config_version),
    },
  });

  const finishStatements = [
    c.env.DB.prepare(
      `INSERT INTO run_final_results (run_id, outcome, cleared_waves, payload_json, submitted_at)
       VALUES (?, ?, ?, ?, ?)`,
    ).bind(runId, body.outcome, body.completedWaveCount, JSON.stringify(body), now),
    c.env.DB.prepare(
      `UPDATE runs
       SET status = ?, finished_at = ?
       WHERE id = ? AND account_id = ?`,
    ).bind(getFinishedRunStatus(body.outcome), now, runId, authContext.accountId),
    c.env.DB.prepare(
      `INSERT INTO run_reward_previews (
         id, account_id, run_id, status, balance_config_version, reward_plan_json, expires_at, created_at, claimed_at, reward_claim_id
       ) VALUES (?, ?, ?, 'ACTIVE', ?, ?, ?, ?, NULL, NULL)`,
    ).bind(
      rewardPreviewId,
      authContext.accountId,
      runId,
      row.balance_config_version,
      rewardPlanJson,
      now + runRewardPreviewTtlMs,
      now,
    ),
    ...(rewardPreview.calculation.firstClearApplied
      ? [
          c.env.DB.prepare(
            `INSERT INTO account_first_clears (account_id, operation_key, risk, cleared_at, run_id)
             VALUES (?, ?, ?, ?, ?)`,
          ).bind(authContext.accountId, previewMetadata.operationId, risk, now, runId),
        ]
      : []),
    ...(storyProgressPlan === null || storyProgressMutationId === null || storyProgressLedgerId === null
      ? []
      : [
          c.env.DB.prepare(
            `INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at)
             VALUES (?, ?, ?, 'STORY_PROGRESS', ?, ?)`,
          ).bind(storyProgressMutationId, authContext.accountId, nextMutationSequence, `run_finish:${idempotencyKey}`, now),
          c.env.DB.prepare(
            `INSERT INTO account_story_progress (
               account_id, act_id, highest_cleared_operation_id, highest_story_progress_id,
               cleared_operations_json, story_flags_json, narrative_version, last_progress_run_id, updated_at
             ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
             ON CONFLICT(account_id) DO UPDATE SET
               act_id = excluded.act_id,
               highest_cleared_operation_id = excluded.highest_cleared_operation_id,
               highest_story_progress_id = excluded.highest_story_progress_id,
               cleared_operations_json = excluded.cleared_operations_json,
               story_flags_json = excluded.story_flags_json,
               narrative_version = excluded.narrative_version,
               last_progress_run_id = excluded.last_progress_run_id,
               updated_at = excluded.updated_at`,
          ).bind(
            authContext.accountId,
            previewMetadata.actId,
            storyProgressPlan.highestClearedOperationId,
            storyProgressPlan.highestStoryProgressId,
            JSON.stringify(storyProgressPlan.clearedOperations),
            JSON.stringify(storyProgressPlan.storyFlags),
            "act1-narrative-v1",
            runId,
            now,
          ),
          ...(storyProgressPlan.nextOperationUnlockId === null
            ? []
            : [
                c.env.DB.prepare(
                  `INSERT OR IGNORE INTO account_unlocks (account_id, unlock_id, unlocked_at)
                   VALUES (?, ?, ?)`,
                ).bind(authContext.accountId, storyProgressPlan.nextOperationUnlockId, now),
              ]),
          c.env.DB.prepare(
            `INSERT INTO progression_ledger (id, account_id, mutation_id, progression_type, target_id, delta_json, created_at)
             VALUES (?, ?, ?, 'STORY_PROGRESS', ?, ?, ?)`,
          ).bind(
            storyProgressLedgerId,
            authContext.accountId,
            storyProgressMutationId,
            storyProgressPlan.highestStoryProgressId,
            JSON.stringify({
              runId,
              actId: previewMetadata.actId,
              highestStoryProgressId: storyProgressPlan.highestStoryProgressId,
              highestClearedOperationId: storyProgressPlan.highestClearedOperationId,
              unlockedOperation: storyProgressPlan.nextOperationUnlockId,
            }),
            now,
          ),
        ]),
    c.env.DB.prepare(
      `INSERT INTO idempotency_records (id, account_id, idempotency_key, operation, result_ref, created_at)
       VALUES (?, ?, ?, 'run_finish', ?, ?)`,
    ).bind(createDomainId("idem"), authContext.accountId, idempotencyKey, rewardPreviewId, now),
  ];

  try {
    await c.env.DB.batch(finishStatements);
  } catch {
    if (storyProgressPlan !== null) {
      await writeGameApiOpsErrorLog(c, {
        severity: "error",
        errorCode: "story_progress_state_conflict",
        messageSummary: "Run finish story progress commit transaction failed.",
        accountId: authContext.accountId,
        runId,
        storyProgressId: storyProgressPlan.highestStoryProgressId,
        idempotencyOperation: "run_finish",
        idempotencyKey,
        correlation: {
          runId,
          operationId: previewMetadata.operationId,
          storyProgressId: storyProgressPlan.highestStoryProgressId,
        },
        context: {
          phase: "run_finish_story_progress_commit",
          responseShape: "run_finish_rejected",
        },
        retryable: true,
      });
      return c.json(createRunFinishRejectedResponse({
        runId,
        balanceVersion: String(row.balance_config_version),
        errorCode: "story_progress_state_conflict",
        message: "Story progress could not be committed safely.",
      }), 409);
    }
    return c.json(createRunFinishRejectedResponse({
      runId,
      balanceVersion: String(row.balance_config_version),
      errorCode: "run_state_conflict",
      message: "Run finish could not be committed safely.",
    }), 409);
  }

  return c.json(createRunFinishAcceptedResponse({
    runId,
    rewardPreviewId,
    balanceVersion: String(row.balance_config_version),
    serverReceivedAt: now,
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


