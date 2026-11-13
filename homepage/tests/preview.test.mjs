import test from 'node:test';
import assert from 'node:assert/strict';
import {createPreviewServer} from '../preview.mjs';
test('local routes, independent static status, isolated QA and serving boundary',async t=>{
 const server=createPreviewServer();await new Promise(r=>server.listen(0,'127.0.0.1',r));t.after(()=>new Promise(r=>server.close(r)));
 const base=`http://127.0.0.1:${server.address().port}`;
 for(const route of ['/','/wiki','/support','/community'])assert.equal((await fetch(base+route)).status,200);
 let r=await fetch(base+'/__qa/web-maintenance/wiki');assert.equal(r.status,503);assert.equal(r.headers.get('retry-after'),'120');let html=await r.text();assert.match(html,/홈페이지 점검 중/);assert.doesNotMatch(html,/<script/);
 r=await fetch(base+'/__qa/web-maintenance/status');assert.equal(r.status,200);assert.match(await r.text(),/홈페이지/);
 r=await fetch(base+'/__qa/game-maintenance/wiki');assert.equal(r.status,200);assert.match(await r.text(),/게임 점검/);
 r=await fetch(base+'/__qa/read-only/support');html=await r.text();assert.equal(r.status,200);assert.match(html,/읽기 전용/);assert.match(html,/disabled/);
 r=await fetch(base+'/__qa/stale/status');assert.match(await r.text(),/현재 미확인/);
 for(const path of ['/package.json','/tests/status.test.mjs','/.git/config','/unknown','/src/nope.mjs']) assert.equal((await fetch(base+path)).status,404);
 assert.equal((await fetch(base+'/support',{method:'POST',body:'private'})).status,405);
 assert.equal((await fetch(base+'/status')).status,200);
 assert.match(await (await fetch(base+'/status')).text(),/확인된 기록 없음/);
 assert.equal((await fetch(base+'/__qa/web-maintenance/robots.txt')).status,200);
});

test('preview serves approved API consumer modules without enabling remote connections',async t=>{
 const server=createPreviewServer();await new Promise(r=>server.listen(0,'127.0.0.1',r));t.after(()=>new Promise(r=>server.close(r)));
 const base=`http://127.0.0.1:${server.address().port}`;
 for(const name of ['api-http','api-contracts','web-api','web-session-model','support-model','support-read-model','maintenance-login-model','community-delete-model','community-edit-model','community-command-model','community-contracts','community-comment-model','community-read-model','community-post-model','community-block-model','community-public-read-model','community-consent-model','community-consent-contracts','community-report-model','community-report-contracts']){
 const r=await fetch(base+'/src/'+name+'.mjs');assert.equal(r.status,200);assert.match(r.headers.get('content-type'),/javascript/);assert.match(r.headers.get('content-security-policy'),/connect-src 'self'/);
 }
});








