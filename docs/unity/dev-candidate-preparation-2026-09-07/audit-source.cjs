// Local observation only. Never modifies Assets, settings, archives or Git state.
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const cp = require('node:child_process');
const project = path.resolve(__dirname, '../../../unity-project');
const repo = path.dirname(project);
const out = process.env.LASTRAIL_AUDIT_OUT ? path.resolve(process.env.LASTRAIL_AUDIT_OUT) : __dirname;
if (process.env.LASTRAIL_AUDIT_OUT) fs.mkdirSync(out, { recursive: false });
const slash = p => p.replaceAll('\\', '/');
const rel = p => slash(path.relative(project, p));
const hash = b => crypto.createHash('sha256').update(b).digest('hex');
const read = p => fs.readFileSync(path.join(project, p), 'utf8');
const git = args => cp.execFileSync('git', args, { cwd: repo, encoding: 'utf8', maxBuffer: 16 * 1024 * 1024 }).trimEnd();
function walk(dir) {
  if (!fs.existsSync(dir)) throw new Error(`Missing input root: ${dir}`);
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap(e => {
    const p = path.join(dir, e.name);
    if (fs.lstatSync(p).isSymbolicLink()) throw new Error(`Reparse/symlink input requires review: ${p}`);
    return e.isDirectory() ? walk(p) : [p];
  }).sort();
}
const roots = ['Assets', 'Packages', 'ProjectSettings', 'Tools'];
const all = roots.flatMap(r => walk(path.join(project, r)));
const tracked = new Set(git(['ls-files', '-z', '--', ...roots.map(r => `unity-project/${r}`)]).split('\0'));
const changes = new Map();
const status = git(['status', '--porcelain=v1', '-z', '--untracked-files=all', '--', ...roots.map(r => `unity-project/${r}`)]).split('\0');
for (let i = 0; i < status.length; i++) {
  const line = status[i]; if (!line) continue;
  changes.set(line.slice(3), line.slice(0, 2));
  if (/[RC]/.test(line.slice(0, 2))) i++;
}
const excluded = p => /(^|\/)(qa|tests|editor|docs|sources|showcase|generateddelta)(\/|$)/i.test(p)
  || /showcase|contact_sheet/i.test(p) || (p.endsWith('.unity') && p !== 'Assets/_LASTRAIL/Scenes/Main.unity');
const guids = new Map();
for (const p of all.filter(p => p.endsWith('.meta'))) {
  const m = /^guid: ([a-f0-9]{32})\s*$/mi.exec(fs.readFileSync(p, 'utf8'));
  if (m) { const list = guids.get(m[1].toLowerCase()) || []; list.push(rel(p.slice(0, -5))); guids.set(m[1].toLowerCase(), list); }
}
const resolve = g => (guids.get(g) || []).length === 1 ? guids.get(g)[0] : null;
const guidRefs = text => [...text.matchAll(/\bguid:\s*([a-f0-9]{32})\b/gi)].map(m => m[1].toLowerCase());
// Schema-specific extraction of the flat Unity serialized group list, not a YAML parser.
function section(text, name) {
  const lines = text.split(/\r?\n/); const at = lines.findIndex(l => l.startsWith(`  ${name}:`));
  if (at < 0) throw new Error(`Missing serialized section ${name}`);
  let end = at + 1; while (end < lines.length && !/^  [A-Za-z_][A-Za-z0-9_]*:/.test(lines[end])) end++;
  return lines.slice(at, end).join('\n');
}
function scalar(value) {
  if (value.startsWith('"')) return JSON.parse(value);
  if (value.startsWith("'")) return value.slice(1, -1).replaceAll("''", "'");
  if (/^[\[{&*!]/.test(value)) throw new Error('Unsupported serialized scalar');
  return value;
}
const settingsPath = 'Assets/AddressableAssetsData/AddressableAssetSettings.asset';
const settingsText = read(settingsPath);
const groupRefs = guidRefs(section(settingsText, 'm_GroupAssets'));
const groups = groupRefs.map(guid => {
  const file = resolve(guid);
  if (!file) return { guid, path: null, error: 'unresolved_or_duplicate_group_guid' };
  const text = read(file); const body = section(text, 'm_SerializeEntries');
  const chunks = body.split(/^  - m_GUID: /m).slice(1);
  const entries = chunks.map(chunk => {
    const id = chunk.slice(0, 32).toLowerCase();
    if (!/^[a-f0-9]{32}$/.test(id)) throw new Error(`Unsupported entry in ${file}`);
    const address = /^    m_Address: (.*)$/m.exec(chunk);
    if (!address) throw new Error(`Entry address missing in ${file}`);
    const asset = resolve(id); const p = asset && path.join(project, asset);
    return { guid: id, address: scalar(address[1]), path: asset,
      labels: [...chunk.matchAll(/^    - (.*)$/gm)].map(m => scalar(m[1])),
      excluded: asset ? excluded(asset) : null,
      missing: !p || !fs.existsSync(p), folder: !!p && fs.existsSync(p) && fs.statSync(p).isDirectory() };
  });
  const schemas = guidRefs(section(text, 'm_SchemaSet')).map(resolve).filter(Boolean).map(p => ({ path: p, sha256: hash(read(p)) }));
  return { guid, path: file, name: /^  m_GroupName: (.*)$/m.exec(text)?.[1],
    sourceSettingsGuid: guidRefs(section(text, 'm_Settings'))[0], excluded: excluded(file), schemas, entries };
});
const serialized = new Set(['.prefab', '.unity', '.asset', '.mat', '.controller', '.overridecontroller', '.anim', '.spriteatlas', '.playable']);
const cache = new Map();
function references(p) {
  if (cache.has(p)) return cache.get(p);
  const texts = []; const full = path.join(project, p);
  if (serialized.has(path.extname(p).toLowerCase()) && fs.existsSync(full)) {
    const b = fs.readFileSync(full);
    if (b.subarray(0, 100).toString().includes('%YAML')) texts.push(b.toString('utf8'));
  }
  if (fs.existsSync(full + '.meta')) texts.push(fs.readFileSync(full + '.meta', 'utf8'));
  const result = [...new Set(texts.flatMap(guidRefs))].filter(g => !/^0+$/.test(g)); cache.set(p, result); return result;
}
const findings = [], unresolved = new Map();
for (const group of groups.filter(g => g.entries)) {
  const seen = new Set(); const queue = group.entries.filter(e => e.path && !e.folder).map(e => ({ p: e.path, chain: [e.path] }));
  while (queue.length) {
    const { p, chain } = queue.shift(); if (seen.has(p)) continue; seen.add(p);
    if (excluded(p)) findings.push({ group: group.name, groupPath: group.path, path: p, chain });
    for (const g of references(p)) {
      const next = resolve(g);
      if (!next) { if (!unresolved.has(g)) unresolved.set(g, p); continue; }
      if (next !== p && !seen.has(next) && fs.existsSync(path.join(project, next)) && !fs.statSync(path.join(project, next)).isDirectory())
        queue.push({ p: next, chain: [...chain, next] });
    }
  }
  group.staticReachableCount = seen.size;
}
const files = all.map(p => {
  const before = fs.statSync(p), bytes = fs.readFileSync(p), after = fs.statSync(p), relative = rel(p);
  return { path: relative, length: bytes.length, sha256: hash(bytes), gitStatus: changes.get(`unity-project/${relative}`) || (tracked.has(`unity-project/${relative}`) ? 'tracked-clean' : 'untracked-or-ignored'),
    sourceDecision: 'preserve_in_reviewed_source_snapshot',
    payloadDecision: relative.startsWith('Assets/') && !excluded(relative) && !relative.endsWith('.meta') ? 'requires_shipping_dependency_selection' : 'not_direct_shipping_payload',
    changedDuringRead: before.size !== after.size || before.mtimeMs !== after.mtimeMs };
});
const common = { status: 'OBSERVATION_ONLY_NOT_FROZEN_NOT_APPROVED_CANDIDATE', observedAt: new Date().toISOString(), project,
  observedHead: git(['rev-parse', 'HEAD']), limitations: ['No Unity AssetDatabase/build execution.', 'Static serialized GUID references only; code/dynamic/native importer dependencies are not complete.',
    'Unresolved GUIDs can be packages/builtins/missing references; not automatically missing assets.', 'Hashes are per-file observations, not a simultaneous source freeze.', 'Source include does not mean ship in bundles.'] };
function save(name, value) { fs.writeFileSync(path.join(out, name), JSON.stringify(value, null, 2) + '\n', { flag: 'wx' }); }
save('source-inclusion-observation.json', { ...common, roots, files, deletedOrAbsentChangedPaths: [...changes.keys()].filter(p => !fs.existsSync(path.join(repo, p))) });
save('addressables-membership-observation.json', { ...common, settingsPath, settingsSha256: hash(settingsText), groups, excludedReferencePaths: findings,
  duplicateAssetGuids: [...guids].filter(([, p]) => p.length > 1).map(([guid, paths]) => ({ guid, paths })),
  unresolvedReferences: [...unresolved].map(([guid, referencedBy]) => ({ guid, referencedBy })) });
console.log(JSON.stringify({ files: files.length, bytes: files.reduce((s, f) => s + f.length, 0), changedDuringRead: files.filter(f => f.changedDuringRead).length,
  gitStates: files.reduce((m, f) => (m[f.gitStatus] = (m[f.gitStatus] || 0) + 1, m), {}),
  groups: groups.map(g => ({ name: g.name, path: g.path, entries: g.entries?.length, excluded: g.excluded, reachable: g.staticReachableCount })),
  excludedPaths: findings, unresolved: unresolved.size }, null, 2));
