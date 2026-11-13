import {z} from 'zod';
import {rewardProposalSchema,rewardReviewPreviewSchema} from './rewardPreviewContract';
export const REWARD_DRAFT_CONTRACT='admin.reward-draft.v1.20261020' as const;
const revision=z.number().int().positive().safe(),id=z.string().min(1).max(128);
export const rewardDraftCommandSchema=z.object({command:z.literal('SAVE_DRAFT'),expectedRevision:revision.nullable(),reason:z.string().trim().min(1).max(500),proposal:rewardProposalSchema}).strict();
const body={contractVersion:z.literal(REWARD_DRAFT_CONTRACT),environment:z.enum(['DEV','PRD']),batchId:id,revision,status:z.literal('DRAFT'),preview:rewardReviewPreviewSchema,updatedAt:z.number().int().nonnegative().safe(),reviewApproved:z.literal(false),executable:z.literal(false)};
const bound=(r:{environment:string;batchId:string;revision:number;preview:{environment:string;batchId:string;revision:number;proposal:{environment:string;batchId:string;revision:number}}})=>r.environment===r.preview.environment&&r.batchId===r.preview.batchId&&r.revision===r.preview.revision&&r.preview.proposal.environment===r.environment&&r.preview.proposal.batchId===r.batchId&&r.preview.proposal.revision===r.revision;
export const rewardDraftDetailSchema=z.object({...body,observationsCurrent:z.literal(false)}).refine(bound,'저장 원본 scope/revision 불일치');
export const rewardDraftReceiptSchema=z.object({...body,accepted:z.literal(true),auditId:z.string().min(1),requestId:z.string().min(1)}).refine(bound,'확정 저장 scope/revision 불일치');
export type RewardDraftCommand=z.infer<typeof rewardDraftCommandSchema>;
export type RewardDraftDetail=z.infer<typeof rewardDraftDetailSchema>;
export type RewardDraftReceipt=z.infer<typeof rewardDraftReceiptSchema>;

