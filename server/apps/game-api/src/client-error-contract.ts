import {z} from 'zod';
export const clientErrorV2='client-errors.v2.20261019';
export const clientErrorPhases={
 STATE_PREPARATION_FAILED:'PREPARATION',REQUIRED_ASSET_LOAD_FAILED:'ASSET_LOAD',REQUIRED_STATE_SAVE_FAILED:'STATE_SAVE',
 RESPONSE_CONTRACT_MISMATCH:'RESPONSE_VALIDATION',ORIGINAL_OPERATION_RECOVERY_BLOCKED:'RECOVERY',
 EFFECT_APPLICATION_FAILED:'EFFECT_APPLY',REWARD_OWNER_MISMATCH:'OWNER_RECONCILIATION',
 MANAGED_EXCEPTION:'RUNTIME',MANAGED_ASSERT:'RUNTIME',UNITY_RUNTIME_ERROR:'RUNTIME',
} as const;
const time=z.iso.datetime({offset:true}).transform(value=>Date.parse(value)).refine(value=>Number.isSafeInteger(value)&&value>=0);
const code=z.enum(Object.keys(clientErrorPhases) as [keyof typeof clientErrorPhases,...(keyof typeof clientErrorPhases)[]]);
const event=z.strictObject({kind:z.enum(['UNITY_ERROR','UNITY_EXCEPTION','UNITY_ASSERT']),occurredAt:time,lastOccurredAt:time,
 occurrenceCount:z.number().int().min(1).max(60),appVersion:z.string().max(64).regex(/^[0-9A-Za-z][0-9A-Za-z.+_-]*$/u).nullable(),
 appVersionCode:z.string().regex(/^\d{1,19}$/u).nullable(),scope:z.literal('AUTHENTICATED_CURRENT_SESSION'),errorCode:code,
 phase:z.enum(['PREPARATION','ASSET_LOAD','STATE_SAVE','RESPONSE_VALIDATION','RECOVERY','EFFECT_APPLY','OWNER_RECONCILIATION','RUNTIME']),
 platform:z.enum(['ANDROID','IOS','WINDOWS','MACOS','LINUX']).nullable(),operationKeyHash:z.string().regex(/^sha256:[a-f0-9]{64}$/u).nullable(),
}).refine(e=>e.lastOccurredAt>=e.occurredAt&&clientErrorPhases[e.errorCode]===e.phase
 && (e.errorCode!=='MANAGED_EXCEPTION'||e.kind==='UNITY_EXCEPTION')
 && (e.errorCode!=='MANAGED_ASSERT'||e.kind==='UNITY_ASSERT')
 && (e.errorCode!=='UNITY_RUNTIME_ERROR'||e.kind==='UNITY_ERROR'));
export const clientErrorV2Body=z.strictObject({version:z.literal(clientErrorV2),events:z.array(event).min(1).max(25)});
export type ClientErrorDiagnostic=Pick<z.infer<typeof event>,'errorCode'|'phase'|'lastOccurredAt'|'platform'|'operationKeyHash'>;
export function parseClientErrorV2(value:unknown){
 const result=clientErrorV2Body.safeParse(value);if(!result.success)return null;
 return result.data.events.map(({errorCode,phase,lastOccurredAt,platform,operationKeyHash,...base})=>({...base,diagnostic:{errorCode,phase,lastOccurredAt,platform,operationKeyHash}}));
}
