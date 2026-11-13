import {z} from 'zod';
import {orderPersonalShieldLayers} from './personal-shield-order';
const id=z.string().min(1).max(400),nat=z.number().int().nonnegative().safe();
const kind=z.enum(['KYLE_ORIGINAL','O13','T17','O02']);
const layer=z.strictObject({layerId:id,sourceId:id,creationEventId:id,sourceKind:kind,amount:nat,expiresAtMs:nat.nullable(),o15Eligible:z.boolean()});
const request=z.strictObject({eventId:id,atMs:nat,donorId:id,recipientId:id,donorAvailable:z.boolean(),recipientAvailable:z.boolean(),recipientMaxHp:nat.positive(),
 donorLayers:z.array(layer).max(1000),recipientEligibleLayers:z.array(layer).max(1000),ownerOrder:z.array(id).max(1000).nullable()});
/** Integer shield-boundary plan for the original owner's unmodified O19 event.
 * Caller must prove source classifications, the single selected living donor,
 * manual-ultimate first effect, owned O19 and original sequence. This is not an
 * authority adapter or a second shield owner. Apply ALL debits/credits/receipt
 * atomically in that owner's transaction; never execute a replayed plan twice.
 * Explicit source dynamic-cap/modifier rules require that original processor.
 */
export function planUnmodifiedO19Transfer(input:unknown){
 const c=request.parse(input),unique=(layers:z.infer<typeof layer>[])=>new Set(layers.map(l=>l.layerId)).size===layers.length;
 if(c.donorId===c.recipientId||!unique(c.donorLayers)||!unique(c.recipientEligibleLayers)||c.recipientEligibleLayers.some(l=>l.sourceKind==='O02'))throw Error('o19_source_set_invalid');
 const live=(l:z.infer<typeof layer>)=>l.amount>0&&(l.expiresAtMs===null||l.expiresAtMs>c.atMs);
 const existingBig=c.recipientEligibleLayers.filter(live).reduce((sum,l)=>sum+BigInt(l.amount),0n);
 if(existingBig>BigInt(Number.MAX_SAFE_INTEGER))throw Error('o19_balance_overflow');
 const existing=Number(existingBig),perEvent=Number(BigInt(c.recipientMaxHp)*1500n/10000n),totalCap=Number(BigInt(c.recipientMaxHp)*2500n/10000n);
 const eligible=c.donorLayers.filter(l=>l.sourceKind!=='O02'),ids=new Set(eligible.map(l=>l.layerId));
 if(c.ownerOrder!==null&&(c.ownerOrder.length!==c.donorLayers.length||new Set(c.ownerOrder).size!==c.ownerOrder.length||c.ownerOrder.some(id=>!c.donorLayers.some(l=>l.layerId===id))))throw Error('o19_owner_order_invalid');
 const ordered=orderPersonalShieldLayers({recipientId:c.donorId,atMs:c.atMs,ownerOrder:c.ownerOrder?.filter(id=>ids.has(id))??null,
  layers:eligible.map(({sourceKind,o15Eligible,...l})=>{void sourceKind;void o15Eligible;return {...l,scope:'PERSONAL',recipientId:c.donorId};})});
 const available=new Map(eligible.map(l=>[l.layerId,l])),debits:{layerId:string;amount:number;remaining:number}[]=[],credits:(Omit<z.infer<typeof layer>,'layerId'>&{originalLayerId:string;recipientId:string;transferEventId:string})[]=[];
 let room=c.donorAvailable&&c.recipientAvailable?Math.min(perEvent,Math.max(0,totalCap-existing)):0,accepted=0;
 for(const layerId of ordered){if(room===0)break;const original=available.get(layerId)!,amount=Math.min(room,original.amount);room-=amount;accepted+=amount;
  debits.push({layerId,amount,remaining:original.amount-amount});const {layerId:originalLayerId,...origin}=original;
  credits.push({...origin,originalLayerId,recipientId:c.recipientId,transferEventId:c.eventId,amount});}
 const outcome=!c.recipientAvailable?'RECIPIENT_UNAVAILABLE':!c.donorAvailable?'DONOR_UNAVAILABLE':accepted>0?'TRANSFERRED':totalCap<=existing||perEvent===0?'NO_CAPACITY':'NO_ELIGIBLE_BALANCE';
 return {version:'combat.o19-transfer-plan.v1.20261019',approval:'31#shield-transfer-approved-20261019',status:'PREPARED_NOT_COMMITTED',eventId:c.eventId,
  donorId:c.donorId,recipientId:c.recipientId,accepted,outcome,debits,credits,recipientEligibleBefore:existing,recipientEligibleAfter:existing+accepted,
  limits:{perEvent,totalCap},contribution:'TRANSFER_NOT_CREATION_HEAL_OR_ABSORPTION',runtimeIntegrated:false};
}
