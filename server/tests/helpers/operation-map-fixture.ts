export function plan(){
 const nodes=Array.from({length:11},(_,i)=>Array.from({length:3},(_,j)=>({nodeId:`n${i+1}-${j}`,visitDepth:i+1,eventKind:j===0?"COMBAT":j===1?"EQUIPMENT":"ELITE",publicInfo:{name:`node ${i+1}-${j}`,iconId:`icon-${j}`},encounterProfileRef:"test-profile",rewardPlanRef:"private-reward"}))).flat();
 nodes.push({nodeId:"boss",visitDepth:12,eventKind:"BOSS",publicInfo:{name:"final",iconId:"boss-icon"},encounterProfileRef:"test-boss",rewardPlanRef:"private-boss"});
 const edges:{edgeId:string;fromNodeId:string;toNodeId:string}[]=[];
 for(const from of [{nodeId:"entry",visitDepth:0},...nodes.filter(n=>n.visitDepth<12)])for(const to of nodes.filter(n=>n.visitDepth===from.visitDepth+1))edges.push({edgeId:`${from.nodeId}:${to.nodeId}`,fromNodeId:from.nodeId,toNodeId:to.nodeId});
 return {version:"operation.map-plan.v1",profile:"ORDINARY_12_NODE",graphId:"test-graph",graphRevision:1,eventAssignmentVersion:"test-assignment",definitionVersion:"test-def",entryNodeId:"entry",enabledEventKinds:["COMBAT","EQUIPMENT","ELITE"],nodes,edges};
}
