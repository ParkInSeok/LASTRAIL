import {readFile,realpath,lstat,mkdtemp,writeFile,unlink,rmdir} from 'node:fs/promises';
import {tmpdir} from 'node:os';
import {resolve,dirname,basename,join,sep} from 'node:path';
import {execFile} from 'node:child_process';
import {promisify} from 'node:util';
import {sha256,canonical,validateManifest} from './publication.mjs';

const execute=promisify(execFile);
const project=resolve(import.meta.dirname,'../..');
const android='D:/Program Files/Unity/Hub/Editor/6000.3.22f1/Editor/Data/PlaybackEngines/AndroidPlayer';
const ownerRoot='D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL';
const ownerHelper=join(ownerRoot,'gacha-audit-20260907/content-release-operations.mts');
const tsx=join(ownerRoot,'gacha-release-20260907-3/server/node_modules/tsx/dist/cli.mjs');

export async function verifySnapshot(bytes,verify){
  const snapshot=Buffer.from(bytes);
  const root=await mkdtemp(join(tmpdir(),'lastrail-aab-verify-'));
  const path=join(root,'artifact.aab');
  try{
    await writeFile(path,snapshot,{flag:'wx',mode:0o400});
    await verify(path);
    if(!snapshot.equals(await readFile(path)))throw new Error('VERIFICATION_SNAPSHOT_CHANGED');
    return snapshot;
  }finally{await unlink(path).catch(error=>{if(error.code!=='ENOENT')throw error;});await rmdir(root);}
}

async function regularFile(path){
  const absolute=resolve(path);
  if((await realpath(absolute)).toLowerCase()!==absolute.toLowerCase())throw new Error('LINKED_ARTIFACT_REJECTED');
  if(!(await lstat(absolute)).isFile())throw new Error('ARTIFACT_FILE_REQUIRED');
  return absolute;
}

export async function verifyApp(m){
  validateManifest(m);
  const path=await regularFile(m.appPath);
  const expected=resolve(project,'Builds','Android','DEV',m.version+'_'+m.versionCode,'LASTRAIL_DEV_'+m.version+'_'+m.versionCode+'.aab');
  if(path.toLowerCase()!==expected.toLowerCase())throw new Error('APP_OUTPUT_PATH_MISMATCH');
  const bytes=await readFile(path);
  if(bytes.length!==m.appBytes||sha256(bytes)!==m.appSha256)throw new Error('APP_BYTES_CHANGED');
  const java=join(android,'OpenJDK/bin/java.exe');
  return verifySnapshot(bytes,async snapshotPath=>{
  try{
    await execute(java,[join(project,'Tools/VerifySignedBundle.java'),snapshotPath,m.signerSha256],{windowsHide:true,timeout:120000,maxBuffer:1024*1024});
    const fields={package:m.package,'android:versionCode':String(m.versionCode),'android:versionName':m.version};
    for(const [field,value] of Object.entries(fields)){
      const result=await execute(java,['-jar',join(android,'Tools/bundletool-all-1.17.2.jar'),'dump','manifest','--bundle='+snapshotPath,'--xpath=/manifest/@'+field],{windowsHide:true,timeout:120000,maxBuffer:1024*1024});
      if(result.stdout.trim()!==value)throw new Error('APP_MANIFEST_MISMATCH');
    }
  }catch{throw new Error('APP_SIGNATURE_OR_IDENTITY_INVALID');}
  });
}

export async function readCandidate(m){
  const path=await regularFile(m.candidatePath);
  if(basename(path)!=='candidate.json'||!path.toLowerCase().startsWith((resolve(project,'QA')+sep).toLowerCase()))throw new Error('CANDIDATE_PATH_REJECTED');
  const bytes=await readFile(path);
  if(sha256(bytes)!==m.candidateSha256)throw new Error('CANDIDATE_BYTES_CHANGED');
  const candidate=JSON.parse(bytes.toString('utf8'));const c=candidate.configuration;
  if(candidate.status!=='LOCAL_BUILT_NOT_DEPLOYED_NOT_DEVICE_VERIFIED'||c?.environment!=='DEV'||c.target!=='Android'||c.appVersion!==m.version||c.versionCode!==m.versionCode)throw new Error('CONTENT_APP_IDENTITY_MISMATCH');
  const release={status:'APPROVED',releaseId:c.releaseId,contentVersion:c.contentVersion,catalogUrl:candidate.catalogUrl,catalogHash:candidate.catalogHash,minimumVersionCode:String(c.minimumVersionCode),maximumVersionCode:String(c.maximumVersionCode)};
  if(canonical(release)!==canonical(m.contentRelease)||c.trustedCatalogRoot!=='https://dev-content.lastrail.app/android/releases/'||c.releaseNamespace!==c.trustedCatalogRoot+c.releaseId+'/')throw new Error('CONTENT_RELEASE_MISMATCH');
  return candidate;
}

export function contentOwner(){
  async function run(mode,m){
    const candidate=await readCandidate(m);const c=candidate.configuration;
    // Preserve the owner helper's existing exact scope. No legacy activation mode.
    if(c.appVersion!=='0.1.0'||c.versionCode!==1||c.minimumVersionCode!==1||c.maximumVersionCode!==1)throw new Error('CONTENT_OWNER_GENERALIZED_SCOPE_REQUIRED');
    try{
      await execute(process.execPath,[tsx,ownerHelper,mode,resolve(m.candidatePath)],{cwd:ownerRoot,windowsHide:true,timeout:900000,maxBuffer:4*1024*1024});
    }catch{throw new Error('CONTENT_OWNER_OPERATION_FAILED');}
    const receipt=JSON.parse(await readFile(join(ownerRoot,'gacha-audit-20260907/content-release-'+c.releaseId+'.json'),'utf8'));
    if(receipt.candidateSha256!==m.candidateSha256||receipt.releaseId!==c.releaseId||!Array.isArray(receipt.files)||!receipt.files.length)throw new Error('CONTENT_OWNER_RECEIPT_MISMATCH');
    if(mode==='upload'&&receipt.uploaded!==true)throw new Error('CONTENT_UPLOAD_UNCONFIRMED');
    if(mode==='verify'&&(receipt.uploaded!==true||receipt.httpsVerified!==true))throw new Error('CONTENT_HTTPS_UNCONFIRMED');
    return receipt;
  }
  return {audit:m=>run('audit',m),upload:m=>run('upload',m),verify:m=>run('verify',m)};
}
