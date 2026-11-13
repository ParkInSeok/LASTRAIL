const base='https://dev-admin.lastrail.app/v1/admin/client-releases';
const error=(code,status=0)=>Object.assign(new Error(code),{code,status});
export class RegistryApi {
  constructor({credentialProvider,fetchImpl=globalThis.fetch}){this.credentials=credentialProvider;this.fetch=fetchImpl;}
  get(){return this.request('GET',base+'?target=DEV');}
  register(body){
    if(body?.target!=='DEV'||Object.keys(body).some(k=>!['target','expectedRevision','reason','version','versionCode','contentRelease','legacyTransitionConfirmation'].includes(k)))throw error('REGISTRY_REQUEST_REJECTED');
    return this.request('POST',base+'/register',JSON.stringify(body));
  }
  async request(method,url,body){
    let credentials;
    try{credentials=await this.credentials();}catch{throw error('REGISTRY_AUTH_REQUIRED');}
    if(!credentials||!['clientId','clientSecret','userAccessJwt'].every(k=>typeof credentials[k]==='string'&&credentials[k].length>0&&!/[\r\n]/.test(credentials[k])))throw error('REGISTRY_AUTH_REQUIRED');
    const headers={'CF-Access-Client-Id':credentials.clientId,'CF-Access-Client-Secret':credentials.clientSecret,
      'X-Lastrail-User-Access-Jwt':credentials.userAccessJwt,Accept:'application/json'};
    if(body)headers['Content-Type']='application/json';
    let response;
    try{response=await this.fetch(url,{method,headers,body,redirect:'error',signal:AbortSignal.timeout(30000)});}
    catch{throw error('REGISTRY_TRANSPORT_ERROR');}
    finally{credentials=undefined;}
    if(response.redirected||response.status<200||response.status>=300)throw error('REGISTRY_HTTP_ERROR',response.status);
    try{return await response.json();}catch{throw error('REGISTRY_INVALID_RESPONSE');}
  }
}
