import {canonicalJson,sha256Hex} from '@lastrail/common';
import {z} from 'zod';
import {CommunityStoreError} from './community-content-store';
export const communityConsentVersion='community.consent.v1.20261008' as const;
export const communityCreationEligibilityVersion='community.creation-eligibility.v1.20261008' as const;
const id=z.string().regex(/^[A-Za-z0-9_-]{1,128}$/u),version=z.string().regex(/^[A-Za-z0-9][A-Za-z0-9_.-]{0,255}$/u),digest=z.string().regex(/^[0-9a-f]{64}$/u);
const reference=z.strictObject({id,version,contentDigest:digest});
const references=z.strictObject({serviceTerms:reference,communityRules:reference});
export const communityConsentPolicySchema=z.strictObject({environment:z.enum(['DEV','PRD']),locale:z.literal('ko-KR'),policyRevision:z.number().int().positive().safe(),policyDigest:digest,documents:references});
export const communityConsentAcceptSchema=communityConsentPolicySchema.extend({commandId:id});
const receiptSchema=communityConsentAcceptSchema.extend({contractVersion:z.literal(communityConsentVersion),acceptedAt:z.number().int().nonnegative().safe()});
function policyOf(command:z.infer<typeof communityConsentAcceptSchema>){const {commandId:_id,...policy}=command;void _id;return communityConsentPolicySchema.parse(policy);}
export type CommunityConsentPolicy=z.infer<typeof communityConsentPolicySchema>;
export type PublishedCommunityPolicy={policy:CommunityConsentPolicy;documents:{kind:'SERVICE_TERMS'|'COMMUNITY_RULES';id:string;version:string;contentDigest:string;bodyText:string}[];guards:D1PreparedStatement[]};
/** Existing published sources can replace this adapter; policy references are not a second copy of document bodies. */
export interface CommunityConsentSource{read():Promise<PublishedCommunityPolicy>}
export type CommunityCreationConsent={guards:D1PreparedStatement[];assertCurrent:()=>Promise<void>};
export class D1CommunityConsentSource implements CommunityConsentSource{
 constructor(private readonly db:D1Database,private readonly environment:'DEV'|'PRD',private readonly locale:'ko-KR'){}
 async read():Promise<PublishedCommunityPolicy>{
  const row=await this.db.prepare(`SELECT p.policy_revision,p.references_json,p.policy_digest,p.publication_state FROM community_policy_current c JOIN community_policy_pairs p ON p.environment=c.environment AND p.locale=c.locale AND p.policy_revision=c.policy_revision WHERE c.environment=? AND c.locale=?`).bind(this.environment,this.locale).first<{policy_revision:number;references_json:string;policy_digest:string;publication_state:string}>();
  if(!row||row.publication_state!=='PUBLISHED')throw new CommunityStoreError(503,'community_policy_source_unavailable');
  let policy:CommunityConsentPolicy;try{policy=communityConsentPolicySchema.parse({environment:this.environment,locale:this.locale,policyRevision:row.policy_revision,policyDigest:row.policy_digest,documents:JSON.parse(row.references_json)});const {policyDigest:expected,...manifest}=policy;if(canonicalJson(policy.documents)!==row.references_json||await sha256Hex(canonicalJson(manifest))!==expected)throw new Error();}catch{throw new CommunityStoreError(503,'community_policy_source_invalid');}
  const guards=[this.db.prepare(`INSERT INTO community_content_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM community_policy_current c JOIN community_policy_pairs p ON p.environment=c.environment AND p.locale=c.locale AND p.policy_revision=c.policy_revision WHERE c.environment=? AND c.locale=? AND c.policy_revision=? AND p.references_json=? AND p.policy_digest=? AND p.publication_state='PUBLISHED') THEN 1 ELSE 0 END`).bind(this.environment,this.locale,policy.policyRevision,row.references_json,row.policy_digest)],documents:PublishedCommunityPolicy['documents']=[];
  for(const [field,kind] of [['serviceTerms','SERVICE_TERMS'],['communityRules','COMMUNITY_RULES']] as const){const ref=policy.documents[field];const doc=await this.db.prepare('SELECT kind,content_digest,body_text,publication_state FROM community_policy_documents WHERE environment=? AND locale=? AND document_id=? AND document_version=?').bind(this.environment,this.locale,ref.id,ref.version).first<{kind:string;content_digest:string;body_text:string;publication_state:string}>();
   if(!doc||doc.publication_state!=='PUBLISHED')throw new CommunityStoreError(503,'community_policy_source_unavailable');
   if(doc.kind!==kind||doc.content_digest!==ref.contentDigest||typeof doc.body_text!=='string'||doc.body_text.trim().length===0||await sha256Hex(doc.body_text)!==ref.contentDigest)throw new CommunityStoreError(503,'community_policy_source_invalid');
   documents.push({kind,...ref,bodyText:doc.body_text});guards.push(this.db.prepare(`INSERT INTO community_content_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM community_policy_documents WHERE environment=? AND locale=? AND document_id=? AND document_version=? AND kind=? AND content_digest=? AND body_text=? AND publication_state='PUBLISHED') THEN 1 ELSE 0 END`).bind(this.environment,this.locale,ref.id,ref.version,kind,ref.contentDigest,doc.body_text));
  }
  return {policy,documents,guards};
 }
}
/** One acceptance/receipt owner; explicit authenticated command only, no account booleans or game-consent inference. */
export class CommunityConsentService{
 private readonly source:CommunityConsentSource;
 constructor(private readonly db:D1Database,private readonly environment:'DEV'|'PRD',private readonly locale:'ko-KR',private readonly authorize?:()=>Promise<D1PreparedStatement[]>,private readonly now:()=>number=()=>Date.now(),source?:CommunityConsentSource){this.source=source??new D1CommunityConsentSource(db,environment,locale);}
 private async sourceNow(){try{const source=await this.source.read();communityConsentPolicySchema.parse(source.policy);if(source.policy.environment!==this.environment||source.policy.locale!==this.locale)throw new Error();return source;}catch(e){if(e instanceof CommunityStoreError)throw e;throw new CommunityStoreError(503,'community_policy_source_unavailable');}}
 private async unchanged(expected:CommunityConsentPolicy){if(canonicalJson((await this.sourceNow()).policy)!==canonicalJson(expected))throw new CommunityStoreError(409,'community_policy_changed');}
 private async accepted(accountId:string,policy:CommunityConsentPolicy){
  const row=await this.db.prepare(`SELECT a.policy_revision,a.policy_digest,a.command_id,a.accepted_at,a.receipt_json,r.response_json,r.request_hash FROM community_consent_current a LEFT JOIN community_consent_receipts r ON r.account_id=a.account_id AND r.command_id=a.command_id WHERE a.account_id=? AND a.environment=? AND a.locale=?`).bind(accountId,this.environment,this.locale).first<{policy_revision:number;policy_digest:string;command_id:string;accepted_at:number;receipt_json:string;response_json:string;request_hash:string}>();if(!row||row.policy_revision!==policy.policyRevision||row.policy_digest!==policy.policyDigest)return false;
  try{const receipt=receiptSchema.parse(JSON.parse(row.receipt_json));const {contractVersion:_contract,acceptedAt,...command}=receipt;void _contract;
   if(row.response_json!==row.receipt_json||canonicalJson(receipt)!==row.receipt_json||acceptedAt!==row.accepted_at||receipt.commandId!==row.command_id||canonicalJson(policyOf(command))!==canonicalJson(policy)||row.request_hash!==await sha256Hex(canonicalJson({contractVersion:communityConsentVersion,...command})))throw new Error();return true;
  }catch{throw new CommunityStoreError(503,'community_consent_storage_invalid');}
 }
 async current(accountId:string){if(!id.safeParse(accountId).success)throw new CommunityStoreError(400,'community_command_invalid');await this.authorize?.();const source=await this.sourceNow(),accepted=await this.accepted(accountId,source.policy);await this.authorize?.();await this.unchanged(source.policy);if(await this.accepted(accountId,source.policy)!==accepted)throw new CommunityStoreError(409,'community_consent_changed');return {contractVersion:communityConsentVersion,policy:source.policy,documents:source.documents,accepted};}
 async accept(accountId:string,raw:unknown){
  const parsed=communityConsentAcceptSchema.safeParse(raw);if(!parsed.success||!id.safeParse(accountId).success)throw new CommunityStoreError(400,'community_command_invalid');const command=parsed.data;if(command.environment!==this.environment||command.locale!==this.locale)throw new CommunityStoreError(400,'community_command_invalid');
  const hash=await sha256Hex(canonicalJson({contractVersion:communityConsentVersion,...command}));const replay=async()=>{const row=await this.db.prepare('SELECT request_hash,response_json FROM community_consent_receipts WHERE account_id=? AND command_id=?').bind(accountId,command.commandId).first<{request_hash:string;response_json:string}>();if(!row)return null;if(row.request_hash!==hash)throw new CommunityStoreError(409,'community_request_conflict');try{const receipt=receiptSchema.parse(JSON.parse(row.response_json));const {contractVersion:_contract,acceptedAt:_at,...saved}=receipt;void _contract;void _at;if(canonicalJson(saved)!==canonicalJson(command))throw new Error();return receipt;}catch{throw new CommunityStoreError(503,'community_consent_storage_invalid');}};
  await this.authorize?.();const original=await replay();if(original){await this.authorize?.();return original;}
  const source=await this.sourceNow();if(canonicalJson(policyOf(command))!==canonicalJson(source.policy))throw new CommunityStoreError(409,'community_policy_changed');
  const receipt=receiptSchema.parse({...command,contractVersion:communityConsentVersion,acceptedAt:this.now()}),json=canonicalJson(receipt),rights=await this.authorize?.()??[];await this.unchanged(source.policy);
  try{await this.db.batch([...rights,...source.guards,
   this.db.prepare('INSERT INTO community_consent_receipts(account_id,command_id,request_hash,response_json) VALUES(?,?,?,?)').bind(accountId,command.commandId,hash,json),
   this.db.prepare(`INSERT INTO community_consent_current(account_id,environment,locale,policy_revision,policy_digest,command_id,accepted_at,receipt_json) VALUES(?,?,?,?,?,?,?,?) ON CONFLICT(account_id,environment,locale) DO UPDATE SET policy_revision=excluded.policy_revision,policy_digest=excluded.policy_digest,command_id=excluded.command_id,accepted_at=excluded.accepted_at,receipt_json=excluded.receipt_json`).bind(accountId,this.environment,this.locale,command.policyRevision,command.policyDigest,command.commandId,receipt.acceptedAt,json),this.db.prepare('DELETE FROM community_content_write_guard')]);
  }catch{await this.authorize?.();const result=await replay();if(result)return result;await this.unchanged(source.policy);throw new CommunityStoreError(503,'community_consent_storage_unavailable');}return receipt;
 }
 async creationEligibility(accountId:string):Promise<CommunityCreationConsent>{if(!id.safeParse(accountId).success)throw new CommunityStoreError(400,'community_command_invalid');const source=await this.sourceNow();if(!await this.accepted(accountId,source.policy))throw new CommunityStoreError(403,'community_consent_required');
  return {guards:[...source.guards,this.db.prepare(`INSERT INTO community_content_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM community_consent_current a JOIN community_consent_receipts r ON r.account_id=a.account_id AND r.command_id=a.command_id WHERE a.account_id=? AND a.environment=? AND a.locale=? AND a.policy_revision=? AND a.policy_digest=? AND r.response_json=a.receipt_json) THEN 1 ELSE 0 END`).bind(accountId,this.environment,this.locale,source.policy.policyRevision,source.policy.policyDigest)],assertCurrent:async()=>{await this.unchanged(source.policy);if(!await this.accepted(accountId,source.policy))throw new CommunityStoreError(403,'community_consent_required');}};
 }
 async creationGuards(accountId:string){return (await this.creationEligibility(accountId)).guards;}
}