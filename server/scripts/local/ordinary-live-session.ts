import { createServer } from 'node:http';
import { randomBytes } from 'node:crypto';
import { appendFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { Hono } from 'hono';
import { createOrdinaryExecutionEngine } from './ordinary-execution-engine';
import { prepareOrdinaryLocalSession } from './ordinary-session-bootstrap';
import { createAccessToken } from '../../apps/game-api/src/auth';
import { resolveAuthContext } from '../../apps/game-api/src/index';
import { OrdinaryLocalExecutionStore, registerOrdinaryLocalExecution } from '../../apps/game-api/src/ordinary-local-execution';
import { registerOperationMapCommandRoutes } from '../../apps/game-api/src/operation-map-command-routes';
import { readOperationMapView } from '../../apps/game-api/src/operation-map-view';

export async function startOrdinaryLiveSession(options: { directory: string; geometryPath: string; geometrySha256: string;
  engine: unknown; lifetimeMs: number; port?: number | undefined; persistDatabase: (snapshot: Uint8Array) => Promise<void> }) {
  if (!Number.isSafeInteger(options.lifetimeMs) || options.lifetimeMs < 60000 || options.lifetimeMs > 3600000
    || !Number.isSafeInteger(options.port ?? 0) || (options.port ?? 0) < 0 || (options.port ?? 0) > 65535) throw new Error('local_session_options_invalid');
  const engine = await createOrdinaryExecutionEngine(options.engine, join(options.directory, 'engine-inputs'));
  const owned = await prepareOrdinaryLocalSession(options.directory, options.geometryPath, options.geometrySha256, engine.pin);
  let server: ReturnType<typeof createServer> | undefined;
  try {
    const secret = randomBytes(32).toString('hex'), now = Date.now(), expiresAt = now + options.lifetimeMs, sessionId = crypto.randomUUID();
    owned.sql.run("UPDATE auth_sessions SET status='REVOKED' WHERE status='ACTIVE'");
    owned.sql.run("INSERT INTO auth_sessions(id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at) VALUES(?,?,?,?,0,'ACTIVE',?,?)",
      [sessionId, owned.accountId, randomBytes(32).toString('hex'), crypto.randomUUID(), now, expiresAt]);
    const env = { DB: owned.db, ENVIRONMENT: 'DEV', AUTH_SIGNING_KEY_CURRENT: secret, ALLOWED_ADMIN_ORIGINS: 'http://localhost:5173,http://127.0.0.1:5173' } satisfies Env & { AUTH_SIGNING_KEY_CURRENT: string };
    const store = new OrdinaryLocalExecutionStore(owned.db, engine);
    const current = await readOperationMapView(owned.db, owned.accountId, owned.runId);
    if (!current.currentAttempt && current.view.currentNodeId !== owned.nodeId) throw new Error('local_node_scope_conflict');
    // Replay empty/current accepted prefix before advertising a playable endpoint. Missing runtime fails startup.
    const prefix = current.currentAttempt ? await owned.db.prepare('SELECT sequence FROM ordinary_local_execution WHERE run_id=? AND node_id=? AND generation=?')
      .bind(owned.runId, owned.nodeId, current.currentAttempt.generation).first<{ sequence: number }>() : null;
    if (current.currentAttempt && !prefix) await store.commit(owned.accountId, `local-open-${current.currentAttempt.generation}`, {
      runId: owned.runId, nodeId: owned.nodeId, attemptGeneration: current.currentAttempt.generation,
      snapshotHash: current.currentAttempt.snapshotHash, expectedSequence: 0, inputs: [],
    });
    const local = new Hono<{ Bindings: Env }>();
    registerOrdinaryLocalExecution(local, resolveAuthContext, store);
    registerOperationMapCommandRoutes(local, resolveAuthContext, () => store);
    local.get('/__local/ordinary-execution', async c => {
      c.header('Cache-Control', 'no-store'); const auth = await resolveAuthContext(c);
      if ('errorCode' in auth) return c.json({ accepted: false, errorCode: auth.errorCode }, auth.status);
      if (auth.accountId !== owned.accountId) return c.json({ accepted: false, errorCode: 'local_scope_forbidden' }, 403);
      const status = await readOperationMapView(owned.db, auth.accountId, owned.runId), attempt = status.currentAttempt;
      if (!attempt) return c.json({ accepted: true, status, source: null });
      const source = await store.source(auth.accountId, { runId: owned.runId, nodeId: owned.nodeId,
        attemptGeneration: attempt.generation, snapshotHash: attempt.snapshotHash });
      const latest = await owned.db.prepare('SELECT sequence,result_json FROM ordinary_local_execution WHERE run_id=? AND node_id=? AND generation=?')
        .bind(owned.runId, owned.nodeId, attempt.generation).first<{ sequence: number; result_json: string }>();
      return c.json({ accepted: true, status, source: source.input, lastSequence: latest?.sequence ?? 0,
        result: latest ? JSON.parse(latest.result_json) : null });
    });
    // Only the process holding the OS lock may replace the persistent database.
    const persist = () => options.persistDatabase(owned.sql.export());
    await persist();
    let tail = Promise.resolve(), closing = false, stopped!: () => void;
    const closed = new Promise<void>(done => { stopped = done; });
    const paths = new Set(['/__local/ordinary-execution', `/v1/runs/${owned.runId}/operation-map/restart`, `/v1/runs/${owned.runId}/operation-map/completion`]);
    server = createServer((request, response) => {
      const handle = async () => {
        const address = server!.address(), host = address && typeof address !== 'string' ? `127.0.0.1:${address.port}` : '';
        const reject = (status: number, errorCode: string) => { response.writeHead(status, { 'Content-Type': 'application/json', 'Cache-Control': 'no-store' }); response.end(JSON.stringify({ accepted: false, errorCode })); };
        if (closing || Date.now() >= expiresAt) { reject(503, 'local_session_ended'); return; }
        if (request.headers.host !== host || request.headers.origin) { reject(403, 'local_host_forbidden'); return; }
        if (!paths.has(request.url ?? '') || !['GET', 'POST'].includes(request.method ?? '')
          || request.method === 'GET' && request.url !== '/__local/ordinary-execution') { reject(404, 'local_scope_unavailable'); return; }
        const parts: Buffer[] = []; let length = 0;
        for await (const chunk of request) { length += chunk.length; if (length > 65536) { reject(413, 'local_body_limit'); return; } parts.push(Buffer.from(chunk)); }
        const headers = new Headers(); for (const [key, value] of Object.entries(request.headers)) if (typeof value === 'string') headers.set(key, value);
        const result = await local.fetch(new Request(`http://${host}${request.url}`, { method: request.method!, headers,
          ...(request.method === 'GET' ? {} : { body: Buffer.concat(parts) }) }), env);
        const bytes = Buffer.from(await result.arrayBuffer()); await persist();
        appendFileSync(join(options.directory, 'requests.jsonl'), JSON.stringify({ at: new Date().toISOString(), method: request.method,
          path: request.url, status: result.status, bytes: bytes.length }) + '\n');
        response.writeHead(result.status, Object.fromEntries(result.headers)); response.end(bytes);
      };
      tail = tail.then(handle).catch(error => {
        appendFileSync(join(options.directory, 'errors.jsonl'), JSON.stringify({ at: new Date().toISOString(), kind: error instanceof Error ? error.name : 'UnknownError' }) + '\n');
        if (!response.headersSent) { response.writeHead(503, { 'Content-Type': 'application/json', 'Cache-Control': 'no-store' }); response.end('{"accepted":false,"errorCode":"ordinary_local_outcome_unknown"}'); }
        else response.destroy();
      });
    });
    server.requestTimeout = 15000; server.headersTimeout = 10000;
    await new Promise<void>((resolve, reject) => { server!.once('error', reject); server!.listen(options.port ?? 0, '127.0.0.1', () => { server!.off('error', reject); resolve(); }); });
    const address = server.address(); if (!address || typeof address === 'string') throw new Error('local_bind_failed');
    let shutdown: Promise<void> | undefined;
    const close = () => shutdown ??= (async () => { closing = true; clearTimeout(timer);
      await tail;
      try { await persist(); }
      finally { await new Promise<void>(resolve => { server!.close(() => resolve()); server!.closeAllConnections(); }); owned.sql.close(); stopped(); }
    })();
    const timer = setTimeout(() => { void close(); }, options.lifetimeMs);
    const accessToken = await createAccessToken({ secret, accountId: owned.accountId, sessionId, nowMs: now, ttlMs: options.lifetimeMs });
    const bootstrap = { version: 'ordinary.live-local-session.v1.20261106', classification: 'ISOLATED_LOCAL_AUTHORED_GAME_EXECUTION',
      accountId: owned.accountId, runId: owned.runId, nodeId: owned.nodeId, baseUrl: `http://127.0.0.1:${address.port}`, accessToken,
      expiresAt, processId: process.pid, sourcePath: '/__local/ordinary-execution', databasePath: join(options.directory, 'session.sqlite'),
      logsPath: join(options.directory, 'requests.jsonl'), geometrySha256: options.geometrySha256, enginePin: engine.pin,
      stop: 'SIGINT/SIGTERM to this process; persistent DB retained; restart rotates local authentication', restored: owned.restored };
    writeFileSync(join(options.directory, 'bootstrap.json'), JSON.stringify(bootstrap, null, 2));
    return { bootstrap, close, closed };
  } catch (error) { server?.close(); owned.sql.close(); throw error; }
}
