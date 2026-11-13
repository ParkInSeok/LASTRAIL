import { z } from "zod";
import { canonicalJson } from "@lastrail/common";
import { id, nat, pos, sha } from "./ce03-local-schema";
import { pinSupportTargets, supportDefinitions, supportVersion } from "./ce03-support-v2";

const skill = id.refine(v => supportDefinitions.some(d => d.id === v));
const selection = z.array(skill).length(3).refine(a => new Set(a).size === 3);
const tick = nat.max(1_000_000_000);
const pins = z.array(z.strictObject({ entityId: id, actionId: id.nullable() })).min(1).max(4096)
  .refine(p => new Set(p.map(x => x.entityId)).size === p.length);
export const supportCommandSchema = z.strictObject({ version: z.literal("ce03-support-command-v2"), key: id, skillId: skill, targetId: id.nullable() });
const receiptSchema = z.strictObject({ command: supportCommandSchema, sequence: pos, startTick: tick, pins });
const pendingSchema = z.strictObject({ commandKey: id, skillId: skill, sequence: pos, dueTick: tick, releaseTick: tick, pins });
const stateSchema = z.strictObject({ version: z.literal("ce03-support-checkpoint-v2"), policyVersion: z.literal(supportVersion),
  profileSha256: sha, selected: selection, tick, sequence: nat,
  receipts: z.array(receiptSchema).max(10000), cooldowns: z.array(z.strictObject({ skillId: skill, readyTick: tick })).length(3),
  pending: pendingSchema.nullable(), outbox: z.array(pendingSchema).max(10000),
});
export type SupportState = z.infer<typeof stateSchema>;
const definition = (key: string) => supportDefinitions.find(d => d.id === key)!;
function materialize(profileSha256: string, selected: string[], now: number, receipts: SupportState["receipts"]): SupportState {
  const cooldowns = selected.map(skillId => ({ skillId, readyTick: 0 }));
  const outbox: SupportState["outbox"] = [];
  let pending: SupportState["pending"] = null, lastRelease = 0;
  const keys = new Set<string>();
  for (const [index, r] of receipts.entries()) {
    const d = definition(r.command.skillId), cd = cooldowns.find(c => c.skillId === r.command.skillId);
    if (!cd || keys.has(r.command.key) || r.sequence !== index + 1 || r.startTick > now || r.startTick < lastRelease || r.startTick < cd.readyTick) throw new Error("invalid support receipt history");
    const explicit = d.role === "weakness_scan" || d.role === "electronic_disruption";
    if ((explicit && (r.command.targetId === null || r.pins.length !== 1 || r.pins[0]!.entityId !== r.command.targetId)) || (!explicit && r.command.targetId !== null)) throw new Error("invalid support target contract");
    if (r.pins.some(p => (p.actionId !== null) !== (d.role === "electronic_disruption")) || (d.role === "suppressive_barrage" && r.pins.length > 3) || (["emergency_protection", "firepower_boost"].includes(d.role) && r.pins.length !== 1)) throw new Error("invalid support pins");
    keys.add(r.command.key);
    cd.readyTick = r.startTick + d.cooldownMs / 50;
    const intent = { commandKey: r.command.key, skillId: r.command.skillId, sequence: r.sequence,
      dueTick: r.startTick + d.windupMs / 50, releaseTick: r.startTick + (d.windupMs + d.recoveryMs) / 50, pins: r.pins };
    lastRelease = intent.releaseTick;
    if (intent.dueTick <= now) outbox.push(intent);
    if (now < intent.releaseTick) pending = intent;
  }
  return stateSchema.parse({ version: "ce03-support-checkpoint-v2", policyVersion: supportVersion, profileSha256, selected, tick: now,
    sequence: receipts.length, receipts, cooldowns, pending, outbox });
}
export function createSupportState(profileInput: unknown, selectedInput: unknown): SupportState {
  return materialize(sha.parse(profileInput), selection.parse(selectedInput), 0, []);
}
// Restore only from the trusted battle checkpoint, with expected profile/selection from its binding.
export function restoreSupportState(input: unknown, expectedProfile: unknown, expectedSelected: unknown): SupportState {
  const s = stateSchema.parse(input);
  if (s.profileSha256 !== sha.parse(expectedProfile) || canonicalJson(s.selected) !== canonicalJson(selection.parse(expectedSelected))) throw new Error("support checkpoint binding mismatch");
  const derived = materialize(s.profileSha256, s.selected, s.tick, s.receipts);
  if (canonicalJson(s) !== canonicalJson(derived)) throw new Error("support checkpoint derived state mismatch");
  return derived;
}
function checked(input: SupportState) { return restoreSupportState(input, input.profileSha256, input.selected); }
export function acceptSupportCommand(input: SupportState, commandInput: unknown, actors: unknown, focus: unknown): SupportState {
  const s = checked(input), command = supportCommandSchema.parse(commandInput);
  const prior = s.receipts.find(r => r.command.key === command.key);
  if (prior) {
    if (canonicalJson(prior.command) !== canonicalJson(command)) throw new Error("support command key conflict");
    return s;
  }
  const cd = s.cooldowns.find(c => c.skillId === command.skillId);
  if (!cd || s.pending || s.tick < cd.readyTick || s.receipts.length === 10000) throw new Error("support command unavailable");
  const d = definition(command.skillId);
  const pinned = pinSupportTargets(d.role, actors, focus, command.targetId);
  return materialize(s.profileSha256, s.selected, s.tick, [...s.receipts, { command, sequence: s.sequence + 1, startTick: s.tick, pins: pinned }]);
}
// Exactly one 50 ms simulation step, no wall-clock catch-up. Outbox is durable intent history.
// Effects must be applied atomically with the consuming battle checkpoint's applied-intent cursor.
export function stepSupportState(input: SupportState): SupportState {
  const s = checked(input);
  return materialize(s.profileSha256, s.selected, tick.parse(s.tick + 1), s.receipts);
}
