import {canonicalJson,sha256Hex} from '@lastrail/common';
import type {PublicCatalogSnapshot} from '../../../packages/common/src/public-release-policy';
import {readPublicCatalogSnapshot,readPublicReleaseIndex} from './public-release-store';
import {CommunityStoreError} from './community-content-store';
import type {CommunityPostCreateRequest} from './community-post-contract';
/** Only existing verified, published, fixed public snapshots may back wiki references. No ID/name fabrication. */
export function communityPostReferenceVerifier(db:D1Database,environment:'DEV'|'PRD',appVersionCode?:string){
 return async(post:CommunityPostCreateRequest):Promise<D1PreparedStatement[]>=>{
  if(!post.wikiRefs.length&&post.sourceBalanceVersion===null)return [];
  const guards:D1PreparedStatement[]=[],seen=new Set<string>(),cache=new Map<string,PublicCatalogSnapshot>();
  const verify=async(snapshotId:string)=>{
   try{const snapshot=await readPublicCatalogSnapshot(db,environment,snapshotId);
    if(!snapshot||environment==='DEV'&&snapshot.appVersionCode!==appVersionCode)throw new CommunityStoreError(400,'community_wiki_reference_unavailable');
    return snapshot;
   }catch(e){if(e instanceof CommunityStoreError)throw e;throw new CommunityStoreError(503,'community_wiki_source_unavailable');}
  };
  const published=async(snapshotId:string)=>{
   const cached=cache.get(snapshotId);if(cached)return cached;const snapshot=await verify(snapshotId);
   const indexRow=await db.prepare("SELECT index_id,payload_json,payload_digest FROM public_release_indices WHERE environment=? AND json_extract(payload_json,'$.snapshotId')=? AND channel<>'INTERNAL' AND app_version_code=? ORDER BY index_id LIMIT 1").bind(environment,snapshotId,snapshot.appVersionCode).first<{index_id:string;payload_json:string;payload_digest:string}>();
   if(!indexRow)throw new CommunityStoreError(400,'community_wiki_reference_unavailable');
   let index:Awaited<ReturnType<typeof readPublicReleaseIndex>>;try{index=await readPublicReleaseIndex(db,environment,indexRow.index_id);}catch{throw new CommunityStoreError(503,'community_wiki_source_unavailable');}
   if(!index||index.snapshotId!==snapshotId||index.channel!==snapshot.channel||index.appVersionCode!==snapshot.appVersionCode||index.releaseId!==snapshot.contentReleaseId)throw new CommunityStoreError(503,'community_wiki_source_unavailable');
   if(!seen.has(snapshotId)){seen.add(snapshotId);const source=await db.prepare('SELECT payload_json,payload_digest FROM public_catalog_snapshots WHERE environment=? AND snapshot_id=?').bind(environment,snapshotId).first<{payload_json:string;payload_digest:string}>();if(!source||source.payload_json!==canonicalJson(snapshot)||await sha256Hex(source.payload_json)!==source.payload_digest)throw new CommunityStoreError(503,'community_wiki_source_unavailable');
    guards.push(db.prepare(`INSERT INTO community_content_write_guard(valid) SELECT CASE WHEN
     EXISTS(SELECT 1 FROM public_catalog_snapshots WHERE environment=? AND snapshot_id=? AND payload_json=? AND payload_digest=?)
     AND EXISTS(SELECT 1 FROM public_release_indices WHERE environment=? AND index_id=? AND payload_json=? AND payload_digest=?) THEN 1 ELSE 0 END`).bind(environment,snapshotId,source.payload_json,source.payload_digest,environment,indexRow.index_id,indexRow.payload_json,indexRow.payload_digest));
   }cache.set(snapshotId,snapshot);return snapshot;
  };
  for(const ref of post.wikiRefs){const snapshot=await published(ref.snapshotId);if(!snapshot.items.some(item=>item.id===ref.itemId))throw new CommunityStoreError(400,'community_wiki_reference_unavailable');}
  if(post.sourceBalanceVersion!==null){const rows=await db.prepare(`SELECT snapshot_id FROM public_catalog_snapshots WHERE environment=? AND json_extract(payload_json,'$.sourceBalanceVersion')=? ${environment==='DEV'?"AND json_extract(payload_json,'$.appVersionCode')=?":''} AND EXISTS(SELECT 1 FROM public_release_indices i WHERE i.environment=public_catalog_snapshots.environment AND json_extract(i.payload_json,'$.snapshotId')=public_catalog_snapshots.snapshot_id AND i.channel<>'INTERNAL') ORDER BY snapshot_id LIMIT 1`).bind(environment,post.sourceBalanceVersion,...(environment==='DEV'?[appVersionCode!]:[])).all<{snapshot_id:string}>();
   if(!rows.success)throw new CommunityStoreError(503,'community_wiki_source_unavailable');let found=false;for(const row of rows.results){try{const snapshot=await published(row.snapshot_id);if(snapshot.sourceBalanceVersion===post.sourceBalanceVersion){found=true;break;}}catch(e){if(e instanceof CommunityStoreError&&e.status===400)continue;throw e;}}if(!found)throw new CommunityStoreError(400,'community_wiki_reference_unavailable');
  }return guards;
 };
}