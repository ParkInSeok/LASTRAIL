import {z} from 'zod';
import {canonicalJson,sha256Hex,createRequestId} from '@lastrail/common';
import {MaintenancePlanError,maintenanceDocumentSchema,maintenanceIdSchema,
  type MaintenanceCommand,type MaintenanceDraft,type MaintenancePrincipal} from '../../../packages/common/src/maintenance-plan-policy';

const rowSchema=z.strictObject({maintenance_id:maintenanceIdSchema,draft_revision:z.number().int().positive().safe(),
  document_json:z.string(),updated_at:z.number().int().nonnegative().safe()});
type Row=z.infer<typeof rowSchema>;
const select=`SELECT r.maintenance_id,r.draft_revision,r.document_json,r.updated_at
  FROM maintenance_plan_heads h JOIN maintenance_plan_revisions r ON r.environment=h.environment
  AND r.maintenance_id=h.maintenance_id AND r.draft_revision=h.draft_revision`;
function decode(row:Row):MaintenanceDraft{
  try{
    const valid=rowSchema.parse(row);
    return {maintenanceId:valid.maintenance_id,draftRevision:valid.draft_revision,planState:'DRAFT',
      document:maintenanceDocumentSchema.parse(JSON.parse(valid.document_json)),updatedAt:valid.updated_at};
  }catch{throw new MaintenancePlanError(503,'maintenance_plan_storage_invalid');}
}
export class MaintenancePlanStore{
  constructor(private readonly db:D1Database,private readonly environment:string){}
  async read(id:string){
    const row=await this.db.prepare(select+' WHERE h.environment=? AND h.maintenance_id=?').bind(this.environment,id).first<Row>();
    return row?decode(row):null;
  }
  async list(after:string,limit:number){
    const rows=await this.db.prepare(select+' WHERE h.environment=? AND h.maintenance_id>? ORDER BY h.maintenance_id LIMIT ?')
      .bind(this.environment,after,limit).all<Row>();
    return rows.results.map(decode);
  }
  async command(actor:MaintenancePrincipal,id:string,command:MaintenanceCommand,key:string){
    if(actor.role!=='OWNER')throw new MaintenancePlanError(403,'ADMIN_PERMISSION_DENIED');
    if(command.environment!==this.environment)throw new MaintenancePlanError(400,'maintenance_environment_mismatch');
    // Operational commands cannot be enabled with a DTO flag or a saved draft.
    if(command.command!=='SAVE_DRAFT')throw new MaintenancePlanError(503,'maintenance_apply_unavailable');
    const db=this.db,environment=this.environment;
    const hash=await sha256Hex(canonicalJson({maintenanceId:id,...command}));
    const replay=async()=>{
      const row=await db.prepare('SELECT payload_hash,response_json FROM maintenance_plan_receipts WHERE principal_id=? AND environment=? AND request_key=?')
        .bind(actor.id,environment,key).first<{payload_hash:string;response_json:string}>();
      if(!row)return null;
      if(row.payload_hash!==hash)throw new MaintenancePlanError(409,'idempotency_conflict');
      return JSON.parse(row.response_json) as Record<string,unknown>;
    };
    const old=await replay();if(old)return old;
    const current=await this.read(id);
    if((current?.draftRevision??null)!==command.expectedDraftRevision||current?.draftRevision===Number.MAX_SAFE_INTEGER){
      const committed=await replay();if(committed)return committed;
      throw new MaintenancePlanError(409,'maintenance_draft_revision_conflict');
    }
    const revision=(current?.draftRevision??0)+1,now=Date.now(),auditId='audit_'+crypto.randomUUID(),requestId=createRequestId();
    const response={accepted:true,contractVersion:'admin-maintenance-draft-command-v1',environment,maintenanceId:id,
      draftRevision:revision,planState:'DRAFT',applied:false,currentPolicyRevision:null,auditId,requestId};
    try{
      await db.batch([
        db.prepare(`INSERT INTO maintenance_plan_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM admin_principals
          WHERE id=? AND role='OWNER' AND status='ACTIVE') THEN 1 ELSE 0 END`).bind(actor.id),
        db.prepare(`INSERT INTO maintenance_plan_guard SELECT CASE WHEN
          (? IS NULL AND NOT EXISTS(SELECT 1 FROM maintenance_plan_heads WHERE environment=? AND maintenance_id=?))
          OR EXISTS(SELECT 1 FROM maintenance_plan_heads WHERE environment=? AND maintenance_id=? AND draft_revision=?) THEN 1 ELSE 0 END`)
          .bind(command.expectedDraftRevision,environment,id,environment,id,command.expectedDraftRevision),
        db.prepare('INSERT INTO maintenance_plan_revisions VALUES(?,?,?,?,?)').bind(environment,id,revision,canonicalJson(command.plan),now),
        db.prepare(`INSERT INTO maintenance_plan_heads VALUES(?,?,?) ON CONFLICT(environment,maintenance_id)
          DO UPDATE SET draft_revision=excluded.draft_revision`).bind(environment,id,revision),
        db.prepare(`INSERT INTO admin_audit_logs(id,principal_id,role,permission,action,resource_type,resource_id,request_id,
          confirmation_id,payload_hash,change_summary_json,account_mutation_id,result,error_code,created_at)
          VALUES(?,?,?,'MAINTENANCE_DRAFT_WRITE','MAINTENANCE_DRAFT_SAVE','maintenance_plan_draft',?,?,NULL,?,?,NULL,'SUCCESS',NULL,?)`)
          .bind(auditId,actor.id,actor.role,id,requestId,hash,canonicalJson({environment,reason:command.reason,
            beforeDraftRevision:current?.draftRevision??null,afterDraftRevision:revision,applied:false}),now),
        db.prepare('INSERT INTO maintenance_plan_receipts VALUES(?,?,?,?,?,?)').bind(actor.id,environment,key,hash,auditId,canonicalJson(response)),
        db.prepare('DELETE FROM maintenance_plan_guard'),
      ]);
    }catch(error){
      const committed=await replay();if(committed)return committed;
      if(/CHECK constraint failed: valid=1/u.test(String(error))){
        const active=await db.prepare("SELECT id FROM admin_principals WHERE id=? AND role='OWNER' AND status='ACTIVE'").bind(actor.id).first();
        if(!active)throw new MaintenancePlanError(403,'ADMIN_PERMISSION_DENIED');
        throw new MaintenancePlanError(409,'maintenance_draft_revision_conflict');
      }
      throw new MaintenancePlanError(503,'maintenance_plan_unavailable');
    }
    return response;
  }
}
