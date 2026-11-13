import {prepareCompleteRunClaim,readCompleteRunClaim} from './run-complete-claim';
import { createRequestId } from "@lastrail/common";
import { prepareLaunchRewards, launchEntitlementStatements, type LaunchLine } from "./launch-entitlements";
import { readEconomyBinding } from "./economy-binding";
import { planCrystalChange } from "./crystal-wallet";
import { ActivityError } from "./activity-policy";
import { getRewardLines, getCurrencyRewardLines, getItemRewardLines, currencyIdToD1Currency } from "./reward-plan-lines";
import { multiplyCombatRewards } from "./combat-roulette-policy";
import { parseCombatRewardPolicy } from "./combat-ad-policy";

export type SettlementPreview = {
  id: string; account_id: string; run_id: string; balance_config_version: number;
  reward_plan_json: string; expires_at: number;
};
type AccountMutationSequenceRow = { sequence: number | null };
type AccountCurrencyRow = { balance: number };
type AccountItemRow = { quantity: number };
function createDomainId(prefix: string) { return prefix + "_" + createRequestId().slice(4); }
function toIso(ms: number) { return new Date(ms).toISOString(); }

/** Only trusted server callers supply a persisted, authorized preview. The batch
 * rechecks that snapshot and owns every wallet, entitlement and receipt write.
 * No public caller may provide a multiplier; verified ad integration is separate. */
export async function settleRunReward(db: D1Database, row: SettlementPreview, idempotencyKey: string, combatIntentId?: string, completeResponse=false) {
  if(completeResponse&&combatIntentId)throw new ActivityError(409,'run_claim_complete_ad_scope_unsupported');
  const now = Date.now();
  const accountId = row.account_id;
  const runId = row.run_id;
  const rewardPreviewId = row.id;
  const rewardClaimId = createDomainId("reward_claim");
  const mutationId = createDomainId("mutation");
  const economyTransactionId = createDomainId("economy");
  const latestMutation = await db.prepare(
    `SELECT MAX(sequence) AS sequence
     FROM account_mutations
     WHERE account_id = ?`,
  )
    .bind(accountId)
    .first<AccountMutationSequenceRow>();
  const nextSequence = (latestMutation?.sequence ?? 0) + 1;
  const originalPlan = JSON.parse(row.reward_plan_json) as {calculation?:{operationId?:string}};
  const ad = combatIntentId ? await db.prepare(`SELECT id,multiplier,policy_json FROM combat_ad_intents
    WHERE id=? AND account_id=? AND run_id=? AND preview_id=? AND status='RESULT_FIXED'
      AND base_json=? AND balance_version=?`)
    .bind(combatIntentId,accountId,runId,rewardPreviewId,row.reward_plan_json,row.balance_config_version)
    .first<{id:string;multiplier:2|3|4;policy_json:string}>() : null;
  if(combatIntentId&&!ad)throw new ActivityError(409,"ad_reward_not_claimable");
  const adPolicy=ad?parseCombatRewardPolicy(JSON.parse(ad.policy_json)):null;
  const launchPlan = await prepareLaunchRewards(db, accountId, originalPlan.calculation?.operationId ?? "", getRewardLines(row.reward_plan_json) as LaunchLine[], (await readEconomyBinding(db,row.balance_config_version)) !== null);
  if(ad)launchPlan.lines=multiplyCombatRewards(launchPlan.lines,ad.multiplier);
  const effectiveRewardJson = JSON.stringify({...originalPlan,rewardLines:launchPlan.lines,
    ...(ad?{roulette:{intentId:ad.id,multiplier:ad.multiplier,policyVersion:adPolicy!.version}}:{})});
  const rewardLines = getCurrencyRewardLines(effectiveRewardJson);
  const itemRewardLines = getItemRewardLines(effectiveRewardJson);
  const crystalLines = launchPlan.lines.filter(line => line.kind === "CRYSTAL");
  const crystalPlan = crystalLines.length === 0 ? null : await planCrystalChange({db:db,accountId,operationId:createDomainId("crystal_operation"),mutationId,operation:"LAUNCH_FIRST_CLEAR",now,
    ...(launchPlan.conversionPolicyVersion ? { expectedPolicyVersion: launchPlan.conversionPolicyVersion } : {}),
    grants:crystalLines.map(line => ({quantity:line.quantity,sourceType:"LAUNCH_ENTITLEMENT" as const,sourceRef:line.launchSourceKey!,orderId:null}))});
  const claimRewardLines = [];
  const statements = [
    db.prepare(
      `INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at)
       VALUES (?, ?, ?, 'RUN_REWARD_CLAIM', ?, ?)`,
    ).bind(mutationId, accountId, nextSequence, idempotencyKey, now),
    db.prepare(
      `INSERT INTO reward_transactions (id, account_id, run_id, mutation_id, balance_config_version, reward_json, created_at)
       VALUES (?, CASE WHEN EXISTS (
         SELECT 1 FROM run_reward_previews p JOIN runs r ON r.id=p.run_id
         WHERE p.id=? AND p.run_id=? AND p.account_id=? AND r.account_id=p.account_id
           AND p.status='ACTIVE' AND p.reward_plan_json=? AND p.balance_config_version=?
           AND p.expires_at=? AND p.expires_at>CAST((julianday('now')-2440587.5)*86400000 AS INTEGER)
       ) THEN ? ELSE NULL END, ?, ?, ?, ?, ?)`,
    ).bind(rewardClaimId, rewardPreviewId, runId, accountId, row.reward_plan_json,
      row.balance_config_version, row.expires_at, accountId, runId, mutationId, row.balance_config_version, effectiveRewardJson, now),
    db.prepare(
      `INSERT INTO economy_transactions (id, account_id, mutation_id, reason, source_type, source_id, created_at)
       VALUES (?, ?, ?, 'RUN_REWARD_CLAIM', 'RUN_REWARD_PREVIEW', ?, ?)`,
    ).bind(economyTransactionId, accountId, mutationId, rewardPreviewId, now),
  ];

  const currencyBalances = new Map<string, number>();
  for (const rewardLine of rewardLines) {
    const currency = currencyIdToD1Currency[rewardLine.id as keyof typeof currencyIdToD1Currency];
    const current = await db.prepare(
      `SELECT balance
       FROM account_currencies
       WHERE account_id = ? AND currency = ?`,
    )
      .bind(accountId, currency)
      .first<AccountCurrencyRow>();
    const balanceAfter = (currencyBalances.get(currency) ?? current?.balance ?? 0) + rewardLine.quantity;
    currencyBalances.set(currency,balanceAfter);
    const ledgerId = createDomainId("ledger");
    statements.push(
      db.prepare(
        `INSERT INTO account_currencies (account_id, currency, balance, updated_at)
         VALUES (?, ?, ?, ?)
         ON CONFLICT(account_id, currency) DO UPDATE SET balance = excluded.balance, updated_at = excluded.updated_at`,
      ).bind(accountId, currency, balanceAfter, now),
      db.prepare(
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

  statements.push(...launchEntitlementStatements(db, accountId, launchPlan.sources, rewardClaimId, now), ...(crystalPlan?.statements ?? []));
  claimRewardLines.push(...crystalLines);

  const itemBalances = new Map<string, number>();
  for (const rewardLine of itemRewardLines) {
    const current = await db.prepare(
      `SELECT quantity
       FROM account_items
       WHERE account_id = ? AND item_id = ?`,
    )
      .bind(accountId, rewardLine.id)
      .first<AccountItemRow>();
    const balanceAfter = (itemBalances.get(rewardLine.id) ?? current?.quantity ?? 0) + rewardLine.quantity;
    itemBalances.set(rewardLine.id,balanceAfter);
    const itemLedgerId = createDomainId("item_ledger");
    statements.push(
      db.prepare(
        `INSERT INTO account_items (account_id, item_id, quantity, updated_at)
         VALUES (?, ?, ?, ?)
         ON CONFLICT(account_id, item_id) DO UPDATE SET quantity = excluded.quantity, updated_at = excluded.updated_at`,
      ).bind(accountId, rewardLine.id, balanceAfter, now),
      db.prepare(
        `INSERT INTO item_ledger (id, account_id, mutation_id, item_id, delta, quantity_after, reason, source_type, source_id, created_at)
         VALUES (?, ?, ?, ?, ?, ?, 'RUN_REWARD_ITEM_GRANT', 'RUN_REWARD_CLAIM', ?, ?)`,
      ).bind(itemLedgerId, accountId, mutationId, rewardLine.id, rewardLine.quantity, balanceAfter, rewardPreviewId, now),
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
    db.prepare(
      `UPDATE run_reward_previews
       SET status = 'CLAIMED', claimed_at = ?, reward_claim_id = ?, reward_plan_json = ?
       WHERE id = ? AND run_id = ? AND status = 'ACTIVE'`,
    ).bind(now, rewardClaimId, effectiveRewardJson, rewardPreviewId, runId),
    db.prepare(
      `INSERT INTO idempotency_records (id, account_id, idempotency_key, operation, result_ref, created_at)
       VALUES (?, ?, ?, ?, ?, ?)`,
    ).bind(createDomainId("idem"), accountId, idempotencyKey, ad ? "combat_ad_reward_claim" : "run_reward_claim", rewardClaimId, now),
  );

  const complete=completeResponse?await prepareCompleteRunClaim(db,{accountId,runId,previewId:rewardPreviewId,transactionId:rewardClaimId,commandKey:idempotencyKey,sequence:nextSequence,at:now,balanceVersion:row.balance_config_version,lines:claimRewardLines,currencies:currencyBalances,items:itemBalances,
    wallet:crystalPlan?{kind:'SNAPSHOT',coverage:['BALANCES'],currencyId:crystalPlan.response.currencyId,policyVersion:crystalPlan.response.policyVersion,freeBalance:crystalPlan.response.freeBalance,paidBalance:crystalPlan.response.paidBalance,totalBalance:crystalPlan.response.totalBalance,sourceMutation:{versionSource:'CRYSTAL_OPERATION_MUTATION',operationId:crystalPlan.response.operationId,basisAccountSequence:nextSequence-1,mutationSequence:nextSequence}}:{kind:'UNCHANGED'},rewardPlan:JSON.parse(effectiveRewardJson)}):null;
  if(complete){statements.unshift(complete.assertion);statements.push(complete.insert,complete.cleanup);}

  if(ad){
    // Verification, pinned draw and single preview consumption are checked in
    // the same batch as wallet/ledger writes, never via a client multiplier.
    statements.unshift(db.prepare(`INSERT INTO combat_ad_guard SELECT CASE WHEN EXISTS(
      SELECT 1 FROM combat_ad_intents i JOIN reward_opportunity_approvals a ON a.consumer='COMBAT' AND a.intent_id=i.id
        AND a.account_id=i.account_id AND a.environment=i.environment
      JOIN run_final_results f ON f.run_id=i.run_id JOIN runs r ON r.id=i.run_id AND r.account_id=i.account_id
      WHERE i.id=? AND i.account_id=? AND i.run_id=? AND i.preview_id=? AND i.status='RESULT_FIXED'
        AND i.multiplier=? AND i.base_json=? AND i.balance_version=? AND f.outcome='VICTORY' AND r.status='COMPLETED'
        AND ((a.cause='AD_SSV' AND EXISTS(SELECT 1 FROM ad_provider_receipts pr WHERE pr.consumer='COMBAT' AND pr.intent_id=i.id
          AND pr.account_id=i.account_id AND pr.provider='ADMOB' AND pr.transaction_id=a.provider_transaction_id))
          OR(a.cause='PURCHASE_ENTITLEMENT' AND EXISTS(SELECT 1 FROM verified_store_orders o WHERE o.id=a.order_id
          AND o.account_id=i.account_id AND o.environment=i.environment AND o.product_kind='AD_SKIP_PERMANENT')))
    ) THEN 1 ELSE 0 END`).bind(ad.id,accountId,runId,rewardPreviewId,ad.multiplier,row.reward_plan_json,row.balance_config_version));
    const result={accepted:true,intentId:ad.id,runId,rewardPreviewId,status:"CLAIMED",multiplier:ad.multiplier,
      policyVersion:adPolicy!.version,rewardClaimId,serverClaimedAt:toIso(now),rewardLines:claimRewardLines};
    statements.push(db.prepare("UPDATE combat_ad_intents SET status='CLAIMED',claim_id=?,result_json=?,write_revision=write_revision+1 WHERE id=? AND status='RESULT_FIXED'")
      .bind(rewardClaimId,JSON.stringify(result),ad.id),db.prepare("DELETE FROM combat_ad_guard"));
  }

  try { await db.batch(statements); }
  catch (error) {
    if(completeResponse){const original=await readCompleteRunClaim(db,accountId,runId,rewardPreviewId,idempotencyKey);if(original)return original;}
    // The NOT NULL account_id fence participates in the same atomic batch as all grants.
    if (/NOT NULL constraint failed: reward_transactions\.account_id/iu.test(String(error))) {
      // A winner can commit after this request read ACTIVE but before it read
      // the account sequence. That schedule hits this fence, not UNIQUE retry.
      const committed = await db.prepare(`SELECT r.id,r.created_at,r.balance_config_version
        FROM run_reward_previews p JOIN reward_transactions r ON r.id=p.reward_claim_id
        WHERE p.id=? AND p.run_id=? AND p.account_id=? AND p.status='CLAIMED'
          AND r.run_id=p.run_id AND r.account_id=p.account_id`)
        .bind(rewardPreviewId,runId,accountId)
        .first<{id:string;created_at:number;balance_config_version:number}>();
      if(committed&&completeResponse){const original=await readCompleteRunClaim(db,accountId,runId,rewardPreviewId,idempotencyKey);if(original)return original;throw new ActivityError(409,'run_claim_complete_unavailable');}
      if (committed) return {
        runId,rewardPreviewId,rewardClaimId:committed.id,status:"DUPLICATE_REPLAY",
        serverClaimedAt:toIso(committed.created_at),balanceVersion:String(committed.balance_config_version),
        rewardLines:[],errorCode:"duplicate_replay",message:"Reward claim was already committed.",
      };
      throw new ActivityError(409, "reward_preview_changed");
    }
    if(completeResponse&&/CHECK constraint failed/u.test(String(error)))throw new ActivityError(409,'run_claim_complete_source_changed');
    throw error;
  }

  if(complete)return complete.response;
  return {
    runId,
    rewardPreviewId,
    rewardClaimId,
    status: "CLAIMED",
    serverClaimedAt: toIso(now),
    balanceVersion: String(row.balance_config_version),
    rewardLines: claimRewardLines,
    errorCode: null,
    message: null,
  };
}



