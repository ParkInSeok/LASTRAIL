import {expect,it} from 'vitest';
import {parseMultishotDefinition,selectHighestMultishot} from '../../apps/game-api/src/equipment-multishot-definition';
const definition=(appliedStar=5,coefficientBps=8500)=>({version:'multishot-enchant-v1.20261015',appliedStar,coefficientBps,maxSecondaryTargets:2,radiusL:{numerator:1,denominator:2},attackEvaluation:'AT_FIRE',targetDefenseEvaluation:'AT_IMPACT'});
it.each([[1,1500],[2,2500],[3,4000],[4,6000],[5,8500]])('accepts only the approved fixed row for star %s', (star,bps)=>{expect(parseMultishotDefinition(definition(star,bps))).toEqual(definition(star,bps));});
it.each([0,6,-1,1.5,NaN,Infinity])('rejects invalid procedure star %s',star=>{expect(()=>parseMultishotDefinition(definition(star))).toThrow();});
it.each([0,-1,8500.5,NaN,Infinity,1500])('rejects invalid or mismatched coefficient %s',bps=>{expect(()=>parseMultishotDefinition(definition(5,bps))).toThrow();});
it('rejects unknown versions, extra fields and changes to fixed scope or timing',()=>{for(const patch of [{version:'future'},{effectId:'EN17'},{maxSecondaryTargets:3},{radiusL:{numerator:1,denominator:3}},{attackEvaluation:'AT_IMPACT'},{targetDefenseEvaluation:'AT_FIRE'}])expect(()=>parseMultishotDefinition({...definition(),...patch})).toThrow();});
it('selects one strongest procedure rather than summing three and detaches the result',()=>{const input=[{instanceId:'c',definitionSnapshot:definition(2,2500)},{instanceId:'b',definitionSnapshot:definition()},{instanceId:'a',definitionSnapshot:definition(4,6000)}],before=structuredClone(input);const result=selectHighestMultishot(input);expect(result).toEqual(input[1]);expect(input).toEqual(before);result!.definitionSnapshot.radiusL.numerator=1;expect(result!.definitionSnapshot).not.toBe(input[1]!.definitionSnapshot);expect(result!.definitionSnapshot.radiusL).not.toBe(input[1]!.definitionSnapshot.radiusL);});
it('breaks ties by stable ASCII instance identity independent of input order',()=>{const a={instanceId:'A',definitionSnapshot:definition()},b={instanceId:'a',definitionSnapshot:definition()};expect(selectHighestMultishot([b,a])?.instanceId).toBe('A');expect(selectHighestMultishot([a,b])?.instanceId).toBe('A');});
it('returns null for no eligible snapshots and rejects malformed or over-capacity input',()=>{expect(selectHighestMultishot([])).toBeNull();const item={instanceId:'a',definitionSnapshot:definition()};for(const bad of [[item,item],Array.from({length:4},(_,i)=>({...item,instanceId:String(i)})),[{...item,instanceId:''}],[{...item,definitionSnapshot:definition(4,8500)}],[{...item,wearerId:'invented'}]])expect(()=>selectHighestMultishot(bad)).toThrow();});
it('never silently skips a malformed weaker candidate',()=>{expect(()=>selectHighestMultishot([{instanceId:'good',definitionSnapshot:definition()},{instanceId:'bad',definitionSnapshot:definition(1,-1)}])).toThrow();});
it('keeps two secondary targets with three five-star items and accepts frozen caller input',()=>{
  const input=['c','b','a'].map(instanceId=>Object.freeze({instanceId,definitionSnapshot:Object.freeze({...definition(),radiusL:Object.freeze({numerator:1,denominator:2})})}));
  const selected=selectHighestMultishot(Object.freeze(input));
  expect(selected?.instanceId).toBe('a');
  expect(selected?.definitionSnapshot).toMatchObject({appliedStar:5,coefficientBps:8500,maxSecondaryTargets:2});
  expect(input.map(item=>item.instanceId)).toEqual(['c','b','a']);
});
it('rejects missing, null, wrong-type, nested unknown data and invalid instance identities',()=>{
  for(const value of [null,undefined,{},'definition',{...definition(),appliedStar:'5'},{...definition(),radiusL:{numerator:1,denominator:2,meters:48}}])expect(()=>parseMultishotDefinition(value)).toThrow();
  for(const value of [null,{},[{instanceId:'가',definitionSnapshot:definition()}],[{instanceId:'a'.repeat(129),definitionSnapshot:definition()}]])expect(()=>selectHighestMultishot(value)).toThrow();
});