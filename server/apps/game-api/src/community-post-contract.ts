import {z} from 'zod';
import {communityMarkdownVersion} from './community-markdown';
export const communityPostCreateVersion='community.post-create.v1.20261008' as const;
// Stable technical IDs for the approved four boards; labels never grant rights.
export const communityBoards=Object.freeze({free:'자유',qna:'질문',guide_build:'공략·빌드',feedback:'피드백'});
const id=z.string().regex(/^[A-Za-z0-9_-]{1,128}$/u),version=z.string().regex(/^[A-Za-z0-9][A-Za-z0-9_.-]{0,255}$/u);
export const communityPostCreateRequestSchema=z.strictObject({commandId:id,boardId:z.enum(['free','qna','guide_build','feedback']),title:z.string().max(200),markdown:z.string().min(1).max(20000),spoiler:z.boolean().default(false),wikiRefs:z.array(z.strictObject({snapshotId:z.string().regex(/^pcs_[a-f0-9]{64}$/u),itemId:version})).default([]),sourceBalanceVersion:version.nullable().default(null)}).refine(v=>new Set(v.wikiRefs.map(r=>r.snapshotId+':'+r.itemId)).size===v.wikiRefs.length);
export type CommunityPostCreateRequest=z.infer<typeof communityPostCreateRequestSchema>;
export const communityPostCreateReceiptSchema=z.strictObject({contractVersion:z.literal(communityPostCreateVersion),rendererVersion:z.literal(communityMarkdownVersion),commandId:id,contentId:id,parentId:z.null(),boardId:z.enum(['free','qna','guide_build','feedback']),detailsHash:z.string().regex(/^[0-9a-f]{64}$/u),kind:z.literal('POST'),revision:z.literal(1),visibility:z.enum(['VISIBLE','REVIEW_PENDING']),createdAt:z.number().int().nonnegative().safe()});
export function communityPostDetails(post:CommunityPostCreateRequest){return {boardId:post.boardId,spoiler:post.spoiler,wikiRefs:post.wikiRefs.map(r=>({...r})),sourceBalanceVersion:post.sourceBalanceVersion};}