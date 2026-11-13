import {z} from 'zod';
export const offlineClaimCompletionVersion='offline.claim.v2.20261021';
const nat=z.number().int().nonnegative().safe();
const account=z.strictObject({kind:z.literal('PATCH'),coverage:z.tuple([z.literal('CURRENCIES')]),beforeMutationSequence:nat,mutationSequence:nat,
 currencies:z.array(z.strictObject({currency:z.string().min(1),balance:nat}))});
export const offlineClaimCompletionSchema=z.object({accepted:z.literal(true),claimStatus:z.literal('CLAIMED'),contractVersion:z.literal(offlineClaimCompletionVersion),
 refreshRequired:z.literal(false),rewardTransactionId:z.string().min(1),serverClaimedAt:z.string().datetime(),owners:z.strictObject({account,
 offline:z.strictObject({kind:z.literal('PATCH'),lastClaimAt:z.string().datetime(),nextEligibleAt:z.string().datetime()})}),invalidates:z.tuple([z.literal('OFFLINE_REWARD_PREVIEW')])}).passthrough()
 .refine(r=>r.owners.account.mutationSequence===r.owners.account.beforeMutationSequence+1);
/** Stored with the original reward in the existing transaction. No new ledger,
 * current-state GET, or recomputation during duplicate recovery. */
export function completeOfflineClaim(response:{serverClaimedAt:string;minimumClaimSeconds:number;[key:string]:unknown},sequence:number,currencies:{currency:string;balance:number}[]){
 return offlineClaimCompletionSchema.parse({...response,contractVersion:offlineClaimCompletionVersion,refreshRequired:false,
  owners:{account:{kind:'PATCH',coverage:['CURRENCIES'],beforeMutationSequence:sequence-1,mutationSequence:sequence,currencies},
   offline:{kind:'PATCH',lastClaimAt:response.serverClaimedAt,nextEligibleAt:new Date(Date.parse(response.serverClaimedAt)+response.minimumClaimSeconds*1000).toISOString()}},invalidates:['OFFLINE_REWARD_PREVIEW']});
}
