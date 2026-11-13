import { createHash } from 'node:crypto';
import { createReadStream } from 'node:fs';
import { readdir, realpath } from 'node:fs/promises';
import { isAbsolute, join, relative, sep } from 'node:path';
import { z } from 'zod';
import { canonicalJson } from '@lastrail/common';

const schema = z.strictObject({ version: z.literal('ce03.player-build.v1.20261022'),
  files: z.array(z.strictObject({ path: z.string().min(1), sha256: z.string().regex(/^[a-f0-9]{64}$/u) })).min(1).max(8192) });
const fail = (): never => { throw new Error('ce03_player_build_mismatch'); };
async function digest(path: string) {
  const hash = createHash('sha256');
  for await (const bytes of createReadStream(path)) hash.update(bytes);
  return hash.digest('hex');
}
/** Build manifest must itself be in the reviewed source bundle. It is an
 * execution identity pin, not proof of coverage or a substitute for asset roles. */
export async function pinPlayerBuild(buildRoot: string, manifestJson: string, executable: string) {
  const manifest = schema.parse(JSON.parse(manifestJson));
  if (canonicalJson(manifest) !== manifestJson) return fail();
  const expected = manifest.files.map(f => f.path);
  if (new Set(expected.map(p => p.toLowerCase())).size !== expected.length || expected.some(p =>
    isAbsolute(p) || p.includes('\\') || p.includes(':') || p.split('/').some(x => x === '' || x === '.' || x === '..'))) return fail();
  const root = await realpath(buildRoot), entry = await realpath(executable), rel = relative(root, entry);
  if (!rel || isAbsolute(rel) || rel === '..' || rel.startsWith(`..${sep}`) || !expected.includes(rel.split(sep).join('/'))) return fail();
  const check = async () => {
    try {
      if (await realpath(buildRoot) !== root || await realpath(executable) !== entry) return false;
      const actual: string[] = [];
      const walk = async (directory: string, prefix: string): Promise<void> => {
        for (const row of await readdir(directory, { withFileTypes: true })) {
          if (row.isSymbolicLink()) return fail();
          const name = prefix + row.name, path = join(directory, row.name);
          if (row.isDirectory()) await walk(path, name + '/');
          else if (row.isFile()) { actual.push(name); if (actual.length > 8192) return fail(); }
          else return fail();
        }
      };
      await walk(root, '');
      if (canonicalJson(actual.sort()) !== canonicalJson([...expected].sort())) return false;
      for (const row of manifest.files) if (await digest(join(root, row.path)) !== row.sha256) return false;
      return true;
    } catch { return false; }
  };
  if (!await check()) return fail();
  return check;
}
