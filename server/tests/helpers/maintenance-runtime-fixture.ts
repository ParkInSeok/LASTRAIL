import initSqlJs from 'sql.js';
import {readFileSync,readdirSync} from 'node:fs';
import {SqlJsD1Database} from './growth-d1';
import {MaintenanceRuntimePolicyStore} from '../../apps/admin-api/src/maintenance-runtime-policy-store';
import {MaintenancePlanStore} from '../../apps/admin-api/src/maintenance-plan-store';
export async function runtimeFixture(){
 const SQL=await initSqlJs(),sql=new SQL.Database();
 for(const name of readdirSync('packages/db/migrations').filter(x=>x.endsWith('.sql')).sort())sql.run(readFileSync('packages/db/migrations/'+name,'utf8'));
 sql.run("INSERT INTO admin_principals VALUES('owner','owner','OWNER','ACTIVE',NULL,0,0)");
 const db=new SqlJsD1Database(sql) as unknown as D1Database;
 const actor={id:'owner',role:'OWNER' as const};
 const store=new MaintenanceRuntimePolicyStore(db,{environment:'DEV',applicationId:'LASTRAIL'});
 const plan=new MaintenancePlanStore(db,'DEV');
 await plan.command(actor,'m',{command:'SAVE_DRAFT',environment:'DEV',expectedDraftRevision:null,reason:'TEST_ONLY',plan:{applicationId:'LASTRAIL',serviceScope:'GAME',webMode:null,title:'test',summary:'test',noticeId:null,drainStartsAt:1,startsAt:2,estimatedEndAt:3,existingRunPolicy:'DRAIN_THEN_PRESERVE',statusFreshnessSeconds:60,retryAfterSeconds:15}},'draft');
 const count=(table:string)=>Number(sql.exec('SELECT count(*) FROM '+table)[0]!.values[0]![0]);
 return {sql,db,actor,store,count};
}
