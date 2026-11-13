import {describe,expect,it} from 'vitest';
import {buildPublicCatalogSnapshot,createPublicReleaseRevision,PublicCatalogError} from '../../packages/common/src/public-release-policy';

const digest='sha256:'+'a'.repeat(64);
const verified={environment:'DEV' as const,channel:'PUBLIC',appVersionCode:'123',contentReleaseId:'release-1',sourceBalanceVersion:'balance-1',sourceDigest:digest,
  publicSchemaVersion:1 as const,templateVersion:'template-1',localeVersion:'ko-1',items:[{id:'aug-1',kind:'AUGMENT' as const,name:'Example',description:'Public',category:'combat',unit:'count',values:[{label:'stacks',value:'2'}],conditions:['active'],stackLimit:2,compatibleWith:['op-1'],acquisition:['run'],iconPath:'/assets/icon.png'}]};

describe('public release core',()=>{
 it('fails closed without a verified release manifest and catalog adapter',async()=>{
  await expect(buildPublicCatalogSnapshot(verified,undefined)).rejects.toMatchObject({code:'public_release_source_unavailable'});
 });
 it('allows only public catalog fields and makes content-addressed snapshots',async()=>{
  const adapter={verify:async()=>verified};
  const a=await buildPublicCatalogSnapshot(verified,adapter);
  expect(a.snapshotId).toMatch(/^pcs_[a-f0-9]{64}$/u);
  expect(await buildPublicCatalogSnapshot(verified,adapter)).toEqual(a);
  await expect(buildPublicCatalogSnapshot({...verified,items:[{...verified.items[0],privateKey:'SECRET'}]},adapter)).rejects.toBeInstanceOf(PublicCatalogError);
 });
 it('records rollback as a new index revision without rewriting old versions',async()=>{
  const adapter={verify:async()=>verified};
  const a=await buildPublicCatalogSnapshot(verified,adapter);
  const first=await createPublicReleaseRevision({environment:'DEV',channel:'PUBLIC',appVersionCode:'123',revision:1,releaseId:'release-1',snapshotId:a.snapshotId,disclosureIds:[],publishedAt:100},null);
  const rollback=await createPublicReleaseRevision({environment:'DEV',channel:'PUBLIC',appVersionCode:'123',revision:2,releaseId:'release-1',snapshotId:a.snapshotId,disclosureIds:[],publishedAt:200},first);
  expect(rollback.previousIndexId).toBe(first.indexId);
  expect(rollback.indexId).not.toBe(first.indexId);
  await expect(createPublicReleaseRevision({environment:'DEV',channel:'PUBLIC',appVersionCode:'123',revision:2,releaseId:'release-1',snapshotId:a.snapshotId,disclosureIds:[],publishedAt:300},rollback)).rejects.toBeInstanceOf(PublicCatalogError);
 });
});


