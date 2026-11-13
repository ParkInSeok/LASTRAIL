import {expect,it} from "vitest";
import {captureOperationMap,projectOperationMap,requireDirectDestination} from "../../apps/game-api/src/operation-map-plan";
function plan(){
 const nodes=Array.from({length:11},(_,i)=>Array.from({length:3},(_,j)=>({nodeId:`n${i+1}-${j}`,visitDepth:i+1,eventKind:j===0?"COMBAT":j===1?"EQUIPMENT":"ELITE",publicInfo:{name:`node ${i+1}-${j}`,iconId:`icon-${j}`},encounterProfileRef:"test-profile",rewardPlanRef:"private-reward"}))).flat();
 nodes.push({nodeId:"boss",visitDepth:12,eventKind:"BOSS",publicInfo:{name:"final",iconId:"boss-icon"},encounterProfileRef:"test-boss",rewardPlanRef:"private-boss"});
 const edges:{edgeId:string;fromNodeId:string;toNodeId:string}[]=[];
 for(const from of [{nodeId:"entry",visitDepth:0},...nodes.filter(n=>n.visitDepth<12)])for(const to of nodes.filter(n=>n.visitDepth===from.visitDepth+1))edges.push({edgeId:`${from.nodeId}:${to.nodeId}`,fromNodeId:from.nodeId,toNodeId:to.nodeId});
 return {version:"operation.map-plan.v1",profile:"ORDINARY_12_NODE",graphId:"test-graph",graphRevision:1,eventAssignmentVersion:"test-assignment",definitionVersion:"test-def",entryNodeId:"entry",enabledEventKinds:["COMBAT","EQUIPMENT","ELITE"],nodes,edges};
}
it("reveals every assigned event including far and unavailable branches without private rewards",()=>{
 const p=captureOperationMap(plan()),path=["entry:n1-0","n1-0:n2-1"],view=projectOperationMap(p,path);
 expect(view.nodes).toHaveLength(34);expect(view.nodes.find(n=>n.nodeId==="boss")).toMatchObject({eventKind:"BOSS",publicInfo:{iconId:"boss-icon"}});
 expect(view.nodes.find(n=>n.nodeId==="n1-2")).toMatchObject({access:"UNREACHABLE",eventKind:"ELITE"});
 expect(view.nodes.find(n=>n.nodeId==="n3-0")).toMatchObject({access:"DIRECT"});
 expect(JSON.stringify(view)).not.toContain("private-reward");expect(JSON.stringify(view)).not.toContain("encounterProfileRef");
 expect(view.currentNodeId).toBe("n2-1");expect(projectOperationMap(p,path)).toEqual(view);
});
it("allows only direct authored edges regardless of full visibility",()=>{
 const p=captureOperationMap(plan());expect(requireDirectDestination(p,[],"entry:n1-0")).toMatchObject({toNodeId:"n1-0"});
 expect(()=>requireDirectDestination(p,[],"n5-0:n6-0")).toThrow("operation_map_edge_ineligible");
 expect(()=>projectOperationMap(p,["n5-0:n6-0"])).toThrow("operation_map_path_invalid");
});
it("preserves shared node identity and accepts zero augment paths",()=>{
 const p=captureOperationMap(plan());expect(p.nodes.some(n=>n.eventKind==="AUGMENT")).toBe(false);
 const a=projectOperationMap(p,["entry:n1-0"]),b=projectOperationMap(p,["entry:n1-1"]);
 expect(a.nodes.find(n=>n.nodeId==="n2-0")?.publicInfo).toEqual(b.nodes.find(n=>n.nodeId==="n2-0")?.publicInfo);
});
it("rejects duplicate identities/edges, cycles, skips, unknown nodes and isolated nodes",()=>{
 const invalid=[];let p=plan();p.nodes.push({...p.nodes[0]});invalid.push(p);
 p=plan();p.edges.push({...p.edges[0],edgeId:"duplicate-pair"});invalid.push(p);
 p=plan();p.edges[0].toNodeId="n2-0";invalid.push(p);
 p=plan();p.edges[0].toNodeId="absent";invalid.push(p);
 p=plan();p.edges.push({edgeId:"cycle",fromNodeId:"n2-0",toNodeId:"n1-0"});invalid.push(p);
 p=plan();p.nodes.push({...p.nodes[0],nodeId:"isolated"});invalid.push(p);
 for(const x of invalid)expect(()=>captureOperationMap(x)).toThrow("operation_map_plan_invalid");
});
it("requires all twelve visit layers, one boss and authored event icons",()=>{
 const invalid=[];let p=plan();p.nodes=p.nodes.filter(n=>n.visitDepth!==8);invalid.push(p);
 p=plan();p.nodes.find(n=>n.nodeId==="boss")!.eventKind="COMBAT";invalid.push(p);
 p=plan();p.nodes[10].publicInfo.iconId="";invalid.push(p);
 p=plan();p.nodes.push({...p.nodes.find(n=>n.nodeId==="boss")!,nodeId:"second-boss"});invalid.push(p);
 for(const x of invalid)expect(()=>captureOperationMap(x)).toThrow("operation_map_plan_invalid");
});
it("rejects hidden event substitution, unenabled events and elite-only choices",()=>{
 const invalid=[];let p=plan();p.nodes[3].eventKind="UNKNOWN";invalid.push(p);
 p=plan();p.nodes[3].eventKind="AUGMENT";invalid.push(p);
 p=plan();for(const n of p.nodes.filter(n=>n.visitDepth===3))n.eventKind="ELITE";invalid.push(p);
 for(const x of invalid)expect(()=>captureOperationMap(x)).toThrow("operation_map_plan_invalid");
});
it("does not convert old or story profiles and does not mutate the pinned input",()=>{
 expect(()=>captureOperationMap({...plan(),profile:"ORDINARY_15_WAVE"})).toThrow("operation_map_plan_invalid");
 expect(()=>captureOperationMap({...plan(),profile:"STORY_12_NODE"})).toThrow("operation_map_plan_invalid");
 const p=plan(),saved=captureOperationMap(p);p.nodes[0].publicInfo.name="changed";expect(saved.nodes[0].publicInfo.name).not.toBe("changed");
 const view=projectOperationMap(saved,[]);view.nodes[0].publicInfo.name="view-edit";expect(saved.nodes[0].publicInfo.name).not.toBe("view-edit");
});

it("reaches the terminal after exactly twelve visits and offers no extra destination",()=>{
 const p=captureOperationMap(plan()),path=["entry:n1-0",...Array.from({length:10},(_,i)=>`n${i+1}-0:n${i+2}-0`),"n11-0:boss"];
 const view=projectOperationMap(p,path);expect(view.currentNodeId).toBe("boss");expect(view.traversedEdgeIds).toHaveLength(12);expect(view.nodes.filter(n=>n.access==="VISITED")).toHaveLength(11);
 expect(view.nodes.some(n=>n.access==="DIRECT")).toBe(false);expect(()=>requireDirectDestination(p,path,"entry:n1-0")).toThrow("operation_map_edge_ineligible");
});
it("keeps missed future branch events visible even when there is no remaining path to them",()=>{
 const p=plan();p.nodes.push({...p.nodes.find(n=>n.nodeId==="n4-2")!,nodeId:"n4-extra",publicInfo:{name:"missed",iconId:"missed-icon"}});
 const incoming=p.edges.find(e=>e.fromNodeId==="n3-1"&&e.toNodeId==="n4-2")!;incoming.toNodeId="n4-extra";
 for(let j=0;j<3;j++)p.edges.push({edgeId:`extra:${j}`,fromNodeId:"n4-extra",toNodeId:`n5-${j}`});
 const view=projectOperationMap(captureOperationMap(p),["entry:n1-0","n1-0:n2-0","n2-0:n3-0"]);
 expect(view.nodes.find(n=>n.nodeId==="n4-extra")).toMatchObject({access:"UNREACHABLE",eventKind:"ELITE",publicInfo:{iconId:"missed-icon"}});
});
