import { createCe03LocalHost } from '../../apps/game-api/src/ce03-local-host';
import { attestLocalConsumer } from '../../apps/game-api/src/ce03-consumer-attestation';
import { executeCe03ConsumerRunner } from './ce03-executed-runner';

type HostOptions = Omit<Parameters<typeof createCe03LocalHost>[0], 'artifacts' | 'verifier'>;
type RunnerOptions = Parameters<typeof executeCe03ConsumerRunner>[0];

/** Trusted local composition only. The running host remains unchanged on any
 * failure; callers install the returned host only after this promise succeeds.
 * Each new START still rechecks source/build identity and current owner state. */
export async function executeCe03PlayerHost(hostOptions: HostOptions, runnerOptions: RunnerOptions) {
  if ('artifacts' in hostOptions || 'verifier' in hostOptions || !runnerOptions.player ||
      hostOptions.playerAssetManifestJson !== runnerOptions.player.manifestJson || !runnerOptions.prepare.playerAssetBinding)
    throw new Error('ce03_player_host_configuration_mismatch');
  const { disposableDatabase, authenticate, ...configuration } = hostOptions;
  const hostConfiguration = { ...structuredClone(configuration), disposableDatabase, authenticate };
  const options = { ...runnerOptions, sourceFiles: [...runnerOptions.sourceFiles],
    prepare: structuredClone(runnerOptions.prepare), player: structuredClone(runnerOptions.player) };
  // Validate host mode combinations before starting an external program. This
  // constructs route handlers only; it never creates a run or spends fuel.
  createCe03LocalHost(hostConfiguration);
  const executed = await executeCe03ConsumerRunner(options);
  await attestLocalConsumer(options.prepare.draft, executed.artifacts, executed.verifier, options.prepare.commandContract,
    undefined, { manifestJson: options.player.manifestJson, prepareSha256: options.prepare.prepareSha256 });
  return { host: createCe03LocalHost({ ...hostConfiguration,
    artifacts: async () => executed.artifacts, verifier: executed.verifier }), executed };
}
