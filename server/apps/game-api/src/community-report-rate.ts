export const communityReportRateVersion='community.report-rate.v1.20261008' as const;
export const communityReportRateWindows=Object.freeze({minute:Object.freeze({duration:60000,limit:3}),day:Object.freeze({duration:86400000,limit:20})});
/** Successful new cases only, server rolling lower-exclusive windows; maximum release, rounded up seconds. */
export function evaluateCommunityReportRate(events:readonly number[],at:number){
 if(!Number.isSafeInteger(at)||at<0||events.some(t=>!Number.isSafeInteger(t)||t<0||t>at))throw new Error('community_report_rate_source_invalid');
 let release=at;for(const window of Object.values(communityReportRateWindows)){const live=events.filter(t=>t>at-window.duration).sort((a,b)=>a-b);if(live.length>=window.limit)release=Math.max(release,live[live.length-window.limit]!+window.duration);}
 return release>at?{retryAfter:Math.ceil((release-at)/1000)}:null;
}