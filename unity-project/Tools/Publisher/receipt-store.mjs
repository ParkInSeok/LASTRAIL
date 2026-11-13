import {open,readFile,rename,unlink,mkdir} from 'node:fs/promises';
import {join,resolve} from 'node:path';
import {randomUUID} from 'node:crypto';

export class ReceiptStore {
  constructor(root){this.root=resolve(root);}
  path(m){if(m.environment!=='DEV'||!Number.isSafeInteger(m.versionCode)||m.versionCode<1)throw new Error('INVALID_RECEIPT_ID');return join(this.root,'DEV-'+m.versionCode+'.json');}
  async load(m){try{return JSON.parse(await readFile(this.path(m),'utf8'));}catch(error){if(error.code==='ENOENT')return undefined;throw new Error('RECEIPT_UNREADABLE');}}
  async save(value,m){
    await mkdir(this.root,{recursive:true});
    const destination=this.path(m),temporary=destination+'.'+randomUUID()+'.tmp';
    const file=await open(temporary,'wx');
    try{await file.writeFile(JSON.stringify(value,null,2));await file.sync();}finally{await file.close();}
    try{await rename(temporary,destination);}catch{await unlink(temporary);throw new Error('RECEIPT_PERSIST_FAILED');}
  }
  async lock(m){
    await mkdir(this.root,{recursive:true});
    const path=this.path(m)+'.lock';
    let held;
    try{held=await open(path,'wx');}catch{throw new Error('PUBLICATION_LOCKED_RECONCILE_INTERRUPTED_RUN');}
    await held.writeFile(JSON.stringify({pid:process.pid,operation:randomUUID()}));await held.sync();
    return async()=>{await held.close();await unlink(path);};
  }
}
