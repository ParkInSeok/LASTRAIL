import { spawn } from "node:child_process";
import { createHash } from "node:crypto";
import { createReadStream } from "node:fs";
import { mkdir, mkdtemp, readFile, realpath, stat, writeFile } from "node:fs/promises";
import { isAbsolute, join, relative, resolve, sep } from "node:path";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import type { ConsumerAttestationVerifier } from "../../apps/game-api/src/ce03-consumer-attestation";
import { presentationSourcePaths, validatePresentationManifest } from "../../apps/game-api/src/ce03-presentation-manifest";

type Json = Parameters<typeof canonicalJson>[0];
export type Ce03RunnerPaths = { outputDirectory: string; sourceBundlePath: string; preparePath: string };
type Options = { executable: string; cwd: string; sourceRoot: string; sourceFiles: readonly string[]; outputRoot: string;
  prepare: { ownedSelection: Json; carriageOwnership: Json; draft: Json; commandContract: Json; prepareSha256: string; presentationManifest?: Json };
  buildArgs(paths: Ce03RunnerPaths): string[]; timeoutMs?: number; reviewedSourceBundleSha256?: string };
async function fileHash(path: string) {
  const hash = createHash("sha256"); for await (const chunk of createReadStream(path)) hash.update(chunk);
  return hash.digest("hex");
}
function inside(root: string, path: string) {
  const rel = relative(root, path);
  if (!rel || isAbsolute(rel) || rel === ".." || rel.startsWith(`..${sep}`)) throw new Error("ce03_runner_path_invalid");
}
async function textFile(path: string, maxBytes: number) {
  if ((await stat(path)).size > maxBytes) throw new Error("ce03_runner_artifact_too_large");
  return new TextDecoder("utf-8", { fatal: true, ignoreBOM: true }).decode(await readFile(path));
}

/** Trusted local process adapter, never an HTTP endpoint or artifact-only trust
 * loader. Caller must review the runner and complete dependency list. Running a
 * program proves execution identity, not the sufficiency of its assertions. The
 * host still requires full strict report-v2 and actual consumer coverage. */
export async function executeCe03ConsumerRunner(options: Options) {
  if (!isAbsolute(options.executable) || options.sourceFiles.length < 1 || options.sourceFiles.length > 1024 ||
      new Set(options.sourceFiles.map(s => s.toLowerCase())).size !== options.sourceFiles.length) throw new Error("ce03_runner_configuration_invalid");
  const root = await realpath(options.sourceRoot), executable = await realpath(options.executable);
  const executableSha256 = await fileHash(executable);
  const files: { path: string; sha256: string; content: string }[] = [];
  const actualPaths: string[] = [];
  for (const name of [...options.sourceFiles].sort()) {
    if (isAbsolute(name) || name.includes("\\") || name.split("/").some(p => p === ".." || p === "." || p === "")) throw new Error("ce03_runner_path_invalid");
    const path = await realpath(resolve(root, name)); inside(root, path);
    if (actualPaths.some(p => p.toLowerCase() === path.toLowerCase())) throw new Error("ce03_runner_path_duplicate");
    const content = await textFile(path, 8_000_000);
    actualPaths.push(path); files.push({ path: name, sha256: await sha256Hex(content), content });
  }
  const algorithmSource = canonicalJson({ schemaVersion: "ce03-source-bundle-v1", files });
  if (algorithmSource.length > 8_000_000) throw new Error("ce03_runner_artifact_too_large");
  if (options.reviewedSourceBundleSha256 !== undefined &&
      (!/^[a-f0-9]{64}$/u.test(options.reviewedSourceBundleSha256) || await sha256Hex(algorithmSource) !== options.reviewedSourceBundleSha256))
    throw new Error("ce03_runner_reviewed_source_mismatch");
  const sourceUnchanged = async () => {
    for (let i = 0; i < actualPaths.length; i++) {
      const current = await realpath(resolve(root, files[i]!.path)); inside(root, current);
      if (current !== actualPaths[i] || await fileHash(current) !== files[i]!.sha256) return false;
    }
    return await fileHash(executable) === executableSha256;
  };
  // All identity inputs are snapshotted before launching, with no credentials.
  const { ownedSelection, carriageOwnership, draft, commandContract, presentationManifest, prepareSha256 } = options.prepare;
  if (ownedSelection === undefined || carriageOwnership === undefined || draft === undefined || commandContract === undefined)
    throw new Error("ce03_runner_prepare_invalid");
  const context = { ownedSelection, carriageOwnership, draft, commandContract, ...(presentationManifest === undefined ? {} : { presentationManifest }) };
  if (await sha256Hex(canonicalJson(context)) !== prepareSha256) throw new Error("ce03_runner_prepare_mismatch");
  const prepareJson = canonicalJson({ ...context, prepareSha256 }), draftSha256 = await sha256Hex(canonicalJson(draft));
  const commandContractJson = canonicalJson(commandContract), commandContractSha256 = await sha256Hex(commandContractJson);
  const presentationManifestJson = presentationManifest === undefined ? undefined : canonicalJson(presentationManifest);
  const presentationManifestSha256 = presentationManifestJson === undefined ? undefined : await sha256Hex(presentationManifestJson);
  if (presentationManifest !== undefined) {
    const manifest = validatePresentationManifest(presentationManifest);
    if (manifest.sourceFiles.some(s => !files.some(f => f.path === presentationSourcePaths[s.logicalName] && f.sha256 === s.rawSha256)))
      throw new Error("ce03_runner_presentation_source_mismatch");
  }
  const simulationVersion = (JSON.parse(prepareJson).draft as { simulationVersion?: string }).simulationVersion;
  await mkdir(options.outputRoot, { recursive: true });
  const directory = await mkdtemp(join(options.outputRoot, "ce03-execution-"));
  const outputDirectory = join(directory, "output"); await mkdir(outputDirectory);
  const paths = { outputDirectory, sourceBundlePath: join(directory, "source-input.json"), preparePath: join(directory, "prepare-input.json") };
  await writeFile(paths.sourceBundlePath, algorithmSource); await writeFile(paths.preparePath, prepareJson);
  const args = options.buildArgs(Object.freeze(paths));
  if (!args.every(a => typeof a === "string")) throw new Error("ce03_runner_arguments_invalid");
  const timeoutMs = options.timeoutMs ?? 600000;
  if (!Number.isSafeInteger(timeoutMs) || timeoutMs < 1 || timeoutMs > 1800000) throw new Error("ce03_runner_timeout_invalid");
  const startedAt = Date.now();
  await new Promise<void>((resolveRun, reject) => {
    const child = spawn(executable, args, { cwd: options.cwd, shell: false, windowsHide: true, stdio: "ignore" });
    let timedOut = false;
    const timer = setTimeout(() => { timedOut = true; child.kill(); }, timeoutMs);
    child.once("error", () => { clearTimeout(timer); reject(new Error("ce03_runner_launch_failed")); });
    child.once("close", code => {
      clearTimeout(timer);
      if (timedOut || code !== 0) reject(new Error(timedOut ? "ce03_runner_timeout" : "ce03_runner_failed")); else resolveRun();
    });
  });
  if (!await sourceUnchanged()) throw new Error("ce03_runner_source_changed");
  const output = async (name: string, max: number) => {
    const path = await realpath(join(outputDirectory, name)); inside(outputDirectory, path); return textFile(path, max);
  };
  const artifacts = Object.freeze({ algorithmSource: await output("algorithm-source.json", 32_000_000),
    checkpointSchema: await output("checkpoint-schema.json", 8_000_000), vectorsJson: await output("vectors.json", 8_000_000),
    reportJson: await output(presentationManifest === undefined ? "runner-report-v2.json" : "runner-report-v3.json", 8_000_000) });
  if (artifacts.algorithmSource !== algorithmSource) throw new Error("ce03_runner_bundle_mismatch");
  const hashes = { algorithmSourceSha256: await sha256Hex(artifacts.algorithmSource), checkpointSchemaSha256: await sha256Hex(artifacts.checkpointSchema),
    vectorsSha256: await sha256Hex(artifacts.vectorsJson), reportSha256: await sha256Hex(artifacts.reportJson) };
  const verifier: ConsumerAttestationVerifier = { verify: async request => {
    try {
      return request.algorithmSource === artifacts.algorithmSource && request.checkpointSchema === artifacts.checkpointSchema &&
        request.vectorsJson === artifacts.vectorsJson && request.reportJson === artifacts.reportJson &&
        request.draftSha256 === draftSha256 && request.simulationVersion === simulationVersion &&
        request.commandContractJson === commandContractJson && request.commandContractSha256 === commandContractSha256 &&
        request.presentationManifestJson === presentationManifestJson && request.presentationManifestSha256 === presentationManifestSha256 &&
        request.prepareSha256 === (presentationManifest === undefined ? undefined : prepareSha256) &&
        request.algorithmSourceSha256 === hashes.algorithmSourceSha256 && request.checkpointSchemaSha256 === hashes.checkpointSchemaSha256 &&
        request.vectorsSha256 === hashes.vectorsSha256 && request.reportSha256 === hashes.reportSha256 && await sourceUnchanged();
    } catch { return false; }
  } };
  return { artifacts, verifier: Object.freeze(verifier), execution: Object.freeze({ executableSha256, sourceBundleSha256: hashes.algorithmSourceSha256,
    outputDirectory, startedAt, finishedAt: Date.now(), exitCode: 0 }) };
}
