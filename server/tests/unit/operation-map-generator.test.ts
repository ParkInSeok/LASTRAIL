import {expect,it} from "vitest";
import {generateOperationMap,mapGenerationVersion} from "../../apps/game-api/src/operation-map-generator";
import {OperationMapPublisher,capturePublishedOperationMap} from "../../apps/game-api/src/operation-map-publisher";
import {captureOperationMap} from "../../apps/game-api/src/operation-map-plan";
const kinds=["COMBAT","ELITE","AUGMENT","EQUIPMENT","ENCHANT","MAINTENANCE","MERCHANT","BOSS"];
function request(seed=0){return {version:mapGenerationVersion,seed:seed.toString(16).padStart(64,"0"),graphId:"g",graphRevision:1,definitionVersion:"fixture-def",candidates:kinds.map(eventKind=>({candidateId:eventKind,eventKind,publicInfo:{name:eventKind,iconId:eventKind},encounterProfileRef:`e-${eventKind}`,rewardPlanRef:`r-${eventKind}`}))};}
function catalog(){return {version:"operation.map-catalog.v1",catalogId:"fixture-only",revision:1,definitionVersion:"fixture-def",encounters:kinds.map(eventKind=>({ref:`e-${eventKind}`,version:"1",profile:"ORDINARY_12_NODE",eventKind,definition:{fixtureOnly:true}})),rewards:kinds.map(eventKind=>({ref:`r-${eventKind}`,version:"1",profile:"ORDINARY_12_NODE",eventKind,definition:{fixtureOnly:true}}))};}
it("validates every graph and four twelve-visit strategies across 1000 fixed seeds",async()=>{
 const observed=new Set<string>();let zeroAugment=false;
 for(let seed=0;seed<1000;seed++){
  const result=await generateOperationMap(request(seed)),p=captureOperationMap(result.plan);
  expect(result.generation.attemptsPerDepth).toHaveLength(11);
  expect(result.generation.attemptsPerDepth.every(n=>n>=1&&n<=256)).toBe(true);
  for(const n of p.nodes)observed.add(n.eventKind);
  for(let strategy=0;strategy<4;strategy++){
   let current=p.entryNodeId,augments=0;
   for(let depth=1;depth<=12;depth++){
    const choices=p.edges.filter(e=>e.fromNodeId===current).map(e=>p.nodes.find(n=>n.nodeId===e.toNodeId)!);
    const chosen=strategy===3?choices[(seed+depth)%choices.length]:choices.find(n=>strategy===0?n.eventKind==="COMBAT":strategy===1?!["COMBAT","ELITE","BOSS"].includes(n.eventKind):n.eventKind==="ELITE")??choices[0];
    expect(chosen.visitDepth).toBe(depth);current=chosen.nodeId;if(chosen.eventKind==="AUGMENT")augments++;
   }
   expect(p.nodes.find(n=>n.nodeId===current)?.eventKind).toBe("BOSS");if(augments===0)zeroAugment=true;
  }
 }
 expect([...observed].sort()).toEqual([...kinds].sort());expect(zeroAugment).toBe(true);
},30000);
it("replays deterministically independent of candidate input order and does not mutate input",async()=>{
 const r=request(19),before=structuredClone(r),a=await generateOperationMap(r);r.candidates.reverse();
 expect(await generateOperationMap(r)).toEqual(a);expect(before.candidates).toEqual([...r.candidates].reverse());
 expect((await generateOperationMap(request(20))).plan).not.toEqual(a.plan);
});
it("excludes scout from new seven-event publication while replaying old generated maps",async()=>{
 const latest=request(71),scout={...latest.candidates[0],candidateId:"SCOUT",eventKind:"SCOUT",encounterProfileRef:"e-SCOUT",rewardPlanRef:"r-SCOUT"};
 latest.candidates.push(scout);
 await expect(generateOperationMap(latest)).rejects.toThrow("operation_map_generation_invalid");
 const historical={...latest,version:"operation.map-generation.v1"};
 const old=await generateOperationMap(historical);
 expect(old.generation.request.version).toBe("operation.map-generation.v1");
 expect(old.plan.eventAssignmentVersion).toBe("operation.map-generation.v1");
 expect((await generateOperationMap(historical)).plan).toEqual(old.plan);
 await expect(new OperationMapPublisher({read:async()=>catalog()},{supportsEncounter:async()=>true,supportsReward:async()=>true}).generate(historical)).rejects.toThrow("operation_map_generation_version_retired");
 const current=await generateOperationMap(request(71));expect(current.plan.enabledEventKinds).not.toContain("SCOUT");
});
it("fails unsupported pools rather than substituting or guaranteeing augments",async()=>{
 for(const remove of ["COMBAT","BOSS","NONCOMBAT"]){const r=request();r.candidates=r.candidates.filter(c=>remove==="NONCOMBAT"?["COMBAT","ELITE","BOSS"].includes(c.eventKind):c.eventKind!==remove);await expect(generateOperationMap(r)).rejects.toThrow("operation_map_generation_pool_insufficient");}
 const r=request();r.candidates=r.candidates.filter(c=>["COMBAT","EQUIPMENT","BOSS"].includes(c.eventKind));const result=await generateOperationMap(r);expect(result.plan.nodes.some(n=>n.eventKind==="AUGMENT")).toBe(false);
 r.candidates.push({...r.candidates[0]});await expect(generateOperationMap(r)).rejects.toThrow("operation_map_generation_invalid");
});
it("publishes from one trusted catalog, pins generation and restores without current catalog",async()=>{
 let reads=0;const c=catalog(),publisher=new OperationMapPublisher({read:async()=>{reads++;return c;}},{supportsEncounter:async()=>true,supportsReward:async()=>true});
 const p=await publisher.generate(request(123));expect(reads).toBe(1);expect(p.version).toBe("operation.map-content.v2");
 expect(await capturePublishedOperationMap(p,p.plan)).toEqual(p);
 c.encounters[0].version="changed";expect(await capturePublishedOperationMap(p,p.plan)).toEqual(p);
 const forged=structuredClone(p);forged.generation.request.seed="f".repeat(64);await expect(capturePublishedOperationMap(forged,p.plan)).rejects.toThrow();
});
it("checks every eligible candidate against real support before sampling, including unused ones",async()=>{
 const r=request();r.candidates.push({...r.candidates[0],candidateId:"unsupported",encounterProfileRef:"missing"});
 await expect(new OperationMapPublisher({read:async()=>catalog()}).generate(r)).rejects.toThrow("operation_map_content_unresolved");
 await expect(new OperationMapPublisher().generate(request())).rejects.toThrow("operation_map_catalog_unavailable");
});

it("requires actual executor and reward-validator capability checks for every candidate before drawing",async()=>{
 const c=catalog(),checkedEncounters:string[]=[],checkedRewards:string[]=[];
 const support={supportsEncounter:async(e:{ref:string})=>{checkedEncounters.push(e.ref);return true;},supportsReward:async(e:{ref:string})=>{checkedRewards.push(e.ref);return true;}};
 await expect(new OperationMapPublisher({read:async()=>c}).generate(request())).rejects.toThrow("operation_map_support_unavailable");
 const result=await new OperationMapPublisher({read:async()=>c},support).generate(request(56));
 expect(result.version).toBe("operation.map-content.v2");
 expect(checkedEncounters.sort()).toEqual(kinds.map(k=>`e-${k}`).sort());expect(checkedRewards.sort()).toEqual(kinds.map(k=>`r-${k}`).sort());
 expect(await capturePublishedOperationMap(result,result.plan)).toEqual(result);
});
it("rejects an unsupported kind even if random selection would not include it",async()=>{
 const c=catalog(),r=request(22);
 const publisher=new OperationMapPublisher({read:async()=>c},{supportsEncounter:async(e:{ref:string})=>e.ref!=="e-MAINTENANCE",supportsReward:async()=>true});
 await expect(publisher.generate(r)).rejects.toThrow("operation_map_content_unsupported");
 const rewardPublisher=new OperationMapPublisher({read:async()=>c},{supportsEncounter:async()=>true,supportsReward:async(e:{ref:string})=>e.ref!=="r-MERCHANT"});
 await expect(rewardPublisher.generate(r)).rejects.toThrow("operation_map_content_unsupported");
});
