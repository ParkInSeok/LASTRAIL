import {z} from 'zod';
import {exactCombatValueSchema} from './combat-exact-value';

const id=z.string().regex(/^[\x21-\x7e]{1,128}$/u);
const revision=z.number().int().positive().safe();
const slot=z.strictObject({instanceId:id,definitionId:id,definitionVersion:id,categoryId:id,setTag:id,star:z.number().int().min(1).max(5)});
const stats=z.strictObject({maxHp:revision,exactMaxHp:exactCombatValueSchema,attack:exactCombatValueSchema,defense:exactCombatValueSchema,
 additiveBps:z.strictObject({HP:z.number().int().nonnegative(),ATK:z.number().int().nonnegative(),DEF:z.number().int().nonnegative()}),
 rounding:z.literal('HP_INTEGER_FLOOR_AT_HP_BOUNDARY_ATK_DEF_EXACT_UNTIL_ORIGINAL_DAMAGE_OWNER')});
/** Supported static projection only. This never acknowledges a client's runtime application. */
export const equipmentMaintenanceConfigurationSchema=z.strictObject({sourceVersion:id,sourceHash:z.string().regex(/^[a-f0-9]{64}$/u),
 observedInventoryRevision:revision,actors:z.array(z.strictObject({wearerKind:z.enum(['operator','turret']),wearerId:id,targetId:id,
 slots:z.array(slot.nullable()).length(3),stats})).min(1).max(2)});
export const equipmentMaintenanceApplicationSchema=z.strictObject({applicationId:id,
 status:z.literal('SERVER_COMMITTED_CLIENT_APPLY_REQUIRED'),inventoryRevision:revision,healthRevision:revision,
 configuration:equipmentMaintenanceConfigurationSchema});
export type EquipmentMaintenanceConfiguration=z.infer<typeof equipmentMaintenanceConfigurationSchema>;
