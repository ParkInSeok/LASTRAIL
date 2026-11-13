import {z} from 'zod';

export class NoticeError extends Error {
  constructor(readonly status:400|401|403|404|409|503,readonly code:string){super(code);}
}
export const noticeIdSchema=z.string().regex(/^[A-Za-z0-9][A-Za-z0-9_-]{0,79}$/u);
export const noticeEnvironment=z.enum(['DEV','PRD']);
const revision=z.number().int().positive().safe();
const timestamp=z.number().int().nonnegative().safe();
const plain=(max:number)=>z.string().trim().min(1).max(max).refine(s=>!/[<>]/u.test(s)
  && Array.from(s).every(c=>(c.charCodeAt(0)>=32&&c.charCodeAt(0)!==127)||'\t\n\r'.includes(c)),'Only plain text is supported');
export const localeSchema=z.string().regex(/^[a-z]{2,3}(?:-[A-Z]{2})?$/u);
const target=z.string().regex(/^[A-Za-z0-9_-]{1,40}$/u);
export const noticeDocumentSchema=z.strictObject({
  presentationRevision:revision,type:z.enum(['GENERAL','UPDATE','MAINTENANCE','EVENT','KNOWN_ISSUE']),
  title:plain(200),summary:plain(500),localizedBody:z.record(localeSchema,plain(6000)).refine(v=>Object.keys(v).length>0&&Object.keys(v).length<=8),
  bodyFormat:z.literal('PLAIN_TEXT'),surfaces:z.enum(['APP','WEB','BOTH']),visibility:z.enum(['PUBLIC','AUTHENTICATED','INTERNAL']),
  channel:target.nullable(),platform:target.nullable(),minVersionCode:revision.nullable(),maxVersionCode:revision.nullable(),
  startsAt:timestamp,endsAt:timestamp.nullable(),imageAssetId:z.null(),action:z.null(),popupMode:z.literal('NONE'),
  priority:z.number().int().min(-1000).max(1000),pinned:z.boolean(),
}).refine(d=>d.endsAt===null||d.endsAt>d.startsAt)
  .refine(d=>d.minVersionCode===null||d.maxVersionCode===null||d.maxVersionCode>=d.minVersionCode);
const common={environment:noticeEnvironment,expectedRevision:revision.nullable(),reason:plain(500)};
export const noticeCommandSchema=z.discriminatedUnion('command',[
  z.strictObject({...common,command:z.literal('SAVE_DRAFT'),document:noticeDocumentSchema}),
  z.strictObject({...common,command:z.enum(['PUBLISH','SCHEDULE','WITHDRAW'])}),
]);
export type NoticeDocument=z.infer<typeof noticeDocumentSchema>;
export type NoticeCommand=z.infer<typeof noticeCommandSchema>;
export type NoticePrincipal={id:string;role:'OWNER'|'OPERATOR'|'VIEWER'};
export type NoticePermission='NOTICE_READ'|'NOTICE_EDIT'|'NOTICE_PUBLISH'|'NOTICE_WITHDRAW';
export function isNoticePermission(value:string):value is NoticePermission {
  return ['NOTICE_READ','NOTICE_EDIT','NOTICE_PUBLISH','NOTICE_WITHDRAW'].includes(value);
}
export function hasNoticePermission(principal:NoticePrincipal,permission:NoticePermission) {
  return permission==='NOTICE_READ'||principal.role==='OWNER'||(permission==='NOTICE_EDIT'&&principal.role==='OPERATOR');
}
export function commandPermission(command:NoticeCommand):NoticePermission {
  return command.command==='SAVE_DRAFT'?'NOTICE_EDIT':command.command==='WITHDRAW'?'NOTICE_WITHDRAW':'NOTICE_PUBLISH';
}
export const noticeStateSchema=z.enum(['DRAFT','SCHEDULED','PUBLISHED','WITHDRAWN']);
export type NoticeState=z.infer<typeof noticeStateSchema>;
export type StoredNotice={noticeId:string;revision:number;status:NoticeState;document:NoticeDocument;updatedAt:number};
export function effectiveNoticeStatus(notice:StoredNotice,now:number) {
  if(notice.status==='DRAFT'||notice.status==='WITHDRAWN')return notice.status;
  if(notice.document.endsAt!==null&&now>=notice.document.endsAt)return 'EXPIRED';
  return now<notice.document.startsAt?'SCHEDULED':'PUBLISHED';
}
export function nextNotice(command:NoticeCommand,current:StoredNotice|null,published:StoredNotice|null,now:number) {
  const permission=commandPermission(command);
  if(command.command==='SAVE_DRAFT'){
    const previous=current?.document.presentationRevision??1;
    if(command.document.presentationRevision<previous||command.document.presentationRevision>previous+1)
      throw new NoticeError(409,'notice_presentation_revision_conflict');
    return {status:'DRAFT' as const,document:command.document,permission};
  }
  if(command.command==='WITHDRAW'){
    if(!published||published.status==='WITHDRAWN')throw new NoticeError(409,'notice_transition_conflict');
    // Withdrawal removes publication only; an editor's pending draft remains editable.
    return {status:current?.status==='DRAFT'?'DRAFT' as const:'WITHDRAWN' as const,document:current?.status==='DRAFT'?current.document:published.document,permission};
  }
  if(!current||current.status!=='DRAFT')throw new NoticeError(409,'notice_transition_conflict');
  const d=current.document;
  if((d.endsAt!==null&&d.endsAt<=now)||(command.command==='PUBLISH'&&d.startsAt>now)||(command.command==='SCHEDULE'&&d.startsAt<=now))
    throw new NoticeError(409,'notice_schedule_conflict');
  return {status:command.command==='SCHEDULE'?'SCHEDULED' as const:'PUBLISHED' as const,document:d,permission};
}
export type NoticeQuery={locale:string;channel:string|null;platform:string|null;versionCode:number|null;limit:number;after:string;context:string};
export function parseNoticeQuery(params:URLSearchParams,environment:string,list:boolean):NoticeQuery {
  const keys=list?['locale','channel','platform','versionCode','limit','cursor']:['locale','channel','platform','versionCode'];
  params.forEach((_value,key)=>{if(!keys.includes(key)||params.getAll(key).length!==1)throw new NoticeError(400,'contract_violation');});
  const locale=params.get('locale');
  if(!localeSchema.safeParse(locale).success)throw new NoticeError(400,'contract_violation');
  const channel=params.get('channel'),platform=params.get('platform');
  // Public filtering can narrow a publication; it cannot select an internal distribution.
  for(const value of [channel,platform])if(value!==null&&(!target.safeParse(value).success||value.toUpperCase()==='INTERNAL'))
    throw new NoticeError(400,'contract_violation');
  const code=params.get('versionCode'),rawLimit=params.get('limit');
  if(code!==null&&(!/^[1-9][0-9]*$/u.test(code)||!Number.isSafeInteger(Number(code))))throw new NoticeError(400,'contract_violation');
  if(rawLimit!==null&&(!/^[1-9][0-9]*$/u.test(rawLimit)||Number(rawLimit)>50))throw new NoticeError(400,'contract_violation');
  const limit=rawLimit===null?20:Number(rawLimit);
  const context=JSON.stringify([environment,locale,channel,platform,code,limit]);
  let after='';
  const cursor=params.get('cursor');
  if(cursor!==null){
    if(cursor.length>2048)throw new NoticeError(400,'contract_violation');
    try {
      const parsed=z.strictObject({context:z.string(),after:noticeIdSchema}).parse(JSON.parse(atob(cursor)));
      if(parsed.context!==context)throw new Error('context');after=parsed.after;
    }catch{throw new NoticeError(400,'contract_violation');}
  }
  return {locale:locale!,channel,platform,versionCode:code===null?null:Number(code),limit,after,context};
}
export function noticeCursor(query:NoticeQuery,after:string){return btoa(JSON.stringify({context:query.context,after}));}
export function publicNotice(notice:StoredNotice,query:NoticeQuery,now:number) {
  const d=notice.document;
  if(effectiveNoticeStatus(notice,now)!=='PUBLISHED'||d.visibility!=='PUBLIC'||d.surfaces==='APP'
    ||(d.channel!==null&&d.channel!==query.channel)||(d.platform!==null&&d.platform!==query.platform)
    ||(d.minVersionCode!==null&&(query.versionCode===null||query.versionCode<d.minVersionCode))
    ||(d.maxVersionCode!==null&&(query.versionCode===null||query.versionCode>d.maxVersionCode))
    ||!Object.hasOwn(d.localizedBody,query.locale))return null;
  return {noticeId:notice.noticeId,revision:notice.revision,presentationRevision:d.presentationRevision,type:d.type,
    title:d.title,summary:d.summary,body:d.localizedBody[query.locale]!,locale:query.locale,bodyFormat:d.bodyFormat,
    startsAt:d.startsAt,endsAt:d.endsAt,updatedAt:notice.updatedAt,priority:d.priority,pinned:d.pinned,imageAssetId:null,action:null};
}
