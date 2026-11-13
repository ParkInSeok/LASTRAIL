using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

// Synthetic committed transport fixture using actual saved local geometry. Not a live server receipt.
public sealed class OrdinaryLiveExecutionTests {
 const string GeometryPath="D:/Project/LASTRAIL/unity-project/Assets/_LASTRAIL/Content/Battle/OrdinaryLiveGeometryV02/definition.json";
 const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
 static string RawHash(string bytes){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(bytes))).Replace("-","").ToLowerInvariant();}
 static object Invoke(object owner,string name,params object[] args){try{return owner.GetType().GetMethods(Hidden).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(owner,args);}catch(TargetInvocationException e){throw e.InnerException;}}
 static object Field(object owner,string name)=>owner.GetType().GetField(name,Hidden).GetValue(owner);
 internal static JObject CreateInput()=>CreateInput(GeometryPath);
 internal static JObject CreateInput(string geometryPath){
  Assert.IsTrue(File.Exists(geometryPath),"Run actual authoring builder before this integration fixture");string bytes=File.ReadAllText(geometryPath);var geometry=JObject.Parse(bytes);
  var pin=new JObject{["definitionId"]=geometry["definitionId"].DeepClone(),["version"]=geometry["version"].DeepClone(),["definitionHash"]=RawHash(bytes)};
  var original=FirstPlayableScalingTests.Fixture(staticSource:true,geometryReference:pin);var start=OrdinaryFiveStartInput.Read(original.Start,"a",(string)original.Start["runId"]);
  var template=((OrdinaryFiveStartInput Start,BRouteCommand Command,JObject Response))typeof(OrdinaryNodeCombatFactoryTests).GetMethod("Fixture",Hidden).Invoke(null,null);
  var node=(JObject)template.Response["receipt"]["nodeStart"].DeepClone();var snapshot=(JObject)node["snapshot"];var runtime=(JObject)snapshot["runtime"];var scaling=(JObject)original.Response["receipt"]["firstPlayableScaling"].DeepClone();
  snapshot["startSnapshotHash"]=start.StartSnapshotHash;runtime["enemies"]["scalingBindingHash"]=scaling["bindingHash"].DeepClone();
  runtime["effects"]=new JObject{["version"]="ordinary.local-entry-effects.v1.20261106",["accountId"]=start.AccountId,["runId"]=start.RunId,["nodeId"]=node["nodeId"].DeepClone(),["startSnapshotHash"]=start.StartSnapshotHash,["originGeneration"]=1,["revision"]=1,["atTick"]=0,["activeEffects"]=new JArray(),["shields"]=new JArray(),["interceptions"]=new JArray()};
  runtime["spatial"]=new JObject{["definitionBinding"]=new JObject{["version"]="ordinary.node-geometry-binding.v1.20261031",["accountId"]=start.AccountId,["runId"]=start.RunId,["nodeId"]=node["nodeId"].DeepClone(),["entryMapRevision"]=node["entryMapRevision"].DeepClone(),["originGeneration"]=1,["definitionRef"]=pin}};
  runtime["sequence"]=new JObject{["version"]="ordinary.obstacle-sequence.v1.20261106",["obstacle"]=OrdinaryObstacleSequenceTests.Definition(),["nextWave"]=new JObject{["kind"]="ORIGINAL_NODE_ENEMIES",["enemiesHash"]=BDirectAttackInput.Digest(runtime["enemies"]).Substring(7)}};
  node["snapshotHash"]=BDirectAttackInput.Digest(snapshot).Substring(7);
  var current=new JObject{["version"]="ordinary.current-attempt.v1.20261102",["accountId"]=start.AccountId,["runId"]=start.RunId,["nodeId"]=node["nodeId"].DeepClone(),["generation"]=1,["mapRevision"]=node["entryMapRevision"].DeepClone(),["snapshotHash"]=node["snapshotHash"].DeepClone()};
  return new JObject{["version"]=OrdinaryLocalExecutionReplay.InputVersion,["accountId"]=start.AccountId,["runId"]=start.RunId,["originalStart"]=original.Start,["initializationResponse"]=original.Init,["nodeStart"]=node,["firstPlayableScaling"]=scaling,["currentAttempt"]=current,["geometryDefinition"]=bytes,["inputs"]=new JArray()};
 }
 internal static OrdinaryFiveCombatFactory CreateFactory(JObject input){
  object parsed;try{parsed=typeof(OrdinaryLocalExecutionReplay).GetMethod("ReadInput",Hidden).Invoke(null,new object[]{input});}catch(TargetInvocationException e){throw e.InnerException;}
  var original=(OrdinaryFiveStartInput)Field(parsed,"Original");var node=(OrdinaryNodeStart)Field(parsed,"Node");var scaling=(FirstPlayableScaling)Field(parsed,"Scaling");var attempt=(OrdinaryNodeAttempt)Field(parsed,"Attempt");
  var factory=OrdinaryFiveCombatFactory.FromNodeStart(original,node,scaling,node.ReadActions(original).StepTimeSpanTicks,attempt);factory=(OrdinaryFiveCombatFactory)Invoke(factory,"WithLiveSources",(string)input["geometryDefinition"]);return input["boardingSource"] is JObject boarding?factory.WithBoardingSource(boarding,node.BoardingSourceHash):factory;
 }
 static void Record(JObject input,long tick,JObject command){var rows=(JArray)input["inputs"];rows.Add(new JObject{["sequence"]=rows.Count+1,["tick"]=tick,["command"]=command});}
 [Test] public void SavedMeasuredGeometryAndOriginalAttemptPinReachPublicReplay(){var input=CreateInput();var result=OrdinaryLocalExecutionReplay.Execute(input);Assert.AreEqual(0L,(long)result["tick"]);Assert.AreEqual(0L,(long)result["lastSequence"]);Assert.AreEqual(0,((JArray)result["healthChanges"]).Count);Assert.AreEqual(JTokenType.Null,result["evidence"].Type);var changed=(JObject)input.DeepClone();changed["geometryDefinition"]=(string)changed["geometryDefinition"]+" ";Assert.Throws<ArgumentException>(()=>OrdinaryLocalExecutionReplay.Execute(changed));}
 [Test] public void PauseAndAllThreeSpeedUnitsReplayExactlyThroughPublicEntry(){
  var input=CreateInput();var model=CreateFactory(input).Create();
  void Command(string kind,JToken value){Record(input,model.Tick,new JObject{["kind"]=kind,["value"]=value});if(kind=="PAUSE")model.SetPaused((bool)value);else model.SetSpeed((int)value);}
  void Advance(long elapsed,int budget){Record(input,model.Tick,new JObject{["kind"]="ADVANCE",["elapsed"]=elapsed,["budget"]=budget});model.Advance(elapsed,budget);}
  Command("PAUSE",true);Advance(1700000,20);Assert.AreEqual(0,model.Tick);Command("PAUSE",false);
  foreach(int speed in new[]{2,3,4}){Command("SPEED",speed);long before=model.Tick;Advance(340000,20);Assert.AreEqual(speed,model.Tick-before);}
  var replay=OrdinaryLocalExecutionReplay.Execute(input);Assert.AreEqual(model.Tick,(long)replay["tick"]);Assert.IsTrue(JToken.DeepEquals(JObject.Parse(model.CaptureLocalState()),replay["checkpoint"]));
 }
 [TestCase(7000)][TestCase(13000)] public void ActualObstacleThenOriginalWaveAdvancesWithoutClockResetOrForcedKills(int throttle){
  var input=CreateInput();var factory=CreateFactory(input);var driver=CombatSceneDriver.ForOrdinaryNode(factory,()=>true);driver.Open();Assert.IsTrue(driver.IsExecutionReady);
  driver.SetRailThrottle(throttle);long previous=0;string before=driver.StateJson;
  for(int batch=0;batch<100&&(string)driver.HudState["ordinaryObstacle"]["phase"]=="OBSTACLE";batch++){
   driver.Advance(5100000,30);long tick=(long)driver.HudState["tick"];Assert.Greater(tick,previous,"Actual cloned driver transaction must advance");previous=tick;
  }
  var checkpoint=JObject.Parse(driver.StateJson);Assert.AreEqual("COMBAT",(string)driver.HudState["ordinaryObstacle"]["phase"]);Assert.Greater(previous,0);Assert.AreNotEqual(before,driver.StateJson);
  var resolution=checkpoint["obstacleSequence"]["resolution"];CollectionAssert.Contains(new[]{"DESTROYED","PASSED","COLLISION"},(string)resolution["outcome"]);Assert.IsTrue((bool)resolution["tailBeyondZone"]);Assert.AreEqual(0,(int)resolution["unresolvedProjectiles"]);
  Assert.Greater(((JArray)checkpoint["obstacleExecution"]["events"]).Count,0);Assert.AreEqual(2,driver.HudState["actors"].Count(a=>(bool)a["enemy"]));Assert.Greater((long)checkpoint["basicExecution"]["startTick"],0);
  long started=(long)checkpoint["basicExecution"]["startTick"];Assert.AreEqual(started,(long)checkpoint["gunnerExecution"]["startTick"]);
  for(int i=0;i<8;i++)driver.Advance(5100000,30);
  checkpoint=JObject.Parse(driver.StateJson);Assert.Greater((long)driver.HudState["tick"],previous);Assert.AreEqual(started,(long)checkpoint["basicExecution"]["startTick"]);Assert.Greater((long)checkpoint["basicExecution"]["tick"],started);Assert.Greater((long)checkpoint["gunnerExecution"]["resolvedTick"],started);
  Assert.Greater(((JArray)checkpoint["gunnerExecution"]["shots"]).Count,0,"Original reachable gunner must emit through its scheduler");
  StringAssert.Contains("hpLoss",checkpoint["gunnerExecution"]["shots"].ToString(),"Actual projectile arrival should retain a health receipt; no test damage injection");driver.Retire();
 }
}
