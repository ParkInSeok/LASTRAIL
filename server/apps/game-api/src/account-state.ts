import type { Context, Hono } from "hono";
import { readBoundedBody, type ActivityAuthResult } from "./activity-routes";
import { ActivityError } from "./activity-policy";

// All subqueries run in one SQLite statement: holdings cannot straddle a committed mutation.
// Table/column expressions below are code constants, never request input.
const collections = [
  ["currencies", "account_currencies", "currency", "'currency',currency,'balance',balance"],
  ["items", "account_items", "item_id", "'itemId',item_id,'quantity',quantity"],
  ["operators", "account_operators", "operator_id", "'operatorId',operator_id,'skillTraining',skill_training,'promotion',promotion,'revision',revision,'acquiredAt',acquired_at"],
  ["trainingCoreSlots", "operator_training_core_slots", "slot_index", "'slot',slot_index,'operatorId',operator_id,'trainedLevel',trained_level,'investedOperatorExp',invested_operator_exp,'revision',revision"],
  ["carriages", "account_carriages", "carriage_id", "'carriageId',carriage_id,'level',level,'revision',revision"],
  ["turrets", "account_turrets", "turret_id", "'turretId',turret_id,'level',level,'revision',revision"],
  ["presets", "commander_presets", "id", "'id',id,'name',name,'definitionJson',preset_json,'revision',revision,'updatedAt',updated_at"],
  ["unlocks", "account_unlocks", "unlock_id", "'unlockId',unlock_id,'unlockedAt',unlocked_at"],
] as const;
const snapshotSql = `SELECT
  (SELECT COALESCE(MAX(sequence),0) FROM account_mutations WHERE account_id=a.id) AS mutationSequence,
  (SELECT json_object('current',fuel,'max',max_fuel,'lastRegenAt',last_regen_at,'updatedAt',updated_at)
    FROM fuel_state WHERE account_id=a.id) AS fuel,
  (SELECT json_object('trainId',train_id,'level',level,'revision',revision,'updatedAt',updated_at)
    FROM account_train WHERE account_id=a.id) AS train,
  (SELECT json_object('activePresetId',preset_id,'activePresetRevision',preset_revision,'revision',revision,
      'operators',json_extract(formation_json,'$.operators'),'updatedAt',updated_at)
    FROM account_formation WHERE account_id=a.id) AS activeFormation,
  ${collections.map(([alias, table, order, fields]) => `(SELECT json_group_array(json_object(${fields}))
    FROM (SELECT * FROM ${table} WHERE account_id=a.id ORDER BY ${order})) AS ${alias}`).join(",\n")}
  FROM accounts a WHERE a.id=?`;

type SnapshotRow = Record<typeof collections[number][0], string> & {
  mutationSequence: number; fuel: string | null; train: string | null; activeFormation: string | null;
};
const iso = (value: number) => new Date(value).toISOString();

export async function readAccountState(db: D1Database, accountId: string) {
  const row = await db.prepare(snapshotSql).bind(accountId).first<SnapshotRow>();
  if (!row) throw new ActivityError(503, "account_state_unavailable");
  const currencies: Record<string, number> = { CREDITS: 0, MACHINE_PARTS: 0, TACTICAL_DATA: 0, SPECIAL_CORE: 0, OPERATOR_EXP: 0 };
  for (const entry of JSON.parse(row.currencies) as { currency: string; balance: number }[]) currencies[entry.currency] = entry.balance;
  const fuel = row.fuel === null ? null : JSON.parse(row.fuel) as { current: number; max: number; lastRegenAt: number; updatedAt: number };
  const train = row.train === null ? null : JSON.parse(row.train) as { trainId: string; level: number; revision: number; updatedAt: number };
  const operators = JSON.parse(row.operators) as { operatorId: string; skillTraining: number; promotion: number; revision: number; acquiredAt: number }[];
  const presets = JSON.parse(row.presets) as { id: string; name: string; definitionJson: string; revision: number; updatedAt: number }[];
  const unlocks = JSON.parse(row.unlocks) as { unlockId: string; unlockedAt: number }[];
  const activeFormation = row.activeFormation === null ? null : JSON.parse(row.activeFormation) as {
    activePresetId: string; activePresetRevision: number; revision: number; operators: { slot: number; operatorId: string }[]; updatedAt: number;
  };
  return {
    accepted: true, accountId, serverTime: iso(Date.now()), mutationSequence: row.mutationSequence,
    currencies, items: JSON.parse(row.items) as { itemId: string; quantity: number }[],
    fuel: fuel === null ? null : { ...fuel, lastRegenAt: iso(fuel.lastRegenAt), updatedAt: iso(fuel.updatedAt) },
    operators: operators.map(o => ({ ...o, acquiredAt: iso(o.acquiredAt) })),
    trainingCoreSlots: JSON.parse(row.trainingCoreSlots) as { slot: number; operatorId: string; trainedLevel: number; investedOperatorExp: number; revision: number }[],
    train: train === null ? null : { ...train, updatedAt: iso(train.updatedAt) },
    carriages: JSON.parse(row.carriages) as { carriageId: string; level: number; revision: number }[],
    turrets: JSON.parse(row.turrets) as { turretId: string; level: number; revision: number }[],
    formation: { ...(activeFormation === null
      ? { activePresetId: null, activePresetRevision: null, revision: 0, operators: [], updatedAt: null }
      : { ...activeFormation, updatedAt: iso(activeFormation.updatedAt) }), activationSupported: true,
      presets: presets.map(p => ({ ...p, updatedAt: iso(p.updatedAt) })) },
    unlocks: unlocks.map(u => ({ ...u, unlockedAt: iso(u.unlockedAt) })),
  };
}

export function registerAccountStateRoutes(app: Hono<{ Bindings: Env }>, authenticate: (c: Context<{ Bindings: Env }>) => Promise<ActivityAuthResult>): void {
  app.get("/v1/account/state", async c => {
    c.header("Cache-Control", "no-store");
    const auth = await authenticate(c);
    if ("errorCode" in auth) return c.json({ accepted: false, errorCode: auth.errorCode }, auth.status);
    try {
      if (new URL(c.req.url).search !== "" || (await readBoundedBody(c)).length !== 0) throw new ActivityError(400, "contract_violation");
      return c.json(await readAccountState(c.env.DB, auth.accountId));
    } catch (error) {
      if (error instanceof ActivityError) return c.json({ accepted: false, errorCode: error.code }, error.status);
      throw error;
    }
  });
}
