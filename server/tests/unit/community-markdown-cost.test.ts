import { writeFileSync } from 'node:fs';
import { expect,it } from 'vitest';
import { renderCommunityMarkdown } from '../../apps/game-api/src/community-markdown';
it('does not repeatedly rescan unclosed link labels at the approved post limit',()=>{
 const samples:Array<{points:number;medianMs:number}>=[];
 for(const points of [1000,10000]){const raw='['.repeat(points);for(let i=0;i<3;i++)renderCommunityMarkdown('POST',raw);const timings:number[]=[];for(let i=0;i<7;i++){const start=performance.now();const result=renderCommunityMarkdown('POST',raw);timings.push(performance.now()-start);expect(result.sanitizedBody).toBe('<p>'+raw+'</p>');}timings.sort((a,b)=>a-b);samples.push({points,medianMs:timings[3]!});}
 const phase=process.env.COMMUNITY_MARKDOWN_COST_PHASE==='after'?'after':'before';
 writeFileSync('D:/Project/LASTRAIL/QA/CommanderS1-20260922/community-markdown-cost-'+phase+'-20261008.json',JSON.stringify({sourceStatus:'LOCAL_DELIMITER_SCALING_NOT_WORKERS_CPU_OR_CONCURRENT_BENCHMARK',samples},null,2));
 // Loose relative regression envelope with absolute noise allowance; not a production latency SLA.
 expect(samples[1]!.medianMs).toBeLessThanOrEqual(samples[0]!.medianMs*30+2);
},15000);
