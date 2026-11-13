import { z } from 'zod';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import { ActivityError } from './activity-policy';
import type { OrdinaryNodeEntryRuntimeOwner } from './ordinary-node-entry-source';
import { cooldownTimeSpanToNodeSteps } from './ordinary-node-actions';
import { prepareOrdinaryLocalBoardingSource } from './ordinary-local-boarding-source';
import { readOrdinaryRunStatOrigin } from './combat-ordinary-run-source';

const id = z.string().min(1).max(200);
export const ordinaryLocalEntryEffectsSchema = z.strictObject({ version: z.literal('ordinary.local-entry-effects.v1.20261106'),
  accountId: id, runId: id, nodeId: id, startSnapshotHash: z.string().regex(/^[a-f0-9]{64}$/u),
  originGeneration: z.literal(1), revision: z.literal(1), atTick: z.literal(0),
  activeEffects: z.array(z.never()).length(0), shields: z.array(z.never()).length(0), interceptions: z.array(z.never()).length(0) });
const fail = (): never => { throw new ActivityError(409, 'ordinary_local_entry_source_conflict'); };

/** Fresh local node entry only. The immutable SQL effect origin is created before any accepted game input.
 * Dynamic effects after tick zero belong to the same shared execution model, never these initial arrays. */
export class OrdinaryLocalNodeRuntime implements OrdinaryNodeEntryRuntimeOwner {
  constructor(private readonly db: D1Database) {}
  async prepare(input: Parameters<OrdinaryNodeEntryRuntimeOwner['prepare']>[0]) {
    // New boarding entries cannot inherit retired operator interception while train definitions are pending.
    if (input.geometry && 'scope' in input.geometry.definition.source && input.geometry.definition.source.boardingGeometry)
      throw new ActivityError(409, 'ordinary_train_support_definition_unavailable');
    const row = await this.db.prepare('SELECT revision,entry_effects_json FROM ordinary_local_node_scope WHERE account_id=? AND run_id=? AND node_id=?')
      .bind(input.accountId, input.runId, input.nodeId).first<{ revision: number; entry_effects_json: string }>();
    if (!row || row.revision !== 1 || !input.executionOrigin || !input.geometry || !input.enemies || !input.sequence || !input.entry.automation) return fail();
    const effects = ordinaryLocalEntryEffectsSchema.parse(JSON.parse(row.entry_effects_json));
    if (effects.accountId !== input.accountId || effects.runId !== input.runId || effects.nodeId !== input.nodeId
      || effects.startSnapshotHash !== await sha256Hex(input.startSnapshot)) return fail();
    const geometry = input.geometry.definition;
    if (!('scope' in geometry.source) || geometry.source.scope !== 'LOCAL_AUTHORED_JUDGEMENT_GEOMETRY_NOT_APPROVED_ART_CALIBRATION') return fail();
    if (input.entry.actors.some(a => !geometry.entities.some(e => e.role === 'OPERATOR' && e.definitionRef === a.operatorId))) return fail();
    const stepTimeSpanTicks = 500000;
    const boardingSource = geometry.source.boardingGeometry ? await prepareOrdinaryLocalBoardingSource(
      await readOrdinaryRunStatOrigin(this.db, input.accountId, input.runId), input.nodeId, input.executionOrigin) : undefined;
    if (boardingSource && (geometry.source.boardingGeometry!.cars.length !== boardingSource.packet.physicalHostIds.length
      || geometry.source.boardingGeometry!.cars.some((car, index) => car.carOrder !== index))) return fail();
    const state = { ...input.executionOrigin, sequence: input.sequence, enemies: input.enemies, effects,
      ...(boardingSource ? { boardingSourceHash: boardingSource.sourceHash } : {}),
      spatial: { definitionBinding: input.geometry.binding }, actions: { version: 'ordinary.node-actions.v1.20261030',
        stepTimeSpanTicks, ultimateAuto: input.entry.automation.ultimateAuto, actors: input.entry.actors.map(actor => {
          const action = input.entry.automation!.actors.find(a => a.operatorId === actor.operatorId);
          if (!action) return fail();
          return { operatorId: actor.operatorId, basicNextOpportunityInTicks: 0,
            activeCooldownRemainingTicks: cooldownTimeSpanToNodeSteps(action.activeCooldownRemainingTicks, stepTimeSpanTicks),
            ultimateCharge: action.ultimateCharge };
        }) } };
    return { state: JSON.parse(canonicalJson(state)), assertions: [this.db.prepare(`INSERT INTO operation_map_write_guard(valid)
      SELECT CASE WHEN EXISTS(SELECT 1 FROM ordinary_local_node_scope WHERE account_id=? AND run_id=? AND node_id=?
        AND revision=1 AND entry_effects_json=?) AND NOT EXISTS(SELECT 1 FROM ordinary_local_execution WHERE run_id=? AND node_id=?)
      THEN 1 ELSE 0 END`).bind(input.accountId, input.runId, input.nodeId, row.entry_effects_json, input.runId, input.nodeId)] };
  }
}
