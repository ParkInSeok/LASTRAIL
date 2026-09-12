import { z } from "zod";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { bindingSchema, id, policySchema, sha, sourceFileSchema, templateSchema } from "./ce03-local-schema";
import { resolveSupportThree, supportDefinitions, supportPolicies, supportVersion } from "./ce03-support-v2";

const baselineSchema = z.strictObject({ profileId: id, engineProfileVersion: id, effectiveStatsVersion: id, roundingVersion: id, simulationVersion: id,
  template: templateSchema, executionPolicy: policySchema, bindings: z.array(bindingSchema).min(1), sourceFiles: z.array(sourceFileSchema).min(1) });
const exact = <T>(values: readonly T[]) => z.custom<T>(value => {
  const parsed = z.json().safeParse(value);
  if (!parsed.success) return false;
  try { return values.some(v => canonicalJson(JSON.parse(JSON.stringify(v))) === canonicalJson(parsed.data)); } catch { return false; }
});
const manualSchema = z.union([templateSchema.shape.train.shape.manual.element, exact(supportDefinitions)]);
// Cross-field refinements are checked on the closed baseline, then exact projection
// equality below preserves every field except its deliberately widened manual rows.
const projectedTemplateSchema = z.strictObject({ ...templateSchema.shape, train: templateSchema.shape.train.extend({ manual: z.array(manualSchema).length(3) }) });
const selectedSchema = z.array(id).length(3).refine(s => new Set(s).size === 3);
const profileDraftSchema = z.strictObject({ schemaVersion: z.literal("ce03-support-full-draft-r7"), simulationVersion: z.literal(supportVersion),
  ready: z.literal(false), executionEvidence: z.null(), baseline: baselineSchema, baselineSha256: sha,
  selectedSupport: selectedSchema, template: projectedTemplateSchema, supportPolicy: exact([supportPolicies]),
  profileSha256: sha,
});
export type SupportProfileDraft = z.infer<typeof profileDraftSchema>;
export async function createSupportProfileDraft(baselineInput: unknown, selectedInput: unknown): Promise<SupportProfileDraft> {
  const baseline = baselineSchema.parse(baselineInput), selectedSupport = selectedSchema.parse(selectedInput);
  if (baseline.template.execution.policyVersion !== baseline.executionPolicy.version || baseline.template.execution.policySha256 !== await sha256Hex(canonicalJson(baseline.executionPolicy))) throw new Error("baseline execution policy mismatch");
  const template = projectedTemplateSchema.parse({ ...baseline.template, train: { ...baseline.template.train,
    manual: resolveSupportThree(selectedSupport, baseline.template.train.manual) } });
  const core = { schemaVersion: "ce03-support-full-draft-r7" as const, simulationVersion: supportVersion,
    ready: false as const, executionEvidence: null, baseline, baselineSha256: await sha256Hex(canonicalJson(baseline)),
    selectedSupport, template, supportPolicy: supportPolicies };
  return structuredClone(profileDraftSchema.parse({ ...core, profileSha256: await sha256Hex(canonicalJson(core)) }));
}
export async function parseSupportProfileDraft(input: unknown): Promise<SupportProfileDraft> {
  const parsed = profileDraftSchema.parse(input);
  const expected = await createSupportProfileDraft(parsed.baseline, parsed.selectedSupport);
  if (canonicalJson(parsed) !== canonicalJson(expected)) throw new Error("support full draft projection mismatch");
  return expected;
}
