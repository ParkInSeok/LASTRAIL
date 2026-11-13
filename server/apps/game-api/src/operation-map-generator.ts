import {z} from "zod";
import {canonicalJson,sha256Hex} from "@lastrail/common";
import {captureOperationMap,type OperationMapPlan} from "./operation-map-plan";
import {mapFail,mapId} from "./operation-map-state";
export const mapGenerationVersion="operation.map-generation.v2";
const historicalGenerationVersion="operation.map-generation.v1";
const currentWeights={COMBAT:40,ELITE:12,AUGMENT:20,EQUIPMENT:14,ENCHANT:14,MAINTENANCE:8,MERCHANT:8,SCOUT:0} as const;
const historicalWeights={...currentWeights,SCOUT:4} as const;
type Kind=keyof typeof historicalWeights;
const candidate=z.strictObject({candidateId:mapId,eventKind:z.enum(["COMBAT","ELITE","AUGMENT","EQUIPMENT","ENCHANT","MAINTENANCE","MERCHANT","SCOUT","BOSS"]),publicInfo:z.strictObject({name:z.string().min(1).max(200),iconId:mapId}),encounterProfileRef:mapId,rewardPlanRef:mapId});
export const mapGenerationRequestSchema=z.strictObject({version:z.enum([historicalGenerationVersion,mapGenerationVersion]),seed:z.string().regex(/^[a-f0-9]{64}$/u),graphId:mapId,graphRevision:z.number().int().positive().safe(),definitionVersion:mapId,candidates:z.array(candidate).min(1).max(1000)});
export function captureMapGenerationRequest(input:unknown){
 const parsed=mapGenerationRequestSchema.safeParse(input);if(!parsed.success)return mapFail("generation_invalid");
 const r=parsed.data;if(new Set(r.candidates.map(c=>c.candidateId)).size!==r.candidates.length||r.version===mapGenerationVersion&&r.candidates.some(c=>c.eventKind==="SCOUT"))return mapFail("generation_invalid");
 r.candidates.sort((a,b)=>a.candidateId<b.candidateId?-1:a.candidateId>b.candidateId?1:0);return r;
}
/** Stable SHA256 counter stream with rejection sampling (no modulo bias). Never Math.random.
 * Each bounded draw fails after 128 rejections; each layer after 256 attempts. No seed reroll.
 */
class SeedStream {
 private counter=0;private words:number[]=[];
 constructor(private readonly seed:string,private readonly version:string){}
 async below(bound:number){
  const limit=Math.floor(0x100000000/bound)*bound;
  for(let attempt=0;attempt<128;attempt++){
   if(this.words.length===0){const hash=await sha256Hex(canonicalJson([this.version,this.seed,this.counter++]));this.words=hash.match(/.{8}/gu)!.map(w=>parseInt(w,16));}
   const value=this.words.shift()!;if(value<limit)return value%bound;
  }
  return mapFail("generation_exhausted");
 }
}
/** Server-internal ordinary SOLO trial. Caller resolves eligibility/support before invoking.
 * v1 chooses the approved minimum width 3; parents share each next layer's three nodes.
 * Weighted independent draws are conditioned only on approved branch constraints. Their
 * resulting frequency is not the source weight/probability and no augment quota is added.
 */
export async function generateOperationMap(input:unknown){
 const request=captureMapGenerationRequest(input),pool=request.candidates;
 const weights=request.version===mapGenerationVersion?currentWeights:historicalWeights;
 const enabled=(Object.keys(weights) as Kind[]).filter(k=>weights[k]>0&&pool.some(c=>c.eventKind===k));
 const noncombat=(kind:string)=>kind!=="COMBAT"&&kind!=="ELITE"&&kind!=="BOSS";
 const bosses=pool.filter(c=>c.eventKind==="BOSS");
 if(!enabled.includes("COMBAT")||!enabled.some(noncombat)||!bosses.length)return mapFail("generation_pool_insufficient");
 const rng=new SeedStream(request.seed,request.version),total=enabled.reduce((n,k)=>n+weights[k],0);
 const pick=async()=>{let draw=await rng.below(total);for(const kind of enabled){if(draw<weights[kind]){const choices=pool.filter(c=>c.eventKind===kind);return choices[await rng.below(choices.length)]!;}draw-=weights[kind];}return mapFail("generation_invalid");};
 const nodes:OperationMapPlan["nodes"]=[],attemptsPerDepth:number[]=[];
 for(let depth=1;depth<=11;depth++){
  let accepted=false;
  for(let attempt=1;attempt<=256;attempt++){
   const layer=[await pick(),await pick(),await pick()];
   if(!layer.some(c=>noncombat(c.eventKind))||!layer.some(c=>c.eventKind==="COMBAT"||c.eventKind==="ELITE")||(depth===1&&!layer.some(c=>c.eventKind==="COMBAT")))continue;
   layer.forEach((c,slot)=>nodes.push({eventKind:c.eventKind,publicInfo:c.publicInfo,encounterProfileRef:c.encounterProfileRef,rewardPlanRef:c.rewardPlanRef,nodeId:`n${depth}-${slot}`,visitDepth:depth}));
   attemptsPerDepth.push(attempt);accepted=true;break;
  }
  if(!accepted)return mapFail("generation_exhausted");
 }
 const boss=bosses[await rng.below(bosses.length)]!;nodes.push({eventKind:boss.eventKind,publicInfo:boss.publicInfo,encounterProfileRef:boss.encounterProfileRef,rewardPlanRef:boss.rewardPlanRef,nodeId:"boss",visitDepth:12});
 const edges:OperationMapPlan["edges"]=[];
 for(const from of [{nodeId:"entry",visitDepth:0},...nodes.filter(n=>n.visitDepth<12)])for(const to of nodes.filter(n=>n.visitDepth===from.visitDepth+1))edges.push({edgeId:`${from.nodeId}:${to.nodeId}`,fromNodeId:from.nodeId,toNodeId:to.nodeId});
 const plan=captureOperationMap({version:"operation.map-plan.v1",profile:"ORDINARY_12_NODE",graphId:request.graphId,graphRevision:request.graphRevision,eventAssignmentVersion:request.version,definitionVersion:request.definitionVersion,entryNodeId:"entry",enabledEventKinds:enabled,nodes,edges});
 return {plan,generation:{request,attemptsPerDepth}};
}
