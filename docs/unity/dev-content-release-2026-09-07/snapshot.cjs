const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const cp = require('node:child_process');
const repo = 'E:/Projects/LASTRAIL';
const source = path.join(repo, 'unity-project');
const roots = ['Assets', 'Packages', 'ProjectSettings', 'Tools'];
const slash = p => p.replaceAll('\\', '/');
const digest = p => crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
function files(dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap(entry => {
    const p = path.join(dir, entry.name);
    if (fs.lstatSync(p).isSymbolicLink()) throw Error(`Review required for linked input: ${p}`);
    return entry.isDirectory() ? files(p) : [p];
  }).sort();
}
function manifest(project) {
  return roots.flatMap(root => files(path.join(project, root))).map(p => ({
    path: slash(path.relative(project, p)), length: fs.statSync(p).size, sha256: digest(p)
  }));
}
function writeNew(p, value) { fs.writeFileSync(p, JSON.stringify(value, null, 2) + '\n', { flag: 'wx' }); }
function same(expected, actual) { return JSON.stringify(expected) === JSON.stringify(actual); }
const mode = process.argv[2];
if (mode === 'create') {
  const sourceFiles = manifest(source);
  const privatePaths = sourceFiles.filter(f => /(^|\/)(\.env(?:\..*)?|[^/]+\.(pfx|p12|jks|keystore|pem|key))$/i.test(f.path));
  if (privatePaths.length) throw Error('Private-looking input requires review before copying: ' + privatePaths.map(f => f.path).join(', '));
  const commitSha = cp.execFileSync('git', ['rev-parse', 'HEAD'], { cwd: repo, encoding: 'utf8' }).trim();
  const id = 'dev-main-' + new Date().toISOString().replace(/[-:.TZ]/g, '').slice(0, 14) + '-' + crypto.randomBytes(6).toString('hex');
  const work = path.join(source, 'QA', 'DevContentWork', id);
  fs.mkdirSync(work, { recursive: true });
  fs.writeFileSync(path.join(work, 'snapshot-owner.json'), JSON.stringify({ source: slash(source), work: slash(work), commitSha, id }), { flag: 'wx' });
  for (const destination of ['source-original', 'build-project']) {
    for (const file of sourceFiles) {
      const src = path.join(destination === 'source-original' ? source : path.join(work, 'source-original'), file.path);
      const target = path.join(work, destination, file.path);
      fs.mkdirSync(path.dirname(target), { recursive: true });
      fs.copyFileSync(src, target, fs.constants.COPYFILE_EXCL);
    }
    if (!same(sourceFiles, manifest(path.join(work, destination)))) throw Error('Copied source failed byte verification. Retain failed work directory.');
  }
  if (!same(sourceFiles, manifest(source))) throw Error('Source changed during copy. Retain failed work directory.');
  writeNew(path.join(work, 'source-original-manifest.json'), { commitSha, files: sourceFiles });
  writeNew(path.join(work, 'snapshot-receipt.json'), { status: 'COPIED_AND_REHASHED_NOT_A_PURE_COMMIT_SNAPSHOT',
    createdAt: new Date().toISOString(), work: slash(work), source: slash(source), observedCommit: commitSha,
    originalManifestSha256: digest(path.join(work, 'source-original-manifest.json')),
    count: sourceFiles.length, bytes: sourceFiles.reduce((sum, f) => sum + f.length, 0),
    transformations: 'None yet. QA source files preserved in both copies; only settings membership will be changed in build-project.' });
  console.log(JSON.stringify({ work: slash(work), commitSha, files: sourceFiles.length, bytes: sourceFiles.reduce((s, f) => s + f.length, 0) }));
} else if (mode === 'freeze' || mode === 'review') {
  const work = path.resolve(process.argv[3]);
  const root = path.resolve(source, 'QA/DevContentWork') + path.sep;
  if (!work.startsWith(root)) throw Error('Workspace escapes approved output root.');
  const original = JSON.parse(fs.readFileSync(path.join(work, 'source-original-manifest.json'), 'utf8'));
  const attempt = process.argv[4] || 'a01';
  if (!/^a[0-9]{2}$/.test(attempt)) throw Error('Explicit numbered attempt required.');
  if (!same(original.files, manifest(path.join(work, 'source-original')))) throw Error('Original snapshot changed.');
  const current = manifest(path.join(work, 'build-project'));
  const prior = new Map(original.files.map(f => [f.path, f]));
  const now = new Map(current.map(f => [f.path, f]));
  const changes = [...new Set([...prior.keys(), ...now.keys()])].filter(p => JSON.stringify(prior.get(p)) !== JSON.stringify(now.get(p)))
    .map(p => ({ path: p, before: prior.get(p) || null, after: now.get(p) || null }));
  if (mode === 'review') { console.log(JSON.stringify({ files: current.length, changes }, null, 2)); process.exit(0); }
  const buildProject = path.join(work, 'build-project');
  const commitSha = cp.execFileSync('git', ['rev-parse', 'HEAD'], { cwd: buildProject, encoding: 'utf8' }).trim();
  const dirty = cp.execFileSync('git', ['status', '--porcelain=v1', '--untracked-files=all', '--', ...roots], { cwd: buildProject, encoding: 'utf8' }).trim();
  if (dirty) throw Error('Commit the exact prepared input roots before freezing.');
  const sourceManifest = path.join(work, `build-source-manifest-${attempt}.json`);
  writeNew(sourceManifest, { commitSha, files: current });
  writeNew(path.join(work, `source-transformations-${attempt}.json`), { status: 'REQUIRES_REVIEW_BEFORE_BUILD', originalObservedCommit: original.commitSha, buildSourceCommit: commitSha, changes });
  const sourceManifestSha256 = digest(sourceManifest);
  const releaseId = 'android-main-20260907-' + sourceManifestSha256.slice(0, 16) + '-' + path.basename(work).split('-').pop() + '-' + attempt;
  const trustedCatalogRoot = 'https://dev-content.lastrail.app/android/releases/';
  const input = { environment: 'DEV', target: 'Android', trustedCatalogRoot,
    releaseNamespace: trustedCatalogRoot + releaseId + '/', releaseId,
    contentVersion: 'android-main-' + sourceManifestSha256.slice(0, 16), appVersion: '0.1.0', versionCode: 1,
    minimumVersionCode: 1, maximumVersionCode: 1, sourceCommit: commitSha,
    sourceManifest: slash(sourceManifest), sourceManifestSha256, outputId: attempt };
  const inputPath = path.join(work, `dev-build-input-${attempt}.json`);
  writeNew(inputPath, input);
  console.log(JSON.stringify({ work: slash(work), input: slash(inputPath), releaseId, sourceCommit: commitSha,
    manifestSha256: sourceManifestSha256, changes: changes.map(c => c.path), files: current.length,
    compatibility: 'INTENDED_SOURCE_APP_0.1.0_CODE_1_ONLY_NOT_APK_OR_DEVICE_VERIFIED', remoteNamespace: 'SERVER_MUST_CONFIRM_UNUSED' }));
} else throw Error('Use create or freeze <work>.');
