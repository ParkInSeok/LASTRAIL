import {commentLockCommand,assertCommentLockReceipt,type CommentLockBody} from './commentLockContract';
import {SupportWriteSession} from '../support/SupportWriteSession';
import {AdminApiError} from '../api/client';
import {moderationId,moderationQuery,reviewCommand,moderateCommand,parseModerationRead,parseModerationReceipt,type ReadKind,type Resource,type ModerationBody,type ModerationQuery} from './moderationContract';
type CommandKind=Resource|'COMMENT_LOCK';
type CommandBody=ModerationBody|CommentLockBody;
type Port={verify:()=>Promise<void>;read:(kind:ReadKind,id?:string,query?:ModerationQuery)=>Promise<unknown>;write:(kind:CommandKind,id:string,body:CommandBody,key:string)=>Promise<unknown>};
/** Actor/environment-scoped private read and command owner. No member storage or View connection. */
export class ModerationSession {
 private readonly command:SupportWriteSession;
 private read?:{kind:ReadKind;data:ReturnType<typeof parseModerationRead>};
 private readError=false;private generation=0;private disposed=false;private denied=false;
 private listeners=new Set<()=>void>();private readonly detach:()=>void;
 constructor(private readonly environment:'DEV'|'PRD',private readonly port:Port,makeKey?:()=>string){this.command=new SupportWriteSession({verify:port.verify,write:async(path,body,key)=>{const [kind,id]=path.split('/');const value=await port.write(kind as CommandKind,id,body as CommandBody,key);return kind==='COMMENT_LOCK'?assertCommentLockReceipt(value,environment,id,body as CommentLockBody):parseModerationReceipt(value,environment,kind as Resource,id,body as ModerationBody);}},makeKey);this.detach=this.command.subscribe(()=>{const c=this.command.snapshot();if(c.phase==='revoked'||c.deniedPath){this.denied=true;this.generation++;this.read=undefined;}this.emit();});}
 snapshot=()=>structuredClone({command:this.command.snapshot(),read:this.read,readError:this.readError});
 subscribe=(listener:()=>void)=>{if(this.disposed)return()=>{};this.listeners.add(listener);return()=>{this.listeners.delete(listener);};};
 private emit(){if(!this.disposed)for(const listener of this.listeners)listener();}
 review(kind:CommandKind,id:string,body:CommandBody){if(this.disposed||this.denied||this.command.locked)return;moderationId.parse(id);this.generation++;this.read=undefined;const parsed=kind==='COMMENT_LOCK'?commentLockCommand.parse(body):kind==='REPORT'?reviewCommand.parse(body):moderateCommand.parse(body);this.command.review(kind+'/'+id,parsed);}
 cancel(){if(!this.disposed)this.command.cancel();}
 revoke(){this.denied=true;this.generation++;this.read=undefined;this.command.revoke();}
 dispose(){this.disposed=true;this.revoke();this.detach();this.listeners.clear();}
 async load(kind:ReadKind,id?:string,query?:ModerationQuery){if(this.disposed||this.denied)return;if(kind!=='LIST')moderationId.parse(id);else moderationQuery.parse(query??{});const generation=++this.generation;this.read=undefined;this.readError=false;this.emit();try{const data=parseModerationRead(kind,await this.port.read(kind,id,query),this.environment,id);if(generation===this.generation&&!this.disposed)this.read={kind,data};}catch(error){if(generation!==this.generation||this.disposed)return;if(error instanceof AdminApiError&&[401,403].includes(error.status)){this.revoke();return;}this.readError=true;}if(generation===this.generation)this.emit();}
 private async execute(retry:boolean){if(this.disposed||this.denied)return;const c=this.command.snapshot();if(c.phase!==(retry?'uncertain':'review')||!c.attempt)return;const [kind,id]=c.attempt.path.split('/');await(retry?this.command.retry():this.command.confirm());const after=this.command.snapshot();if(after.phase==='saved'||after.phase==='rejected'&&!after.deniedPath)await this.load(kind==='COMMENT_LOCK'?'CONTENT':kind as Resource,id);}
 confirm=()=>this.execute(false);retry=()=>this.execute(true);
}
