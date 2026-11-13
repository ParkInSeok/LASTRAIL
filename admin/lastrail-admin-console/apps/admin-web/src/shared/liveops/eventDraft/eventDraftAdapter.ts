import {parseKst,type EventDefinition,type EventScheduleDraft,type EventRewardRow} from "../eventScheduleDraft";
import {eventPreviewInputFromDraft,eventDefinitionDraftSchema,type EventDefinitionDraft} from "../eventPreview/eventPreviewContract";
/** Scoped UI gate from the supplied Server event-draft contract only. Not a global grant;
 * the original Server independently rechecks ACTIVE role in the atomic write batch.
 */
export const eventDraftCanSaveRole=(role:string)=>role==="OWNER"||role==="OPERATOR";
const localKind:Record<EventDefinitionDraft["rewards"][number]["kind"],EventRewardRow["kind"]>={PARTICIPATION:"participation",STORY:"story",CUMULATIVE:"cumulative",GIFT:"anniversary"};
function localTime(time:number|null){if(time===null)return "";const iso=new Date(time+9*3600000).toISOString();if(!/^\d{4}-/.test(iso))throw new Error("EVENT_LOCAL_DATE_EDIT_RANGE_UNSUPPORTED");return iso.slice(0,-1).replace(/\.000$/u,"");}
export function eventDefinitionToLocal(raw:EventDefinitionDraft):EventDefinition{
 const d=eventDefinitionDraftSchema.parse(raw);return {roundId:d.roundId,policyVersion:d.policyVersion??"",title:d.title,theme:d.theme,environment:d.environment,schedule:{publicAt:localTime(d.schedule.publishAt),start:localTime(d.schedule.startAt),end:localTime(d.schedule.endAt),claimEnd:localTime(d.schedule.claimUntil),giftOpen:localTime(d.schedule.giftOpenAt)},rows:d.rewards.map(row=>({id:row.rightId,kind:localKind[row.kind],condition:row.condition,sourceId:row.producerId??"",itemId:row.itemId??"",quantity:row.quantity,perAccount:row.perAccountCount,...(row.threshold===null?{}:{threshold:row.threshold}),policySource:row.policyVersion??"",catalogReady:false,ledgerReady:false,clientReady:false,assetReady:false,unownedHandlingReady:false,duplicateHandlingReady:false})),producerPolicy:null,closePolicy:null,settlementPolicy:null};
}
/** Reuse existing mapping; preserve original subsecond precision without rounding timestamps. */
export function eventDraftDefinitionFromLocal(snapshot:ReturnType<EventScheduleDraft["snapshot"]>,currentRevision:number|null):EventDefinitionDraft{
 const normalized={...snapshot.definition,schedule:{...snapshot.definition.schedule}},fractions:Partial<Record<keyof EventDefinition["schedule"],number>>={};
 for(const key of Object.keys(normalized.schedule) as Array<keyof EventDefinition["schedule"]>){if(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/u.test(normalized.schedule[key]))normalized.schedule[key]+=":00";const match=/^(.*)\.(\d{1,3})$/u.exec(normalized.schedule[key]);if(match){const base=parseKst(match[1]);if(base===null)throw new Error("EVENT_REQUIRED_SCHEDULE_INVALID");normalized.schedule[key]=match[1];fractions[key]=base+Number(match[2].padEnd(3,"0"));}}
 const wire=eventPreviewInputFromDraft({...snapshot,definition:normalized},(currentRevision??0)+1),keys={publicAt:"publishAt",start:"startAt",end:"endAt",claimEnd:"claimUntil",giftOpen:"giftOpenAt"} as const;
 for(const key of Object.keys(fractions) as Array<keyof typeof fractions>){wire.schedule[keys[key]]=fractions[key]!;}
 return eventDefinitionDraftSchema.parse(wire);
}
