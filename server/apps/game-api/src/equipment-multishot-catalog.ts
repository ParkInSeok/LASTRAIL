import {canonicalJson,sha256Hex} from '@lastrail/common';
import {multishotCatalogVersion,equipmentMultishotEnchantmentSchema} from './equipment-effect-snapshot';
import {multishotDefinitionVersion} from './equipment-multishot-definition';
/** Approved code catalog, not a published operating candidate pool. All stars are
 * defined; actual equipped-trial support remains 1-2 with supported companions. */
export async function multishotCatalogBinding(){
 const body={version:'equipment.multishot-formal-binding.v1.20261015',sourceStatus:'APPROVED_CODE_CATALOG_NOT_PUBLISHED',
  effectId:'EN17',definitionVersion:multishotCatalogVersion,catalogVersion:multishotCatalogVersion,logicalDefinitionVersion:multishotDefinitionVersion,
  rows:[1500,2500,4000,6000,8500].map((coefficientBps,i)=>equipmentMultishotEnchantmentSchema.parse({effectId:'EN17',appliedStar:i+1,definitionVersion:multishotCatalogVersion,definitionSnapshot:{version:multishotDefinitionVersion,appliedStar:i+1,coefficientBps,maxSecondaryTargets:2,radiusL:{numerator:1,denominator:2},attackEvaluation:'AT_FIRE',targetDefenseEvaluation:'AT_IMPACT'}}))};
 return {...body,bindingHash:await sha256Hex(canonicalJson(body))};
}
