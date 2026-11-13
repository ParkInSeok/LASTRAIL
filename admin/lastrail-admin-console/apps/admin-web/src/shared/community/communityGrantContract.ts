import {z} from 'zod';
export const COMMUNITY_GRANT_VERSION='community.moderation-grants.v1.20261008';
export const communityPrincipalId=z.string().regex(/^[A-Za-z0-9_-]{1,128}$/);
const revision=z.number().int().nonnegative().safe(),permission=z.literal('COMMUNITY_MODERATE'),environment=z.enum(['DEV','PRD']);
export const communityGrantCommandSchema=z.strictObject({action:z.enum(['GRANT','REVOKE']),permission,expectedRevision:revision,reason:z.string().trim().min(1).max(500)});
export type CommunityGrantCommand=z.infer<typeof communityGrantCommandSchema>;
export const communityGrantStateSchema=z.object({contractVersion:z.literal(COMMUNITY_GRANT_VERSION),environment,state:z.object({principalId:communityPrincipalId,displayName:z.string().nullable(),role:z.string(),status:z.string(),permission,granted:z.boolean(),revision,lastChange:z.object({actorId:z.string().nullable(),at:z.number().nonnegative().safe(),reason:z.string().nullable(),outcome:z.string(),environment}).nullable()})});
const receiptSchema=z.object({contractVersion:z.literal(COMMUNITY_GRANT_VERSION),environment,result:z.object({principalId:communityPrincipalId,permission,granted:z.boolean(),revision,outcome:z.enum(['APPLIED','NO_CHANGE'])})});
export type CommunityGrantState=z.infer<typeof communityGrantStateSchema>;
export function assertCommunityGrantState(value:unknown,env:'DEV'|'PRD',id:string){const r=communityGrantStateSchema.parse(value);if(r.environment!==env||r.state.principalId!==id)throw new Error('검수 권한 조회 응답 불일치');return r;}
export function assertCommunityGrantReceipt(value:unknown,env:'DEV'|'PRD',id:string,command:CommunityGrantCommand){const r=receiptSchema.parse(value);if(r.environment!==env||r.result.principalId!==id||r.result.granted!==(command.action==='GRANT')||r.result.revision!==command.expectedRevision+(r.result.outcome==='APPLIED'?1:0))throw new Error('검수 권한 원결과 불일치');return r;}
