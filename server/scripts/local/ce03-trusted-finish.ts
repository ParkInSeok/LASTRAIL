import { execFile } from "node:child_process";
import { createHash } from "node:crypto";
import { readFile, realpath, stat } from "node:fs/promises";
import { isAbsolute, relative, resolve, sep } from "node:path";
import { z } from "zod";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { id, nat, pos, sha } from "../../apps/game-api/src/ce03-local-schema";

type Json = Parameters<typeof canonicalJson>[0];
type Options = { outputDirectory: string; owner: string; origin: string; prepared: Json; sourceBundleSha256: string; dynamicResourceFileCount: number;
  assertUnityExited(): Promise<void>; verifyExecutionIdentity(): Promise<boolean>;
  lookupOriginalStart(key: string, body: Json): Promise<string | null>;
  sendFinish(path: string, body: Json, key: string): Promise<{ status: number; responseRaw: string }> };
const keySchema = z.string().min(1).max(512).refine(s => s.trim().length > 0);
const startBody = z.strictObject({ operationPreviewId: id, expectedSupportRevision: pos, prepareSha256: sha });
const originalSchema = z.strictObject({ version: z.literal("ce03-original-start-v1"), owner: id, origin: z.string(), key: keySchema,
  body: startBody, responseRaw: z.string().max(8_000_000), prepared: z.json() });
const completionSchema = z.object({ schemaVersion: z.literal("ce03-actual-local-completion-v1"), actualAccountRun: z.literal(true), scope: z.literal("DISPOSABLE_LOCAL"),
  runId: id, seed: nat.max(4294967295), originalSnapshotSha256: sha, algorithmSourceSha256: sha, prepareSha256: sha,
  completedWaveCount: nat.max(15), outcome: z.enum(["VICTORY", "FAILURE"]), modelSha256: sha, durableRevision: pos, tick: nat }).passthrough();
const finishBody = z.strictObject({ runId: id, encounterType: z.literal("ORDINARY_15_WAVE"), currentStage: z.literal("Complete"), currentWaveNumber: z.literal(0),
  completedWaveCount: nat.max(15), outcome: z.enum(["VICTORY", "FAILURE"]), runtimeStateJson: z.string(), tacticSelections: z.array(z.never()).length(0), clientCompletedAt: z.iso.datetime() });
const intentSchema = z.strictObject({ method: z.literal("POST"), path: z.string(), idempotencyKey: keySchema, sent: z.literal(false), body: finishBody });
const check = (condition: boolean, message: string) => { if (!condition) throw new Error(message); };

// Desktop-only guard: fail closed if process inspection fails or any Unity
// Editor remains alive. No window activation, process termination or secrets.
export async function assertUnityExited(): Promise<void> {
  const windowsRoot = process.env.SystemRoot;
  if (process.platform !== "win32" || !windowsRoot || !isAbsolute(windowsRoot)) throw new Error("ce03_unity_exit_check_windows_required");
  await new Promise<void>((done, reject) => {
    execFile(resolve(windowsRoot, "System32/WindowsPowerShell/v1.0/powershell.exe"), ["-NoProfile", "-NonInteractive", "-Command", "$ErrorActionPreference='Stop'; if (@(Get-Process | Where-Object { $_.ProcessName -eq 'Unity' }).Count -gt 0) { exit 1 }; exit 0"],
      { windowsHide: true, timeout: 10000, maxBuffer: 1024 }, error => error ? reject(new Error("ce03_unity_exit_not_confirmed")) : done());
  });
}
export async function trustedFinishCe03(options: Options) {
  check(options.dynamicResourceFileCount === 66, "ce03_finish_dynamic_resources_required");
  check(isAbsolute(options.outputDirectory), "ce03_finish_absolute_output_required");
  const origin = new URL(options.origin);
  check(origin.protocol === "http:" && ["127.0.0.1", "localhost", "[::1]"].includes(origin.hostname) && origin.origin === options.origin, "ce03_finish_loopback_required");
  const root = await realpath(options.outputDirectory), files: { name: string; path: string; raw: string }[] = [];
  const read = async (name: string, limit: number) => {
    const path = await realpath(resolve(root, name)), rel = relative(root, path), info = await stat(path);
    check(!!rel && !isAbsolute(rel) && rel !== ".." && !rel.startsWith(`..${sep}`) && info.isFile() && info.size <= limit, "ce03_finish_output_invalid");
    const raw = new TextDecoder("utf-8", { fatal: true, ignoreBOM: true }).decode(await readFile(path));
    files.push({ name, path, raw }); return raw;
  };
  await options.assertUnityExited();
  check(await options.verifyExecutionIdentity(), "ce03_finish_execution_changed");
  const original = originalSchema.parse(JSON.parse(await read("original-start.json", 8_000_000)));
  check(original.owner === options.owner && original.origin === options.origin && canonicalJson(original.prepared) === canonicalJson(options.prepared), "ce03_finish_original_binding_mismatch");
  const preparedHash = z.object({ prepareSha256: sha }).parse(options.prepared).prepareSha256;
  check(original.body.prepareSha256 === preparedHash, "ce03_finish_prepare_mismatch");
  const retained = await options.lookupOriginalStart(original.key, original.body);
  check(retained !== null && retained === original.responseRaw, "ce03_finish_retained_start_mismatch");
  const start = z.object({ accepted: z.literal(true), runId: id, admission: z.object({ loadout: z.object({ trainId: id }) }),
    combat: z.object({ seed: z.object({ value: nat.max(4294967295) }), snapshotSha256: sha,
      profile: z.object({ baseProfile: z.object({ executionEvidence: z.object({ algorithmSourceSha256: sha }) }) }) }) }).parse(JSON.parse(retained!));
  check(start.combat.profile.baseProfile.executionEvidence.algorithmSourceSha256 === sha.parse(options.sourceBundleSha256), "ce03_finish_source_binding_mismatch");
  const modelRaw = await read("model-final.json", 8_000_000), completionRaw = await read("completion.json", 65536);
  const completion = completionSchema.parse(JSON.parse(completionRaw));
  check(completion.runId === start.runId && completion.seed === start.combat.seed.value && completion.originalSnapshotSha256 === start.combat.snapshotSha256 &&
    completion.prepareSha256 === preparedHash && completion.algorithmSourceSha256 === options.sourceBundleSha256 && completion.modelSha256 === await sha256Hex(modelRaw), "ce03_finish_completion_mismatch");
  const model = z.object({ schema: z.literal("ce03-headless-state-v1"), outcome: z.enum(["Victory", "Failure"]), completedWaves: nat.max(15),
    clock: z.object({ completed: nat }), actors: z.array(z.object({ id, vitals: z.object({ hp: nat }) })).min(1) }).parse(JSON.parse(modelRaw));
  const trains = model.actors.filter(a => a.id === start.admission.loadout.trainId);
  check(new Set(model.actors.map(a => a.id)).size === model.actors.length && trains.length === 1 && model.completedWaves === completion.completedWaveCount && model.clock.completed === completion.tick &&
    (completion.outcome === "VICTORY" ? model.outcome === "Victory" && model.completedWaves === 15 && trains[0]!.vitals.hp > 0 : model.outcome === "Failure" && trains[0]!.vitals.hp === 0), "ce03_finish_nonterminal_model");
  const intent = intentSchema.parse(JSON.parse(await read("finish-intent.json", 16384)));
  check(intent.path === `/v1/runs/${start.runId}/finish` && intent.body.runId === start.runId && intent.body.outcome === completion.outcome &&
    intent.body.completedWaveCount === completion.completedWaveCount && intent.body.runtimeStateJson === completionRaw, "ce03_finish_intent_mismatch");
  await options.assertUnityExited();
  check(await options.verifyExecutionIdentity(), "ce03_finish_execution_changed");
  for (const file of files) check(await realpath(resolve(root, file.name)) === file.path &&
    createHash("sha256").update(await readFile(file.path)).digest("hex") === await sha256Hex(file.raw), "ce03_finish_output_changed");
  const result = await options.sendFinish(intent.path, intent.body, intent.idempotencyKey);
  check(result.status === 200, "ce03_finish_http_rejected");
  const response = z.object({ runId: id, rewardPreviewId: id, accepted: z.boolean(), resultStatus: z.string().optional() }).parse(JSON.parse(result.responseRaw));
  check(response.runId === start.runId && (response.accepted || response.resultStatus === "DUPLICATE_REPLAY"), "ce03_finish_response_mismatch");
  return { outputDirectory: root, runId: start.runId, rewardPreviewId: response.rewardPreviewId, key: intent.idempotencyKey,
    bodySha256: await sha256Hex(canonicalJson(intent.body)), responseRaw: result.responseRaw, completionSha256: await sha256Hex(completionRaw),
    modelSha256: completion.modelSha256, sourceBundleSha256: options.sourceBundleSha256, processRestartVerified: false as const };
}
