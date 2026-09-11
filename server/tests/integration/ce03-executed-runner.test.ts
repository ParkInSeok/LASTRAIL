import { describe, expect, it } from "vitest";
import { mkdtemp, writeFile, readFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { executeCe03ConsumerRunner } from "../../scripts/local/ce03-executed-runner";

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
  return { root, entry, source, options: { executable: process.execPath, cwd: root, sourceRoot: root,
    sourceFiles: ["simulation.cs", "runner.mjs"], outputRoot: root, prepare: { draft: { simulationVersion: "fixture" }, commandContract: {} },
    buildArgs: (paths: { outputDirectory: string; sourceBundlePath: string }) => [entry, paths.outputDirectory, paths.sourceBundlePath, source],
    timeoutMs: mode === "timeout" ? 100 : 5000 } };
}
describe("executed runner provider plumbing (synthetic process only)", () => {
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
});
