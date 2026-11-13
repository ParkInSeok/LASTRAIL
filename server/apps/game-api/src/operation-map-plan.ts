import {z} from "zod";
import {ActivityError} from "./activity-policy";
const id=z.string().min(1).max(200),revision=z.number().int().min(1).max(Number.MAX_SAFE_INTEGER);
const event=z.enum(["COMBAT","ELITE","AUGMENT","EQUIPMENT","ENCHANT","MAINTENANCE","MERCHANT","SCOUT"]);
const node=z.strictObject({nodeId:id,visitDepth:z.number().int().min(1).max(12),eventKind:z.enum([...event.options,"BOSS"]),publicInfo:z.strictObject({name:z.string().min(1).max(200),iconId:id}),encounterProfileRef:id,rewardPlanRef:id});
const edge=z.strictObject({edgeId:id,fromNodeId:id,toNodeId:id});
const schema=z.strictObject({version:z.literal("operation.map-plan.v1"),profile:z.literal("ORDINARY_12_NODE"),graphId:id,graphRevision:revision,eventAssignmentVersion:id,definitionVersion:id,entryNodeId:id,enabledEventKinds:z.array(event).min(1).max(8),nodes:z.array(node).min(1).max(78),edges:z.array(edge).min(1).max(1000)});
export type OperationMapPlan=z.infer<typeof schema>;
const fail=(code:string):never=>{throw new ActivityError(409,code);};
const combat=(kind:string)=>kind==="COMBAT"||kind==="ELITE"||kind==="BOSS";
/** Validates an explicitly authored ordinary plan, not content support or random generation.
 * START must resolve catalog references and atomically pin this plan with its admission snapshot.
 */
export function captureOperationMap(input:unknown):OperationMapPlan{
 const parsed=schema.safeParse(input);if(!parsed.success)return fail("operation_map_plan_invalid");const p=parsed.data;
 const invalid=()=>fail("operation_map_plan_invalid"),nodes=new Map(p.nodes.map(n=>[n.nodeId,n])),byFrom=new Map<string,z.infer<typeof edge>[]>(),pairs=new Set<string>();
 if(nodes.size!==p.nodes.length||nodes.has(p.entryNodeId)||new Set(p.enabledEventKinds).size!==p.enabledEventKinds.length||new Set(p.edges.map(e=>e.edgeId)).size!==p.edges.length)return invalid();
 for(let depth=1;depth<=12;depth++){const layer=p.nodes.filter(n=>n.visitDepth===depth);if(depth===12?layer.length!==1:layer.length<3||layer.length>7)return invalid();}
 for(const n of p.nodes)if((n.visitDepth===12)!==(n.eventKind==="BOSS")||(n.eventKind!=="BOSS"&&!p.enabledEventKinds.includes(n.eventKind)))return invalid();
 for(const e of p.edges){
  const fromDepth=e.fromNodeId===p.entryNodeId?0:nodes.get(e.fromNodeId)?.visitDepth,to=nodes.get(e.toNodeId),pair=JSON.stringify([e.fromNodeId,e.toNodeId]);
  if(fromDepth===undefined||!to||to.visitDepth!==fromDepth+1||pairs.has(pair))return invalid();
  pairs.add(pair);byFrom.set(e.fromNodeId,[...(byFrom.get(e.fromNodeId)??[]),e]);
 }
 const mixed=p.enabledEventKinds.some(combat)&&p.enabledEventKinds.some(k=>!combat(k));
 for(const parent of [{nodeId:p.entryNodeId,visitDepth:0,eventKind:"ENTRY"},...p.nodes]){
  const children=(byFrom.get(parent.nodeId)??[]).map(e=>nodes.get(e.toNodeId)!);
  if(children.length!==(parent.visitDepth===12?0:parent.visitDepth===11?1:3))return invalid();
  if(parent.visitDepth<11){
   if(parent.visitDepth===0&&(!children.some(n=>n.eventKind==="COMBAT")||!children.some(n=>!combat(n.eventKind))))return invalid();
   if(mixed&&(!children.some(n=>combat(n.eventKind))||!children.some(n=>!combat(n.eventKind))))return invalid();
   if(parent.eventKind==="ELITE"&&!children.some(n=>n.eventKind!=="ELITE"))return invalid();
  }
 }
 const reached=new Set([p.entryNodeId]),pending=[p.entryNodeId];
 while(pending.length){const current=pending.pop()!;for(const e of byFrom.get(current)??[])if(!reached.has(e.toNodeId)){reached.add(e.toNodeId);pending.push(e.toNodeId);}}
 if(reached.size!==p.nodes.length+1)return invalid();
 return p;
}
function cursor(p:OperationMapPlan,input:unknown){
 const parsed=z.array(id).max(12).safeParse(input);if(!parsed.success)return fail("operation_map_path_invalid");
 let current=p.entryNodeId;const visited:string[]=[],edges=new Map(p.edges.map(e=>[e.edgeId,e]));
 for(const edgeId of parsed.data){const e=edges.get(edgeId);if(!e||e.fromNodeId!==current)return fail("operation_map_path_invalid");visited.push(current);current=e.toNodeId;}
 return {current,visited,path:parsed.data};
}
/** Read-only full reveal from a pinned plan and trusted persisted traversal. No reroll or destination mutation. */
export function projectOperationMap(input:unknown,traversedEdgeIds:unknown){
 const p=captureOperationMap(input),c=cursor(p,traversedEdgeIds),reachable=new Set([c.current]),pending=[c.current];
 while(pending.length){const current=pending.pop()!;for(const e of p.edges)if(e.fromNodeId===current&&!reachable.has(e.toNodeId)){reachable.add(e.toNodeId);pending.push(e.toNodeId);}}
 const direct=new Set(p.edges.filter(e=>e.fromNodeId===c.current).map(e=>e.toNodeId));
 return {version:"operation.map-view.v1" as const,graphId:p.graphId,graphRevision:p.graphRevision,eventAssignmentVersion:p.eventAssignmentVersion,definitionVersion:p.definitionVersion,profile:p.profile,entryNodeId:p.entryNodeId,currentNodeId:c.current,traversedEdgeIds:c.path,
  nodes:p.nodes.map(n=>({nodeId:n.nodeId,visitDepth:n.visitDepth,eventKind:n.eventKind,publicInfo:{...n.publicInfo},access:n.nodeId===c.current?"CURRENT":c.visited.includes(n.nodeId)?"VISITED":direct.has(n.nodeId)?"DIRECT":reachable.has(n.nodeId)?"FUTURE":"UNREACHABLE"})),
  edges:p.edges.map(e=>({...e,traversed:c.path.includes(e.edgeId)}))};
}
/** Structural eligibility only. The command owner must ALSO check node completion, holds, account,
 * run status and progress revision, then persist destination+receipt atomically before movement.
 */
export function requireDirectDestination(input:unknown,traversedEdgeIds:unknown,edgeId:unknown){
 const p=captureOperationMap(input),c=cursor(p,traversedEdgeIds),target=p.edges.find(e=>e.edgeId===edgeId&&e.fromNodeId===c.current);
 if(!target)return fail("operation_map_edge_ineligible");return {...target};
}
