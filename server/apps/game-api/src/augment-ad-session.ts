import {z} from 'zod';
import {canonicalJson} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {verifyAdmobCallback} from './admob-ssv';
import {augmentExtraTargetSchema} from './solo-augment-selection';
const id=z.string().min(1).max(200);
const time=z.number().int().nonnegative().max(Number.MAX_SAFE_INTEGER);
export const augmentAdSessionSchema=z.strictObject({version:z.literal('augment-ad-session.v1'),intentId:id,
 target:augmentExtraTargetSchema,
 binding:z.strictObject({adUnit:id,customData:z.string().min(1).max(256),userId:id,rewardAmount:id,rewardItem:id}),
 createdAt:time,startedAt:time,expiresAt:time,callbackDeadline:time,
}).superRefine((s,c)=>{if(s.startedAt<s.createdAt||s.expiresAt<=s.startedAt||s.callbackDeadline<s.expiresAt)c.addIssue({code:'custom',message:'invalid_augment_ad_session_time'});});
/** Internal evidence verification only. Session must come from the trusted persisted
 * issuer/start approval, never an HTTP body. No default lifetime or start approval is
 * invented here. The consumer must still atomically recheck session state/deadline,
 * reserve the global provider receipt, and apply the original grant exactly once.
 * This result is not ExtraApproval and cannot authorize a reroll on its own. */
export async function verifyAugmentAdSession(rawQuery:string,trustedKey:(id:string)=>Promise<string|null>,
 sessionInput:unknown,expectedTargetInput:unknown,serverEnvironment:unknown,receivedAtInput:unknown){
 const session=augmentAdSessionSchema.parse(sessionInput),target=augmentExtraTargetSchema.parse(expectedTargetInput);
 const environment=z.enum(['LOCAL','DEV','PRD']).parse(serverEnvironment),receivedAt=time.parse(receivedAtInput);
 if(session.target.environment!==environment)throw new ActivityError(403,'augment_ad_environment_mismatch');
 if(canonicalJson(session.target)!==canonicalJson(target))throw new ActivityError(409,'augment_ad_target_mismatch');
 const callback=await verifyAdmobCallback(rawQuery,trustedKey,session.binding);
 if(callback.timestamp<session.startedAt||callback.timestamp>session.expiresAt||receivedAt>session.callbackDeadline||callback.timestamp>receivedAt)
  throw new ActivityError(409,'augment_ad_expired');
 return {intentId:session.intentId,accountId:session.target.accountId,environment,transactionId:callback.transactionId,
  provider:callback.provider,timestamp:callback.timestamp};
}
