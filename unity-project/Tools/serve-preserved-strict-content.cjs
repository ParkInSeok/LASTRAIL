const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const manifest = JSON.parse(fs.readFileSync(process.argv[2], 'utf8'));
const hash = data => 'sha256:' + crypto.createHash('sha256').update(data).digest('hex');
if (hash(fs.readFileSync(manifest.settings)) !== manifest.settingsHash) throw new Error('Preserved settings changed');
const files = new Map();
for (const [name, entry] of Object.entries(manifest.files)) {
  if (path.basename(name) !== name) throw new Error('Unsafe manifest name');
  const data = fs.readFileSync(path.join(manifest.root, name));
  if (data.length !== entry.bytes || hash(data) !== entry.hash) throw new Error('Preserved content changed: ' + name);
  files.set('/LOCAL_TEST/StandaloneWindows64/' + name, data);
}
const requests = [];
const server = http.createServer((req, res) => {
  if (req.method !== 'GET') { res.writeHead(405); res.end(); return; }
  if (req.url === '/__stats') { res.end(JSON.stringify({ requests })); return; }
  if (['/client-config.json', '/missing-config.json', '/rejected-config.json'].includes(req.url)) {
    if (req.headers['x-lastrail-app-version-code'] !== '42') { res.writeHead(400); res.end('{}'); return; }
    if (req.url === '/rejected-config.json') { res.end(JSON.stringify({ accepted: false, clientAdmission: { status: 'REJECTED', updateRequired: false } })); return; }
    res.end(JSON.stringify({ accepted: true, maintenance: { enabled: false },
      clientAdmission: { status: 'ACCEPTED', updateRequired: false, installedVersionCode: '42', minimumVersionCode: '1' },
      contentRelease: { status: 'APPROVED_COMPATIBLE', releaseId: 'preserved_strict_windows', contentVersion: 'preserved_strict_v1',
        catalogUrl: 'http://127.0.0.1:18284/LOCAL_TEST/StandaloneWindows64/' + (req.url === '/missing-config.json' ? 'missing.bin' : manifest.catalog),
        catalogHash: manifest.files[manifest.catalog].hash, compatibility: { minimumVersionCode: '1', maximumVersionCode: '100' } } })); return;
  }
  const data = files.get(req.url);
  if (!data) { res.writeHead(404); res.end(); return; }
  res.writeHead(200, { 'content-type': 'application/octet-stream', 'content-length': data.length });
  if (!req.url.endsWith('.bundle')) { res.end(data); return; }
  const record = { path: req.url, bytes: 0, aborted: false }; requests.push(record);
  let position = 0, timer;
  res.on('close', () => { clearTimeout(timer); if (!res.writableFinished) record.aborted = true; });
  function chunk() {
    if (res.destroyed) return;
    const next = Math.min(position + 65536, data.length);
    res.write(data.subarray(position, next)); record.bytes += next - position; position = next;
    if (position === data.length) res.end(); else timer = setTimeout(chunk, 5);
  }
  chunk();
});
server.listen(18284, '127.0.0.1', () => console.log('PRESERVED_STRICT_SERVER_READY files=' + files.size));
process.on('SIGINT', () => server.close(() => process.exit(0)));
