import { expect } from 'vitest';
import app from '../../apps/game-api/src/index';
import { hashPassword } from '../../apps/game-api/src/password-auth';
import { setup } from './departure-fixture';
import {installSyntheticCommunityConsent} from './community-consent-fixture';
import {communityCreationEligibilityVersion} from '../../apps/game-api/src/community-consent';
const origin='https://dev.lastrail.app', path='/v1/web/community/contents/p/delete';
export async function communityWebFixture(action:'delete'|'edit'='delete'){
 const f=await setup(false,true,null),hash=await hashPassword('TestOnlyStrongPassword-123!');
 f.sql.run(`INSERT INTO auth_password_credentials(account_id,login_id_normalized,password_hash,password_hash_algorithm,password_hash_params_json,recovery_email_normalized,created_at,updated_at) VALUES('a','tester',?,?,?,?,0,0)`,[hash.passwordHash,hash.algorithm,hash.paramsJson,'tester@example.invalid']);
 f.sql.run("INSERT INTO account_tester_entitlements VALUES('a','DEV','123','ACTIVE',?,NULL,'test',1,0,0)",[Date.now()+60000]);
 await installSyntheticCommunityConsent(f.sql,f.env.DB);
 const state={kind:'POST',id:'p',parentId:null,accountId:'a',publicAuthorId:'public',visibility:'VISIBLE',commentLocked:false,revision:1,title:'Title',sanitizedBody:'Secret body'};
 f.sql.run('INSERT INTO community_contents(id,account_id,revision,state_json) VALUES(?,?,?,?)',['p','a',1,JSON.stringify(state)]);
 const env={...f.env,WEB_ALLOWED_ORIGIN:origin,CLIENT_ADMISSION_MODE:'ENFORCED' as const};
 const login=await app.request('/v1/web/sessions/password',{method:'POST',headers:{Origin:origin,'Content-Type':'application/json'},body:JSON.stringify({loginId:'tester',password:'TestOnlyStrongPassword-123!'})},env);
 expect(login.status).toBe(200);const csrf=(await login.json() as {csrfToken:string}).csrfToken;
 const headers={'X-Community-Creation-Eligibility-Version':communityCreationEligibilityVersion,Origin:origin,Cookie:login.headers.get('Set-Cookie')!.split(';')[0]!,'X-CSRF-Token':csrf,'X-LastRail-App-Version-Code':'123','Content-Type':'application/json','X-Community-Content-Version':action==='delete'?'community.content-store.v1.20261008':'community.content-edit.v1.20261008'};
 const call=(body:unknown={commandId:'delete-1',expectedRevision:1},extra:Record<string,string>={},url=path.replace('/delete','/'+action))=>app.request(url,{method:'POST',headers:{...headers,...extra},body:JSON.stringify(body)},env);
 return {...f,env,gameHeaders:f.headers,headers,call};
}

