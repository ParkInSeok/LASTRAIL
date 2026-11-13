import { z } from 'zod';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import { ActivityError } from './activity-policy';
import { readOrdinaryKitFromOriginal } from './combat-ordinary-kit-source';
import type { readOrdinaryRunStatOrigin } from './combat-ordinary-run-source';

const nat = z.number().int().min(0).max(Number.MAX_SAFE_INTEGER);
const id = z.string().min(1).max(128);
const digest = z.string().regex(/^[a-f0-9]{64}$/u);
const amount = nat.max(2147483647);
const healthReceipt = z.strictObject({ eventId: id, targetId: id, tick: nat, hpAfter: amount,
  hpLoss: amount, absorbed: amount, hpGain: amount, derivedExcess: amount, shieldAdded: amount });
const actionSchema = z.object({ id, actor: id, ability: id, target: id,
  targets: z.array(id), due: nat, status: z.string(), healthReceipt: healthReceipt.optional() });
const rowSchema = z.strictObject({ id: digest, runId: id, profileSha256: digest,
  configurationSha256: digest, journalSequence: nat, globalSequence: nat,
  commandId: id, abilityId: id, effectIndex: z.literal(0), targetId: id,
  pulseIndex: z.literal(0), kind: z.enum(['HEAL', 'SHIELD_ISSUE']), tick: nat,
  sourceId: id, sourceKind: z.literal('SKILL'), actualResult: z.unknown(), nodeId: id.optional(),
  attemptGeneration: nat.min(1).optional(), executorId: id.optional(), healthReceipt: healthReceipt.optional() });
const heal = z.strictObject({ actualHpGain: amount });
const shield = z.strictObject({ previousRemaining: amount, resultingRemaining: amount,
  addedAmount: amount, previousExpiry: nat, expiry: nat,
  outcome: z.enum(['ADD', 'REFRESH', 'UNCHANGED']) });
const fail = (): never => { throw new ActivityError(409, 'ordinary_support_attribution_conflict'); };

/** Attribution only. Actions/configuration must come from the independently verified
 * executor; this does not prove HP amounts, target eligibility or journal completeness. */
export async function verifyOrdinarySupportAttribution(
  original: Awaited<ReturnType<typeof readOrdinaryRunStatOrigin>>,
  configurationHash: string, actionsInput: unknown, rowsInput: unknown,
  attempt?: { nodeId: string; generation: number },
) {
  const kit = await readOrdinaryKitFromOriginal(original);
  const parsed = z.object({ configurationHash: digest, actions: z.array(actionSchema),
    rows: z.array(rowSchema) }).safeParse({ configurationHash, actions: actionsInput, rows: rowsInput });
  if (!parsed.success) return fail();
  const { actions, rows } = parsed.data;
  if (new Set(actions.map(a => a.id)).size !== actions.length) return fail();
  const seen = new Set<number>();
  for (const [index, row] of rows.entries()) {
    const action = actions[row.globalSequence - 1];
    const actor = kit.packet.operators.find(o => o.operatorId === row.sourceId);
    const ability = actor?.abilities.find(a => a.id === row.abilityId);
    if (!action || !ability || row.runId !== original.origin.runId
      || row.profileSha256 !== configurationHash || row.configurationSha256 !== configurationHash
      || (attempt ? row.journalSequence <= (rows[index - 1]?.journalSequence ?? 0) : row.journalSequence !== index + 1) || seen.has(row.globalSequence)
      || action.id !== row.commandId || action.actor !== row.sourceId || action.ability !== row.abilityId
      || action.status !== 'APPLIED' || row.tick < action.due || action.target !== row.targetId
      || action.targets.length !== 1 || action.targets[0] !== row.targetId) return fail();
    seen.add(row.globalSequence);
    if (attempt) {
      const receipt = row.healthReceipt;
      if (row.nodeId !== attempt.nodeId || row.attemptGeneration !== attempt.generation
        || row.executorId !== row.sourceId || !receipt || !action.healthReceipt
        || canonicalJson(receipt) !== canonicalJson(action.healthReceipt)
        || receipt.eventId !== action.id || receipt.targetId !== row.targetId || receipt.tick !== row.tick
        || receipt.hpLoss !== 0 || receipt.absorbed !== 0) return fail();
    } else if (row.nodeId !== undefined || row.attemptGeneration !== undefined || row.executorId !== undefined
      || row.healthReceipt !== undefined || action.healthReceipt !== undefined) return fail();
    if (row.kind === 'HEAL') {
      const result = heal.safeParse(row.actualResult);
      if (ability.effect !== 'HEAL_ONE' || !kit.packet.operators.some(o => o.operatorId === row.targetId)
        || !result.success || (attempt && (row.healthReceipt!.hpGain !== result.data.actualHpGain
          || row.healthReceipt!.shieldAdded !== 0))) return fail();
    } else {
      const result = shield.safeParse(row.actualResult);
      if (ability.effect !== 'SHIELD_AND_TAUNT' || row.targetId !== row.sourceId || !result.success
        || result.data.addedAmount !== Math.max(0, result.data.resultingRemaining - result.data.previousRemaining)
        || (attempt && (row.healthReceipt!.hpGain !== 0 || row.healthReceipt!.derivedExcess !== 0
          || (row.healthReceipt!.shieldAdded !== 0 && row.healthReceipt!.shieldAdded !== result.data.resultingRemaining)))) return fail();
    }
    const expected = await sha256Hex(canonicalJson([row.runId, row.profileSha256, row.globalSequence,
      row.commandId, row.abilityId, row.effectIndex, row.targetId, row.pulseIndex, row.kind]));
    if (row.id !== expected) return fail();
  }
  return { scope: 'ORDINARY_SUPPORT_ATTRIBUTION_ONLY' as const, verifiedRows: rows.length };
}
