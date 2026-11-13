const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const root = path.resolve(process.argv[2]);
const fixture = JSON.parse(fs.readFileSync(path.join(root, 'fixture.json'), 'utf8'));
if (fixture.platform !== 'StandaloneWindows64') throw new Error('Windows fixtures required');
const files = new Map();
for (const entry of [fixture.old, fixture.update]) {
  if (path.basename(entry.file) !== entry.file) throw new Error('Unsafe fixture filename');
  const bytes = fs.readFileSync(path.join(root, 'bundles', entry.file));
  if (crypto.createHash('sha256').update(bytes).digest('hex') !== entry.sha256) throw new Error('Fixture hash mismatch');
  files.set('/bundles/' + entry.file, bytes);
}
let requests = 0, bytes = 0;
const catalogs = new Map();
for (const [label, archive] of [['old', 'full_A'], ['update', 'update_B']]) {
  const dir = path.join(root, 'archives', archive);
  const manifest = JSON.parse(fs.readFileSync(path.join(dir, 'manifest.json'), 'utf8'));
  const matches = manifest.files.filter(e => /^remote\/catalog.*\.bin$/.test(e.path));
  if (matches.length !== 1) throw new Error('Expected one binary catalog');
  const data = fs.readFileSync(path.join(dir, matches[0].path));
  if (crypto.createHash('sha256').update(data).digest('hex') !== matches[0].sha256) throw new Error('Catalog hash mismatch');
  catalogs.set('/catalogs/' + label, data);
}
const server = http.createServer((req, res) => {
  if (req.method !== 'GET') { res.writeHead(405); res.end(); return; }
  if (req.url === '/stats') { res.setHeader('content-type', 'application/json'); res.end(JSON.stringify({ requests, bytes })); return; }
  if (catalogs.has(req.url)) { const data = catalogs.get(req.url); res.writeHead(200, { 'content-length': data.length }); res.end(data); return; }
  const data = files.get(req.url);
  if (!data) { res.writeHead(404); res.end(); return; }
  requests++; bytes += data.length;
  res.writeHead(200, { 'content-type': 'application/octet-stream', 'content-length': data.length }); res.end(data);
});
server.listen(18285, '127.0.0.1', () => console.log('SYNTHETIC_DELTA_SERVER_READY ' + root));
process.on('SIGINT', () => server.close(() => process.exit(0)));
