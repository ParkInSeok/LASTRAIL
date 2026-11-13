import {z} from 'zod';
import {canonicalJson} from '@lastrail/common';
export const communityCaseRetentionVersion='community.case-retention-calculation.v1.20261011' as const;
const dayMs=86400000,ordinaryMaximumDays=90;
const timestamp=z.number().int().nonnegative().max(8640000000000000),basis=z.string().trim().min(1).max(500),start={basisId:basis,startAt:timestamp.nullable()};
const material=z.discriminatedUnion('category',[
 z.strictObject({category:z.literal('GENERAL_REPORT'),materialType:z.literal('REPORT_EVIDENCE'),...start,basisId:z.literal('CR03')}),
 z.strictObject({category:z.literal('CUSTOMER_DISPUTE'),materialType:z.literal('CUSTOMER_DISPUTE_MINIMUM'),...start}),
 z.strictObject({category:z.literal('PAYMENT_DISPUTE'),materialType:z.literal('PAYMENT_MINIMUM'),...start}),
 z.strictObject({category:z.literal('SEPARATE_PRESERVATION'),materialType:z.literal('SEPARATE_MINIMUM'),...start,reason:basis,endCondition:basis,nextReviewAt:timestamp})
]);
export class CommunityRetentionError extends Error{constructor(readonly status:400|404|409|503,readonly code:string){super(code);}}
/** Computes approved ceilings for explicitly selected minimal material; never classifies a case or grants preservation/purge authority. */
export class CommunityCaseRetentionModel{
 calculate(raw:unknown){const parsed=material.safeParse(raw);if(!parsed.success)throw new CommunityRetentionError(400,'community_retention_material_invalid');const input=parsed.data;
  const special=input.category==='SEPARATE_PRESERVATION',startEvent=input.category==='GENERAL_REPORT'?'FINAL_DISPOSITION_NOTIFIED_AT':special?'DOCUMENTED_SEPARATE_PURPOSE':'VERIFIED_MATERIAL_POLICY_START';let maximumUntil:number|null=null;
  if(input.startAt!==null&&!special){if(input.category==='GENERAL_REPORT')maximumUntil=input.startAt+ordinaryMaximumDays*dayMs;else{const date=new Date(input.startAt),month=date.getUTCMonth(),dateOfMonth=date.getUTCDate(),year=date.getUTCFullYear()+(input.category==='CUSTOMER_DISPUTE'?3:5);date.setUTCFullYear(year,month,1);date.setUTCDate(Math.min(dateOfMonth,new Date(Date.UTC(year,month+1,0)).getUTCDate()));maximumUntil=date.getTime();}if(!timestamp.safeParse(maximumUntil).success)throw new CommunityRetentionError(400,'community_retention_material_invalid');}
  return {contractVersion:communityCaseRetentionVersion,category:input.category,materialType:input.materialType,basisId:input.basisId,startEvent,startAt:input.startAt,maximumUntil,nextReviewAt:special?input.nextReviewAt:null,state:special?'REVIEW_REQUIRED' as const:input.startAt===null?'AWAITING_START_EVENT' as const:'STARTED' as const,purgeAuthorized:false as const};
 }
}
const id=z.string().regex(/^[A-Za-z0-9_-]{1,128}$/u);
const sourceSchema=z.strictObject({case_id:id,revision:z.number().int().positive().safe(),status:z.enum(['OPEN','IN_REVIEW','ACTIONED','DISMISSED']),submitted_at:timestamp,case_notified_at:timestamp.nullable(),evidence_case_id:id.nullable(),evidence_notified_at:timestamp.nullable(),necessity_review_due_at:timestamp.nullable(),retain_until:timestamp.nullable(),retention_policy_version:z.string().nullable()});
const sourceSql=`SELECT c.case_id,c.revision,c.status,c.submitted_at,c.final_notified_at AS case_notified_at,e.case_id AS evidence_case_id,e.final_notified_at AS evidence_notified_at,e.necessity_review_due_at,e.retain_until,e.retention_policy_version FROM community_report_cases c LEFT JOIN community_report_evidence e ON e.case_id=c.case_id WHERE c.case_id=?`;
/** Read-only source mapping for the existing ordinary intake. No evidence body, legal classification write or public endpoint. */
export class CommunityReportRetentionSource{
 private readonly model=new CommunityCaseRetentionModel();
 constructor(private readonly db:D1Database,private readonly now:()=>number=Date.now){}
 private async row(caseId:string){const raw=await this.db.prepare(sourceSql).bind(caseId).first();if(!raw)throw new CommunityRetentionError(404,'community_report_unavailable');const parsed=sourceSchema.safeParse(raw);if(!parsed.success)throw new CommunityRetentionError(503,'community_retention_source_invalid');return parsed.data;}
 async readGeneralEvidence(caseId:string){if(!id.safeParse(caseId).success)throw new CommunityRetentionError(400,'community_retention_material_invalid');const row=await this.row(caseId);if(row.evidence_case_id===null)throw new CommunityRetentionError(503,'community_report_evidence_unavailable');const at=this.now();if(!timestamp.safeParse(at).success||row.case_id!==caseId||row.evidence_case_id!==caseId||row.retention_policy_version!=='community-retention-v3-20260930'||row.case_notified_at!==row.evidence_notified_at||row.submitted_at>at||row.necessity_review_due_at===null||row.necessity_review_due_at<row.submitted_at)throw new CommunityRetentionError(503,'community_retention_source_invalid');const notified=row.case_notified_at;
  if(notified!==null&&(notified<row.submitted_at||notified>at||!['ACTIONED','DISMISSED'].includes(row.status)))throw new CommunityRetentionError(503,'community_retention_source_invalid');const result=this.model.calculate({category:'GENERAL_REPORT',materialType:'REPORT_EVIDENCE',basisId:'CR03',startAt:notified});if(row.retain_until!==null&&(notified===null||row.retain_until<notified||result.maximumUntil===null||row.retain_until>result.maximumUntil))throw new CommunityRetentionError(503,'community_retention_source_invalid');const current=await this.row(caseId);if(canonicalJson(current)!==canonicalJson(row))throw new CommunityRetentionError(409,'community_retention_source_stale');
  return {...result,caseId,sourceRevision:row.revision,recordedRetainUntil:row.retain_until,nextReviewAt:row.necessity_review_due_at,sourcePolicyVersion:row.retention_policy_version,classificationVerified:false as const,purgeAuthorized:false as const};
 }
}
