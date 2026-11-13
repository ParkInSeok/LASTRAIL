import type initSqlJs from 'sql.js';
/** Explicit synthetic policy fixture; never imported by production or operational migrations. */
export function testMaintenancePolicy(sql:initSqlJs.Database,blocked=false){
 sql.run("INSERT OR IGNORE INTO admin_principals VALUES('maintenance_test','maintenance_test','OWNER','ACTIVE',NULL,0,0)");
 const rev=Number(sql.exec("SELECT revision FROM maintenance_runtime_heads WHERE environment='DEV' AND application_id='LASTRAIL'")[0]?.values[0]?.[0]??0);
 if(!blocked&&rev)return;
 if(blocked){
  sql.run("INSERT OR IGNORE INTO maintenance_plan_revisions VALUES('DEV','maintenance_test',1,'{}',0)");
 }
 sql.run('INSERT INTO maintenance_runtime_revisions VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?)',['DEV','LASTRAIL',rev+1,blocked?'ACTIVE':'NORMAL',blocked?'maintenance_test':null,blocked?1:null,blocked?1:null,blocked?2:null,blocked?3:null,null,'maintenance_test','TEST_ONLY',0]);
 sql.run("INSERT INTO maintenance_runtime_heads VALUES('DEV','LASTRAIL',?) ON CONFLICT(environment,application_id) DO UPDATE SET revision=excluded.revision",[rev+1]);
}
