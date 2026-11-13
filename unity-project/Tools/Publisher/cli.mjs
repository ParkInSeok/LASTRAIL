import {readFile} from 'node:fs/promises';
import {isAbsolute,resolve,basename} from 'node:path';
import {PlayApi} from './play-api.mjs';
import {RegistryApi} from './registry-api.mjs';
import {ReceiptStore} from './receipt-store.mjs';
import {runPublication,validateManifest} from './publication.mjs';
import {verifyApp,contentOwner} from './local-adapters.mjs';

const tokenProvider=async()=>process.env.LASTRAIL_PLAY_ACCESS_TOKEN;
const admin=async()=>({clientId:process.env.LASTRAIL_ADMIN_ACCESS_CLIENT_ID,clientSecret:process.env.LASTRAIL_ADMIN_ACCESS_CLIENT_SECRET,userAccessJwt:process.env.LASTRAIL_ADMIN_USER_ACCESS_JWT});
try{
  const [mode,path,...flags]=process.argv.slice(2);
  const play=new PlayApi({tokenProvider,timeoutMs:600000});
  if(mode==='readiness'&&path===undefined){
    if(!process.env.LASTRAIL_PLAY_ACCESS_TOKEN)throw new Error('PLAY_AUTH_REQUIRED');
    const result=await play.readCommittedReleases();
    if(!Array.isArray(result.releases))throw new Error('PLAY_READINESS_INVALID');
    console.log(JSON.stringify({phase:'PLAY_READ_ONLY',package:'app.lastrail.game.dev',track:'internal',releaseCount:result.releases.length,mutationPerformed:false}));
  }else{
    if(mode!=='publish'||!path||!isAbsolute(path)||flags.some(flag=>flag!=='--execute'&&flag!=='--acknowledge-legacy-cutover')||!flags.includes('--execute'))throw new Error('EXPLICIT_PUBLICATION_ARGUMENTS_REQUIRED');
    const manifest=validateManifest(JSON.parse(await readFile(path,'utf8')));
    const output=resolve(import.meta.dirname,'../../Builds/Android/DEV',manifest.version+'_'+manifest.versionCode,'publication-manifest.json');
    if(resolve(path).toLowerCase()!==output.toLowerCase()||basename(path)!=='publication-manifest.json')throw new Error('PUBLICATION_MANIFEST_PATH_REJECTED');
    if(!process.env.LASTRAIL_PLAY_ACCESS_TOKEN)throw new Error('PLAY_AUTH_REQUIRED');
    const credentials=await admin();
    if(!Object.values(credentials).every(value=>typeof value==='string'&&value.length>0))throw new Error('REGISTRY_AUTH_REQUIRED');
    const registry=new RegistryApi({credentialProvider:admin});
    // Read-only permission probes precede content mutations; runtime still checks policy.
    await play.readCommittedReleases();await registry.get();
    const state=await runPublication(manifest,{play,registry,verifyApp,content:contentOwner(),store:new ReceiptStore(resolve(import.meta.dirname,'../../Builds/Android/.publication-receipts')),
      legacyCutoverAcknowledged:flags.includes('--acknowledge-legacy-cutover')});
    console.log(JSON.stringify({phase:state.phase,environment:'DEV',package:manifest.package,versionCode:manifest.versionCode,playLifecycle:state.play.lifecycle,deviceVerified:false}));
  }
}catch(error){
  const safe=/^[A-Z][A-Z0-9_]{1,100}$/.test(error.code??'')?error.code:/^[A-Z][A-Z0-9_]{1,100}$/.test(error.message??'')?error.message:'PUBLICATION_FAILED';
  console.error(JSON.stringify({phase:'BLOCKED',code:safe,status:Number.isInteger(error.status)?error.status:undefined}));
  process.exitCode=1;
}finally{
  for(const name of ['LASTRAIL_PLAY_ACCESS_TOKEN','LASTRAIL_ADMIN_ACCESS_CLIENT_ID','LASTRAIL_ADMIN_ACCESS_CLIENT_SECRET','LASTRAIL_ADMIN_USER_ACCESS_JWT'])delete process.env[name];
}
