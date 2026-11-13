import { z } from 'zod';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import { ActivityError } from './activity-policy';
import { ordinaryNodeExecutionOriginSchema } from './ordinary-node-execution-origin';
import { ordinaryGeometryReferenceSchema } from './ordinary-native-geometry';

const id = z.string().min(1).max(200), revision = z.number().int().positive().safe();
const scope = z.object({ accountId: id, runId: id, nodeId: id });
const geometrySchema = z.strictObject({ ...scope.shape, version: z.literal('ordinary.node-geometry-binding.v1.20261031'),
  entryMapRevision: revision, originGeneration: z.literal(1), definitionRef: ordinaryGeometryReferenceSchema });
const observationSchema = ordinaryNodeExecutionOriginSchema.extend({ nodeId: id, attemptGeneration: revision,
  snapshotHash: z.string().regex(/^[a-f0-9]{64}$/u), geometryBinding: geometrySchema });
const entrySchema = scope.extend({ version: z.literal('ordinary.node-entry-state.v1.20261028'), entryMapRevision: revision,
  runtime: z.object({ ...ordinaryNodeExecutionOriginSchema.shape,
    sequence: z.json().optional(), enemies: z.json().optional(),
    spatial: z.object({ definitionBinding: geometrySchema }),
    actions: z.object({ stepTimeSpanTicks: revision }).optional() }) });
const fail = (): never => { throw new ActivityError(409, 'ordinary_execution_binding_conflict'); };

/** Identity binding only: observing the matching geometry is not proof of LOS or hit.
 * The completion caller owns current map/account guards; this adds the retained origin/attempt fence. */
export async function prepareOrdinaryNodeExecutionBinding(db: D1Database,
  context: { accountId: string; runId: string; nodeId: string; attemptGeneration: number; startSnapshot: string }, input: unknown) {
  const observed = observationSchema.safeParse(input);
  if (!observed.success || observed.data.nodeId !== context.nodeId || observed.data.attemptGeneration !== context.attemptGeneration) return fail();
  const row = await db.prepare('SELECT start_snapshot_json,snapshot_json,snapshot_hash,generation FROM ordinary_node_attempts WHERE run_id=? AND node_id=?')
    .bind(context.runId, context.nodeId).first<{ start_snapshot_json: string; snapshot_json: string; snapshot_hash: string; generation: number }>();
  if (!row || row.start_snapshot_json !== context.startSnapshot || row.generation !== context.attemptGeneration
    || row.snapshot_hash !== observed.data.snapshotHash || await sha256Hex(row.snapshot_json) !== row.snapshot_hash) return fail();
  let entry: z.infer<typeof entrySchema>, pinned: z.infer<typeof ordinaryGeometryReferenceSchema>;
  try {
    entry = entrySchema.parse(JSON.parse(row.snapshot_json));
    pinned = ordinaryGeometryReferenceSchema.parse(JSON.parse(context.startSnapshot).ordinaryGeometryReference);
  } catch { return fail(); }
  const geometry = entry.runtime.spatial.definitionBinding;
  if (entry.accountId !== context.accountId || entry.runId !== context.runId || entry.nodeId !== context.nodeId
    || geometry.accountId !== entry.accountId || geometry.runId !== entry.runId || geometry.nodeId !== entry.nodeId
    || geometry.entryMapRevision !== entry.entryMapRevision || canonicalJson(geometry.definitionRef) !== canonicalJson(pinned)
    || canonicalJson(geometry) !== canonicalJson(observed.data.geometryBinding)
    || entry.runtime.executionVersion !== observed.data.executionVersion
    || canonicalJson(entry.runtime.determinism) !== canonicalJson(observed.data.determinism)) return fail();
  return { binding: observed.data, entry, assertions: [db.prepare(`INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN EXISTS(
    SELECT 1 FROM ordinary_node_attempts WHERE run_id=? AND node_id=? AND generation=?
      AND start_snapshot_json=? AND snapshot_json=? AND snapshot_hash=?) THEN 1 ELSE 0 END`)
    .bind(context.runId, context.nodeId, context.attemptGeneration, context.startSnapshot, row.snapshot_json, row.snapshot_hash)] };
}
