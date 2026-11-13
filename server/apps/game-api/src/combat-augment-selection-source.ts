import {canonicalJson,sha256Hex} from '@lastrail/common';
import {z} from 'zod';
import {ActivityError} from './activity-policy';
import {readCombatAuthority} from './combat-authority';
import {SoloAugmentSelection,soloSelectionIssueSchema} from './solo-augment-selection';
export const augmentSelectionSourceVersion='combat.augment-selection-source.v1.20261016';
const id=z.string().min(1).max(200),hash=z.string().regex(/^[a-f0-9]{64}$/u),revision=z.number().int().positive().safe();
const requestSchema=z.strictObject({runId:id,eventId:id,environment:id,expectedStoreRevision:revision,
 expectedCatalogVersion:id,expectedStartSnapshotHash:hash});
const trustSchema=z.strictObject({accountId:id,runId:id,eventId:id,environment:id});
const json=(v:unknown)=>canonicalJson(z.json().parse(v));
const digest=(v:unknown)=>sha256Hex(json(v));
function fail(code:string):never{throw new ActivityError(409,'augment_selection_source_'+code);}
const sql=`SELECT g.account_id,g.environment,g.stage_id,g.issue_hash,g.start_snapshot_json,g.initial_json,g.checkpoint_json,g.write_revision,
 r.account_id AS run_owner,r.status AS run_status,a.status AS account_status,s.snapshot_json AS current_start_snapshot_json
 FROM solo_augment_grants g JOIN runs r ON r.id=g.run_id JOIN accounts a ON a.id=r.account_id
 JOIN run_snapshots s ON s.run_id=r.id WHERE g.run_id=? AND g.event_id=?`;
type Row={account_id:string;environment:string;stage_id:string;issue_hash:string;start_snapshot_json:string;initial_json:string;
 checkpoint_json:string;write_revision:number;run_owner:string;run_status:string;account_status:string;current_start_snapshot_json:string};
const readOnly={extraApproval:{authorize:async()=>{throw Error('read_only');}}};
/** Internal read-only ORDINARY_SOLO selection evidence from the existing real grant store.
 * CatalogVersion/selected ID are issuance facts, NOT validation of a catalog definition or effect ownership.
 * No issuer, catalog activation, new augment ID, trial exemption, START mutation or public route.
 * A formal effect producer must later bind its approved ID+definition to this verified source. */
export async function captureAugmentSelectionSource(db:D1Database,accountId:string,input:unknown){
 return captureOriginal(db,accountId,input,true);
}
/** Recovery only: return the existing selection, including after run completion.
 * Retains account access and all original/hash checks. Supplies no active-run
 * evidence packet and cannot authorize a new grant or runtime effect. */
export async function readOriginalAugmentSelection(db:D1Database,accountId:string,input:unknown){
 return (await captureOriginal(db,accountId,input,false)).selection;
}
async function captureOriginal(db:D1Database,accountId:string,input:unknown,requireActiveRun:boolean){
 id.parse(accountId);const req=requestSchema.parse(input);
 const read=()=>db.prepare(sql).bind(req.runId,req.eventId).first<Row>();
 const row=await read();if(!row)return fail('missing');
 if(row.account_id!==accountId||row.run_owner!==accountId||row.environment!==req.environment)
  throw new ActivityError(403,'augment_selection_source_forbidden');
 if(row.account_status!=='ACTIVE'||(requireActiveRun&&row.run_status!=='ACTIVE'))return fail('inactive');
 if(!revision.safeParse(row.write_revision).success||row.write_revision!==req.expectedStoreRevision)return fail('revision_conflict');
 if(row.start_snapshot_json!==row.current_start_snapshot_json||await sha256Hex(row.start_snapshot_json)!==req.expectedStartSnapshotHash)return fail('start_conflict');
 let mode:unknown;
 try{mode=readCombatAuthority(z.object({admission:z.object({combatAuthority:z.unknown()})}).parse(JSON.parse(row.start_snapshot_json)).admission.combatAuthority).contract?.mode;}catch{return fail('start_invalid');}
 if(mode!=='ORDINARY_SOLO')return fail('mode_unsupported');
 let selection:ReturnType<SoloAugmentSelection['read']>,issue:z.infer<typeof soloSelectionIssueSchema>;
 try{
  if(row.checkpoint_json.length>1024*1024||row.initial_json.length>1024*1024)return fail('capacity');
  const initial=SoloAugmentSelection.restore(row.initial_json,readOnly).read();
  selection=SoloAugmentSelection.restore(row.checkpoint_json,readOnly).read();
  const initialIssue=soloSelectionIssueSchema.parse(JSON.parse(row.initial_json).issue);
  issue=soloSelectionIssueSchema.parse(JSON.parse(row.checkpoint_json).issue);
  if(json(initialIssue)!==json(issue)||initial.grantRevision!==0||initial.state!=='SELECTING')return fail('issue_conflict');
 }catch{return fail('checkpoint_invalid');}
 if(issue.accountId!==accountId||issue.selectionOwner!==accountId||issue.environment!==req.environment
  ||issue.runId!==req.runId||issue.eventId!==req.eventId||issue.stageId!==row.stage_id||issue.catalogVersion!==req.expectedCatalogVersion
  ||await digest(issue)!==row.issue_hash)return fail('issue_conflict');
 const body={version:augmentSelectionSourceVersion,accountId,environment:req.environment,runId:req.runId,eventId:req.eventId,
  stageId:issue.stageId,catalogVersion:issue.catalogVersion,storeRevision:row.write_revision,
  issueHash:row.issue_hash,checkpointHash:await sha256Hex(row.checkpoint_json),startSnapshotHash:req.expectedStartSnapshotHash,
  selection,sourceStatus:'ORIGINAL_GRANT_SELECTION_NOT_EFFECT_OWNERSHIP',effectBinding:null,effectOwnershipEstablished:false as const,
  runtimeSupported:false as const,canStart:false as const};
 const packet={...body,packetHash:await digest(body)};
 if(json(await read())!==json(row))return fail('stale');
 return packet;
}
export type AugmentSelectionSource=Awaited<ReturnType<typeof captureAugmentSelectionSource>>;
/** Trusted independently captured original/hash, never caller-asserted ownership. No current DB/catalog read.
 * Combat checkpoint owner must actually persist and pin this packet; this reader does not install that writer. */
export async function readPinnedAugmentSelectionSource(input:unknown,original:AugmentSelectionSource,expectedHash:unknown,trust:unknown):Promise<AugmentSelectionSource>{
 const t=trustSchema.parse(trust),expected=hash.parse(expectedHash),frozen=structuredClone(original);
 let candidate:string;try{candidate=json(input);}catch{return fail('original_conflict');}
 if(candidate.length>1024*1024)return fail('capacity');
 const {packetHash,...body}=frozen;
 if(frozen.version!==augmentSelectionSourceVersion||frozen.accountId!==t.accountId||frozen.environment!==t.environment
  ||frozen.runId!==t.runId||frozen.eventId!==t.eventId||packetHash!==expected||await digest(body)!==packetHash||candidate!==json(frozen))return fail('original_conflict');
 return frozen;
}

