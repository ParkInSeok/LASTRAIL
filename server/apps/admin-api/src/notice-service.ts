import {canonicalJson,sha256Hex,createRequestId} from '@lastrail/common';
import {NoticeError,commandPermission,hasNoticePermission,nextNotice,type NoticeCommand,type NoticePrincipal} from '../../../packages/common/src/notice-policy';
import {NoticeStore} from '../../game-api/src/notice-store';

export async function saveNoticeCommand(db:D1Database,environment:string,actor:NoticePrincipal,id:string,command:NoticeCommand,key:string,
  review?:{id:string|null;prepare:()=>Promise<readonly D1PreparedStatement[]>}) {
  if(environment!==command.environment)throw new NoticeError(400,'notice_environment_mismatch');
  const permission=commandPermission(command);
  if(!hasNoticePermission(actor,permission))throw new NoticeError(403,'ADMIN_PERMISSION_DENIED');
  const hash=await sha256Hex(canonicalJson({noticeId:id,...command,...(review?.id?{liveopsReviewId:review.id}:{})}));
  const replay=async()=>{
    const row=await db.prepare('SELECT payload_hash,response_json FROM notice_command_receipts WHERE principal_id=? AND environment=? AND request_key=?')
      .bind(actor.id,environment,key).first<{payload_hash:string;response_json:string}>();
    if(!row)return null;
    if(row.payload_hash!==hash)throw new NoticeError(409,'idempotency_conflict');
    return JSON.parse(row.response_json) as Record<string,unknown>;
  };
  const old=await replay();if(old)return old;
  const store=new NoticeStore(db,environment);
  const current=await store.read(id),published=await store.read(id,true);
  if((current?.revision??null)!==command.expectedRevision||current?.revision===Number.MAX_SAFE_INTEGER){
    const committed=await replay();if(committed)return committed;
    throw new NoticeError(409,'notice_revision_conflict');
  }
  const now=Date.now(),next=nextNotice(command,current,published,now),revision=(current?.revision??0)+1;
  // The original-key receipt above wins after commit, even when the source has
  // since changed. New executions fence the exact reviewed source in this batch.
  const reviewAssertions=review?await review.prepare():[];
  const auditId='audit_'+crypto.randomUUID(),requestId=createRequestId();
  const publicRevision=command.command==='WITHDRAW'?null:command.command==='SAVE_DRAFT'?(published?.revision??null):revision;
  const response={accepted:true,contractVersion:'admin-notice-command-v1',environment,noticeId:id,revision,status:next.status,
    command:command.command,publishedRevision:publicRevision,auditId,requestId,
    notice:{noticeId:id,revision,status:next.status,document:next.document,updatedAt:now}};
  try{
    await db.batch([
      ...reviewAssertions,
      db.prepare(`INSERT INTO notice_write_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM admin_principals
        WHERE id=? AND status='ACTIVE' AND role=? AND (role='OWNER' OR (?='NOTICE_EDIT' AND role='OPERATOR'))) THEN 1 ELSE 0 END`)
        .bind(actor.id,actor.role,permission),
      db.prepare(`INSERT INTO notice_write_guard SELECT CASE WHEN
        (? IS NULL AND NOT EXISTS(SELECT 1 FROM notice_heads WHERE environment=? AND notice_id=?))
        OR EXISTS(SELECT 1 FROM notice_heads WHERE environment=? AND notice_id=? AND revision=?) THEN 1 ELSE 0 END`)
        .bind(command.expectedRevision,environment,id,environment,id,command.expectedRevision),
      db.prepare('INSERT INTO notice_revisions VALUES(?,?,?,?,?,?)').bind(environment,id,revision,next.status,canonicalJson(next.document),now),
      db.prepare(`INSERT INTO notice_heads VALUES(?,?,?,?) ON CONFLICT(environment,notice_id)
        DO UPDATE SET revision=excluded.revision,published_revision=excluded.published_revision`).bind(environment,id,revision,publicRevision),
      db.prepare(`INSERT INTO admin_audit_logs(id,principal_id,role,permission,action,resource_type,resource_id,request_id,
        confirmation_id,payload_hash,change_summary_json,account_mutation_id,result,error_code,created_at)
        VALUES(?,?,?,?,'NOTICE_COMMAND','notice',?,?,NULL,?,?,NULL,'SUCCESS',NULL,?)`)
        .bind(auditId,actor.id,actor.role,permission,id,requestId,hash,canonicalJson({environment,command:command.command,
          reason:command.reason,beforeRevision:current?.revision??null,afterRevision:revision,publishedRevision:publicRevision}),now),
      db.prepare('INSERT INTO notice_command_receipts VALUES(?,?,?,?,?,?)').bind(actor.id,environment,key,hash,auditId,canonicalJson(response)),
      db.prepare('DELETE FROM notice_write_guard'),
    ]);
  }catch(error){
    const committed=await replay();if(committed)return committed;
    if(/CHECK constraint failed: valid=1/u.test(String(error))){
      const active=await db.prepare("SELECT role FROM admin_principals WHERE id=? AND status='ACTIVE'").bind(actor.id).first<{role:NoticePrincipal['role']}>();
      if(!active||active.role!==actor.role||!hasNoticePermission({...actor,role:active.role},permission))throw new NoticeError(403,'ADMIN_PERMISSION_DENIED');
      throw new NoticeError(409,'notice_revision_conflict');
    }
    throw new NoticeError(503,'notice_unavailable');
  }
  return response;
}
