import {z} from 'zod';
import {canonicalJson,sha256Hex} from './index';
export class ProbabilityDisclosureError extends Error {
  constructor(readonly status:400|401|403|404|409|503,readonly code:string){super(code);}
}
export const disclosureEnvironment=z.enum(['DEV','PRD']);
export const disclosureConsumer=z.enum(['OFFLINE','COMBAT']);
export const disclosureSourceKind=z.enum(['AD_POLICY','PURCHASE_POLICY']);
export const policyVersionSchema=z.string().regex(/^[A-Za-z0-9][A-Za-z0-9_.-]{0,255}$/u);
export const disclosureIdSchema=z.string().regex(/^pd_[a-f0-9]{64}$/u);
const digest=z.string().regex(/^sha256:[a-f0-9]{64}$/u);
export const publishDisclosureSchema=z.strictObject({
  environment:disclosureEnvironment,consumer:disclosureConsumer,policyVersion:policyVersionSchema,
  sourceKind:disclosureSourceKind.optional(),
  expectedSourceDigest:digest,reason:z.string().trim().min(1).max(500),
});
export type PublishDisclosure=z.infer<typeof publishDisclosureSchema>;
export type DisclosurePrincipal={id:string;role:'OWNER'|'OPERATOR'|'VIEWER'};
export function percentage(n:number){return Math.floor(n/100)+'.'+String(n%100).padStart(2,'0');}
const outcome=z.strictObject({multiplier:z.number().int(),numerator:z.number().int().min(0).max(10000),
  denominator:z.literal(10000),percentage:z.string()});
const adProjectionSchema=z.strictObject({
  environment:disclosureEnvironment,consumer:disclosureConsumer,policyVersion:policyVersionSchema,
  scope:z.literal('MULTIPLIER_DISTRIBUTION'),sourceDigest:digest,publicSchemaVersion:z.literal(1),
  distribution:z.array(outcome).length(3).refine(rows=>rows.reduce((sum,r)=>sum+r.numerator,0)===10000
    &&rows.every((r,i)=>r.multiplier===i+2&&r.percentage===percentage(r.numerator))),
});
// Keep the v1 ad projection unchanged, including the bytes that determine old permalink IDs.
const purchaseProjectionSchema=adProjectionSchema.extend({
  publicSchemaVersion:z.literal(2),sourceKind:z.literal('PURCHASE_POLICY'),
});
export const disclosureProjectionSchema=z.discriminatedUnion('publicSchemaVersion',[adProjectionSchema,purchaseProjectionSchema]);
export type DisclosureProjection=z.infer<typeof disclosureProjectionSchema>;
const publication={
  contractVersion:z.literal('probability-disclosure-v1'),disclosureId:disclosureIdSchema,
  publishedAt:z.number().int().nonnegative().safe(),
};
export const publicDisclosureSchema=z.discriminatedUnion('publicSchemaVersion',[
  adProjectionSchema.extend(publication),purchaseProjectionSchema.extend(publication),
]);
export async function disclosureId(projection:DisclosureProjection){return 'pd_'+await sha256Hex(canonicalJson(projection));}


/** Cursor is a navigation position, never an authorization credential. */
export function probabilityHistoryCursor(environment:'DEV'|'PRD',lastId:string){
  return environment+'.'+lastId;
}
export function parseProbabilityHistoryQuery(params:URLSearchParams,environment:'DEV'|'PRD'){
  params.forEach((_value,key)=>{
    if(!['limit','cursor'].includes(key)||params.getAll(key).length!==1)throw new ProbabilityDisclosureError(400,'contract_violation');
  });
  const rawLimit=params.get('limit')??'20',cursor=params.get('cursor');
  if(!/^[1-9][0-9]?$/u.test(rawLimit)||Number(rawLimit)>50)throw new ProbabilityDisclosureError(400,'contract_violation');
  let after='';
  if(cursor!==null){
    const prefix=environment+'.';
    if(!cursor.startsWith(prefix)||!disclosureIdSchema.safeParse(cursor.slice(prefix.length)).success)throw new ProbabilityDisclosureError(400,'contract_violation');
    after=cursor.slice(prefix.length);
  }
  return {limit:Number(rawLimit),after};
}
