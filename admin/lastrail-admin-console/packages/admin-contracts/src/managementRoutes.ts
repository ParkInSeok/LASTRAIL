export type ManagementRoute={requiresKey:boolean;error?:'contract_violation'};
/** Shared transport allowlist only; domain/auth/transaction validation stays in the original Server. */
export function managementRoute(path:string,method:string,query:URLSearchParams,hasIfMatch:boolean):ManagementRoute|null{
 if(!/^\/(?:liveops|reward-tracking|unity-builds|events)(?:\/|$)/.test(path))return null;
 let expected='GET',allowed:string[]=[],requiresKey=false;
 if(path==='/liveops/capabilities'||path==='/reward-tracking/capabilities'){}
 else if(path==='/reward-tracking/review-preview')expected='POST';
 else if(path==='/events/definition-preview'||path==='/events/outage-preview')expected='POST';
 else if(path==='/events/cosmetic-replacement-table'){}
 else if(path==='/events/definitions')allowed=['after','limit'];
 else if(path.startsWith('/events/definitions/')||path.startsWith('/reward-tracking/drafts/')){
  const match=/^\/(?:events\/definitions|reward-tracking\/drafts)\/([^/]+)(?:\/(commands))?$/.exec(path);
  if(!match)return {requiresKey:false,error:'contract_violation'};
  let id:string;try{id=decodeURIComponent(match[1]);}catch{return {requiresKey:false,error:'contract_violation'};}
  if(!id.trim()||id.length>128||id!==id.trim())return {requiresKey:false,error:'contract_violation'};
  if(match[2]){expected='POST';requiresKey=true;}
 }
 else if(path==='/unity-builds/capabilities'){}
 else if(path==='/unity-builds'){if(method==='POST'){expected='POST';requiresKey=true;}else allowed=['after','limit'];}
 else if(path.startsWith('/unity-builds/')){const match=/^\/unity-builds\/[A-Za-z0-9][A-Za-z0-9._-]{0,127}(?:\/(cancel|logs|artifacts))?$/.exec(path);if(!match)return {requiresKey:false,error:'contract_violation'};if(match[1]==='cancel'){expected='POST';requiresKey=true;}if(match[1]==='logs')allowed=['after'];}
 else if(path==='/liveops/notices')allowed=['after','limit'];
 else{const match=/^\/liveops\/notices\/[A-Za-z0-9][A-Za-z0-9_-]{0,79}(?:\/(history|commands))?$/.exec(path);if(!match)return {requiresKey:false,error:'contract_violation'};if(match[1]==='history')allowed=['before','limit'];if(match[1]==='commands'){expected='POST';requiresKey=true;}}
 const error=method!==expected||hasIfMatch||Array.from(query.keys()).some(k=>!allowed.includes(k)||query.getAll(k).length!==1);
 return error?{requiresKey,error:'contract_violation'}:{requiresKey};
}

