import {z} from 'zod';
export const communityModerationVersion='community.moderation.v1.20261008' as const;
export const communityModerationId=z.string().regex(/^[A-Za-z0-9_-]{1,128}$/u);
const revision=z.number().int().positive().max(Number.MAX_SAFE_INTEGER-1);
const reason=z.string().trim().min(1).max(500);
export const communityReviewCommand=z.strictObject({expectedRevision:revision,status:z.enum(['IN_REVIEW','ACTIONED','DISMISSED']),reason});
export const communityModerateCommand=z.strictObject({expectedRevision:revision,action:z.enum(['HIDE','RESTORE']),reason});
const base={contractVersion:z.literal(communityModerationVersion),environment:z.enum(['DEV','PRD']),resourceId:communityModerationId,revision:z.number().int().min(2).safe()};
export const communityModerationReceipt=z.discriminatedUnion('resourceType',[
 z.strictObject({...base,resourceType:z.literal('REPORT'),status:z.enum(['IN_REVIEW','ACTIONED','DISMISSED'])}),
 z.strictObject({...base,resourceType:z.literal('CONTENT'),visibility:z.enum(['VISIBLE','HIDDEN']),rendererVersion:z.literal('community.markdown.v1.20261008')})
]);
export type CommunityModerationReceipt=z.infer<typeof communityModerationReceipt>;
/** Approved transition rules only; terminal outcomes never imply a content change or final notification. */
export function canReviewCommunityReport(previous:string,next:string){return previous==='OPEN'&&next==='IN_REVIEW'||previous==='IN_REVIEW'&&(next==='ACTIONED'||next==='DISMISSED');}
export const communityCommentLockVersion='community.comment-lock.v1.20261009' as const;
export const communityCommentLockCommand=z.strictObject({expectedRevision:revision,action:z.enum(['LOCK_COMMENTS','UNLOCK_COMMENTS']),reason});
export const communityCommentLockReceipt=z.strictObject({contractVersion:z.literal(communityCommentLockVersion),environment:z.enum(['DEV','PRD']),resourceType:z.literal('CONTENT'),resourceId:communityModerationId,kind:z.literal('POST'),revision:z.number().int().min(2).safe(),visibility:z.enum(['VISIBLE','HIDDEN','REVIEW_PENDING']),commentLocked:z.boolean(),rendererVersion:z.literal('community.markdown.v1.20261008')});
export const communityAdminCommandReceipt=z.union([communityModerationReceipt,communityCommentLockReceipt]);
export type CommunityAdminCommandReceipt=z.infer<typeof communityAdminCommandReceipt>;