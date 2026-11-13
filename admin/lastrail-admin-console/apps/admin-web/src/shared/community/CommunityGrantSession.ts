import {SupportWriteSession} from '../support/SupportWriteSession';
import {AdminApiError} from '../api/client';
import {assertCommunityGrantReceipt,assertCommunityGrantState,communityGrantCommandSchema,communityPrincipalId,type CommunityGrantCommand,type CommunityGrantState} from './communityGrantContract';
type Port={verify:()=>Promise<void>;write:(id:string,body:CommunityGrantCommand,key:string)=>Promise<unknown>;read:(id:string)=>Promise<unknown>};
/** Caller owns one authenticated actor/environment session; no global state or View binding. */
export class CommunityGrantSession {
 private readonly command:SupportWriteSession;
 private current?:CommunityGrantState;
 private readError=false;
 private generation=0;
 private disposed=false;
 private listeners=new Set<()=>void>();
 private readonly detach:()=>void;
 constructor(private readonly environment:'DEV'|'PRD',private readonly port:Port,makeKey?:()=>string){
  this.command=new SupportWriteSession({verify:port.verify,write:async(id,body,key)=>assertCommunityGrantReceipt(await port.write(id,body as CommunityGrantCommand,key),environment,id,body as CommunityGrantCommand)},makeKey);
  this.detach=this.command.subscribe(()=>{const phase=this.command.snapshot().phase;if(phase==='revoked'||phase==='rejected'&&this.command.snapshot().deniedPath){this.generation++;this.current=undefined;}this.emit();});
 }
 snapshot=()=>structuredClone({command:this.command.snapshot(),current:this.current,readError:this.readError});
 subscribe=(listener:()=>void)=>{if(this.disposed)return()=>{};this.listeners.add(listener);return()=>{this.listeners.delete(listener);};};
 private emit(){if(!this.disposed)for(const listener of this.listeners)listener();}
 review(id:string,body:CommunityGrantCommand){if(this.disposed||this.command.locked)return;this.generation++;this.current=undefined;this.command.review(communityPrincipalId.parse(id),communityGrantCommandSchema.parse(body));}
 cancel(){if(!this.disposed)this.command.cancel();}
 revoke(){this.generation++;this.current=undefined;this.command.revoke();}
 dispose(){this.disposed=true;this.generation++;this.current=undefined;this.command.revoke();this.detach();this.listeners.clear();}
 async refresh(id:string){if(this.disposed||this.command.snapshot().phase==='revoked')return;communityPrincipalId.parse(id);const generation=++this.generation;this.current=undefined;this.readError=false;this.emit();try{const r=assertCommunityGrantState(await this.port.read(id),this.environment,id);if(generation===this.generation&&!this.disposed)this.current=r;}catch(error){if(generation!==this.generation||this.disposed)return;if(error instanceof AdminApiError&&[401,403].includes(error.status)){this.revoke();return;}this.readError=true;}if(generation===this.generation)this.emit();}
 private async execute(retry:boolean){if(this.disposed)return;const before=this.command.snapshot();if(before.phase!==(retry?'uncertain':'review')||!before.attempt)return;const id=before.attempt.path;await(retry?this.command.retry():this.command.confirm());const result=this.command.snapshot();if(result.phase==='saved'||result.phase==='rejected'&&!result.deniedPath)await this.refresh(id);}
 confirm=()=>this.execute(false);
 retry=()=>this.execute(true);
}
