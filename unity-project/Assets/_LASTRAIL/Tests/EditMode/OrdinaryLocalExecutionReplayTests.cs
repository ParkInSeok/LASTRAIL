using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class OrdinaryLocalExecutionReplayTests {
 const BindingFlags Hidden=BindingFlags.Static|BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 static object Call(Type type,string name,params object[] args){try{return type.GetMethod(name,Hidden).Invoke(null,args);}catch(TargetInvocationException e){throw e.InnerException;}}
 static object Read(JObject input)=>Call(typeof(OrdinaryLocalExecutionReplay),"ReadInput",input);
 static T Field<T>(object value,string name)=>(T)value.GetType().GetField(name,Hidden).GetValue(value);
 static void Pin(JObject value,string field){value.Remove(field);value[field]=BDirectAttackInput.Digest(value).Substring(7);}
 static string HashBytes(string value){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","").ToLowerInvariant();}
 static JObject Fixture(){
  // Intake-only synthetic definition; this is deliberately not a native geometry readiness test.
  string geometry="{\n\"version\":\""+OrdinaryGeometryReference.Version+"\",\"definitionId\":\"intake-test-only\"\n}";
  var reference=new JObject{["version"]=OrdinaryGeometryReference.Version,["definitionId"]="intake-test-only",["definitionHash"]=HashBytes(geometry)};
  var source=FirstPlayableScalingTests.Fixture(staticSource:true,geometryReference:reference);
  var wire=OrdinaryNodeStartWireTests.Fixture(reference);var node=(JObject)wire.Response["receipt"]["nodeStart"];
  node["snapshot"]["runtime"]["spatial"]["definitionBinding"]=new JObject{["version"]="ordinary.node-geometry-binding.v1.20261031",["accountId"]=node["accountId"].DeepClone(),["runId"]=node["runId"].DeepClone(),["nodeId"]=node["nodeId"].DeepClone(),["entryMapRevision"]=node["entryMapRevision"].DeepClone(),["originGeneration"]=1,["definitionRef"]=reference};
  node["snapshotHash"]=BDirectAttackInput.Digest(node["snapshot"]).Substring(7);
  return new JObject{["version"]=OrdinaryLocalExecutionReplay.InputVersion,["accountId"]=wire.Start.AccountId,["runId"]=source.Start["runId"].DeepClone(),
   ["originalStart"]=source.Start,["initializationResponse"]=source.Init,["nodeStart"]=node,["firstPlayableScaling"]=source.Response["receipt"]["firstPlayableScaling"].DeepClone(),
   ["currentAttempt"]=new JObject{["version"]="ordinary.current-attempt.v1.20261102",["accountId"]=node["accountId"].DeepClone(),["runId"]=node["runId"].DeepClone(),["nodeId"]=node["nodeId"].DeepClone(),["generation"]=2,["mapRevision"]=(long)node["entryMapRevision"]+1,["snapshotHash"]=node["snapshotHash"].DeepClone()},["geometryDefinition"]=geometry,["inputs"]=new JArray()};
 }
 [Test] public void IntakeRetainsOriginalNodeHpCurrentAttemptAndExactGeometryBytes(){
  var input=Fixture();var parsed=Read(input);var node=Field<OrdinaryNodeStart>(parsed,"Node");var attempt=Field<OrdinaryNodeAttempt>(parsed,"Attempt");
  Assert.AreEqual(70,(int)node.Capture()["snapshot"]["actors"][0]["hp"]);Assert.AreEqual(800,(int)node.Capture()["snapshot"]["train"]["hp"]);
  Assert.AreEqual(2,attempt.Generation);Assert.AreEqual((string)input["geometryDefinition"],Field<string>(parsed,"GeometryJson"));
  ((JArray)input["inputs"]).Add(new JObject());Assert.AreEqual(0,Field<JArray>(parsed,"Commands").Count);
  input["nodeStart"]["snapshot"]["actors"][0]["hp"]=1;Assert.AreEqual(70,(int)node.Capture()["snapshot"]["actors"][0]["hp"]);
 }
 [TestCase("rawHp")][TestCase("account")][TestCase("attempt")][TestCase("snapshot")][TestCase("geometryBytes")][TestCase("geometryObject")][TestCase("scaling")][TestCase("initialization")]
 public void IntakeRejectsUntrustedOrConflictingEnvelope(string fault){
  var input=Fixture();
  if(fault=="rawHp")input["rawHp"]=1;if(fault=="account")input["accountId"]="other";
  if(fault=="attempt")input["currentAttempt"]["snapshotHash"]=new string('0',64);
  if(fault=="snapshot")input["nodeStart"]["snapshot"]["actors"][0]["hp"]=1;
  if(fault=="geometryBytes")input["geometryDefinition"]=(string)input["geometryDefinition"]+" ";
  if(fault=="geometryObject")input["geometryDefinition"]=JObject.Parse((string)input["geometryDefinition"]);
  if(fault=="scaling"){input["firstPlayableScaling"]["applied"]["gunnerHp"]["numerator"]="1";Pin((JObject)input["firstPlayableScaling"],"bindingHash");}
  if(fault=="initialization")input["initializationResponse"]["initializationReceipt"]["phase"]="other";
  Assert.Catch(()=>Read(input));
 }
 static BasicCombatSimulation Model(){
  var f=((OrdinaryFiveStartInput Start,BRouteCommand Command,JObject Response))Call(typeof(OrdinaryNodeCombatFactoryTests),"Fixture");
  var parsed=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command);return OrdinaryFiveCombatFactory.FromNodeStart(f.Start,parsed.NodeStart,parsed.Scaling,170000).Create();
 }
 static JObject Row(long sequence,long tick,JObject command)=>new JObject{["sequence"]=sequence,["tick"]=tick,["command"]=command};
 static long Replay(BasicCombatSimulation model,JArray inputs)=>(long)Call(typeof(OrdinaryLocalExecutionReplay),"ReplayCommands",model,inputs);
 [Test] public void CommandsUseExistingModelControlsAndContiguousSequence(){
  var model=Model();var inputs=new JArray(Row(1,0,new JObject{["kind"]="PAUSE",["value"]=true}),Row(2,0,new JObject{["kind"]="SPEED",["value"]=3}),Row(3,0,new JObject{["kind"]="ULTIMATE_AUTO",["value"]=false}));
  Assert.AreEqual(3,Replay(model,inputs));Assert.IsTrue(model.IsPaused);var state=JObject.Parse(model.CaptureLocalState());Assert.AreEqual(3,(int)state["clock"]["speed"]);Assert.IsFalse((bool)state["ultimateAuto"]);Assert.AreEqual(0,model.Tick);
 }
 [TestCase("sequence")][TestCase("tick")][TestCase("rawHp")][TestCase("kind")][TestCase("boolean")][TestCase("speed")][TestCase("elapsed")]
 public void InvalidCommandsCannotChangeModel(string fault){
  var model=Model();var command=new JObject{["kind"]="PAUSE",["value"]=true};var row=Row(1,0,command);
  if(fault=="sequence")row["sequence"]=2;if(fault=="tick")row["tick"]=1;if(fault=="rawHp")command["rawHp"]=0;if(fault=="kind")command["kind"]="HIT";if(fault=="boolean")command["value"]=1;
  if(fault=="speed"){command["kind"]="SPEED";command["value"]=5;}
  if(fault=="elapsed")row["command"]=new JObject{["kind"]="ADVANCE",["elapsed"]=-1,["budget"]=1};
  var before=model.CaptureLocalState();Assert.Catch(()=>Replay(model,new JArray(row)));Assert.AreEqual(before,model.CaptureLocalState());
 }
}
