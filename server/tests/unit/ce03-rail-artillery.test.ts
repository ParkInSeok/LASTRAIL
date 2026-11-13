import { describe, expect, it } from "vitest";
import { writeFileSync } from "node:fs";
import { join } from "node:path";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import * as artillery from "../../apps/game-api/src/ce03-rail-artillery";

describe("rail artillery deterministic encounters", () => {
  it("stops launches at terminal candidacy while draining existing shots", async () => {
    const c = artillery.createRailArtilleryConfig(10000);
    let s = await artillery.createRailArtilleryState(c, c.encounters[0]!.id, 2, 0, 0);
    for (let tick = 1; tick <= 60; tick++) s = artillery.stepRailArtillery(c, s, {
      tick, distanceMillimeters: tick * 500, damageTaken: 0, terminalRequested: tick === 11
    });
    expect(s).toMatchObject({ phase: "ACTIVE", launchesStopped: true, trainDamageApplied: 50, killCount: 0 });
    expect(s.pendingShots).toHaveLength(0);
    expect(s.events.map(e => e.kind)).toEqual(["SPAWN", "LAUNCH", "TERMINAL_DRAIN", "IMPACT"]);
  });
  it("launches after windup, applies in-flight impact after passing, and never awards a passed kill", async () => {
    const c = artillery.createRailArtilleryConfig(10000);
    let s = await artillery.createRailArtilleryState(c, c.encounters[0]!.id, 2, 0, 0);
    for (let tick = 1; tick <= 10; tick++) s = artillery.stepRailArtillery(c, s, { tick, distanceMillimeters: tick * 500, damageTaken: 0 });
    expect(s.pendingShots).toHaveLength(1);
    expect(s.trainDamageApplied).toBe(0);
    s = artillery.stepRailArtillery(c, s, { tick: 11, distanceMillimeters: 120000, damageTaken: 0 });
    expect(s).toMatchObject({ phase: "PASSED", killCount: 0 });
    for (let tick = 12; tick <= 20; tick++) s = artillery.stepRailArtillery(c, s, { tick, distanceMillimeters: 120000, damageTaken: 0 });
    expect(s.trainDamageApplied).toBe(50);
    expect(s.pendingShots).toHaveLength(0);
    expect(s.events.map(e => e.kind)).toEqual(["SPAWN", "LAUNCH", "PASSED", "IMPACT"]);
  });
  it("cancels an unlaunched shot when destroyed at the launch tick, and rejects distance/time reversal", async () => {
    const c = artillery.createRailArtilleryConfig(10000);
    let s = await artillery.createRailArtilleryState(c, c.encounters[0]!.id, 2, 0, 0);
    for (let tick = 1; tick < 10; tick++) s = artillery.stepRailArtillery(c, s, { tick, distanceMillimeters: tick * 500, damageTaken: 0 });
    const before = canonicalJson(s);
    expect(() => artillery.stepRailArtillery(c, s, { tick: 9, distanceMillimeters: 4500, damageTaken: 0 })).toThrow();
    expect(() => artillery.stepRailArtillery(c, s, { tick: 10, distanceMillimeters: 0, damageTaken: 0 })).toThrow();
    expect(canonicalJson(s)).toBe(before);
    s = artillery.stepRailArtillery(c, s, { tick: 10, distanceMillimeters: 5000, damageTaken: 1000 });
    expect(s).toMatchObject({ phase: "DESTROYED", killCount: 1, trainDamageApplied: 0, hp: 0 });
    expect(s.pendingShots).toHaveLength(0);
    await expect(artillery.createRailArtilleryState(c, c.encounters[0]!.id, 3, 0, 0)).rejects.toThrow();
  });
  it("replays and pins encounter reports, preserving a fired shot after destruction", async () => {
    const c = artillery.createRailArtilleryConfig(10000), encounterId = c.encounters[0]!.id;
    let s = await artillery.createRailArtilleryState(c, encounterId, 2, 0, 0);
    const steps = Array.from({ length: 20 }, (_, i) => ({ tick: i + 1, distanceMillimeters: (i + 1) * 500, damageTaken: i === 10 ? 1000 : 0 }));
    for (const step of steps) s = artillery.stepRailArtillery(c, s, step);
    expect(s.trainDamageApplied).toBe(50); expect(s.phase).toBe("DESTROYED");
    const report = { version: "ce03-rail-artillery-report-v1", owner: "a", sessionId: "s", configSha256: s.configSha256,
      encounterId, spawnWave: 2, startedAtTick: 0, originDistanceMm: 0, steps, finalState: s };
    const pins = { owner: "a", sessionId: "s", configSha256: s.configSha256, encounterId, spawnWave: 2, startedAtTick: 0, originDistanceMm: 0,
      stepsSha256: await sha256Hex(canonicalJson(steps)), finalStateSha256: await sha256Hex(canonicalJson(s)) };
    expect(await artillery.verifyRailArtilleryReport(c, report, pins)).toMatchObject({ status: "ARTILLERY_REPLAY_VERIFIED", ready: false, reward: "NONE" });
    if (process.env.LASTRAIL_RAIL_EVIDENCE_DIR) writeFileSync(join(process.env.LASTRAIL_RAIL_EVIDENCE_DIR, "synthetic-artillery-bundle.json"),
      JSON.stringify({ provenance: "SERVER_TEST_SYNTHETIC_NOT_UNITY_EXECUTION", config: c, report, trustedPins: pins }, null, 2) + "\n");
    await expect(artillery.verifyRailArtilleryReport(c, { ...report, finalState: { ...s, killCount: 0 } }, pins)).rejects.toThrow();
    await expect(artillery.verifyRailArtilleryReport(c, { ...report, owner: "b" }, pins)).rejects.toThrow();
  });
});
