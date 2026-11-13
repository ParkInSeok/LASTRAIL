import { createServer } from 'node:http';
import { readFileSync, writeFileSync, renameSync } from 'node:fs';
import { createHash } from 'node:crypto';
import initSqlJs from 'sql.js';
import { canonicalJson } from '@lastrail/common';
import app from '../../apps/game-api/src/index';
import { SqlJsD1Database } from './growth-d1';

// Disposable test DB only. Uses the production app, not an alternate admission route.
process.once('message', async (message: { path: string; runId: string; secret: string; dropResponse: boolean }) => {
  const SQL = await initSqlJs(), sql = new SQL.Database(readFileSync(message.path));
  sql.create_function('unixepoch', () => Date.now() / 1000);
  sql.run('PRAGMA foreign_keys=ON');
  const env = { DB: new SqlJsD1Database(sql) as unknown as D1Database, ENVIRONMENT: 'DEV',
    ALLOWED_ADMIN_ORIGINS: 'http://localhost:5173', AUTH_SIGNING_KEY_CURRENT: message.secret } satisfies Env;
  const path = `/v1/runs/${message.runId}/ordinary-initialization`;
  let tail = Promise.resolve(), requests = 0;
  const server = createServer((req, res) => {
    tail = tail.then(async () => {
      const address = server.address(), host = address && typeof address !== 'string' ? `127.0.0.1:${address.port}` : '';
      if (req.headers.host !== host || req.headers.origin || req.method !== 'POST' || req.url !== path) {
        res.writeHead(403); res.end(); return;
      }
      const chunks: Buffer[] = []; let length = 0;
      for await (const chunk of req) {
        length += chunk.length;
        if (length > 256) { res.writeHead(413); res.end(); return; }
        chunks.push(Buffer.from(chunk));
      }
      const headers = new Headers();
      for (const [key, value] of Object.entries(req.headers)) if (typeof value === 'string') headers.set(key, value);
      const before = Buffer.from(sql.export());
      const response = await app.fetch(new Request(`http://${host}${path}`, { method: 'POST', headers, body: Buffer.concat(chunks) }), env);
      const bytes = Buffer.from(await response.arrayBuffer()), after = Buffer.from(sql.export());
      // Publish committed bytes before optionally losing the response, then exit/reopen in the parent test.
      writeFileSync(message.path + '.next', after); renameSync(message.path + '.next', message.path);
      requests++;
      const scalar = (query: string) => sql.exec(query)[0]?.values[0]?.[0];
      process.send?.({ kind: 'REQUEST', requests, pid: process.pid, method: 'POST', status: response.status,
        responseHash: createHash('sha256').update(canonicalJson(JSON.parse(bytes.toString('utf8')))).digest('hex'), unchanged: before.equals(after),
        initializationReceipts: scalar('SELECT COUNT(*) FROM ordinary_combat_activation_receipts'),
        statRows: scalar('SELECT COUNT(*) FROM ordinary_run_stat_states'), finalRows: scalar('SELECT COUNT(*) FROM run_final_results') });
      if (message.dropResponse && requests === 1) { res.destroy(); return; }
      res.writeHead(response.status, Object.fromEntries(response.headers)); res.end(bytes);
    }).catch(() => { process.send?.({ kind: 'ERROR', error: 'probe_request_failed' }); res.destroy(); });
  });
  server.requestTimeout = 10000; server.headersTimeout = 10000;
  await new Promise<void>((resolve, reject) => { server.once('error', reject); server.listen(0, '127.0.0.1', resolve); });
  const address = server.address();
  if (!address || typeof address === 'string') throw new Error('probe_bind_failed');
  process.send?.({ kind: 'READY', pid: process.pid, baseUrl: `http://127.0.0.1:${address.port}` });
});
