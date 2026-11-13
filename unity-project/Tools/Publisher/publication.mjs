import {createHash} from 'node:crypto';

export const canonical = value => value === null || typeof value !== 'object' ? JSON.stringify(value)
  : Array.isArray(value) ? '['+value.map(canonical).join(',')+']'
  : '{'+Object.keys(value).sort().map(key=>JSON.stringify(key)+':'+canonical(value[key])).join(',')+'}';
export const sha256 = bytes => createHash('sha256').update(bytes).digest('hex');
const fail = code => {throw new Error(code);};
const require = (condition,code) => {if(!condition)fail(code);};
const codeNumber = value => /^\d+$/.test(String(value)) && Number.isSafeInteger(Number(value)) && Number(value)>0 && Number(value)<=2100000000;

export function validateManifest(m) {
  require(m?.schemaVersion===1&&m.environment==='DEV'&&m.package==='app.lastrail.game.dev'&&m.track==='internal'&&m.format==='aab','DEV_INTERNAL_AAB_ONLY');
  require(/^[A-Za-z0-9][A-Za-z0-9._+-]{0,63}$/.test(m.version)&&codeNumber(m.versionCode)&&typeof m.versionCode==='number','INVALID_APP_VERSION');
  require(['appSha256','signerSha256','candidateSha256'].every(key=>/^[a-f0-9]{64}$/.test(m[key]))&&Number.isSafeInteger(m.appBytes)&&m.appBytes>0,'INVALID_ARTIFACT_IDENTITY');
  const c=m.contentRelease;
  require(c?.status==='APPROVED'&&/^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$/.test(c.releaseId)&&typeof c.contentVersion==='string'&&c.contentVersion.length>0,'INVALID_CONTENT_IDENTITY');
  const root='https://dev-content.lastrail.app/android/releases/'+c.releaseId+'/';
  require(typeof c.catalogUrl==='string'&&c.catalogUrl.startsWith(root)&&/^catalog[A-Za-z0-9._-]*\.bin$/.test(c.catalogUrl.slice(root.length))&&/^sha256:[a-f0-9]{64}$/.test(c.catalogHash),'CONTENT_AUTHORITY_MISMATCH');
  require(codeNumber(c.minimumVersionCode)&&codeNumber(c.maximumVersionCode)&&Number(c.minimumVersionCode)<=m.versionCode&&Number(c.maximumVersionCode)>=m.versionCode,'CONTENT_COMPATIBILITY_MISMATCH');
  require(typeof m.appPath==='string'&&typeof m.candidatePath==='string','ARTIFACT_PATH_REQUIRED');
  const permitted=['status','releaseId','contentVersion','catalogUrl','catalogHash','minimumVersionCode','maximumVersionCode'];
  require(Object.keys(c).length===permitted.length&&Object.keys(c).every(key=>permitted.includes(key)),'UNEXPECTED_CONTENT_FIELD');
  return m;
}

function guardRegistry(dto,m) {
  const p=dto?.distributionPolicy;
  require(dto?.target==='DEV'&&dto.canMutate===true&&dto.capabilities?.register===true&&dto.capabilities?.promote===false,'REGISTRY_AUTH_OR_TARGET_REQUIRED');
  require(p?.initialStatus==='STABLE'&&p.testerGate===false&&canonical(p.supportedStatuses?.slice().sort())===canonical(['DISABLED','STABLE'])&&canonical(p.mutableStatuses?.slice().sort())===canonical(['DISABLED','STABLE']),'REGISTRY_DEPLOYMENT_POLICY_REQUIRED');
  require(typeof dto.revision==='string'&&/^"[^"\r\n]+"$/.test(dto.revision)&&Array.isArray(dto.rows),'REGISTRY_REVISION_REQUIRED');
  require(codeNumber(dto.minimumVersionCode)&&m.versionCode>=Number(dto.minimumVersionCode),'REGISTRY_MINIMUM_CONFLICT');
  const rows=dto.rows.filter(row=>String(row.versionCode)===String(m.versionCode));
  require(rows.length<=1,'REGISTRY_DUPLICATE_CODE');
  if(!rows.length)return false;
  const row=rows[0];
  require(row.version===m.version&&canonical(row.contentRelease)===canonical(m.contentRelease)&&row.artifactIdentity==='sha256:'+sha256(canonical(m.contentRelease)),'REGISTRY_ARTIFACT_CONFLICT');
  require(row.status!=='DISABLED','REGISTRY_DISABLED');
  require(row.status==='STABLE','REGISTRY_STATUS_CONFLICT');
  return true;
}

function confirmedRelease(dto,m,state) {
  require(Array.isArray(dto?.releases),'PLAY_RELEASE_READBACK_INVALID');
  const matches=dto.releases.filter(r=>r.track==='internal'&&r.releaseName===state.play?.releaseName&&r.activeArtifacts?.some(a=>Number(a.versionCode)===m.versionCode));
  if(!matches.length)return null;
  require(matches.length===1&&state.play?.bundleSha256===m.appSha256,'PLAY_RELEASE_IDENTITY_UNCONFIRMED');
  const lifecycle=matches[0].releaseLifecycleState;
  require(['RELEASE_LIFECYCLE_STATE_PUBLISHED','RELEASE_LIFECYCLE_STATE_IN_REVIEW','RELEASE_LIFECYCLE_STATE_APPROVED_NOT_PUBLISHED','RELEASE_LIFECYCLE_STATE_NOT_SENT_FOR_REVIEW'].includes(lifecycle),'PLAY_RELEASE_NOT_READY');
  return lifecycle;
}

export async function runPublication(manifest,{store,verifyApp,content,registry,play,legacyCutoverAcknowledged=false}) {
  const m=validateManifest(structuredClone(manifest));
  Object.freeze(m.contentRelease);Object.freeze(m);
  const unlock=await store.lock(m);
  try {
    const identity=sha256(canonical(m));
    let state=await store.load(m);
    require(!state||state.manifestSha256===identity,'RECEIPT_IDENTITY_CONFLICT');
    state??={schemaVersion:1,manifestSha256:identity,phase:'LOCAL_VERIFIED',content:{},registry:{},play:{}};
    const save=async()=>store.save(state,m);
    const bytes=await verifyApp(m);
    require(bytes instanceof Uint8Array&&bytes.byteLength===m.appBytes&&sha256(bytes)===m.appSha256,'APP_BYTES_CHANGED');
    const initialRegistry=await registry.get();
    guardRegistry(initialRegistry,m);
    require(initialRegistry.registryStatus!=='LEGACY'||legacyCutoverAcknowledged===true,'LEGACY_CUTOVER_ACK_REQUIRED');
    require(Array.isArray((await play.readCommittedReleases())?.releases),'PLAY_RELEASE_READBACK_INVALID');
    await content.audit(m);
    await save();
    if(!state.content.uploaded){await content.upload(m);state.content.uploaded=true;state.phase='CONTENT_UPLOADED';await save();}
    await content.verify(m);
    state.content.httpsVerified=true;state.phase='CONTENT_HTTPS_VERIFIED';await save();
    let current=await registry.get();
    if(!guardRegistry(current,m)) {
      require(!state.registry.attempted,'REGISTRY_RECONCILIATION_REQUIRED');
      require(current.registryStatus==='CONFIGURED'||current.registryStatus==='LEGACY','REGISTRY_STATUS_UNKNOWN');
      require(current.registryStatus!=='LEGACY'||legacyCutoverAcknowledged===true,'LEGACY_CUTOVER_ACK_REQUIRED');
      const body={target:'DEV',expectedRevision:current.revision,reason:'Verified signed build '+identity,version:m.version,versionCode:String(m.versionCode),contentRelease:m.contentRelease};
      if(current.registryStatus==='LEGACY')body.legacyTransitionConfirmation='ENABLE_EXACT_LOOKUP';
      state.registry.attempted=true;state.registry.expectedRevision=current.revision;await save();
      let problem;
      try {await registry.register(body);}catch(error){problem=error;}
      current=await registry.get();
      if(!guardRegistry(current,m)) {if(problem)throw problem;fail('REGISTRY_READBACK_MISSING');}
    }
    state.registry.verified=true;state.registry.revision=current.revision;state.phase='REGISTRY_STABLE_VERIFIED';await save();

    let lifecycle=confirmedRelease(await play.readCommittedReleases(),m,state);
    if(lifecycle) {
      state.phase='PLAY_RELEASE_CONFIRMED';state.play.lifecycle=lifecycle;await save();return state;
    }
    require(!state.play.commitAttempted,'PLAY_COMMIT_UNCONFIRMED');
    if(state.play.editId) {
      try {await play.getEdit(state.play.editId);}catch {fail('PLAY_EDIT_RECONCILIATION_REQUIRED');}
    } else {
      require(!state.play.createAttempted,'PLAY_EDIT_CREATION_UNCONFIRMED');
      state.play.createAttempted=true;await save();
      const edit=await play.createEdit();
      require(typeof edit.id==='string'&&edit.id.length>0,'PLAY_EDIT_ID_MISSING');
      state.play.editId=edit.id;state.phase='PLAY_EDIT_CREATED';await save();
    }
    const id=state.play.editId;
    const tracks=await play.listTracks(id);
    require(Array.isArray(tracks?.tracks),'PLAY_TRACK_LIST_INVALID');
    const internal=tracks.tracks.filter(t=>t.track==='internal');
    require(internal.length===1,'PLAY_INTERNAL_TRACK_MISSING');
    const existingReleases=internal[0].releases??[];
    require(Array.isArray(existingReleases)&&existingReleases.every(r=>r.status==='completed'),'PLAY_TRACK_HAS_UNFINISHED_RELEASE');
    let bundles=(await play.listBundles(id)).bundles??[];
    require(Array.isArray(bundles),'PLAY_BUNDLE_LIST_INVALID');
    const match=()=>bundles.find(b=>Number(b.versionCode)===m.versionCode);
    if(match())require(match().sha256?.toLowerCase()===m.appSha256,'PLAY_CODE_CONFLICT');
    else {
      require(!state.play.uploadAttempted,'PLAY_UPLOAD_RECONCILIATION_REQUIRED');
      const known=tracks.tracks.flatMap(t=>(t.releases??[]).flatMap(r=>r.versionCodes??[])).concat(bundles.map(b=>b.versionCode)).map(Number);
      require(known.every(Number.isSafeInteger)&&known.every(n=>n<m.versionCode),'PLAY_REMOTE_VERSION_CODE_CONFLICT');
      state.play.uploadAttempted=true;await save();
      try {
        const uploaded=await play.uploadBundle(id,bytes);
        require(Number(uploaded.versionCode)===m.versionCode&&uploaded.sha256?.toLowerCase()===m.appSha256,'PLAY_UPLOAD_IDENTITY_MISMATCH');
      } catch(error) {
        bundles=(await play.listBundles(id)).bundles??[];
        if(!match()||match().sha256?.toLowerCase()!==m.appSha256)throw error;
      }
    }
    state.play.bundleSha256=m.appSha256;state.phase='PLAY_BUNDLE_VERIFIED';await save();
    const versionCodes=[...new Set(existingReleases.flatMap(r=>r.versionCodes??[]).concat(String(m.versionCode)).map(String))];
    require(versionCodes.every(codeNumber),'PLAY_TRACK_VERSION_INVALID');
    const release={name:'LASTRAIL_DEV_'+m.version+'_'+m.versionCode+'_'+m.appSha256.slice(0,12),versionCodes,status:'completed'};
    state.play.releaseName=release.name;await save();
    const updated=await play.updateInternal(id,release);
    require(updated?.track==='internal'&&updated.releases?.some(r=>r.name===release.name&&r.status==='completed'&&canonical(r.versionCodes?.slice().sort())===canonical(versionCodes.slice().sort())),'PLAY_TRACK_READBACK_MISMATCH');
    await play.validateEdit(id);
    state.play.commitAttempted=true;state.phase='PLAY_COMMIT_PENDING';await save();
    try {await play.commitEdit(id);state.play.commitResponded=true;await save();}catch { /* The next GET decides whether commit happened. */ }
    lifecycle=confirmedRelease(await play.readCommittedReleases(),m,state);
    require(lifecycle,'PLAY_COMMIT_UNCONFIRMED');
    state.play.lifecycle=lifecycle;state.phase='PLAY_RELEASE_CONFIRMED';await save();return state;
  } finally {await unlock();}
}
