import {z} from 'zod';
import {ActivityError} from './activity-policy';
import {readPinnedBNamedOperatorSource,type BNamedOperatorSource} from './combat-b-named-operator-source';

export const basicSourceProjectionVersion = 'basic-source-projection-v1.20261015';
const requestSchema = z.strictObject({
  accountId: z.string().min(1).max(200),
  operatorIds: z.array(z.enum(['op_kyle','op_sable'])).min(1).max(2)
    .refine(ids => new Set(ids).size === ids.length),
});
type Requirement = {code: string; owner: 'DEFINITION_PRODUCER' | 'EFFECTIVE_STAT_OWNER' | 'GEOMETRY_OWNER' | 'MODE_COMBAT_OWNER'};
// These describe missing inputs, never fabricated values or formal ability identities.
const staticRequirements: readonly Requirement[] = [
  {code:'FORMAL_BASIC_ABILITY_ID',owner:'DEFINITION_PRODUCER'},
  {code:'BASIC_ATTACK_FORM',owner:'DEFINITION_PRODUCER'},
  {code:'EFFECTIVE_ATTACK_STATS',owner:'EFFECTIVE_STAT_OWNER'},
  {code:'PROJECTILE_RANGE_MM',owner:'DEFINITION_PRODUCER'},
  {code:'PROJECTILE_GEOMETRY_AND_L',owner:'GEOMETRY_OWNER'},
  {code:'MUZZLE_ANCHOR',owner:'GEOMETRY_OWNER'},
  {code:'PROJECTILE_SPEED_AND_WIDTH',owner:'DEFINITION_PRODUCER'},
];
const dynamicRequirements: readonly Requirement[] = [
  {code:'ORIGINAL_OPPORTUNITY_ID',owner:'MODE_COMBAT_OWNER'},
  {code:'FINAL_ATTACK_BUDGET_D',owner:'MODE_COMBAT_OWNER'},
  {code:'ORIGINAL_CRITICAL_SNAPSHOT',owner:'MODE_COMBAT_OWNER'},
  {code:'COMMITTED_BODY_TARGET_SET',owner:'MODE_COMBAT_OWNER'},
  {code:'FIRE_GEOMETRY_OBSERVATION',owner:'MODE_COMBAT_OWNER'},
  {code:'PROJECTILE_LIFECYCLE_STATE',owner:'MODE_COMBAT_OWNER'},
];

/** Internal projection from an independently trusted capture/hash, NOT HTTP authentication,
 * current ownership admission, a new catalog or projectile permission. Never accept a caller's
 * self-rehashed packet as the trusted original. Restore keeps original values without DB rereads.
 * The existing strict pinned reader owns all packet/hash validation; do not duplicate it here. */
export async function projectPinnedBasicSource(input: unknown, original: BNamedOperatorSource,
  expectedOriginalHash: unknown, rawRequest: unknown) {
  const parsed = requestSchema.safeParse(rawRequest);
  if (!parsed.success) throw new ActivityError(400,'combat_b_basic_projection_request_invalid');
  const request = parsed.data;
  const packet = await readPinnedBNamedOperatorSource(input,original,expectedOriginalHash);
  if (packet.accountId !== request.accountId) throw new ActivityError(403,'combat_b_basic_projection_account_mismatch');
  if (request.operatorIds.some(id => !packet.actors.some(actor => actor.operatorId === id))) {
    throw new ActivityError(403,'combat_b_basic_projection_operator_unavailable');
  }
  // The pinned reader returned a detached snapshot. Preserve original units, fractions and
  // capability flags. Do not promote permanent stats or null distance to effective/infinite.
  return {
    version: basicSourceProjectionVersion,
    source: {
      version: packet.version, accountId: packet.accountId, packetHash: packet.packetHash,
      definitionHash: packet.definitionHash, ownedProfileHash: packet.ownedProfileHash,
      ownedSourceHash: packet.ownedSourceHash, formationRevision: packet.formationRevision,
      loadoutRevision: packet.loadoutRevision, refit: packet.refit, sourceStatus: packet.sourceStatus,
      runtimeSupported: packet.runtimeSupported, canStart: packet.canStart,
    },
    actors: packet.actors.filter(actor => request.operatorIds.includes(actor.operatorId)).map(actor => ({
      actorId: actor.actorId, operatorId: actor.operatorId, profileVersion: actor.profileVersion,
      definitionVersion: actor.definitionVersion, definitionHash: actor.definitionHash,
      baseDefinition: actor.baseDefinition, owned: actor.owned,
      basic: actor.basic, permanentStats: actor.permanentStats, effectiveStats: actor.effectiveStats,
      equipment: actor.equipment, placement: actor.placement, bcb: actor.bcb,
      basicCapability: actor.capabilities.basic, sourceMissing: actor.missing,
      requirements: {static: structuredClone(staticRequirements), dynamic: structuredClone(dynamicRequirements)},
      // A readiness diagnostic only; it neither authorizes nor blocks existing LOCAL BASIC.
      projectileReady: false as const,
    })),
  };
}
export type BasicSourceProjection = Awaited<ReturnType<typeof projectPinnedBasicSource>>;