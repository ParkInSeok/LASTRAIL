import {writeFileSync} from 'node:fs';
import {join} from 'node:path';
import {startAugmentLoopback} from './augment-loopback-http';
const directory=process.env.LASTRAIL_AUGMENT_TRIAL_DIR;
if(!directory)throw Error('LASTRAIL_AUGMENT_TRIAL_DIR_required');
const host=await startAugmentLoopback({directory,...(process.env.LASTRAIL_AUGMENT_TRIAL_LIFETIME_MS?{lifetimeMs:Number(process.env.LASTRAIL_AUGMENT_TRIAL_LIFETIME_MS)}:{})});
const stop=()=>{void host.close();};process.once('SIGINT',stop);process.once('SIGTERM',stop);
try{
 writeFileSync(join(directory,'bootstrap.json'),JSON.stringify({...host.bootstrap,processId:process.pid},null,2));
 console.log(JSON.stringify({version:host.bootstrap.version,baseUrl:host.bootstrap.baseUrl,classification:host.bootstrap.classification,bootstrapPath:join(directory,'bootstrap.json')}));
 await host.closed;
}finally{process.removeListener('SIGINT',stop);process.removeListener('SIGTERM',stop);await host.close();}
