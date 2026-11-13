import { canonicalJson, sha256Hex } from "@lastrail/common";
import { z } from "zod";
import { trainingCoreExpCost } from "../../../packages/balance/src/launch-economy";
import { ActivityError, activityId } from "./activity-policy";
import { readEconomyBinding } from "./economy-binding";
import { walletId } from "./crystal-wallet";

const base = { economyVersion: z.string().min(1).max(128).regex(/^[A-Za-z0-9_.-]+$/u),responseVersion:z.literal('growth.command.v2.20261021').optional() };
const slot = z.number().int().min(0).max(4);
export const growthActionSchema = z.discriminatedUnion("kind", [
  z.strictObject({ ...base, kind: z.literal("CORE_LEVEL"), slot, targetLevel: z.number().int().min(2).max(50) }),
  z.strictObject({ ...base, kind: z.literal("CORE_SYNC") }),
  z.strictObject({ ...base, kind: z.literal("CORE_REPLACE"), slot, operatorId: activityId }),
  ...(["TRAIN", "SKILL", "PROMOTION", "EQUIPMENT", "RESEARCH"] as const).map(kind =>
    z.strictObject({ ...base, kind: z.literal(kind), targetId: activityId })),
]);
export type GrowthAction = z.infer<typeof growthActionSchema>;
type Core = { slot_index: number; operator_id: string; trained_level: number; invested_operator_exp: number;revision:number };
type Currency = "CREDITS" | "MACHINE_PARTS" | "TACTICAL_DATA" | "SPECIAL_CORE" | "OPERATOR_EXP";
const safe = (n: number) => {
  if (!Number.isSafeInteger(n) || n < 0) throw new ActivityError(409, "growth_amount_invalid");
  return n;
};

export async function mutateGrowth(input: { db: D1Database; accountId: string }, raw: GrowthAction, key: string) {
  const parsed = growthActionSchema.safeParse(raw);
  if (!parsed.success || !/^[\x21-\x7e]{1,128}$/u.test(key)) throw new ActivityError(400, "contract_violation");
  const action = parsed.data;
  const { db, accountId } = input;
  const {responseVersion:requestedVersion,...originalAction}=action;
  const hash = await sha256Hex(canonicalJson({...originalAction,...(requestedVersion?{responseVersion:requestedVersion}:{})}));
  for (let attempt = 0; attempt < 4; attempt++) {
    const replay = await db.prepare("SELECT request_hash,response_json FROM growth_receipts WHERE account_id=? AND idempotency_key=?")
      .bind(accountId, key).first<{ request_hash: string; response_json: string }>();
    if (replay) {
      if (replay.request_hash !== hash) throw new ActivityError(409, "idempotency_conflict");
      return JSON.parse(replay.response_json) as Record<string, unknown>;
    }
    const sequence = (await db.prepare("SELECT COALESCE(MAX(sequence),0)+1 AS value FROM account_mutations WHERE account_id=?")
      .bind(accountId).first<{ value: number }>())!.value;
    try {
    const binding = await readEconomyBinding(db);
    if (!binding) throw new ActivityError(503, "economy_unconfigured");
    if (binding.version !== action.economyVersion) throw new ActivityError(409, "economy_version_mismatch");
    if (await db.prepare("SELECT id FROM active_combat_runs WHERE account_id=? AND status IN ('ACTIVE','FINISH_PENDING','QUARANTINED') LIMIT 1")
      .bind(accountId).first()) throw new ActivityError(409, "growth_open_run");
    const rows = (await db.prepare("SELECT currency,balance FROM account_currencies WHERE account_id=?")
      .bind(accountId).all<{ currency: Currency; balance: number }>()).results;
    const balances = Object.fromEntries(rows.map(r => [r.currency, safe(r.balance)])) as Partial<Record<Currency, number>>;
    const deltas: Partial<Record<Currency, number>> = {};
    const change = (currency: Currency, delta: number) => { deltas[currency] = (deltas[currency] ?? 0) + delta; };
    const now = Date.now(), mutation = walletId("growth_mut"), receiptId = walletId("growth"), economy = walletId("growth_tx");
    const updates: D1PreparedStatement[] = [];
    const growth = binding.config.growth;
    let result: Record<string, unknown>;
    let ownerChange:Record<string,unknown>;
    if (action.kind === "CORE_LEVEL" || action.kind === "CORE_SYNC" || action.kind === "CORE_REPLACE") {
      const cores = (await db.prepare(`SELECT c.slot_index,c.operator_id,c.trained_level,c.invested_operator_exp,c.revision
        FROM operator_training_core_slots c JOIN account_operators o ON o.account_id=c.account_id AND o.operator_id=c.operator_id
        WHERE c.account_id=? ORDER BY c.slot_index`).bind(accountId).all<Core>()).results;
      const originals = new Map(cores.map(c => [c.slot_index, { ...c }]));
      if (action.kind === "CORE_REPLACE") {
        if (!await db.prepare("SELECT operator_id FROM account_operators WHERE account_id=? AND operator_id=?")
          .bind(accountId, action.operatorId).first()) throw new ActivityError(400, "unknown_growth_target");
        if (cores.some(c => c.operator_id === action.operatorId)) throw new ActivityError(409, "operator_already_in_core");
        const old = cores.find(c => c.slot_index === action.slot);
        change("OPERATOR_EXP", safe(old?.invested_operator_exp ?? 0));
        const replacement = { slot_index: action.slot, operator_id: action.operatorId, trained_level: 1, invested_operator_exp: 0,revision:old?.revision??0 };
        if (old) Object.assign(old, replacement); else cores.push(replacement);
      } else if (action.kind === "CORE_LEVEL") {
        const core = cores.find(c => c.slot_index === action.slot);
        if (!core) throw new ActivityError(400, "unknown_growth_target");
        if (action.targetLevel <= core.trained_level) throw new ActivityError(409, "growth_level_invalid");
        const cost = trainingCoreExpCost(binding.config, core.trained_level, action.targetLevel);
        change("OPERATOR_EXP", -cost); core.trained_level = action.targetLevel;
        core.invested_operator_exp = safe(core.invested_operator_exp + cost);
      } else {
        if (cores.length !== 5) throw new ActivityError(409, "training_core_incomplete");
        let available = balances.OPERATOR_EXP ?? 0;
        while (true) {
          const core = [...cores].sort((a, b) => a.trained_level - b.trained_level || a.slot_index - b.slot_index)[0]!;
          if (core.trained_level >= growth.operatorMaxLevel) break;
          const cost = trainingCoreExpCost(binding.config, core.trained_level, core.trained_level + 1);
          if (cost > available) break;
          available -= cost; change("OPERATOR_EXP", -cost); core.trained_level++;
          core.invested_operator_exp = safe(core.invested_operator_exp + cost);
        }
      }
      for (const core of cores) {
        if (canonicalJson(originals.get(core.slot_index) ?? null) === canonicalJson(core)) continue;
        core.revision=safe(core.revision+1);
        updates.push(db.prepare(`INSERT INTO operator_training_core_slots
          (account_id,slot_index,operator_id,trained_level,invested_operator_exp,revision,last_mutation_id) VALUES(?,?,?,?,?,1,?)
          ON CONFLICT(account_id,slot_index) DO UPDATE SET operator_id=excluded.operator_id,trained_level=excluded.trained_level,
          invested_operator_exp=excluded.invested_operator_exp,revision=operator_training_core_slots.revision+1,last_mutation_id=excluded.last_mutation_id`)
          .bind(accountId, core.slot_index, core.operator_id, core.trained_level, core.invested_operator_exp, mutation));
      }
      cores.sort((a, b) => a.slot_index - b.slot_index);
      result = { slots: cores.map(({revision:ownerRevision,...legacy})=>{void ownerRevision;return legacy;}) };
      ownerChange={coverage:['TRAINING_CORE_SLOTS','CURRENCIES'],trainingCoreSlots:cores.map(c=>({slot:c.slot_index,operatorId:c.operator_id,trainedLevel:c.trained_level,investedOperatorExp:c.invested_operator_exp,revision:c.revision}))};
    } else {
      if (action.kind === "EQUIPMENT" || action.kind === "RESEARCH") throw new ActivityError(400, "unknown_growth_target");
      const train = action.kind === "TRAIN";
      const target = train
        ? await db.prepare("SELECT level,revision FROM account_train WHERE account_id=? AND train_id=?").bind(accountId, action.targetId).first<{ level: number;revision:number }>()
        : await db.prepare(`SELECT ${action.kind === "SKILL" ? "skill_training" : "promotion"} AS level,revision FROM account_operators WHERE account_id=? AND operator_id=?`)
          .bind(accountId, action.targetId).first<{ level: number;revision:number }>();
      if (!target) throw new ActivityError(400, "unknown_growth_target");
      const costs = train ? growth.trainCoreStepsCreditsPartsCores
        : action.kind === "SKILL" ? growth.skillStepsCreditsData : growth.promotionStepsCreditsDataCores;
      const cost = costs[target.level - (action.kind === "PROMOTION" ? 0 : 1)];
      if (!cost) throw new ActivityError(409, "growth_max_level");
      change("CREDITS", -cost[0]); change(train ? "MACHINE_PARTS" : "TACTICAL_DATA", -cost[1]);
      if (cost.length === 3) change("SPECIAL_CORE", -cost[2]!);
      updates.push(train ? db.prepare("UPDATE account_train SET level=?,revision=revision+1,updated_at=? WHERE account_id=? AND train_id=?")
        .bind(target.level + 1, now, accountId, action.targetId)
        : db.prepare(`UPDATE account_operators SET ${action.kind === "SKILL" ? "skill_training" : "promotion"}=?,revision=revision+1,last_mutation_id=? WHERE account_id=? AND operator_id=?`)
          .bind(target.level + 1, mutation, accountId, action.targetId));
      result = { targetId: action.targetId, level: target.level + 1 };
      ownerChange=train?{coverage:['TRAIN','CURRENCIES'],train:{trainId:action.targetId,level:target.level+1,revision:safe(target.revision+1),updatedAt:new Date(now).toISOString()}}
       :{coverage:['OPERATORS','CURRENCIES'],operator:{operatorId:action.targetId,revision:safe(target.revision+1),[action.kind==='SKILL'?'skillTraining':'promotion']:target.level+1}};
    }
    const changes = Object.entries(deltas).filter(([, delta]) => delta !== 0) as [Currency, number][];
    for (const [currency, delta] of changes) {
      if ((balances[currency] ?? 0) + delta < 0) throw new ActivityError(409, "insufficient_balance");
      safe((balances[currency] ?? 0) + delta);
    }
    const response = { accepted: true, receiptId, economyVersion: binding.version, economyHash: binding.sha256,
      balanceVersion: binding.balanceVersion, kind: action.kind, ...result,
      currencyChanges: changes.map(([currency, delta]) => ({ currency, delta, balanceAfter: (balances[currency] ?? 0) + delta })),
      ...(action.responseVersion?{contractVersion:action.responseVersion,refreshRequired:false,owners:{account:{kind:'PATCH',beforeMutationSequence:sequence-1,mutationSequence:sequence,...ownerChange,
       currencies:changes.map(([currency,delta])=>({currency,balance:(balances[currency]??0)+delta}))}},invalidates:['DEPARTURE_PREVIEW']}: {}) };
    const statements = [
      db.prepare("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,idempotency_key,created_at) VALUES(?,?,?,'GROWTH',?,?)")
        .bind(mutation, accountId, sequence, key, now),
      db.prepare(`INSERT INTO growth_write_guard(valid) SELECT CASE WHEN
        EXISTS(SELECT 1 FROM balance_config_active WHERE singleton_id=1 AND version=?)
        AND NOT EXISTS(SELECT 1 FROM active_combat_runs WHERE account_id=? AND status IN ('ACTIVE','FINISH_PENDING','QUARANTINED'))
        THEN 1 ELSE 0 END`).bind(binding.balanceVersion, accountId),
      db.prepare("DELETE FROM growth_write_guard"),
      db.prepare("INSERT INTO economy_transactions(id,account_id,mutation_id,reason,source_type,source_id,created_at) VALUES(?,?,?,'GROWTH','GROWTH',?,?)")
        .bind(economy, accountId, mutation, receiptId, now),
      ...changes.flatMap(([currency, delta]) => [
        db.prepare("INSERT INTO account_currencies(account_id,currency,balance,updated_at) VALUES(?,?,?,?) ON CONFLICT(account_id,currency) DO UPDATE SET balance=account_currencies.balance+?,updated_at=excluded.updated_at")
          .bind(accountId, currency, Math.max(0, delta), now, delta),
        db.prepare("INSERT INTO currency_ledger(id,account_id,transaction_id,currency,delta,balance_after,created_at) VALUES(?,?,?,?,?,?,?)")
          .bind(walletId("growth_ledger"), accountId, economy, currency, delta, (balances[currency] ?? 0) + delta, now),
      ]), ...updates,
      db.prepare("INSERT INTO progression_ledger(id,account_id,mutation_id,progression_type,target_id,delta_json,created_at) VALUES(?,?,?,?,?,?,?)")
        .bind(walletId("growth_progress"), accountId, mutation, action.kind, "slot" in action ? String(action.slot) : "targetId" in action ? action.targetId : "training-core", JSON.stringify(response), now),
      db.prepare("INSERT INTO growth_receipts(id,account_id,idempotency_key,request_hash,economy_version,mutation_id,response_json,created_at) VALUES(?,?,?,?,?,?,?,?)")
        .bind(receiptId, accountId, key, hash, binding.version, mutation, JSON.stringify(response), now),
    ];
    try { await db.batch(statements); return response; }
    catch (error) {
      if (/UNIQUE constraint failed|CHECK constraint failed: valid=1/iu.test(String(error))) {
        if (attempt < 3) continue;
        throw new ActivityError(409, "growth_write_conflict");
      }
      throw error;
    }
    } catch (error) {
      // Another request may commit after our initial replay read but before a
      // subsequent validation read. Its completed receipt still takes precedence.
      if (error instanceof ActivityError) {
        const committed = await db.prepare("SELECT request_hash,response_json FROM growth_receipts WHERE account_id=? AND idempotency_key=?")
          .bind(accountId, key).first<{ request_hash: string; response_json: string }>();
        if (committed) {
          if (committed.request_hash !== hash) throw new ActivityError(409, "idempotency_conflict");
          return JSON.parse(committed.response_json) as Record<string, unknown>;
        }
      }
      throw error;
    }
  }
  throw new ActivityError(409, "growth_write_conflict");
}
