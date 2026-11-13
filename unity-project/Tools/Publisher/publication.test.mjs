import test from 'node:test';
test('unresolved upload is not replayed on resume',async()=>{
  const f=fixture();let attempts=0;
  f.play.uploadBundle=async()=>{attempts++;throw new Error('UPLOAD_TIMEOUT');};
  await assert.rejects(runPublication(manifest(),f),/UPLOAD_TIMEOUT/);
  await assert.rejects(runPublication(manifest(),f),/PLAY_UPLOAD_RECONCILIATION_REQUIRED/);
  assert.equal(attempts,1);
});
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { runPublication } from './publication.mjs';

const canonical = value => JSON.stringify(value, Object.keys(value).sort());
const appBytes=Buffer.from('fixture');
const appHash=createHash('sha256').update(appBytes).digest('hex');
const contentRelease = {status:'APPROVED',releaseId:'fixture',contentVersion:'fixture',catalogUrl:'https://dev-content.lastrail.app/android/releases/fixture/catalog.bin',catalogHash:'sha256:'+'b'.repeat(64),minimumVersionCode:'2',maximumVersionCode:'2'};
const manifest = () => ({schemaVersion:1,environment:'DEV',package:'app.lastrail.game.dev',version:'0.1.1',versionCode:2,format:'aab',track:'internal',appPath:'fixture.aab',appSha256:appHash,appBytes:appBytes.length,signerSha256:'c'.repeat(64),candidatePath:'candidate.json',candidateSha256:'d'.repeat(64),contentRelease});
const identity = 'sha256:'+createHash('sha256').update(canonical(contentRelease)).digest('hex');
function fixture() {
  const events = [];
  let receipt;
  let row;
  let uploaded = false;
  let committed = false;
  const store = {load:async()=>structuredClone(receipt),save:async value=>{receipt=structuredClone(value);},lock:async()=>async()=>{}};
  const registry = {
    get:async()=>({target:'DEV',registryStatus:'CONFIGURED',revision:'"etag"',minimumVersionCode:'1',rows:row?[row]:[],canMutate:true,
      capabilities:{register:true,promote:false},distributionPolicy:{supportedStatuses:['STABLE','DISABLED'],mutableStatuses:['STABLE','DISABLED'],initialStatus:'STABLE',testerGate:false}}),
    register:async body=>{events.push('register');assert.equal(body.minimumVersionCode,undefined);row={version:body.version,versionCode:body.versionCode,status:'STABLE',contentRelease:body.contentRelease,artifactIdentity:identity};}
  };
  const content = {audit:async()=>events.push('audit'),upload:async()=>events.push('upload-content'),verify:async()=>events.push('verify-content')};
  const play = {
    readCommittedReleases:async()=>({releases:committed?[{track:'internal',releaseName:'LASTRAIL_DEV_0.1.1_2_'+appHash.slice(0,12),activeArtifacts:[{versionCode:2}],releaseLifecycleState:'RELEASE_LIFECYCLE_STATE_PUBLISHED'}]:[]}),
    createEdit:async()=>{events.push('create-edit');return{id:'edit1'};}, getEdit:async()=>({id:'edit1'}),
    listTracks:async()=>({tracks:[{track:'internal',releases:[]}]}),
    listBundles:async()=>({bundles:uploaded?[{versionCode:2,sha256:appHash}]:[]}),
    uploadBundle:async()=>{events.push('upload-aab');uploaded=true;return{versionCode:2,sha256:appHash};},
    updateInternal:async(id,release)=>{events.push('track');return{track:'internal',releases:[release]};},
    validateEdit:async()=>{events.push('validate');return{id:'edit1'};},
    commitEdit:async()=>{events.push('commit');committed=true;return{id:'edit1'};}
  };
  return {events,store,registry,content,play,verifyApp:async()=>{events.push('verify-app');return Buffer.from('fixture');},
    get receipt(){return receipt;},setRow:value=>{row=value;}};
}

test('DEV verified content and registry precede Play, resume never rebuilds/reuploads', async()=>{
  const f=fixture();
  const result=await runPublication(manifest(),f);
  assert.equal(result.phase,'PLAY_RELEASE_CONFIRMED');
  assert.deepEqual(f.events,['verify-app','audit','upload-content','verify-content','register','create-edit','upload-aab','track','validate','commit']);
  await runPublication(manifest(),f);
  assert.equal(f.events.filter(x=>x==='upload-aab').length,1);
  assert.equal(f.events.filter(x=>x==='create-edit').length,1);
  assert.equal(f.events.filter(x=>x==='register').length,1);
});

test('PRD and production input rejected before any adapter call',async()=>{
  for(const override of [{environment:'PRD'},{package:'app.lastrail.game'},{track:'production'},{format:'apk'}]) {
    const f=fixture();await assert.rejects(runPublication({...manifest(),...override},f));assert.deepEqual(f.events,[]);
  }
});

test('content failure never registers or calls Play',async()=>{
  const f=fixture();f.content.verify=async()=>{throw new Error('HASH_MISMATCH');};
  await assert.rejects(runPublication(manifest(),f),/HASH_MISMATCH/);
  assert.equal(f.events.includes('register'),false);assert.equal(f.events.includes('create-edit'),false);
});

test('disabled exact row is not reactivated',async()=>{
  const f=fixture();f.setRow({version:'0.1.1',versionCode:'2',status:'DISABLED',contentRelease,artifactIdentity:identity});
  await assert.rejects(runPublication(manifest(),f),/REGISTRY_DISABLED/);
  assert.equal(f.events.includes('register'),false);assert.equal(f.events.includes('create-edit'),false);
});

test('same version code with different Play bytes is a hard conflict',async()=>{
  const f=fixture();f.play.listBundles=async()=>({bundles:[{versionCode:2,sha256:'e'.repeat(64)}]});
  await assert.rejects(runPublication(manifest(),f),/PLAY_CODE_CONFLICT/);
  assert.equal(f.events.includes('upload-aab'),false);assert.equal(f.events.includes('commit'),false);
});

test('ambiguous committed write reconciles without second commit',async()=>{
  const f=fixture();const commit=f.play.commitEdit;f.play.commitEdit=async()=>{await commit();throw Object.assign(new Error('TIMEOUT'),{status:502});};
  const result=await runPublication(manifest(),f);assert.equal(result.phase,'PLAY_RELEASE_CONFIRMED');
  await runPublication(manifest(),f);assert.equal(f.events.filter(x=>x==='commit').length,1);
});

test('missing actual internal track is not synthesized',async()=>{
  const f=fixture();f.play.listTracks=async()=>({tracks:[{track:'production',releases:[]}]});
  await assert.rejects(runPublication(manifest(),f),/PLAY_INTERNAL_TRACK_MISSING/);
  assert.equal(f.events.includes('track'),false);
});

test('receipt cannot be reused for changed manifest',async()=>{
  const f=fixture();await runPublication(manifest(),f);
  await assert.rejects(runPublication({...manifest(),appSha256:'f'.repeat(64)},f),/RECEIPT_IDENTITY_CONFLICT/);
});

test('old registry policy and unacknowledged legacy stop before upload',async()=>{
  for(const override of [{distributionPolicy:undefined},{registryStatus:'LEGACY'},{canMutate:false}]){
    const f=fixture(),get=f.registry.get;f.registry.get=async()=>({...await get(),...override});
    await assert.rejects(runPublication(manifest(),f));assert.equal(f.events.includes('upload-content'),false);
  }
});

test('unconfirmed commit is not repeated or converted to success on resume',async()=>{
  const f=fixture();f.play.commitEdit=async()=>{f.events.push('commit');throw new Error('TIMEOUT');};
  await assert.rejects(runPublication(manifest(),f),/PLAY_COMMIT_UNCONFIRMED/);
  await assert.rejects(runPublication(manifest(),f),/PLAY_COMMIT_UNCONFIRMED/);
  assert.equal(f.events.filter(x=>x==='commit').length,1);
});

test('remote newer code and unrelated unfinished internal release block writes',async()=>{
  const f=fixture();f.play.listBundles=async()=>({bundles:[{versionCode:9,sha256:'b'.repeat(64)}]});
  await assert.rejects(runPublication(manifest(),f),/PLAY_REMOTE_VERSION_CODE_CONFLICT/);
  assert.equal(f.events.includes('upload-aab'),false);
  const other=fixture();other.play.listTracks=async()=>({tracks:[{track:'internal',releases:[{name:'manual-draft',status:'draft',versionCodes:['1']}]}]});
  await assert.rejects(runPublication(manifest(),other),/PLAY_TRACK_HAS_UNFINISHED_RELEASE/);
  assert.equal(other.events.includes('track'),false);
});

test('registry ambiguous registration reconciles exact STABLE row without another POST',async()=>{
  const f=fixture(),register=f.registry.register;f.registry.register=async body=>{await register(body);throw Object.assign(new Error('AMBIGUOUS'),{status:502});};
  await runPublication(manifest(),f);assert.equal(f.events.filter(x=>x==='register').length,1);
});
