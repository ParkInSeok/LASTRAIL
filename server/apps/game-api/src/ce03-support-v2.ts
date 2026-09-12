import { z } from "zod";
import { id, nat, pos, templateSchema } from "./ce03-local-schema";

// Isolated development contract only. No r6 producer, admission or runtime registration.
export const supportVersion = "ce03-support-local-v2.20260912.001";
const roles = ["suppressive_barrage", "medical_support", "smoke", "weakness_scan", "firepower_boost", "electronic_disruption", "emergency_protection"] as const;
const roleSchema = z.enum(roles);
const definitions = [
  { role: roles[0], windupMs: 500, cooldownMs: 28000, effect: "TRAIN_ATK_DAMAGE_AND_STUN", numerator: 6, denominator: 5, durationMs: 500 },
  { role: roles[1], windupMs: 350, cooldownMs: 30000, effect: "TARGET_MAX_HP_HEAL", numerator: 3, denominator: 20, durationMs: 0 },
  { role: roles[2], windupMs: 300, cooldownMs: 30000, effect: "OPERATOR_ORDINARY_DAMAGE_REDUCTION", valueBps: 2000, durationMs: 6000 },
  { role: roles[3], windupMs: 300, cooldownMs: 24000, effect: "MARK", valueBps: 1500, durationMs: 6000 },
  { role: roles[4], windupMs: 300, cooldownMs: 30000, effect: "TRAIN_BASIC_AND_CARRIAGE_DAMAGE_BUFF", valueBps: 1500, durationMs: 6000 },
  { role: roles[5], windupMs: 200, cooldownMs: 24000, effect: "PINNED_ACTION_INTERRUPT", durationMs: 0 },
  { role: roles[6], windupMs: 250, cooldownMs: 28000, effect: "TARGET_MAX_HP_SHIELD", numerator: 1, denominator: 4, durationMs: 5000 },
].map(d => Object.freeze({ ...d, id: `ce03.role.${d.role}`, firstReadyMs: 0, recoveryMs: 250, kind: "TRAIN_MANUAL" as const }));
export const supportDefinitions = Object.freeze(definitions);
export const supportPolicies = Object.freeze({ version: supportVersion, scope: "LOCAL_DEVELOPMENT_ONLY",
  cooldownAt: "CAST_START", cooldowns: "INDEPENDENT", channel: "SHARED_SUPPORT_CAST_AND_RECOVERY",
  rejectedBeforeStart: "NO_COST", fizzleAfterStart: "NO_REFUND", retarget: "NEVER",
  crit: false, dodge: false, leaderCharge: 0, duplicateCommand: "NO_EFFECT",
  smokeStage: "AFTER_OUTGOING_MARK_ARMOR_BEFORE_SHIELD_ONCE", smokeCapBps: 3000,
  smokeStacking: "STRONGEST_WITH_SOURCE_EXPIRY", markCapBps: 2000, outgoingCapBps: 3000,
  shieldCapMaxHpBps: 4000, shieldStacking: "SOURCE_ABILITY_EFFECT_INDEX_IGNORE_WEAKER_REFRESH_EQUAL_OR_STRONGER",
  interruption: "PINNED_CURRENT_ACTION_UNCOMMITTED_FUTURE_PULSES_ONLY",
});

// Legacy rows must originate from the closed old ability schema; selected rows are copied.
export function resolveSupportThree(selection: unknown, legacyInput: unknown) {
  const keys = z.array(id).length(3).refine(a => new Set(a).size === 3).parse(selection);
  const legacy = templateSchema.shape.train.shape.manual.element.array().max(3).parse(legacyInput);
  if (new Set(legacy.map(x => x.id)).size !== legacy.length || legacy.some(x => !["support_fire", "barrier", "emergency_repair"].includes(x.id) || x.kind !== "TRAIN_MANUAL")) throw new Error("invalid legacy support");
  return keys.map(key => {
    const value = supportDefinitions.find(d => d.id === key) ?? legacy.find(d => d.id === key);
    if (!value) throw new Error("unknown or missing support definition");
    return structuredClone(value);
  });
}

export function targetMaxHpAmount(maxHpInput: unknown, hpInput: unknown, kindInput: unknown): number {
  const maxHp = pos.parse(maxHpInput), hp = nat.max(maxHp).parse(hpInput);
  const kind = z.enum(["HEAL", "SHIELD"]).parse(kindInput);
  const amount = Number(BigInt(maxHp) * (kind === "HEAL" ? 3n : 1n) / (kind === "HEAL" ? 20n : 4n));
  return kind === "HEAL" ? Math.min(amount, maxHp - hp) : amount;
}
const actorSchema = z.strictObject({ entityId: id, hp: nat, maxHp: pos, slot: nat, spawnOrder: nat,
  side: z.enum(["OPERATOR", "ENEMY", "TRAIN"]), lane: z.enum(["INTERIOR", "EXTERIOR"]), returning: z.boolean(),
  action: z.strictObject({ id, kind: z.enum(["BASIC", "SKILL", "CHANNEL"]), interruptible: z.boolean(), immune: z.boolean() }).nullable(),
}).refine(a => a.hp <= a.maxHp);
const actorsSchema = z.array(actorSchema).max(4096).refine(a => new Set(a.map(x => x.entityId)).size === a.length);
export type SupportTargetPin = { entityId: string; actionId: string | null };
const ordinal = (a: string, b: string) => a < b ? -1 : a > b ? 1 : 0;

// Snapshot at cast acceptance. Commit must validate these pins, never rerun selection.
export function pinSupportTargets(roleInput: unknown, actorsInput: unknown, focusInput: unknown, explicitInput: unknown): SupportTargetPin[] {
  const role = roleSchema.parse(roleInput), focus = id.nullable().parse(focusInput), explicit = id.nullable().parse(explicitInput);
  const actors = actorsSchema.parse(actorsInput);
  const alive = actors.filter(a => a.hp > 0 && !a.returning);
  let selected = alive.filter(a => a.side === "OPERATOR");
  if (role === "suppressive_barrage") selected = alive.filter(a => a.side === "ENEMY" && a.lane === "INTERIOR")
    .sort((a, b) => Number(b.entityId === focus) - Number(a.entityId === focus) || a.spawnOrder - b.spawnOrder || ordinal(a.entityId, b.entityId)).slice(0, 3);
  else if (role === "weakness_scan" || role === "electronic_disruption") {
    selected = alive.filter(a => a.side === "ENEMY" && a.entityId === explicit);
    if (role === "electronic_disruption") selected = selected.filter(a => a.action && a.action.kind !== "BASIC" && a.action.interruptible && !a.action.immune);
  } else if (role === "emergency_protection") selected = selected.sort((a, b) => {
    const difference = BigInt(a.hp) * BigInt(b.maxHp) - BigInt(b.hp) * BigInt(a.maxHp);
    return (difference < 0n ? -1 : difference > 0n ? 1 : 0) || a.slot - b.slot || ordinal(a.entityId, b.entityId);
  }).slice(0, 1);
  else if (role === "firepower_boost") selected = alive.filter(a => a.side === "TRAIN");
  if (!selected.length || (role === "medical_support" && selected.every(a => a.hp === a.maxHp))) throw new Error("support cast has no valid target");
  return selected.map(a => ({ entityId: a.entityId, actionId: role === "electronic_disruption" ? a.action!.id : null }));
}

// Internal trusted checkpoint pins; this function does not authorize caller-supplied pins.
export function validSupportPins(pinsInput: unknown, actorsInput: unknown): SupportTargetPin[] {
  const pins = z.array(z.strictObject({ entityId: id, actionId: id.nullable() })).max(4096)
    .refine(p => new Set(p.map(x => x.entityId)).size === p.length).parse(pinsInput);
  const actors = new Map(actorsSchema.parse(actorsInput).map(a => [a.entityId, a]));
  return pins.filter(pin => {
    const a = actors.get(pin.entityId);
    if (!a || a.hp === 0 || a.returning) return false;
    return pin.actionId === null || (a.side === "ENEMY" && a.action?.id === pin.actionId && a.action.kind !== "BASIC" && a.action.interruptible && !a.action.immune);
  });
}
export function firepowerApplies(sourceInput: unknown): boolean {
  const source = z.enum(["TRAIN_BASIC", "COMBAT_DEVICE", "GUN_DEVICE", "OPERATOR", "SUPPORT", "HEAL", "REPAIR", "MECHANIC_FIXED"]).parse(sourceInput);
  return source === "TRAIN_BASIC" || source === "COMBAT_DEVICE" || source === "GUN_DEVICE";
}
