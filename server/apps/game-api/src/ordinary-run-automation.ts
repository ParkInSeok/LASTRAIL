import { z } from 'zod';
import { ActivityError } from './activity-policy';
import { canonicalJson } from '@lastrail/common';
import { CombatUltimateState } from './combat-ultimate-state';
import type { readOrdinaryRunStatOrigin } from './combat-ordinary-run-source';

export const ordinaryActionCarryoverSchema = z.array(z.strictObject({ operatorId: z.string().min(1).max(200),
  activeCooldownRemainingTicks: z.number().int().nonnegative().safe(), ultimateCharge: z.number().int().min(0).max(200) }))
  .length(5).refine(xs => new Set(xs.map(x => x.operatorId)).size === 5);
export const ordinaryAutomationSchema = z.strictObject({ version: z.literal('ordinary.run-actions.v1.20261031'),
  revision: z.number().int().positive().safe(), ultimateAuto: z.boolean(), actors: ordinaryActionCarryoverSchema });
const fail = (): never => { throw new ActivityError(409, 'ordinary_automation_source_conflict'); };

/** Called only while creating a new START, after the original policy and roster were validated. */
export function initialOrdinaryRunActions(origin: Awaited<ReturnType<typeof readOrdinaryRunStatOrigin>>['origin']) {
  const policy = z.object({ actionRules: z.object({ firstActiveReadyMs: z.literal(0) }),
    ultimate: z.object({ initialCharge: z.literal(0) }) }).parse(origin.policy);
  return ordinaryActionCarryoverSchema.parse(origin.actors.map(a => ({ operatorId: a.operatorId,
    activeCooldownRemainingTicks: policy.actionRules.firstActiveReadyMs * 10000,
    ultimateCharge: CombatUltimateState.start(a.operatorId).snapshot().charge })));
}

/** The current confirmed run setting; no row means historical absence, never OFF. */
export async function readOrdinaryRunAutomation(db: D1Database, accountId: string, runId: string) {
  const row = await db.prepare('SELECT account_id,revision,ultimate_auto,actions_json FROM ordinary_run_automation WHERE run_id=?')
    .bind(runId).first<{ account_id: string; revision: number; ultimate_auto: number; actions_json: string }>();
  if (!row) return undefined;
  if (row.account_id !== accountId || ![0, 1].includes(row.ultimate_auto)) return fail();
  const state = ordinaryAutomationSchema.parse({ version: 'ordinary.run-actions.v1.20261031',
    revision: row.revision, ultimateAuto: row.ultimate_auto === 1, actors: JSON.parse(row.actions_json) });
  return { state, assertion: db.prepare(`INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN EXISTS(
    SELECT 1 FROM ordinary_run_automation WHERE run_id=? AND account_id=? AND revision=? AND ultimate_auto=? AND actions_json=?)
    THEN 1 ELSE 0 END`).bind(runId, accountId, row.revision, row.ultimate_auto, row.actions_json) };
}

/** Only a trusted COMPLETE proof supplies the final setting; caller commits with its map CAS and receipt. */
export async function prepareOrdinaryAutomationCompletion(db: D1Database, accountId: string, runId: string,
  ultimateAuto: boolean | undefined, actions: unknown) {
  const owned = await readOrdinaryRunAutomation(db, accountId, runId);
  if (!owned) return undefined;
  if (typeof ultimateAuto !== 'boolean' || owned.state.revision === Number.MAX_SAFE_INTEGER) return fail();
  const parsed = ordinaryActionCarryoverSchema.safeParse(actions);
  if (!parsed.success || parsed.data.some(a => !owned.state.actors.some(o => o.operatorId === a.operatorId))) return fail();
  const actors = owned.state.actors.map(o => parsed.data.find(a => a.operatorId === o.operatorId)!);
  const state = ordinaryAutomationSchema.parse({ ...owned.state, revision: owned.state.revision + 1, ultimateAuto, actors });
  return { state, statements: [owned.assertion,
    db.prepare('UPDATE ordinary_run_automation SET revision=?,ultimate_auto=?,actions_json=? WHERE run_id=? AND account_id=? AND revision=?')
      .bind(state.revision, ultimateAuto ? 1 : 0, canonicalJson(actors), runId, accountId, owned.state.revision)] };
}
