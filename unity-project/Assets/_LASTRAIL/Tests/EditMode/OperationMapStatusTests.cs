#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public sealed class OperationMapStatusTests {
  static Type Type(){var t=typeof(BRouteBinding).Assembly.GetType("LastRail.Run.OperationMapStatus");Assert.NotNull(t,"Missing operation map display parser");return t;}
  static object Read(JObject p,string run="run")=>Type().GetMethod("Read").Invoke(null,new object[]{p.ToString(),run});
  static object Get(object value,string name)=>value.GetType().GetProperty(name).GetValue(value);
  static JObject Sample()=>JObject.Parse(@"{'version':'operation.map-status.v1','runId':'run','runStatus':'ACTIVE','revision':1,'progress':{'phase':'READY','pendingEdgeId':null,'blockedByPendingTransactions':false},'view':{'version':'operation.map-view.v1','graphId':'graph','graphRevision':1,'eventAssignmentVersion':'events','definitionVersion':'defs','profile':'ORDINARY_12_NODE','entryNodeId':'entry','currentNodeId':'entry','traversedEdgeIds':[],'nodes':[{'nodeId':'n1','visitDepth':1,'eventKind':'COMBAT','publicInfo':{'name':'First','iconId':'combat'},'access':'DIRECT'},{'nodeId':'n2','visitDepth':2,'eventKind':'ELITE','publicInfo':{'name':'Future','iconId':'elite'},'access':'FUTURE'}],'edges':[{'edgeId':'e1','fromNodeId':'entry','toNodeId':'n1','traversed':false},{'edgeId':'e2','fromNodeId':'n1','toNodeId':'n2','traversed':false}]}}");
  [Test] public void OnlyDirectCandidatesAreOfferedAndFullDisplaySnapshotIsIsolated(){var p=Sample();var result=Read(p);Assert.AreEqual("run",Get(result,"RunId"));Assert.AreEqual(1L,Get(result,"Revision"));Assert.AreEqual("READY",Get(result,"Phase"));Assert.AreEqual(true,Get(result,"CanChoose"));var choices=((IEnumerable)Get(result,"Choices")).Cast<object>().ToArray();Assert.AreEqual(1,choices.Length);Assert.AreEqual("e1",Get(choices[0],"EdgeId"));Assert.AreEqual("n1",Get(choices[0],"NodeId"));Assert.AreEqual("First",Get(choices[0],"Name"));var capture=(JObject)result.GetType().GetMethod("Capture").Invoke(result,null);Assert.IsTrue(JToken.DeepEquals(p,capture));capture["view"]["nodes"][0]["publicInfo"]["name"]="mutated";Assert.AreEqual("First",Get(choices[0],"Name"));Assert.IsFalse(JToken.DeepEquals(capture,result.GetType().GetMethod("Capture").Invoke(result,null) as JToken));}
  [TestCase("hold")][TestCase("travelling")][TestCase("inactive")][TestCase("exhausted")]
  public void ValidNonChoosingStatesRemainDisplayable(string mode){var p=Sample();if(mode=="hold")p["progress"]["blockedByPendingTransactions"]=true;if(mode=="travelling"){p["progress"]["phase"]="TRAVELLING";p["progress"]["pendingEdgeId"]="e1";}if(mode=="inactive")p["runStatus"]="FINISH_PENDING";if(mode=="exhausted")p["revision"]=9007199254740991L;Assert.AreEqual(false,Get(Read(p),"CanChoose"));}
  [Test] public void ServerMaximumLengthIdsAndNamesAreAccepted(){var p=Sample();p["runId"]=new string('r',200);p["view"]["nodes"][0]["publicInfo"]["name"]=new string('n',200);Assert.NotNull(Read(p,new string('r',200)));}
  [Test] public void EmptyGraphAndOversizeWireAreRejected(){var p=Sample();p["view"]["nodes"]=new JArray();Assert.Throws<TargetInvocationException>(()=>Read(p));p=Sample();p["view"]["nodes"][0]["publicInfo"]["name"]=new string('x',300000);Assert.Throws<TargetInvocationException>(()=>Read(p));}
  [Test] public void TraversalMustLeadToCurrentAndMatchEdgeFlags(){var p=Sample();p["view"]["currentNodeId"]="n1";p["view"]["nodes"][0]["access"]="CURRENT";p["view"]["nodes"][1]["access"]="DIRECT";p["view"]["traversedEdgeIds"]=new JArray("e1");p["view"]["edges"][0]["traversed"]=true;Assert.AreEqual("e2",Get(((IEnumerable)Get(Read(p),"Choices")).Cast<object>().Single(),"EdgeId"));p["view"]["edges"][0]["traversed"]=false;Assert.Throws<TargetInvocationException>(()=>Read(p));}
  [TestCase("foreign")][TestCase("version")][TestCase("revision")][TestCase("missing")][TestCase("nodes")][TestCase("edges")][TestCase("access")][TestCase("dangling")][TestCase("pending")][TestCase("name")]
  public void InvalidOrAmbiguousCandidatesAreRejected(string mode){var p=Sample();switch(mode){case "foreign":p["runId"]="other";break;case "version":p["version"]="unknown";break;case "revision":p["revision"]=9007199254740992L;break;case "missing":((JObject)p["progress"]).Remove("blockedByPendingTransactions");break;case "nodes":((JArray)p["view"]["nodes"]).Add(p["view"]["nodes"][0].DeepClone());break;case "edges":((JArray)p["view"]["edges"]).Add(p["view"]["edges"][0].DeepClone());break;case "access":p["view"]["nodes"][0]["access"]="FUTURE";break;case "dangling":p["view"]["edges"][0]["toNodeId"]="absent";break;case "pending":p["progress"]["pendingEdgeId"]="e1";break;case "name":((JObject)p["view"]["nodes"][0]["publicInfo"]).Remove("name");break;}var ex=Assert.Throws<TargetInvocationException>(()=>Read(p));Assert.IsInstanceOf<ArgumentException>(ex.InnerException);}
 }
}
#endif
