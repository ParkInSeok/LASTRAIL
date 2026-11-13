import {z} from 'zod';
import {canonicalJson,sha256Hex} from './index';

export class PublicCatalogError extends Error {
  constructor(readonly code:string){super(code);}
}

const environment=z.enum(['DEV','PRD']);
const channel=z.string().regex(/^[A-Za-z0-9][A-Za-z0-9_.-]{0,63}$/u).refine(value=>value!=='INTERNAL');
const appVersionCode=z.string().regex(/^[1-9][0-9]{0,9}$/u).refine(value=>Number(value)<=2147483647);
const version=z.string().regex(/^[A-Za-z0-9][A-Za-z0-9_.-]{0,255}$/u);
const digest=z.string().regex(/^sha256:[a-f0-9]{64}$/u);
const snapshotId=z.string().regex(/^pcs_[a-f0-9]{64}$/u);
const indexId=z.string().regex(/^pri_[a-f0-9]{64}$/u);
const disclosureId=z.string().regex(/^pd_[a-f0-9]{64}$/u);
const publicText=z.string().trim().min(1).max(2000);
const publicItem=z.strictObject({
  id:version,kind:z.enum(['AUGMENT','EQUIPMENT','UNIQUE_EFFECT','SET','ENCHANTMENT','OPERATOR','TURRET','COMMANDER','MAP','COMBAT']),
  name:publicText,description:publicText,category:publicText,unit:publicText,
  values:z.array(z.strictObject({label:publicText,value:publicText})).max(64),
  conditions:z.array(publicText).max(64),stackLimit:z.number().int().nonnegative().safe().nullable(),
  compatibleWith:z.array(version).max(256),acquisition:z.array(publicText).max(64),
  iconPath:z.string().regex(/^\/assets\/[A-Za-z0-9/_-]+\.(?:png|webp|svg)$/u).nullable(),
});
export const verifiedPublicReleaseSchema=z.strictObject({
  environment,channel,appVersionCode,contentReleaseId:version,sourceBalanceVersion:version,sourceDigest:digest,
  publicSchemaVersion:z.literal(1),templateVersion:version,localeVersion:version,
  items:z.array(publicItem).min(1).max(10000),
}).superRefine((data,ctx)=>{
  const ids=new Set<string>();
  for(const item of data.items){
    if(ids.has(item.id))ctx.addIssue({code:'custom',message:'duplicate public ID'});
    ids.add(item.id);
  }
});
export type VerifiedPublicRelease=z.infer<typeof verifiedPublicReleaseSchema>;
export type PublicCatalogSnapshot=VerifiedPublicRelease&{contractVersion:'public-catalog-snapshot-v1';snapshotId:string};
/** Only a trusted server adapter backed by a real release manifest and catalog may satisfy this port. */
export type PublicReleaseSource={verify:(candidate:unknown)=>Promise<unknown>};
export async function buildPublicCatalogSnapshot(candidate:unknown,source?:PublicReleaseSource):Promise<PublicCatalogSnapshot>{
  if(!source)throw new PublicCatalogError('public_release_source_unavailable');
  const checked=verifiedPublicReleaseSchema.safeParse(candidate);
  if(!checked.success)throw new PublicCatalogError('public_catalog_invalid');
  let actual:unknown;
  try{actual=await source.verify(candidate);}catch{throw new PublicCatalogError('public_release_source_unavailable');}
  const verified=verifiedPublicReleaseSchema.safeParse(actual);
  if(!verified.success||canonicalJson(verified.data)!==canonicalJson(checked.data))
    throw new PublicCatalogError('public_release_source_mismatch');
  const projection=verified.data;
  return {...projection,contractVersion:'public-catalog-snapshot-v1',snapshotId:'pcs_'+await sha256Hex(canonicalJson(projection))};
}

const revisionInput=z.strictObject({environment,channel,appVersionCode,revision:z.number().int().positive().safe(),releaseId:version,
  snapshotId,disclosureIds:z.array(disclosureId).max(100),publishedAt:z.number().int().nonnegative().safe()});
export type PublicReleaseIndex={contractVersion:'public-release-index-v1';environment:'DEV'|'PRD';channel:string;appVersionCode:string;revision:number;
  releaseId:string;snapshotId:string;disclosureIds:string[];publishedAt:number;previousIndexId:string|null;indexId:string};
/** Pure revision projection. The store checks the actual source and commits with CAS. */
export async function createPublicReleaseRevision(input:unknown,previous:PublicReleaseIndex|null):Promise<PublicReleaseIndex>{
  const parsed=revisionInput.safeParse(input);
  if(!parsed.success)throw new PublicCatalogError('public_release_index_invalid');
  const data=parsed.data;
  if(data.revision!==(previous?.revision??0)+1||previous&&(previous.environment!==data.environment||previous.channel!==data.channel||previous.appVersionCode!==data.appVersionCode)||
     new Set(data.disclosureIds).size!==data.disclosureIds.length)
    throw new PublicCatalogError('public_release_revision_conflict');
  const projection={...data,disclosureIds:[...data.disclosureIds].sort(),previousIndexId:previous?.indexId??null};
  return {...projection,contractVersion:'public-release-index-v1',indexId:'pri_'+await sha256Hex(canonicalJson(projection))};
}
export function validatePublicCatalogSnapshot(value:unknown):Promise<PublicCatalogSnapshot>{
  if(!value||typeof value!=='object')throw new PublicCatalogError('public_catalog_invalid');
  const raw={...value as Record<string,unknown>};
  delete raw.contractVersion;delete raw.snapshotId;
  const parsed=verifiedPublicReleaseSchema.safeParse(raw);
  if(!parsed.success)throw new PublicCatalogError('public_catalog_invalid');
  return buildSnapshotFromStored(value,parsed.data);
}
async function buildSnapshotFromStored(value:unknown,projection:VerifiedPublicRelease):Promise<PublicCatalogSnapshot>{
  const payload=value as Record<string,unknown>;
  if(payload.contractVersion!=='public-catalog-snapshot-v1'||!snapshotId.safeParse(payload.snapshotId).success||
     Object.keys(payload).sort().join(',')!==[...Object.keys(projection),'contractVersion','snapshotId'].sort().join(','))
    throw new PublicCatalogError('public_catalog_invalid');
  const expected='pcs_'+await sha256Hex(canonicalJson(projection));
  if(payload.snapshotId!==expected)throw new PublicCatalogError('public_catalog_invalid');
  return payload as PublicCatalogSnapshot;
}
export async function validatePublicReleaseIndex(value:unknown):Promise<PublicReleaseIndex>{
  if(!value||typeof value!=='object')throw new PublicCatalogError('public_release_index_invalid');
  const raw={...value as Record<string,unknown>};
  delete raw.contractVersion;delete raw.indexId;delete raw.previousIndexId;
  const parsed=revisionInput.safeParse(raw);
  if(!parsed.success)throw new PublicCatalogError('public_release_index_invalid');
  const payload=value as Record<string,unknown>;
  if(payload.contractVersion!=='public-release-index-v1'||!indexId.safeParse(payload.indexId).success||
     !(payload.previousIndexId===null||indexId.safeParse(payload.previousIndexId).success)||
     Object.keys(payload).sort().join(',')!==[...Object.keys(parsed.data),'previousIndexId','contractVersion','indexId'].sort().join(','))
    throw new PublicCatalogError('public_release_index_invalid');
  const projection={...parsed.data,previousIndexId:payload.previousIndexId as string|null};
  if(new Set(parsed.data.disclosureIds).size!==parsed.data.disclosureIds.length||
    canonicalJson(parsed.data.disclosureIds)!==canonicalJson([...parsed.data.disclosureIds].sort())||
    payload.indexId!=='pri_'+await sha256Hex(canonicalJson(projection)))throw new PublicCatalogError('public_release_index_invalid');
  return payload as PublicReleaseIndex;
}




