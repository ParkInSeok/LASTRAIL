import { describe, expect, it } from "vitest";
import { mkdtemp, mkdir, writeFile, readFile, copyFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { dirname, join } from "node:path";
import { createHash } from "node:crypto";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { executeCe03ConsumerRunner } from "../../scripts/local/ce03-executed-runner";
import { bindPlayerAssets } from '../../apps/game-api/src/ce03-player-assets';
import { presentationFixture } from "../helpers/ce03-presentation-fixture";
import { createCommandContract, localLeaderChargePolicy, localSupportCatalog } from "../../apps/game-api/src/ce03-command-contract";

const legacyCommands = createCommandContract({ rulesVersion: localSupportCatalog.rulesVersion,
  catalogVersion: localSupportCatalog.catalogVersion, revision: 1,
  slots: ["barrier", "support_fire", "emergency_repair"].map((skillId, slot) => ({ slot, skillId })) }, localLeaderChargePolicy);

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
    draft: { simulationVersion: "fixture" }, commandContract: legacyCommands };
  return { root, entry, source, options: { executable: process.execPath, cwd: root, sourceRoot: root,
    sourceFiles: ["simulation.cs", "runner.mjs"], outputRoot: root, prepare: { ...context, prepareSha256: await sha256Hex(canonicalJson(context)) },
    buildArgs: (paths: { outputDirectory: string; sourceBundlePath: string }) => [entry, paths.outputDirectory, paths.sourceBundlePath, source],
    timeoutMs: mode === "timeout" ? 100 : 5000 } };
}
async function dynamicFixture(mode = "pass") {
  const f = await fixture(mode), manifestPath = "QA/Ce03CommandIntegration/dynamic-resource-closure.json";
  const abilities = ["op_kyle.active", "op_kyle.ultimate", "op_reina.active", "op_reina.ultimate", "op_eve.active", "op_eve.ultimate", "op_sera.active", "op_sera.ultimate", "op_mira.active", "op_mira.ultimate", "support_fire", "barrier", "emergency_repair"];
  const keys = [...abilities.map(id => `CombatPresentation/Audio/${id}`), ...abilities.map(id => `CombatPresentation/EffectAudio/${id}-0`),
    ...["op_kyle.active-1", "op_sera.active-1", "op_sera.active-2", "op_sera.ultimate-1", "op_mira.ultimate-1"].map(id => `CombatPresentation/EffectAudio/${id}`), "CombatPresentation/Audio/levels", "CombatPresentation/EffectAudio/bindings"].sort();
  const rows = [];
  for (const [i, resourceKey] of keys.entries()) {
    const type = resourceKey.endsWith("/levels") || resourceKey.endsWith("/bindings") ? "TextAsset" : "AudioClip";
    const path = `Assets/_LASTRAIL/Resources/${resourceKey}${type === "AudioClip" ? ".wav" : ".json"}`;
    const raw = Buffer.from([0, 255, i]), meta = `guid: ${i.toString(16).padStart(32, "0")}\n`;
    await mkdir(dirname(join(f.root, path)), { recursive: true }); await writeFile(join(f.root, path), raw); await writeFile(join(f.root, path + ".meta"), meta);
    rows.push({ resourceKey, path, type, guid: i.toString(16).padStart(32, "0"), localFileId: "8300000", rawSha256: createHash("sha256").update(raw).digest("hex"), metaSha256: await sha256Hex(meta) });
  }
  await mkdir(dirname(join(f.root, manifestPath)), { recursive: true });
  const manifest = { schema: "ce03-dynamic-resource-closure-v1", unityVersion: "6000.3.22f1", rows };
  await writeFile(join(f.root, manifestPath), canonicalJson(manifest)); f.options.sourceFiles.push(manifestPath);
  const repin = async () => {
    const files = []; for (const path of [...f.options.sourceFiles].sort()) { const content = await readFile(join(f.root, path), "utf8"); files.push({ path, content, sha256: await sha256Hex(content) }); }
    return sha256Hex(canonicalJson({ schemaVersion: "ce03-source-bundle-v1", files }));
  };
  return { ...f, manifest, manifestPath, repin, resource: join(f.root, rows[0]!.path), options: { ...f.options, dynamicResourceClosurePath: manifestPath, reviewedSourceBundleSha256: await repin() } };
}

it('binds a v6 executed process provider to the reviewed Player build and prepare without granting combat proof', async () => {
  const f = await fixture(), buildRoot = join(f.root, 'build'); await mkdir(buildRoot);
  // A copied Node executable is a synthetic process fixture, NOT a Unity Player.
  const executable = join(buildRoot, 'fixture.exe'); await copyFile(process.execPath, executable);
  await writeFile(join(buildRoot, 'assets.bin'), 'original build data');
  await writeFile(f.entry, (await readFile(f.entry, 'utf8')).replace('runner-report-v2.json', 'runner-report-v6.json'));
  const manifestJson = await readFile('D:/Project/LASTRAIL/QA/OperatorInformation-20261019/BattleAssets424/manifest.json', 'utf8');
  const binding = await bindPlayerAssets(manifestJson, ['op_kyle', 'op_reina', 'op_eve', 'op_sera', 'op_mira']);
  const buildManifestPath = 'player-build.json';
  await writeFile(join(f.root, buildManifestPath), canonicalJson({ version: 'ce03.player-build.v1.20261022', files: [
    {path:'fixture.exe', sha256:createHash('sha256').update(await readFile(executable)).digest('hex')},
    {path:'assets.bin', sha256:await sha256Hex('original build data')},
  ] }));
  const sourceFiles = [...f.options.sourceFiles, buildManifestPath], files = [];
  for (const path of [...sourceFiles].sort()) { const content=await readFile(join(f.root,path),'utf8'); files.push({path,content,sha256:await sha256Hex(content)}); }
  const {prepareSha256:_old,...oldContext}=f.options.prepare; expect(_old).toHaveLength(64);
  const context={...oldContext,playerAssetBinding:binding};
  const options={...f.options,executable,sourceFiles,reviewedSourceBundleSha256:await sha256Hex(canonicalJson({schemaVersion:'ce03-source-bundle-v1',files})),
    prepare:{...context,prepareSha256:await sha256Hex(canonicalJson(context))},player:{manifestJson,buildRoot,buildManifestPath}};
  const result=await executeCe03ConsumerRunner(options),a=result.artifacts;
  const request={...a,reportSha256:await sha256Hex(a.reportJson),draftSha256:await sha256Hex(canonicalJson(context.draft)),simulationVersion:'fixture',
    algorithmSourceSha256:await sha256Hex(a.algorithmSource),vectorsSha256:await sha256Hex(a.vectorsJson),checkpointSchemaSha256:await sha256Hex(a.checkpointSchema),
    commandContractJson:canonicalJson(legacyCommands),commandContractSha256:await sha256Hex(canonicalJson(legacyCommands)),
    playerAssetManifestJson:manifestJson,playerAssetManifestSha256:binding.manifestSha256,prepareSha256:options.prepare.prepareSha256};
  expect(await result.verifier.verify(request)).toBe(true);
  options.player.manifestJson = 'caller changed options after execution';
  options.prepare.playerAssetBinding.manifestSha256 = 'b'.repeat(64);
  expect(await result.verifier.verify(request)).toBe(true);
  expect(await result.verifier.verify({...request,playerAssetManifestSha256:'0'.repeat(64)})).toBe(false);
  expect(await result.verifier.verify({...request,prepareSha256:'0'.repeat(64)})).toBe(false);
  options.player.manifestJson = manifestJson;
  options.prepare.playerAssetBinding.manifestSha256 = request.playerAssetManifestSha256;
  await writeFile(join(buildRoot,'assets.bin'),'changed');
  expect(await result.verifier.verify(request)).toBe(false);
  await expect(executeCe03ConsumerRunner(options)).rejects.toThrow();
  await expect(executeCe03ConsumerRunner({...options,reviewedSourceBundleSha256:undefined})).rejects.toThrow();
}, 30000);

it("pins dynamic raw and meta bytes before spawn, after execution and every provider verification", async () => {
  const f = await dynamicFixture(), result = await executeCe03ConsumerRunner(f.options), a = result.artifacts;
  const request = { ...a, reportSha256: await sha256Hex(a.reportJson), draftSha256: await sha256Hex(canonicalJson(f.options.prepare.draft)), simulationVersion: "fixture", algorithmSourceSha256: await sha256Hex(a.algorithmSource), vectorsSha256: await sha256Hex(a.vectorsJson), checkpointSchemaSha256: await sha256Hex(a.checkpointSchema), commandContractJson: canonicalJson(legacyCommands), commandContractSha256: await sha256Hex(canonicalJson(legacyCommands)) };
  expect(await result.verifier.verify(request)).toBe(true);
  const raw = await readFile(f.resource); await writeFile(f.resource, "changed");
  expect(await result.verifier.verify(request)).toBe(false);
  await writeFile(f.resource, raw); await writeFile(f.resource + ".meta", "changed meta");
  expect(await result.verifier.verify(request)).toBe(false);
});
it("rejects dynamic resource drift before and during the runner", async () => {
  const before = await dynamicFixture(); await writeFile(before.resource, "changed");
  await expect(executeCe03ConsumerRunner(before.options)).rejects.toThrow("ce03_runner_dynamic_resource_mismatch");
  const during = await dynamicFixture("mutate");
  await expect(executeCe03ConsumerRunner({ ...during.options, buildArgs: paths => [during.entry, paths.outputDirectory, paths.sourceBundlePath, during.resource] })).rejects.toThrow("ce03_runner_source_changed");
});
it("requires reviewed bundled canonical closure with the exact closed resource set", async () => {
  const f = await dynamicFixture();
  await expect(executeCe03ConsumerRunner({ ...f.options, dynamicResourceClosurePath: "missing.json" })).rejects.toThrow();
  const { reviewedSourceBundleSha256: _pin, ...unreviewed } = f.options;
  expect(_pin).toHaveLength(64);
  await expect(executeCe03ConsumerRunner(unreviewed)).rejects.toThrow("ce03_runner_dynamic_resource_configuration_invalid");
  f.manifest.rows.pop(); await writeFile(join(f.root, f.manifestPath), canonicalJson(f.manifest));
  await expect(executeCe03ConsumerRunner({ ...f.options, reviewedSourceBundleSha256: await f.repin() })).rejects.toThrow();
});
it.each(["path", "duplicate", "unknown", "meta"])("rejects dynamic manifest mismatch %s before spawn", async mode => {
  const f = await dynamicFixture("mutate");
  if (mode === "path") f.manifest.rows[0]!.path = "../outside.wav";
  if (mode === "duplicate") f.manifest.rows[1] = { ...f.manifest.rows[0]! };
  const manifest = mode === "unknown" ? { ...f.manifest, permit: true } : f.manifest;
  if (mode === "meta") await writeFile(f.resource + ".meta", "changed");
  await writeFile(join(f.root, f.manifestPath), canonicalJson(manifest));
  await expect(executeCe03ConsumerRunner({ ...f.options, reviewedSourceBundleSha256: await f.repin() })).rejects.toThrow();
  expect(await readFile(f.source, "utf8")).toBe("synthetic simulation code\r\n");
});
describe("executed runner provider plumbing (synthetic process only)", () => {
  it("rejects a different reviewed source pin before executing any runner", async () => {
    const f = await fixture("mutate");
    await expect(executeCe03ConsumerRunner({ ...f.options, reviewedSourceBundleSha256: "0".repeat(64) }))
      .rejects.toThrow("ce03_runner_reviewed_source_mismatch");
    expect(await readFile(f.source, "utf8")).toBe("synthetic simulation code\r\n");
  });
  it("accepts only the exact reviewed canonical bundle when a pin is supplied", async () => {
    const f = await fixture(), files = [];
    for (const path of [...f.options.sourceFiles].sort()) {
      const content = await readFile(join(f.root, path), "utf8"); files.push({ path, content, sha256: await sha256Hex(content) });
    }
    const pin = await sha256Hex(canonicalJson({ schemaVersion: "ce03-source-bundle-v1", files }));
    const result = await executeCe03ConsumerRunner({ ...f.options, reviewedSourceBundleSha256: pin });
    expect(result.execution.sourceBundleSha256).toBe(pin);
  });
  it("includes a complete embedded-package assembly up to 2048 source files without omitting files", async () => {
    const f = await fixture();
    for (let start = 0; start < 2046; start += 64) {
      const names = Array.from({ length: Math.min(64, 2046 - start) }, (_, i) => `part-${start + i}.cs`);
      await Promise.all(names.map(name => writeFile(join(f.root, name), "// source\n"))); f.options.sourceFiles.push(...names);
    }
    const result = await executeCe03ConsumerRunner(f.options);
    expect(JSON.parse(result.artifacts.algorithmSource).files).toHaveLength(2048);
    await expect(executeCe03ConsumerRunner({ ...f.options, sourceFiles: [...f.options.sourceFiles, "one-too-many.cs"] })).rejects.toThrow("ce03_runner_configuration_invalid");
  }, 30_000);
  it("retains the aggregate source bundle size limit even with the larger file count", async () => {
    const f = await fixture();
    await writeFile(f.source, "a".repeat(6_000_000)); await writeFile(join(f.root, "extra.cs"), "b".repeat(6_000_000));
    await writeFile(join(f.root, "third.cs"), "c".repeat(6_000_000));
    f.options.sourceFiles.push("extra.cs", "third.cs");
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
      commandContractJson: canonicalJson(legacyCommands), commandContractSha256: await sha256Hex(canonicalJson(legacyCommands)) };
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
      commandContractJson: canonicalJson(legacyCommands), commandContractSha256: await sha256Hex(canonicalJson(legacyCommands)), prepareSha256: prepare.prepareSha256,
      presentationManifestJson: canonicalJson(presentationManifest), presentationManifestSha256: await sha256Hex(canonicalJson(presentationManifest)) };
    expect(await result.verifier.verify(request)).toBe(true);
    expect(await result.verifier.verify({ ...request, prepareSha256: "0".repeat(64) })).toBe(false);
    expect(await result.verifier.verify({ ...request, presentationManifestJson: "{}" })).toBe(false);
  });
});
