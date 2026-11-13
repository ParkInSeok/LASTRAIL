import { mkdtemp, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { expect, it } from 'vitest';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import { pinPlayerBuild } from '../../scripts/local/ce03-player-build';

it('pins all build files and detects changed or added files without substituting assets', async () => {
  const root = await mkdtemp(join(tmpdir(), 'player-build-'));
  await writeFile(join(root, 'game.exe'), 'synthetic executable');
  await writeFile(join(root, 'data.bin'), 'original');
  const manifest = canonicalJson({ version: 'ce03.player-build.v1.20261022', files: [
    { path: 'data.bin', sha256: await sha256Hex('original') },
    { path: 'game.exe', sha256: await sha256Hex('synthetic executable') },
  ] });
  const unchanged = await pinPlayerBuild(root, manifest, join(root, 'game.exe'));
  expect(await unchanged()).toBe(true);
  await writeFile(join(root, 'data.bin'), 'wrong version');
  expect(await unchanged()).toBe(false);
  await expect(pinPlayerBuild(root, manifest, join(root, 'game.exe'))).rejects.toThrow();
  await writeFile(join(root, 'data.bin'), 'original');
  await writeFile(join(root, 'extra.dll'), 'unreviewed');
  expect(await unchanged()).toBe(false);
});
it('rejects traversal, case aliases and executable outside the reviewed build', async () => {
  const root = await mkdtemp(join(tmpdir(), 'player-build-invalid-'));
  await writeFile(join(root, 'game.exe'), 'data');
  for (const paths of [['../game.exe'], ['game.exe', 'GAME.exe']]) {
    const raw = canonicalJson({ version: 'ce03.player-build.v1.20261022', files: paths.map(path => ({path, sha256:'a'.repeat(64)})) });
    await expect(pinPlayerBuild(root, raw, join(root, 'game.exe'))).rejects.toThrow();
  }
  const raw = canonicalJson({ version: 'ce03.player-build.v1.20261022', files: [{path:'game.exe',sha256:await sha256Hex('data')}] });
  await expect(pinPlayerBuild(root, raw, process.execPath)).rejects.toThrow();
});
