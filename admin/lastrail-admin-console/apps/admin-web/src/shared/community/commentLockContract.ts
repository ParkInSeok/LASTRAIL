import {z} from 'zod';
export const COMMENT_LOCK_VERSION='community.comment-lock.v1.20261009';
export const commentLockCommand=z.strictObject({action:z.enum(['LOCK_COMMENTS','UNLOCK_COMMENTS']),expectedRevision:z.number().int().min(1).max(Number.MAX_SAFE_INTEGER-1),reason:z.string().trim().min(1).max(500)});
export type CommentLockBody=z.infer<typeof commentLockCommand>;
const receipt=z.strictObject({contractVersion:z.literal(COMMENT_LOCK_VERSION),environment:z.enum(['DEV','PRD']),resourceType:z.literal('CONTENT'),resourceId:z.string().regex(/^[A-Za-z0-9_-]{1,128}$/),kind:z.literal('POST'),revision:z.number().int().min(2).safe(),visibility:z.enum(['VISIBLE','HIDDEN','REVIEW_PENDING']),commentLocked:z.boolean(),rendererVersion:z.literal('community.markdown.v1.20261008')});
export function assertCommentLockReceipt(value:unknown,env:'DEV'|'PRD',id:string,body:CommentLockBody){const r=receipt.parse(value);if(r.environment!==env||r.resourceId!==id||r.revision!==body.expectedRevision+1||r.commentLocked!==(body.action==='LOCK_COMMENTS'))throw new Error('댓글 잠금 원결과 불일치');return r;}
