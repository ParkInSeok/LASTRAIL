import {z} from 'zod';
export const supportId=z.string().regex(/^[A-Za-z0-9_-]{1,128}$/);
const revision=z.number().int().nonnegative().safe();
export const supportStatus=z.enum(['OPEN','IN_REVIEW','CLOSED']);
export const statusLabels={OPEN:'접수',IN_REVIEW:'검토 중',CLOSED:'처리 종료'};
const history=z.object({principalId:z.string(),revision,status:supportStatus,publicReply:z.string().nullable(),internalNote:z.string().nullable(),createdAt:z.string().datetime()});
export const processingSchema=z.object({contractVersion:z.literal('admin-support-processing-v1'),ticket:z.object({ticketId:supportId,category:z.string(),status:supportStatus,bodyPreview:z.string(),body:z.string(),serverReceivedAt:z.string().datetime(),revision,publicReply:z.string().nullable(),assigneePrincipalId:z.string().nullable(),history:z.array(history).max(100),historyTruncated:z.boolean()})});
export const processingReceiptSchema=z.object({contractVersion:z.literal('support-ticket-process-v1'),revision});
const text=z.string().trim().min(1).max(2000);
export const processingCommandSchema=z.strictObject({expectedRevision:revision,status:supportStatus,publicReply:text.optional(),internalNote:text.optional()});
export type ProcessingTicket=z.infer<typeof processingSchema>['ticket'];
export type ProcessingCommand=z.infer<typeof processingCommandSchema>;
export type ProcessingAction='reply'|'note'|'status';
export function buildProcessingCommand(ticket:Pick<ProcessingTicket,'status'|'revision'>,action:ProcessingAction,status:ProcessingTicket['status'],reply:string,note:string):ProcessingCommand {
 const publicReply=reply.trim(),internalNote=note.trim();
 if(action==='reply'&&!publicReply)throw new Error('고객 답변을 입력해 주세요.');
 if(action==='note'&&!internalNote)throw new Error('내부 메모를 입력해 주세요.');
 if(action==='status'&&publicReply)throw new Error('작성한 답변을 먼저 등록하거나 지워 주세요.');
 const next=action==='note'?ticket.status:status;
 if(ticket.status==='CLOSED'&&next!=='CLOSED')throw new Error('종료된 문의는 다시 열 수 없습니다.');
 if(next==='CLOSED'&&ticket.status!=='CLOSED'&&(action!=='reply'||!publicReply))throw new Error('종료하려면 이번 요청에 최종 고객 답변이 필요합니다.');
 if(ticket.status==='IN_REVIEW'&&next==='OPEN'&&!internalNote)throw new Error('접수로 되돌리는 내부 사유가 필요합니다.');
 if(action==='status'&&internalNote&&!(ticket.status==='IN_REVIEW'&&next==='OPEN'))throw new Error('작성한 메모를 먼저 저장하거나 지워 주세요.');
 if(action==='status'&&next===ticket.status)throw new Error('변경할 상태가 없습니다.');
 return processingCommandSchema.parse({expectedRevision:ticket.revision,status:next,...(action==='reply'?{publicReply}:{}),...((action==='note'||action==='reply'||next==='OPEN')&&internalNote?{internalNote}:{})});
}
const permission=z.literal('SUPPORT_TICKET_PROCESS');
export const grantCommandSchema=z.strictObject({action:z.enum(['GRANT','REVOKE']),permission,expectedRevision:revision,reason:z.string().trim().min(1).max(500)});
export type GrantCommand=z.infer<typeof grantCommandSchema>;
export const grantStateSchema=z.object({contractVersion:z.literal('support-grant-management-v1'),environment:z.enum(['DEV','PRD']),state:z.object({principalId:supportId,displayName:z.string().nullable(),role:z.string(),status:z.string(),permission,granted:z.boolean(),revision,lastChange:z.object({actorId:z.string().nullable(),at:z.number().nonnegative().safe(),reason:z.string().nullable(),outcome:z.string(),environment:z.string()}).nullable()})});
export const grantReceiptSchema=z.object({contractVersion:z.literal('support-grant-management-v1'),environment:z.enum(['DEV','PRD']),result:z.object({principalId:supportId,permission,granted:z.boolean(),revision,outcome:z.enum(['APPLIED','NO_CHANGE'])})});
export function assertGrantReceipt(value:unknown,environment:'DEV'|'PRD',id:string,command:GrantCommand){
 const r=grantReceiptSchema.parse(value),expected=command.expectedRevision+(r.result.outcome==='APPLIED'?1:0);
 if(r.environment!==environment||r.result.principalId!==id||r.result.granted!==(command.action==='GRANT')||r.result.revision!==expected)throw new Error('권한 변경 원결과가 요청과 일치하지 않습니다.');
 return r;
}
