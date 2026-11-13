import {AdminApiError} from '../api/client';
type Attempt={path:string;body:unknown;key:string};
type Snapshot={phase:'idle'|'review'|'pending'|'uncertain'|'saved'|'rejected'|'revoked';attempt?:Attempt;receipt?:unknown;deniedPath?:string;completed?:{path:string;reply:boolean;note:boolean};message:string};
type Port={verify:()=>Promise<void>;write:(path:string,body:unknown,key:string)=>Promise<unknown>};
/** One actor/environment owns an unresolved command. Views subscribe and may remount. */
export class SupportWriteSession {
 private current:Snapshot={phase:'idle',message:''};
 private listeners=new Set<()=>void>();
 constructor(private readonly port:Port,private readonly makeKey:()=>string=()=>crypto.randomUUID()){}
 snapshot=()=>this.current;
 subscribe=(listener:()=>void)=>{this.listeners.add(listener);return()=>{this.listeners.delete(listener);};};
 private set(value:Snapshot){this.current=value;for(const listener of this.listeners)listener();}
 get locked(){return ['pending','uncertain','revoked'].includes(this.current.phase);}
 review(path:string,body:unknown){if(this.locked)return;this.set({phase:'review',attempt:{path,body:structuredClone(body),key:this.makeKey()},message:''});}
 cancel(){if(!this.locked)this.set({phase:'idle',message:''});}
 revoke(){this.set({phase:'revoked',message:'권한 또는 세션이 변경되어 민감한 작성 내용을 제거했습니다. 다시 인증해 주세요.'});}
 confirm=async()=>{if(this.current.phase!=='review'||!this.current.attempt)return;await this.execute(this.current.attempt,false);};
 retry=async()=>{if(this.current.phase!=='uncertain'||!this.current.attempt)return;await this.execute(this.current.attempt,true);};
 private isRevoked(){return this.current.phase==='revoked';}
 private async execute(attempt:Attempt,recovery:boolean){
  this.set({phase:'pending',attempt,message:'요청 처리 중…'});
  let verified=false;
  try {await this.port.verify();if(this.isRevoked())return;verified=true;const receipt=await this.port.write(attempt.path,attempt.body,attempt.key);if(this.isRevoked())return;const fields=attempt.body as {publicReply?:unknown;internalNote?:unknown};this.set({phase:'saved',receipt,completed:{path:attempt.path,reply:typeof fields.publicReply==='string',note:typeof fields.internalNote==='string'},message:'원요청 처리 결과를 확인했습니다. 최신 상태를 다시 조회합니다.'});}
  catch(error){
   if(this.isRevoked())return;
   if(error instanceof AdminApiError&&(error.status===401||error.status===403)){
    if(!verified||error.status===401)this.revoke();else this.set({phase:'rejected',deniedPath:attempt.path,message:'이 요청의 권한이 없습니다. 민감한 초안을 제거했습니다.'});return;
   }
   const definitive=error instanceof AdminApiError&&((!recovery&&[400,404,409].includes(error.status))||(error.status===409&&['revision_conflict','support_grant_revision_conflict','idempotency_conflict','support_grant_target_ineligible','community_grant_revision_conflict','community_grant_target_ineligible','community_grant_version_conflict','community_moderation_version_conflict','community_revision_conflict','community_review_transition_invalid','community_moderation_transition_invalid','community_deleted','community_parent_unavailable','community_restore_ineligible','community_moderation_authority_stale','community_moderation_stale','community_comment_lock_version_conflict','community_comment_lock_target_invalid','community_comment_lock_transition_invalid'].includes(error.errorCode??'')));
   this.set(definitive?{phase:'rejected',message:`요청 거절 (${error instanceof AdminApiError?error.errorCode??error.status:'오류'}). 최신 상태를 조회하고 다시 검토해 주세요.`}:{phase:'uncertain',attempt,message:'처리 결과 미확인. 같은 요청 확인으로 원결과를 확인해 주세요.'});
  }
 }
}
