import {expect,it} from 'vitest';
import app from '../../apps/game-api/src/index';
import {bRouteFixture} from '../helpers/combat-b-route-fixture';
import {startDeparture} from '../../apps/game-api/src/departure';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {bNativeO22ExecutorVersion} from '../../apps/game-api/src/combat-b-native-o22-executor';

it('dispatches public B START to the installed registry without inventing an executable or writing a run',async()=>{
 const f=await bRouteFixture();try{
  f.sql.run("UPDATE operation_previews SET preview_json=?,status='ACTIVE',expires_at=? WHERE id='LOCAL_B12_PREVIEW'",[JSON.stringify(f.snapshot),Date.now()+60000]);
  const before=f.scalar('SELECT count(*) FROM runs');
  const response=await app.request('/v1/runs/start',{method:'POST',headers:{...f.headers,'Content-Type':'application/json','Idempotency-Key':'registered-start'},body:JSON.stringify({operationPreviewId:'LOCAL_B12_PREVIEW'})},f.env);
  expect(response.status).toBe(409);expect(await response.json()).toMatchObject({errorCode:'combat_runtime_source_unavailable'});
  expect(f.scalar('SELECT count(*) FROM runs')).toBe(before);
 }finally{f.close();}
});
it('public B START dispatches the persisted native executor and rejects an unconsumed catalog version',async()=>{
 const f=await bRouteFixture();try{
  f.sql.run("UPDATE operation_previews SET preview_json=?,status='ACTIVE',expires_at=? WHERE id='LOCAL_B12_PREVIEW'",[JSON.stringify(f.snapshot),Date.now()+60000]);
  // Synthetic registry input, not a supported operational profile or START.
  const source=structuredClone(f.pin!.source);source.profile.executorVersion=bNativeO22ExecutorVersion;
  source.profile.definition.runtimeAugmentCatalog={complete:true,catalogVersion:'augment.utility-definitions.v9.unapproved'};
  const text=canonicalJson(source);
  f.sql.run('INSERT INTO combat_runtime_sources VALUES(?,?,?,?,?,?,1,1)',[f.env.ENVIRONMENT,'a','LOCAL_B12_PREVIEW',text,await sha256Hex(text),await sha256Hex(canonicalJson(source.profile))]);
  const response=await app.request('/v1/runs/start',{method:'POST',headers:{...f.headers,'Content-Type':'application/json','Idempotency-Key':'new-executor'},body:JSON.stringify({operationPreviewId:'LOCAL_B12_PREVIEW'})},f.env);
  expect(response.status).toBe(409);expect(await response.json()).toMatchObject({errorCode:'combat_b_native_o22_consumer_catalog_unsupported'});
  expect(f.scalar('SELECT count(*) FROM runs')).toBe(1);
 }finally{f.close();}
});
it('installs the registry in original B departure preparation while retaining the runtime gate',async()=>{
 const f=await bRouteFixture();try{
  f.sql.run("UPDATE operation_previews SET preview_json=?,status='ACTIVE',expires_at=? WHERE id='LOCAL_B12_PREVIEW'",[JSON.stringify(f.snapshot),Date.now()+60000]);
  const initial=await f.env.DB.prepare("SELECT * FROM operation_previews WHERE id='LOCAL_B12_PREVIEW'").first<Parameters<typeof startDeparture>[2]>();
  const snapshot=JSON.parse(f.originalSnapshot);delete snapshot.bCombatPin;delete snapshot.departureResponse.bCombatPin;
  await expect(startDeparture(f.env.DB,'a',initial!,'registered-preparation',async(_p,runId)=>({...snapshot.departureResponse,runId}),undefined,'DEV')).rejects.toThrow('combat_runtime_source_unavailable');
  expect(f.scalar('SELECT count(*) FROM runs')).toBe(1);
 }finally{f.close();}
});
