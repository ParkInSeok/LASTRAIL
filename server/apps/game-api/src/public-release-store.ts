import {canonicalJson,sha256Hex} from '@lastrail/common';
import {readProbabilityDisclosure} from './probability-disclosure-store';
import {buildPublicCatalogSnapshot,createPublicReleaseRevision,validatePublicCatalogSnapshot,validatePublicReleaseIndex,
  PublicCatalogError,type PublicCatalogSnapshot,type PublicReleaseIndex,type PublicReleaseSource} from '../../../packages/common/src/public-release-policy';

type Stored={payload_json:string;payload_digest:string;environment:string;channel?:string;app_version_code?:string};
const fail=(code:string):never=>{throw new PublicCatalogError(code);};
async function decode<T>(row:Stored,validate:(value:unknown)=>Promise<T>):Promise<T>{
  try{
    if(row.payload_json.length>2_000_000||await sha256Hex(row.payload_json)!==row.payload_digest)throw new Error('digest');
    return await validate(JSON.parse(row.payload_json));
  }catch{return fail('public_release_storage_invalid');}
}
export async function readPublicCatalogSnapshot(db:D1Database,environment:'DEV'|'PRD',id:string){
  const row=await db.prepare('SELECT environment,payload_json,payload_digest FROM public_catalog_snapshots WHERE environment=? AND snapshot_id=?')
    .bind(environment,id).first<Stored>();
  if(!row)return null;
  const snapshot=await decode(row,validatePublicCatalogSnapshot);
  if(snapshot.environment!==row.environment)fail('public_release_storage_invalid');
  return snapshot;
}
export async function readPublicReleaseIndex(db:D1Database,environment:'DEV'|'PRD',id:string){
  const row=await db.prepare('SELECT environment,channel,app_version_code,payload_json,payload_digest FROM public_release_indices WHERE environment=? AND index_id=?')
    .bind(environment,id).first<Stored>();
  if(!row)return null;
  const index=await decode(row,validatePublicReleaseIndex);
  if(index.environment!==row.environment||index.channel!==row.channel||index.appVersionCode!==row.app_version_code)
    fail('public_release_storage_invalid');
  return index;
}
export async function readCurrentPublicReleaseIndex(db:D1Database,environment:'DEV'|'PRD',channel:string,appVersionCode:string){
  const head=await db.prepare('SELECT index_id,revision FROM public_release_heads WHERE environment=? AND channel=? AND app_version_code=?')
    .bind(environment,channel,appVersionCode).first<{index_id:string;revision:number}>();
  if(!head)return null;
  const index=await readPublicReleaseIndex(db,environment,head.index_id);
  if(!index||index.revision!==head.revision||index.channel!==channel||index.appVersionCode!==appVersionCode)fail('public_release_storage_invalid');
  return index;
}
/** Stage only a server-verified actual release. A missing adapter never creates a row. */
export async function stagePublicCatalogSnapshot(db:D1Database,candidate:unknown,source?:PublicReleaseSource):Promise<PublicCatalogSnapshot>{
  const snapshot=await buildPublicCatalogSnapshot(candidate,source);
  const existing=await readPublicCatalogSnapshot(db,snapshot.environment,snapshot.snapshotId);
  if(existing){if(canonicalJson(existing)!==canonicalJson(snapshot))fail('public_release_storage_invalid');return existing;}
  const payload=canonicalJson(snapshot);
  if(payload.length>2_000_000)fail('public_catalog_invalid');
  try{await db.prepare('INSERT INTO public_catalog_snapshots(snapshot_id,environment,payload_json,payload_digest) VALUES(?,?,?,?)')
    .bind(snapshot.snapshotId,snapshot.environment,payload,await sha256Hex(payload)).run();}
  catch{
    const raced=await readPublicCatalogSnapshot(db,snapshot.environment,snapshot.snapshotId);
    if(raced&&canonicalJson(raced)===payload)return raced;
    fail('public_release_storage_unavailable');
  }
  return snapshot;
}
/** CAS-select a verified snapshot and exact approved disclosure IDs as the current release. */
export async function publishPublicReleaseIndex(db:D1Database,input:{environment:'DEV'|'PRD';channel:string;appVersionCode:string;releaseId:string;snapshotId:string;
  disclosureIds:string[];publishedAt:number},expectedIndexId:string|null):Promise<PublicReleaseIndex>{
  const current=await readCurrentPublicReleaseIndex(db,input.environment,input.channel,input.appVersionCode);
  if((current?.indexId??null)!==expectedIndexId)fail('public_release_revision_conflict');
  const snapshot=await readPublicCatalogSnapshot(db,input.environment,input.snapshotId);
  if(!snapshot||snapshot.contentReleaseId!==input.releaseId||snapshot.channel!==input.channel||snapshot.appVersionCode!==input.appVersionCode)fail('public_release_snapshot_unavailable');
  for(const id of input.disclosureIds){
    if(!await readProbabilityDisclosure(db,input.environment,id))fail('public_release_disclosure_unavailable');
  }
  const index=await createPublicReleaseRevision({...input,revision:(current?.revision??0)+1},current);
  const payload=canonicalJson(index);
  const allDisclosureChecks=input.disclosureIds.map(()=>
    'EXISTS(SELECT 1 FROM probability_disclosures WHERE environment=? AND disclosure_id=?)').join(' AND ');
  const guard=`INSERT INTO public_release_guard SELECT CASE WHEN ${current
    ?'EXISTS(SELECT 1 FROM public_release_heads WHERE environment=? AND channel=? AND app_version_code=? AND index_id=? AND revision=?)'
    :'NOT EXISTS(SELECT 1 FROM public_release_heads WHERE environment=? AND channel=? AND app_version_code=?)'}
    AND EXISTS(SELECT 1 FROM public_catalog_snapshots WHERE environment=? AND snapshot_id=?)
    ${allDisclosureChecks?' AND '+allDisclosureChecks:''} THEN 1 ELSE 0 END`;
  const bindings:unknown[]=current?[input.environment,input.channel,input.appVersionCode,current.indexId,current.revision]:[input.environment,input.channel,input.appVersionCode];
  bindings.push(input.environment,input.snapshotId);
  for(const id of input.disclosureIds)bindings.push(input.environment,id);
  try{
    await db.batch([
      db.prepare(guard).bind(...bindings),
      db.prepare('INSERT INTO public_release_indices(index_id,environment,channel,app_version_code,revision,payload_json,payload_digest) VALUES(?,?,?,?,?,?,?)')
        .bind(index.indexId,index.environment,index.channel,index.appVersionCode,index.revision,payload,await sha256Hex(payload)),
      current?db.prepare('UPDATE public_release_heads SET index_id=?,revision=? WHERE environment=? AND channel=? AND app_version_code=? AND index_id=? AND revision=?')
        .bind(index.indexId,index.revision,index.environment,index.channel,index.appVersionCode,current.indexId,current.revision)
        :db.prepare('INSERT INTO public_release_heads(environment,channel,app_version_code,index_id,revision) VALUES(?,?,?,?,?)')
          .bind(index.environment,index.channel,index.appVersionCode,index.indexId,index.revision),
      db.prepare('DELETE FROM public_release_guard'),
    ]);
  }catch{
    if((await readCurrentPublicReleaseIndex(db,input.environment,input.channel,input.appVersionCode))?.indexId!==expectedIndexId)
      fail('public_release_revision_conflict');
    fail('public_release_storage_unavailable');
  }
  return index;
}




