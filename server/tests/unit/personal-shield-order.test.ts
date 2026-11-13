import {expect,it} from 'vitest';
import {orderPersonalShieldLayers,captureShieldLayerContract} from '../../apps/game-api/src/personal-shield-order';
const layer=(layerId:string,expiresAtMs:number|null,creationEventId=layerId)=>({layerId,scope:'PERSONAL',recipientId:'op',sourceId:'source:'+layerId,creationEventId,amount:100,expiresAtMs});
it('orders only live eligible personal layers by expiration then original event',()=>{
 const layers=[layer('b',50),layer('a',50),layer('infinite',null),layer('expired',10)];const before=structuredClone(layers);
 expect(orderPersonalShieldLayers({recipientId:'op',atMs:10,layers,ownerOrder:null})).toEqual(['a','b','infinite']);expect(layers).toEqual(before);
});
it('preserves a complete explicit owner order without inventing shared priority',()=>{
 const layers=[layer('a',20),layer('b',30)];expect(orderPersonalShieldLayers({recipientId:'op',atMs:10,layers,ownerOrder:['b','a']})).toEqual(['b','a']);
 expect(()=>orderPersonalShieldLayers({recipientId:'op',atMs:10,layers:[{...layer('a',20),scope:'SHARED'}],ownerOrder:null})).toThrow();
 expect(()=>orderPersonalShieldLayers({recipientId:'op',atMs:10,layers:[{...layer('a',20),recipientId:'other'}],ownerOrder:null})).toThrow();
});
it('rejects incomplete owner order and ambiguous original event ties',()=>{
 const layers=[layer('a',20),layer('b',30)];expect(()=>orderPersonalShieldLayers({recipientId:'op',atMs:10,layers,ownerOrder:['b']})).toThrow();
 expect(()=>orderPersonalShieldLayers({recipientId:'op',atMs:10,layers:[layer('a',20,'same'),layer('b',20,'same')],ownerOrder:null})).toThrow();
});
it('retains original Kyle definitions and distinguishes consumption from absorption',()=>{
 expect(captureShieldLayerContract()).toMatchObject({version:'combat.personal-shield-order.v1.20261019',globalCap:null,runtimeIntegrated:false,
  contribution:{HIT_ABSORPTION:'DEFENSE',O15_CONSUMPTION:'ATTACK_ENHANCEMENT',EXPIRY:'NONE'},
  o15:{personalOnly:true,remainingBps:2000,actorMaxHpCapBps:500,baseAttackBudgetCapBps:5000},
  kyle:[{id:'op_kyle.active',shieldDurationMs:6000},{id:'op_kyle.ultimate',shieldDurationMs:8000}]});
});
