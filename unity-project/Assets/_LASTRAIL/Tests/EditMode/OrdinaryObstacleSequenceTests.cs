using System;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class OrdinaryObstacleSequenceTests {
 const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 static object Call(object target,string method,params object[] args){try{return target.GetType().GetMethod(method,Hidden).Invoke(target,args);}catch(TargetInvocationException e){throw e.InnerException;}}
 static JObject Resolution(OrdinaryNodeAttempt attempt,string outcome="DESTROYED",long at=0)=>new JObject{["accountId"]=attempt.AccountId,["runId"]=attempt.RunId,["nodeId"]=attempt.NodeId,["attemptGeneration"]=attempt.Generation,["snapshotHash"]=attempt.SnapshotHash,["eventId"]="synthetic-resolution",["atMs"]=at,["outcome"]=outcome,["obstacleHp"]=outcome=="DESTROYED"?0:25,["trainHp"]=outcome=="DEFEAT"?0:800,["tailBeyondZone"]=true,["remainingHazards"]=0,["unresolvedProjectiles"]=0,["collisionCommitted"]=outcome=="COLLISION"};
 internal static JObject Definition()=>JObject.Parse(@"{""version"":""ordinary.falling-obstacle.v1.20261106"",""hp"":100,""defense"":0,""shotDamage"":25,""launchIntervalMs"":1650,""preparationMs"":150,""rangeMm"":100000,""projectileSpeedMmPerSecond"":300000,""zoneStartOffsetMm"":277000,""zoneEndOffsetMm"":283000,""hazardAtMs"":16000,""horizontalBaseDamage"":600,""fallingBaseDamage"":900,""cruiseSpeedMmPerSecond"":20000,""slowSpeedMmPerSecond"":14000,""fastSpeedMmPerSecond"":26000,""accelerationMmPerSecondSquared"":5000}");
 internal static (OrdinaryFiveStartInput Start,BRouteCommand Command,JObject Response) Fixture(){
  // Consumer contract fixture, not admitted gameplay or an authoritative collision source.
  var f=((OrdinaryFiveStartInput Start,BRouteCommand Command,JObject Response))typeof(OrdinaryNodeCombatFactoryTests).GetMethod("Fixture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
  var route=f.Command.Binding.Capture();var profile=(JObject)route["pendingNode"]["encounterDefinition"];
  profile["ordinaryObstacle"]=Definition();route["pendingNode"]["encounterDefinitionHash"]=BDirectAttackInput.Digest(profile).Substring(7);
  route.Remove("packetHash");route["packetHash"]=BDirectAttackInput.Digest(route).Substring(7);
  var binding=BRouteBinding.Read(route.ToString(),(string)route["packetHash"],f.Start.AccountId,f.Start.RunId);
  var command=BRouteCommand.FromOriginal(binding,(JObject)f.Command.Capture()["command"]);
  var node=(JObject)f.Response["receipt"]["nodeStart"];var runtime=(JObject)node["snapshot"]["runtime"];
  runtime["sequence"]=new JObject{["version"]="ordinary.obstacle-sequence.v1.20261106",["obstacle"]=Definition(),["nextWave"]=new JObject{["kind"]="ORIGINAL_NODE_ENEMIES",["enemiesHash"]=BDirectAttackInput.Digest(runtime["enemies"]).Substring(7)}};
  node["snapshotHash"]=BDirectAttackInput.Digest(node["snapshot"]).Substring(7);
  return(f.Start,command,f.Response);
 }
 [Test] public void IssuedPrefixReachesOriginalNodeOwnerWithoutSpawningEnemies(){
  var f=Fixture();var arrival=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command);
  var factory=OrdinaryFiveCombatFactory.FromNodeStart(f.Start,arrival.NodeStart,arrival.Scaling,170000);
  var driver=CombatSceneDriver.ForOrdinaryNode(factory,()=>true);driver.Open();var hud=driver.HudState;
  Assert.AreEqual("OBSTACLE",(string)hud["ordinaryObstacle"]?["phase"]);
  Assert.IsFalse(hud["actors"].Any(a=>(bool)a["enemy"]));Assert.AreEqual(800,(int)hud["trainHp"]);
  Assert.IsFalse(driver.IsExecutionReady,"A sequence pin cannot replace missing execution sources.");
  var before=driver.StateJson;driver.Advance(10000000,20);Assert.AreEqual(before,driver.StateJson);
  driver.SetPaused(true);Assert.AreEqual("OBSTACLE",(string)driver.HudState["ordinaryObstacle"]["phase"]);
  var root=new UnityEngine.GameObject("ObstacleConsumerView",typeof(UnityEngine.RectTransform),typeof(UnityEngine.Canvas));
  try{
   var view=root.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));
   Assert.AreEqual("장애물 구간 준비 중",root.GetComponentsInChildren<TMPro.TMP_Text>(true).Single(t=>t.name=="Notice").text);
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 [Test] public void OriginalWaveIsReleasedOnceWithoutResettingHpMotionOrClock(){
  var f=Fixture();var arrival=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command);
  var factory=OrdinaryFiveCombatFactory.FromNodeStart(f.Start,arrival.NodeStart,arrival.Scaling,170000);
  var driver=CombatSceneDriver.ForOrdinaryNode(factory,()=>true);driver.Open();driver.SetSpeed(3);
  var before=driver.HudState;var resolution=Resolution(factory.Attempt);
  var result=(JObject)Call(driver,"ResolveOrdinaryObstacle",resolution);var after=driver.HudState;
  Assert.AreEqual("COMBAT",(string)result["phase"]);Assert.AreEqual(2,(int)result["revision"]);
  Assert.AreEqual(2,after["actors"].Count(a=>(bool)a["enemy"]));Assert.AreEqual(1,(int)after["wave"]);
  Assert.IsTrue(JToken.DeepEquals(before["ordinaryRail"],after["ordinaryRail"]));Assert.AreEqual(before["trainHp"],after["trainHp"]);Assert.AreEqual(before["tick"],after["tick"]);Assert.AreEqual(3,(int)after["speedHalfUnits"]);
  var saved=driver.StateJson;driver.TryGetCommittedRevision(out long rev);
  Assert.IsTrue(JToken.DeepEquals(result,(JObject)Call(driver,"ResolveOrdinaryObstacle",resolution)));
  Assert.AreEqual(saved,driver.StateJson);driver.TryGetCommittedRevision(out long again);Assert.AreEqual(rev,again);
  Assert.IsFalse(driver.IsExecutionReady);driver.Retire();Assert.Throws<InvalidOperationException>(()=>Call(driver,"ResolveOrdinaryObstacle",resolution));
 }
 [TestCase("enemy-hash")][TestCase("definition")][TestCase("unissued")][TestCase("missing")][TestCase("extra")]
 public void InvalidOrUnissuedSequenceCannotEnterNode(string fault){
  var f=Fixture();var runtime=(JObject)f.Response["receipt"]["nodeStart"]["snapshot"]["runtime"];
  if(fault=="enemy-hash")runtime["sequence"]["nextWave"]["enemiesHash"]=new string('0',64);
  if(fault=="definition")runtime["sequence"]["obstacle"]["hp"]=101;
  if(fault=="extra")runtime["sequence"]["freeReward"]=true;
  if(fault=="missing")runtime.Remove("sequence");
  if(fault=="unissued"){
   var route=f.Command.Binding.Capture();((JObject)route["pendingNode"]["encounterDefinition"]).Remove("ordinaryObstacle");route["pendingNode"]["encounterDefinitionHash"]=BDirectAttackInput.Digest(route["pendingNode"]["encounterDefinition"]).Substring(7);route.Remove("packetHash");route["packetHash"]=BDirectAttackInput.Digest(route).Substring(7);
   f.Command=BRouteCommand.FromOriginal(BRouteBinding.Read(route.ToString(),(string)route["packetHash"],f.Start.AccountId,f.Start.RunId),(JObject)f.Command.Capture()["command"]);
  }
  f.Response["receipt"]["nodeStart"]["snapshotHash"]=BDirectAttackInput.Digest(f.Response["receipt"]["nodeStart"]["snapshot"]).Substring(7);
  Assert.Throws<ArgumentException>(()=>BRouteCommandResult.Read(200,f.Response.ToString(),f.Command));
 }
 [TestCase("PASSED")][TestCase("COLLISION")][TestCase("DEFEAT")]
 public void InternalContractPreservesTerminalAndClearanceRules(string outcome){
  var f=Fixture();var arrival=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command);var attempt=OrdinaryNodeAttempt.FromEntry(arrival.NodeStart);
  var type=typeof(OrdinaryNodeStart).Assembly.GetType("LastRail.Run.OrdinaryObstacleSequence");
  var pin=typeof(OrdinaryNodeStart).GetProperty("Obstacle").GetValue(arrival.NodeStart);
  object Fresh()=>Activator.CreateInstance(type,Hidden,null,new[]{pin,attempt},null);
  var sequence=Fresh();var resolution=Resolution(attempt,outcome,16000);
  if(outcome!="DEFEAT"){
   foreach(var field in new[]{"tailBeyondZone","remainingHazards","unresolvedProjectiles"}){
    var invalid=(JObject)resolution.DeepClone();invalid[field]=field=="tailBeyondZone"?new JValue(false):new JValue(1);
    Assert.Throws<ArgumentException>(()=>Call(sequence,"Resolve",invalid));Assert.AreEqual("OBSTACLE",(string)((JObject)Call(sequence,"Capture"))["phase"]);
   }
  }
  var receipt=(JObject)Call(sequence,"Resolve",resolution);Assert.AreEqual(outcome=="DEFEAT"?"DEFEAT":"COMBAT",(string)receipt["phase"]);
  Assert.AreEqual(outcome=="DEFEAT",receipt["nextWave"].Type==JTokenType.Null);
  var stale=(JObject)resolution.DeepClone();stale["attemptGeneration"]=2;Assert.Throws<ArgumentException>(()=>Call(sequence,"Resolve",stale));
  var changed=(JObject)resolution.DeepClone();changed["atMs"]=16001;Assert.Throws<ArgumentException>(()=>Call(sequence,"Resolve",changed));
 }
 [Test] public void RestartUsesCurrentGenerationAndOriginalPin(){
  var f=Fixture();var arrival=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command);var node=arrival.NodeStart;
  var response=new JObject{["accepted"]=true,["version"]="ordinary.node-restart.v1.20261028",["requestId"]="restart",["accountId"]=node.AccountId,["runId"]=node.RunId,["nodeId"]=node.NodeId,["status"]="RESTARTED",["revision"]=node.EntryMapRevision+1,["generation"]=2,["snapshot"]=node.Capture()["snapshot"],["snapshotHash"]=node.SnapshotHash};
  var accepted=new OrdinaryNodeRestart(node,"restart",node.EntryMapRevision,1).ReadResponse(response.ToString());
  var attempt=OrdinaryNodeAttempt.FromRestart(accepted);var factory=OrdinaryFiveCombatFactory.FromNodeStart(f.Start,node,arrival.Scaling,170000,attempt);
  var driver=CombatSceneDriver.ForOrdinaryNode(factory,()=>true);driver.Open();
  Assert.AreEqual(2,(int)driver.HudState["ordinaryObstacle"]["attemptGeneration"]);Assert.AreEqual("OBSTACLE",(string)driver.HudState["ordinaryObstacle"]["phase"]);
  Assert.Throws<ArgumentException>(()=>Call(driver,"ResolveOrdinaryObstacle",Resolution(OrdinaryNodeAttempt.FromEntry(node))));
 }
 [Test] public void DeathCommitsFailureWithoutReleasingWave(){
  var f=Fixture();var node=f.Response["receipt"]["nodeStart"];node["snapshot"]["train"]["hp"]=0;node["snapshotHash"]=BDirectAttackInput.Digest(node["snapshot"]).Substring(7);
  var arrival=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command);var factory=OrdinaryFiveCombatFactory.FromNodeStart(f.Start,arrival.NodeStart,arrival.Scaling,170000);
  var driver=CombatSceneDriver.ForOrdinaryNode(factory,()=>true);driver.Open();Call(driver,"ResolveOrdinaryObstacle",Resolution(factory.Attempt,"DEFEAT"));
  Assert.AreEqual(BasicCombatOutcome.Failure,driver.Outcome);Assert.AreEqual(0,(int)driver.HudState["wave"]);
  Assert.IsFalse(driver.HudState["actors"].Any(a=>(bool)a["enemy"]));Assert.AreEqual(JTokenType.Null,driver.HudState["ordinaryObstacle"]["nextWave"].Type);
  var root=new UnityEngine.GameObject("ObstacleDefeatView",typeof(UnityEngine.RectTransform),typeof(UnityEngine.Canvas));
  try{
   var view=root.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));
   Assert.AreEqual("작전 실패",root.GetComponentsInChildren<TMPro.TMP_Text>(true).Single(t=>t.name=="Notice").text);
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 [TestCase("trainHp")][TestCase("atMs")]
 public void ResolutionCannotSubstituteForOriginalHealthOrClock(string field){
  var f=Fixture();var arrival=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command);var factory=OrdinaryFiveCombatFactory.FromNodeStart(f.Start,arrival.NodeStart,arrival.Scaling,170000);
  var model=factory.Create();var before=model.CaptureLocalState();var resolution=Resolution(factory.Attempt);resolution[field]=(long)resolution[field]+1;
  Assert.Throws<ArgumentException>(()=>Call(model,"ResolveOrdinaryObstacle",resolution));Assert.AreEqual(before,model.CaptureLocalState());
 }
}
