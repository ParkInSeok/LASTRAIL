import {expect,it} from 'vitest';
import {planUnmodifiedO19Transfer} from '../../apps/game-api/src/shield-transfer-plan';
const layer=(id:string,amount:number,kind='KYLE_ORIGINAL',expiresAtMs=100)=>({layerId:id,sourceId:'original:'+id,creationEventId:id,sourceKind:kind,amount,expiresAtMs,o15Eligible:true});
const input=(overrides:Record<string,unknown>={})=>({eventId:'ultimate-1',atMs:10,donorId:'donor',recipientId:'recipient',donorAvailable:true,recipientAvailable:true,recipientMaxHp:1000,
 donorLayers:[layer('donor-layer',200)],recipientEligibleLayers:[layer('existing',220)],ownerOrder:null,...overrides});
it('moves only accepted space and keeps the unaccepted donor balance with its original origin and expiry',()=>{
 const before=input(),copy=structuredClone(before),result=planUnmodifiedO19Transfer(before);
 expect(result).toMatchObject({accepted:30,outcome:'TRANSFERRED',debits:[{layerId:'donor-layer',amount:30,remaining:170}],credits:[{sourceId:'original:donor-layer',expiresAtMs:100,amount:30,o15Eligible:true}],recipientEligibleAfter:250});
 expect(before).toEqual(copy);
});
it('keeps lawfully held shields after maximum HP decreases and never converts rejected transfer into healing',()=>{
 expect(planUnmodifiedO19Transfer(input({recipientMaxHp:500}))).toMatchObject({accepted:0,outcome:'NO_CAPACITY',recipientEligibleAfter:220,debits:[],credits:[]});
 expect(planUnmodifiedO19Transfer(input({recipientEligibleLayers:[]}))).toMatchObject({accepted:150,debits:[{remaining:50}]});
});
it('excludes O02 and expired pieces, keeps per-piece expiry, and uses no second donor',()=>{
 expect(planUnmodifiedO19Transfer(input({donorLayers:[layer('o02',100,'O02'),layer('expired',100,'T17',10)],recipientEligibleLayers:[]}))).toMatchObject({accepted:0,outcome:'NO_ELIGIBLE_BALANCE'});
 expect(planUnmodifiedO19Transfer(input({donorLayers:[layer('later',100,'O13',90),layer('first',100,'T17',80)],recipientEligibleLayers:[]}))).toMatchObject({accepted:150,credits:[{sourceId:'original:first',amount:100,expiresAtMs:80},{sourceId:'original:later',amount:50,expiresAtMs:90}]});
});
it('returns zero for unavailable recipients but rejects missing source classification',()=>{
 expect(planUnmodifiedO19Transfer(input({recipientAvailable:false}))).toMatchObject({accepted:0,outcome:'RECIPIENT_UNAVAILABLE'});
 expect(()=>planUnmodifiedO19Transfer(input({donorLayers:[layer('unknown',100,'UNKNOWN')]}))).toThrow();
 expect(()=>planUnmodifiedO19Transfer(input({recipientEligibleLayers:[layer('wrong',100,'O02')]}))).toThrow();
});
