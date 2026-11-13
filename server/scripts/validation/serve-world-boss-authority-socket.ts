import {resolve} from 'node:path';
import {writeFile} from 'node:fs/promises';
import {startWorldBossAuthoritySocket,type WorldSocketRole} from '../local/world-boss-authority-socket';
const canonical='D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server';
if(resolve(process.cwd()).toLowerCase()!==resolve(canonical).toLowerCase())throw new Error('world_socket_canonical_checkout_required');
const args=process.argv.slice(2);if(!args.includes('--ipc')&&!args.includes('--self-check')||args.includes('--ipc')&&args.includes('--self-check'))throw new Error('world_socket_mode_required');
let lifetimeMs=600000;for(const arg of args){if(['--ipc','--self-check'].includes(arg))continue;if(arg.startsWith('--lifetime-ms=')){const raw=arg.slice(14);if(!/^[1-9][0-9]*$/u.test(raw))throw new Error('world_socket_args_invalid');lifetimeMs=Number(raw);}else throw new Error('world_socket_args_invalid');}
if(args.includes('--ipc')&&(!process.send||!process.connected))throw new Error('world_socket_private_parent_ipc_required');
let shutdownRequested=false;const rememberShutdown=()=>{shutdownRequested=true;};
process.once('disconnect',rememberShutdown);process.once('SIGINT',rememberShutdown);process.once('SIGTERM',rememberShutdown);
let host:Awaited<ReturnType<typeof startWorldBossAuthoritySocket>>;
try{if(args.includes('--ipc'))await new Promise<void>((done,reject)=>process.send!({kind:'STARTING'},error=>error?reject(error):done()));host=await startWorldBossAuthoritySocket({lifetimeMs});}finally{process.removeListener('disconnect',rememberShutdown);process.removeListener('SIGINT',rememberShutdown);process.removeListener('SIGTERM',rememberShutdown);}
if(shutdownRequested||(args.includes('--ipc')&&!process.connected)){await host.close();}
else if(args.includes('--self-check')){
 const traces:unknown[]=[];const path='/_local/world-boss/attempts/trial/authority';let checkpoint:Uint8Array|undefined;
 try{
  const intake=host.intake('train'),command={version:'world-boss.rail-action.v3.20261002',attemptId:'trial',contentVersion:'LOCAL_GEO',roundId:'p1r1',commandId:'live-no-producer',expectedRevision:1,action:{kind:'CONFIRM_ROUTE',side:'RIGHT'}};
  for(const [name,status,role,write] of [['LIVE_READ',200,'train',false],['OPERATOR_READ',200,'operator',false],['OUTSIDER_DENIED',403,'outsider',false],['NEW_COMMAND_NO_PRODUCER',503,'train',true]] as const){const credentials=host.intake(role),response=await fetch(host.origin+path+(write?'/commands':''),{method:write?'POST':'GET',headers:{...credentials.headers,...(write?{'Idempotency-Key':command.commandId,'X-World-Journal-Revision':'1'}:{})},...(write?{body:JSON.stringify(command)}:{})});if(response.status!==status)throw new Error('world_socket_self_check_failed');traces.push({name,httpStatus:response.status,response:await response.json(),request:{method:write?'POST':'GET',path:path+(write?'/commands':''),role,body:write?command:null},actualTcp:true});}
  await host.renew('train');const old=await fetch(host.origin+path,{headers:intake.headers});if(old.status!==401)throw new Error('world_socket_old_session_not_revoked');traces.push({name:'OLD_GAME_SESSION_REPLACED',httpStatus:old.status,response:await old.json(),actualTcp:true});
  await host.revoke('train');const revoked=await fetch(host.origin+path,{headers:host.intake('train').headers});if(revoked.status!==401)throw new Error('world_socket_revoke_failed');traces.push({name:'REVOKED_CURRENT_SESSION',httpStatus:revoked.status,response:await revoked.json(),actualTcp:true});
  await host.renew('train');checkpoint=await host.checkpoint();if(host.diagnostics().journalEntries!==1)throw new Error('world_socket_read_mutated_source');
 }finally{await host.close();}
 try{const restored=await startWorldBossAuthoritySocket({restore:checkpoint!});try{const response=await fetch(restored.origin+path,{headers:restored.intake('operator').headers});if(response.status!==200||restored.diagnostics().journalEntries!==1)throw new Error('world_socket_restore_failed');traces.push({name:'MEMORY_CHECKPOINT_RESTART_READ',httpStatus:200,response:await response.json(),actualTcp:true});}finally{await restored.close();}if(restored.metadata.lifecycle!=='CLOSED'||!restored.metadata.databaseDiscarded)throw new Error('world_socket_cleanup_failed');}finally{checkpoint?.fill(0);}
 await writeFile('D:/Project/LASTRAIL/QA/CommanderS1-20260922/world-boss-socket-self-check-20261002.json',JSON.stringify({version:host.metadata.version,status:'PASS',clockMode:'LIVE',operationalProducer:false,credentialArtifact:false,traces,metadata:host.metadata,listenerClosed:host.metadata.lifecycle==='CLOSED',databaseDiscarded:host.metadata.databaseDiscarded,checkpointMemoryDisposed:true},null,2)+'\n');process.stdout.write(JSON.stringify({traces:traces.length,actualTcp:true,listenerClosed:true,databaseDiscarded:true,credentialArtifact:false})+'\n');
}else{
 // This channel is inherited from the spawning supervisor, never the public socket/stdout/file.
 const send=(message:unknown)=>new Promise<void>((done,reject)=>{if(!process.connected||!process.send){done();return;}process.send(message,error=>error?reject(error):done());});
 let stopping:Promise<void>|undefined,pending=Promise.resolve();
 const stop=()=>{if(!stopping)stopping=(async()=>{await host.close();await send({kind:'STOPPED',metadata:host.metadata});if(process.connected&&process.disconnect)process.disconnect();})();return stopping;};
 process.once('disconnect',()=>{void stop().catch(()=>{process.exitCode=1;});});process.once('SIGINT',()=>{void stop().catch(()=>{process.exitCode=1;});});process.once('SIGTERM',()=>{void stop().catch(()=>{process.exitCode=1;});});
 process.on('message',(message:unknown)=>{pending=pending.then(async()=>{
  if(stopping)return;if(!message||typeof message!=='object')return send({kind:'ERROR',code:'world_socket_ipc_invalid'});const m=message as {kind?:unknown;role?:unknown;requestId?:unknown};if(typeof m.requestId!=='string'||!/^[A-Za-z0-9_-]{1,64}$/u.test(m.requestId))return send({kind:'ERROR',code:'world_socket_ipc_invalid'});
  if(m.kind==='STOP'){await stop();return;}if(!['train','operator','outsider'].includes(String(m.role)))return send({kind:'ERROR',requestId:m.requestId,code:'world_socket_ipc_invalid'});const role=m.role as WorldSocketRole;
  if(m.kind==='INTAKE')await send({kind:'INTAKE',requestId:m.requestId,intake:host.intake(role)});else if(m.kind==='RENEW')await send({kind:'INTAKE',requestId:m.requestId,intake:await host.renew(role)});else if(m.kind==='REVOKE'){await host.revoke(role);await send({kind:'REVOKED',requestId:m.requestId});}else await send({kind:'ERROR',requestId:m.requestId,code:'world_socket_ipc_invalid'});
 }).catch(async()=>{process.exitCode=1;try{await send({kind:'ERROR',code:'world_socket_ipc_failed'});}finally{await stop();}});});
 void host.closed.then(()=>stop()).catch(()=>{process.exitCode=1;});await send({kind:'READY',metadata:host.metadata});
}