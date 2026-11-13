import {maintenanceAdmissionStatement,maintenanceContext,maintenanceAdmissionError} from './maintenance-admission';
import {prepareRewardDisclosure,rewardDisclosureView,type RewardDisclosurePinRow} from './reward-disclosure-pin';
import { calculateOfflineRewardPreview } from "../../../packages/balance/src/index";
import { ActivityError } from "./activity-policy";
import {requireNewAd} from './ad-runtime-policy';
import {requireAdStartForCallback} from './ad-start-approval';
import { planActivityGrant } from "./activity-grant-plan";
import { walletId } from "./crystal-wallet";
import { readOfflineEconomy } from "./offline-economy";
import { drawOfflineAdMultiplier, getOfflineAdDisplayMultipliers, getOfflineAdProbabilities, multiplyOfflineSnapshot, parseOfflineAdPolicy,parseOfflineRewardPolicy } from "./offline-ad-policy";
import {readRewardApproval,ssvApprovalStatement} from './reward-opportunity-approval';
import type { VerifiedAdmobCallback } from "./admob-ssv";
import { actOneNarrativeVersion } from "./narrative-version";
import { providerReceiptStatement,existingProviderReceipt } from "./ad-provider-receipts";

type Row = RewardDisclosurePinRow & {
  id: string; account_id: string; environment: string; create_key: string; baseline_ms: number; baseline_revision: number;
  created_at: number; expires_at: number|null; callback_deadline: number|null; policy_json: string; base_json: string; metadata_json: string;
  story_progress_id: string; narrative_version: string; user_binding: string|null; challenge: string|null;
  origin:'AD_REQUEST'|'PURCHASE_ONLY';
  status: "AD_PENDING" | "AD_VERIFIED" | "CLAIMED"; provider_transaction_id: string | null; response_json: string | null;
};
type Result = { accepted: true; intentId: string; status: "CLAIMED"; multiplier: 2 | 3 | 4; rewardTransactionId: string;
  rewardLines: Array<Record<string, unknown>>; policyVersion: string };
const fail = (code: string): never => { throw new ActivityError(409, code); };
const sequence = async (db: D1Database, account: string) => (await db.prepare("SELECT COALESCE(MAX(sequence),0)+1 AS value FROM account_mutations WHERE account_id=?").bind(account).first<{ value: number }>())!.value;
async function baseline(db: D1Database, account: string) {
  const row = await db.prepare(`SELECT COALESCE(s.last_offline_reward_claim_at,a.created_at) AS baseline,
    COALESCE(s.revision,0) AS revision FROM accounts a LEFT JOIN account_offline_reward_state s ON s.account_id=a.id
    WHERE a.id=? AND a.status='ACTIVE'`).bind(account).first<{ baseline: number; revision: number }>();
  if (!row) throw new ActivityError(403, "account_not_active");
  return row;
}
function guard(db: D1Database, account: string, base: number, revision: number) {
  return db.prepare(`INSERT INTO offline_ad_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM accounts a
    LEFT JOIN account_offline_reward_state s ON s.account_id=a.id WHERE a.id=? AND a.status='ACTIVE'
    AND COALESCE(s.last_offline_reward_claim_at,a.created_at)=? AND COALESCE(s.revision,0)=?) THEN 1 ELSE 0 END`).bind(account, base, revision);
}
async function retry<T>(action: () => Promise<T>): Promise<T> {
  for (let n = 0; n < 4; n++) {
    try { return await action(); } catch (error) {
      if(String(error).includes('reward_disclosure_changed'))throw new ActivityError(409,'reward_disclosure_policy_changed');
      if(String(error).includes('ad_runtime_policy_blocked'))throw new ActivityError(409,'ad_runtime_policy_changed');
      if (!/UNIQUE constraint failed|CHECK constraint failed: valid=1/iu.test(String(error))) throw error;
      if (n === 3) fail("ad_reward_state_conflict");
    }
  }
  return fail("ad_reward_state_conflict");
}
function view(row: Row) {
  const policy = parseOfflineRewardPolicy(JSON.parse(row.policy_json));
  return { accepted: true, probabilityDisclosure:rewardDisclosureView(row), intentId: row.id, status: row.status, createdAt: row.created_at, expiresAt: row.expires_at,
    userId: row.user_binding, customData: row.challenge, adUnit: row.origin==='AD_REQUEST'?parseOfflineAdPolicy(JSON.parse(row.policy_json)).adUnit:null, policyVersion: policy.version,
    displayMultipliers: getOfflineAdDisplayMultipliers(policy),
    probabilities: getOfflineAdProbabilities(policy),
    result: row.response_json ? JSON.parse(row.response_json) as Result : null };
}
export async function readOfflineAdIntent(db: D1Database, account: string, id: string) {
  const row = await db.prepare("SELECT * FROM offline_ad_intents WHERE id=? AND account_id=?").bind(id, account).first<Row>();
  if (!row) throw new ActivityError(403, "ad_intent_not_found");
  const result = view(row);
  if (row.status === "CLAIMED") return { ...result, canSpin: false };
  const current = await baseline(db, account);
  if (current.baseline !== row.baseline_ms || current.revision !== row.baseline_revision) {
    return { ...result, status: "SUPPLY_CONSUMED", canSpin: false };
  }
  if (row.status === "AD_PENDING" && row.callback_deadline!==null && Date.now() > row.callback_deadline) return { ...result, status: "EXPIRED", canSpin: false };
  return { ...result, canSpin: row.status === "AD_VERIFIED" };
}

export async function readOfflineAdCreationReplay(db: D1Database, account: string, environment: string, key: string) {
  const prior = await db.prepare("SELECT i.* FROM offline_ad_create_receipts r JOIN offline_ad_intents i ON i.id=r.intent_id WHERE r.account_id=? AND r.idempotency_key=?").bind(account, key).first<Row>();
  if (!prior) return null;
  if (prior.environment !== environment) throw new ActivityError(403, "ad_intent_not_found");
  return readOfflineAdIntent(db, account, prior.id);
}

export async function createOfflineAdIntent(db: D1Database, account: string, environment: string, input: unknown, key: string,origin:'AD_REQUEST'|'PURCHASE_ONLY'='AD_REQUEST') {
  const replay = await readOfflineAdCreationReplay(db, account, environment, key);
  if (replay) return replay;
  const policy = parseOfflineRewardPolicy(input),delivery=origin==='AD_REQUEST'?parseOfflineAdPolicy(input):null;
  if (policy.environment !== environment) throw new ActivityError(503, "ad_reward_policy_unconfigured");
  return retry(async () => {
    const prior = await readOfflineAdCreationReplay(db, account, environment, key);
    if (prior) return prior;
    const seq = await sequence(db, account), current = await baseline(db, account), now = Date.now();
    const existing = await db.prepare("SELECT * FROM offline_ad_intents WHERE account_id=? AND baseline_ms=? AND baseline_revision=?").bind(account, current.baseline, current.revision).first<Row>();
    if (existing) {
      if(existing.environment!==environment)throw new ActivityError(403,'ad_intent_not_found');
      await db.batch([db.prepare("INSERT INTO offline_ad_create_receipts(account_id,idempotency_key,intent_id) VALUES(?,?,?)").bind(account, key, existing.id)]);
      return readOfflineAdIntent(db, account, existing.id);
    }
    try{
    if(origin==='AD_REQUEST')await requireNewAd(db,environment);
    const offset = (delivery?.dayStartUtcMinutes??0) * 60000;
    const dayStart = Math.floor((now - offset) / 86400000) * 86400000 + offset;
    const count = (await db.prepare("SELECT count(*) AS value FROM offline_ad_intents WHERE account_id=? AND created_at>=? AND origin='AD_REQUEST'").bind(account, dayStart).first<{ value: number }>())!.value;
    if (delivery && count >= delivery.maxAdsPerDay) fail("ad_daily_limit_reached");
    const economy = await readOfflineEconomy(db);
    const progress = await db.prepare("SELECT highest_story_progress_id,narrative_version FROM account_story_progress WHERE account_id=?").bind(account).first<{ highest_story_progress_id: string; narrative_version: string }>();
    const story = progress?.highest_story_progress_id ?? "none", narrative = progress?.narrative_version ?? actOneNarrativeVersion;
    const preview = calculateOfflineRewardPreview({ config: economy.config, storyProgressKey: story, actualOfflineSeconds: Math.floor((now - current.baseline) / 1000) });
    if (!preview.rewardLines.length || preview.calculation.claimableSeconds === 0) fail("offline_reward_not_claimable");
    const expires = delivery?now + delivery.challengeLifetimeMs:null, deadline = delivery?expires! + delivery.callbackGraceMs:null;
    if (delivery&&!Number.isSafeInteger(deadline)) throw new ActivityError(503, "ad_reward_policy_unconfigured");
    const disclosure=await prepareRewardDisclosure(db,environment,'OFFLINE',policy,origin);
    const id = walletId("offline_ad"), user = delivery?crypto.randomUUID():null, challenge = delivery?crypto.randomUUID():null;
    await db.batch([
      maintenanceAdmissionStatement(db,maintenanceContext(environment)),
      db.prepare("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,idempotency_key,created_at) VALUES(?,?,?,'OFFLINE_AD_INTENT',?,?)").bind(walletId("mutation"), account, seq, key, now),
      guard(db, account, current.baseline, current.revision),
      disclosure.guard,
      db.prepare(`INSERT INTO offline_ad_intents(id,account_id,environment,create_key,baseline_ms,baseline_revision,created_at,expires_at,callback_deadline,
        policy_json,base_json,metadata_json,story_progress_id,narrative_version,user_binding,challenge,status,origin,write_revision,probability_disclosure_id,probability_source_digest)
        VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,'AD_PENDING',?,1,?,?)`).bind(id, account, environment, key, current.baseline, current.revision, now, expires, deadline,
        JSON.stringify(policy), JSON.stringify(preview.rewardLines), JSON.stringify(economy.metadata), story, narrative, user, challenge,origin,disclosure.disclosureId,disclosure.sourceDigest),
      db.prepare("INSERT INTO offline_ad_create_receipts(account_id,idempotency_key,intent_id) VALUES(?,?,?)").bind(account, key, id),
      db.prepare("DELETE FROM offline_ad_guard"),
      db.prepare("DELETE FROM reward_disclosure_guard"),
    ]);
    return readOfflineAdIntent(db, account, id);
    }catch(error){
      const committed=await readOfflineAdCreationReplay(db,account,environment,key);
      if(committed)return committed;
      const winner=await db.prepare("SELECT * FROM offline_ad_intents WHERE account_id=? AND baseline_ms=? AND baseline_revision=?")
        .bind(account,current.baseline,current.revision).first<Row>();
      if(winner){
        if(winner.environment!==environment)throw new ActivityError(403,'ad_intent_not_found');
        await db.prepare("INSERT OR IGNORE INTO offline_ad_create_receipts(account_id,idempotency_key,intent_id) VALUES(?,?,?)").bind(account,key,winner.id).run();
        const recovered=await readOfflineAdCreationReplay(db,account,environment,key);
        if(recovered)return recovered;
      }
      throw maintenanceAdmissionError(error)??error;
    }
  });
}

/** Internal only: caller MUST obtain callback from verifyAdmobCallback first.
 * This transition never draws or grants. Provider transaction is globally unique.
 */
export async function recordOfflineAdVerification(db: D1Database, id: string, callback: VerifiedAdmobCallback) {
  return retry(async () => {
    const row = await db.prepare("SELECT * FROM offline_ad_intents WHERE id=?").bind(id).first<Row>();
    if (!row) throw new ActivityError(403, "ad_intent_not_found");
    const policy = parseOfflineAdPolicy(JSON.parse(row.policy_json));
    if (callback.provider !== "ADMOB" || callback.adUnit !== policy.adUnit || callback.userId !== row.user_binding || callback.customData !== row.challenge) fail("ad_ssv_binding_mismatch");
    if(await existingProviderReceipt(db,callback.transactionId,'OFFLINE',id,row.account_id))return {accepted:true};
    if (row.provider_transaction_id === callback.transactionId) return { accepted: true };
    const approval=await readRewardApproval(db,'OFFLINE',id);
    const observation=approval?.cause==='PURCHASE_ENTITLEMENT';
    if (row.status !== "AD_PENDING"&&!observation) fail("ad_intent_already_verified");
    if (row.expires_at===null||row.callback_deadline===null||callback.timestamp < row.created_at || callback.timestamp > row.expires_at || Date.now() > row.callback_deadline) fail("ad_verification_expired");
    await requireAdStartForCallback(db,'OFFLINE',id,row.account_id,row.environment,callback.timestamp);
    await db.batch([
      // Recheck the callback window at commit time, not only before awaiting batch.
      // Equality remains valid, matching the existing provider/grace boundary.
      db.prepare(`INSERT INTO offline_ad_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM offline_ad_intents
        WHERE id=? AND (status='AD_PENDING' OR EXISTS(SELECT 1 FROM reward_opportunity_approvals a WHERE a.consumer='OFFLINE' AND a.intent_id=offline_ad_intents.id AND a.cause='PURCHASE_ENTITLEMENT')) AND account_id=? AND policy_json=?
        AND user_binding=? AND challenge=? AND ? BETWEEN created_at AND expires_at
        AND callback_deadline>=CAST(ROUND(unixepoch('subsec')*1000) AS INTEGER)) THEN 1 ELSE 0 END`)
        .bind(id, row.account_id, row.policy_json, callback.userId, callback.customData, callback.timestamp),
      providerReceiptStatement(db,callback.transactionId,"OFFLINE",id,row.account_id),
      ...(!observation?[ssvApprovalStatement(db,'OFFLINE',id,row.account_id,row.environment,callback.transactionId),
        db.prepare("UPDATE offline_ad_intents SET status='AD_VERIFIED',provider_transaction_id=?,verified_at=?,write_revision=write_revision+1 WHERE id=?").bind(callback.transactionId, Date.now(), id)]:[]),
      db.prepare("DELETE FROM offline_ad_guard"),
    ]);
    return { accepted: true };
  });
}

export async function spinOfflineAd(db: D1Database, account: string, id: string, key: string): Promise<Result> {
  return retry(async () => {
    const seq = await sequence(db, account);
    const used = await db.prepare("SELECT id FROM offline_ad_intents WHERE account_id=? AND spin_key=?").bind(account, key).first<{ id: string }>();
    if (used && used.id !== id) fail("idempotency_key_conflict");
    const row = await db.prepare("SELECT * FROM offline_ad_intents WHERE id=? AND account_id=?").bind(id, account).first<Row>();
    if (!row) throw new ActivityError(403, "ad_intent_not_found");
    if (row.response_json) return JSON.parse(row.response_json) as Result;
    const current = await baseline(db, account);
    if (current.baseline !== row.baseline_ms || current.revision !== row.baseline_revision) fail("ad_supply_already_consumed");
    if (row.status === "AD_PENDING" && row.callback_deadline!==null && Date.now() > row.callback_deadline) fail("ad_verification_expired");
    if (row.status !== "AD_VERIFIED") fail("ad_verification_pending");
    const policy = parseOfflineRewardPolicy(JSON.parse(row.policy_json));
    let fixed = await db.prepare("SELECT multiplier,policy_json,base_json FROM offline_ad_fixed_results WHERE intent_id=?")
      .bind(id).first<{ multiplier: 2 | 3 | 4; policy_json: string; base_json: string }>();
    if (!fixed) {
      // Commit only the decision first. A failed payout must not allow a redraw.
      // Concurrent proposals all read the first stored winner before settlement.
      const proposed = drawOfflineAdMultiplier(policy);
      await db.batch([
        guard(db, account, row.baseline_ms, row.baseline_revision),
        db.prepare(`INSERT INTO offline_ad_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM offline_ad_intents
          WHERE id=? AND account_id=? AND status='AD_VERIFIED' AND policy_json=? AND base_json=?) THEN 1 ELSE 0 END`)
          .bind(id, account, row.policy_json, row.base_json),
        db.prepare(`INSERT INTO offline_ad_fixed_results(intent_id,multiplier,policy_json,base_json) VALUES(?,?,?,?)
          ON CONFLICT(intent_id) DO NOTHING`).bind(id, proposed, row.policy_json, row.base_json),
        db.prepare("DELETE FROM offline_ad_guard"),
      ]);
      fixed = await db.prepare("SELECT multiplier,policy_json,base_json FROM offline_ad_fixed_results WHERE intent_id=?")
        .bind(id).first<{ multiplier: 2 | 3 | 4; policy_json: string; base_json: string }>();
    }
    if (!fixed || fixed.policy_json !== row.policy_json || fixed.base_json !== row.base_json) return fail("ad_reward_state_conflict");
    const multiplier = fixed.multiplier, now = Date.now();
    const rewards = multiplyOfflineSnapshot(JSON.parse(row.base_json), multiplier).map(line => ({ kind: "CURRENCY" as const, ...line }));
    const mutation = walletId("mutation"), claim = walletId("offline_ad_claim"), economy = walletId("economy");
    const plan = await planActivityGrant({ db, accountId: account, mutationId: mutation, claimId: claim, now, rewards, operation: "OFFLINE_AD_REWARD", economyId: economy });
    const metadata = JSON.parse(row.metadata_json) as { balanceVersion: number | null };
    const response: Result = { accepted: true, intentId: id, status: "CLAIMED", multiplier, rewardTransactionId: claim, rewardLines: plan.lines, policyVersion: policy.version };
    const json = JSON.stringify(response);
    await db.batch([
      db.prepare("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,idempotency_key,created_at) VALUES(?,?,?,'OFFLINE_AD_REWARD',?,?)").bind(mutation, account, seq, key, now),
      guard(db, account, row.baseline_ms, row.baseline_revision),
      db.prepare(`INSERT INTO offline_ad_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM offline_ad_intents i
        JOIN offline_ad_fixed_results f ON f.intent_id=i.id WHERE i.id=? AND i.account_id=? AND i.status='AD_VERIFIED'
        AND i.policy_json=f.policy_json AND i.base_json=f.base_json AND f.policy_json=? AND f.base_json=? AND f.multiplier=?)
        THEN 1 ELSE 0 END`).bind(id, account, row.policy_json, row.base_json, multiplier),
      db.prepare("INSERT INTO economy_transactions(id,account_id,mutation_id,reason,source_type,source_id,created_at) VALUES(?,?,?,'OFFLINE_AD_REWARD','OFFLINE_AD_REWARD',?,?)").bind(economy, account, mutation, id, now),
      ...plan.statements,
      db.prepare("INSERT INTO reward_transactions(id,account_id,mutation_id,balance_config_version,reward_json,created_at) VALUES(?,?,?,?,?,?)").bind(claim, account, mutation, metadata.balanceVersion, json, now),
      db.prepare(`INSERT INTO account_offline_reward_state(account_id,last_offline_reward_claim_at,last_story_progress_id,last_balance_config_version,last_narrative_version,revision,updated_at,last_reward_transaction_id)
        VALUES(?,?,?,?,?,1,?,?) ON CONFLICT(account_id) DO UPDATE SET last_offline_reward_claim_at=excluded.last_offline_reward_claim_at,
        last_story_progress_id=excluded.last_story_progress_id,last_balance_config_version=excluded.last_balance_config_version,last_narrative_version=excluded.last_narrative_version,
        revision=account_offline_reward_state.revision+1,updated_at=excluded.updated_at,last_reward_transaction_id=excluded.last_reward_transaction_id`).bind(account, row.created_at, row.story_progress_id, metadata.balanceVersion, row.narrative_version, now, claim),
      db.prepare("UPDATE offline_ad_intents SET status='CLAIMED',spin_key=?,response_json=?,write_revision=write_revision+1 WHERE id=?").bind(key, json, id),
      db.prepare("DELETE FROM offline_ad_guard"),
    ]);
    return response;
  });
}
