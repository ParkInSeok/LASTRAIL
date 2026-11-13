import { z } from 'zod';

// Shared with OrdinaryFiveCombatFactory and RunRandom; this pin is not a readiness claim.
export const ordinaryNodeExecutionOriginSchema = z.strictObject({
  executionVersion: z.literal('combat.ordinary-five-execution.v1.20261028'),
  determinism: z.strictObject({
    version: z.literal('ordinary.node-random.v1.20261029'),
    algorithm: z.literal('XORSHIFT32_13_17_5_V1'),
    seed: z.number().int().min(0).max(0xffffffff),
    initialState: z.number().int().min(1).max(0xffffffff),
  }).refine(x => x.initialState === (x.seed === 0 ? 0x6d2b79f5 : x.seed)),
});
export type OrdinaryNodeExecutionOrigin = z.infer<typeof ordinaryNodeExecutionOriginSchema>;

/** Only a committed ARRIVE makes this candidate authoritative; never call on restart. */
export function issueOrdinaryNodeExecutionOrigin(): OrdinaryNodeExecutionOrigin {
  const seed = crypto.getRandomValues(new Uint32Array(1))[0]!;
  return { executionVersion: 'combat.ordinary-five-execution.v1.20261028', determinism: {
    version: 'ordinary.node-random.v1.20261029', algorithm: 'XORSHIFT32_13_17_5_V1',
    seed, initialState: seed === 0 ? 0x6d2b79f5 : seed,
  } };
}
