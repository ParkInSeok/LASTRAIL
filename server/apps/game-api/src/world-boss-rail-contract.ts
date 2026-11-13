import { z } from 'zod';
export const railVersion = 'world-boss.rail-action.v3.20261002';
export const worldBossPatterns = {
  world_boss_raider_king: ['rail-bombardment', 'traction-chain', 'main-gun-charge'],
  world_boss_veil: ['boarding-leap', 'carriage-breach', 'retreat-reentry'],
  world_boss_ort: ['rail-resonance', 'polarity-switch', 'resonance-backflow'],
} as const;
const id = z.string().min(1).max(200);
const ms = z.number().int().min(0).max(Number.MAX_SAFE_INTEGER - 180000);
export const sideSchema = z.enum(['LEFT', 'RIGHT']);
const roundSchema = z.strictObject({
  roundId: id, phase: z.number().int().min(1).max(3), round: z.number().int().min(1).max(2),
  previewAt: ms, lockAt: ms, internalOpenAt: ms, internalCloseAt: ms, resolveAt: ms, tailClearAt: ms,
  safeSides: z.array(sideSchema).min(1).max(2), entrySafe: z.boolean(),
  internalActions: z.strictObject({ LEFT: id, RIGHT: id, ENTRY: id }),
  railAttackRef: id, internalAttackRef: id, targetRef: id,
  buffBps: z.number().int().min(10000).max(20000), buffDurationMs: ms.max(180000),
});
export const railDefinitionSchema = z.strictObject({
  version: z.literal(railVersion), status: z.literal('LOCAL_CANDIDATE'),
  bossId: z.enum(['world_boss_raider_king', 'world_boss_veil', 'world_boss_ort']), contentVersion: id,
  startedAt: ms, mode: z.enum(['SOLO', 'COOP']), trainAccountId: id, operatorAccountId: id,
  geometry: z.strictObject({ currentPathStatus: z.literal('LOCAL_LOGICAL_ONLY_UNVERIFIED_Y'), branchLengthMm: z.number().int().positive().max(1000000000), trainLengthMm: z.number().int().positive().max(1000000000),
    minTraversalMmPerSecond: z.number().int().positive().max(1000000000), maxTraversalMmPerSecond: z.number().int().positive().max(1000000000), latencyMs: ms }),
  rounds: z.array(roundSchema).length(6),
}).superRefine((d, ctx) => {
  const invalid = (message: string) => ctx.addIssue({ code: 'custom', message });
  if ((d.mode === 'SOLO') !== (d.trainAccountId === d.operatorAccountId)) invalid('rail_role_ownership');
  if (d.geometry.minTraversalMmPerSecond > d.geometry.maxTraversalMmPerSecond) invalid('rail_speed_range');
  // This bound requires a guaranteed minimum over the entire traversal, including acceleration.
  // Geometry input alone does not prove that real Unity movement satisfies that guarantee.
  const travel = Math.ceil((d.geometry.branchLengthMm + d.geometry.trainLengthMm) * 1000 / d.geometry.minTraversalMmPerSecond) + d.geometry.latencyMs;
  const ids = new Set<string>();
  d.rounds.forEach((r, i) => {
    if (ids.has(r.roundId)) invalid('rail_round_duplicate'); ids.add(r.roundId);
    const phase = Math.floor(i / 2) + 1;
    if (r.phase !== phase || r.round !== i % 2 + 1) invalid('rail_six_round_order');
    if (!(r.previewAt >= (phase - 1) * 60000 && r.previewAt < r.lockAt && r.lockAt < r.internalOpenAt
      && r.internalOpenAt < r.internalCloseAt && r.internalCloseAt <= r.resolveAt && r.resolveAt <= r.tailClearAt
      && r.tailClearAt <= phase * 60000)) invalid('rail_time_order');
    if (r.tailClearAt - r.lockAt < travel) invalid('rail_tail_geometry_shortfall');
    if (i > 0 && d.rounds[i - 1]!.tailClearAt > r.previewAt) invalid('rail_tail_before_next_branch');
    if (new Set(r.safeSides).size !== r.safeSides.length) invalid('rail_duplicate_side');
    const both = d.bossId === 'world_boss_ort' && phase > 1;
    if (r.safeSides.length !== (both ? 2 : 1)) invalid('rail_pattern_safety');
  });
});
export type RailDefinition = z.infer<typeof railDefinitionSchema>;
export const parseRailDefinition = (input: unknown): RailDefinition => railDefinitionSchema.parse(input);
export const railCommandSchema = z.strictObject({
  version: z.literal(railVersion), attemptId: id, contentVersion: id, roundId: id, commandId: id,
  expectedRevision: z.number().int().min(0), action: z.discriminatedUnion('kind', [
    z.strictObject({ kind: z.literal('CONFIRM_ROUTE'), side: sideSchema }),
    z.strictObject({ kind: z.literal('INTERNAL'), actionId: id }),
  ]),
});
export const railAvailabilitySchema = z.strictObject({trainAvailable:z.boolean(),operatorAvailable:z.boolean(),trainConnected:z.boolean(),operatorConnected:z.boolean()});
export function readRailSnapshot(input: unknown) {
  const identity = z.object({ version: z.string() }).safeParse(input);
  if (!identity.success || identity.data.version !== railVersion) return { status: 'HISTORICAL', snapshot: input } as const;
  const parsed = z.object({ version: z.literal(railVersion), definition: railDefinitionSchema }).safeParse(input);
  return parsed.success ? {status:'PINNED',snapshot:structuredClone(input)} as const : {status:'INVALID',snapshot:null} as const;
}