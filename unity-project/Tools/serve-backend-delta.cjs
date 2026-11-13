const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const root = path.resolve(process.argv[2]);
const fixture = JSON.parse(fs.readFileSync(path.join(root, 'fixture.json'), 'utf8'));
if (fixture.platform !== 'StandaloneWindows64') throw new Error('Win64 fixtures required');
const prefix = '/LOCAL_TEST/StandaloneWindows64/';
const files = new Map(), catalogs = {};
for (const [label, archive, entry] of [['A', 'full_A', fixture.old], ['B', 'update_B', fixture.update]]) {
  if (path.basename(entry.file) !== entry.file) throw new Error('Unsafe bundle name');
  const bundle = fs.readFileSync(path.join(root, 'bundles', entry.file));
  if (digest(bundle) !== entry.sha256) throw new Error('Bundle hash mismatch');
  files.set(prefix + entry.file, { data: bundle, bundle: true });
  const dir = path.join(root, 'archives', archive);
  const manifest = JSON.parse(fs.readFileSync(path.join(dir, 'manifest.json'), 'utf8'));
  const entries = manifest.files.filter(e => /^remote\/catalog.*\.bin$/.test(e.path));
  if (entries.length !== 1) throw new Error('Expected one catalog');
  const data = fs.readFileSync(path.join(dir, entries[0].path));
  if (digest(data) !== entries[0].sha256) throw new Error('Catalog hash mismatch');
  catalogs[label] = { data, hash: entries[0].sha256 };
  files.set(prefix + label + '.bin', { data, bundle: false });
}
function digest(data) { return crypto.createHash('sha256').update(data).digest('hex'); }
let selected = 'A', bundleRequests = 0, bundleBytes = 0, catalogRequests = 0, configRequests = 0;
const server = http.createServer((req, res) => {
  if (req.method === 'POST' && /^\/__qa\/select\/(A|B|badB)$/.test(req.url)) {
    selected = req.url.split('/').pop(); res.end('{}'); return;
  }
  if (req.method !== 'GET') { res.writeHead(405); res.end(); return; }
  if (req.url === '/stats') { res.end(JSON.stringify({ selected, bundleRequests, bundleBytes, catalogRequests, configRequests })); return; }
  if (req.url === '/client-config.json') {
    configRequests++;
    if (req.headers['x-lastrail-app-version-code'] !== '42') { res.writeHead(400); res.end('{}'); return; }
    const release = selected === 'A' ? 'A' : 'B';
    res.setHeader('content-type', 'application/json');
    res.end(JSON.stringify({ accepted: true, clientAdmission: { status: 'ACCEPTED', updateRequired: false,
      installedVersionCode: '42', minimumVersionCode: '1' }, maintenance: { enabled: false },
      contentRelease: { status: 'APPROVED_COMPATIBLE', releaseId: 'synthetic_' + release, contentVersion: 'synthetic_' + release,
        catalogUrl: 'http://127.0.0.1:18286' + prefix + release + '.bin',
        catalogHash: 'sha256:' + (selected === 'badB' ? '0'.repeat(64) : catalogs[release].hash),
        compatibility: { minimumVersionCode: '1', maximumVersionCode: '100' } } })); return;
  }
  const file = files.get(req.url);
  if (!file) { res.writeHead(404); res.end(); return; }
  if (file.bundle) { bundleRequests++; bundleBytes += file.data.length; } else catalogRequests++;
  res.writeHead(200, { 'content-type': 'application/octet-stream', 'content-length': file.data.length }); res.end(file.data);
});
server.listen(18286, '127.0.0.1', () => console.log('BACKEND_DELTA_SERVER_READY ' + root));
process.on('SIGINT', () => server.close(() => process.exit(0)));
