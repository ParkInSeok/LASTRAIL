import {execFileSync} from 'node:child_process';
import {createHash} from 'node:crypto';
import {mkdir,mkdtemp,readFile,unlink,writeFile} from 'node:fs/promises';
import {resolve} from 'node:path';
import {createInterface} from 'node:readline';
import {startSupportSocket} from '../local/support-socket';

// Standalone loopback-only supplier. Credentials never appear in stdout/QA MD.
const args=process.argv.slice(2);
if(args.length>1||args.some(arg=>arg!=='--self-check'))throw new Error('Only --self-check is supported');
const selfCheck=args.includes('--self-check');
const project='D:/Project/LASTRAIL',checkout=resolve(project,'Worktrees/11ec/LASTRAIL/attendance-pass-20260907');
if(resolve(process.cwd())!==resolve(checkout,'server'))throw new Error('Use the canonical Server checkout');
const privateRoot=resolve(project,'LocalTemp');await mkdir(privateRoot,{recursive:true});
const privateDirectory=await mkdtemp(resolve(privateRoot,'support-socket-'));
if(process.platform==='win32'){
 const principal=process.env.USERDOMAIN+'\\'+process.env.USERNAME;
 execFileSync('icacls',[privateDirectory,'/inheritance:r','/grant:r',principal+':(OI)(CI)F'],{stdio:'ignore',windowsHide:true});
}
const root=resolve(project,'QA/CommanderS1-20260922/support-socket-runs');await mkdir(root,{recursive:true});
const evidence=await mkdtemp(resolve(root,'run-'));
const credentialPath=resolve(privateDirectory,'credentials.private.json'),unityPath=resolve(privateDirectory,'unity.private.json');
const sourceRevision=execFileSync('git',['-c','safe.directory='+checkout,'-C',checkout,'rev-parse','HEAD'],{encoding:'utf8'}).trim();
const sources=['scripts/local/support-socket.ts','scripts/validation/serve-support-socket.ts','apps/game-api/src/index.ts','apps/game-api/src/web-session-routes.ts','apps/game-api/src/support-private-read-routes.ts','apps/admin-api/src/index.ts','apps/admin-api/src/support-processing-routes.ts','apps/admin-api/src/support-grant-routes.ts','packages/common/src/support-ticket-service.ts','packages/common/src/support-grant-service.ts'];
const sourcePins=[];for(const path of sources)sourcePins.push({path,sha256:createHash('sha256').update(await readFile(path)).digest('hex')});
const host=await startSupportSocket();
let stopped=false;
async function stop(){
 if(stopped)return;stopped=true;
 try{await host.close();}
 finally{
  for(const path of [credentialPath,unityPath])await unlink(path).catch(error=>{if(error.code!=='ENOENT')throw error;});
  await writeFile(resolve(evidence,'lifecycle.json'),JSON.stringify({stopped:true,credentialsRemoved:true,inMemoryDatabaseDiscarded:true,listenerClosed:true},null,2)+'\n');
 }
}
try{
 const metadata={...host.metadata,sourceRevision,sourcePins,accessTokenExpiresAt:host.credentials.accounts[0]!.accessTokenExpiresAt};
 await writeFile(resolve(evidence,'environment.json'),JSON.stringify(metadata,null,2)+'\n');
 await writeFile(credentialPath,JSON.stringify({...host.credentials,origin:host.origin,webOrigin:host.webOrigin,gameHeaders:host.gameHeaders},null,2)+'\n',{mode:0o600});
 const account=host.credentials.accounts[0]!;
 await writeFile(unityPath,JSON.stringify({disposable:true,origin:host.origin,accountId:account.id,authorization:'Bearer '+account.token,profile:'SUPPORT_LOCAL_DEV',environment:'LOCAL_SQLJS',serverRevision:sourceRevision,
  appVersionCode:123,expiresAt:Date.parse(account.accessTokenExpiresAt)},null,2)+'\n',{mode:0o600});
 console.log(JSON.stringify({origin:host.origin,webOrigin:host.webOrigin,evidence,credentialPath,unityConnectionFile:unityPath,stop:'q + Enter or Ctrl+C; maximum lifetime 1 hour'}));
 if(selfCheck){
  const response=await fetch(host.origin+'/v1/support/tickets',{headers:{...host.gameHeaders,Authorization:'Bearer '+account.token}});
  if(response.status!==200||(await response.json() as {items:unknown[]}).items.length!==0)throw new Error('Local support supplier self-check failed');
  await writeFile(resolve(evidence,'self-check.json'),JSON.stringify({status:'PASS',actualSocket:true,ownListHttpStatus:response.status,disposable:true})+'\n');
 }else{
  await new Promise<void>(done=>{
   const input=createInterface({input:process.stdin});
   const finish=()=>{clearTimeout(timer);input.close();process.removeListener('SIGINT',finish);process.removeListener('SIGTERM',finish);done();};
   const timer=setTimeout(finish,3600000);
   input.on('line',line=>{if(line.trim()==='q')finish();});input.once('close',finish);
   process.once('SIGINT',finish);process.once('SIGTERM',finish);
  });
 }
}finally{await stop();}
