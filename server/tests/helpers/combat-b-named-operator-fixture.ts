import {readFileSync} from 'node:fs';
import {canonicalJson} from '@lastrail/common';
import {setup} from './departure-fixture';
import app from '../../apps/game-api/src/index';
import {BRefitStore,refitHttpVersion} from '../../apps/game-api/src/combat-b-refit-store';
import {LocalRefitPersistenceSource} from '../../apps/game-api/src/combat-b-refit-persistence-source';
import {LocalFixtureBSelectionCatalog} from '../../apps/game-api/src/combat-b-selection-catalog';
import {bNamedOperatorSourceVersion,captureBNamedOperatorSource} from '../../apps/game-api/src/combat-b-named-operator-source';
export const namedRequest={operatorIds:['op_kyle','op_sable'],expectedFormationRevision:1,expectedLoadoutRevision:1};
/** Synthetic ownership, approved Lv1 bases; no boosted combat values or operating writes. */
export async function namedFixture(withRefit=false){
 const f=await setup(true,false),ids=['op_kyle','op_sable','op_reina','op_eve','op_sera'];
 ids.forEach((id,slot)=>{f.sql.run("INSERT INTO account_operators(account_id,operator_id,acquired_at) VALUES('a',?,0)",[id]);f.sql.run("INSERT INTO operator_training_core_slots(account_id,slot_index,operator_id,trained_level) VALUES('a',?,?,1)",[slot,id]);});
 f.sql.run("UPDATE account_formation SET formation_json=? WHERE account_id='a'",[JSON.stringify({schemaVersion:1,operators:ids.map((operatorId,slot)=>({slot,operatorId}))})]);
 const headers={...f.headers,'X-B-Named-Operator-Version':bNamedOperatorSourceVersion};
 const post=(input:unknown=namedRequest,extra:Record<string,string>={},path='/v1/combat/b-named-operator-source')=>app.request(path,{method:'POST',headers:{...headers,...extra},body:JSON.stringify(input)},f.env);
 let refitOwner: {read:(accountId:string)=>ReturnType<BRefitStore['read']>}|undefined;
 if(withRefit){
  for(const file of ['combat-b-selection.sql','combat-b-refit.sql','combat-b-refit-http.sql'])f.sql.run(readFileSync('apps/game-api/local/'+file,'utf8'));
  const catalog={version:'LOCAL_NAMED_CATALOG',status:'ISOLATED_LOCAL_FIXTURE',unlockedSlotCount:5,carriages:['car_combat','car_gun'].map(carriageId=>({carriageId,occupants:['OPERATOR','CARGO']}))};
  const selection={version:'combat.b-placement.v2.20261002',mountedTurrets:[],placements:['op_kyle','op_sable'].map((operatorId,slot)=>({slot,carriageId:slot===0?'car_combat':'car_gun',occupant:{kind:'OPERATOR',operatorId}}))};
  const source={status:'ISOLATED_LOCAL_FIXTURE',ranks:['BCB01','BCB02','BCB03','BCB04','BCB05','BCB06','BCB07'].map(type=>({type,rank:1})),instances:['car_combat','car_gun'].map((carriageId,slot)=>({slot,instanceId:'LOCAL_NAMED_CAR_'+slot,carriageId}))};
  f.sql.run("INSERT INTO combat_b_fixture_catalog VALUES('a',1,?)",[JSON.stringify(catalog)]);
  f.sql.run("INSERT INTO combat_b_selections VALUES('a',?,?,?,1,1,0)",['combat.b-selection-http.v1.20261002',canonicalJson(selection),'LOCAL_NAMED_CATALOG']);
  f.sql.run("INSERT INTO combat_b_fixture_refits VALUES('a',1,1,?)",[JSON.stringify(source)]);
  const principal={accountId:'a',sessionId:'s',accessExpiresAt:Number(f.scalar("SELECT expires_at FROM auth_sessions WHERE id='s'"))};
  const owner=new BRefitStore(f.env.DB,new LocalRefitPersistenceSource(),new LocalFixtureBSelectionCatalog()),read=await owner.read(principal);
  await owner.save(principal,{version:refitHttpVersion,expectedRevision:0,selectionRevision:read.selectionRevision,rankRevision:read.rankSource.revision,definitionHash:read.definitionHash,sourceHash:read.sourceHash,refits:[{slot:0,type:'BCB03',rank:1},{slot:1,type:'BCB03',rank:1}]},'LOCAL_NAMED_ONLY');
  refitOwner={read:accountId=>{if(accountId!==principal.accountId)throw new Error('LOCAL_OWNER_ACCOUNT_MISMATCH');return owner.read(principal);}};
 }
 return {...f,headers,post,refitOwner,capture:()=>captureBNamedOperatorSource(f.env.DB,'a',namedRequest,refitOwner)};
}