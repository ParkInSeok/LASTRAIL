import {z} from "zod";

const revision=z.number().int().positive().safe();
const time=z.number().int().nonnegative().safe();
const plain=(max:number)=>z.string().trim().min(1).max(max).refine(s=>!/[<>\x00-\x08\x0b\x0c\x0e-\x1f\x7f]/u.test(s),"일반 텍스트만 입력해 주세요.");
const target=z.string().regex(/^[A-Za-z0-9_-]{1,40}$/u).nullable();
export const noticeIdSchema=z.string().regex(/^[A-Za-z0-9][A-Za-z0-9_-]{0,79}$/u);
export const noticeDocumentSchema=z.object({
 presentationRevision:revision,type:z.enum(["GENERAL","UPDATE","MAINTENANCE","EVENT","KNOWN_ISSUE"]),
 title:plain(200),summary:plain(500),localizedBody:z.record(z.string().regex(/^[a-z]{2,3}(?:-[A-Z]{2})?$/u),plain(6000)).refine(v=>Object.keys(v).length>0&&Object.keys(v).length<=8),
 bodyFormat:z.literal("PLAIN_TEXT"),surfaces:z.enum(["APP","WEB","BOTH"]),visibility:z.enum(["PUBLIC","AUTHENTICATED","INTERNAL"]),
 channel:target,platform:target,minVersionCode:revision.nullable(),maxVersionCode:revision.nullable(),
 startsAt:time,endsAt:time.nullable(),imageAssetId:z.null(),action:z.null(),popupMode:z.literal("NONE"),priority:z.number().int().min(-1000).max(1000),pinned:z.boolean()
}).strict().refine(d=>d.endsAt===null||d.endsAt>d.startsAt,"종료 시각은 시작 시각보다 뒤여야 합니다.")
 .refine(d=>d.minVersionCode===null||d.maxVersionCode===null||d.maxVersionCode>=d.minVersionCode,"버전 범위를 확인해 주세요.");
export type NoticeDocument=z.infer<typeof noticeDocumentSchema>;
const stored=z.object({noticeId:noticeIdSchema,revision,status:z.enum(["DRAFT","PUBLISHED","SCHEDULED","WITHDRAWN"]),document:noticeDocumentSchema,updatedAt:time});
const envelope={environment:z.enum(["DEV","PRD"]),serverTime:time,permissions:z.object({canEdit:z.boolean(),canPublish:z.boolean(),canWithdraw:z.boolean()})};
export const noticeListSchema=z.object({...envelope,contractVersion:z.literal("admin-notice-list-v1"),items:z.array(stored),nextAfter:noticeIdSchema.nullable()});
export const noticeDetailSchema=z.object({...envelope,contractVersion:z.literal("admin-notice-v1"),notice:stored.extend({effectiveStatus:z.enum(["DRAFT","PUBLISHED","SCHEDULED","WITHDRAWN","EXPIRED"])}),publication:stored.nullable()});
export type NoticeList=z.infer<typeof noticeListSchema>;
export type NoticeDetail=z.infer<typeof noticeDetailSchema>;
export type NoticeAction="SAVE_DRAFT"|"PUBLISH"|"SCHEDULE"|"WITHDRAW";
const common={environment:z.enum(["DEV","PRD"]),expectedRevision:revision.nullable(),reason:plain(500)};
export const noticeCommandSchema=z.discriminatedUnion("command",[
 z.object({...common,command:z.literal("SAVE_DRAFT"),document:noticeDocumentSchema}).strict(),
 z.object({...common,command:z.enum(["PUBLISH","SCHEDULE","WITHDRAW"])}).strict()
]);
export type NoticeCommand=z.infer<typeof noticeCommandSchema>;
export const noticeReceiptSchema=z.object({accepted:z.literal(true),contractVersion:z.literal("admin-notice-command-v1"),environment:z.enum(["DEV","PRD"]),noticeId:noticeIdSchema,revision,status:stored.shape.status,command:z.enum(["SAVE_DRAFT","PUBLISH","SCHEDULE","WITHDRAW"]),publishedRevision:revision.nullable(),auditId:z.string().min(1),requestId:z.string().min(1)});
export const noticeLabels:Record<string,string>={DRAFT:"초안",PUBLISHED:"게시 중",SCHEDULED:"예약",WITHDRAWN:"철회",EXPIRED:"게시 종료",SAVE_DRAFT:"초안 저장",PUBLISH:"즉시 게시",SCHEDULE:"예약 게시",WITHDRAW:"게시 철회"};
export function publicationStatus(item:NoticeDetail["publication"],now:number){
 if(!item)return "게시본 없음";
 if(item.document.endsAt!==null&&item.document.endsAt<=now)return "게시 종료";
 return item.document.startsAt>now?"예약":"게시 중";
}
export function newNoticeDocument():NoticeDocument{return {presentationRevision:1,type:"GENERAL",title:"",summary:"",localizedBody:{ko:""},bodyFormat:"PLAIN_TEXT",surfaces:"BOTH",visibility:"PUBLIC",channel:null,platform:null,minVersionCode:null,maxVersionCode:null,startsAt:0,endsAt:null,imageAssetId:null,action:null,popupMode:"NONE",priority:0,pinned:false};}
