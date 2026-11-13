import { z } from "zod";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { id, nat, pos, sha } from "./ce03-local-schema";

export const railArtillerySchema = z.strictObject({ version: z.literal("rail-artillery-v1"), stepMs: z.literal(50),
  definition: z.strictObject({ id: z.literal("rail_artillery_emplacement_v1"), maxHp: pos.max(1000000), trainDamage: pos.max(1000000),
    periodMs: pos.max(60000), windupMs: pos.max(60000), projectileTravelMs: pos.max(60000) }),
  encounters: z.array(z.strictObject({ id, definitionId: z.literal("rail_artillery_emplacement_v1"), spawnWave: pos.max(15), exposureDistanceMm: pos.max(100000000) })).min(1).max(15),
  policy: z.strictObject({ requiredForWaveCompletion: z.literal(false), passedKillCount: z.literal(0), destroyedKillCount: z.literal(1), reward: z.literal("NONE"),
    onExit: z.literal("CANCEL_UNLAUNCHED_KEEP_IN_FLIGHT"), sameTickOrder: z.literal("DAMAGE_EXIT_THEN_LAUNCH_THEN_IMPACT"),
    onCombatTerminal: z.literal("STOP_LAUNCHES_DRAIN_IN_FLIGHT") })
}).superRefine((c, ctx) => {
  const d = c.definition;
  if ([d.periodMs, d.windupMs, d.projectileTravelMs].some(n => n % c.stepMs !== 0) || d.periodMs < d.windupMs)
    ctx.addIssue({ code: "custom", message: "whole fixed steps and period >= windup required" });
  if (new Set(c.encounters.map(e => e.id)).size !== c.encounters.length || c.encounters.some((e, i) => i > 0 && e.spawnWave <= c.encounters[i - 1]!.spawnWave))
    ctx.addIssue({ code: "custom", message: "ordered unique encounters required" });
});
export type RailArtilleryConfig = z.infer<typeof railArtillerySchema>;
export function createRailArtilleryConfig(baseSpeedMillimetersPerSecond: number): RailArtilleryConfig {
  const speed = pos.max(1000000).parse(baseSpeedMillimetersPerSecond);
  return railArtillerySchema.parse({ version: "rail-artillery-v1", stepMs: 50,
    definition: { id: "rail_artillery_emplacement_v1", maxHp: 1000, trainDamage: 50, periodMs: 2000, windupMs: 500, projectileTravelMs: 500 },
    encounters: [2, 5, 8, 11, 14].map(spawnWave => ({ id: `rail_artillery_wave_${spawnWave}`, definitionId: "rail_artillery_emplacement_v1", spawnWave, exposureDistanceMm: speed * 12 })),
    policy: { requiredForWaveCompletion: false, passedKillCount: 0, destroyedKillCount: 1, reward: "NONE",
      onExit: "CANCEL_UNLAUNCHED_KEEP_IN_FLIGHT", sameTickOrder: "DAMAGE_EXIT_THEN_LAUNCH_THEN_IMPACT", onCombatTerminal: "STOP_LAUNCHES_DRAIN_IN_FLIGHT" } });
}

const stepSchema = z.strictObject({ tick: nat, distanceMillimeters: nat, damageTaken: nat.max(100000000), terminalRequested: z.boolean().optional() });
const eventSchema = z.strictObject({ kind: z.enum(["SPAWN", "LAUNCH", "IMPACT", "DESTROYED", "PASSED", "TERMINAL_DRAIN"]), tick: nat, shotSequence: nat });
const artilleryStateSchema = z.strictObject({ version: z.literal("rail-artillery-state-v1"), configSha256: sha, configJson: z.string().max(20000), encounterId: id,
  startedAtTick: nat, originDistanceMm: nat, tick: nat, distanceMillimeters: nat, hp: nat, phase: z.enum(["ACTIVE", "DESTROYED", "PASSED"]),
  nextLaunchTick: nat, shotSequence: nat, pendingShots: z.array(z.strictObject({ sequence: pos, launchTick: nat, impactTick: nat, damage: pos })).max(1201),
  trainDamageApplied: nat, killCount: nat.max(1), launchesStopped: z.boolean(), events: z.array(eventSchema).max(30000) });
export type RailArtilleryState = z.infer<typeof artilleryStateSchema>;
const ensure = (ok: boolean, reason: string) => { if (!ok) throw new Error(reason); };

export async function createRailArtilleryState(configInput: unknown, encounterId: string, wave: number, tick: number, distanceMm: number): Promise<RailArtilleryState> {
  const c = railArtillerySchema.parse(configInput), encounter = c.encounters.find(e => e.id === encounterId);
  ensure(encounter !== undefined && encounter.spawnWave === wave, "rail_artillery_not_due");
  nat.parse(tick); nat.parse(distanceMm);
  const configJson = canonicalJson(c);
  return artilleryStateSchema.parse({ version: "rail-artillery-state-v1", configSha256: await sha256Hex(configJson), configJson, encounterId,
    startedAtTick: tick, originDistanceMm: distanceMm, tick, distanceMillimeters: distanceMm, hp: c.definition.maxHp, phase: "ACTIVE",
    nextLaunchTick: tick + c.definition.windupMs / c.stepMs, shotSequence: 0, pendingShots: [], trainDamageApplied: 0, killCount: 0,
    launchesStopped: false, events: [{ kind: "SPAWN", tick, shotSequence: 0 }] });
}

/** Pure reference step on an initialized/restored trusted state. Damage is the
 * already-resolved engine HP damage, not a client-authorized attack value. */
export function stepRailArtillery(configInput: unknown, stateInput: unknown, stepInput: unknown): RailArtilleryState {
  const c = railArtillerySchema.parse(configInput), previous = artilleryStateSchema.parse(stateInput), input = stepSchema.parse(stepInput);
  ensure(canonicalJson(c) === previous.configJson, "rail_artillery_config_changed");
  ensure(input.tick === previous.tick + 1 && input.distanceMillimeters >= previous.distanceMillimeters, "rail_artillery_step_invalid");
  const encounter = c.encounters.find(e => e.id === previous.encounterId);
  ensure(encounter !== undefined, "rail_artillery_encounter_invalid");
  const s = structuredClone(previous), emit = (kind: z.infer<typeof eventSchema>["kind"], shotSequence = 0) => s.events.push({ kind, tick: input.tick, shotSequence });
  if (input.terminalRequested === true && !s.launchesStopped) { s.launchesStopped = true; emit("TERMINAL_DRAIN"); }
  if (s.phase === "ACTIVE") {
    s.hp = Math.max(0, s.hp - input.damageTaken);
    if (s.hp === 0) { s.phase = "DESTROYED"; s.killCount = c.policy.destroyedKillCount; emit("DESTROYED"); }
    else if (input.distanceMillimeters - s.originDistanceMm >= encounter!.exposureDistanceMm) { s.phase = "PASSED"; emit("PASSED"); }
  }
  if (s.phase === "ACTIVE" && !s.launchesStopped && input.tick === s.nextLaunchTick) {
    const sequence = ++s.shotSequence;
    s.pendingShots.push({ sequence, launchTick: input.tick, impactTick: input.tick + c.definition.projectileTravelMs / c.stepMs, damage: c.definition.trainDamage });
    s.nextLaunchTick += c.definition.periodMs / c.stepMs; emit("LAUNCH", sequence);
  }
  // Delivered damage is resolved before existing train Vitals shielding/HP;
  // do not apply armor again or interpret this counter as actual HP loss.
  for (const shot of s.pendingShots) if (shot.impactTick === input.tick) { s.trainDamageApplied += shot.damage; emit("IMPACT", shot.sequence); }
  s.pendingShots = s.pendingShots.filter(shot => shot.impactTick > input.tick);
  s.tick = input.tick; s.distanceMillimeters = input.distanceMillimeters;
  return artilleryStateSchema.parse(s);
}

const artilleryReportSchema = z.strictObject({ version: z.literal("ce03-rail-artillery-report-v1"), owner: id, sessionId: id, configSha256: sha,
  encounterId: id, spawnWave: pos.max(15), startedAtTick: nat, originDistanceMm: nat, steps: z.array(stepSchema).max(10000), finalState: artilleryStateSchema });
const artilleryPinsSchema = z.strictObject({ owner: id, sessionId: id, configSha256: sha, encounterId: id, spawnWave: pos.max(15),
  startedAtTick: nat, originDistanceMm: nat, stepsSha256: sha, finalStateSha256: sha });

/** Trusted pins bind owner/session and independently recorded engine steps. No
 * live damage/rewards are performed; this is an offline deterministic replay. */
export async function verifyRailArtilleryReport(configInput: unknown, reportInput: unknown, trustedPins: unknown) {
  const c = railArtillerySchema.parse(configInput), r = artilleryReportSchema.parse(reportInput), pins = artilleryPinsSchema.parse(trustedPins);
  const identityKeys = ["owner", "sessionId", "configSha256", "encounterId", "spawnWave", "startedAtTick", "originDistanceMm"] as const;
  ensure(identityKeys.every(k => r[k] === pins[k]), "rail_artillery_report_identity");
  let replay = await createRailArtilleryState(c, r.encounterId, r.spawnWave, r.startedAtTick, r.originDistanceMm);
  const jsonSteps = r.steps.map(({ terminalRequested, ...step }) => terminalRequested === undefined ? step : { ...step, terminalRequested });
  ensure(replay.configSha256 === pins.configSha256 && await sha256Hex(canonicalJson(jsonSteps)) === pins.stepsSha256, "rail_artillery_report_pin");
  for (const step of r.steps) replay = stepRailArtillery(c, replay, step);
  ensure(canonicalJson(replay) === canonicalJson(r.finalState) && await sha256Hex(canonicalJson(replay)) === pins.finalStateSha256, "rail_artillery_report_replay");
  return { status: "ARTILLERY_REPLAY_VERIFIED" as const, ready: false as const, reward: "NONE" as const,
    phase: replay.phase, killCount: replay.killCount, trainDamageApplied: replay.trainDamageApplied, pendingImpacts: replay.pendingShots.length };
}
