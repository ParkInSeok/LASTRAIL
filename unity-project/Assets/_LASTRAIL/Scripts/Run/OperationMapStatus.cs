using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 public sealed class OperationMapChoice {
  public string EdgeId{get;} public string NodeId{get;} public string Name{get;} public string EventKind{get;} public string IconId{get;}
  internal OperationMapChoice(string edge,string node,string name,string eventKind,string iconId){EdgeId=edge;NodeId=node;Name=name;EventKind=eventKind;IconId=iconId;}
 }
 // Display eligibility only; the destination command remains server-authorized.
 public sealed class OperationMapStatus {
  readonly JObject original;
  public string RunId{get;} public long Revision{get;} public string Phase{get;} public string RunStatus{get;}
  public bool CanChoose{get;} public ReadOnlyCollection<OperationMapChoice> Choices{get;}
  public OrdinaryCurrentAttempt CurrentAttempt{get;}
  public string CurrentNodeId{get;} public string CurrentNodeName{get;}
  public bool BlockedByPendingTransactions=>(bool)original["progress"]["blockedByPendingTransactions"];
  OperationMapStatus(JObject p,List<OperationMapChoice> choices){original=p;RunId=(string)p["runId"];Revision=(long)p["revision"];Phase=(string)p["progress"]["phase"];RunStatus=(string)p["runStatus"];Choices=choices.AsReadOnly();CanChoose=RunStatus=="ACTIVE"&&Phase=="READY"&&!BlockedByPendingTransactions&&Revision<9007199254740991L&&Choices.Count>0;
   CurrentNodeId=(string)p["view"]["currentNodeId"];CurrentNodeName=(string)((JArray)p["view"]["nodes"]).FirstOrDefault(n=>(string)n["nodeId"]==CurrentNodeId)?["publicInfo"]?["name"];
   if(p["currentAttempt"] is JObject value){CurrentAttempt=new OrdinaryCurrentAttempt(value);Require(RunStatus=="ACTIVE"&&Phase=="ACTIVE"&&CurrentAttempt.RunId==RunId&&CurrentAttempt.Revision==Revision&&CurrentAttempt.NodeId==(string)p["view"]["currentNodeId"]);}
   else Require(p.Property("currentAttempt")==null||p["currentAttempt"].Type==JTokenType.Null);
  }
  public JObject Capture()=>(JObject)original.DeepClone();
  static void Require(bool valid){if(!valid)throw new ArgumentException("operation_map_status_invalid");}
  static string Id(JToken p,string key){var value=BDirectAttackInput.Text(p,key,200);Require(value.Length>0);return value;}
  static bool Flag(JToken p,string key){Require(p?[key]?.Type==JTokenType.Boolean);return (bool)p[key];}
  static JArray Array(JToken p,int min,int max){var a=p as JArray;Require(a!=null&&a.Count>=min&&a.Count<=max);return a;}
  public static OperationMapStatus Read(string json,string expectedRun){
   var p=SafeTransitionWire.Parse(json);BDirectAttackInput.Fields(p,new[]{"version","runId","runStatus","revision","progress","view"}.Concat(p.Property("currentAttempt")==null?System.Array.Empty<string>():new[]{"currentAttempt"}).ToArray());
   Require((string)p["version"]=="operation.map-status.v1"&&Id(p,"runId")==expectedRun);Id(p,"runStatus");BDirectAttackInput.Nat(p,"revision",1);
   var progress=p["progress"] as JObject;BDirectAttackInput.Fields(progress,"phase","pendingEdgeId","blockedByPendingTransactions");
   var phase=Id(progress,"phase");Require(new[]{"READY","TRAVELLING","ACTIVE","FINAL_NODE_COMPLETE"}.Contains(phase));Flag(progress,"blockedByPendingTransactions");
   Require((phase=="TRAVELLING")== (progress["pendingEdgeId"].Type!=JTokenType.Null));if(phase=="TRAVELLING")Id(progress,"pendingEdgeId");
   var view=p["view"] as JObject;BDirectAttackInput.Fields(view,"version","graphId","graphRevision","eventAssignmentVersion","definitionVersion","profile","entryNodeId","currentNodeId","traversedEdgeIds","nodes","edges");
   Require((string)view["version"]=="operation.map-view.v1"&&(string)view["profile"]=="ORDINARY_12_NODE");
   foreach(var key in new[]{"graphId","eventAssignmentVersion","definitionVersion"})Id(view,key);BDirectAttackInput.Nat(view,"graphRevision",1);
   var entry=Id(view,"entryNodeId");var current=Id(view,"currentNodeId");var nodes=new Dictionary<string,JObject>();
   foreach(var token in Array(view["nodes"],1,78)){
    var node=token as JObject;BDirectAttackInput.Fields(node,"nodeId","visitDepth","eventKind","publicInfo","access");var id=Id(node,"nodeId");Require(id!=entry&&!nodes.ContainsKey(id));nodes.Add(id,node);BDirectAttackInput.Nat(node,"visitDepth",1,12);
    Require(new[]{"COMBAT","ELITE","AUGMENT","EQUIPMENT","ENCHANT","MAINTENANCE","MERCHANT","SCOUT","BOSS"}.Contains(Id(node,"eventKind")));
    Require(new[]{"CURRENT","VISITED","DIRECT","FUTURE","UNREACHABLE"}.Contains(Id(node,"access")));Require(((string)node["access"]=="CURRENT")== (id==current));
    var info=node["publicInfo"] as JObject;BDirectAttackInput.Fields(info,"name","iconId");Id(info,"name");Id(info,"iconId");
   }
   Require(current==entry||nodes.ContainsKey(current));var edges=new Dictionary<string,JObject>();var pairs=new HashSet<string>();var choices=new List<OperationMapChoice>();var direct=new HashSet<string>();
   foreach(var token in Array(view["edges"],1,1000)){
    var edge=token as JObject;BDirectAttackInput.Fields(edge,"edgeId","fromNodeId","toNodeId","traversed");var id=Id(edge,"edgeId");var from=Id(edge,"fromNodeId");var to=Id(edge,"toNodeId");Flag(edge,"traversed");
    Require(!edges.ContainsKey(id)&&(from==entry||nodes.ContainsKey(from))&&nodes.ContainsKey(to));edges.Add(id,edge);
    Require(pairs.Add(new JArray(from,to).ToString(Newtonsoft.Json.Formatting.None)));Require((long)nodes[to]["visitDepth"]==(from==entry?0:(long)nodes[from]["visitDepth"])+1);
    if(from==current){Require((string)nodes[to]["access"]=="DIRECT");direct.Add(to);choices.Add(new OperationMapChoice(id,to,(string)nodes[to]["publicInfo"]["name"],(string)nodes[to]["eventKind"],(string)nodes[to]["publicInfo"]["iconId"]));}
   }
   Require(nodes.Values.All(n=>((string)n["access"]=="DIRECT")==direct.Contains((string)n["nodeId"])));
   var path=Array(view["traversedEdgeIds"],0,12);var visited=new HashSet<string>();var pathEdges=new HashSet<string>();var cursor=entry;
   foreach(var token in path){Require(token.Type==JTokenType.String);var id=(string)token;Require(edges.ContainsKey(id)&&pathEdges.Add(id));var edge=edges[id];Require((string)edge["fromNodeId"]==cursor);visited.Add(cursor);cursor=(string)edge["toNodeId"];}
   Require(cursor==current&&edges.All(e=>(bool)e.Value["traversed"]==pathEdges.Contains(e.Key)));
   Require(nodes.Values.All(n=>((string)n["access"]=="VISITED")==visited.Contains((string)n["nodeId"])));
   if(phase=="TRAVELLING")Require(edges.ContainsKey((string)progress["pendingEdgeId"])&&(string)edges[(string)progress["pendingEdgeId"]]["fromNodeId"]==current);
   return new OperationMapStatus(p,choices);
  }
 }
}
