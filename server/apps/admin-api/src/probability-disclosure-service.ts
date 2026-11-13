import {canonicalJson,sha256Hex,createRequestId} from '@lastrail/common';
import {ProbabilityDisclosureError as Failure,type PublishDisclosure,type DisclosurePrincipal} from '../../../packages/common/src/probability-disclosure-policy';
import {readDisclosureSource,disclosureSourcePredicate} from '../../game-api/src/probability-disclosure-source';
export async function publishProbabilityDisclosure(db:D1Database,environment:string,actor:DisclosurePrincipal,command:PublishDisclosure,key:string){
  if(environment!==command.environment)throw new Failure(400,'probability_environment_mismatch');
  if(actor.role!=='OWNER')throw new Failure(403,'ADMIN_PERMISSION_DENIED');
  const {sourceKind,...base}=command;
  const hash=await sha256Hex(canonicalJson(sourceKind===undefined?base:{...base,sourceKind}));
  const replay=async()=>{
    const row=await db.prepare('SELECT payload_hash,response_json FROM probability_disclosure_receipts WHERE principal_id=? AND environment=? AND request_key=?')
      .bind(actor.id,environment,key).first<{payload_hash:string;response_json:string}>();
    if(!row)return null;
    if(row.payload_hash!==hash)throw new Failure(409,'idempotency_conflict');
    return JSON.parse(row.response_json) as Record<string,unknown>;
  };
  const old=await replay();if(old)return old;
  let source:Awaited<ReturnType<typeof readDisclosureSource>>;
  try{
    source=await readDisclosureSource(db,command.environment,command.consumer,command.policyVersion,command.sourceKind);
    if(!source.publishable||source.projection.sourceDigest!==command.expectedSourceDigest)throw new Failure(409,'probability_source_conflict');
  }catch(error){
    const committed=await replay();if(committed)return committed;
    throw error;
  }
  const sourceCheck=disclosureSourcePredicate(command,source.rawConfig);
  const now=Date.now(),auditId='audit_'+crypto.randomUUID(),requestId=createRequestId();
  const payload=canonicalJson({...source.projection,disclosureId:source.disclosureId,contractVersion:'probability-disclosure-v1',publishedAt:now});
  const response={accepted:true,contractVersion:'admin-probability-publish-v1',environment,disclosureId:source.disclosureId,auditId,requestId};
  try{
    await db.batch([
      db.prepare(`INSERT INTO probability_disclosure_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM admin_principals WHERE id=? AND role='OWNER' AND status='ACTIVE') THEN 1 ELSE 0 END`).bind(actor.id),
      db.prepare(`INSERT INTO probability_disclosure_guard SELECT CASE WHEN EXISTS(${sourceCheck.sql}) THEN 1 ELSE 0 END`).bind(...sourceCheck.bindings),
      db.prepare('INSERT INTO probability_disclosures VALUES(?,?,?,?)').bind(source.disclosureId,environment,payload,await sha256Hex(payload)),
      db.prepare(`INSERT INTO admin_audit_logs(id,principal_id,role,permission,action,resource_type,resource_id,request_id,
        confirmation_id,payload_hash,change_summary_json,account_mutation_id,result,error_code,created_at)
        VALUES(?,?,'OWNER','PROBABILITY_PUBLISH','PROBABILITY_PUBLISH','probability_disclosure',?,?,NULL,?,?,NULL,'SUCCESS',NULL,?)`)
        .bind(auditId,actor.id,source.disclosureId,requestId,hash,canonicalJson({environment,consumer:command.consumer,sourceKind:command.sourceKind??'AD_POLICY',policyVersion:command.policyVersion,sourceDigest:command.expectedSourceDigest,reason:command.reason}),now),
      db.prepare('INSERT INTO probability_disclosure_receipts VALUES(?,?,?,?,?)').bind(actor.id,environment,key,hash,canonicalJson(response)),
      db.prepare('DELETE FROM probability_disclosure_guard'),
    ]);
  }catch{
    const committed=await replay();if(committed)return committed;
    const active=await db.prepare("SELECT id FROM admin_principals WHERE id=? AND role='OWNER' AND status='ACTIVE'").bind(actor.id).first();
    if(!active)throw new Failure(403,'ADMIN_PERMISSION_DENIED');
    const unchanged=await db.prepare(sourceCheck.sql).bind(...sourceCheck.bindings).first();
    if(!unchanged)throw new Failure(409,'probability_source_conflict');
    if(await db.prepare('SELECT disclosure_id FROM probability_disclosures WHERE disclosure_id=?').bind(source.disclosureId).first())throw new Failure(409,'probability_disclosure_exists');
    throw new Failure(503,'probability_disclosure_unavailable');
  }
  return response;
}
