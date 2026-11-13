import {writeFileSync} from 'node:fs';
import {setup} from '../../tests/helpers/departure-fixture';
import admin from '../../apps/admin-api/src/index';
import game from '../../apps/game-api/src/index';

// Synthetic PRD selection tests public routing only: no real Access, network or production database.
const f=await setup(false,true,null);
f.sql.run("INSERT INTO admin_principals VALUES('OWNER','OWNER','OWNER','ACTIVE',NULL,0,0)");
const env={...f.env,ENVIRONMENT:'PRD',ADMIN_AUTH_MODE:'LOCAL_HEADER',ADMIN_ACCESS_ISSUER:'',ADMIN_ACCESS_AUD:'',ADMIN_DELEGATED_ACCESS_ISSUER:'',ADMIN_DELEGATED_ACCESS_AUD:''};
const document={presentationRevision:1,type:'GENERAL',title:'TEST_ONLY 안내',summary:'공개 목록 요약',localizedBody:{ko:'실제 운영 공지가 아닌 격리 테스트 응답입니다.'},bodyFormat:'PLAIN_TEXT',surfaces:'WEB',visibility:'PUBLIC',channel:null,platform:null,minVersionCode:null,maxVersionCode:null,startsAt:0,endsAt:null,imageAssetId:null,action:null,popupMode:'NONE',priority:0,pinned:false};
const samples:Record<string,unknown>={};
async function capture(name:string,response:Promise<Response>){const r=await response;samples[name]={status:r.status,cacheControl:r.headers.get('Cache-Control'),body:await r.json()};}
const command=(body:object,key:string)=>admin.request('/v1/admin/notices/test_notice/commands',{method:'POST',headers:{'Cf-Access-Subject':'OWNER','Content-Type':'application/json','Idempotency-Key':key},body:JSON.stringify({environment:'PRD',reason:'TEST_ONLY internal reason',...body})},env);
const pub=(path='/v1/public/notices/test_notice?locale=ko')=>game.request(path,{}, {...env,CLIENT_ADMISSION_MODE:'ENFORCED'});
await capture('draftSaved',command({command:'SAVE_DRAFT',expectedRevision:null,document},'draft'));
await capture('draftNotPublic',pub());
await capture('published',command({command:'PUBLISH',expectedRevision:1},'publish'));
await capture('publicDetail',pub());
await capture('publicList',pub('/v1/public/notices?locale=ko'));
await capture('adminRead',admin.request('/v1/admin/notices/test_notice',{headers:{'Cf-Access-Subject':'OWNER'}},env));
await capture('newDraft',command({command:'SAVE_DRAFT',expectedRevision:2,document:{...document,title:'TEST_ONLY 다음 초안'}},'edit'));
await capture('withdrawPreservesDraft',command({command:'WITHDRAW',expectedRevision:3},'withdraw'));
await capture('withdrawnNotPublic',pub());
await capture('oldPublishReplay',command({command:'PUBLISH',expectedRevision:1},'publish'));
await capture('stillNotPublic',pub());
await capture('devUnauthenticated',game.request('/v1/public/notices?locale=ko',{}, {...env,ENVIRONMENT:'DEV'}));
writeFileSync('D:/Project/LASTRAIL/QA/PublicNoticeServer-20260921/http-fixtures.json',JSON.stringify({scope:'TEST_ONLY_LOCAL_HONO_SQLJS',samples},null,2)+'\n');
f.sql.close();