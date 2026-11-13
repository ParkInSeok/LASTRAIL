import { z } from 'zod';
import { sha256Hex } from '@lastrail/common';
import { ActivityError } from './activity-policy';

// Supplied Unity build artifact, not a mutable client-provided catalog. A new
// build requires an explicit source/version review; old receipts retain this pin.
export const playerAssetManifestSha256 = '59e07dcc9cf4c2dc0c41b45871fa36740b7996e4a7f26e7247030fea4dbe4e7c';
const manifestSchema = z.object({ version: z.literal('combat-player-assets-v1'), admissionGranted: z.literal(false),
  unityVersion: z.literal('6000.3.22f1'), configurationPath: z.string().min(1), formation: z.array(z.string()).length(5) });
export async function bindPlayerAssets(manifestJson: string, selectedOperators: readonly string[]) {
  if (await sha256Hex(manifestJson) !== playerAssetManifestSha256)
    throw new ActivityError(409, 'ce03_player_asset_manifest_mismatch');
  const manifest = manifestSchema.parse(JSON.parse(manifestJson));
  if (selectedOperators.length !== manifest.formation.length || selectedOperators.some((id, i) => id !== manifest.formation[i]))
    throw new ActivityError(409, 'ce03_player_asset_formation_mismatch');
  return { version: 'ce03.player-asset-binding.v1.20261022' as const,
    manifestVersion: manifest.version, manifestSha256: playerAssetManifestSha256,
    unityVersion: manifest.unityVersion, configurationPath: manifest.configurationPath,
    formation: [...manifest.formation], admissionGranted: false as const };
}
export type PlayerAssetBinding = Awaited<ReturnType<typeof bindPlayerAssets>>;
