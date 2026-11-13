import {maintenanceContextSchema,type MaintenanceContext} from '../../../packages/common/src/maintenance-runtime-policy';
import {ActivityError} from './activity-policy';
import {validLoginDocumentSql} from './maintenance-login-document-sql';
const applicationId='LASTRAIL';
export function maintenanceContext(environment:unknown):MaintenanceContext{
 const parsed=maintenanceContextSchema.safeParse({environment,applicationId});
 if(!parsed.success)throw new ActivityError(503,'maintenance_policy_unavailable');
 return parsed.data;
}
// Defensive validation also covers data restored with SQL constraints bypassed.
const validPolicy=`typeof(r.revision)='integer' AND r.revision BETWEEN 1 AND 9007199254740991
 AND typeof(r.created_at)='integer' AND r.created_at BETWEEN 0 AND 9007199254740991
 AND ((r.state='IMMEDIATE' AND r.revision>1 AND r.maintenance_id IS NULL AND r.draft_revision IS NULL
 AND r.drain_starts_at IS NULL AND r.starts_at IS NULL AND r.ended_at IS NULL
 AND (r.estimated_end_at IS NULL OR (typeof(r.estimated_end_at)='integer' AND r.estimated_end_at>r.created_at AND r.estimated_end_at<=9007199254740991))
 AND EXISTS(SELECT 1 FROM maintenance_immediate_modes m WHERE m.environment=r.environment AND m.application_id=r.application_id AND m.revision=r.revision
 AND m.game_mode IN ('NORMAL','DRAINING','MAINTENANCE') AND m.web_mode='NORMAL'))
 OR (r.state='NORMAL' AND r.revision=1 AND r.maintenance_id IS NULL AND r.draft_revision IS NULL
 AND r.drain_starts_at IS NULL AND r.starts_at IS NULL AND r.estimated_end_at IS NULL AND r.ended_at IS NULL)
 OR (r.state IN ('SCHEDULED','ACTIVE','COMPLETED','CANCELLED') AND r.maintenance_id IS NOT NULL
 AND typeof(r.draft_revision)='integer' AND r.draft_revision BETWEEN 1 AND 9007199254740991
 AND typeof(r.starts_at)='integer' AND r.starts_at BETWEEN 0 AND 9007199254740991
 AND (r.drain_starts_at IS NULL OR (typeof(r.drain_starts_at)='integer' AND r.drain_starts_at BETWEEN 0 AND r.starts_at))
 AND (r.estimated_end_at IS NULL OR (typeof(r.estimated_end_at)='integer' AND r.estimated_end_at>r.starts_at AND r.estimated_end_at<=9007199254740991))
 AND ((r.state IN ('SCHEDULED','ACTIVE') AND r.ended_at IS NULL) OR (r.state IN ('COMPLETED','CANCELLED') AND typeof(r.ended_at)='integer' AND r.ended_at BETWEEN 0 AND 9007199254740991))))`;
/** Must run in the same batch as the NEW approval. Never prepend to replay/settlement. */
export function maintenanceAdmissionStatement(db:D1Database,input:MaintenanceContext):D1PreparedStatement{
 const c=maintenanceContextSchema.parse(input);
 return db.prepare(`INSERT INTO maintenance_admission_guard(code) SELECT CASE
 WHEN NOT EXISTS(SELECT 1 FROM maintenance_runtime_heads h JOIN maintenance_runtime_revisions r USING(environment,application_id,revision)
 WHERE h.environment=? AND h.application_id=? AND ${validPolicy}) THEN 'UNAVAILABLE'
 WHEN EXISTS(SELECT 1 FROM maintenance_runtime_heads h JOIN maintenance_runtime_revisions r USING(environment,application_id,revision)
 WHERE h.environment=? AND h.application_id=? AND (r.state IN ('NORMAL','COMPLETED','CANCELLED') OR
 (r.state='IMMEDIATE' AND EXISTS(SELECT 1 FROM maintenance_immediate_modes m WHERE m.environment=r.environment AND m.application_id=r.application_id AND m.revision=r.revision AND m.game_mode='NORMAL' AND m.web_mode='NORMAL')) OR
 (r.state IN ('SCHEDULED','ACTIVE') AND CAST((julianday('now')-2440587.5)*86400000 AS INTEGER)<COALESCE(r.drain_starts_at,r.starts_at))))
 THEN 'OK' ELSE 'BLOCKED' END`).bind(c.environment,c.applicationId,c.environment,c.applicationId);
}
export function maintenanceAdmissionError(error:unknown):ActivityError|null{
 const message=error instanceof Error?error.message:String(error);
 for(const code of ['maintenance_new_request_blocked','maintenance_policy_unavailable'] as const){
  if(new RegExp(`(?:^|[ :])${code}(?:$|[ :])`,'u').test(message))return new ActivityError(503,code);
 }
 return null;
}
const loginPolicyCte=`WITH current_policy AS (
  SELECT r.*,p.document_json,m.game_mode,m.web_mode FROM maintenance_runtime_heads h
  JOIN maintenance_runtime_revisions r USING(environment,application_id,revision)
  LEFT JOIN maintenance_plan_revisions p ON p.environment=r.environment AND p.maintenance_id=r.maintenance_id AND p.draft_revision=r.draft_revision
  LEFT JOIN maintenance_immediate_modes m ON m.environment=r.environment AND m.application_id=r.application_id AND m.revision=r.revision
  WHERE h.environment=? AND h.application_id=? AND ${validPolicy}
  AND (r.state IN ('NORMAL','IMMEDIATE') OR (${validLoginDocumentSql}))
 )`;
const loginDecisionSql=`CASE WHEN NOT EXISTS(SELECT 1 FROM current_policy) THEN 'UNAVAILABLE'
 WHEN EXISTS(SELECT 1 FROM current_policy WHERE state='IMMEDIATE' AND game_mode='MAINTENANCE') THEN 'BLOCKED'
 WHEN EXISTS(SELECT 1 FROM current_policy WHERE state IN ('SCHEDULED','ACTIVE')
  AND CAST((julianday('now')-2440587.5)*86400000 AS INTEGER)>=starts_at
  AND (json_extract(document_json,'$.serviceScope') IN (?, 'GAME_AND_WEB')
   OR json_extract(document_json,'$.serviceScope')='GAME')) THEN 'BLOCKED' ELSE 'OK' END`;
/** Full maintenance only: DRAINING prevents new runs, not new authentication. */
export function maintenanceLoginStatement(db:D1Database,input:MaintenanceContext,channel:'GAME'|'WEB'):D1PreparedStatement{
 const c=maintenanceContextSchema.parse(input);
 return db.prepare(`INSERT INTO maintenance_admission_guard(code) ${loginPolicyCte} SELECT ${loginDecisionSql}`)
  .bind(c.environment,c.applicationId,channel);
}
/** Same policy and clock as the commit guard, with both channels in one snapshot. */
export function maintenanceLoginReadStatement(db:D1Database,input:MaintenanceContext):D1PreparedStatement{
 const c=maintenanceContextSchema.parse(input);
 return db.prepare(`${loginPolicyCte} SELECT ${loginDecisionSql} AS game_code,${loginDecisionSql} AS web_code,
  (SELECT revision FROM current_policy) AS revision,(SELECT document_json FROM current_policy) AS document_json,
  (SELECT state FROM current_policy) AS stored_state,(SELECT maintenance_id FROM current_policy) AS maintenance_id,
  (SELECT draft_revision FROM current_policy) AS draft_revision,
  (SELECT game_mode FROM current_policy) AS immediate_game_mode,(SELECT web_mode FROM current_policy) AS immediate_web_mode,
  (SELECT estimated_end_at FROM current_policy) AS estimated_end_at,
  CAST((julianday('now')-2440587.5)*86400000 AS INTEGER) AS checked_at,
  (SELECT COUNT(*) FROM maintenance_admission_guard) AS guard_check`)
  .bind(c.environment,c.applicationId,'GAME','WEB');
}
