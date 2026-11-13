import {it,expect} from 'vitest';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {persistedBDepartureFixture} from '../helpers/combat-b-persisted-departure-fixture';
import {CombatRuntimeRegistry} from '../../apps/game-api/src/combat-runtime-registry';
import {createPersistedBNativeO22OriginalSourceOwner} from '../../apps/game-api/src/combat-b-native-o22-original-source';
import {createBOperationBinding,bOperationContentVersion} from '../../apps/game-api/src/combat-b-operation-binding';
import {startDeparture} from '../../apps/game-api/src/departure';
import {createBNativeO22ExecutorAdapter,bNativeO22ExecutorVersion} from '../../apps/game-api/src/combat-b-native-o22-executor';
import {augmentUtilityDefinitionVersion} from '../../apps/game-api/src/augment-utility-definition';
import {ActivityError} from '../../apps/game-api/src/activity-policy';

it.each(['injected-registry','installed-native-factory','measured-authoring'])('synthetic registered B %s keeps original identity through authenticated source preparation',async mode=>{
 const f=await persistedBDepartureFixture();try{
  const raw=JSON.parse(f.baseJson),previewId=f.newPreviewId;
  raw.contentVersion=bOperationContentVersion;raw.rewardReference={};
  raw.bOperationBinding=await createBOperationBinding(raw.operationMapContent,raw.rewardReference,1);
  const now=Date.now();
  f.sql.run("INSERT INTO operation_previews(id,account_id,mutation_id,objective,risk,balance_config_version,preview_json,status,expires_at,created_at,admission_version) VALUES(?,'a','local-retained-mutation','opn_01_last_departure','V',1,?,'ACTIVE',?,?,'departure-v4')",[previewId,JSON.stringify(raw),now+60000,now]);
  const original=createPersistedBNativeO22OriginalSourceOwner(f.env.DB,f.auth);
  let calls=0;
  if(mode!=='injected-registry'){
   f.source.profile.executorVersion=bNativeO22ExecutorVersion;
   Object.assign(f.source.profile.definition,{runtimeAugmentCatalog:{catalogVersion:augmentUtilityDefinitionVersion,complete:true}});
  }
  // Synthetic executable binding only. The installed real original producer is
  // exercised and must stop at absent physical geometry, never authorize START.
  const registry=new CombatRuntimeRegistry(f.env.DB,'LOCAL',[mode!=='injected-registry'?await createBNativeO22ExecutorAdapter(f.source.profile):{
   profileId:f.source.profile.id,profileVersion:f.source.profile.version,executorVersion:f.source.profile.executorVersion,
   profileHash:await sha256Hex(canonicalJson(f.source.profile)),
   prepare:async(source,context)=>{
    calls++;expect(source).toEqual(f.source);expect(context.runId).toBe(f.newRunId);
    await original.prepare(context);
    throw Error('unexpected_geometry_support');
   }
  }]);
  await registry.register({accountId:'a',operationPreviewId:previewId,source:f.source});
  const initial=await f.env.DB.prepare('SELECT * FROM operation_previews WHERE id=?').bind(previewId).first<Parameters<typeof startDeparture>[2]>();
  let measuredCalls=0;
  const preparation=mode==='measured-authoring'?{auth:f.auth,physicalAuthoring:{capture:async(c:{accountId:string;runId:string},owned:{accountId:string;runId:string;actor:{operatorId:string}})=>{
   measuredCalls++;expect(c.accountId).toBe('a');expect(c.runId).toBe(f.newRunId);
   expect(owned).toMatchObject({accountId:'a',runId:f.newRunId,actor:{operatorId:'op_kyle'}});
   // Stop at the independently owned missing operating authoring boundary.
   // Reaching this owner must not consume fuel or authorize a START.
   throw new ActivityError(503,'operating_authoring_not_supplied');
  }}}:mode==='installed-native-factory'?{auth:f.auth}:{auth:f.auth,sourceOwner:registry};
  await expect(startDeparture(f.env.DB,'a',initial!,'candidate-original',async(_row,runId)=>{
   expect(runId).toBe(f.newRunId);
   return {...raw.departureResponse,runId,operationPreviewId:previewId,contentVersion:bOperationContentVersion,rewardReference:raw.rewardReference,bOperationBinding:raw.bOperationBinding};
  },undefined,'LOCAL',undefined,undefined,preparation)).rejects.toThrow(mode==='measured-authoring'?'operating_authoring_not_supplied':'combat_b_native_o22_geometry_composition_source_unavailable');
  expect(calls).toBe(mode==='injected-registry'?1:0);
  expect(measuredCalls).toBe(mode==='measured-authoring'?1:0);
  expect(f.scalar('SELECT count(*) FROM runs WHERE id=?',[f.newRunId])).toBe(0);
  expect(f.scalar("SELECT fuel FROM fuel_state WHERE account_id='a'")).toBe(30);
 }finally{f.close();}
});
