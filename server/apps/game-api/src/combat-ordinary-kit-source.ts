import { z } from 'zod';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import { ActivityError } from './activity-policy';
import { captureCombatAuthority, readCombatAuthority } from './combat-authority';
import { bindCombatPolicyRequirements } from './combat-policy-binding';
import { readOrdinaryFiveStatOrigin, ordinaryNyxStatVersion } from './combat-ordinary-stat-owner';
import { readOrdinaryRunStatOrigin } from './combat-ordinary-run-source';
import { reinaBarrageDefinition } from './reina-barrage';
import { approvedKyleTauntV2 } from './combat-shield-budget';

export const ordinaryKitVersion = 'combat.ordinary-five-kit.v1.20261023';
export const ordinaryKitV2Version = 'combat.ordinary-five-kit.v2.20261102';
export const ordinaryKitV3Version = 'combat.ordinary-five-kit.v3.20261107';
const json = (v: unknown) => canonicalJson(z.json().parse(v));
const hash = (v: unknown) => sha256Hex(json(v));
const fail = (reason: string): never => { throw new ActivityError(409, `ordinary_kit_${reason}`); };

/** START-owned input production, not runtime capability. Uses the existing kit
 * definitions and the pinned five-actor origin; no current lobby lookup, new
 * balance, or guessed basic-attack range/windup. Core values and skillMagnitude
 * stay in ordinaryStatSource and are referenced by originHash.
 */
export async function captureOrdinaryKitSource(originInput: unknown, originHash: string, admissionInput: unknown) {
  return captureKit(originInput, originHash, admissionInput, ordinaryKitVersion);
}

async function captureKit(originInput: unknown, originHash: string, admissionInput: unknown, version: string, currentKyle = true) {
  const origin = await readOrdinaryFiveStatOrigin(originInput, originHash);
  if ((origin.version === ordinaryNyxStatVersion) !== (version === ordinaryKitV3Version)) return fail('version_scope_conflict');
  const admission = z.object({ version: z.literal('departure-v4'), combatAuthority: z.unknown(),
    combatPolicyRequirements: z.unknown(), formation: z.object({ operators: z.array(z.object({ operatorId: z.string(), slot: z.number() })) }) })
    .parse(admissionInput);
  const authority = readCombatAuthority(admission.combatAuthority);
  // This START path is ordinary solo. Cooperative host assignment needs its own
  // original contract; it must not be inferred from a client-provided mode flag.
  if (authority.status !== 'PINNED' || authority.contract?.mode !== 'ORDINARY_SOLO'
    || json(admission.combatPolicyRequirements) !== json(origin.policy)
    || json([...admission.formation.operators].sort((a, b) => a.slot - b.slot))
      !== json(origin.actors.map(a => ({ operatorId: a.operatorId, slot: a.slot })).sort((a, b) => a.slot - b.slot)))
    return fail('origin_conflict');
  const binding = bindCombatPolicyRequirements(admission, 'I');
  if (binding.status !== 'EXECUTION_UNAVAILABLE' || binding.operators.length !== 5
    || binding.operators.some(o => o.abilities.length !== 2)) return fail('definition_unavailable');
  if (currentKyle) for (const operator of binding.operators)
    operator.abilities = operator.abilities.map(a => a.id === approvedKyleTauntV2.id ? { ...approvedKyleTauntV2 } : a);
  const packet = { version, accountId: origin.accountId, runId: origin.runId, originHash,
    policyVersion: binding.policyVersion!, policyHash: await hash(origin.policy),
    authority: captureCombatAuthority('ORDINARY_SOLO'), execution: 'REQUIRES_MATCHING_UNITY_HANDLERS' as const,
    operators: binding.operators, requiredEffectKinds: binding.requiredEffectKinds };
  return { packet, kitHash: await hash(packet) };
}

export async function captureOrdinaryKitSourceV2(originInput: unknown, originHash: string, admissionInput: unknown) {
  const previous = await captureOrdinaryKitSource(originInput, originHash, admissionInput);
  return withReinaBarrage(previous, ordinaryKitV2Version);
}

export async function captureOrdinaryKitSourceV3(originInput: unknown, originHash: string, admissionInput: unknown) {
  return withReinaBarrage(await captureKit(originInput, originHash, admissionInput, ordinaryKitV3Version), ordinaryKitV3Version);
}

async function withReinaBarrage(previous: Awaited<ReturnType<typeof captureKit>>, version: string) {
  const operators: typeof previous.packet.operators = previous.packet.operators.map(o => ({ ...o,
    abilities: o.abilities.map(a => a.id === 'op_reina.ultimate' ? reinaBarrageDefinition() : a) }));
  const packet = { ...previous.packet, version, operators,
    requiredEffectKinds: [...new Set(operators.flatMap(o => o.abilities.map(a => a.effect)))].sort() };
  return { packet, kitHash: await hash(packet) };
}

/** Read exactly the stored START input, never manufacture a kit for a historical
 * run that lacks it. Retained v1 and v2 are reconstructed separately. */
export async function readOrdinaryRunKitSource(db: D1Database, accountId: string, runId: string) {
  const original = await readOrdinaryRunStatOrigin(db, accountId, runId);
  return readOrdinaryKitFromOriginal(original);
}

/** Reuses the caller's validated original so initialization fences the same snapshot. */
export async function readOrdinaryKitFromOriginal(original: Awaited<ReturnType<typeof readOrdinaryRunStatOrigin>>) {
  const snapshot = JSON.parse(original.startSnapshotJson) as { admission?: unknown; ordinaryKitSource?: unknown };
  if (snapshot.ordinaryKitSource === undefined) return fail('source_unavailable');
  try {
    const packet = z.object({ packet: z.object({ version: z.enum([ordinaryKitVersion, ordinaryKitV2Version, ordinaryKitV3Version]),
      operators: z.array(z.object({ abilities: z.array(z.object({ id: z.string(), tauntPolicyVersion: z.string().optional() })) })) }) })
      .parse(snapshot.ordinaryKitSource).packet;
    const kyleRevision = packet.operators.flatMap(o => o.abilities).find(a => a.id === approvedKyleTauntV2.id)?.tauntPolicyVersion;
    if (kyleRevision !== undefined && kyleRevision !== approvedKyleTauntV2.tauntPolicyVersion) return fail('source_invalid');
    const base = await captureKit(original.origin, original.originHash, snapshot.admission, packet.version, kyleRevision !== undefined);
    const expected = packet.version === ordinaryKitVersion ? base : await withReinaBarrage(base, packet.version);
    if (json(snapshot.ordinaryKitSource) !== json(expected)) return fail('source_invalid');
    return expected;
  } catch { return fail('source_invalid'); }
}
