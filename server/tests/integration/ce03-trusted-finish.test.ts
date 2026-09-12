import { expect, it } from "vitest";
import { mkdtemp, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { trustedFinishCe03 } from "../../scripts/local/ce03-trusted-finish";

async function fixture() {
  const outputDirectory = await mkdtemp(join(tmpdir(), "ce03-finish-test-"));
  const hash = "a".repeat(64), prepare = { prepareSha256: "b".repeat(64), ownedSelection: { accountId: "a" } };
  const startBody = { operationPreviewId: "preview", expectedSupportRevision: 1, prepareSha256: prepare.prepareSha256 };
  const responseRaw = canonicalJson({ accepted: true, runId: "run1", admission: { loadout: { trainId: "train" } }, combat: { seed: { value: 42 }, snapshotSha256: "c".repeat(64), profile: { baseProfile: { executionEvidence: { algorithmSourceSha256: hash } } } } });
  const original = { version: "ce03-original-start-v1", owner: "a", origin: "http://127.0.0.1:1234", key: "original key", body: startBody, responseRaw, prepared: prepare };
  const model = canonicalJson({ schema: "ce03-headless-state-v1", outcome: "Victory", completedWaves: 15, clock: { completed: 12 }, actors: [{ id: "train", vitals: { hp: 50 } }] });
  const completion = { schemaVersion: "ce03-actual-local-completion-v1", actualAccountRun: true, scope: "DISPOSABLE_LOCAL", runId: "run1", seed: 42, originalSnapshotSha256: "c".repeat(64), algorithmSourceSha256: hash, prepareSha256: prepare.prepareSha256, completedWaveCount: 15, outcome: "VICTORY", modelSha256: await sha256Hex(model), durableRevision: 3, tick: 12 };
  const body = { runId: "run1", encounterType: "ORDINARY_15_WAVE", currentStage: "Complete", currentWaveNumber: 0, completedWaveCount: 15, outcome: "VICTORY", runtimeStateJson: canonicalJson(completion), tacticSelections: [], clientCompletedAt: "2026-09-12T01:00:00.000Z" };
  const intent = { method: "POST", path: "/v1/runs/run1/finish", idempotencyKey: "finish original key", body, sent: false };
  for (const [name, raw] of [["original-start.json", canonicalJson(original)], ["model-final.json", model], ["completion.json", canonicalJson(completion)], ["finish-intent.json", canonicalJson(intent)]]) await writeFile(join(outputDirectory, name!), raw!);
  let sends = 0;
  const options = { outputDirectory, owner: "a", origin: original.origin, prepared: prepare, sourceBundleSha256: hash, dynamicResourceFileCount: 66,
    assertUnityExited: async () => {}, verifyExecutionIdentity: async () => true,
    lookupOriginalStart: async (key: string, request: unknown) => key === original.key && canonicalJson(request as never) === canonicalJson(startBody) ? responseRaw : null,
    sendFinish: async (_path: string, sentBody: unknown, key: string) => { expect(sentBody).toEqual(body); expect(key).toBe(intent.idempotencyKey); sends++; return { status: 200, responseRaw: canonicalJson({ accepted: sends === 1, resultStatus: sends === 1 ? "FINISHED" : "DUPLICATE_REPLAY", runId: "run1", rewardPreviewId: "rp1" }) }; } };
  return { options, outputDirectory, original, completion, intent, sends: () => sends };
}
it("validates retained fresh START and terminal model before sending the preserved finish once per invocation", async () => {
  const f = await fixture();
  expect((await trustedFinishCe03(f.options)).rewardPreviewId).toBe("rp1");
  expect((await trustedFinishCe03(f.options)).rewardPreviewId).toBe("rp1");
  expect(f.sends()).toBe(2);
});
it.each(["source", "unity", "receipt", "seed", "model", "intent", "revision"])("never sends on failed boundary %s", async mode => {
  const f = await fixture();
  if (mode === "source") f.options.verifyExecutionIdentity = async () => false;
  if (mode === "unity") f.options.assertUnityExited = async () => { throw new Error("still running"); };
  if (mode === "receipt") f.options.lookupOriginalStart = async () => null;
  if (mode === "seed") { f.completion.seed++; await writeFile(join(f.outputDirectory, "completion.json"), canonicalJson(f.completion)); }
  if (mode === "model") await writeFile(join(f.outputDirectory, "model-final.json"), "{}");
  if (mode === "intent") { f.intent.body.runtimeStateJson = "{}"; await writeFile(join(f.outputDirectory, "finish-intent.json"), canonicalJson(f.intent)); }
  if (mode === "revision") { f.completion.durableRevision = 0; await writeFile(join(f.outputDirectory, "completion.json"), canonicalJson(f.completion)); }
  await expect(trustedFinishCe03(f.options)).rejects.toThrow(); expect(f.sends()).toBe(0);
});
it("rejects a self-consistent model hash when the model is not terminal", async () => {
  const f = await fixture();
  const raw = canonicalJson({ schema: "ce03-headless-state-v1", outcome: "Running", completedWaves: 15, clock: { completed: 12 }, actors: [{ id: "train", vitals: { hp: 50 } }] });
  f.completion.modelSha256 = await sha256Hex(raw); f.intent.body.runtimeStateJson = canonicalJson(f.completion);
  await writeFile(join(f.outputDirectory, "model-final.json"), raw); await writeFile(join(f.outputDirectory, "completion.json"), canonicalJson(f.completion)); await writeFile(join(f.outputDirectory, "finish-intent.json"), canonicalJson(f.intent));
  await expect(trustedFinishCe03(f.options)).rejects.toThrow(); expect(f.sends()).toBe(0);
});
it.each(["source", "output"])("rechecks %s immediately before sending", async mode => {
  const f = await fixture(); let checks = 0;
  f.options.verifyExecutionIdentity = async () => { checks++; if (checks === 2) { if (mode === "source") return false; await writeFile(join(f.outputDirectory, "model-final.json"), "changed after validation"); } return true; };
  await expect(trustedFinishCe03(f.options)).rejects.toThrow(); expect(f.sends()).toBe(0);
});
it("rejects wrong owner/origin and reward fields rather than forwarding them", async () => {
  const f = await fixture();
  await expect(trustedFinishCe03({ ...f.options, owner: "foreign" })).rejects.toThrow();
  await expect(trustedFinishCe03({ ...f.options, origin: "https://example.com" })).rejects.toThrow();
  await writeFile(join(f.outputDirectory, "finish-intent.json"), canonicalJson({ ...f.intent, body: { ...f.intent.body, rewards: [] } }));
  await expect(trustedFinishCe03(f.options)).rejects.toThrow(); expect(f.sends()).toBe(0);
});
it("requires retained dynamic resource pins for this fresh UI finish flow", async () => {
  const f = await fixture();
  await expect(trustedFinishCe03({ ...f.options, dynamicResourceFileCount: 0 })).rejects.toThrow();
  expect(f.sends()).toBe(0);
});
