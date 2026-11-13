import {z} from 'zod';
import {ActivityError} from './activity-policy';
import {createRunProgressSnapshot,progressSourceSchema} from './run-progress-snapshot';
export const storyProgressObservationVersion='story.progress-observation.v1.20261022';
const stringArrayJson=z.string().refine(value=>{
 try{const parsed:unknown=JSON.parse(value);return Array.isArray(parsed)&&parsed.every(x=>typeof x==='string');}catch{return false;}
});
const storySchema=z.object({actId:z.string(),highestClearedOperationId:z.string(),highestStoryProgressId:z.string(),
 clearedOperationsJson:stringArrayJson,storyFlagsJson:stringArrayJson,narrativeVersion:z.string().nullable(),lastProgressRunId:z.string().nullable(),updatedAt:z.number().int().nonnegative().safe()});
export async function readStoryProgressObservation(db:D1Database,accountId:string){
 // All source rows and the actual progression ledger sequence share one read snapshot.
 const row=await db.prepare(`SELECT json_object(
 'progressVersion',json((SELECT json_object('sequence',m.sequence,'targetId',l.target_id,'deltaJson',l.delta_json) FROM progression_ledger l JOIN account_mutations m ON m.id=l.mutation_id AND m.account_id=l.account_id WHERE l.account_id=a.id AND l.progression_type='STORY_PROGRESS' AND m.mutation_type='STORY_PROGRESS' ORDER BY m.sequence DESC LIMIT 1)),
 'story',(SELECT json_object('actId',p.act_id,'highestClearedOperationId',p.highest_cleared_operation_id,'highestStoryProgressId',p.highest_story_progress_id,'clearedOperationsJson',p.cleared_operations_json,'storyFlagsJson',p.story_flags_json,'narrativeVersion',p.narrative_version,'lastProgressRunId',p.last_progress_run_id,'updatedAt',p.updated_at) FROM account_story_progress p WHERE p.account_id=a.id),
 'unlocks',json((SELECT json_group_array(json_object('unlockId',unlock_id,'unlockedAt',unlocked_at)) FROM (SELECT * FROM account_unlocks WHERE account_id=a.id ORDER BY unlock_id)))) AS payload
 FROM accounts a WHERE a.id=? AND a.status='ACTIVE'`).bind(accountId).first<{payload:string}>();
 try{
  if(!row)throw Error('missing source');
  const source=progressSourceSchema.parse(JSON.parse(row.payload)),owner=await createRunProgressSnapshot(source);
  const story=source.story===null?null:storySchema.parse(source.story);
  const unlocks=z.array(z.object({unlockId:z.string(),unlockedAt:z.number()})).parse(source.unlocks);
  return {owner,row:story===null?null:{act_id:story.actId,highest_cleared_operation_id:story.highestClearedOperationId,
   highest_story_progress_id:story.highestStoryProgressId,cleared_operations_json:story.clearedOperationsJson,
   story_flags_json:story.storyFlagsJson,narrative_version:story.narrativeVersion,last_progress_run_id:story.lastProgressRunId,updated_at:story.updatedAt},
   unlockedOperationIds:unlocks.map(x=>x.unlockId).filter(x=>x.startsWith('operation:')).map(x=>x.slice('operation:'.length))};
 }catch{throw new ActivityError(503,'story_progress_source_unavailable');}
}
