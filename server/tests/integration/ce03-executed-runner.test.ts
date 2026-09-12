import { describe, expect, it } from "vitest";
import { mkdtemp, mkdir, writeFile, readFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { executeCe03ConsumerRunner } from "../../scripts/local/ce03-executed-runner";
import { presentationFixture } from "../helpers/ce03-presentation-fixture";

async function fixture(mode = "pass") {
  const root = await mkdtemp(join(tmpdir(), "ce03-runner-test-")), entry = join(root, "runner.mjs");
  // Synthetic external runner checks process plumbing only, NOT consumer coverage.
  await writeFile(entry, `import {readFile,writeFile} from 'node:fs/promises';import {join} from 'node:path';
const [out,bundle,source]=process.argv.slice(2);
if(${JSON.stringify(mode)}==='fail')process.exit(9);
if(${JSON.stringify(mode)}==='timeout')await new Promise(r=>setTimeout(r,10000));
if(${JSON.stringify(mode)}==='mutate')await writeFile(source,'changed');
await writeFile(join(out,'algorithm-source.json'),await readFile(bundle));
if(${JSON.stringify(mode)}==='bundle-mismatch')await writeFile(join(out,'algorithm-source.json'),'changed bundle');
await writeFile(join(out,'checkpoint-schema.json'),'synthetic checkpoint');
await writeFile(join(out,'vectors.json'),'synthetic vectors');
if(${JSON.stringify(mode)}==='missing')process.exit(0);
await writeFile(join(out,'runner-report-v2.json'),'synthetic report');`);
  const source = join(root, "simulation.cs"); await writeFile(source, "synthetic simulation code\r\n");
  const context = { ownedSelection: { accountId: "owner-a", loadoutRevision: 7 }, carriageOwnership: { carriages: [{ type: "Gun", revision: 9 }] },
    draft: { simulationVersion: "fixture" }, commandContract: {} };
  return { root, entry, source, options: { executable: process.execPath, cwd: root, sourceRoot: root,
    sourceFiles: ["simulation.cs", "runner.mjs"], outputRoot: root, prepare: { ...context, prepareSha256: await sha256Hex(canonicalJson(context)) },
    buildArgs: (paths: { outputDirectory: string; sourceBundlePath: string }) => [entry, paths.outputDirectory, paths.sourceBundlePath, source],
    timeoutMs: mode === "timeout" ? 100 : 5000 } };
}
describe("executed runner provider plumbing (synthetic process only)", () => {
  it("includes a complete assembly up to 1024 source files without omitting files", async () => {
    const f = await fixture();
    for (let start = 0; start < 1022; start += 64) {
      const names = Array.from({ length: Math.min(64, 1022 - start) }, (_, i) => `part-${start + i}.cs`);
      await Promise.all(names.map(name => writeFile(join(f.root, name), "// source\n"))); f.options.sourceFiles.push(...names);
    }
    const result = await executeCe03ConsumerRunner(f.options);
    expect(JSON.parse(result.artifacts.algorithmSource).files).toHaveLength(1024);
    await expect(executeCe03ConsumerRunner({ ...f.options, sourceFiles: [...f.options.sourceFiles, "one-too-many.cs"] })).rejects.toThrow("ce03_runner_configuration_invalid");
  });
  it("retains the aggregate source bundle size limit even with the larger file count", async () => {
    const f = await fixture();
    await writeFile(f.source, "a".repeat(4_000_000)); await writeFile(join(f.root, "extra.cs"), "b".repeat(4_000_000));
    f.options.sourceFiles.push("extra.cs");
    await expect(executeCe03ConsumerRunner(f.options)).rejects.toThrow("ce03_runner_artifact_too_large");
  });
  it("requires a successful new execution, preserves source bytes and binds only the exact outputs", async () => {
    const f = await fixture(), result = await executeCe03ConsumerRunner(f.options);
    const a = result.artifacts;
    const bundle = JSON.parse(a.algorithmSource);
    expect(bundle.files.map((x: { path: string }) => x.path)).toEqual(["runner.mjs", "simulation.cs"]);
    expect(bundle.files[1].content).toBe("synthetic simulation code\r\n");
    const request = { ...a, reportSha256: await sha256Hex(a.reportJson), draftSha256: await sha256Hex(canonicalJson(f.options.prepare.draft)), simulationVersion: "fixture",
      algorithmSourceSha256: await sha256Hex(a.algorithmSource), vectorsSha256: await sha256Hex(a.vectorsJson), checkpointSchemaSha256: await sha256Hex(a.checkpointSchema),
      commandContractJson: canonicalJson({}), commandContractSha256: await sha256Hex(canonicalJson({})) };
    expect(await result.verifier.verify(request)).toBe(true);
    expect(await result.verifier.verify({ ...request, reportJson: "changed" })).toBe(false);
    expect(await result.verifier.verify({ ...request, commandContractJson: '{"changed":true}' })).toBe(false);
    expect(await result.verifier.verify({ ...request, draftSha256: "0".repeat(64) })).toBe(false);
    await writeFile(f.source, "edited after execution");
    expect(await result.verifier.verify(request)).toBe(false);
    expect(result.execution.exitCode).toBe(0);
  });
  it.each(["fail", "timeout", "mutate", "bundle-mismatch", "missing"])("rejects runner failure mode %s", async mode => {
    const f = await fixture(mode); await expect(executeCe03ConsumerRunner(f.options)).rejects.toThrow();
  });
  it("rejects path escape and duplicates before starting a process", async () => {
    const f = await fixture();
    await expect(executeCe03ConsumerRunner({ ...f.options, sourceFiles: ["../outside.cs"] })).rejects.toThrow();
    await expect(executeCe03ConsumerRunner({ ...f.options, sourceFiles: ["simulation.cs", "simulation.cs"] })).rejects.toThrow();
    expect(await readFile(f.source, "utf8")).toBe("synthetic simulation code\r\n");
  });
  it("rejects changed ownership under the original prepare hash", async () => {
    const f = await fixture();
    f.options.prepare.ownedSelection.accountId = "foreign-owner";
    await expect(executeCe03ConsumerRunner(f.options)).rejects.toThrow("ce03_runner_prepare_mismatch");
  });
  it("passes complete prepare bytes to a v3 process and binds presentation plus prepare identity", async () => {
    const f = await fixture();
    await writeFile(f.entry, (await readFile(f.entry, "utf8")).replace("runner-report-v2.json", "runner-report-v3.json"));
    const { prepareSha256: _old, ...context } = f.options.prepare;
    expect(_old).toHaveLength(64);
    const presentationManifest = JSON.parse(JSON.stringify(presentationFixture()));
    const names = ["ApprovedCombatView", "CombatHudWidgets", "CombatPresentationAssets", "CombatWindowView"];
    await mkdir(join(f.root, "Assets/_LASTRAIL/Scripts/Battle"), { recursive: true });
    for (let i = 0; i < names.length; i++) {
      const content = `synthetic ${names[i]}`, path = `Assets/_LASTRAIL/Scripts/Battle/${names[i]}.cs`;
      await writeFile(join(f.root, path), content);
      presentationManifest.sourceFiles[i].rawSha256 = await sha256Hex(content);
    }
    const prepare = { ...context, presentationManifest, prepareSha256: await sha256Hex(canonicalJson({ ...context, presentationManifest })) };
    await expect(executeCe03ConsumerRunner({ ...f.options, prepare })).rejects.toThrow("ce03_runner_presentation_source_mismatch");
    f.options.sourceFiles.push(...names.map(n => `Assets/_LASTRAIL/Scripts/Battle/${n}.cs`));
    let preparePath = "";
    const result = await executeCe03ConsumerRunner({ ...f.options, prepare, buildArgs: paths => { preparePath = paths.preparePath; return f.options.buildArgs(paths); } });
    expect(JSON.parse(await readFile(preparePath, "utf8"))).toEqual(prepare);
    const a = result.artifacts, request = { ...a, reportSha256: await sha256Hex(a.reportJson), draftSha256: await sha256Hex(canonicalJson(prepare.draft)), simulationVersion: "fixture",
      algorithmSourceSha256: await sha256Hex(a.algorithmSource), vectorsSha256: await sha256Hex(a.vectorsJson), checkpointSchemaSha256: await sha256Hex(a.checkpointSchema),
      commandContractJson: canonicalJson({}), commandContractSha256: await sha256Hex(canonicalJson({})), prepareSha256: prepare.prepareSha256,
      presentationManifestJson: canonicalJson(presentationManifest), presentationManifestSha256: await sha256Hex(canonicalJson(presentationManifest)) };
    expect(await result.verifier.verify(request)).toBe(true);
    expect(await result.verifier.verify({ ...request, prepareSha256: "0".repeat(64) })).toBe(false);
    expect(await result.verifier.verify({ ...request, presentationManifestJson: "{}" })).toBe(false);
  });
});
