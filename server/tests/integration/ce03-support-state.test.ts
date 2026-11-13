import { expect, it, vi } from "vitest";
import { createSupportState, acceptSupportCommand, stepSupportState, restoreSupportState } from "../../apps/game-api/src/ce03-support-state";
const selected = ["ce03.role.medical_support", "ce03.role.smoke", "ce03.role.electronic_disruption"];
const hash = "a".repeat(64);
const actors = [{ entityId: "op", hp: 50, maxHp: 100, slot: 0, spawnOrder: 0, side: "OPERATOR", lane: "INTERIOR", returning: false, action: null }];
const command = (key: string, skillId = selected[0]) => ({ version: "ce03-support-command-v2", key, skillId, targetId: null });
it("preserves remaining simulation delay through loading time and repeated checkpoint restoration", () => {
  const wallClock = vi.spyOn(Date, "now").mockReturnValue(1000);
  try {
    const initial = createSupportState(hash, selected);
    let state = acceptSupportCommand(initial, command("before-suspend"), actors, null);
    for (let i = 0; i < 3; i++) state = stepSupportState(state);
    const serialized = JSON.stringify(state);
    expect(state.pending!.dueTick - state.tick).toBe(4);
    wallClock.mockReturnValue(1000 + 24 * 60 * 60 * 1000);
    expect(createSupportState(hash, selected)).toEqual(initial);
    for (let i = 0; i < 3; i++) {
      const restored = restoreSupportState(JSON.parse(serialized), hash, selected);
      expect(restored).toEqual(state);
      expect(acceptSupportCommand(restored, command("before-suspend"), [], null)).toEqual(state);
      expect(restored.outbox).toHaveLength(0);
    }
    state = restoreSupportState(JSON.parse(serialized), hash, selected);
    for (let i = 0; i < 3; i++) state = stepSupportState(state);
    expect(state.outbox).toHaveLength(0);
    state = stepSupportState(state);
    expect(state.outbox).toHaveLength(1);
    expect(state.outbox[0]).toMatchObject({ commandKey: "before-suspend", dueTick: 7, sequence: 1 });
    expect(restoreSupportState(JSON.parse(JSON.stringify(state)), hash, selected).outbox).toEqual(state.outbox);
    expect(() => restoreSupportState(undefined, hash, selected)).toThrow();
  } finally { wallClock.mockRestore(); }
});
it("persists cast pins, starts independent cooldown, rejects shared channel without spending", () => {
  const initial = createSupportState(hash, selected);
  const cast = acceptSupportCommand(initial, command("a"), actors, null);
  expect(cast.pending?.pins).toEqual([{ entityId: "op", actionId: null }]);
  expect(cast.cooldowns.map(x => x.readyTick)).toEqual([600, 0, 0]);
  expect(() => acceptSupportCommand(cast, command("b", selected[1]), actors, null)).toThrow();
  expect(initial.pending).toBeNull();
});
it("restores exact state and emits one durable due intent even across duplicate input", () => {
  let state = acceptSupportCommand(createSupportState(hash, selected), command("a"), actors, null);
  state = restoreSupportState(JSON.parse(JSON.stringify(state)), hash, selected);
  expect(acceptSupportCommand(state, command("a"), [], null)).toEqual(state);
  for (let i = 0; i < 7; i++) state = stepSupportState(state);
  expect(state.outbox).toHaveLength(1);
  expect(state.outbox[0]).toMatchObject({ dueTick: 7, sequence: 1, commandKey: "a" });
  expect(stepSupportState(state).outbox).toHaveLength(1);
  expect(() => acceptSupportCommand(state, command("a", selected[1]), actors, null)).toThrow();
});
it("releases shared channel only after windup plus recovery while cooldown is independent", () => {
  let state = acceptSupportCommand(createSupportState(hash, selected), command("a"), actors, null);
  for (let i = 0; i < 12; i++) state = stepSupportState(state);
  expect(state.pending).toBeNull();
  expect(() => acceptSupportCommand(state, command("again"), actors, null)).toThrow();
  expect(acceptSupportCommand(state, command("smoke", selected[1]), actors, null).pending?.skillId).toBe(selected[1]);
});
it("fails closed for wrong profile, forged timing, unknown checkpoint fields and nonselected commands", () => {
  const state = acceptSupportCommand(createSupportState(hash, selected), command("a"), actors, null);
  expect(() => restoreSupportState(state, "b".repeat(64), selected)).toThrow();
  expect(() => restoreSupportState({ ...state, authorized: true }, hash, selected)).toThrow();
  expect(() => restoreSupportState({ ...state, pending: { ...state.pending, releaseTick: 1 } }, hash, selected)).toThrow();
  expect(() => acceptSupportCommand(createSupportState(hash, selected), command("x", "ce03.role.weakness_scan"), actors, null)).toThrow();
});
