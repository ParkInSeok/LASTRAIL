import {z} from 'zod';
export const MODERATION_VERSION='community.moderation.v1.20261008';
export const moderationId=z.string().regex(/^[A-Za-z0-9_-]{1,128}$/);
const revision=z.number().int().positive().safe(),expected=revision.max(Number.MAX_SAFE_INTEGER-1),reason=z.string().trim().min(1).max(500),status=z.enum(['OPEN','IN_REVIEW','ACTIONED','DISMISSED']),targetType=z.enum(['POST','COMMENT']);
export const reviewCommand=z.strictObject({status:z.enum(['IN_REVIEW','ACTIONED','DISMISSED']),expectedRevision:expected,reason});
export const moderateCommand=z.strictObject({action:z.enum(['HIDE','RESTORE']),expectedRevision:expected,reason});
export type ModerationBody=z.infer<typeof reviewCommand>|z.infer<typeof moderateCommand>;
export type Resource='REPORT'|'CONTENT';
export type ReadKind='LIST'|Resource;
const base={contractVersion:z.literal(MODERATION_VERSION),environment:z.enum(['DEV','PRD'])};
const item=z.strictObject({caseId:moderationId,targetType,targetId:moderationId,targetRevision:revision,status,revision,submittedAt:z.number().int().nonnegative().safe()});
const snapshot=z.strictObject({targetType,targetId:moderationId,revision,title:z.string().max(200).nullable(),sanitizedBody:z.string().min(1).max(100000),rendererVersion:z.literal('community.markdown.v1.20261008'),observedAt:z.number().int().nonnegative().safe()});
const list=z.strictObject({...base,items:z.array(item).max(50),nextCursor:z.string().min(1).max(1024).nullable()});
const report=z.strictObject({...base,report:item,evidence:z.strictObject({snapshot,reasonCode:z.enum(['HARASSMENT','PRIVACY_EXPOSURE','ILLEGAL_CONTENT','CHILD_EXPLOITATION','SPAM','IMPERSONATION','RIGHTS_INFRINGEMENT']),optionalDescription:z.string().max(4000).nullable()})});
const content=z.strictObject({...base,content:z.strictObject({id:moderationId,kind:targetType,parentId:moderationId.nullable(),revision,visibility:z.enum(['VISIBLE','REVIEW_PENDING','HIDDEN','AUTHOR_DELETED']),commentLocked:z.boolean()})});
export const moderationQuery=z.strictObject({status:status.optional(),limit:z.number().int().min(1).max(50).optional(),cursor:z.string().min(1).max(1024).optional()});
export type ModerationQuery=z.infer<typeof moderationQuery>;
export function parseModerationRead(kind:ReadKind,value:unknown,env:'DEV'|'PRD',id?:string){
 const data=kind==='LIST'?list.parse(value):kind==='REPORT'?report.parse(value):content.parse(value);
 if(data.environment!==env||kind==='REPORT'&&'report'in data&&data.report.caseId!==id||kind==='CONTENT'&&'content'in data&&data.content.id!==id)throw new Error('검수 조회 응답 불일치');
 if('report'in data){const r=data.report,s=data.evidence.snapshot;if(r.targetId!==s.targetId||r.targetType!==s.targetType||r.targetRevision!==s.revision||r.submittedAt!==s.observedAt)throw new Error('신고 증거 응답 불일치');}
 return data;
}
export const moderationReceipt=z.discriminatedUnion('resourceType',[
 z.strictObject({...base,resourceType:z.literal('REPORT'),resourceId:moderationId,revision:revision.min(2),status:z.enum(['IN_REVIEW','ACTIONED','DISMISSED'])}),
 z.strictObject({...base,resourceType:z.literal('CONTENT'),resourceId:moderationId,revision:revision.min(2),visibility:z.enum(['VISIBLE','HIDDEN']),rendererVersion:z.literal('community.markdown.v1.20261008')})]);
export function parseModerationReceipt(value:unknown,env:'DEV'|'PRD',kind:Resource,id:string,body:ModerationBody){const r=moderationReceipt.parse(value);if(r.environment!==env||r.resourceType!==kind||r.resourceId!==id||r.revision!==body.expectedRevision+1||r.resourceType==='REPORT'&&(!('status'in body)||r.status!==body.status)||r.resourceType==='CONTENT'&&(!('action'in body)||r.visibility!==(body.action==='HIDE'?'HIDDEN':'VISIBLE')))throw new Error('검수 원결과 불일치');return r;}
export function moderationPath(kind:ReadKind,id?:string,write=false){if(kind==='LIST')return '/api/community/reports';moderationId.parse(id);return `/api/community/${kind==='REPORT'?'reports':'contents'}/${id}${write?(kind==='REPORT'?'/review':'/moderate'):''}`;}
