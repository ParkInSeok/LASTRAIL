import {canonicalJson,sha256Hex} from '@lastrail/common';
import {z} from 'zod';
import {ActivityError} from './activity-policy';
const id=z.string().min(1).max(200),nat=z.number().int().nonnegative().safe();
export const progressSourceSchema=z.object({
 story:z.json().nullable(),unlocks:z.array(z.json()),
 progressVersion:z.strictObject({sequence:nat,targetId:id,deltaJson:z.string()}).nullable(),
});
/** Shared claim/observation rule. Financial claim sequence is only an optional upper fence. */
export async function createRunProgressSnapshot(source:z.infer<typeof progressSourceSchema>,claimSequence?:number){
 const fail=():never=>{throw new ActivityError(409,'run_claim_complete_progress_version_unavailable');};
 if(source.progressVersion===null&&(source.story!==null||source.unlocks.length!==0))fail();
 if(source.progressVersion!==null){
  const story=z.object({actId:id,highestStoryProgressId:id,highestClearedOperationId:id,lastProgressRunId:id}).safeParse(source.story);
  const raw:unknown=JSON.parse(source.progressVersion.deltaJson);
  const delta=z.object({runId:id,actId:id,highestStoryProgressId:id,highestClearedOperationId:id}).safeParse(raw);
  if(!story.success||!delta.success||source.progressVersion.sequence<1||(claimSequence!==undefined&&source.progressVersion.sequence>=claimSequence)
   ||source.progressVersion.targetId!==story.data.highestStoryProgressId||delta.data.runId!==story.data.lastProgressRunId||delta.data.actId!==story.data.actId||delta.data.highestStoryProgressId!==story.data.highestStoryProgressId||delta.data.highestClearedOperationId!==story.data.highestClearedOperationId)fail();
 }
 const sequence=source.progressVersion?.sequence??0,state={story:source.story,unlocks:source.unlocks};
 return {kind:'SNAPSHOT' as const,beforeMutationSequence:sequence,mutationSequence:sequence,
  versionSource:source.progressVersion?'STORY_PROGRESS_MUTATION' as const:'VERIFIED_EMPTY' as const,
  contentHash:await sha256Hex(canonicalJson(state)),state};
}
