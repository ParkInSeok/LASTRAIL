import { z } from 'zod';
import { ActivityError } from './activity-policy';
import { bProfileRulesVersion, validateBPlacement } from './combat-b-profile-rules';
export const bPlacementVersion = 'combat.b-placement.v2.20261002';
export const bPlacementRequestSchema = z.strictObject({
 version: z.string().min(1).max(128), mountedTurrets: z.array(z.unknown()).max(128),
 placements: z.array(z.strictObject({ slot: z.number().int().min(0).max(4),
  carriageId: z.string().min(1).max(128), occupant: z.discriminatedUnion('kind', [
   z.strictObject({kind:z.literal('OPERATOR'),operatorId:z.string().min(1).max(128)}),
   z.strictObject({kind:z.literal('CARGO')}),
  ]) })).min(1).max(5),
});
export function parseBPlacementRequest(input: unknown) {
 const parsed=bPlacementRequestSchema.safeParse(input);
 if(!parsed.success)throw new ActivityError(400,'combat_b_request_invalid');
 if(parsed.data.version!==bPlacementVersion)throw new ActivityError(409,'combat_b_version_conflict');
 if(parsed.data.mountedTurrets.length)throw new ActivityError(409,'combat_b_mounted_turret_forbidden');
 return {...parsed.data,placements:parsed.data.placements.sort((a,b)=>a.slot-b.slot)};
}
export function validateBPlacementV2(input: unknown,source: unknown) {
 const p=parseBPlacementRequest(input);
 const original=validateBPlacement({...p,version:bProfileRulesVersion},source);
 return {...original,version:bPlacementVersion,totalPlacementCount:p.placements.length};
}