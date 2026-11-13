import { CombatHealthStore } from './combat-health-store';
import { OrdinaryObstacleSequence, ordinaryCombatDefinitionMatches, prepareOrdinaryObstacleSequence, readOrdinaryObstacleDefinition } from './ordinary-obstacle-sequence';
import { replayOrdinaryGunnerHealth } from './ordinary-gunner-health-replay';
import { prepareOrdinaryNodeExecutionBinding } from './ordinary-node-execution-binding';
import { verifyOrdinarySceneObservations } from './ordinary-scene-observations';
import { OrdinaryNativeGeometryRegistry } from './ordinary-native-geometry';
import { readOrdinaryRailFromOriginal } from './combat-ordinary-rail-source';
import { verifyOrdinarySupportAttribution } from './ordinary-support-attribution';
import { verifyResolvedArtilleryImpact } from './rail-artillery-hazard';
import { reinaBarrageDefinition, replayReinaBarrage, verifyReinaBarrageChargeTransition } from './reina-barrage';
import { ordinaryKitV2Version, readOrdinaryKitFromOriginal } from './combat-ordinary-kit-source';
import { z } from 'zod';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import { ActivityError } from './activity-policy';
import { ordinaryActionCarryoverSchema } from './ordinary-run-automation';
import { firstEncounterObjectiveDefinition } from './combat-first-encounter-objective';
import { firstBattleEncounterSourceV6Version, firstBattleEncounterSourceV7Version, firstBattleEncounterSourceV8Version, readFirstBattleEncounterFromOriginal, resolveFirstBattleProfile } from './combat-first-battle-source';
import { readOrdinaryRunStatOrigin } from './combat-ordinary-run-source';
import { readOrdinaryTrainFromOriginal } from './combat-ordinary-train-source';
import { capturePublishedOperationMap } from './operation-map-publisher';
import { projectOperationMap } from './operation-map-plan';
import type { OperationMapVerifier } from './operation-map-store';

const id = z.string().min(1).max(200), hash = z.string().regex(/^[a-f0-9]{64}$/u);
const natural = z.number().int().nonnegative().safe(), revision = natural.min(1);
const reference = z.strictObject({ version: id, definitionHash: hash });
export const firstObjectiveEvidenceSchema = z.strictObject({
  version: z.literal('combat.first-objective-evidence.v1.20261028'),
  accountId: id, runId: id, nodeId: id, encounterId: id, evidenceRef: id, mapRevision: revision,
  attemptGeneration: revision.optional(),
  startSnapshotHash: hash, sourceHash: hash, profileHash: hash,
  objective: reference, nodeObjectives: z.array(reference).min(1).max(100),
  checkpoint: z.strictObject({ revision, hash, atMs: natural }),
  issuedGunnerIds: z.array(id).length(2).refine(xs => new Set(xs).size === 2),
  defeats: z.array(z.strictObject({ gunnerId: id, eventId: id, atMs: natural })).length(2),
  issuedHazardId: id,
  hazard: z.strictObject({ hazardId: id, eventId: id, outcome: z.enum(['HIT', 'MISSED', 'CANCELLED_BEFORE_LAUNCH']),
    launchedAtMs: natural.nullable(), resolvedAtMs: natural }),
  targets: z.array(z.strictObject({ targetId: id, hp: natural, maxHp: revision })
    .refine(t => t.hp <= t.maxHp)).length(6),
  unresolvedProjectileIds: z.array(id).max(1000), remainingThreatIds: z.array(id).max(1000),
  ultimateAuto: z.boolean().optional(),
  actions: ordinaryActionCarryoverSchema.optional(),
});
type Context = Parameters<OperationMapVerifier['prepare']>[0];

/** Trusted server boundary, NOT a parser for a client-submitted proof.
 * Resolve the original issuance, published node's entire objective set and committed checkpoint.
 * Verify defeat/hazard provenance, cancellation authority and final HP/shields/effects at that revision.
 * Resolve reward/transaction holdIds from their existing owners; fence those records too.
 * Fence ALL inspected records with operation_map_write_guard assertions in the caller's batch.
 * Self hashes, client booleans and unconditional production assertions are not accepted authority.
 * No production evidence owner is installed until those original records are available. */
export interface FirstObjectiveEvidenceOwner {
  /** Include every support result from the independently replayed current attempt.
   * Its configuration/actions/results must be covered by the same source assertions. */
  prepare(context: Readonly<Context>): Promise<{ evidence: unknown; holdIds: string[]; assertions: D1PreparedStatement[];
    supportAttribution?: unknown;
    /** Complete original unshielded gunner damage execution; numeric/protection sources
     * and original event order must be independently verified by this owner. */
    gunnerHealthReplay?: unknown;
    /** Identity of the actual node source, separate from its geometry observations. */
    executionBinding?: unknown;
    /** Complete captures from the same native pose owner; matching local geometry is not motion authority. */
    sceneObservations?: unknown;
    /** Resolution from the same owned attempt execution; fence contact, tail clearance and residual threats. */
    obstacleResolution?: unknown;
    /** Actual allied HP transitions from independently executed current-attempt events, committed with COMPLETE. */
    healthChanges?: unknown;
    /** Original issued warning, exact impact frame/numeric sources and step must
     * be independently owned and fenced alongside the evidence, never client claims. */
    artilleryReplay?: unknown;
    /** All casts and original acceptance/geometry/charge observations from the same independently verified execution. */
    reinaBarrages?: unknown }>;
}
const json = (x: unknown) => canonicalJson(z.json().parse(x));
const fail = (code: string): never => { throw new ActivityError(409, 'ordinary_objective_' + code); };

/** Stable issuance from the original accepted ARRIVE; no catalog lookup or extra ID ledger. */
export async function firstObjectiveEncounterIdentities(input: unknown) {
  const origin = z.strictObject({ accountId: id, runId: id, encounterId: id, sourceHash: hash }).parse(input);
  const issue = async (role: string) => (await sha256Hex(json({
    version: 'combat.first-objective-identity.v1.20261028', ...origin, role }))).slice(0, 32);
  return { issuedGunnerIds: await Promise.all(['gunner-0', 'gunner-1'].map(issue)), issuedHazardId: await issue('single-artillery') };
}

/** Only COMPLETE; map state, receipts, HP and same-node encounter state retain their existing owners. */
export class OrdinaryFirstObjectiveVerifier implements OperationMapVerifier {
  constructor(private readonly db: D1Database, private readonly owner?: FirstObjectiveEvidenceOwner) {}
  async prepare(context: Context) {
    const c = context.command;
    if (c.kind !== 'COMPLETE') return fail('command_unsupported');
    if (!this.owner) return fail('evidence_unavailable');
    const original = await readOrdinaryRunStatOrigin(this.db, context.accountId, context.runId);
    if (original.startSnapshotJson !== context.startSnapshot) return fail('source_conflict');
    const source = await readFirstBattleEncounterFromOriginal(original);
    const train = await readOrdinaryTrainFromOriginal(original);
    if (!source || ![firstBattleEncounterSourceV6Version, firstBattleEncounterSourceV7Version, firstBattleEncounterSourceV8Version].includes(source.packet.version) || !train)
      return fail('version_unsupported');
    const profile = await resolveFirstBattleProfile(source);
    if (!profile) return fail('version_unsupported');
    const node = context.plan.nodes.find(n => n.nodeId === c.nodeId);
    if (!node || node.eventKind !== 'COMBAT' || node.visitDepth > 11 || context.state.phase !== 'ACTIVE'
      || context.state.holdIds.length || projectOperationMap(context.plan, context.state.traversedEdgeIds).currentNodeId !== c.nodeId)
      return fail('node_conflict');
    const publication = await capturePublishedOperationMap(JSON.parse(context.startSnapshot).operationMapContent, context.plan);
    const entry = publication.encounters.find(e => e.ref === node.encounterProfileRef);
    if (!entry || entry.ref !== profile.profileId || entry.version !== profile.version || !ordinaryCombatDefinitionMatches(entry.definition, profile))
      return fail('profile_conflict');
    const supplied = await this.owner.prepare(structuredClone(context));
    const parsed = firstObjectiveEvidenceSchema.safeParse(supplied?.evidence);
    const holds = z.array(id).max(1000).refine(xs => new Set(xs).size === xs.length).safeParse(supplied?.holdIds);
    if (!parsed.success || !holds.success || !Array.isArray(supplied.assertions) || !supplied.assertions.length
      || supplied.assertions.length > 128 || supplied.assertions.some(s => typeof s?.bind !== 'function')) return fail('evidence_invalid');
    const e = parsed.data, objective = await firstEncounterObjectiveDefinition();
    const ref = { version: objective.version, definitionHash: objective.definitionHash };
    if (e.accountId !== context.accountId || e.runId !== context.runId || e.nodeId !== c.nodeId
      || e.evidenceRef !== c.evidenceRef || e.mapRevision !== c.expectedRevision
      || e.startSnapshotHash !== await sha256Hex(context.startSnapshot) || e.sourceHash !== source.sourceHash
      || e.profileHash !== profile.definitionHash || json(e.objective) !== json(ref)) return fail('scope_conflict');
    // Bind the evidence to the already committed original ARRIVE, not an arbitrary encounter label.
    const arrival = await this.db.prepare('SELECT response_json FROM operation_map_receipts WHERE run_id=? AND event_id=?')
      .bind(context.runId, e.encounterId).first<{ response_json: string }>();
    const scaling = arrival ? JSON.parse(arrival.response_json).firstPlayableScaling : null;
    if (!scaling || scaling.encounterId !== e.encounterId || scaling.nodeId !== c.nodeId
      || scaling.sourceHash !== source.sourceHash || scaling.profileHash !== profile.definitionHash
      || scaling.progressRevision > c.expectedRevision) return fail('arrival_conflict');
    const issued = await firstObjectiveEncounterIdentities({ accountId: context.accountId, runId: context.runId,
      encounterId: e.encounterId, sourceHash: source.sourceHash });
    if (json(e.issuedGunnerIds) !== json(issued.issuedGunnerIds) || e.issuedHazardId !== issued.issuedHazardId)
      return fail('issuance_conflict');
    if (json(e.nodeObjectives) !== json([ref]) || e.remainingThreatIds.length || e.unresolvedProjectileIds.length)
      return fail('incomplete');
    const ids = [...original.origin.actors.map(a => a.operatorId), train.packet.train.trainId];
    if (new Set(e.targets.map(t => t.targetId)).size !== 6 || e.targets.some(t => !ids.includes(t.targetId))
      || e.targets.find(t => t.targetId === train.packet.train.trainId)!.hp === 0) return fail('survival_unproven');
    if (e.issuedGunnerIds.some(g => ids.includes(g)) || new Set(e.defeats.map(d => d.gunnerId)).size !== 2
      || new Set([...e.defeats.map(d => d.eventId), e.hazard.eventId]).size !== 3
      || e.defeats.some(d => !e.issuedGunnerIds.includes(d.gunnerId) || d.atMs > e.checkpoint.atMs)
      || e.hazard.hazardId !== e.issuedHazardId || e.hazard.resolvedAtMs > e.checkpoint.atMs)
      return fail('events_invalid');
    if (e.hazard.outcome === 'CANCELLED_BEFORE_LAUNCH' ? e.hazard.launchedAtMs !== null
      : e.hazard.launchedAtMs === null || e.hazard.launchedAtMs > e.hazard.resolvedAtMs) return fail('hazard_unresolved');
    if (e.attemptGeneration !== c.attemptGeneration) return fail('attempt_conflict');
    const executionAssertions: D1PreparedStatement[] = [];
    const obstacle = await readOrdinaryObstacleDefinition(JSON.parse(context.startSnapshot), c.nodeId);
    if (obstacle && supplied.executionBinding === undefined) return fail('obstacle_binding_required');
    if (!obstacle && supplied.obstacleResolution !== undefined) return fail('obstacle_unissued');
    if (supplied.executionBinding !== undefined) {
      if (c.attemptGeneration === undefined) return fail('attempt_conflict');
      const verified = await prepareOrdinaryNodeExecutionBinding(this.db, {
        accountId: context.accountId, runId: context.runId, nodeId: c.nodeId,
        attemptGeneration: c.attemptGeneration, startSnapshot: context.startSnapshot }, supplied.executionBinding);
      executionAssertions.push(...verified.assertions);
      if (obstacle) {
        const pin = await prepareOrdinaryObstacleSequence(JSON.parse(context.startSnapshot), c.nodeId, verified.entry.runtime.enemies);
        if (!pin || json(pin) !== json(verified.entry.runtime.sequence ?? null)) return fail('obstacle_sequence_conflict');
        const sequence = new OrdinaryObstacleSequence(pin, { accountId: context.accountId, runId: context.runId,
          nodeId: c.nodeId, attemptGeneration: c.attemptGeneration, snapshotHash: verified.binding.snapshotHash });
        const transition = sequence.resolve(supplied.obstacleResolution);
        if (transition.phase !== 'COMBAT' || transition.atMs > e.checkpoint.atMs
          || e.defeats.some(d => d.atMs < transition.atMs) || e.hazard.resolvedAtMs < transition.atMs
          || e.hazard.launchedAtMs !== null && e.hazard.launchedAtMs < transition.atMs) return fail('obstacle_wave_order');
      } else if (verified.entry.runtime.sequence !== undefined) return fail('obstacle_unissued');
      if (supplied.sceneObservations !== undefined) {
        const actions = verified.entry.runtime.actions;
        if (!actions) return fail('scene_observation_invalid');
        const registered = await new OrdinaryNativeGeometryRegistry(this.db, verified.binding.geometryBinding.definitionRef).read();
        const rail = await readOrdinaryRailFromOriginal(original, train);
        if (!rail?.packet.physicalConsist) return fail('scene_observation_invalid');
        try { verifyOrdinarySceneObservations(registered.definition, {
          accountId: context.accountId, runId: context.runId, nodeId: c.nodeId, attemptGeneration: c.attemptGeneration,
          snapshotHash: verified.binding.snapshotHash, definitionHash: registered.reference.definitionHash,
          entryMapRevision: verified.entry.entryMapRevision, mapRevision: c.expectedRevision,
          stepTimeSpanTicks: actions.stepTimeSpanTicks, checkpointAtMs: e.checkpoint.atMs,
          operatorIds: original.origin.actors.map(a => a.operatorId), gunnerIds: issued.issuedGunnerIds,
          trainIds: [train.packet.train.trainId, ...rail.packet.physicalConsist.instanceIds,
            ...(scaling.enemySpatial ? [scaling.enemySpatial.physicalInstanceId] : [])],
          deviceIds: [issued.issuedHazardId] }, supplied.sceneObservations); }
        catch { return fail('scene_observation_invalid'); }
        executionAssertions.push(registered.assertion);
      }
    } else if (supplied.sceneObservations !== undefined) {
      return fail('scene_observation_binding_required');
    }
    if (supplied.gunnerHealthReplay !== undefined) {
      const input = z.strictObject({ events: z.unknown(), result: z.unknown() }).safeParse(supplied.gunnerHealthReplay);
      if (!input.success || c.attemptGeneration === undefined) return fail('gunner_health_invalid');
      let replay: ReturnType<typeof replayOrdinaryGunnerHealth>;
      try { replay = replayOrdinaryGunnerHealth({ gunnerIds: issued.issuedGunnerIds, hp: scaling.applied.gunnerHp }, input.data.events); }
      catch { return fail('gunner_health_invalid'); }
      if (json(replay) !== json(input.data.result) || replay.targets.some(t => t.hp !== 0)
        || replay.receipts.some(r => r.atMs > e.checkpoint.atMs)
        || replay.defeats.length !== e.defeats.length || replay.defeats.some(d => !e.defeats.some(expected => json(d) === json(expected))))
        return fail('gunner_health_source_conflict');
    }
    if (supplied.supportAttribution !== undefined) {
      const support = z.strictObject({ configurationHash: hash, actions: z.array(z.unknown()).max(10000),
        rows: z.array(z.unknown()).max(10000) }).safeParse(supplied.supportAttribution);
      if (!support.success || c.attemptGeneration === undefined) return fail('support_invalid');
      await verifyOrdinarySupportAttribution(original, support.data.configurationHash, support.data.actions,
        support.data.rows, { nodeId: c.nodeId, generation: c.attemptGeneration });
    }
    if (supplied.artilleryReplay !== undefined) {
      const replay = z.strictObject({ definition: z.unknown(), impact: z.unknown(), stepMs: revision }).safeParse(supplied.artilleryReplay);
      if (!replay.success || c.attemptGeneration === undefined || e.hazard.outcome === 'CANCELLED_BEFORE_LAUNCH')
        return fail('artillery_invalid');
      let verified: ReturnType<typeof verifyResolvedArtilleryImpact>;
      try { verified = verifyResolvedArtilleryImpact(replay.data.definition, replay.data.impact, replay.data.stepMs); }
      catch { return fail('artillery_invalid'); }
      const d = verified.definition, a = profile.artillery;
      const launchAt = BigInt(d.previewAtMs) + BigInt(a.launchAfterPreviewMs);
      if (d.runId !== context.runId || d.trainId !== train.packet.train.trainId || d.eventId !== e.issuedHazardId
        || d.version !== a.hazardVersion || json(d.damage) !== json(scaling.applied.artilleryRawDamage)
        || d.endMm - d.startMm !== a.dangerLengthMm
        || BigInt(d.timing.impactAtMs) !== BigInt(d.previewAtMs) + BigInt(a.impactAfterPreviewMs)
        || d.previewAtMs % replay.data.stepMs !== 0 || launchAt % BigInt(replay.data.stepMs) !== 0n
        || launchAt !== BigInt(e.hazard.launchedAtMs!) || d.timing.impactAtMs !== e.hazard.resolvedAtMs
        || BigInt(d.previewAtMs) < BigInt(Math.max(...e.defeats.map(x => x.atMs))) + BigInt(profile.minimumThreatClearToPreviewMs)
        || verified.collision.outcome !== e.hazard.outcome) return fail('artillery_source_conflict');
    }
    if (supplied.reinaBarrages !== undefined) {
      const casts = z.array(z.strictObject({ input: z.unknown(), result: z.unknown(), chargeTransition: z.unknown().optional() })).max(10000).safeParse(supplied.reinaBarrages);
      if (!casts.success || c.attemptGeneration === undefined) return fail('reina_barrage_invalid');
      const kit = await readOrdinaryKitFromOriginal(original);
      const ability = kit.packet.operators.find(o => o.operatorId === 'op_reina')?.abilities.find(a => a.id === 'op_reina.ultimate');
      if (kit.packet.version !== ordinaryKitV2Version || json(ability) !== json(reinaBarrageDefinition())) return fail('reina_barrage_source_conflict');
      const ids = new Set<string>();
      for (const cast of casts.data) {
        let replay: ReturnType<typeof replayReinaBarrage>;
        try { replay = replayReinaBarrage(cast.input); } catch { return fail('reina_barrage_invalid'); }
        if (replay.nodeId !== c.nodeId || replay.attemptGeneration !== c.attemptGeneration || ids.has(replay.castId)
          || BigInt(replay.clockTick) * BigInt(replay.stepTimeSpanTicks) > BigInt(e.checkpoint.atMs) * 10000n
          || replay.unresolvedImpacts !== 0 || json(replay) !== json(cast.result)) return fail('reina_barrage_source_conflict');
        // replayReinaBarrage has already validated every operation and its commit shape.
        const commit = (cast.input as { operations: { kind: string; charge?: number }[] }).operations.find(o => o.kind === 'COMMIT');
        try { await verifyReinaBarrageChargeTransition({ accountId: context.accountId, runId: context.runId },
          replay, commit?.charge, cast.chargeTransition); } catch { return fail('reina_barrage_charge_conflict'); }
        ids.add(replay.castId);
      }
    }
    const healthRow = await this.db.prepare('SELECT revision,initial_hash,start_snapshot_json,state_json,actor_state_json FROM combat_damage_states WHERE run_id=?')
      .bind(context.runId).first<{ revision: number; initial_hash: string; start_snapshot_json: string; state_json: string; actor_state_json: string | null }>();
    const health = await new CombatHealthStore(this.db).read(context.accountId, context.runId);
    const healthStatements: D1PreparedStatement[] = [];
    if (supplied.healthChanges !== undefined) {
      if (c.attemptGeneration === undefined || supplied.executionBinding === undefined) return fail('health_binding_required');
      const parsedChanges = z.array(z.strictObject({ eventId: id, targetId: id,
        reason: z.enum(['DAMAGE', 'HEAL', 'REPAIR', 'RECOVERY']), hp: z.number().int().nonnegative().max(2147483647) })).max(1000).safeParse(supplied.healthChanges);
      if (!parsedChanges.success || new Set(parsedChanges.data.map(x => x.eventId)).size !== parsedChanges.data.length) return fail('health_changes_invalid');
      if (parsedChanges.data.length) {
        const prepared = await new CombatHealthStore(this.db, { prepare: async () => ({ assertions: supplied.assertions }) })
          .prepareChanges(context.accountId, context.runId, parsedChanges.data.map((event, index) => ({
            eventId: event.eventId, targetId: event.targetId, expectedRevision: health.revision + index,
            attempt: { nodeId: c.nodeId, generation: c.attemptGeneration }, change: { kind: 'ACTUAL_HP', reason: event.reason, hp: event.hp },
          })));
        if (!prepared.statements.length) return fail('health_changes_already_committed');
        for (const receipt of prepared.receipts) {
          const target = health.state.targets.find(t => t.id === receipt.targetId);
          if (!target) return fail('health_source_conflict');
          target.health = receipt.after;
        }
        healthStatements.push(...prepared.statements);
      }
    }
    if (!healthRow || healthRow.revision !== health.revision || healthRow.start_snapshot_json !== context.startSnapshot
      || health.state.targets.length !== e.targets.length || e.targets.some(target => {
        const owned = health.state.targets.find(t => t.id === target.targetId)?.health;
        return !owned || owned.hp !== target.hp || owned.maxHp !== target.maxHp;
      })) return fail('health_source_conflict');

    return { proof: { kind: 'COMPLETE', verificationId: c.evidenceRef, holdIds: holds.data,
      ...(e.ultimateAuto === undefined ? {} : { ultimateAuto: e.ultimateAuto }),
      ...(e.actions === undefined ? {} : { actions: e.actions }),
      ...(e.attemptGeneration === undefined ? {} : { attemptGeneration: e.attemptGeneration }) }, assertions: [
      ...supplied.assertions,
      ...executionAssertions,
      this.db.prepare(`INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN EXISTS(
        SELECT 1 FROM combat_damage_states WHERE run_id=? AND revision=? AND initial_hash=? AND start_snapshot_json=?
          AND state_json=? AND actor_state_json IS ?) THEN 1 ELSE 0 END`)
        .bind(context.runId, healthRow.revision, healthRow.initial_hash, healthRow.start_snapshot_json,
          healthRow.state_json, healthRow.actor_state_json),
      this.db.prepare('INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM operation_map_receipts WHERE run_id=? AND event_id=? AND response_json=?) THEN 1 ELSE 0 END')
        .bind(context.runId, e.encounterId, arrival!.response_json),
      ...healthStatements,
    ] };
  }
}
