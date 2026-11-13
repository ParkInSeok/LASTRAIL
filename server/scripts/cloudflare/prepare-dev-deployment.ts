import {readFile,writeFile} from 'node:fs/promises';
import {resolve,dirname,basename,join} from 'node:path';
import {pathToFileURL} from 'node:url';
import {parse,type ParseError} from 'jsonc-parser';
import {createDevDeploymentConfig} from './dev-deployment-config';
/** Prepare only; never deploy, pause/resume a queue, or change remote settings. */
export async function prepareDevDeployment(argv:string[]) {
  const allowed=new Set(['--config','--service','--state','--queue-observation']);
  const args=new Map<string,string>();
  for(let i=0;i<argv.length;i+=2){const key=argv[i],value=argv[i+1];if(!key||!allowed.has(key)||!value||value.startsWith('--')||args.has(key))throw Error('invalid_deployment_arguments');args.set(key,value);}
  if(args.size!==4)throw Error('required: --config --service --state --queue-observation');
  const path=resolve(args.get('--config')!);
  if(basename(path)!=='wrangler.jsonc'||basename(dirname(path))!==args.get('--service'))throw Error('deployment_config_path_mismatch');
  const errors:ParseError[]=[];const config:unknown=parse(await readFile(path,'utf8'),errors);
  if(errors.length)throw Error('invalid_deployment_config_jsonc');
  const evidence:unknown=JSON.parse(await readFile(resolve(args.get('--queue-observation')!),'utf8'));
  const prepared=createDevDeploymentConfig(config,args.get('--service'),args.get('--state'),evidence);
  // Same directory preserves all Wrangler-relative resource and migration paths.
  const output=join(dirname(path),'wrangler.dev-deployment.jsonc');
  await writeFile(output,JSON.stringify(prepared,null,2)+'\n',{flag:'wx'});
  return {output,environment:'dev',state:args.get('--state'),remoteOperations:0,queueObservationIsNotLiveLock:true};
}
if(process.argv[1]&&import.meta.url===pathToFileURL(process.argv[1]).href){
 prepareDevDeployment(process.argv.slice(2)).then(result=>console.log(JSON.stringify(result))).catch(error=>{console.error(error instanceof Error?error.message:'deployment_prepare_failed');process.exitCode=1;});
}
