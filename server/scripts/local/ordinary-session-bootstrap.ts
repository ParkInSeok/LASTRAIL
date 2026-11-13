import { readFileSync, existsSync, realpathSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { join, relative } from 'node:path';
import initSqlJs from 'sql.js';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import { ce03ProducerFixture } from '../../tests/helpers/ce03-producer-fixture';
import { SqlJsD1Database } from '../../tests/helpers/growth-d1';
import { OrdinaryNativeGeometryRegistry, ordinaryGeometryDefinitionSchema } from '../../apps/game-api/src/ordinary-native-geometry';
import { firstPlayableProfileV4 } from '../../apps/game-api/src/combat-first-playable-profile';
import { OperationMapPublisher } from '../../apps/game-api/src/operation-map-publisher';
import { captureOperationMap } from '../../apps/game-api/src/operation-map-plan';
import { ordinaryObstacleDefinition } from '../../apps/game-api/src/ordinary-obstacle-sequence';
import { startDeparture } from '../../apps/game-api/src/departure';
import { OrdinaryCombatLifecycle } from '../../apps/game-api/src/combat-ordinary-combat-lifecycle';
import { OrdinaryNodeEntrySource } from '../../apps/game-api/src/ordinary-node-entry-source';
import { OrdinaryLocalNodeRuntime, ordinaryLocalEntryEffectsSchema } from '../../apps/game-api/src/ordinary-local-entry';
import { OperationMapStore } from '../../apps/game-api/src/operation-map-store';
import { readOrdinaryRunStatOrigin } from '../../apps/game-api/src/combat-ordinary-run-source';

/** Read-only asset validation; does not register geometry or create account rights. */
export async function verifyOrdinaryLocalGeometry(raw: string, expectedGeometryHash: string) {
  if (await sha256Hex(raw) !== expectedGeometryHash) throw new Error('local_geometry_pin_conflict');
  const geometry = ordinaryGeometryDefinitionSchema.parse(JSON.parse(raw));
  if (!('scope' in geometry.source)) throw new Error('local_authored_geometry_required');
  const entries: [string, string][] = [[geometry.source.scenePath, geometry.source.sceneHash], [geometry.source.prefabPath, geometry.source.prefabHash],
    ...Object.entries(geometry.source.originalAssetHashes), ...geometry.entities.map(e => [e.assetPath, e.assetHash] as [string, string]),
    ...(geometry.source.boardingGeometry?.enemyTemplates.map(e => [e.assetPath, e.assetHash] as [string, string]) ?? [])];
  const assets = new Map<string, string>();
  for (const [path, expected] of entries) {
    if (assets.has(path) && assets.get(path) !== expected) throw new Error('local_geometry_asset_conflict');
    assets.set(path, expected);
  }
  const unity = realpathSync('D:/Project/LASTRAIL/unity-project');
  for (const [path, expected] of assets) {
    const actual = realpathSync(join(unity, path)), child = relative(unity, actual);
    if (!child || child.startsWith('..') || child.includes(':') || createHash('sha256').update(readFileSync(actual)).digest('hex') !== expected)
      throw new Error('local_geometry_asset_conflict');
  }
  if (geometry.source.boardingGeometry) {
    const originals = Object.keys(geometry.source.originalAssetHashes).filter(path => path.endsWith('/definition.json'));
    const roles = [
      {path: 'Assets/_LASTRAIL/Content/Battle/OrdinaryLiveGeometryV02/definition.json', id: 'ordinary-live-obstacle-and-five-v2', hash: '642a36d851438d26b24103b28494d72a06d6bcf0c204e41ec4eadfcaa02b8bf2'},
      ...(geometry.source.formationGeometry ? [{path: 'Assets/_LASTRAIL/Content/Battle/OrdinaryBoardingV01/definition.json', id: 'ordinary-live-boarding-v1', hash: 'b3713ea09cbd0cba85b2c996942af12ae275363abd61ce3e83e34dc540b751ed'}] : []),
    ];
    if (originals.length !== roles.length || originals.some(path => !roles.some(r => r.path === path))) throw new Error('local_boarding_original_geometry_missing');
    for (const role of roles) {
    if (geometry.source.originalAssetHashes[role.path] !== role.hash) throw new Error('local_boarding_original_geometry_conflict');
    const original = ordinaryGeometryDefinitionSchema.parse(JSON.parse(readFileSync(join(unity, role.path), 'utf8')));
    if (original.definitionId !== role.id) throw new Error('local_boarding_original_geometry_conflict');
    if ('scope' in original.source) {
      for (const [path, hash] of Object.entries(original.source.originalAssetHashes)) {
        if (geometry.source.originalAssetHashes[path] !== hash) throw new Error('local_boarding_original_geometry_conflict');
      }
      if (geometry.source.originalAssetHashes[original.source.prefabPath] !== original.source.prefabHash)
        throw new Error('local_boarding_original_geometry_conflict');
    }
    for (const entity of original.entities) {
      // The new consist replaces allied bodies; operator/enemy judgement shapes remain original.
      if (geometry.source.formationGeometry && ['Engine', 'Combat', 'Gun'].includes(entity.authoringId)) continue;
      const current = geometry.entities.find(e => e.authoringId === entity.authoringId);
      if (!current || current.role !== entity.role || current.definitionRef !== entity.definitionRef
        || canonicalJson(current.anchors) !== canonicalJson(entity.anchors)
        || canonicalJson(current.shapes) !== canonicalJson(entity.shapes)) throw new Error('local_boarding_original_entity_changed');
    }
    if ('scope' in original.source && original.source.boardingGeometry) {
      for (const template of original.source.boardingGeometry.enemyTemplates) {
        const current = geometry.source.boardingGeometry.enemyTemplates.find(t => t.definitionRef === template.definitionRef);
        if (!current || canonicalJson(current.anchors) !== canonicalJson(template.anchors)
          || canonicalJson(current.shapes) !== canonicalJson(template.shapes)) throw new Error('local_boarding_original_entity_changed');
      }
    }
    }
  }
  return geometry;
}

/** Operator-authored isolated account and first-node spawn. No fabricated combat result or replacement public admission handler. */
export async function prepareOrdinaryLocalSession(directory: string, geometryPath: string, expectedGeometryHash: string, enginePin: unknown) {
  const raw = readFileSync(geometryPath, 'utf8');
  const geometry = await verifyOrdinaryLocalGeometry(raw, expectedGeometryHash);
  if ('scope' in geometry.source && geometry.source.formationGeometry) throw new Error('local_five_car_ownership_binding_unavailable');
  const dbPath = join(directory, 'session.sqlite');
  if (existsSync(dbPath)) {
    const SQL = await initSqlJs(), sql = new SQL.Database(readFileSync(dbPath));
    sql.create_function('unixepoch', () => Date.now() / 1000); sql.run('PRAGMA foreign_keys=ON');
    const db = new SqlJsD1Database(sql) as unknown as D1Database;
    try {
      const rows = await db.prepare('SELECT account_id,run_id,node_id,engine_pin_json FROM ordinary_local_node_scope').all<{ account_id: string; run_id: string; node_id: string; engine_pin_json: string }>();
      if (rows.results.length !== 1 || rows.results[0]!.engine_pin_json !== canonicalJson(enginePin as never)) throw new Error('local_session_origin_conflict');
      const row = rows.results[0]!, original = await readOrdinaryRunStatOrigin(db, row.account_id, row.run_id);
      const reference = JSON.parse(original.startSnapshotJson).ordinaryGeometryReference;
      if (reference.definitionHash !== expectedGeometryHash) throw new Error('local_session_geometry_conflict');
      await new OrdinaryNativeGeometryRegistry(db, reference).read();
      return { sql, db, accountId: row.account_id, runId: row.run_id, nodeId: row.node_id, restored: true };
    } catch (error) { sql.close(); throw error; }
  }
  if ('scope' in geometry.source && geometry.source.boardingGeometry)
    throw new Error('ordinary_train_support_definition_unavailable');
  // Reuse existing account/inventory seed only. Its test combat producer/request is never invoked.
  const seed = await ce03ProducerFixture();
  try {
    const db = seed.env.DB, accountId = 'a', profile = await firstPlayableProfileV4();
    seed.sql.run(readFileSync('apps/game-api/local/ordinary-execution.sql', 'utf8'));
    const nodes = Array.from({ length: 11 }, (_, i) => Array.from({ length: 3 }, (_, branch) => ({
      nodeId: `n${i + 1}-${branch}`, visitDepth: i + 1, eventKind: branch === 0 ? 'COMBAT' : branch === 1 ? 'EQUIPMENT' : 'ELITE',
      publicInfo: { name: i === 0 && branch === 0 ? '로컬 전투 · 낙하 장애물' : '시험 범위 밖', iconId: branch === 0 ? 'combat' : 'unavailable' },
      encounterProfileRef: i === 0 && branch === 0 ? profile.profileId : `u${i}-${branch}`, rewardPlanRef: `r${i}-${branch}`,
    }))).flat();
    nodes.push({ nodeId: 'boss', visitDepth: 12, eventKind: 'BOSS', publicInfo: { name: '시험 범위 밖', iconId: 'unavailable' }, encounterProfileRef: 'ub', rewardPlanRef: 'rb' });
    const parents = [{ nodeId: 'entry', visitDepth: 0 }, ...nodes.filter(n => n.visitDepth < 12)];
    const edges = parents.flatMap(p => nodes.filter(n => n.visitDepth === p.visitDepth + 1).map(n => ({ edgeId: `${p.nodeId}:${n.nodeId}`, fromNodeId: p.nodeId, toNodeId: n.nodeId })));
    const plan = captureOperationMap({ version: 'operation.map-plan.v1', profile: 'ORDINARY_12_NODE', graphId: 'local-obstacle-first-node',
      graphRevision: 1, eventAssignmentVersion: 'local-authored-v1', definitionVersion: 'ordinary.local-content.v1.20261106',
      entryNodeId: 'entry', enabledEventKinds: ['COMBAT', 'EQUIPMENT', 'ELITE'], nodes, edges });
    const publication = await new OperationMapPublisher({ read: async () => ({ version: 'operation.map-catalog.v1', catalogId: 'LOCAL_AUTHORED_OBSTACLE', revision: 1,
      definitionVersion: plan.definitionVersion, encounters: nodes.map(n => ({ ref: n.encounterProfileRef,
        version: n.encounterProfileRef === profile.profileId ? profile.version : 'local-unavailable-v1', profile: plan.profile, eventKind: n.eventKind,
        definition: n.encounterProfileRef === profile.profileId ? { ...profile, ordinaryObstacle: ordinaryObstacleDefinition() } : { localUnavailable: true } })),
      rewards: nodes.map(n => ({ ref: n.rewardPlanRef, version: 'local-review-v1', profile: plan.profile, eventKind: n.eventKind, definition: { localReviewOnly: true } })) }) }).publish(plan);
    const quote = await (await seed.preview()).json() as { operationPreviewId: string };
    const originalPreview = await db.prepare('SELECT * FROM operation_previews WHERE id=?').bind(quote.operationPreviewId).first<Parameters<typeof startDeparture>[2]>();
    if (!originalPreview) throw new Error('local_preview_missing');
    const privatePreview = JSON.parse(originalPreview.preview_json), previewId = `local-preview-${crypto.randomUUID()}`, mutationId = `local-preview-mutation-${crypto.randomUUID()}`;
    const pinned = { ...privatePreview, encounterType: 'ORDINARY_12_NODE', operationMapContent: publication,
      admission: { ...privatePreview.admission, operationMapPlan: plan } };
    seed.sql.run(`INSERT INTO account_mutations(id,account_id,sequence,mutation_type,idempotency_key,created_at)
      SELECT ?,?,COALESCE(MAX(sequence),0)+1,'DEPARTURE_PREVIEW',?,? FROM account_mutations WHERE account_id=?`,
      [mutationId, accountId, previewId, Date.now(), accountId]);
    seed.sql.run(`INSERT INTO operation_previews(id,account_id,mutation_id,objective,risk,balance_config_version,preview_json,status,expires_at,created_at,admission_version)
      VALUES(?,?,?,?,?,?,?,'ACTIVE',?,?,?)`, [previewId, accountId, mutationId, originalPreview.objective, originalPreview.risk, originalPreview.balance_config_version,
      JSON.stringify(pinned), Date.now() + 300000, Date.now(), originalPreview.admission_version ?? null]);
    const preview = (await db.prepare('SELECT * FROM operation_previews WHERE id=?').bind(previewId).first<Parameters<typeof startDeparture>[2]>())!;
    const registry = new OrdinaryNativeGeometryRegistry(db, { definitionId: geometry.definitionId, version: geometry.version, definitionHash: expectedGeometryHash });
    await registry.register(raw);
    const start = await startDeparture(db, accountId, preview, `local-start-${previewId}`, async (p, runId) => ({ accepted: true,
      startStatus: 'STARTED', runId, operationPreviewId: p.id, operationId: p.objective, actId: pinned.actId, regionId: pinned.regionId,
      encounterType: 'ORDINARY_12_NODE', risk: p.risk, balanceVersion: p.balance_config_version }), undefined, 'DEV', undefined, undefined, undefined, registry);
    const runId = String(start.runId), nodeId = nodes[0]!.nodeId;
    await new OrdinaryCombatLifecycle(db).activate(accountId, runId, { requestId: 'local-initialization', source: 'ORIGINAL_START' });
    const original = await readOrdinaryRunStatOrigin(db, accountId, runId), startHash = await sha256Hex(original.startSnapshotJson);
    if (startHash !== await sha256Hex(canonicalJson(JSON.parse(original.startSnapshotJson)))) throw new Error('local_start_hash_not_canonical');
    const effects = ordinaryLocalEntryEffectsSchema.parse({ version: 'ordinary.local-entry-effects.v1.20261106', accountId, runId, nodeId,
      startSnapshotHash: startHash, originGeneration: 1, revision: 1, atTick: 0, activeEffects: [], shields: [], interceptions: [] });
    seed.sql.run('INSERT INTO ordinary_local_node_scope VALUES(?,?,?,1,?,?,?)', [accountId, runId, nodeId, canonicalJson(effects),
      `local-review:${nodeId}`, canonicalJson(enginePin as never)]);
    // This local content begins at its authored first-node spawn. It does not assert an unobserved journey.
    const map = new OperationMapStore(db, { prepare: async context => {
      if (context.command.kind !== 'ARRIVE' || context.command.edgeId !== edges[0]!.edgeId) throw new Error('local_entry_scope_conflict');
      return { proof: { kind: 'ARRIVE', verificationId: `local-authored-spawn:${geometry.definitionId}`, holdIds: [] },
        assertions: [db.prepare(`INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM ordinary_local_node_scope
          WHERE account_id=? AND run_id=? AND node_id=? AND entry_effects_json=? AND engine_pin_json=?)
          AND NOT EXISTS(SELECT 1 FROM ordinary_local_execution WHERE run_id=?) THEN 1 ELSE 0 END`)
          .bind(accountId, runId, nodeId, canonicalJson(effects), canonicalJson(enginePin as never), runId)] };
    } }, new OrdinaryNodeEntrySource(db, new OrdinaryLocalNodeRuntime(db)));
    await map.commit(accountId, runId, { kind: 'CONFIRM', expectedRevision: 1, eventId: 'local-entry-selection', edgeId: edges[0]!.edgeId });
    await map.commit(accountId, runId, { kind: 'ARRIVE', expectedRevision: 2, eventId: 'local-node-entry', edgeId: edges[0]!.edgeId, evidenceRef: 'local-authored-spawn' });
    return { sql: seed.sql, db, accountId, runId, nodeId, restored: false };
  } catch (error) { seed.sql.close(); throw error; }
}
