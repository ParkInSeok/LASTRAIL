import {z} from "zod";
export const maintenanceIdSchema=z.string().regex(/^[A-Za-z0-9][A-Za-z0-9_-]{0,79}$/);
const id=maintenanceIdSchema;
const time=z.number().int().nonnegative().safe();
const revision=z.number().int().positive().safe();
const seconds=z.number().int().positive().max(Math.floor(Number.MAX_SAFE_INTEGER/1000));
const text=(max:number)=>z.string().trim().min(1).max(max).refine(v=>!/[<>\x00-\x08\x0b\x0c\x0e-\x1f\x7f]/u.test(v));
export const maintenanceDocumentSchema=z.object({applicationId:id,serviceScope:z.enum(["GAME","WEB","GAME_AND_WEB"]),webMode:z.enum(["READ_ONLY","MAINTENANCE"]).nullable(),title:text(200),summary:text(500),noticeId:z.null(),drainStartsAt:time.nullable(),startsAt:time,estimatedEndAt:time.nullable(),existingRunPolicy:z.enum(["DRAIN_THEN_PRESERVE","EMERGENCY_PRESERVE"]).nullable(),statusFreshnessSeconds:seconds,retryAfterSeconds:seconds}).strict().superRefine((p,c)=>{
 const fail=()=>c.addIssue({code:"custom",message:"점검 계획의 대상·시간·출격 보존 조건이 맞지 않습니다."});
 if(p.estimatedEndAt!==null&&p.estimatedEndAt<=p.startsAt)fail();
 if(p.serviceScope==="GAME"?p.webMode!==null:p.webMode===null)fail();
 if(p.serviceScope==="WEB"){if(p.drainStartsAt!==null||p.existingRunPolicy!==null)fail();}
 else if(p.existingRunPolicy==="DRAIN_THEN_PRESERVE"){if(p.drainStartsAt===null||p.drainStartsAt>p.startsAt)fail();}
 else if(p.existingRunPolicy==="EMERGENCY_PRESERVE"){if(p.drainStartsAt!==null)fail();}
 else fail();
});
const plan=z.object({maintenanceId:id,draftRevision:revision,planState:z.literal("DRAFT"),document:maintenanceDocumentSchema,updatedAt:time});
const base={environment:z.enum(["DEV","PRD"]),serverTime:time,application:z.object({applied:z.literal(false),status:z.literal("UNCONNECTED"),currentPolicyRevision:z.null(),blockReasons:z.array(z.string().min(1)).min(1)}),permissions:z.object({canSaveDraft:z.boolean(),canApply:z.literal(false)})};
export const maintenanceListSchema=z.object({...base,contractVersion:z.literal("admin-maintenance-plan-list-v1"),items:z.array(plan),nextAfter:id.nullable()});
export const maintenanceDetailSchema=z.object({...base,contractVersion:z.literal("admin-maintenance-plan-v1"),plan});
export const maintenancePreviewSchema=z.object({...base,contractVersion:z.literal("admin-maintenance-preview-v1"),preview:z.object({kind:z.literal("HYPOTHETICAL_SCHEDULE"),applied:z.literal(false),maintenanceId:id,draftRevision:revision,at:time,effectiveGameMode:z.enum(["NORMAL","DRAINING","MAINTENANCE"]),effectiveWebMode:z.enum(["NORMAL","READ_ONLY","MAINTENANCE"]),estimatedEndAt:time.nullable(),automaticCompletion:z.literal(false),existingRunPolicy:z.enum(["DRAIN_THEN_PRESERVE","EMERGENCY_PRESERVE"]).nullable(),title:text(200),summary:text(500)})});
export type MaintenanceEnvironment="DEV"|"PRD";
export type MaintenanceDocument=z.infer<typeof maintenanceDocumentSchema>;
export const maintenanceSaveSchema=z.object({environment:z.enum(["DEV","PRD"]),command:z.literal("SAVE_DRAFT"),expectedDraftRevision:revision.nullable(),reason:text(500),plan:maintenanceDocumentSchema}).strict();
export type MaintenanceSave=z.infer<typeof maintenanceSaveSchema>;
export const maintenanceReceiptSchema=z.object({accepted:z.literal(true),contractVersion:z.literal("admin-maintenance-draft-command-v1"),environment:z.enum(["DEV","PRD"]),maintenanceId:id,draftRevision:revision,planState:z.literal("DRAFT"),applied:z.literal(false),currentPolicyRevision:z.null(),auditId:z.string().min(1),requestId:z.string().min(1)});
export function newMaintenanceDocument():MaintenanceDocument{return {applicationId:"",serviceScope:"GAME",webMode:null,title:"",summary:"",noticeId:null,drainStartsAt:null,startsAt:0,estimatedEndAt:null,existingRunPolicy:"DRAIN_THEN_PRESERVE",statusFreshnessSeconds:30,retryAfterSeconds:60};}
export function assertMaintenanceEnvironment<T extends {environment:MaintenanceEnvironment}>(value:T,environment:MaintenanceEnvironment):T{if(value.environment!==environment)throw new Error("점검 환경 응답 불일치");return value;}
export const maintenanceModes:Record<string,string>={NORMAL:"정상",DRAINING:"진행 중 출격 정리",MAINTENANCE:"점검",READ_ONLY:"조회만 허용"};
export const maintenanceScopes:Record<string,string>={GAME:"게임",WEB:"웹",GAME_AND_WEB:"게임 · 웹"};
export const maintenanceBlocks:Record<string,string>={request_gates_unconnected:"요청 차단 기능 미연결",run_preservation_unverified:"진행 중 출격 보존 미검증",callback_recovery_unverified:"결제·광고 콜백 복구 미검증",public_status_projection_unconnected:"외부 점검 상태 표시 미연결"};
export function parseMaintenanceTime(value:string):number|null{if(!/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/.test(value))return null;const at=Date.parse(`${value}:00+09:00`);return Number.isSafeInteger(at)&&at>=0?at:null;}
