import {appendFileSync,existsSync,realpathSync,writeFileSync} from 'node:fs';
import {join,relative} from 'node:path';
import {startDataLifecycleLoopback} from './data-lifecycle-loopback-http';
const directory=realpathSync(process.env.LASTRAIL_DATA_TRIAL_DIR??'');
const relativePath=relative(realpathSync('D:/Project/LASTRAIL/QA/CommanderS1-20260922'),directory);
if(!relativePath||relativePath.startsWith('..')||relativePath.includes(':'))throw Error('local_directory_forbidden');
if(existsSync(join(directory,'bootstrap.json')))throw Error('new_trial_requires_fresh_directory');
const host=await startDataLifecycleLoopback({onRequest:entry=>appendFileSync(join(directory,'requests.jsonl'),JSON.stringify(entry)+'\n')});
const stop=()=>{void host.close();};process.once('SIGINT',stop);process.once('SIGTERM',stop);
try{
 writeFileSync(join(directory,'bootstrap.json'),JSON.stringify({...host.bootstrap,processId:process.pid},null,2));
 console.log(JSON.stringify({baseUrl:host.bootstrap.baseUrl,bootstrapPath:join(directory,'bootstrap.json'),classification:host.bootstrap.classification}));
 await host.closed;
}finally{process.removeListener('SIGINT',stop);process.removeListener('SIGTERM',stop);await host.close();}
