import { readFileSync } from 'node:fs';
import { expect, it } from 'vitest';
import { bindPlayerAssets } from '../../apps/game-api/src/ce03-player-assets';

const raw = readFileSync('D:/Project/LASTRAIL/QA/OperatorInformation-20261019/BattleAssets424/manifest.json', 'utf8');
const formation = ['op_kyle', 'op_reina', 'op_eve', 'op_sera', 'op_mira'];
it('binds the supplied Player build bytes and exact selected order without granting admission', async () => {
  const binding = await bindPlayerAssets(raw, formation);
  expect(binding).toMatchObject({ version: 'ce03.player-asset-binding.v1.20261022',
    manifestSha256: '59e07dcc9cf4c2dc0c41b45871fa36740b7996e4a7f26e7247030fea4dbe4e7c',
    formation, unityVersion: '6000.3.22f1', admissionGranted: false });
  expect(binding).not.toHaveProperty('manifestJson');
  await expect(bindPlayerAssets(raw + ' ', formation)).rejects.toThrow();
  await expect(bindPlayerAssets(raw, [...formation].reverse())).rejects.toThrow();
  await expect(bindPlayerAssets(raw.replace('"admissionGranted":false', '"admissionGranted":true'), formation)).rejects.toThrow();
});
