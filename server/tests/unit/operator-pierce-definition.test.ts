import {describe,it,expect} from 'vitest';
import {capturePierceDefinition,readPierceDefinition} from '../../apps/game-api/src/operator-pierce-definition';

describe('approved logical piercing boundary without formal augment ID',()=>{
 it('validates detached approved procedure without claiming ownership or trial exemption',async()=>{
  const original=await capturePierceDefinition();
  const parsed=await readPierceDefinition(JSON.parse(JSON.stringify(original)));
  expect(parsed).toEqual(original);
  expect(parsed.formalAugmentId).toBeNull();
  expect(parsed.ownedSource).toBeNull();
  expect(parsed.trialPolicy).toBeNull();
  expect(parsed.runtimeSupported).toBe(false);
  expect(parsed.canStart).toBe(false);
  expect(parsed.procedure.reservedCoefficients).toEqual([{numerator:'3',denominator:'5'},{numerator:'3',denominator:'10'}]);
  parsed.procedure.reservedCoefficients[0]!.numerator='9';
  expect((await capturePierceDefinition()).procedure.reservedCoefficients[0]!.numerator).toBe('3');
 });
 it.each(['coefficient','path','version','id','extra','trial'])('rejects altered/rehashed %s definitions',async kind=>{
  const raw=await capturePierceDefinition() as unknown as Record<string,unknown>;
  const procedure=raw.procedure as Record<string,unknown>;
  if(kind==='coefficient')(procedure.reservedCoefficients as unknown[]).reverse();
  if(kind==='path')procedure.pathsPerOriginalOpportunity=2;
  if(kind==='version')raw.version='operator-pierce.v99';
  if(kind==='id')raw.formalAugmentId='O21';
  if(kind==='extra')raw.unapproved=true;
  if(kind==='trial')raw.trialPolicy='NOT_APPLICABLE';
  const {definitionHash:_,...body}=raw;void _;
  const {canonicalJson,sha256Hex}=await import('@lastrail/common');
  raw.definitionHash=await sha256Hex(canonicalJson(body));
  await expect(readPierceDefinition(raw)).rejects.toThrow('operator_pierce_definition_mismatch');
 });
});

