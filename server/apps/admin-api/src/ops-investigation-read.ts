import {opsLogSearch} from './ops-log-search';

export const opsInvestigationVersion='ops-investigation.v1.20261019';
const context="CASE WHEN json_valid(sanitized_context_json) THEN sanitized_context_json ELSE '{}' END";
export const contextField=(field:string)=>`json_extract(${context}, '$.${field}')`;
const textField=(field:string)=>`CASE WHEN json_type(${context}, '$.${field}')='text' THEN ${contextField(field)} ELSE NULL END`;
export const opsDimensions={phase:textField('phase'),serverVersion:textField('serverVersion'),appVersion:textField('appVersion'),appVersionCode:textField('appVersionCode'),contractVersion:textField('contractVersion')};
const occurrence=`CASE WHEN json_type(${context}, '$.occurrenceCount')='integer' AND ${contextField('occurrenceCount')} BETWEEN 1 AND 60 THEN ${contextField('occurrenceCount')} ELSE NULL END`;
const last=`CASE WHEN json_type(${context}, '$.lastOccurredAt')='integer' AND ${contextField('lastOccurredAt')} BETWEEN occurred_at AND created_at THEN ${contextField('lastOccurredAt')} ELSE occurred_at END`;
export function opsReadFilter(params:URLSearchParams){
 const get=(key:string)=>params.get(key)??undefined;
 const search=opsLogSearch(get('category'),get('q'),get('serverVersion'),get('appVersion'),get('appVersionCode'));
 const clauses=[...search.clauses],values:Array<string|number>=[...search.values];
 const filters={environment:'environment',service:'service',severity:'severity',status:'status',source:'source',errorCode:'error_code',requestId:'request_id',accountId:'account_id',runId:'run_id',rewardPreviewId:'reward_preview_id',rewardClaimId:'reward_claim_id',offlineRewardClaimId:'offline_reward_claim_id',gachaBatchId:'gacha_batch_id',gachaBannerId:'gacha_banner_id',storyProgressId:'story_progress_id',integrityEventId:'integrity_event_id',supportTicketId:'support_ticket_id',phase:opsDimensions.phase,contractVersion:opsDimensions.contractVersion};
 for(const [key,column] of Object.entries(filters)){const value=params.get(key)?.trim();if(value){if(value.length>200)throw new Error('invalid_log_filter');clauses.push(`${column} = ?`);values.push(value);}}
 const times:Array<number|null>=[];
 for(const [key,comparison] of [['occurredFrom','>='],['occurredTo','<=']] as const){
  const raw=params.get(key),time=raw===null?null:Date.parse(raw);times.push(time);
  if(time!==null){if(!Number.isSafeInteger(time)||time<0)throw new Error('invalid_log_filter');clauses.push(`occurred_at ${comparison} ?`);values.push(time);}
 }
 if(times[0]!==null&&times[1]!==null&&times[0]!>times[1]!)throw new Error('invalid_log_filter');
 const rawLimit=Number(params.get('limit')??50),rawCursor=Number(params.get('cursor')??0);
 const limit=Number.isSafeInteger(rawLimit)&&rawLimit>0?Math.min(rawLimit,50):50;
 const offset=Number.isSafeInteger(rawCursor)&&rawCursor>=0?rawCursor:0;
 return {where:clauses.length?'WHERE '+clauses.join(' AND '):'',values,limit,offset,filters:Object.fromEntries([...params].filter(([k])=>k!=='limit'&&k!=='cursor')),period:{occurredFrom:times[0]===null?null:new Date(times[0]!).toISOString(),occurredTo:times[1]===null?null:new Date(times[1]!).toISOString()}};
}
export type OpsReadFilter=ReturnType<typeof opsReadFilter>;
const metrics=`COUNT(*) AS eventCount, SUM(${occurrence}) AS occurrenceCount,
 SUM(CASE WHEN ${occurrence} IS NULL THEN 1 ELSE 0 END) AS occurrenceUnknownEventCount,
 COUNT(DISTINCT account_id) AS affectedAccountCount,
 COALESCE(SUM(CASE WHEN account_id IS NULL THEN 1 ELSE 0 END),0) AS unattributedEventCount,
 MIN(occurred_at) AS firstOccurredAt, MAX(${last}) AS lastOccurredAt`;
function normalize(row:Record<string,unknown>){
 return {...row,occurrenceCount:row.occurrenceCount??(row.eventCount===0?0:null),occurrenceUnknownEventCount:row.occurrenceUnknownEventCount??0,
  firstOccurredAt:row.firstOccurredAt===null?null:new Date(Number(row.firstOccurredAt)).toISOString(),lastOccurredAt:row.lastOccurredAt===null?null:new Date(Number(row.lastOccurredAt)).toISOString()};
}
export class OpsInvestigationRead {
 constructor(private readonly db:D1Database){}
 async summary(filter:OpsReadFilter){
  const row=await this.db.prepare(`SELECT ${metrics}, COUNT(*) AS recentErrorCount,
   COALESCE(SUM(severity='warning'),0) AS warningCount, COALESCE(SUM(severity='critical'),0) AS criticalCount,
   COALESCE(SUM(retryable=1),0) AS retryableCount, COALESCE(SUM(masked=1),0) AS maskedCount,
   COALESCE(SUM(status='open'),0) AS openCount, COALESCE(SUM(status='acknowledged'),0) AS acknowledgedCount,
   MAX(CASE WHEN severity='critical' THEN occurred_at ELSE NULL END) AS latestCriticalAt
   FROM ops_error_logs ${filter.where}`).bind(...filter.values).first<Record<string,unknown>>();
  if(!row)throw new Error('ops_summary_unavailable');
  return {...normalize(row),latestCriticalAt:row.latestCriticalAt===null?null:new Date(Number(row.latestCriticalAt)).toISOString(),...this.metadata(filter)};
 }
 async groups(filter:OpsReadFilter){
  const dimensions=`environment,service,error_code AS errorCode,${Object.entries(opsDimensions).map(([name,sql])=>`${sql} AS ${name}`).join(',')}`;
  const names='environment,service,errorCode,phase,serverVersion,appVersion,appVersionCode,contractVersion';
  const row=await this.db.prepare(`WITH grouped AS (SELECT ${dimensions}, ${metrics} FROM ops_error_logs ${filter.where} GROUP BY ${names}),
   page AS (SELECT * FROM grouped ORDER BY lastOccurredAt DESC,${names} LIMIT ? OFFSET ?)
   SELECT (SELECT COUNT(*) FROM grouped) AS total,
   (SELECT json_group_array(json_object(${[...names.split(','),'eventCount','occurrenceCount','occurrenceUnknownEventCount','affectedAccountCount','unattributedEventCount','firstOccurredAt','lastOccurredAt'].map(k=>`'${k}',${k}`).join(',')})) FROM page) AS items`)
   .bind(...filter.values,filter.limit,filter.offset).first<{total:number;items:string}>();
  if(!row)throw new Error('ops_groups_unavailable');
  const items=(JSON.parse(row.items) as Record<string,unknown>[]).map(normalize);
  return {...this.metadata(filter),items,totalGroupCount:row.total,pageItemCount:items.length,nextCursor:filter.offset+items.length<row.total?String(filter.offset+filter.limit):null};
 }
 metadata(filter:OpsReadFilter){return {contractVersion:opsInvestigationVersion,collectionAvailable:true,scope:'FILTERED_ALL',statusScope:'ORIGINAL_EVENT',filters:filter.filters,period:filter.period,serverCalculatedAt:new Date().toISOString()};}
}
