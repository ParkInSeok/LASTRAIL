import {z} from "zod";
export const probabilityTargetSchema=z.object({consumer:z.enum(["OFFLINE","COMBAT"]),sourceKind:z.enum(["AD_POLICY","PURCHASE_POLICY"]),policyVersion:z.string().regex(/^[A-Za-z0-9][A-Za-z0-9_.-]{0,255}$/u)});
export type ProbabilityTarget=z.infer<typeof probabilityTargetSchema>;
export const probabilityPublishSchema=probabilityTargetSchema.extend({environment:z.enum(["DEV","PRD"]),expectedSourceDigest:z.string().regex(/^sha256:[a-f0-9]{64}$/),reason:z.string().trim().min(1).max(500)}).strict();
export type ProbabilityPublish=z.infer<typeof probabilityPublishSchema>;
export const probabilityReceiptSchema=z.object({accepted:z.literal(true),contractVersion:z.literal("admin-probability-publish-v1"),environment:z.enum(["DEV","PRD"]),disclosureId:z.string().regex(/^pd_[a-f0-9]{64}$/),auditId:z.string().min(1),requestId:z.string().min(1)});
const outcome=z.object({multiplier:z.number().int(),numerator:z.number().int().min(0).max(10000),denominator:z.literal(10000),percentage:z.string()}).strict();
const common={environment:z.enum(["DEV","PRD"]),consumer:z.enum(["OFFLINE","COMBAT"]),policyVersion:probabilityTargetSchema.shape.policyVersion,scope:z.literal("MULTIPLIER_DISTRIBUTION"),sourceDigest:z.string().regex(/^sha256:[a-f0-9]{64}$/),distribution:z.array(outcome).length(3).refine(rows=>rows.reduce((n,r)=>n+r.numerator,0)===10000&&rows.every((r,i)=>r.multiplier===i+2&&r.percentage===`${Math.floor(r.numerator/100)}.${String(r.numerator%100).padStart(2,"0")}`)),contractVersion:z.literal("admin-probability-source-v1"),disclosureId:z.string().regex(/^pd_[a-f0-9]{64}$/),publicationStatus:z.enum(["PUBLISHED","UNPUBLISHED"]),canPublish:z.boolean()};
const sourceSchema=z.discriminatedUnion("publicSchemaVersion",[
 z.object({...common,publicSchemaVersion:z.literal(1),sourceStatus:z.enum(["ACTIVE","RETIRED"])}).strict(),
 z.object({...common,publicSchemaVersion:z.literal(2),sourceKind:z.literal("PURCHASE_POLICY"),sourceStatus:z.literal("CONFIGURED")}).strict()
]).refine(value=>!value.canPublish||(value.publicationStatus==="UNPUBLISHED"&&value.sourceStatus!=="RETIRED"));
export type ProbabilitySource=z.infer<typeof sourceSchema>;
export function parseProbabilitySource(raw:unknown,environment:"DEV"|"PRD",target:ProbabilityTarget):ProbabilitySource{
 const value=sourceSchema.parse(raw);
 if(value.environment!==environment||value.consumer!==target.consumer||value.policyVersion!==target.policyVersion||(value.publicSchemaVersion===2?"PURCHASE_POLICY":"AD_POLICY")!==target.sourceKind)throw new Error("확률 공시 원본 응답 불일치");
 return value;
}
export const probabilityConsumers={OFFLINE:"오프라인 보상",COMBAT:"전투 보상"};
export const probabilitySources={AD_POLICY:"광고 정책",PURCHASE_POLICY:"구매 정책"};
