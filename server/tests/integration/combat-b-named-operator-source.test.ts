import {afterEach,expect,it} from 'vitest';
import {Buffer} from 'node:buffer';
import type initSqlJs from 'sql.js';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {Hono} from 'hono';
import {namedFixture,namedRequest} from '../helpers/combat-b-named-operator-fixture';
import {bNamedOperatorDefinition,captureBNamedOperatorSource,readPinnedBNamedOperatorSource} from '../../apps/game-api/src/combat-b-named-operator-source';
import {registerBNamedOperatorSourceRoutes} from '../../apps/game-api/src/combat-b-named-operator-source-routes';
import {readOwnedCombatProfile} from '../../apps/game-api/src/combat-owned-profile';
import {captureOwnedCombatBaseDefinitions} from '../../apps/game-api/src/combat-owned-base-definitions';
import {publishCombatDefinitionContract} from '../../apps/game-api/src/combat-definition-contract';
import {bInterceptAbilityDefinition} from '../../apps/game-api/src/combat-b-intercept-ability';
const dbs:initSqlJs.Database[]=[];afterEach(()=>{for(const db of dbs.splice(0))db.close();});
async function fixture(refit=false){const f=await namedFixture(refit);dbs.push(f.sql);return f;}
it('supplies approved Lv1 real bases, original ability provenance, explicit missing native fields',async()=>{
 const f=await fixture(),p=await f.capture();expect(p.actors.map(a=>a.operatorId)).toEqual(['op_kyle','op_sable']);
 expect(p.actors[0]?.permanentStats).toEqual({maxHp:1400,attack:{numerator:'3600000000',denominator:'100000000'},defense:250,basicIntervalMs:1800});
 expect(p.actors[1]?.permanentStats).toEqual({maxHp:1600,attack:{numerator:'3200000000',denominator:'100000000'},defense:282,basicIntervalMs:2000});
 const original=await bInterceptAbilityDefinition();expect(p.actors.map(a=>a.interception.definition)).toEqual(original.abilities);
 for(const a of p.actors){expect(a.effectiveStats).toBeNull();expect(a.equipment).toBeNull();expect(a.basic.maximumDistanceMm).toBeNull();expect(a.interception.acceptedTiming).toBeNull();expect(Object.values(a.capabilities).every(c=>!c.nativeSupported)).toBe(true);expect(a.capabilities.movement.definitionAvailable).toBe(false);}
 expect(p).toMatchObject({runtimeSupported:false,canStart:false});
});
it('reuses exact growth arithmetic while preserving original C3 Sable unknown definition',async()=>{
 const f=await fixture();f.sql.run("UPDATE operator_training_core_slots SET trained_level=10 WHERE account_id='a'");f.sql.run("UPDATE account_operators SET promotion=2,skill_training=3,revision=2 WHERE account_id='a' AND operator_id='op_kyle'");
 const before=await readOwnedCombatProfile(f.env.DB,'a',{expectedFormationRevision:1,expectedLoadoutRevision:1}),p=await f.capture();
 expect(p.actors[0]?.permanentStats).toEqual(before.operators[0]?.permanentStats);expect(p.actors[0]?.permanentStats?.maxHp).toBe(1999);expect(p.actors[0]?.permanentStats?.attack.numerator).toBe('4970700000');expect(p.actors[0]?.owned.skillMagnitude).toEqual({numerator:'11000',denominator:'10000'});
 expect(before.operators[1]?.permanentStats).toBeNull();expect(await readOwnedCombatProfile(f.env.DB,'a',{expectedFormationRevision:1,expectedLoadoutRevision:1})).toEqual(before);
 const d=await bNamedOperatorDefinition();expect(d.originalBase).toEqual(captureOwnedCombatBaseDefinitions());expect(d.originalDefinition).toEqual(await publishCombatDefinitionContract());expect(d.interception).toEqual(await bInterceptAbilityDefinition());
});
it('preserves approved Sable HP18 shield, passive once, strongest guard and removed shared cap',async()=>{
 const d=await bNamedOperatorDefinition(),s=d.operators.find(o=>o.operatorId==='op_sable')!;
 expect(s.skills?.[0]).toMatchObject({shieldCasterHpBps:1800,shieldOutputPassiveBps:1000,shieldDurationMs:6000,guardStanceBps:2500,guardGroup:'STRONGEST_SAME_GROUP',shieldCommonHpCap:null});expect(s.skills?.[1]).toMatchObject({guardStanceBps:2000,guardStanceDurationMs:8000,shieldGranted:false});expect(canonicalJson(s)).not.toContain('shieldCasterDef');
});
it('never substitutes level1 for incomplete original training',async()=>{const f=await fixture();f.sql.run("DELETE FROM operator_training_core_slots WHERE account_id='a' AND slot_index=4");const p=await f.capture();for(const a of p.actors){expect(a.owned.level).toBeNull();expect(a.permanentStats).toBeNull();expect(a.missing).toContain('COMPLETE_FIVE_TRAINING_CORE_SOURCE');}});
it('binds original persisted LOCAL B placement and BCB without inventing accepted haste',async()=>{const f=await fixture(true),p=await f.capture();expect(p.refit).not.toBeNull();for(const a of p.actors){expect(a.bcb).toMatchObject({type:'BCB03',rank:1,rankRevision:1,selectionRevision:1,sourceStatus:'ISOLATED_LOCAL_FIXTURE'});expect(a.placement?.carriageInstanceId).toMatch(/^LOCAL_NAMED_CAR_/u);expect(a.interception.originalHasteSources).toBeNull();expect(a.missing).not.toContain('ORIGINAL_B_PLACEMENT_AND_BCB_REFIT');}f.sql.run("UPDATE combat_b_fixture_refits SET revision=2 WHERE account_id='a'");await expect(f.capture()).rejects.toMatchObject({code:'combat_b_named_refit_conflict'});});
it('rejects source changes across awaited original refit read instead of mixing captures',async()=>{
 const f=await fixture(true);let changed=false;const owner={read:async(accountId:string)=>{const result=await f.refitOwner!.read(accountId);if(!changed){changed=true;f.sql.run("UPDATE account_operators SET revision=2 WHERE account_id='a' AND operator_id='op_kyle'");}return result;}};
 await expect(captureBNamedOperatorSource(f.env.DB,'a',namedRequest,owner)).rejects.toMatchObject({code:'combat_owned_source_changed'});
});
it('rejects wrong owner, non-selected named, unowned selected, stale revision and claimed stats',async()=>{
 const f=await fixture();await expect(captureBNamedOperatorSource(f.env.DB,'b',namedRequest)).rejects.toMatchObject({code:'combat_owned_selection_missing'});
 await expect(captureBNamedOperatorSource(f.env.DB,'a',{...namedRequest,expectedFormationRevision:2})).rejects.toMatchObject({code:'combat_owned_selection_changed'});
 expect((await f.post({...namedRequest,accountId:'b'})).status).toBe(400);expect((await f.post({...namedRequest,attack:50000})).status).toBe(400);expect((await f.post({...namedRequest,operatorIds:['op_kyle','op_kyle']})).status).toBe(400);
 f.sql.run("UPDATE account_formation SET formation_json=? WHERE account_id='a'",[JSON.stringify({schemaVersion:1,operators:['op0','op_sable','op_reina','op_eve','op_sera'].map((operatorId,slot)=>({slot,operatorId}))})]);await expect(f.capture()).rejects.toMatchObject({code:'combat_b_named_operator_not_selected'});
 f.sql.run("DELETE FROM operator_training_core_slots WHERE account_id='a' AND operator_id='op_sable'");f.sql.run("DELETE FROM account_operators WHERE account_id='a' AND operator_id='op_sable'");await expect(f.capture()).rejects.toMatchObject({code:'combat_owned_operator_not_owned'});
});
it('rejects stale source and definition hashes and preserves original snapshot after growth changes',async()=>{
 const f=await fixture(),p=await f.capture();await expect(captureBNamedOperatorSource(f.env.DB,'a',{...namedRequest,expectedDefinitionHash:'0'.repeat(64)})).rejects.toMatchObject({code:'combat_b_named_definition_changed'});
 f.sql.run("UPDATE account_operators SET promotion=1,revision=2 WHERE account_id='a' AND operator_id='op_kyle'");await expect(captureBNamedOperatorSource(f.env.DB,'a',{...namedRequest,expectedOwnedSourceHash:p.ownedSourceHash})).rejects.toMatchObject({code:'combat_owned_source_changed'});
 const fresh=await f.capture();expect(fresh.packetHash).not.toBe(p.packetHash);expect(await readPinnedBNamedOperatorSource(p,p,p.packetHash)).toEqual(p);expect(p.actors[0]?.permanentStats?.maxHp).toBe(1400);
});
it('strict reader rejects nested injection, rehashed stat replacement, swapped trusted hash and borrowed mutation',async()=>{
 const f=await fixture(),p=await f.capture(),injected=structuredClone(p);Object.assign(injected.actors[0]!,{extra:true});await expect(readPinnedBNamedOperatorSource(injected,p,p.packetHash)).rejects.toMatchObject({code:'combat_b_named_packet_invalid'});
 const forged=structuredClone(p);forged.actors[0]!.baseDefinition.hp=1000000;const {packetHash,...body}=forged;void packetHash;forged.packetHash=await sha256Hex(canonicalJson(body));await expect(readPinnedBNamedOperatorSource(forged,p,forged.packetHash)).rejects.toMatchObject({code:'combat_b_named_original_packet_conflict'});await expect(readPinnedBNamedOperatorSource(p,p,'0'.repeat(64))).rejects.toThrow();
 const clone=await readPinnedBNamedOperatorSource(p,p,p.packetHash);clone.actors[0]!.baseDefinition.hp=1;expect(p.actors[0]!.baseDefinition.hp).toBe(1400);
});
it('signed read contract enforces exact version, If-Match, query and session boundaries',async()=>{
 const f=await fixture(),r=await f.post(),p=await r.json() as {packetHash:string};expect(r.status).toBe(200);expect(r.headers.get('ETag')).toBe(p.packetHash);expect(r.headers.get('Cache-Control')).toBe('no-store');
 expect((await f.post(namedRequest,{'If-Match':p.packetHash})).status).toBe(200);expect((await f.post(namedRequest,{'If-Match':'0'.repeat(64)})).status).toBe(409);expect((await f.post(namedRequest,{'X-B-Named-Operator-Version':'old'})).status).toBe(409);expect((await f.post(namedRequest,{},'/v1/combat/b-named-operator-source?accountId=b')).status).toBe(400);
 expect((await f.post(namedRequest,{Authorization:''})).status).toBe(401);f.sql.run("UPDATE auth_sessions SET status='REVOKED' WHERE id='s'");expect((await f.post()).status).toBe(401);
});
it('reauthenticates after capture and rejects principal switch',async()=>{
 const f=await fixture(),h=new Hono<{Bindings:Env}>();let n=0;registerBNamedOperatorSourceRoutes(h,async()=>({accountId:++n===1?'a':'b',sessionId:'s',accessExpiresAt:Date.now()+60000}));const r=await h.request('/v1/combat/b-named-operator-source',{method:'POST',headers:f.headers,body:JSON.stringify(namedRequest)},f.env);expect(r.status).toBe(409);
});
it('source and HTTP reads do not mutate any original ownership, economy or run rows',async()=>{
 const f=await fixture(true),before=f.sql.export();await f.capture();expect((await f.post()).status).toBe(200);expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);expect(f.scalar('SELECT count(*) FROM runs')).toBe(0);expect(f.scalar("SELECT fuel FROM fuel_state WHERE account_id='a'")).toBe(30);

});