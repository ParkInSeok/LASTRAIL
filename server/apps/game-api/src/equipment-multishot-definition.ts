import {z} from 'zod';

/** Internal logical definition only: no equipment effect ID or operating catalog registration. */
export const multishotDefinitionVersion = 'multishot-enchant-v1.20261015';
const coefficients = [1500, 2500, 4000, 6000, 8500] as const;
const definitionSchema = z.strictObject({
  version: z.literal(multishotDefinitionVersion),
  appliedStar: z.number().int().min(1).max(5),
  coefficientBps: z.number().int().min(1).max(10000),
  maxSecondaryTargets: z.literal(2),
  radiusL: z.strictObject({numerator: z.literal(1), denominator: z.literal(2)}),
  attackEvaluation: z.literal('AT_FIRE'),
  targetDefenseEvaluation: z.literal('AT_IMPACT'),
}).refine(value => value.coefficientBps === coefficients[value.appliedStar - 1], {
  message: 'multishot_procedure_coefficient_mismatch', path: ['coefficientBps'],
});
const candidateSchema = z.strictObject({
  instanceId: z.string().regex(/^[\x21-\x7e]{1,128}$/u),
  definitionSnapshot: definitionSchema,
});
const candidatesSchema = z.array(candidateSchema).max(3)
  .refine(values => new Set(values.map(value => value.instanceId)).size === values.length, {
    message: 'multishot_duplicate_instance',
  });
export type MultishotDefinition = z.infer<typeof definitionSchema>;
export type MultishotSelection = z.infer<typeof candidateSchema>;

/** Validate the original procedure value without deriving it from an upgraded item's current star.
 * Zod parsing detaches the returned JSON from its caller; failures remain explicit ZodErrors. */
export function parseMultishotDefinition(raw: unknown): MultishotDefinition {
  return definitionSchema.parse(raw);
}

/** Caller supplies only this wearer's eligible multishot equipment (at most three actual slots).
 * This pure selector establishes neither ownership/eligibility nor an equipped runtime binding.
 * Compare ASCII identities directly, without locale-sensitive collation or input mutation. */
export function selectHighestMultishot(raw: unknown): MultishotSelection | null {
  const candidates = candidatesSchema.parse(raw);
  let selected: MultishotSelection | null = null;
  for (const candidate of candidates) {
    if (selected === null
      || candidate.definitionSnapshot.coefficientBps > selected.definitionSnapshot.coefficientBps
      || (candidate.definitionSnapshot.coefficientBps === selected.definitionSnapshot.coefficientBps
        && candidate.instanceId < selected.instanceId)) selected = candidate;
  }
  return selected;
}