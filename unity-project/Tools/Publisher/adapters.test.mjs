import test from 'node:test';
import assert from 'node:assert/strict';
import {mkdtemp,rm,readFile} from 'node:fs/promises';
import {tmpdir} from 'node:os';
import {join} from 'node:path';
import {ReceiptStore} from './receipt-store.mjs';
import {RegistryApi} from './registry-api.mjs';
import {verifySnapshot} from './local-adapters.mjs';

test('verification uses a detached snapshot and removes it after verification',async()=>{
  const source=Buffer.from('verified bytes');let path;
  const result=await verifySnapshot(source,async p=>{
    path=p;source.fill(0);
    assert.equal((await readFile(p)).toString(),'verified bytes');
  });
  assert.equal(result.toString(),'verified bytes');
  await assert.rejects(readFile(path),{code:'ENOENT'});
});

test('receipt lock is exclusive, identity state atomically replaces and persists',async()=>{
  const root=await mkdtemp(join(tmpdir(),'lastrail-publisher-test-'));
  try{
    const store=new ReceiptStore(root),m={environment:'DEV',versionCode:2};
    const unlock=await store.lock(m);
    try{
      await assert.rejects(store.lock(m),/PUBLICATION_LOCKED/);
      await store.save({phase:'FIRST'},m);await store.save({phase:'SECOND'},m);
      assert.deepEqual(await store.load(m),{phase:'SECOND'});
      assert.deepEqual(JSON.parse(await readFile(join(root,'DEV-2.json'),'utf8')),{phase:'SECOND'});
    }finally{await unlock();}
    const again=await store.lock(m);await again();
  }finally{await rm(root,{recursive:true,force:true});}
});

test('registry transport fixes DEV destination and delegated headers, never reads error bodies',async()=>{
  let request;
  const api=new RegistryApi({credentialProvider:async()=>({clientId:'fixture-client',clientSecret:'fixture-secret',userAccessJwt:'fixture-jwt'}),fetchImpl:async(url,options)=>{
    request={url,options};return{status:403,redirected:false,json:async()=>{throw new Error('body-must-not-be-read');}};
  }});
  await assert.rejects(api.get(),error=>error.message==='REGISTRY_HTTP_ERROR'&&error.status===403);
  assert.equal(request.url,'https://dev-admin.lastrail.app/v1/admin/client-releases?target=DEV');
  assert.equal(request.options.headers['X-Lastrail-User-Access-Jwt'],'fixture-jwt');
  assert.equal(request.options.redirect,'error');
  assert.ok(request.options.signal instanceof AbortSignal);
  assert.throws(()=>api.register({target:'PRD'}),/REGISTRY_REQUEST_REJECTED/);
  assert.throws(()=>api.register({target:'DEV',minimumVersionCode:'2'}),/REGISTRY_REQUEST_REJECTED/);
});

test('missing registry authorization sends no request',async()=>{
  let sent=false;
  const api=new RegistryApi({credentialProvider:async()=>null,fetchImpl:async()=>{sent=true;}});
  await assert.rejects(api.get(),/REGISTRY_AUTH_REQUIRED/);assert.equal(sent,false);
});
