import { execFile } from 'node:child_process';
import { createHash } from 'node:crypto';
import { readFile, writeFile, mkdir, realpath, unlink } from 'node:fs/promises';
import { dirname, join, relative, resolve } from 'node:path';
import { promisify } from 'node:util';
import { z } from 'zod';

const execute = promisify(execFile);
const hash = (bytes: Buffer) => createHash('sha256').update(bytes).digest('hex');
const pinSchema = z.strictObject({ hostPath: z.string().min(1), hostSha256: z.string().regex(/^[a-f0-9]{64}$/u),
  coreSha256: z.string().regex(/^[a-f0-9]{64}$/u), newtonsoftSha256: z.string().regex(/^[a-f0-9]{64}$/u),
  inputVersion: z.literal('ordinary.local-execution-input.v3.20261109').optional() });

/** Private local operator configuration, never request data. No alternate or synthetic success engine. */
export async function createOrdinaryExecutionEngine(input: unknown, directory: string) {
  const pin = pinSchema.parse(input), project = await realpath('D:/Project/LASTRAIL');
  const owned = async (path: string) => {
    const actual = await realpath(path), part = relative(project, actual);
    if (!part || part.startsWith('..') || part.includes(':')) throw new Error('ordinary_local_path_forbidden');
    return actual;
  };
  const host = await owned(pin.hostPath), folder = dirname(host);
  const target = resolve(directory), relativeTarget = relative(project, target);
  if (!relativeTarget || relativeTarget.startsWith('..') || relativeTarget.includes(':')) throw new Error('ordinary_local_path_forbidden');
  // Validate the existing ancestor before creation so a junction cannot redirect writes outside the project.
  let ancestor = dirname(target);
  for (;;) {
    if (ancestor === project) break;
    try { await owned(ancestor); break; }
    catch (error) { if ((error as NodeJS.ErrnoException).code !== 'ENOENT') throw error; ancestor = dirname(ancestor); }
  }
  await mkdir(target, { recursive: true }); const temporary = await owned(target);
  async function verify() {
    for (const [path, expected] of [[host, pin.hostSha256], [join(folder, 'LastRail.Combat.Core.dll'), pin.coreSha256],
      [join(folder, 'Newtonsoft.Json.dll'), pin.newtonsoftSha256]]) {
      if (hash(await readFile(await owned(path!))) !== expected) throw new Error('ordinary_local_engine_pin_conflict');
    }
  }
  await verify();
  return { pin: structuredClone(pin), inputVersion: pin.inputVersion, async replay(source: unknown) {
    const bytes = JSON.stringify(source);
    if (Buffer.byteLength(bytes) > 1024 * 1024) throw new Error('ordinary_local_engine_input_capacity');
    await verify();
    const path = join(temporary, `execution-${crypto.randomUUID()}.json`);
    try {
      await writeFile(path, bytes, { flag: 'wx' });
      const { stdout } = await execute('dotnet', [host, '--local-execution', path, pin.coreSha256, pin.newtonsoftSha256], {
        cwd: folder, windowsHide: true, timeout: 30000, maxBuffer: 2 * 1024 * 1024,
        env: { ...process.env, TEMP: temporary, TMP: temporary, TMPDIR: temporary },
      });
      return JSON.parse(stdout);
    } catch (error) {
      // Keep one token-free failed source for the shared engine owner to reproduce the boundary failure.
      await writeFile(join(temporary, 'last-failed-input.json'), bytes);
      throw error;
    } finally { await unlink(path).catch(error => { if ((error as NodeJS.ErrnoException).code !== 'ENOENT') throw error; }); }
  } };
}
