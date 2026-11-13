import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";
import { join } from "node:path";

const gameApiBaseUrl = "https://dev-api.lastrail.app";
const adminApiBaseUrl = "https://dev-admin.lastrail.app";
const d1ConfigPath = "apps/game-api/wrangler.jsonc";
const d1Env = "dev";
const d1Binding = "DB";

type JsonRecord = Record<string, unknown>;

function emit(check: string, status: number | string, details: JsonRecord = {}): void {
  console.log(JSON.stringify({ check, status, details }));
}

function sqlString(value: string): string {
  return `'${value.replaceAll("'", "''")}'`;
}

function d1Execute(sql: string): string {
  const wranglerBin = join(process.cwd(), "node_modules", "wrangler", "bin", "wrangler.js");
  try {
    return execFileSync(process.execPath, [
      wranglerBin,
      "d1",
      "execute",
      d1Binding,
      "--remote",
      "--env",
      d1Env,
      "--config",
      d1ConfigPath,
      "--command",
      sql,
    ], {
      cwd: process.cwd(),
      encoding: "utf8",
      stdio: ["ignore", "pipe", "pipe"],
      maxBuffer: 1024 * 1024 * 4,
    });
  } catch {
    throw new Error("D1 execute failed during DEV remote smoke. Wrangler stderr was scrubbed by the smoke runner.");
  }
}

function parseD1Json(stdout: string): Array<{ results?: JsonRecord[]; success?: boolean }> {
  const jsonStart = stdout.indexOf("[");
  if (jsonStart < 0) return [];
  return JSON.parse(stdout.slice(jsonStart)) as Array<{ results?: JsonRecord[]; success?: boolean }>;
}

function d1Query(sql: string): JsonRecord[] {
  const parsed = parseD1Json(d1Execute(sql));
  return parsed[0]?.results ?? [];
}

async function requestJson(method: string, path: string, options: {
  headers?: Record<string, string>;
  body?: unknown;
} = {}): Promise<{ status: number; body: JsonRecord }> {
  const headers = new Headers(options.headers ?? {});
  const init: RequestInit = { method, headers };
  if (options.body !== undefined) {
    headers.set("content-type", "application/json");
    init.body = JSON.stringify(options.body);
  }
  const response = await fetch(`${gameApiBaseUrl}${path}`, init);
  const body = await response.json().catch(() => ({})) as JsonRecord;
  return { status: response.status, body };
}

function safeAuthHeader(accessToken: unknown): Record<string, string> {
  if (typeof accessToken !== "string" || accessToken.trim() === "") {
    throw new Error("DEV smoke could not obtain an access token.");
  }
  return { Authorization: `Bearer ${accessToken}` };
}

function seedBalanceVersion(version: number, suffix: string, now: number): void {
  const configJson = JSON.stringify({
    fuel: { max: 100, runStartCost: 10, rechargeSeconds: 300 },
    runRewards: {
      schemaVersion: 1,
      riskMultipliersBps: { I: 8000, II: 10000, III: 12000, IV: 14500, V: 17500 },
      operationProgressionMultipliersBps: {
        opn_03_water_stop: 12000,
        raid_raider_king_engine: 18000,
      },
      outcomePolicy: {
        victoryFullClearBps: 10000,
        failureBaseBps: 2500,
        failureWaveBps: 5500,
        failureCapBps: 8000,
        abandonedBps: 0,
      },
      operations: {
        opn_03_water_stop: {
          baseRewards: [
            { kind: "CURRENCY", id: "cur_credits", quantity: 360 },
            { kind: "CURRENCY", id: "cur_operator_exp", quantity: 820 },
          ],
          firstClearRewards: [
            { kind: "CURRENCY", id: "cur_machine_parts", quantity: 160 },
            { kind: "ITEM", id: "cur_command_ticket", quantity: 1 },
          ],
        },
        raid_raider_king_engine: {
          baseRewards: [
            { kind: "CURRENCY", id: "cur_credits", quantity: 720 },
            { kind: "CURRENCY", id: "cur_operator_exp", quantity: 1600 },
          ],
          firstClearRewards: [
            { kind: "CURRENCY", id: "cur_special_core", quantity: 2 },
            { kind: "ITEM", id: "cur_command_ticket", quantity: 3 },
          ],
        },
      },
      integrityPolicy: {
        softAnomaly: "ALLOW_PREVIEW_WITH_FLAG",
        hardViolation: "INVALIDATE_PREVIEW",
        quarantinedRun: "INVALIDATE_PREVIEW",
      },
    },
  });
  d1Execute(`
    INSERT INTO balance_config_drafts (
      id, base_version, schema_version, required_content_version, config_json, checksum,
      revision, status, created_by, created_at, updated_at
    ) VALUES (
      ${sqlString(`dev-smoke-draft-${suffix}`)}, NULL, 1, 1, '{}',
      ${sqlString(`dev-smoke-checksum-${suffix}`)}, 1, 'PUBLISHED', 'dev-remote-smoke', ${now}, ${now}
    );
    INSERT INTO balance_config_versions (
      version, schema_version, required_content_version, checksum, config_json, published_at, published_by, draft_id
    ) VALUES (
      ${version}, 1, 1, ${sqlString(`dev-smoke-checksum-${suffix}`)},
      ${sqlString(configJson)}, ${now}, 'dev-remote-smoke', ${sqlString(`dev-smoke-draft-${suffix}`)}
    );
  `);
}

function seedOperationPreview(options: {
  accountId: string;
  mutationId: string;
  previewId: string;
  sequence: number;
  operationId: string;
  risk: string;
  balanceVersion: number;
  previewJson: JsonRecord;
  now: number;
}): void {
  d1Execute(`
    INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at)
    VALUES (
      ${sqlString(options.mutationId)}, ${sqlString(options.accountId)}, ${options.sequence},
      'RUN_PREVIEW', ${sqlString(`${options.previewId}-seed`)}, ${options.now}
    );
    INSERT INTO operation_previews (
      id, account_id, mutation_id, objective, risk, balance_config_version, preview_json, status, expires_at, created_at
    ) VALUES (
      ${sqlString(options.previewId)}, ${sqlString(options.accountId)}, ${sqlString(options.mutationId)},
      ${sqlString(options.operationId)}, ${sqlString(options.risk)}, ${options.balanceVersion},
      ${sqlString(JSON.stringify(options.previewJson))}, 'ACTIVE', ${options.now + 3_600_000}, ${options.now}
    );
  `);
}

function seedGacha(options: {
  accountId: string;
  bannerId: string;
  balanceVersion: number;
  now: number;
}): void {
  // DEV smoke uses a single-candidate pickup pool so result-shape and ledger
  // checks are deterministic. This is not production gacha weight evidence.
  const poolSnapshotJson = JSON.stringify({
    results: [
      { resultRarity: "ELITE", resultType: "ELITE_OPERATOR", resultId: "op_vex", weight: 1, isPickup: true },
    ],
  });
  d1Execute(`
    INSERT INTO account_items (account_id, item_id, quantity, updated_at)
    VALUES (${sqlString(options.accountId)}, 'cur_command_ticket', 12, ${options.now})
    ON CONFLICT(account_id, item_id) DO UPDATE SET quantity = excluded.quantity, updated_at = excluded.updated_at;
    INSERT INTO gacha_banners (
      id, banner_type, status, balance_config_version, pickup_operator_id,
      starts_at, ends_at, pity_config_json, pool_snapshot_json, created_at, updated_at
    ) VALUES (
      ${sqlString(options.bannerId)}, 'PICKUP', 'ACTIVE', ${options.balanceVersion}, 'op_vex',
      1, NULL, '{"elite":30,"pickup":60,"rare":10}', ${sqlString(poolSnapshotJson)}, ${options.now}, ${options.now}
    );
  `);
}

function setOfflineClaimBaseline(accountId: string, now: number): void {
  d1Execute(`
    UPDATE account_offline_reward_state
    SET last_offline_reward_claim_at = ${now - 3_600_000}, updated_at = ${now - 3_600_000}
    WHERE account_id = ${sqlString(accountId)};
  `);
}

function setFuelState(accountId: string, now: number): void {
  d1Execute(`
    INSERT INTO fuel_state (account_id, fuel, max_fuel, last_regen_at, updated_at)
    VALUES (${sqlString(accountId)}, 80, 100, ${now}, ${now})
    ON CONFLICT(account_id) DO UPDATE SET fuel = 80, max_fuel = 100, last_regen_at = excluded.last_regen_at, updated_at = excluded.updated_at;
  `);
}

async function main(): Promise<void> {
  const suffix = `${Date.now()}-${randomUUID().slice(0, 8)}`;
  const now = Date.now();
  const balanceVersion = 100_000_000 + Number(String(now).slice(-8));

  const status = await requestJson("GET", "/status");
  emit("game_status", status.status, { ok: status.body.ok === true });
  const clientConfig = await requestJson("GET", "/v1/client-config");
  emit("client_config", clientConfig.status, { environment: clientConfig.body.environment });

  const guest = await requestJson("POST", "/v1/auth/guest");
  const guestAccount = guest.body.account as JsonRecord | undefined;
  const accountId = typeof guestAccount?.id === "string" ? guestAccount.id : null;
  emit("guest_auth", guest.status, {
    tokenType: guest.body.tokenType,
    accountStatus: guestAccount?.status,
    hasAccessToken: typeof guest.body.accessToken === "string",
    hasRefreshToken: typeof guest.body.refreshToken === "string",
  });
  if (accountId === null) throw new Error("DEV smoke could not obtain an account id.");

  const refresh = await requestJson("POST", "/v1/auth/refresh", {
    body: { refreshToken: guest.body.refreshToken },
  });
  emit("auth_refresh", refresh.status, {
    tokenType: refresh.body.tokenType,
    hasAccessToken: typeof refresh.body.accessToken === "string",
    hasRefreshToken: typeof refresh.body.refreshToken === "string",
  });
  const auth = safeAuthHeader(refresh.body.accessToken);

  seedBalanceVersion(balanceVersion, suffix, now);
  emit("dev_fixture_balance_version", "seeded", { balanceVersion: String(balanceVersion) });

  const loginBonusKey = `dev-smoke-login-${suffix}`;
  const loginBonus = await requestJson("POST", "/v1/login-bonus/claim", {
    headers: { ...auth, "Idempotency-Key": loginBonusKey },
  });
  emit("login_bonus_claim", loginBonus.status, {
    accepted: loginBonus.body.accepted,
    claimStatus: loginBonus.body.claimStatus,
    bonusId: loginBonus.body.bonusId,
    rewardLines: (loginBonus.body.rewardLines as JsonRecord[] | undefined)?.map((line) => ({
      kind: line.kind,
      id: line.id,
      quantity: line.quantity,
      source: line.source,
    })),
  });
  const loginBonusReplay = await requestJson("POST", "/v1/login-bonus/claim", {
    headers: { ...auth, "Idempotency-Key": loginBonusKey },
  });
  emit("login_bonus_replay", loginBonusReplay.status, {
    claimStatus: loginBonusReplay.body.claimStatus,
    sameClaim: loginBonusReplay.body.loginBonusClaimId === loginBonus.body.loginBonusClaimId,
  });

  const ordinaryPreviewId = `dev_smoke_preview_ordinary_${suffix}`;
  seedOperationPreview({
    accountId,
    mutationId: `dev_smoke_mutation_ordinary_${suffix}`,
    previewId: ordinaryPreviewId,
    sequence: 1001,
    operationId: "opn_03_water_stop",
    risk: "III",
    balanceVersion,
    now,
    previewJson: {
      operationId: "opn_03_water_stop",
      actId: "act_01_wasteland_railway",
      regionId: "region_wasteland_railway",
      encounterType: "ORDINARY_15_WAVE",
      allowedOperatorSlots: 5,
      startAugmentPackageRef: "act1_full_start_package_v1",
      startTacticPackageRef: "act1_full_start_package_v1",
      contentVersion: "act1.launch.content.v1",
    },
  });
  const runStartKey = `dev-smoke-run-start-${suffix}`;
  const runStart = await requestJson("POST", "/v1/runs/start", {
    headers: { ...auth, "Idempotency-Key": runStartKey },
    body: { operationPreviewId: ordinaryPreviewId },
  });
  const ordinaryRunId = typeof runStart.body.runId === "string" ? runStart.body.runId : null;
  emit("ordinary_run_start", runStart.status, {
    accepted: runStart.body.accepted,
    startStatus: runStart.body.startStatus,
    encounterType: runStart.body.encounterType,
    ordinary: runStart.body.ordinary,
    raid: runStart.body.raid,
    balanceVersion: runStart.body.balanceVersion,
  });
  const runStartReplay = await requestJson("POST", "/v1/runs/start", {
    headers: { ...auth, "Idempotency-Key": runStartKey },
    body: { operationPreviewId: ordinaryPreviewId },
  });
  emit("ordinary_run_start_replay", runStartReplay.status, {
    startStatus: runStartReplay.body.startStatus,
    sameRun: runStartReplay.body.runId === runStart.body.runId,
  });
  if (ordinaryRunId === null) throw new Error("DEV smoke ordinary run start did not return a run id.");

  const ordinaryFinishKey = `dev-smoke-run-finish-${suffix}`;
  const ordinaryFinish = await requestJson("POST", `/v1/runs/${ordinaryRunId}/finish`, {
    headers: { ...auth, "Idempotency-Key": ordinaryFinishKey },
    body: {
      runId: ordinaryRunId,
      currentStage: "Complete",
      currentWaveNumber: 0,
      completedWaveCount: 15,
      encounterType: "ORDINARY_15_WAVE",
      outcome: "VICTORY",
      runtimeStateJson: "{}",
      tacticSelections: [{ tacticId: "tactic_full_start_1", packageRef: "act1_full_start_package_v1" }],
      clientCompletedAt: new Date(now).toISOString(),
    },
  });
  const rewardPreviewId = typeof ordinaryFinish.body.rewardPreviewId === "string" ? ordinaryFinish.body.rewardPreviewId : null;
  emit("ordinary_run_finish", ordinaryFinish.status, {
    accepted: ordinaryFinish.body.accepted,
    resultStatus: ordinaryFinish.body.resultStatus,
    rewardPreview: rewardPreviewId === null ? null : "present",
    balanceVersion: ordinaryFinish.body.balanceVersion,
  });
  const ordinaryFinishReplay = await requestJson("POST", `/v1/runs/${ordinaryRunId}/finish`, {
    headers: { ...auth, "Idempotency-Key": ordinaryFinishKey },
    body: {
      runId: ordinaryRunId,
      currentStage: "Complete",
      currentWaveNumber: 0,
      completedWaveCount: 15,
      encounterType: "ORDINARY_15_WAVE",
      outcome: "VICTORY",
      runtimeStateJson: "{}",
      tacticSelections: [{ tacticId: "tactic_full_start_1", packageRef: "act1_full_start_package_v1" }],
      clientCompletedAt: new Date(now).toISOString(),
    },
  });
  emit("ordinary_run_finish_replay", ordinaryFinishReplay.status, {
    resultStatus: ordinaryFinishReplay.body.resultStatus,
    samePreview: ordinaryFinishReplay.body.rewardPreviewId === rewardPreviewId,
  });
  if (rewardPreviewId === null) throw new Error("DEV smoke ordinary finish did not return a reward preview id.");

  const rewardPreview = await requestJson("GET", `/v1/runs/${ordinaryRunId}/reward-preview/${rewardPreviewId}`);
  emit("reward_preview", rewardPreview.status, {
    status: rewardPreview.body.status,
    claimability: rewardPreview.body.claimability,
    balanceVersion: rewardPreview.body.balanceVersion,
    rewardLineCount: (rewardPreview.body.rewardLines as unknown[] | undefined)?.length,
  });
  const rewardClaim = await requestJson("POST", `/v1/runs/${ordinaryRunId}/reward-preview/${rewardPreviewId}/claim`, {
    headers: { "Idempotency-Key": `dev-smoke-reward-claim-${suffix}` },
    body: { runId: ordinaryRunId, rewardPreviewId },
  });
  emit("reward_claim", rewardClaim.status, {
    status: rewardClaim.body.status,
    rewardClaim: typeof rewardClaim.body.rewardClaimId === "string" ? "present" : null,
  });

  const storyProgress = await requestJson("GET", "/v1/story-progress", { headers: auth });
  emit("story_progress_after_ordinary", storyProgress.status, {
    highestStoryProgressId: (storyProgress.body.storyProgress as JsonRecord | undefined)?.highestStoryProgressId,
    order: (storyProgress.body.storyProgress as JsonRecord | undefined)?.order,
  });

  setFuelState(accountId, now);
  const raidPreviewId = `dev_smoke_preview_raid_${suffix}`;
  seedOperationPreview({
    accountId,
    mutationId: `dev_smoke_mutation_raid_${suffix}`,
    previewId: raidPreviewId,
    sequence: 2001,
    operationId: "raid_raider_king_engine",
    risk: "V",
    balanceVersion,
    now,
    previewJson: {
      operationId: "raid_raider_king_engine",
      actId: "act_01_wasteland_railway",
      regionId: "region_wasteland_railway",
      encounterType: "RAID_BOSS",
      raidId: "raid_raider_king_engine",
      bossId: "boss_raider_king_engine",
      bossPhaseRefs: ["crown_chase", "trophy_break", "engine_stop"],
      allowedOperatorSlots: 5,
      startAugmentPackageRef: "act1_full_start_package_v1",
      startTacticPackageRef: "act1_full_start_package_v1",
      contentVersion: "act1.launch.content.v1",
    },
  });
  const raidStart = await requestJson("POST", "/v1/runs/start", {
    headers: { ...auth, "Idempotency-Key": `dev-smoke-raid-start-${suffix}` },
    body: { operationPreviewId: raidPreviewId },
  });
  const raidRunId = typeof raidStart.body.runId === "string" ? raidStart.body.runId : null;
  emit("story_raid_start", raidStart.status, {
    accepted: raidStart.body.accepted,
    encounterType: raidStart.body.encounterType,
    raid: raidStart.body.raid,
    allowedOperatorSlots: raidStart.body.allowedOperatorSlots,
  });
  if (raidRunId === null) throw new Error("DEV smoke story raid start did not return a run id.");

  const raidFinishKey = `dev-smoke-raid-finish-${suffix}`;
  const raidFinishBody = {
    runId: raidRunId,
    currentStage: "Complete",
    currentWaveNumber: 0,
    completedWaveCount: 0,
    encounterType: "RAID_BOSS",
    raidResult: {
      raidId: "raid_raider_king_engine",
      bossId: "boss_raider_king_engine",
      bossDefeated: false,
      completedPhaseCount: 2,
      encounterDurationSeconds: 360,
    },
    outcome: "FAILURE",
    runtimeStateJson: "{}",
    tacticSelections: [{ tacticId: "tactic_full_start_raid", packageRef: "act1_full_start_package_v1" }],
    clientCompletedAt: new Date(now).toISOString(),
  };
  const raidFinish = await requestJson("POST", `/v1/runs/${raidRunId}/finish`, {
    headers: { ...auth, "Idempotency-Key": raidFinishKey },
    body: raidFinishBody,
  });
  emit("story_raid_failure_finish", raidFinish.status, {
    accepted: raidFinish.body.accepted,
    resultStatus: raidFinish.body.resultStatus,
    staminaRefund: raidFinish.body.staminaRefund,
  });
  const raidFinishReplay = await requestJson("POST", `/v1/runs/${raidRunId}/finish`, {
    headers: { ...auth, "Idempotency-Key": raidFinishKey },
    body: raidFinishBody,
  });
  const fuelRows = d1Query(`
    SELECT fuel FROM fuel_state WHERE account_id = ${sqlString(accountId)};
  `);
  const fuelLedgerRows = d1Query(`
    SELECT COUNT(*) AS fuel_ledger_count FROM fuel_ledger
    WHERE account_id = ${sqlString(accountId)} AND reason = 'STORY_RAID_FAILURE_REFUND';
  `);
  emit("story_raid_failure_replay", raidFinishReplay.status, {
    resultStatus: raidFinishReplay.body.resultStatus,
    staminaRefund: raidFinishReplay.body.staminaRefund,
    fuel: fuelRows[0]?.fuel,
    refundLedgerCount: fuelLedgerRows[0]?.fuel_ledger_count,
  });

  setOfflineClaimBaseline(accountId, now);
  const offlinePreview = await requestJson("GET", "/v1/offline-rewards/preview", { headers: auth });
  emit("offline_preview_available", offlinePreview.status, {
    previewStatus: offlinePreview.body.previewStatus,
    storyProgressId: offlinePreview.body.storyProgressId,
    rewardLineCount: (offlinePreview.body.rewardLines as unknown[] | undefined)?.length,
  });
  const offlineClaim = await requestJson("POST", "/v1/offline-rewards/claim", {
    headers: { ...auth, "Idempotency-Key": `dev-smoke-offline-claim-${suffix}` },
  });
  emit("offline_claim", offlineClaim.status, {
    claimStatus: offlineClaim.body.claimStatus,
    storyProgressId: offlineClaim.body.storyProgressId,
    rewardLineCount: (offlineClaim.body.rewardLines as unknown[] | undefined)?.length,
  });

  const bannerId = `dev_smoke_banner_${suffix}`;
  // This exact banner id is backed by seedGacha's deterministic single-pickup
  // pool above, so repeated op_vex results prove the smoke fixture wiring.
  seedGacha({ accountId, bannerId, balanceVersion, now });
  const gachaOneKey = `dev-smoke-gacha-one-${suffix}`;
  const gachaOne = await requestJson("POST", `/v1/gacha/banners/${bannerId}/pulls`, {
    headers: { ...auth, "Idempotency-Key": gachaOneKey },
    body: { pullCount: 1 },
  });
  emit("gacha_one_pull", gachaOne.status, {
    resultStatus: gachaOne.body.resultStatus,
    bannerId: gachaOne.body.bannerId,
    pullCount: gachaOne.body.pullCount,
    guaranteeTypes: (gachaOne.body.results as JsonRecord[] | undefined)?.map((result) => result.guaranteeType),
  });
  const gachaTenKey = `dev-smoke-gacha-ten-${suffix}`;
  const gachaTen = await requestJson("POST", `/v1/gacha/banners/${bannerId}/pulls`, {
    headers: { ...auth, "Idempotency-Key": gachaTenKey },
    body: { pullCount: 10 },
  });
  emit("gacha_ten_pull", gachaTen.status, {
    resultStatus: gachaTen.body.resultStatus,
    pullCount: gachaTen.body.pullCount,
    cost: gachaTen.body.cost,
    resultCount: (gachaTen.body.results as unknown[] | undefined)?.length,
  });
  const gachaTenReplay = await requestJson("POST", `/v1/gacha/banners/${bannerId}/pulls`, {
    headers: { ...auth, "Idempotency-Key": gachaTenKey },
    body: { pullCount: 10 },
  });
  const gachaInsufficient = await requestJson("POST", `/v1/gacha/banners/${bannerId}/pulls`, {
    headers: { ...auth, "Idempotency-Key": `dev-smoke-gacha-insufficient-${suffix}` },
    body: { pullCount: 10 },
  });
  emit("gacha_replay_and_insufficient", "checked", {
    replayStatus: gachaTenReplay.body.resultStatus,
    replayErrorCode: gachaTenReplay.body.errorCode,
    insufficientStatus: gachaInsufficient.status,
    insufficientErrorCode: gachaInsufficient.body.errorCode,
  });

  const googleConfig = await requestJson("POST", "/v1/auth/google", {
    body: { idToken: "dev-smoke-placeholder-token" },
  });
  emit("google_config_deferred", googleConfig.status, {
    errorCode: googleConfig.body.errorCode,
  });
  const opsRows = d1Query(`
    SELECT error_code, source, severity, status, masked
    FROM ops_error_logs
    WHERE error_code IN ('google_auth_config_missing', 'run_start_state_conflict', 'story_raid_stamina_refund_state_conflict')
    ORDER BY created_at DESC
    LIMIT 5;
  `);
  emit("ops_error_summary_read", "checked", { rows: opsRows });

  const adminStatus = await fetch(`${adminApiBaseUrl}/status`, { redirect: "manual" });
  emit("admin_status_access_boundary", adminStatus.status, { accessRedirect: adminStatus.status === 302 });

  const quickCheck = d1Query("PRAGMA quick_check;");
  const foreignKeyCheck = d1Query("PRAGMA foreign_key_check;");
  emit("d1_quick_check", "checked", { result: quickCheck[0]?.quick_check });
  emit("d1_foreign_key_check", "checked", { violationCount: foreignKeyCheck.length });
}

await main();
