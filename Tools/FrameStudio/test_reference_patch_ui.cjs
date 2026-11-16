const assert=require('node:assert/strict');const {patchContext,patchRequest,surfaceRequest}=require('./web/reference-patch.js');
(async()=>{
 const p={id:'p',revision:2,frames:[{versions:[{asset:'a'}]},{versions:[{asset:'b'}]}]};
 let c=patchContext(p,0);assert.equal(c.source_asset,'a');assert.equal(patchContext(null,0),null);
 const data=await patchRequest(c,()=>c,async x=>x,'donor','mask','ref','evidence');assert.equal(data.source_index,0);assert.equal(data.donor_png,'donor');
 const old=c;await assert.rejects(patchRequest(old,()=>c,async x=>{c=patchContext(p,1);return x;},'d','m','r','n'),/바뀌었습니다/);
 await assert.rejects(patchRequest(null,()=>null,async()=>'',null,null,'',''),/선택/);
 await assert.rejects(patchRequest(c,()=>c,async()=>{throw Error('read failure')},'d','m','r','n'),/read failure/);
 const plan=JSON.stringify({reference_sha256:'sha',surface_map:{surfaces:[]}});
 const surf=await surfaceRequest(c,()=>c,async x=>x,'mask','ref',plan);assert.equal(surf.mask_png,'mask');assert.equal(surf.reference_sha256,'sha');assert.equal(surf.reference_asset,'ref');
 await assert.rejects(surfaceRequest(c,()=>c,async x=>x,'mask','ref','bad'),/JSON/);
 await assert.rejects(surfaceRequest(c,()=>null,async x=>x,'mask','ref',plan),/바뀌었습니다/);
 console.log('Reference patch UI: snapshot, success, selection race, empty selection and read failure passed.');
})().catch(e=>{console.error(e);process.exitCode=1;});
