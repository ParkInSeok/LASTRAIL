import { describe, expect, it } from "vitest";
import { readFileSync, mkdirSync, writeFileSync } from "node:fs";
import { createHash } from "node:crypto";
import { createContext, runInContext } from "node:vm";
import ts from "typescript";
import { launchGachaPool } from "../../packages/balance/src/gacha-policy";

// Test-only AST extraction executes the exact production function bodies; no
// alternate sampler and no test hook or seed control in the public API.
const source = readFileSync("apps/game-api/src/index.ts", "utf8");
const ast = ts.createSourceFile("index.ts", source, ts.ScriptTarget.Latest, true);
const names = new Set(["parseGachaPoolResults", "parseGachaPityConfig", "getInitialPityState",
  "applyGachaPityResult", "finalizeGachaPityState", "getNextGachaGuaranteeType",
  "isGachaPoolResultEligibleForGuarantee", "hashStringToUint32", "getGachaResultWeight", "selectGachaPoolResult"]);
const functions = ast.statements.filter((node) => ts.isFunctionDeclaration(node) && node.name && names.has(node.name.text));
const code = ts.transpileModule(functions.map((node) => node.getText(ast)).join("\n"), {
  compilerOptions: { target: ts.ScriptTarget.ES2022 },
}).outputText;
type Result = { resultId: string; resultRarity: string; resultType: string; weight?: number; isPickup?: boolean; quantity?: number };
type State = { totalPullCount: number; elitePityCount: number; pickupElitePityCount: number; rareOrHigherPityCount: number; revision: number };
const banner = { banner_type: "PICKUP", pickup_operator_id: "op_vex" };
const draft = launchGachaPool.results;
function runtime(entropy: () => number = () => 0) {
  const context = createContext({ crypto: { getRandomValues: (buffer: Uint32Array) => { buffer[0] = entropy(); return buffer; } } });
  runInContext(code, context);
  return {
    parse: (pool: unknown): Result[] => context.parseGachaPoolResults(JSON.stringify(pool)),
    parseRaw: (pool: string): Result[] => context.parseGachaPoolResults(pool),
    pick: (pool: Result[], guarantee = "NONE", seed = "test-only"): Result | null => context.selectGachaPoolResult({ poolResults: pool, guaranteeType: guarantee, banner, seed }),
    forceLegacyRoll: (value: number) => { context.hashStringToUint32 = () => value; },
    next: (state: State, type = "PICKUP"): string => context.getNextGachaGuaranteeType({ banner: { ...banner, banner_type: type }, pityState: state, pityConfig: { rare: 10, elite: 30, pickup: 60 } }),
    apply: (state: State, result: Result): State => context.applyGachaPityResult(state, { ...result, isPickup: Boolean(result.isPickup) || result.resultId === "op_vex" }),
  };
}
const common: Result = { resultId: "mat_a", resultRarity: "COMMON", resultType: "TRAINING_MATERIAL", weight: 1 };

describe("full-pool release audit against production functions", () => {
  it("fails closed instead of silently changing a draft pool with unsupported candidate types", () => {
    expect(runtime().parse({ results: [{ ...draft[0], resultType: "UNSUPPORTED_TYPE" }] }).length).toBe(0);
  });
  it("rejects invalid JSON, duplicate ids, invalid weights and unsafe totals", () => {
    const run = runtime();
    for (const raw of ["{", "null", "[]", "{}", '{"results":{}}']) expect(run.parseRaw(raw)).toEqual([]);
    for (const results of [[common, common], [{ ...common, resultId: "" }], [{ ...common, weight: -1 }],
      [{ ...common, weight: 0.5 }], [{ ...common, weight: 2 ** 32 + 1 }], [{ ...common, quantity: -1 }]]) {
      expect(run.parse({ results })).toEqual([]);
    }
  });
  it("uses server entropy rather than public account/banner/request text for a fresh draw", () => {
    const pool = [common, { ...common, resultId: "mat_b" }];
    expect(runtime(() => 0).pick(pool)?.resultId).toBe("mat_a");
    expect(runtime(() => 1).pick(pool)?.resultId).toBe("mat_b");
  });
  it("covers every cumulative interval boundary in every existing guarantee domain", () => {
    const accepted = draft; // Sampler-only audit of all 21 approved candidates; route rejection is tested separately.
    for (const guarantee of ["NONE", "RARE_OR_HIGHER_10", "ELITE_30", "PICKUP_ELITE_60"]) {
      const candidates = accepted.filter((r) => guarantee === "NONE" || (guarantee === "RARE_OR_HIGHER_10" ? r.resultRarity !== "COMMON" : guarantee === "ELITE_30" ? r.resultRarity === "ELITE" : r.isPickup));
      let boundary = 0;
      for (const candidate of candidates) {
        for (const roll of [boundary, boundary + candidate.weight - 1]) {
          const picker = runtime(() => roll); picker.forceLegacyRoll(roll);
          expect(picker.pick(accepted, guarantee)?.resultId).toBe(candidate.resultId);
        }
        boundary += candidate.weight;
      }
    }
  });
  it("preserves current zero-weight hard-guarantee fallback and counter precedence/reset", () => {
    const run = runtime();
    const rare = { ...common, resultRarity: "RARE", weight: 0 };
    expect(run.pick([rare])).toBeNull();
    expect(run.pick([rare], "RARE_OR_HIGHER_10")?.resultId).toBe(rare.resultId);
    const state = { totalPullCount: 59, elitePityCount: 29, pickupElitePityCount: 59, rareOrHigherPityCount: 9, revision: 4 };
    expect(run.next(state)).toBe("PICKUP_ELITE_60");
    expect(run.next(state, "STANDARD")).toBe("ELITE_30");
    expect(run.next({ ...state, elitePityCount: 0, pickupElitePityCount: 0 })).toBe("RARE_OR_HIGHER_10");
    expect(run.apply(state, draft.find((r) => r.resultId === "op_vex")!)).toEqual({ totalPullCount: 60, elitePityCount: 0, pickupElitePityCount: 0, rareOrHigherPityCount: 0, revision: 4 });
    expect(run.apply(state, draft[1]).pickupElitePityCount).toBe(60);
  });
  it("rejects the uint32 tail rather than introducing modulo bias and fails closed on entropy failure", () => {
    let calls = 0;
    const pool = [common, { ...common, resultId: "mat_b", weight: 2 }];
    const run = runtime(() => ++calls === 1 ? 0xffffffff : 1);
    expect(run.pick(pool)?.resultId).toBe("mat_b");
    expect(calls).toBe(2);
    expect(runtime(() => { throw new Error("test entropy unavailable"); }).pick(pool)).toBeNull();
    let rejected = 0;
    expect(runtime(() => { if (++rejected > 128) throw new Error("unbounded rejection"); return 0xffffffff; }).pick(pool)).toBeNull();
    expect(rejected).toBe(128);
  });
  it("records reproducible base and conditional simulations separately without claiming launch approval", () => {
    let prng = 0x6a09e667;
    const entropy = () => { prng ^= prng << 13; prng ^= prng >>> 17; prng ^= prng << 5; return prng >>> 0; };
    const run = runtime(entropy);
    const accepted = draft; // Mathematical draft audit only; not a parser or shipping approval bypass.
    const summaries = [];
    for (const guarantee of ["NONE", "RARE_OR_HIGHER_10", "ELITE_30", "PICKUP_ELITE_60"]) {
      const candidates = accepted.filter((r) => guarantee === "NONE" || (guarantee === "RARE_OR_HIGHER_10" ? r.resultRarity !== "COMMON" : guarantee === "ELITE_30" ? r.resultRarity === "ELITE" : r.isPickup));
      const total = candidates.reduce((sum, r) => sum + r.weight, 0);
      const n = 200000;
      const counts = new Map<string, number>();
      for (let index = 0; index < n; index++) {
        const result = run.pick(accepted, guarantee, `audit-simulation-${index}`)!;
        counts.set(result.resultId, (counts.get(result.resultId) ?? 0) + 1);
      }
      const checks = candidates.map((candidate) => {
        const p = candidate.weight / total;
        const expected = n * p;
        const observed = counts.get(candidate.resultId) ?? 0;
        const tolerance = Math.max(1, 6 * Math.sqrt(n * p * (1 - p)));
        expect(observed).toBeGreaterThan(0);
        expect(Math.abs(observed - expected)).toBeLessThanOrEqual(tolerance);
        return { resultId: candidate.resultId, expected, observed, deviation: observed - expected, tolerance };
      });
      summaries.push({ guarantee, n, checks });
    }
    mkdirSync("audit-results", { recursive: true });
    writeFileSync("audit-results/simulation-internal.json", JSON.stringify({
      status: "APPROVED_21_POOL_LOCAL_SAMPLER_VERIFIED_NOT_REMOTE", sourceSha256: createHash("sha256").update(source).digest("hex"),
      method: "test-only xorshift32 entropy; six sigma per candidate; independent guarantee domains; not stateful overall rates", summaries,
    }, null, 2));
  }, 30000);
});
