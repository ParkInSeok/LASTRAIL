import { canonicalJson } from '@lastrail/common';
import { ActivityError } from './activity-policy';
import { capturePredepartureEffectApplicability } from './combat-owned-train-profile';
import { readOrdinaryRunStatOrigin } from './combat-ordinary-run-source';


const version = 'combat.ordinary-initial-modifiers.v1.20261023';
const fail = (): never => { throw new ActivityError(409, 'ordinary_stat_modifier_source_unavailable'); };

/** START-only origin: prepareOrdinaryFiveStatOrigin has already excluded selected
 * commander ranks. Research applicability is pinned, while new-run augment and
 * temporary lifecycles have not begun. This is NOT an ongoing absence provider. */
export function captureOrdinaryInitialModifiers(runId: string, originHash: string) {
  return { version, runId, originHash, stage: 'INITIAL_BEFORE_COMBAT' as const,
    applicability: capturePredepartureEffectApplicability(),
    modifiers: Object.fromEntries(['research', 'commander', 'augments', 'temporary'].map(group => [group,
      { sourceId: `${runId}:${group}`, version, revision: 1, status: 'NO_ACTIVE_STAT_MODIFIERS' }])) };
}

/** Historical START artifact only, never a current modifier/phase authority.
 * Restoring this source must not initialize HP or authorize a live stat command. */
export async function readOrdinaryInitialModifiers(db: D1Database, accountId: string, runId: string) {
  const source = await readOrdinaryRunStatOrigin(db, accountId, runId);
  const expected = captureOrdinaryInitialModifiers(runId, source.originHash);
  const pinned = JSON.parse(source.startSnapshotJson).ordinaryInitialModifiers;
  if (!pinned || canonicalJson(pinned) !== canonicalJson(expected)
    || canonicalJson(source.origin.applicability) !== canonicalJson(expected.applicability)) return fail();
  return structuredClone(expected);
}
