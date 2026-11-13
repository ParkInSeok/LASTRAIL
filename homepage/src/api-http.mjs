// HTTP owns request lifetime only. No credentials, account state, UI or retries are stored here.
export class WebApiError extends Error {
 constructor(kind,{status=null,code=null,retryAfterSeconds=null,maintenanceConfirmed=null,maintenanceProtocol=false}={}){super(kind);this.name='WebApiError';this.kind=kind;this.status=status;this.code=code;this.retryAfterSeconds=retryAfterSeconds;this.maintenanceConfirmed=maintenanceConfirmed;this.maintenanceProtocol=maintenanceProtocol;}
}
export function requireInput(condition){if(!condition)throw new WebApiError('invalid');}
export function requireResponse(condition){if(!condition)throw new WebApiError('invalid-response');}
export class WebHttp {
 #origin; #version; #fetch; #timeout;
 constructor({origin,environment,versionCode,fetch=globalThis.fetch,timeoutMs=10000}){
  const url=new URL(origin);
  requireInput(url.protocol==='https:'&&!url.username&&!url.password&&url.pathname==='/'&&!url.search&&!url.hash&&['DEV','PRD'].includes(environment));
  requireInput(versionCode===undefined&&environment==='PRD'||Number.isSafeInteger(versionCode)&&versionCode>0&&versionCode<=2147483647);
  requireInput(typeof fetch==='function'&&Number.isSafeInteger(timeoutMs)&&timeoutMs>0&&timeoutMs<=60000);
  this.#origin=url.origin;this.#version=versionCode;this.#fetch=fetch;this.#timeout=timeoutMs;
 }
 async request(path,{method='GET',body,csrfToken,key,signal,anonymous=false,communityVersion,communityRead,communityBlock,communityPublic,communityConsent,communityReport}={}){
  signal?.throwIfAborted();
  requireInput(!anonymous||(method==='GET'&&body===undefined&&csrfToken===undefined&&key===undefined));
  const headers=new Headers({'Accept':'application/json'});
  if(!anonymous&&this.#version!==undefined&&!path.startsWith('/v1/web/sessions/'))headers.set('X-LastRail-App-Version-Code',String(this.#version));
  if(body!==undefined)headers.set('Content-Type','application/json');
  if(csrfToken!==undefined){requireInput(typeof csrfToken==='string'&&csrfToken.length>0&&!/[\r\n]/.test(csrfToken));headers.set('X-CSRF-Token',csrfToken);}
  if(key!==undefined){requireInput(typeof key==='string'&&/^[\x21-\x7e]{16,128}$/.test(key));headers.set('Idempotency-Key',key);}
  const serialized=body===undefined?undefined:JSON.stringify(body);
  if(communityVersion!==undefined){
   const edit=communityVersion==='community.content-edit.v1.20261008',comment=communityVersion==='community.comment-create.v1.20261008',post=communityVersion==='community.post-create.v1.20261008';
   requireInput((edit||comment||post||communityVersion==='community.content-store.v1.20261008')&&method==='POST'&&(post?path==='/v1/web/community/posts':new RegExp('^/v1/web/community/contents/[A-Za-z0-9_-]{1,128}/'+(edit?'edit':comment?'comments':'delete')+'$').test(path)));
   requireInput(serialized!==undefined&&new TextEncoder().encode(serialized).byteLength<=(edit||post?128000:18000));
   headers.set('X-Community-Content-Version',communityVersion);
   if(post||comment)headers.set('X-Community-Creation-Eligibility-Version','community.creation-eligibility.v1.20261008');
  }
  if(communityRead!==undefined){
   const detail=communityRead==='detail',posts=communityRead==='posts';
   requireInput((detail||posts||communityRead==='list')&&method==='GET'&&body===undefined&&csrfToken===undefined&&(detail?/^\/v1\/web\/community\/own\/contents\/[A-Za-z0-9_-]{1,128}$/.test(path):path.startsWith(posts?'/v1/web/community/own/posts?':'/v1/web/community/own/comments?')));
   headers.set(detail?'X-Community-Own-Read-Version':posts?'X-Community-Own-Posts-Version':'X-Community-Own-Comments-Version',detail?'community.own-content-read.v1.20261008':posts?'community.own-posts-list.v1.20261008':'community.own-comments-list.v1.20261008');
  }
  if(communityBlock!==undefined){
   requireInput(communityBlock===true&&(method==='POST'?path==='/v1/web/community/blocks'&&serialized!==undefined&&new TextEncoder().encode(serialized).byteLength<=18000:method==='GET'&&path.startsWith('/v1/web/community/blocks?')&&body===undefined&&csrfToken===undefined));
   headers.set('X-Community-Block-Version','community.personal-block.v1.20261008');
  }
  if(communityPublic!==undefined){
   requireInput(communityPublic===true&&!anonymous&&method==='GET'&&body===undefined&&csrfToken===undefined&&(/^(?:\/v1\/web\/community\/contents\/[A-Za-z0-9_-]{1,128}|\/v1\/web\/community\/contents\/[A-Za-z0-9_-]{1,128}\/comments\?.*|\/v1\/web\/community\/posts\?.*)$/.test(path)));
   headers.set('X-Community-Public-Version','community.public-read.v1.20261008');
  }
  if(communityConsent!==undefined){requireInput(communityConsent===true&&!anonymous&&(method==='GET'?path==='/v1/web/community/consent'&&body===undefined&&csrfToken===undefined:method==='POST'&&path==='/v1/web/community/consent/accept'&&serialized!==undefined&&new TextEncoder().encode(serialized).byteLength<=18000));headers.set('X-Community-Consent-Version','community.consent.v1.20261008');}
  if(communityReport!==undefined){const submit=communityReport==='submit';requireInput(!anonymous&&(submit?method==='POST'&&path==='/v1/web/community/reports'&&serialized!==undefined&&new TextEncoder().encode(serialized).byteLength<=26000:communityReport==='read'&&method==='GET'&&/^\/v1\/web\/community\/own\/reports\/[A-Za-z0-9_-]{1,128}$/.test(path)&&body===undefined&&csrfToken===undefined));headers.set(submit?'X-Community-Report-Version':'X-Community-Own-Report-Version',submit?'community.report-submit.v1.20261008':'community.own-report-read.v1.20261008');}
  const request=new AbortController();const abort=()=>request.abort(signal.reason);
  signal?.addEventListener('abort',abort,{once:true});
  const timer=setTimeout(()=>request.abort(new WebApiError('timeout')),this.#timeout);
  try{
   let response;
   try{response=await this.#fetch(this.#origin+path,{method,headers,credentials:anonymous?'omit':'include',cache:'no-store',redirect:'error',signal:request.signal,...(body===undefined?{}:{body:serialized})});}
   catch{if(request.signal.aborted)throw request.signal.reason;throw new WebApiError('network');}
   request.signal.throwIfAborted();
   let data;try{data=await response.json();}catch{if(request.signal.aborted)throw request.signal.reason;throw new WebApiError('invalid-response',{status:response.status});}
   request.signal.throwIfAborted();
   if(!response.ok){
    let kind=({400:'invalid',401:'unauthenticated',403:'forbidden',404:'not-found',409:'conflict',429:'rate-limited',503:'unavailable'})[response.status]??'failure';
    const rawCode=data?.error??data?.errorCode;const code=typeof rawCode==='string'&&/^[A-Za-z0-9_]{1,100}$/.test(rawCode)?rawCode:null;
    let retryAfterSeconds=Number.isSafeInteger(data?.retryAfterSeconds)&&data.retryAfterSeconds>=0&&data.retryAfterSeconds<=86400?data.retryAfterSeconds:null;
    if(response.status===429&&method==='POST'&&(code==='community_report_rate_limited'&&path==='/v1/web/community/reports'||code==='community_rate_limited'&&(path==='/v1/web/community/posts'||/^\/v1\/web\/community\/contents\/[A-Za-z0-9_-]{1,128}\/comments$/.test(path)))){
     const delay=data?.retryAfter,header=response.headers.get('Retry-After');
     retryAfterSeconds=Number.isSafeInteger(delay)&&delay>=1&&delay<=86400&&header===String(delay)?delay:null;
    }
    const loginRoute=method==='POST'&&['/v1/web/sessions/password','/v1/web/sessions/google'].includes(path);
    const publicStatus=method==='GET'&&path==='/v1/public/maintenance/login';
    const supportedVersion=data?.contractVersion==='maintenance-login-v1'||publicStatus&&data?.contractVersion===undefined;
    let maintenanceConfirmed=null,maintenanceProtocol=false;
    if(response.status===503&&(loginRoute||publicStatus)&&supportedVersion){
     if(code==='maintenance_login_blocked'&&data?.contractVersion==='maintenance-login-v1'&&data?.confirmed===true){kind='maintenance';maintenanceConfirmed=true;maintenanceProtocol=true;}
     else if(code==='maintenance_policy_unavailable'&&data?.confirmed===false){maintenanceConfirmed=false;maintenanceProtocol=true;}
    }
    throw new WebApiError(kind,{status:response.status,code,retryAfterSeconds,maintenanceConfirmed,maintenanceProtocol});
   }
   requireResponse(data!==null&&typeof data==='object'&&!Array.isArray(data));return data;
  }finally{clearTimeout(timer);signal?.removeEventListener('abort',abort);}
 }
}










