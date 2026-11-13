import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import type {AugmentEligibleCatalogSource} from './augment-node-issuer';
import {readBDepartureCombatPin} from './combat-b-departure-combat-pin';
import {augmentUtilityDefinitionVersion,captureAugmentUtilityDefinitions} from './augment-utility-definition';

export const runtimeAugmentCatalogVersion='combat.runtime-augment-catalog.v1.20261019';
const id=z.string().min(1).max(200),hash=z.string().regex(/^[a-f0-9]{64}$/u);
const unique=z.array(id).max(1000).refine(rows=>new Set(rows).size===rows.length);
const schema=z.strictObject({version:z.literal(runtimeAugmentCatalogVersion),catalogVersion:z.literal(augmentUtilityDefinitionVersion),complete:z.literal(true),environment:id,
 candidates:z.array(z.strictObject({id,definitionHash:hash,requiredCapabilities:unique,requiresHealer:z.boolean()})).max(1000).refine(rows=>new Set(rows.map(r=>r.id)).size===rows.length),
 capabilities:unique,formation:z.strictObject({revision:z.number().int().positive().safe(),sourceHash:hash,
  operators:z.array(z.strictObject({operatorId:id,abilityDefinitionHash:hash,healer:z.boolean()})).max(128).refine(rows=>new Set(rows.map(r=>r.operatorId)).size===rows.length)})});
const json=(value:unknown)=>canonicalJson(z.json().parse(value));
const fail=(reason:string):never=>{throw new ActivityError(409,'augment_runtime_catalog_'+reason);};

/** Complete supported catalog from the executable profile owner. Candidate rows
 * describe the full published subset, not a preferred/O22-only list. Other card
 * definitions remain that owner's exact hashed definitions; the six utility
 * definitions are additionally cross-checked against their canonical source.
 * No current HP, cooldown, enemy presence or basic-attack muzzle input exists. */
export async function captureRuntimeAugmentCatalog(input:unknown){
 const contract=schema.parse(structuredClone(input)),definitions=await captureAugmentUtilityDefinitions();
 const caps=new Set(contract.capabilities),healer=contract.formation.operators.some(o=>o.healer);
 for(const candidate of contract.candidates){
  const known=definitions.definitions.find(d=>d.id===candidate.id);
  if(known&&(known.definitionHash!==candidate.definitionHash||json([...known.requiredCapabilities].sort())!==json([...candidate.requiredCapabilities].sort())||candidate.requiresHealer!==(candidate.id==='O23')))fail('definition_conflict');
 }
 return {contract,catalogVersion:contract.catalogVersion,eligibleIds:contract.candidates.filter(c=>c.requiredCapabilities.every(cap=>caps.has(cap))&&(!c.requiresHealer||healer)).map(c=>c.id)};
}

/** Original immutable departure formation only. A future mutable formation mode
 * needs its authoritative revision/source adapter before it can use this provider.
 * Revocation (enabled=0) is admission-only: it prevents new START but does not
 * revoke or upgrade this run's immutable original catalog. Account/run permission
 * is still fenced. Runtime emergency revocation needs its own authority contract.
 * No live registry reload upgrades an existing run's catalog or policy. */
export function createAugmentRuntimeCatalogSource(db:D1Database,environment:string):AugmentEligibleCatalogSource {
 id.parse(environment);
 return {prepare:async context=>{
  if(context.environment!==environment) return fail('environment_conflict');
  const pin=await readBDepartureCombatPin(context.startSnapshot,{accountId:context.accountId,runId:context.runId});
  if(!pin)return fail('pin_required');
  const row=await db.prepare(`SELECT source_json,source_hash,profile_hash FROM combat_runtime_sources WHERE environment=? AND account_id=? AND preview_id=?`)
   .bind(environment,context.accountId,pin.origin.operationPreviewId).first<{source_json:string;source_hash:string;profile_hash:string}>();
  if(!row)return fail('registry_required');
  const original=JSON.parse(row.source_json);
  if(await sha256Hex(row.source_json)!==row.source_hash||row.profile_hash!==pin.runtimeProfileHash)return fail('registry_conflict');
  if(json(original)!==json(pin.source))return fail('registry_conflict');
  const captured=await captureRuntimeAugmentCatalog(pin.source.profile.definition.runtimeAugmentCatalog);
  if(captured.contract.environment!==environment)return fail('environment_conflict');
  return {catalogVersion:captured.catalogVersion,eligibleIds:captured.eligibleIds,assertions:[db.prepare(`INSERT INTO solo_augment_write_guard(valid) SELECT CASE WHEN EXISTS(
   SELECT 1 FROM run_snapshots s JOIN runs r ON r.id=s.run_id JOIN accounts a ON a.id=r.account_id
   JOIN combat_runtime_sources c ON c.account_id=r.account_id AND c.preview_id=r.preview_id
   WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND a.status='ACTIVE' AND s.snapshot_json=?
   AND c.environment=? AND c.source_json=? AND c.source_hash=? AND c.profile_hash=?
  ) THEN 1 ELSE 0 END`).bind(context.runId,context.accountId,context.startSnapshot,environment,row.source_json,row.source_hash,row.profile_hash)]};
 }};
}
