const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const assert = require('node:assert/strict');
const work = path.resolve(process.argv[2]);
const attempt = process.argv[3];
assert.match(attempt, /^a\d{2}$/);
const slash = p => p.replaceAll('\\', '/');
const sha = p => crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const read = p => JSON.parse(fs.readFileSync(p, 'utf8'));
const config = read(path.join(work, `dev-build-input-${attempt}.json`));
const project = path.join(work, 'build-project');
const output = path.join(project, 'QA/DevContentCandidates', config.outputId);
const candidate = read(path.join(output, 'candidate.json'));
const proof = read(path.join(output, 'catalog-proof.json'));
assert.deepEqual(candidate.configuration, config);
assert.equal(candidate.status, 'LOCAL_BUILT_NOT_DEPLOYED_NOT_DEVICE_VERIFIED');
assert.equal(sha(config.sourceManifest), config.sourceManifestSha256);
assert.equal(sha(path.join(output, 'source-provenance.json')), config.sourceManifestSha256);
function files(dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap(e => {
    const p = path.join(dir, e.name);
    assert.equal(fs.lstatSync(p).isSymbolicLink(), false);
    return e.isDirectory() ? files(p) : [p];
  });
}
function verifySource(root, manifest) {
  const actual = ['Assets', 'Packages', 'ProjectSettings', 'Tools'].flatMap(r => files(path.join(root, r)))
    .map(p => slash(path.relative(root, p))).sort();
  assert.deepEqual(actual, manifest.files.map(f => f.path).sort());
  for (const f of manifest.files) {
    const p = path.join(root, f.path);
    assert.equal(fs.statSync(p).size, f.length, f.path);
    assert.equal(sha(p), f.sha256, f.path);
  }
}
verifySource(project, read(config.sourceManifest));
const original = read(path.join(work, 'source-original-manifest.json'));
verifySource(path.join(work, 'source-original'), original);
verifySource('E:/Projects/LASTRAIL/unity-project', original);
assert.equal(proof.status, 'ACTUAL_BUILT_BINARY_CATALOG_CHECKED_NOT_DEVICE_VERIFIED');
assert.equal(proof.mainLocationCount, 1);
assert.equal(proof.lobbyLocationCount, 1);
assert.equal(proof.mainSceneKey, 'Assets/_LASTRAIL/Scenes/Main.unity');
// AddressableAssetEntry.CreateCatalogEntries retains the group asset provider
// for scene entries; AddressablesImpl.LoadSceneAsync uses its separate SceneProvider.
assert.equal(proof.mainProvider, 'UnityEngine.ResourceManagement.ResourceProviders.BundledAssetProvider');
assert(proof.mainBundleUrls.length > 0);
const bundleUrls = new Set(candidate.bundles.map(b => b.url));
for (const url of [...proof.requiredBundleUrls, ...proof.mainBundleUrls]) assert(bundleUrls.has(url));
for (const url of proof.mainBundleUrls) assert(proof.requiredBundleUrls.includes(url));
const remote = path.join(output, 'remote');
const names = fs.readdirSync(remote).sort();
const catalogs = names.filter(n => /^catalog.*\.bin$/.test(n));
const hashes = names.filter(n => /^catalog.*\.hash$/.test(n));
assert.equal(catalogs.length, 1);
assert.equal(hashes.length, 1);
assert.deepEqual(names, [...candidate.bundles.map(b => b.file), ...catalogs, ...hashes].sort());
assert.equal(candidate.catalogUrl, config.releaseNamespace + catalogs[0]);
assert.equal(candidate.catalogHash, 'sha256:' + sha(path.join(remote, catalogs[0])));
assert.match(fs.readFileSync(path.join(remote, hashes[0]), 'utf8').trim(), /^[a-f0-9]{32}$/i);
const publicFiles = names.map(name => {
  assert.match(name, /^[A-Za-z0-9_.-]+$/);
  const p = path.join(remote, name);
  const item = { file: name, localPath: slash(p), bytes: fs.statSync(p).size, sha256: sha(p), url: config.releaseNamespace + name };
  const bundle = candidate.bundles.find(b => b.file === name);
  if (bundle) {
    for (const key of ['bytes', 'sha256', 'url']) assert.equal(item[key], bundle[key]);
    assert.match(bundle.hash, /^[a-f0-9]{32}$/i);
    assert(Number.isInteger(bundle.crc) && bundle.crc >= 0 && bundle.crc <= 0xffffffff);
    item.hash128 = bundle.hash; item.crc = bundle.crc;
  }
  return item;
});
const packet = { status: 'LOCAL_ACTUAL_ANDROID_CONTENT_VERIFIED_AWAITING_SERVER_PUBLICATION',
  createdAt: new Date().toISOString(), releaseId: config.releaseId, releaseNamespace: config.releaseNamespace,
  candidatePath: slash(path.join(output, 'candidate.json')), catalogProofPath: slash(path.join(output, 'catalog-proof.json')),
  sourceCommit: config.sourceCommit, sourceManifest: config.sourceManifest, sourceManifestSha256: config.sourceManifestSha256,
  originalSnapshotAndOriginalSourceRehashed: true, configuration: config,
  compatibility: 'INTENDED_APP_0.1.0_CODE_1_ONLY_NOT_APK_OR_DEVICE_VERIFIED',
  publicationOwner: 'LAST RAIL_SERVER_1',
  sceneCatalogSemantics: 'Main resolved as SceneInstance exactly once; catalog group provider is BundledAssetProvider; runtime LoadSceneAsync uses separate SceneProvider',
  publicFiles, publicBytes: publicFiles.reduce((s, f) => s + f.bytes, 0),
  excludedFromUpload: ['source', 'logs', 'state', 'archives', 'candidate metadata', 'provenance', 'catalog proof'],
  remotePublished: false, deviceVerified: false };
const target = path.join(work, `artifact-packet-${attempt}.json`);
fs.writeFileSync(target, JSON.stringify(packet, null, 2) + '\n', { flag: 'wx' });
console.log(JSON.stringify({ packet: slash(target), sha256: sha(target), releaseId: config.releaseId,
  files: publicFiles.length, bytes: packet.publicBytes, bundles: candidate.bundles.length,
  mainBundleCount: proof.mainBundleUrls.length, requiredBundleCount: proof.requiredBundleUrls.length }));
