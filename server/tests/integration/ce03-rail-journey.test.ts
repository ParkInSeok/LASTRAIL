import { describe, expect, it } from "vitest";
import { writeFileSync, readFileSync } from "node:fs";
import { join } from "node:path";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { createCe03LocalHost } from "../../apps/game-api/src/ce03-local-host";
import { ce03ProducerFixture, ce03Inputs } from "../helpers/ce03-producer-fixture";
import { body } from "../helpers/departure-fixture";
import { ce03SupportSchemaSql } from "../../apps/game-api/src/ce03-support-loadout";
import { localSupportCatalog } from "../../apps/game-api/src/ce03-command-contract";
import { resolveAuthContext } from "../../apps/game-api/src/index";
import * as rail from "../../apps/game-api/src/ce03-rail-journey";
import {projectRailPresentation} from "../../scripts/local/ce03-rail-presentation";
import type {RailPresentation} from "../../apps/game-api/src/ce03-rail-presentation";

function evidence(name: string, value: unknown) {
  if (process.env.LASTRAIL_RAIL_EVIDENCE_DIR) writeFileSync(join(process.env.LASTRAIL_RAIL_EVIDENCE_DIR, name), JSON.stringify(value, null, 2) + "\n");
}

async function hostFixture(enabled: boolean, railPresentation?: RailPresentation) {
  const f = await ce03ProducerFixture(); f.sql.run(ce03SupportSchemaSql);
  const options = { disposableDatabase: f.env.DB, allowedAccountIds: ["a"], authenticate: resolveAuthContext,
    inputs: ce03Inputs(), supportCatalog: localSupportCatalog, targetingPolicyId: "ce03.frontline-first-v1" as const,
    ultimatePolicyId: "ce03.per-operator-ultimate-v1" as const, activePolicyId: "ce03.general-active-auto-v1" as const,
    ...(enabled ? { railJourney: { version: "rail-journey-v1" as const, baseSpeedMillimetersPerSecond: 10000 } } : {}), ...(railPresentation ? {railPresentation} : {}) };
  const host = createCe03LocalHost(options);
  const post = (path: string, payload: unknown) => host.request(`http://127.0.0.1${path}`, {
    method: "POST", headers: { ...f.headers, "Idempotency-Key": "rail-fixture" }, body: JSON.stringify(payload)
  }, f.env);
  await post("/__local/ce03/support-loadout", { rulesVersion: localSupportCatalog.rulesVersion, catalogVersion: localSupportCatalog.catalogVersion,
    expectedRevision: 0, slots: ["barrier", "support_fire", "emergency_repair"].map((skillId, slot) => ({ slot, skillId })) });
  const selection = { ...body }; Reflect.deleteProperty(selection, "expectedUltimateRevision");
  return { ...f, post, request: { selection, expectedSupportRevision: 1 } };
}

describe("opt-in rail preparation", () => {
  it.each([false,true])("hashes explicit rail presentation separately from unchanged legacy preparation (Zone02=%s)",async zone=>{
    const base=new URL(zone?"../fixtures/rail-presentation-zone02-v1/":"../fixtures/rail-presentation-v1/",import.meta.url),read=(name:string)=>readFileSync(new URL(name,base),"utf8");
    const profile=await projectRailPresentation({audit:read("presentation-closure-audit.json"),extension:read("second-final-closure-extension.json"),selection:read("design-manifest-v3.json"),asset:read("approved.asset.txt"),
      ...(zone?{profile:"second-final-zone02-v1" as const,zone02Selection:read("zone02-selection-manifest.json"),directionManifest:read("direction-manifest.json")}:{})});
    const f=await hostFixture(true,profile);
    try{const before=f.sql.export(),legacy=await(await f.post("/__local/ce03/prepare",f.request)).json();
      const result=await(await f.post("/__local/ce03/rail-prepare",f.request)).json();
      expect(result.railPresentation).toEqual(profile);expect(result.basePreparation).toEqual(legacy);
      const {preparationSha256,...payload}=result;expect(preparationSha256).toBe(await sha256Hex(canonicalJson(payload)));
      expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(0);
    }finally{f.sql.close();}
    const tampered=await hostFixture(true,{...profile,manifestSha256:"0".repeat(64)});
    try{expect((await tampered.post("/__local/ce03/rail-prepare",tampered.request)).status).toBe(409);}
    finally{tampered.sql.close();}
    await expect(hostFixture(false,profile)).rejects.toThrow("rail_presentation_requires_local_unadmitted_profile");
  });
  it("exposes real authored offers in a separately hashed wrapper without changing legacy prepare or creating a run", async () => {
    const f = await hostFixture(true);
    try {
      const old = await (await f.post("/__local/ce03/prepare", f.request)).json(), before = f.sql.export();
      const response = await f.post("/__local/ce03/rail-prepare", f.request);
      expect(response.status).toBe(200);
      const result = await response.json();
      expect(result).toMatchObject({ version: "ce03-rail-preparation-v1", ready: false, railJourney: {
        version: "rail-journey-v1", selectionScaleBps: 100, recoveryMs: 600, throttleMinimumBps: 7000, throttleCruiseBps: 10000, throttleMaximumBps: 13000 } });
      expect(result.basePreparation).toEqual(old);
      expect(result.railJourney.offers.map((o: { afterCompletedWaves: number }) => o.afterCompletedWaves)).toEqual([3, 6, 9, 12]);
      for (const offer of result.railJourney.offers) expect(offer.options).toHaveLength(2);
      expect(result.railJourneySha256).toBe(await sha256Hex(canonicalJson(result.railJourney)));
      expect(await (await f.post("/__local/ce03/prepare", f.request)).json()).toEqual(old);
      expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
      expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(0);
      evidence("synthetic-rail-preparation.json", result);
    } finally { f.sql.close(); }
  });
  it("requires host opt-in, not a client rail flag", async () => {
    const f = await hostFixture(false);
    try {
      expect((await f.post("/__local/ce03/rail-prepare", f.request)).status).toBe(404);
      expect((await f.post("/__local/ce03/prepare", { ...f.request, railJourney: true })).status).toBe(400);
    } finally { f.sql.close(); }
  });
  it("can carry four verified visual carriages while keeping the two legacy combat actors", async () => {
    const f = await hostFixture(true);
    try {
      const selected = [{ slot: 0, carriageId: "car_combat" }, { slot: 1, carriageId: "car_cargo" }, { slot: 2, carriageId: "car_gun" }, { slot: 3, carriageId: "car_defense" }];
      f.sql.run("UPDATE account_train_loadout SET loadout_json=? WHERE account_id='a'", [JSON.stringify({ rulesVersion: "train-loadout-v1", trainId: "train_a", carriages: selected })]);
      f.progress(["opn_03_water_stop", "opn_05_night_boarders"]);
      const before = f.sql.export(), response = await f.post("/__local/ce03/rail-prepare", f.request);
      expect(response.status).toBe(200);
      const prepared = await response.json();
      expect(prepared.railJourney.carriages).toHaveLength(4);
      expect(prepared.railJourney.carriages.map((x: { type: string }) => x.type)).toEqual(["Combat", "Cargo", "Gun", "Defense"]);
      expect(prepared.basePreparation.draft.template.cars.map((x: { type: string }) => x.type)).toEqual(["Combat", "Gun"]);
      expect(prepared.selectedCarriageOwnership.carriages.map((x: { carriageId: string }) => x.carriageId)).toEqual(selected.map(x => x.carriageId));
      expect(prepared.domainProjection).toBe("LEGACY_COMBAT_GUN_ONLY");
      expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
      evidence("synthetic-rail-preparation-four-cars.json", prepared);
    } finally { f.sql.close(); }
  });
});

async function config() {
  const f = await hostFixture(true);
  try { return (await (await f.post("/__local/ce03/rail-prepare", f.request)).json()).railJourney as rail.RailJourneyConfig; }
  finally { f.sql.close(); }
}
describe("rail choices and control replay", () => {
  it("provides additive artillery encounters without replacing required wave enemies", async () => {
    const c = await config();
    expect(c.artillery).toMatchObject({ version: "rail-artillery-v1", stepMs: 50,
      definition: { id: "rail_artillery_emplacement_v1", maxHp: 1000, trainDamage: 50, periodMs: 2000, windupMs: 500, projectileTravelMs: 500 },
      policy: { requiredForWaveCompletion: false, passedKillCount: 0, reward: "NONE", onExit: "CANCEL_UNLAUNCHED_KEEP_IN_FLIGHT" } });
    expect(c.artillery.encounters.map(e => e.spawnWave)).toEqual([2, 5, 8, 11, 14]);
    expect(c.artillery.encounters.every(e => e.exposureDistanceMm === c.baseSpeedMillimetersPerSecond * 12)).toBe(true);
  });
  it("opens offers only at configured nodes and applies each choice once across checkpoint restoration", async () => {
    const c = await config(); let state = await rail.createRailState(c, "a", "local_session_a");
    const first = c.offers[0]!, selected = first.options[0]!;
    const open = { sequence: 1, kind: "OPEN", tick: 100, completedWaves: 3, offerId: first.id };
    await expect(rail.applyRailControl(c, state, { ...open, completedWaves: 2 })).rejects.toThrow();
    state = await rail.applyRailControl(c, state, open);
    expect(state.bonusBps.SQUAD_DAMAGE).toBe(0);
    state = await rail.applyRailControl(c, state, { sequence: 2, kind: "PREVIEW", tick: 101, completedWaves: 3, offerId: first.id, optionId: selected.id });
    const confirm = { sequence: 3, kind: "CONFIRM", tick: 102, completedWaves: 3, offerId: first.id, optionId: selected.id };
    state = await rail.applyRailControl(c, state, confirm);
    expect(state.bonusBps.SQUAD_DAMAGE).toBe(500);
    expect(state.confirmed[0]).toMatchObject({ offerId: first.id, optionId: selected.id, augmentId: selected.augmentId, routeId: selected.routeId });
    const restored = await rail.restoreRailState(c, "a", "local_session_a", JSON.parse(JSON.stringify(state)));
    expect(restored).toEqual(state);
    await expect(rail.applyRailControl(c, restored, confirm)).rejects.toThrow();
    await expect(rail.applyRailControl(c, restored, { ...confirm, sequence: 4 })).rejects.toThrow();
    await expect(rail.restoreRailState(c, "other", "local_session_a", state)).rejects.toThrow();
    await expect(rail.restoreRailState(c, "a", "other_session", state)).rejects.toThrow();
    await expect(rail.restoreRailState(c, "a", "local_session_a", { ...state, bonusBps: { ...state.bonusBps, SQUAD_DAMAGE: 1000 } })).rejects.toThrow();
  });
  it("rejects forged options, premature/skipped offers, nonmonotonic time and unknown event fields", async () => {
    const c = await config(), initial = await rail.createRailState(c, "a", "s");
    const opened = await rail.applyRailControl(c, initial, { sequence: 1, kind: "OPEN", tick: 10, completedWaves: 3, offerId: c.offers[0]!.id });
    const confirm = { sequence: 2, kind: "CONFIRM", tick: 11, completedWaves: 3, offerId: c.offers[0]!.id, optionId: c.offers[0]!.options[0]!.id };
    for (const bad of [{ ...confirm, optionId: c.offers[1]!.options[0]!.id }, { ...confirm, tick: 9 }, { ...confirm, completedWaves: 4 },
      { ...confirm, effect: { valueBps: 9999 } }, { ...confirm, sequence: 4 }]) await expect(rail.applyRailControl(c, opened, bad)).rejects.toThrow();
    await expect(rail.applyRailControl(c, initial, { sequence: 1, kind: "OPEN", tick: 10, completedWaves: 6, offerId: c.offers[1]!.id })).rejects.toThrow();
    expect(initial.journal).toHaveLength(0);
  });
  it("keeps throttle during selection, has no timeout and caps accumulated additive bonuses", async () => {
    const c = await config(); let s = await rail.createRailState(c, "a", "s");
    expect(s.throttleBps).toBe(10000);
    s = await rail.applyRailControl(c, s, { sequence: 1, kind: "THROTTLE", tick: 0, completedWaves: 0, throttleBps: 13000 });
    s = await rail.applyRailControl(c, s, { sequence: 2, kind: "THROTTLE", tick: 1, completedWaves: 0, throttleBps: 7000 });
    await expect(rail.applyRailControl(c, s, { sequence: 3, kind: "THROTTLE", tick: 2, completedWaves: 0, throttleBps: 6999 })).rejects.toThrow();
    for (const offer of c.offers) {
      s = await rail.applyRailControl(c, s, { sequence: s.nextSequence, kind: "OPEN", tick: s.tick + 1, completedWaves: offer.afterCompletedWaves, offerId: offer.id });
      expect(s.throttleBps).toBe(7000);
      s = await rail.applyRailControl(c, s, { sequence: s.nextSequence, kind: "CONFIRM", tick: s.tick + 100000, completedWaves: offer.afterCompletedWaves, offerId: offer.id, optionId: offer.options[0]!.id });
    }
    expect(s.confirmed).toHaveLength(4);
    expect(Object.values(s.bonusBps).reduce((a, b) => a + b, 0)).toBe(2000);
    const capped = rail.effectiveRailBonuses({ ...c, baseBonusesBps: { SQUAD_DAMAGE: 2900, TRAIN_DAMAGE: 2900, SHIELD_MAGNITUDE: 2900 } }, s.bonusBps);
    expect(capped).toEqual({ SQUAD_DAMAGE: 3000, TRAIN_DAMAGE: 3000, SHIELD_MAGNITUDE: 3000 });
  });
  it("validates a separately bound control report by replay, and rejects truncation/tampering without enabling legacy finish", async () => {
    const c = await config(); let s = await rail.createRailState(c, "a", "s");
    for (const o of c.offers) {
      s = await rail.applyRailControl(c, s, { sequence: s.nextSequence, kind: "OPEN", tick: s.tick + 1, completedWaves: o.afterCompletedWaves, offerId: o.id });
      s = await rail.applyRailControl(c, s, { sequence: s.nextSequence, kind: "CONFIRM", tick: s.tick + 1, completedWaves: o.afterCompletedWaves, offerId: o.id, optionId: o.options[0]!.id });
    }
    const report = { version: "ce03-rail-control-report-v1", owner: "a", sessionId: "s", configSha256: s.configSha256,
      modelSchema: "ce03-rail-headless-state-v1", modelSha256: "1".repeat(64), finalTick: s.tick + 1, completedWaves: 15,
      journal: s.journal, bonusBps: s.bonusBps };
    const expected = { owner: "a", sessionId: "s", configSha256: s.configSha256, modelSha256: report.modelSha256,
      finalTick: report.finalTick, completedWaves: 15, journalSha256: await sha256Hex(canonicalJson(s.journal)) };
    expect(await rail.verifyRailControlReport(c, report, expected)).toMatchObject({ status: "CONTROL_REPLAY_VERIFIED", ready: false, legacyFinishAllowed: false });
    const model = { schema: "ce03-rail-headless-state-v1", configurationSha256: "2".repeat(64), clock: { completed: report.finalTick }, completedWaves: 15,
      railJourney: { currentTick: report.finalTick, completedWaves: 15, journal: s.journal, confirmed: s.confirmed.map(receipt => {
        const choice = c.offers.find(o => o.id === receipt.offerId)!.options.find(o => o.id === receipt.optionId)!;
        const { tick, ...rest } = receipt; return { ...rest, atTick: tick, stat: choice.effect.stat, valueBps: choice.effect.valueBps };
      }) } };
    const rawModel = JSON.stringify(model), modelSha256 = await sha256Hex(rawModel);
    const boundReport = { ...report, modelSha256 }, modelPins = { owner: "a", sessionId: "s", modelSha256, configurationSha256: model.configurationSha256 };
    expect(await rail.verifyRailModelControlReport(c, rawModel, boundReport, modelPins)).toMatchObject({ status: "CONTROL_REPLAY_VERIFIED", legacyFinishAllowed: false });
    evidence("synthetic-control-bundle.json", { provenance: "SERVER_TEST_SYNTHETIC_NOT_UNITY_EXECUTION", config: c, rawModel, report: boundReport, trustedModelPins: modelPins, controlState: s });
    await expect(rail.verifyRailModelControlReport(c, rawModel + " ", boundReport, modelPins)).rejects.toThrow();
    await expect(rail.verifyRailModelControlReport(c, rawModel, boundReport, { ...modelPins, configurationSha256: "3".repeat(64) })).rejects.toThrow();
    const altered = JSON.stringify({ ...model, railJourney: { ...model.railJourney, confirmed: [] } });
    const alteredHash = await sha256Hex(altered);
    await expect(rail.verifyRailModelControlReport(c, altered, { ...boundReport, modelSha256: alteredHash }, { ...modelPins, modelSha256: alteredHash })).rejects.toThrow();
    for (const bad of [{ ...report, owner: "other" }, { ...report, journal: s.journal.slice(0, -1) },
      { ...report, bonusBps: { ...s.bonusBps, SQUAD_DAMAGE: 9999 } }, { ...report, modelSchema: "ce03-headless-state-v1" }])
      await expect(rail.verifyRailControlReport(c, bad, expected)).rejects.toThrow();
  });
});
