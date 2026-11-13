import {it,expect} from 'vitest';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {captureAugmentUtilityDefinitions,readAugmentUtilityDefinitions} from '../../apps/game-api/src/augment-utility-definition';
it('uses uncapped attributed HP damage including backflow while preventing heal conversion recursion',async()=>{
 const p=await captureAugmentUtilityDefinitions();const drain=p.definitions.find(x=>x.id==='O22')!;
 expect(drain.rules).toMatchObject({damageToHealBps:1200,eligible:'ALL_CONFIRMED_ENEMY_HP_DAMAGE_ATTRIBUTED_TO_THIS_OPERATOR',
  addedRecoveryCap:null,internalCooldownMs:0,recipient:'SOLE_ATTRIBUTED_OPERATOR',overflow:'DISCARD_NO_BANK'});
 for(const key of ['rollingWindowMs','maximumHpBudgetBps','windowReset','budgetSpentOn'])expect(drain.rules).not.toHaveProperty(key);
 expect(drain.rules.excluded).not.toEqual(expect.arrayContaining(['BACKFLOW_DAMAGE']));
 expect(drain.rules.blockedTriggers).toEqual(expect.arrayContaining(['O02','O23']));
 const backflow=p.definitions.find(x=>x.id==='O23')!;
 expect(backflow.rules).toMatchObject({projectileProfile:{collision:{lifesteal:'ATTRIBUTED_OPERATOR_O22_AFTER_CONFIRMED_HP_LOSS'}}});
 expect(backflow.rules.blockedTriggers).not.toContain('O22');
 expect(p.lifecycle.checkpoint).not.toContain('ROLLING_LAST_SECOND_ACTUAL_HEAL_HISTORY');
});
it('binds independent life ballistics and keeps committed healing separate from projectile success',async()=>{
 const rules=(await captureAugmentUtilityDefinitions()).definitions.find(x=>x.id==='O23')!.rules;
 expect(rules).toMatchObject({projectileProfile:{id:'augment.o23.life-projectile.v2.20261019',speedMmPerSecond:36000,radiusMm:80,
  maximumRangeMm:24000,maximumTravelMm:24000,flight:'WORLD_SPACE_STRAIGHT_FIXED_AT_FIRE',homing:false,
  lifetime:{numeratorSeconds:24000,denominatorSeconds:36000,rounding:'CEIL_TO_ORIGINAL_COMBAT_STEP',clipEachStepAtMaximumTravel:true},
  collision:{query:'SWEPT_SPHERE_PREVIOUS_TO_CLIPPED_NEXT',damageTargets:'RESERVED_ENEMY_ONLY',maximumDamageCommits:1},
  healing:{commitAt:'ORIGINAL_HEAL_EFFECT_TIME',waitForProjectileHit:false,rollbackOnProjectileFailure:false,reapplyHpOnRecovery:false}}});
});
it('uses a healer formation condition without requiring the healers basic muzzle',async()=>{
 const p=await captureAugmentUtilityDefinitions();const reflux=p.definitions.find(x=>x.id==='O23')!;
 expect(reflux.rules).toMatchObject({formationEligibility:'AT_LEAST_ONE_AUTHORITY_MAPPED_HEALER',requiresBasicAttackMuzzle:false,
  projectile:'AUGMENT_OWN_LIFE_PROJECTILE_PROFILE',missingBallistics:'TECHNICAL_IMPLEMENTATION_PENDING_NOT_PERMANENT_FORMATION_EXCLUSION',
  targetResolverWithoutBasicAttack:'LIFE_PROJECTILE_PROFILE_BINDING_PENDING_NOT_FORMATION_EXCLUSION',targetLoss:'PINNED_LIFE_PROJECTILE_PROFILE_ORIGINAL_ENTITY_LIFETIME'});
 expect(reflux.requiredCapabilities).not.toContain('CASTER_ORIGINAL_MUZZLE_BALLISTICS');
});
it('pins original healing parents and keeps passive mapping and AUTO support explicit',async()=>{
 const p=await captureAugmentUtilityDefinitions();const reflux=p.definitions.find(x=>x.id==='O23')!;
 expect(reflux.rules).toMatchObject({healingOperation:{approval:'31#overflow-healing-operation-approved-20261019',
  eligibleKinds:['ORIGINAL_ACTIVE_DIRECT_HP_HEAL','ORIGINAL_ULTIMATE_DIRECT_HP_HEAL','ORIGINAL_SKILL_HEAL_OVER_TIME_TICK','FORMALLY_MAPPED_INNATE_PASSIVE_HP_HEAL'],
  parent:{periodicPassive:'ORIGINAL_DEFINED_HEAL_CYCLE',newParentEachFrame:false,sharedCapAcrossRecipientsAndTicks:true},
  automatic:{addFullHpCastCondition:false,unconditionalExistingAutoConflict:'REQUIRES_ORIGINAL_PROFILE_CONTRACT_RESOLUTION'},
  launch:{anchors:['VERIFIED_HEAL_PRESENTATION_ANCHOR','VERIFIED_COMMON_UPPER_BODY_ANCHOR'],pixelEstimatedMuzzle:false},
  independentPassiveMappings:[],allHealerProfilesMapped:false}});
 expect(reflux.rules.excluded).toEqual(expect.arrayContaining(['OTHER_DERIVED_HEAL','HEAL_ITEM','EVENT_RECOVERY','O06_REDISTRIBUTION']));
});
it('limits enemy-target gating to the full-HP manual exception and leaves passive ATK timing unbound',async()=>{
 const rules=(await captureAugmentUtilityDefinitions()).definitions.find(x=>x.id==='O23')!.rules;
 expect(rules).toMatchObject({parentAttackSnapshotAt:{cast:'CAST_ACCEPTANCE',periodicPassive:'FORMAL_CYCLE_SOURCE_MAPPING_REQUIRED'},
  healingOperation:{healingTarget:{ordinaryRequirements:'ORIGINAL_HEAL_CONTRACT_WITHOUT_NEW_ENEMY_REQUIREMENT',
   fullHpManualRequirements:expect.arrayContaining(['LEGAL_ENEMY_TARGET'])}}});
 expect((rules.healingOperation as {healingTarget:Record<string,unknown>}).healingTarget).not.toHaveProperty('require');
});
it('publishes the six approved definitions without advertising a 43-card runtime pool',async()=>{
 const p=await captureAugmentUtilityDefinitions();
 expect(p.definitions.map(x=>x.id)).toEqual(['T05','T16','O06','O08','O22','O23']);
 expect(p).toMatchObject({plannedPoolCount:43,operatingCatalogPublished:false,runtimeSupported:false,eligibleIds:[]});
 expect(p.definitions.find(x=>x.id==='O22')?.rules).toMatchObject({damageToHealBps:1200,addedRecoveryCap:null,internalCooldownMs:0});
 expect(p.definitions.find(x=>x.id==='O23')?.rules).toMatchObject({excessToDamageBps:6000,parentCasterAttackCapBps:15000,parentAttackSnapshotAt:{cast:'CAST_ACCEPTANCE'}});
 expect(p.composition).toMatchObject({seraRedistributionFirstBps:5000,o02AndBackflowAllocationBps:[5000,5000],o02ConversionBps:5000,backflowConversionBps:6000,capOverflowReallocation:false});
});
it('keeps missing capability and Eve hold conditions explicit',async()=>{
 const p=await captureAugmentUtilityDefinitions();
 expect(p.definitions.find(x=>x.id==='T05')?.requiredCapabilities).toContain('CANCELLABLE_APPROACH_CLIMB_OWNER');
 expect(p.definitions.find(x=>x.id==='T16')?.requiredCapabilities).toContain('REMOVABLE_ENEMY_TRAIN_SLOW_OWNER');
 expect(p.definitions.find(x=>x.id==='O06')?.rules).toMatchObject({eve:{status:'HELD_PENDING_OBSERVABLE_DIFFERENCE',preserveOriginalSchedule:true}});
 expect(p.lifecycle).toMatchObject({clock:'COMBAT_TIME_FROZEN_DURING_PAUSE_OR_SELECTION_HOLD',restorePolicy:'PINNED_ORIGINAL_NOT_LATEST_REGISTRY',effectOwnershipEstablished:false});
});
it('preserves the approved single living or other-target recipient restrictions',async()=>{
 const p=await captureAugmentUtilityDefinitions();
 expect(p.definitions.find(x=>x.id==='O06')?.rules).toMatchObject({
  reina:{recipient:'NEAREST_OTHER_ELIGIBLE_ENEMY',maxRecipients:1},
  kyle:{recipient:'LIVING_ALLY',maxRecipients:1},
 });
 expect(p.definitions.find(x=>x.id==='T16')?.rules).toMatchObject({attack:{recipient:'FIRST_ELIGIBLE_ORIGINAL_SUPPORT_PRIMARY_TARGET'}});
});
it('rejects rehashed policy edits and detaches every returned definition',async()=>{
 const original=await captureAugmentUtilityDefinitions();const edited=structuredClone(original);edited.plannedPoolCount=99;
 const {definitionHash,...body}=edited;void definitionHash;edited.definitionHash=await sha256Hex(canonicalJson(body));
 await expect(readAugmentUtilityDefinitions(edited)).rejects.toThrow('augment_utility_definition_mismatch');
 expect(await readAugmentUtilityDefinitions(original)).toEqual(original);
 expect((await captureAugmentUtilityDefinitions()).plannedPoolCount).toBe(43);
});
it('carries newly approved parent pinning, logical recovery identity and no-retroactive acquisition boundaries',async()=>{
 const policy=(await captureAugmentUtilityDefinitions()).lifecycle;
 expect(policy).toMatchObject({approval:'31#augment-lifecycle-approved-20261019',
  binding:{directAction:'ORIGINAL_ATTACK_COMMIT',multiTickParent:'ORIGINAL_CAST_ACCEPTANCE',backflow:'EXCESS_ALLOCATION_AND_FIRE_RESERVATION',dynamicReferences:'PRESERVE_ONLY_EXPLICIT_ORIGINAL_RULES'},
  acquisition:{T05:'NEXT_LEGAL_HIGH_SPEED_ENTRY',T16:'NEXT_ELIGIBLE_ORIGINAL_ULTIMATE',O06:'NEW_FIVE_COMBAT_SECONDS_AT_CHARGE_AT_LEAST_100',O08:'NEXT_ORIGINAL_ULTIMATE_FIRST_EFFECT',retroactive:false},
  recovery:{lifestealEventOwner:'ORIGINAL_AUTHORITY_DAMAGE_AND_HEAL_JOURNAL',repeatLifestealOnDownOrPoolReplacement:false,unknownCheckpoint:'RECOVER_ORIGINAL_NOT_EMPTY_STATE',parentBudgetRechargeOnRevive:false},
  node:{ownedDefinitions:'RUN',temporaryEffects:'NODE',hpShieldAndCharge:'EXISTING_ORIGINAL_POLICIES',viewRecreationIsNodeEnd:false},
  ordering:{sameTick:'AUTHORITY_COMMAND_SEQUENCE',oldAction:'COMMITTED_BEFORE_APPLICATION_COMPLETE',newAction:'COMMITTED_AFTER_APPLICATION_COMPLETE',retryIdentity:'ORIGINAL_APPLICATION_ID_AND_REQUEST_KEY'},
 });
});
