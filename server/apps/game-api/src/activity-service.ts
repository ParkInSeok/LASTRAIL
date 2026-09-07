import { canonicalJson, sha256Hex } from "@lastrail/common";
import { ActivityError, attendanceDay, parseActivityPolicy, passLevel, requireWindow } from "./activity-policy";
import type { ActivityPolicy, ItemReward, Reward } from "./activity-policy";
import { planCrystalChange } from "./crystal-wallet";
import { storeOrderInsert, verifiedStoreProof } from "./crystal-orders";

export type VerifiedPurchase = {
  provider: string; appId: string; productId: string; transactionId: string;
  accountId: string; activityId: string; environment: string;
  verifiedAt: number; expiresAt: number; purchasedAt: number; revoked: boolean;
};
// Trusted server adapter only. No default verifier and no request-to-env mapping.
// The adapter MUST verify store signature/API state and store account ownership,
// plus environment, product, current refund/revocation and purchase timestamps.
export type PassPurchaseVerifier = (input: {
  receipt: string; accountId: string; activityId: string; environment: string;
  provider: string; appId: string; productId: string; now: number;
}) => Promise<VerifiedPurchase | null>;

export type ActivityAction =
  | { kind: "CHECK_IN" }
  | { kind: "RECOVER"; dayIndex: number }
  | { kind: "MILESTONE"; days: number }
  | { kind: "PASS_REWARD"; level: number; track: "FREE" | "PREMIUM" }
  | { kind: "PASS_XP"; runId: string }
  | { kind: "PURCHASE"; receipt: string; productId: string };

type ReceiptRow = { request_hash: string; response_json: string };
type Options = { db: D1Database; accountId: string; activityId: string; environment: string; verifier?: PassPurchaseVerifier };
type Response = Record<string, unknown>;
const id = (prefix: string) => `${prefix}_${crypto.randomUUID()}`;

async function readPolicy(db: D1Database, activityId: string): Promise<ActivityPolicy> {
  const row = await db.prepare("SELECT policy_json FROM activity_definitions WHERE id = ?").bind(activityId).first<{ policy_json: string }>();
  if (!row) throw new ActivityError(503, "activity_not_configured");
  return parseActivityPolicy(row.policy_json);
}

async function total(db: D1Database, accountId: string, activityId: string, kind: "ATTENDANCE" | "PASS"): Promise<number> {
  const sql = kind === "ATTENDANCE"
    ? "SELECT COUNT(*) AS value FROM attendance_visits WHERE account_id = ? AND activity_id = ?"
    : "SELECT COALESCE(SUM(xp), 0) AS value FROM pass_xp_events WHERE account_id = ? AND activity_id = ?";
  const row = await db.prepare(sql).bind(accountId, activityId).first<{ value: number }>();
  return row?.value ?? 0;
}

export async function readActivity(options: Options, expectedKind: ActivityPolicy["kind"]): Promise<Response> {
  const { db, accountId, activityId } = options;
  const policy = await readPolicy(db, activityId);
  if (policy.kind !== expectedKind) throw new ActivityError(400, "activity_kind_mismatch");
  const now = Date.now();
  const value = await total(db, accountId, activityId, policy.kind);
  const claims = await db.prepare("SELECT claim_key FROM activity_claims WHERE account_id = ? AND activity_id = ? ORDER BY created_at, claim_key")
    .bind(accountId, activityId).all<{ claim_key: string }>();
  const entitlement = policy.kind === "PASS" ? await db.prepare("SELECT expires_at, revoked_at FROM pass_entitlements WHERE account_id = ? AND activity_id = ?")
    .bind(accountId, activityId).first<{ expires_at: number; revoked_at: number | null }>() : null;
  return {
    activityId, policy, serverTime: now, active: now >= policy.startAt && now < policy.endAt,
    claimable: now >= policy.startAt && now < policy.claimUntil,
    ...(policy.kind === "ATTENDANCE" ? { totalDays: value, dayIndex: attendanceDay(now, policy.resetOffsetMinutes) }
      : { xp: value, level: passLevel(policy, value), premium: !!entitlement && entitlement.revoked_at === null && entitlement.expires_at > now }),
    claimed: claims.results.map((claim) => claim.claim_key),
  };
}

export async function claimActivity(options: Options, action: ActivityAction, key: string): Promise<Response> {
  const { db, accountId, activityId } = options;
  const operation = `activity_${action.kind.toLowerCase()}`;
  // Only a digest of opaque store receipts persists; never log/store the receipt.
  const requestHash = await sha256Hex(canonicalJson({ activityId, action }));
  for (let attempt = 0; attempt < 3; attempt++) {
    const replay = await db.prepare("SELECT request_hash, response_json FROM activity_claims WHERE account_id = ? AND operation = ? AND idempotency_key = ?")
      .bind(accountId, operation, key).first<ReceiptRow>();
    if (replay) {
      if (replay.request_hash !== requestHash) throw new ActivityError(409, "idempotency_conflict");
      return JSON.parse(replay.response_json) as Response;
    }
    // Capture the mutation fence before reading any eligibility or balances.
    const sequence = (await db.prepare("SELECT COALESCE(MAX(sequence), 0) AS value FROM account_mutations WHERE account_id = ?")
      .bind(accountId).first<{ value: number }>())!.value + 1;
    const policy = await readPolicy(db, activityId);
    const expectedKind = action.kind === "CHECK_IN" || action.kind === "RECOVER" || action.kind === "MILESTONE" ? "ATTENDANCE" : "PASS";
    if (policy.kind !== expectedKind) throw new ActivityError(400, "activity_kind_mismatch");
    const now = Date.now();
    requireWindow(policy, now, action.kind === "MILESTONE" || action.kind === "PASS_REWARD");
    const value = await total(db, accountId, activityId, policy.kind);
    let rewards: Reward[] = [];
    let claimKey: string;
    let extra: Response = {};
    let debit: ItemReward | null = null;
    let crystalDebit: { quantity: number; policyVersion: string } | null = null;
    let premiumOrderId: string | null = null;
    let entitlementDeadline: number | null = null;
    const premiumReward = action.kind === "PASS_REWARD" && action.track === "PREMIUM";
    const after: D1PreparedStatement[] = [];
    const claimId = id("activity");
    if ((action.kind === "CHECK_IN" || action.kind === "RECOVER") && policy.kind === "ATTENDANCE") {
      const today = attendanceDay(now, policy.resetOffsetMinutes);
      const day = action.kind === "RECOVER" ? action.dayIndex : today;
      if (action.kind === "RECOVER") {
        if (!policy.recovery) throw new ActivityError(503, "attendance_recovery_unconfigured");
        if (day >= today || day < attendanceDay(policy.startAt, policy.resetOffsetMinutes)) throw new ActivityError(409, "attendance_day_not_recoverable");
        const recovered = await db.prepare("SELECT COUNT(*) AS value FROM activity_claims WHERE account_id = ? AND activity_id = ? AND operation = 'activity_recover'")
          .bind(accountId, activityId).first<{ value: number }>();
        if ((recovered?.value ?? 0) >= policy.recovery.maxPerCampaign) throw new ActivityError(409, "attendance_recovery_limit");
        if ("kind" in policy.recovery) crystalDebit = { quantity: policy.recovery.quantity, policyVersion: policy.recovery.walletPolicyVersion };
        else debit = { id: policy.recovery.itemId, quantity: policy.recovery.quantity };
      }
      claimKey = `day:${day}`;
      rewards = policy.dailyRewards;
      extra = { dayIndex: day, totalDays: value + 1 };
      after.push(db.prepare("INSERT INTO attendance_visits (account_id, activity_id, day_index, claim_id, visited_at) VALUES (?, ?, ?, ?, ?)")
        .bind(accountId, activityId, day, claimId, now));
    } else if (action.kind === "MILESTONE" && policy.kind === "ATTENDANCE") {
      const milestone = policy.milestones.find((entry) => entry.days === action.days);
      if (!milestone || value < action.days) throw new ActivityError(409, "milestone_not_earned");
      claimKey = `milestone:${action.days}`; rewards = milestone.rewards; extra = { totalDays: value };
    } else if (action.kind === "PASS_REWARD" && policy.kind === "PASS") {
      const level = policy.levels[action.level - 1];
      if (!level || value < level.xp) throw new ActivityError(409, "pass_level_not_earned");
      if (action.track === "PREMIUM") {
        const entitlement = await db.prepare("SELECT expires_at, revoked_at, order_id FROM pass_entitlements WHERE account_id = ? AND activity_id = ?")
          .bind(accountId, activityId).first<{ expires_at: number; revoked_at: number | null; order_id: string | null }>();
        if (!entitlement || entitlement.revoked_at !== null || entitlement.expires_at <= now) throw new ActivityError(403, "premium_entitlement_required");
        entitlementDeadline = entitlement.expires_at;
        premiumOrderId = entitlement.order_id;
      }
      rewards = action.track === "FREE" ? level.free : level.premium;
      if (!rewards.length) throw new ActivityError(409, "pass_reward_unavailable");
      claimKey = `level:${action.level}:${action.track}`; extra = { xp: value, level: passLevel(policy, value) };
    } else if (action.kind === "PASS_XP" && policy.kind === "PASS") {
      const run = await db.prepare(`SELECT runs.status, runs.started_at, runs.finished_at, result.outcome FROM runs
        JOIN run_final_results result ON result.run_id = runs.id WHERE runs.id = ? AND runs.account_id = ?`)
        .bind(action.runId, accountId).first<{ status: string; started_at: number; finished_at: number; outcome: string }>();
      const outcome = run?.outcome;
      const xp = outcome === "VICTORY" || outcome === "FAILURE" ? policy.runXp[outcome] : undefined;
      if (!run || !xp || (run.status !== "COMPLETED" && run.status !== "FAILED")
        || run.started_at < policy.startAt || run.finished_at < run.started_at || run.finished_at > now || run.finished_at >= policy.endAt) {
        throw new ActivityError(409, "pass_xp_event_ineligible");
      }
      if (!Number.isSafeInteger(value + xp)) throw new ActivityError(409, "pass_xp_overflow");
      claimKey = `run:${action.runId}`; extra = { xp: value + xp, level: passLevel(policy, value + xp) };
      after.push(db.prepare("INSERT INTO pass_xp_events (account_id, activity_id, run_id, xp, claim_id, created_at) VALUES (?, ?, ?, ?, ?, ?)")
        .bind(accountId, activityId, action.runId, xp, claimId, now));
    } else if (action.kind === "PURCHASE" && policy.kind === "PASS") {
      if (!policy.purchase || !options.verifier) throw new ActivityError(503, "purchase_verifier_unconfigured");
      if (action.productId !== policy.purchase.productId) throw new ActivityError(400, "purchase_product_mismatch");
      let proof: VerifiedPurchase | null;
      try { proof = await options.verifier({ ...policy.purchase, receipt: action.receipt, accountId, activityId, environment: options.environment, now }); }
      catch { throw new ActivityError(503, "purchase_verification_unavailable"); }
      const verifiedNow = Date.now();
      if (!proof || proof.revoked !== false || proof.accountId !== accountId || proof.activityId !== activityId
        || proof.environment !== options.environment || proof.provider !== policy.purchase.provider
        || proof.appId !== policy.purchase.appId || proof.productId !== policy.purchase.productId
        || typeof proof.transactionId !== "string" || !proof.transactionId.trim() || proof.transactionId.length > 512
        || !Number.isSafeInteger(proof.verifiedAt) || proof.verifiedAt > verifiedNow || proof.verifiedAt < verifiedNow - 60000
        || !Number.isSafeInteger(proof.expiresAt) || proof.expiresAt <= verifiedNow
        || !Number.isSafeInteger(proof.purchasedAt) || proof.purchasedAt < policy.startAt || proof.purchasedAt > verifiedNow) {
        throw new ActivityError(403, "purchase_not_verified");
      }
      claimKey = "premium"; extra = { premium: true, expiresAt: Math.min(proof.expiresAt, policy.claimUntil) };
      entitlementDeadline = Math.min(proof.expiresAt, policy.claimUntil);
      const orderProof = verifiedStoreProof(proof, { accountId, environment: options.environment as "LOCAL" | "DEV" | "PRD",
        provider: policy.purchase.provider, appId: policy.purchase.appId, productId: policy.purchase.productId }, verifiedNow);
      const order = storeOrderInsert(db, orderProof, "PASS", claimId);
      after.push(order.statement);
      after.push(db.prepare(`INSERT INTO pass_entitlements (account_id, activity_id, provider, app_id, product_id,
        transaction_id, verified_at, expires_at, revoked_at, claim_id, order_id) VALUES (?, ?, ?, ?, ?, ?, ?, ?, NULL, ?, ?)`)
        .bind(accountId, activityId, proof.provider, proof.appId, proof.productId, proof.transactionId, proof.verifiedAt, entitlementDeadline, claimId, order.orderId));
    } else { throw new ActivityError(400, "activity_kind_mismatch"); }
    const existing = await db.prepare("SELECT id FROM activity_claims WHERE account_id = ? AND activity_id = ? AND claim_key = ?")
      .bind(accountId, activityId, claimKey).first();
    if (existing) throw new ActivityError(409, "activity_already_claimed");
    const mutationId = id("mutation"), economyId = id("economy");
    const statements = [
      db.prepare("INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at) VALUES (?, ?, ?, ?, ?, ?)")
        .bind(mutationId, accountId, sequence, action.kind, key, now),
      db.prepare("INSERT INTO economy_transactions (id, account_id, mutation_id, reason, source_type, source_id, created_at) VALUES (?, ?, ?, ?, 'ACTIVITY', ?, ?)")
        .bind(economyId, accountId, mutationId, action.kind, claimId, now),
    ];
    const rewardLines: Array<Record<string, unknown>> = [];
    const crystalRewards = rewards.filter((reward) => reward.kind === "CRYSTAL");
    if (crystalRewards.length || crystalDebit) {
      if (premiumReward && premiumOrderId === null) throw new ActivityError(503, "premium_order_provenance_missing");
      const sourceType = action.kind === "PASS_REWARD" ? (action.track === "PREMIUM" ? "PASS_PREMIUM" : "PASS_FREE") : "ATTENDANCE";
      const walletPlan = await planCrystalChange({ db, accountId, operationId: claimId, mutationId, operation, now,
        grants: crystalRewards.map((reward) => ({ quantity: reward.quantity, sourceType, sourceRef: claimId, orderId: premiumOrderId })),
        ...(crystalDebit ? { spend: { quantity: crystalDebit.quantity, reason: "ATTENDANCE_RECOVERY", downstreamRef: claimId }, expectedPolicyVersion: crystalDebit.policyVersion } : {}),
      });
      statements.push(...walletPlan.statements);
      rewardLines.push(...walletPlan.grantLines);
      extra.wallet = walletPlan.response;
      if (crystalDebit) extra.cost = { kind: "CRYSTAL", id: walletPlan.policy.currencyId, quantity: crystalDebit.quantity,
        freeSpent: walletPlan.response.freeSpent, paidSpent: walletPlan.response.paidSpent };
    }
    const balances = new Map<string, number>();
    if (debit) {
      const balance = (await db.prepare("SELECT quantity FROM account_items WHERE account_id = ? AND item_id = ?")
        .bind(accountId, debit.id).first<{ quantity: number }>())?.quantity ?? 0;
      if (balance < debit.quantity) throw new ActivityError(409, "attendance_recovery_insufficient_balance");
      const balanceAfter = balance - debit.quantity;
      balances.set(debit.id, balanceAfter);
      extra.cost = { ...debit, balanceAfter };
      statements.push(
        db.prepare("UPDATE account_items SET quantity = quantity - ?, updated_at = ? WHERE account_id = ? AND item_id = ?")
          .bind(debit.quantity, now, accountId, debit.id),
        db.prepare(`INSERT INTO item_ledger (id, account_id, mutation_id, item_id, delta, quantity_after, reason, source_type, source_id, created_at)
          VALUES (?, ?, ?, ?, ?, ?, 'ATTENDANCE_RECOVERY_COST', 'ACTIVITY', ?, ?)`)
          .bind(id("item_ledger"), accountId, mutationId, debit.id, -debit.quantity, balanceAfter, claimId, now),
      );
    }
    for (const reward of rewards) {
      if (reward.kind === "CRYSTAL") continue;
      const balance = balances.get(reward.id) ?? (await db.prepare("SELECT quantity FROM account_items WHERE account_id = ? AND item_id = ?")
        .bind(accountId, reward.id).first<{ quantity: number }>())?.quantity ?? 0;
      const balanceAfter = balance + reward.quantity;
      if (!Number.isSafeInteger(balanceAfter)) throw new ActivityError(409, "reward_balance_overflow");
      rewardLines.push({ kind: "ITEM", ...reward, balanceAfter, source: action.kind });
      statements.push(
        db.prepare(`INSERT INTO account_items (account_id, item_id, quantity, updated_at) VALUES (?, ?, ?, ?)
          ON CONFLICT(account_id, item_id) DO UPDATE SET quantity = account_items.quantity + ?, updated_at = excluded.updated_at`)
          .bind(accountId, reward.id, reward.quantity, now, reward.quantity),
        db.prepare(`INSERT INTO item_ledger (id, account_id, mutation_id, item_id, delta, quantity_after, reason, source_type, source_id, created_at)
          VALUES (?, ?, ?, ?, ?, ?, ?, 'ACTIVITY', ?, ?)`)
          .bind(id("item_ledger"), accountId, mutationId, reward.id, reward.quantity, balanceAfter, action.kind, claimId, now),
      );
    }
    const response = { accepted: true, activityId, claimId, claimKey, rewardTransactionId: claimId, accountMutationId: mutationId, economyTransactionId: economyId, serverClaimedAt: now, rewardLines, ...extra };
    const json = JSON.stringify(response);
    statements.push(
      db.prepare("INSERT INTO reward_transactions (id, account_id, run_id, mutation_id, balance_config_version, reward_json, created_at) VALUES (?, ?, NULL, ?, NULL, ?, ?)")
        .bind(claimId, accountId, mutationId, json, now),
      db.prepare(`INSERT INTO activity_claims (id, account_id, activity_id, claim_key, operation, idempotency_key, request_hash, response_json, created_at)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)`)
        .bind(claimId, accountId, activityId, claimKey, operation, key, requestHash, json, now),
      ...after,
      db.prepare("INSERT INTO idempotency_records (id, account_id, idempotency_key, operation, result_ref, created_at) VALUES (?, ?, ?, ?, ?, ?)")
        .bind(id("idem"), accountId, key, operation, claimId, now),
    );
    // Do not commit a request that crossed the campaign or entitlement boundary while verifying.
    const commitTime = Date.now();
    requireWindow(policy, commitTime, action.kind === "MILESTONE" || action.kind === "PASS_REWARD");
    if (entitlementDeadline !== null && entitlementDeadline <= commitTime) throw new ActivityError(403, "premium_entitlement_expired");
    if (action.kind === "CHECK_IN" && policy.kind === "ATTENDANCE"
      && attendanceDay(commitTime, policy.resetOffsetMinutes) !== attendanceDay(now, policy.resetOffsetMinutes)) continue;
    // Evaluate time and entitlement in the transaction itself, not only before
    // network dispatch. A zero sequence violates the existing CHECK and rolls
    // back the entire batch. Retry re-evaluates the authoritative eligibility.
    const claimOnly = action.kind === "MILESTONE" || action.kind === "PASS_REWARD";
    const checkInDay = action.kind === "CHECK_IN" && policy.kind === "ATTENDANCE" ? attendanceDay(now, policy.resetOffsetMinutes) : null;
    statements[0] = db.prepare(`INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at)
      SELECT ?, ?, CASE WHEN clock.now >= ? AND clock.now < ?
        AND (? IS NULL OR clock.now < ?)
        AND (? IS NULL OR CAST((clock.now - 68400000) / 86400000 AS INTEGER) = ?)
        AND (? = 0 OR EXISTS (SELECT 1 FROM pass_entitlements WHERE account_id = ? AND activity_id = ?
          AND revoked_at IS NULL AND expires_at > clock.now))
        THEN ? ELSE 0 END, ?, ?, ?
      FROM (SELECT CAST(ROUND(unixepoch('subsec') * 1000) AS INTEGER) AS now) AS clock`)
      .bind(mutationId, accountId, policy.startAt, claimOnly ? policy.claimUntil : policy.endAt,
        entitlementDeadline, entitlementDeadline, checkInDay, checkInDay, premiumReward ? 1 : 0,
        accountId, activityId, sequence, action.kind, key, now);
    try { await db.batch(statements); return response; }
    catch (error) {
      if (/CHECK constraint failed.*sequence/iu.test(String(error))) {
        if (attempt < 2) continue;
        throw new ActivityError(409, "activity_eligibility_changed");
      }
      if (attempt < 2 && /UNIQUE constraint failed/iu.test(String(error))) continue;
      // Receipt and store transaction uniqueness are business conflicts; other failures remain errors.
      if (/UNIQUE constraint failed/iu.test(String(error))) throw new ActivityError(409, "activity_write_conflict");
      throw error;
    }
  }
  throw new ActivityError(409, "activity_write_conflict");
}
