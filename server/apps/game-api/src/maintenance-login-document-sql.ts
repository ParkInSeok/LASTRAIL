// Atomic SQL equivalent of maintenanceDocumentSchema. Keep paired boundary tests
// when that approved schema changes; a restored document is not trusted input.
const value=(key:string)=>`json_extract(p.document_json,'$.${key}')`;
const type=(key:string)=>`json_type(p.document_json,'$.${key}')`;
const timestamp=(key:string,nullable=false)=>`(${nullable?`${type(key)}='null' OR `:''}(${type(key)}='integer' AND ${value(key)} BETWEEN 0 AND 9007199254740991))`;
const seconds=(key:string)=>`(${type(key)}='integer' AND ${value(key)} BETWEEN 1 AND 9007199254740)`;
const whitespace='char(9,10,11,12,13,32,160,5760,8192,8193,8194,8195,8196,8197,8198,8199,8200,8201,8202,8232,8233,8239,8287,12288,65279)';
function text(key:string,max:number){
 const trimmed=`trim(${value(key)},${whitespace})`;
 // SQLite counts code points; Zod/JS count UTF-16 code units. Scan bounded text
 // to preserve that limit and the plain-text control-character rule exactly.
 return `(${type(key)}='text' AND instr(${value(key)},char(0))=0 AND length(${trimmed}) BETWEEN 1 AND ${max}
  AND (WITH RECURSIVE chars(n,cp) AS (
   SELECT 1,unicode(substr(${trimmed},1,1)) UNION ALL
   SELECT n+1,unicode(substr(${trimmed},n+1,1)) FROM chars WHERE n<length(${trimmed})
  ) SELECT SUM(CASE WHEN cp>65535 THEN 2 ELSE 1 END)<=${max}
   AND MIN(CASE WHEN cp IN (60,62,127) OR (cp<32 AND cp NOT IN (9,10,13)) THEN 0 ELSE 1 END)=1 FROM chars))`;
}
const keys=['applicationId','serviceScope','webMode','title','summary','noticeId','drainStartsAt','startsAt','estimatedEndAt','existingRunPolicy','statusFreshnessSeconds','retryAfterSeconds'];
export const validLoginDocumentSql=`CASE WHEN json_valid(p.document_json) THEN
 json_type(p.document_json)='object'
 AND (SELECT COUNT(*)=12 AND COUNT(DISTINCT key)=12 AND MIN(key IN (${keys.map(key=>`'${key}'`).join(',')}))=1 FROM json_each(p.document_json))
 AND ${value('applicationId')}=r.application_id
 AND ${value('serviceScope')} IN ('GAME','WEB','GAME_AND_WEB')
 AND ${text('title',200)} AND ${text('summary',500)} AND ${type('noticeId')}='null'
 AND ${timestamp('startsAt')} AND ${timestamp('drainStartsAt',true)} AND ${timestamp('estimatedEndAt',true)}
 AND ${seconds('statusFreshnessSeconds')} AND ${seconds('retryAfterSeconds')}
 AND ${value('startsAt')}=r.starts_at AND ${value('drainStartsAt')} IS r.drain_starts_at
 AND ${value('estimatedEndAt')} IS r.estimated_end_at
 AND (${type('estimatedEndAt')}='null' OR ${value('estimatedEndAt')}>${value('startsAt')})
 AND ((${value('serviceScope')}='GAME' AND ${type('webMode')}='null')
  OR (${value('serviceScope')} IN ('WEB','GAME_AND_WEB') AND ${value('webMode')} IN ('READ_ONLY','MAINTENANCE')))
 AND ((${value('serviceScope')}='WEB' AND ${type('drainStartsAt')}='null' AND ${type('existingRunPolicy')}='null')
  OR (${value('serviceScope')} IN ('GAME','GAME_AND_WEB') AND
   ((${value('existingRunPolicy')}='DRAIN_THEN_PRESERVE' AND ${type('drainStartsAt')}='integer' AND ${value('drainStartsAt')}<=${value('startsAt')})
   OR (${value('existingRunPolicy')}='EMERGENCY_PRESERVE' AND ${type('drainStartsAt')}='null'))))
 ELSE 0 END`;
