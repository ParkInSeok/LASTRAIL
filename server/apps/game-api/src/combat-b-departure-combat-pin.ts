import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {readDepartureMap} from './operation-map-departure';
import {capturePublishedOperationMap} from './operation-map-publisher';
import {operationRiskVersion,operationRiskDefinition,operationRiskRawSchema} from './combat-b-operation-risk';
export const bDepartureCombatPinVersion='combat.b-departure-combat-pin.v1.20261003';
export const bDepartureCombatSourceVersion='combat.b-departure-source.v1.20261003';
const id=z.string().min(1).max(200),hash=z.string().regex(/^[a-f0-9]{64}$/u),risk=z.enum(['I','II','III','IV','V']);
const originSchema=z.strictObject({accountId:id,runId:id,operationPreviewId:id,operationId:id,selectedRisk:risk,departureVersion:id,baseSnapshotHash:hash,definitionVersion:id,planHash:hash,publicationHash:hash});
const sourceSchema=z.strictObject({version:z.literal(bDepartureCombatSourceVersion),binding:z.strictObject({accountId:id,runId:id,operationPreviewId:id,operationId:id,selectedRisk:risk,operationDefinitionVersion:id,operationPlanHash:hash,operationPublicationHash:hash}),contextId:id,profile:z.strictObject({id,version:id,executorVersion:id,definition:z.record(z.string(),z.json())}),riskApplicationVersion:z.literal(operationRiskVersion),rawProfiles:z.array(z.strictObject({actorRef:id,hp:operationRiskRawSchema.shape.hp,actions:z.array(operationRiskRawSchema.shape.damage).min(1).max(32).refine(a=>new Set(a.map(x=>x.sourceId)).size===a.length)})).min(1).max(128).refine(a=>new Set(a.map(x=>x.actorRef)).size===a.length)});
const pinSchema=z.strictObject({version:z.literal(bDepartureCombatPinVersion),origin:originSchema,source:sourceSchema,sourceHash:hash,runtimeProfileHash:hash,riskDefinition:z.json(),pinHash:hash});
export type BDepartureCombatSource=z.infer<typeof sourceSchema>;
export type BDepartureCombatContext=z.infer<typeof originSchema>;
/** Internal actual source/executor owner only, never a JSON request body or capability flag.
 * Server composition installs a registry; a registered executable original source
 * is still required. Assertions must CAS-fence EVERY inspected source/support
 * revision in the caller's SAME original departure transaction.
 */
export interface BDepartureCombatSourceOwner {
 prepare(context:Readonly<BDepartureCombatContext>):Promise<{source:unknown;assertions:readonly D1PreparedStatement[]}>;
 supports(source:Readonly<BDepartureCombatSource>,context:Readonly<BDepartureCombatContext>):Promise<boolean>;
}
export type PreparedBDepartureSnapshot={snapshotJson:string;response:Record<string,unknown>;atomicAssertions:D1PreparedStatement[]};
const json=(x:unknown)=>canonicalJson(x as Parameters<typeof canonicalJson>[0]);
const digest=(x:unknown)=>sha256Hex(json(x));
const fail=(code:string):never=>{throw new ActivityError(409,'combat_b_departure_'+code);};
const snapshotSchema=z.object({runId:id,operationPreviewId:id,operationId:id,risk,balanceVersion:z.union([id,z.number().int().positive().safe()]),encounterType:z.literal('ORDINARY_12_NODE'),admission:z.object({version:id}).passthrough(),operationMapContent:z.unknown(),departureResponse:z.record(z.string(),z.unknown())}).passthrough();
function captureSnapshot(snapshotJson:string){const parsed=snapshotSchema.safeParse(JSON.parse(snapshotJson));if(!parsed.success)return fail('snapshot_invalid');return parsed.data;}
function requireBinding(source:BDepartureCombatSource,origin:BDepartureCombatContext){
 const b=source.binding;
 if(b.accountId!==origin.accountId||b.runId!==origin.runId||b.operationPreviewId!==origin.operationPreviewId||b.operationId!==origin.operationId||b.selectedRisk!==origin.selectedRisk||b.operationDefinitionVersion!==origin.definitionVersion||b.operationPlanHash!==origin.planHash||b.operationPublicationHash!==origin.publicationHash||source.contextId===origin.runId)return fail('source_conflict');
}
async function captureOrigin(base:ReturnType<typeof captureSnapshot>,accountId:string,runId:string){
 if(base.runId!==runId)return fail('source_conflict');
 for(const [key,value] of Object.entries(base.departureResponse)){if(!Object.hasOwn(base,key)||json(value)!==json(base[key]))return fail('response_conflict');}
 const plan=readDepartureMap(base);if(!plan)return fail('profile_unsupported');
 const content=await capturePublishedOperationMap(base.operationMapContent,plan);
 return originSchema.parse({accountId,runId,operationPreviewId:base.operationPreviewId,operationId:base.operationId,selectedRisk:base.risk,departureVersion:base.admission.version,baseSnapshotHash:await digest(base),definitionVersion:plan.definitionVersion,planHash:await digest(plan),publicationHash:content.contentHash});
}
/** Immutable payload plus source CAS statements; caller MUST insert in its existing atomic batch.
 * No INSERT, START, economic operation or support registration happens here.
 */
export async function prepareBDepartureCombatSnapshot(accountId:string,runId:string,baseJson:string,owner?:BDepartureCombatSourceOwner):Promise<PreparedBDepartureSnapshot>{
 id.parse(accountId);id.parse(runId);if(!owner)return fail('source_unavailable');
 const base=captureSnapshot(baseJson);if(Object.hasOwn(base,'bCombatPin')||Object.hasOwn(base.departureResponse,'bCombatPin'))return fail('already_pinned');
 const origin=await captureOrigin(base,accountId,runId),resolved=await owner.prepare(structuredClone(origin));
 const parsed=sourceSchema.safeParse(structuredClone(resolved.source));if(!parsed.success)return fail('source_invalid');const source=parsed.data;requireBinding(source,origin);
 const assertions=Array.from(resolved.assertions??[]);if(assertions.length<1||assertions.length>128||assertions.some(s=>typeof s.bind!=='function'))return fail('source_fence_missing');
 if(await owner.supports(structuredClone(source),structuredClone(origin))!==true)return fail('profile_unsupported');
 const payload={version:bDepartureCombatPinVersion,origin,source,sourceHash:await digest(source),runtimeProfileHash:await digest(source.profile),riskDefinition:await operationRiskDefinition()};
 const pin=pinSchema.parse({...payload,pinHash:await digest(payload)}),response={...base.departureResponse,bCombatPin:structuredClone(pin)};
 return {snapshotJson:JSON.stringify({...base,bCombatPin:pin,departureResponse:response}),response,atomicAssertions:assertions};
}
/** Trusted original SQL snapshot/owner only. Missing/null legacy pins remain missing, never upgraded.
 * A consumer of untrusted bytes MUST supply the expected pin hash from an independent trusted source.
 * Restore validates original hashes; it does NOT consult today's registry or rerun support/new START.
 */
export async function readBDepartureCombatPin(snapshotJson:string,trust:{accountId:string;runId:string;expectedPinHash?:string}){
 const t=z.strictObject({accountId:id,runId:id,expectedPinHash:hash.optional()}).parse(structuredClone(trust));
 const stored:unknown=JSON.parse(snapshotJson);if(!stored||typeof stored!=='object'||Array.isArray(stored))return fail('snapshot_invalid');
 const envelope=stored as Record<string,unknown>,response=envelope.departureResponse;
 const nested=response&&typeof response==='object'&&!Array.isArray(response)?(response as Record<string,unknown>).bCombatPin:undefined;
 if(envelope.bCombatPin===undefined||envelope.bCombatPin===null){if(nested!==undefined&&nested!==null)return fail('pin_conflict');return null;}
 const parsed=pinSchema.safeParse(envelope.bCombatPin);if(!parsed.success)return fail('pin_invalid');const pin=parsed.data;
 if(pin.origin.accountId!==t.accountId||pin.origin.runId!==t.runId||(t.expectedPinHash!==undefined&&pin.pinHash!==t.expectedPinHash)||json(pin)!==json(nested))return fail('pin_conflict');
 const base=structuredClone(envelope);delete base.bCombatPin;const baseResponse=base.departureResponse as Record<string,unknown>;delete baseResponse.bCombatPin;
 const origin=await captureOrigin(captureSnapshot(JSON.stringify(base)),t.accountId,t.runId);requireBinding(pin.source,origin);
 const {pinHash,...payload}=pin;
 if(json(origin)!==json(pin.origin)||await digest(pin.source)!==pin.sourceHash||await digest(pin.source.profile)!==pin.runtimeProfileHash||await digest(payload)!==pinHash||json(await operationRiskDefinition())!==json(pin.riskDefinition))return fail('pin_conflict');
 return pin;
}
export function describeBDepartureCombatPinRequirements(){return {version:bDepartureCombatPinVersion,productionSourceOwnerRegistered:false,canStart:false,missingSources:['ORIGINAL_NATIVE_COMBAT_CONTEXT','SUPPORTED_B_RUNTIME_PROFILE','ORIGINAL_PINNED_RISK_APPLICATION_VERSION','ORIGINAL_UNSCALED_HP_OR_EHP','ORIGINAL_RAW_ACTION_DAMAGE','ORIGINAL_RISK_PROGRESS_APPLIED_FLAGS']};}
const query='SELECT r.id,r.status,r.preview_id,r.balance_config_version,a.status AS account_status,s.snapshot_json FROM runs r JOIN accounts a ON a.id=r.account_id LEFT JOIN run_snapshots s ON s.run_id=r.id WHERE r.id=? AND r.account_id=?';
type Row={id:string;status:string;preview_id:string;balance_config_version:number;account_status:string;snapshot_json:string|null};
export async function readBDepartureCombatPinForRun(db:D1Database,accountId:string,runId:string){
 id.parse(accountId);id.parse(runId);const read=()=>db.prepare(query).bind(runId,accountId).first<Row>();const row=await read();if(!row)return fail('run_not_found');if(row.account_status!=='ACTIVE')return fail('account_inactive');if(!row.snapshot_json)return fail('snapshot_missing');
 const pin=await readBDepartureCombatPin(row.snapshot_json,{accountId,runId});
 if(pin&&(pin.origin.operationPreviewId!==row.preview_id||String(captureSnapshot(row.snapshot_json).balanceVersion)!==String(row.balance_config_version)))return fail('pin_conflict');
 const payload={version:bDepartureCombatPinVersion,accountId,runId,runStatus:row.status,startSnapshotHash:await sha256Hex(row.snapshot_json),status:pin?'PINNED_SOURCE' as const:'HISTORICAL_UNPINNED' as const,pin,canStart:false,sourcePinned:pin!==null,newProfileAdmission:false};
 const packet={...payload,packetHash:await digest(payload)};const latest=await read();if(!latest||json(latest)!==json(row))return fail('stale');return packet;
}
