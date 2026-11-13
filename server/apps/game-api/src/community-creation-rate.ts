export const communityCreationRateVersion='community.creation-rate.v1.20261008' as const;
export const communityCreationLimits=Object.freeze({POST:Object.freeze({minimumInterval:60000,hourly:10}),COMMENT:Object.freeze({minimumInterval:10000,hourly:60})});
/** Approved server rolling window (t-60m,t], successful creation events only. */
export function evaluateCommunityCreationRate(kind:'POST'|'COMMENT',events:readonly number[],at:number):{retryAfter:number}|null{
 if(!Number.isSafeInteger(at)||at<0||events.some(t=>!Number.isSafeInteger(t)||t<0||t>at))throw new Error('community_rate_source_invalid');
 const current=events.filter(t=>t>at-3600000).sort((a,b)=>a-b),limits=communityCreationLimits[kind];
 const last=current.at(-1);let until=last===undefined?at:last+limits.minimumInterval;
 if(current.length>=limits.hourly)until=Math.max(until,current[current.length-limits.hourly]!+3600000);
 return until>at?{retryAfter:Math.ceil((until-at)/1000)}:null;
}