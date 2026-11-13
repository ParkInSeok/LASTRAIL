import { readFileSync, realpathSync } from 'node:fs';
import { spawn } from 'node:child_process';
import { createInterface } from 'node:readline';
import { join, relative } from 'node:path';
import { z } from 'zod';
import { startOrdinaryLiveSession } from './ordinary-live-session';
import { createOrdinaryExecutionEngine } from './ordinary-execution-engine';

async function main() {
 const config = z.strictObject({ directory: z.string(), geometryPath: z.string(), geometrySha256: z.string().regex(/^[a-f0-9]{64}$/u),
  engine: z.unknown(), lifetimeMs: z.number().int().min(60000).max(3600000), port: z.number().int().min(0).max(65535).optional() })
  .parse(JSON.parse(readFileSync(process.argv[2] ?? '', 'utf8')));
for (const path of [config.directory, config.geometryPath]) {
  const part = relative(realpathSync('D:/Project/LASTRAIL'), realpathSync(path));
  if (!part || part.startsWith('..') || part.includes(':')) throw new Error('local_project_path_required');
}
 const checked = await createOrdinaryExecutionEngine(config.engine, join(config.directory, 'engine-inputs'));
 const hostPath = checked.pin.hostPath;
 const lock = spawn('dotnet', [hostPath, '--session-lock', join(config.directory, 'session.lock')], { windowsHide: true, stdio: ['pipe', 'pipe', 'pipe'] });
 lock.stderr.resume();
 const lockExit = new Promise<void>(resolve => lock.once('exit', () => resolve()));
 await new Promise<void>((resolve, reject) => {
  const timer = setTimeout(() => { lock.kill(); reject(new Error('local_session_lock_timeout')); }, 15000);
  lock.once('error', error => { clearTimeout(timer); reject(error); });
  lock.once('exit', () => { clearTimeout(timer); reject(new Error('local_session_locked_or_unavailable')); });
  lock.stdout.once('data', data => { clearTimeout(timer); if (String(data).trim() === 'LOCKED') resolve(); else { lock.kill(); reject(new Error('local_session_lock_failed')); } });
 });
 const lines = createInterface({ input: lock.stdout });
 const persistDatabase = (snapshot: Uint8Array) => new Promise<void>((resolve, reject) => {
  if (lock.exitCode !== null || lock.stdin.destroyed) { reject(new Error('local_session_lock_lost')); return; }
  const cleanup = () => { lines.off('line', saved); lock.off('exit', lost); lock.stdin.off('error', failed); };
  const lost = () => { cleanup(); reject(new Error('local_session_lock_lost')); };
  const failed = (error: Error) => { cleanup(); reject(error); };
  const saved = (line: string) => { cleanup(); if (line === 'SAVED') resolve(); else reject(new Error('local_persistence_failed')); };
  lines.once('line', saved); lock.once('exit', lost); lock.stdin.once('error', failed);
  lock.stdin.write(`SAVE ${Buffer.from(snapshot).toString('base64')}\n`, error => { if (error) failed(error); });
 });
 try {
  const session = await startOrdinaryLiveSession({ ...config, persistDatabase });
  const stop = () => { void session.close().catch(() => { process.exitCode = 1; }); };
  lock.once('exit', stop); if (lock.exitCode !== null) { await session.close(); throw new Error('local_session_lock_lost'); }
  process.once('SIGINT', stop); process.once('SIGTERM', stop);
  console.log(JSON.stringify({ baseUrl: session.bootstrap.baseUrl, bootstrapPath: config.directory + '/bootstrap.json', processId: process.pid }));
  try { await session.closed; } finally { process.removeListener('SIGINT', stop); process.removeListener('SIGTERM', stop); await session.close(); }
 } finally { lines.close(); lock.stdin.end(); await lockExit; }
}
void main().catch(error => { console.error(error instanceof Error ? error.message : 'local_session_failed'); process.exitCode = 1; });
