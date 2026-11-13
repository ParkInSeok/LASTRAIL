import {describe,expect,it} from 'vitest';
import {setup} from '../helpers/departure-fixture';
import {readCurrentPublicReleaseIndex,readPublicCatalogSnapshot,readPublicReleaseIndex,stagePublicCatalogSnapshot,publishPublicReleaseIndex} from '../../apps/game-api/src/public-release-store';

const candidate={environment:'DEV' as const,channel:'PUBLIC',appVersionCode:'123',contentReleaseId:'release-1',sourceBalanceVersion:'balance-1',sourceDigest:'sha256:'+'a'.repeat(64),
 publicSchemaVersion:1 as const,templateVersion:'template-1',localeVersion:'ko-1',items:[{id:'aug-1',kind:'AUGMENT' as const,name:'Augment',description:'Public explanation',category:'combat',unit:'count',values:[{label:'stacks',value:'2'}],conditions:['active'],stackLimit:2,compatibleWith:['op-1'],acquisition:['run'],iconPath:null}]};
const source={verify:async()=>candidate};
describe('public release persistence',()=>{
 it('never stages without actual source verification and preserves immutable history across rollback',async()=>{
  const f=await setup(false,true,null);
  await expect(stagePublicCatalogSnapshot(f.env.DB,candidate)).rejects.toMatchObject({code:'public_release_source_unavailable'});
  expect(f.scalar('SELECT count(*) FROM public_catalog_snapshots')).toBe(0);
  const a=await stagePublicCatalogSnapshot(f.env.DB,candidate,source);
  const b=await stagePublicCatalogSnapshot(f.env.DB,{...candidate,contentReleaseId:'release-2',items:[{...candidate.items[0],name:'Changed'}]},
    {verify:async()=>({...candidate,contentReleaseId:'release-2',items:[{...candidate.items[0],name:'Changed'}]})});
  const input=(releaseId:string,snapshotId:string,publishedAt:number)=>({environment:'DEV' as const,channel:'PUBLIC',appVersionCode:'123',releaseId,snapshotId,disclosureIds:[],publishedAt});
  const first=await publishPublicReleaseIndex(f.env.DB,input('release-1',a.snapshotId,100),null);
  const second=await publishPublicReleaseIndex(f.env.DB,input('release-2',b.snapshotId,200),first.indexId);
  const rollback=await publishPublicReleaseIndex(f.env.DB,input('release-1',a.snapshotId,300),second.indexId);
  expect(rollback.revision).toBe(3);
  expect(rollback.previousIndexId).toBe(second.indexId);
  expect(await readCurrentPublicReleaseIndex(f.env.DB,'DEV','PUBLIC','123')).toEqual(rollback);
  expect(await readPublicReleaseIndex(f.env.DB,'DEV',first.indexId)).toEqual(first);
  expect(await readPublicCatalogSnapshot(f.env.DB,'DEV',a.snapshotId)).toEqual(a);
  expect(await readPublicReleaseIndex(f.env.DB,'PRD',first.indexId)).toBeNull();
  expect(()=>f.sql.run('DELETE FROM public_release_indices')).toThrow('public_release_index_immutable');
  expect(()=>f.sql.run('UPDATE public_catalog_snapshots SET payload_json=\'{}\'')).toThrow('public_catalog_snapshot_immutable');
 });
 it('fails closed for stale CAS, missing snapshot and invented disclosure IDs',async()=>{
  const f=await setup(false,true,null);
  const a=await stagePublicCatalogSnapshot(f.env.DB,candidate,source);
  const base={environment:'DEV' as const,channel:'PUBLIC',appVersionCode:'123',releaseId:'release-1',snapshotId:a.snapshotId,disclosureIds:[],publishedAt:100};
  await expect(publishPublicReleaseIndex(f.env.DB,{...base,snapshotId:'pcs_'+'0'.repeat(64)},null)).rejects.toMatchObject({code:'public_release_snapshot_unavailable'});
  await expect(publishPublicReleaseIndex(f.env.DB,{...base,disclosureIds:['pd_'+'0'.repeat(64)]},null)).rejects.toMatchObject({code:'public_release_disclosure_unavailable'});
  const first=await publishPublicReleaseIndex(f.env.DB,base,null);
  await expect(publishPublicReleaseIndex(f.env.DB,{...base,publishedAt:200},null)).rejects.toMatchObject({code:'public_release_revision_conflict'});
  expect(f.scalar('SELECT count(*) FROM public_release_indices')).toBe(1);
  expect(await readCurrentPublicReleaseIndex(f.env.DB,'DEV','PUBLIC','123')).toEqual(first);
 });
});


describe('public release selector isolation',()=>{
 it('never crosses channel or exact app version when selecting current releases',async()=>{
  const f=await setup(false,true,null);
  for(const [channel,appVersionCode] of [['PUBLIC','123'],['BETA','123'],['PUBLIC','124']] as const){
    const actual={...candidate,channel,appVersionCode};
    const snapshot=await stagePublicCatalogSnapshot(f.env.DB,actual,{verify:async()=>actual});
    const index=await publishPublicReleaseIndex(f.env.DB,{environment:'DEV',channel,appVersionCode,releaseId:actual.contentReleaseId,
      snapshotId:snapshot.snapshotId,disclosureIds:[],publishedAt:100},null);
    expect(await readCurrentPublicReleaseIndex(f.env.DB,'DEV',channel,appVersionCode)).toEqual(index);
  }
  expect(await readCurrentPublicReleaseIndex(f.env.DB,'DEV','PUBLIC','125')).toBeNull();
  expect(await readCurrentPublicReleaseIndex(f.env.DB,'DEV','BETA','124')).toBeNull();
  expect(await readCurrentPublicReleaseIndex(f.env.DB,'PRD','PUBLIC','123')).toBeNull();
 });
});
