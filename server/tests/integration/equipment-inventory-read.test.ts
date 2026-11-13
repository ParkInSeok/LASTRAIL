import {expect,it} from 'vitest';
import {writeFileSync} from 'node:fs';
import app from '../../apps/game-api/src/index';
import {setup} from '../helpers/departure-fixture';
import {equipmentCoreStatVersion} from '../../apps/game-api/src/equipment-run-stat-owner';
async function fixture(){const f=await setup(true),quote=await(await f.preview()).json(),start=await(await f.start(quote.operationPreviewId,'inventory-read')).json();return {...f,runId:start.runId as string};}
it('authenticates owned inventory, exposes stable keys, and never mutates inventory',async()=>{
 const f=await fixture();
 f.sql.run(`INSERT INTO run_equipment_instances(run_id,instance_id,source_item_id,definition_id,definition_version,category_id,set_tag,star,locked,location_kind,revision) VALUES(?,'item','source','gear',?,'ATK','SET01',2,0,'STORED',1)`,[f.runId,equipmentCoreStatVersion]);
 const url='http://localhost/v1/runs/'+f.runId+'/equipment';
 const missing=await app.request(url,{},f.env);expect(missing.status).toBe(401);
 const response=await app.request(url,{headers:f.headers},f.env);expect(response.status).toBe(200);expect(response.headers.get('Cache-Control')).toBe('no-store');
 const value=await response.json();expect(value).toMatchObject({accountId:'a',runId:f.runId,usedSlots:1,revision:1,items:[{presentation:{name:'공격력 장비',iconKey:'equipment.category.atk',effects:['공격력 +25%','고유효과·인챈트 정보 확인 필요']}}]});
 if(process.env.LASTRAIL_EQUIPMENT_READ_QA)writeFileSync(process.env.LASTRAIL_EQUIPMENT_READ_QA,JSON.stringify(value,null,2));
 expect(f.scalar('SELECT revision FROM run_equipment_inventory')).toBe(1);
 expect((await app.request(url+'?accountId=b',{headers:f.headers},f.env)).status).toBe(400);
 f.sql.run('UPDATE runs SET account_id=? WHERE id=?',['b',f.runId]);
 expect((await app.request(url,{headers:f.headers},f.env)).status).toBe(409);
});
it('unknown category and definition version do not invent art or numeric effects',async()=>{
 const f=await fixture();f.sql.run(`INSERT INTO run_equipment_instances(run_id,instance_id,source_item_id,definition_id,definition_version,category_id,set_tag,star,locked,location_kind,revision) VALUES(?,'item','source','gear','unknown','unknown','SET01',1,0,'STORED',1)`,[f.runId]);
 const result=await app.request('http://localhost/v1/runs/'+f.runId+'/equipment',{headers:f.headers},f.env);
 expect(result.status).toBe(200);expect((await result.json()).items[0].presentation).toMatchObject({name:'종류 미확인 장비',iconKey:null,effects:['기본 효과 설명 확인 필요','고유효과·인챈트 정보 확인 필요']});
});
