import { describe, expect, it } from "vitest";
import { parseOfflineAdPolicy, selectOfflineAdMultiplier, multiplyOfflineSnapshot, getOfflineAdDisplayMultipliers } from "../../apps/game-api/src/offline-ad-policy";

// Explicit synthetic weights/time limits; not approved operating settings.
const policy = { version: "TEST_ONLY", weightsBps: [5000, 3000, 2000], challengeLifetimeMs: 1000,
  callbackGraceMs: 2000, maxAdsPerDay: 2, dayStartUtcMinutes: 0, adUnit: "TEST_UNIT", appId: "TEST_APP", placement: "OFFLINE_SUPPLY",
  environment: "LOCAL", rewardItem: "TEST_REWARD", rewardAmount: "1" };
describe("offline ad policy and snapshot arithmetic", () => {
  it("publishes only policy-possible multipliers without probability values", () => {
    expect(getOfflineAdDisplayMultipliers(parseOfflineAdPolicy(policy))).toEqual([2, 3, 4]);
    expect(getOfflineAdDisplayMultipliers(parseOfflineAdPolicy({ ...policy, weightsBps: [5000, 0, 5000] }))).toEqual([2, 4]);
    expect(getOfflineAdDisplayMultipliers(parseOfflineAdPolicy({ ...policy, weightsBps: [0, 10000, 0] }))).toEqual([3]);
  });
  it.each([null, undefined, {}, { ...policy, weightsBps: [1, 1, 1] }, { ...policy, maxAdsPerDay: 0 }])("fails closed on absent or incomplete policy", (input) => {
    expect(() => parseOfflineAdPolicy(input)).toThrow("ad_reward_policy_unconfigured");
  });
  it("accepts only explicit complete policy and boundary-selects 2/3/4", () => {
    const parsed = parseOfflineAdPolicy(policy);
    expect([0, 4999, 5000, 7999, 8000, 9999].map(draw => selectOfflineAdMultiplier(parsed, draw))).toEqual([2, 2, 3, 3, 4, 4]);
  });
  it.each([-1, 10000, 0.5, NaN])("rejects invalid integer RNG draw %s", (draw) => {
    expect(() => selectOfflineAdMultiplier(parseOfflineAdPolicy(policy), draw)).toThrow("ad_reward_draw_invalid");
  });
  it("multiplies the frozen integer base of all four existing resources with no extra rounding", () => {
    const base = [
      { id: "cur_credits", quantity: 3 }, { id: "cur_machine_parts", quantity: 2 },
      { id: "cur_tactical_data", quantity: 1 }, { id: "cur_operator_exp", quantity: 7 },
    ];
    expect(multiplyOfflineSnapshot(base, 3)).toEqual(base.map(line => ({ ...line, quantity: line.quantity * 3 })));
    expect(base[0]?.quantity).toBe(3);
  });
  it.each([
    [{ id: "cur_crystals", quantity: 1 }], [{ id: "cur_credits", quantity: Number.MAX_SAFE_INTEGER }],
    [{ id: "cur_credits", quantity: 1.5 }], [{ id: "cur_credits", quantity: -1 }],
    [{ id: "cur_credits", quantity: 1 }, { id: "cur_credits", quantity: 2 }], [],
  ].map(base => ({ base })))("rejects malformed/duplicate/overflow snapshot", ({ base }) => {
    expect(() => multiplyOfflineSnapshot(base, 4)).toThrow("ad_reward_snapshot_invalid");
  });
});
