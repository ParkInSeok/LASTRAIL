using System;
using System.IO;
using LastRail.Battle;
using LastRail.Diagnostics;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public sealed class OrdinaryRailV4Tests {
 static JObject Export(string folder)=>JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/OperatorInformation-20261019/"+folder+"/source.json"));
 static OrdinaryFiveStartInput Read(JObject e)=>OrdinaryFiveStartInput.Read((JObject)e["originalStart"],(string)e["accountId"],(string)e["runId"]);
 [TestCase("RailRoof797",false)][TestCase("PlayerBudget798",false)][TestCase("RailV4Compatibility800",true)]
 public void EachOriginalVersionRestoresItsOwnProgressWithoutUpgrading(string folder,bool motion){var e=Export(folder);var s=Read(e);Assert.AreEqual(motion?20000:(int?)null,s.Rail.InitialSpeedMmPerSecond);Assert.AreEqual(motion?0:(int?)null,s.Rail.InitialAccelerationMmPerSecondSquared);var f=OrdinaryFiveSeraFactory.FromInitialization(s,e["initializationResponse"].ToString(),500000);var model=f.Create();model.Advance(1000000,2);var before=model.CaptureLocalState();Assert.AreEqual(before,BasicCombatSimulation.RestoreLocalState(before,f.Create).CaptureLocalState());}
 [TestCase("speed")][TestCase("acceleration")][TestCase("geometry")][TestCase("policy")][TestCase("ownedHash")]
 public void RehashedV4ChangesReject(string field){var e=Export("RailV4Compatibility800");var source=e["originalStart"]["ordinaryRailSource"];var p=source["packet"];
  if(field=="speed")p["initialMotion"]["speedMmPerSecond"]=26000;
  if(field=="acceleration")p["initialMotion"]["accelerationMmPerSecondSquared"]=5000;
  if(field=="geometry")p["geometry"]["definitionHash"]=OrdinaryRailSource.GeometryHash;
  if(field=="policy")p["definitionVersion"]="unknown";
  if(field=="ownedHash")p["ownedSourceHash"]="extra";
  source["sourceHash"]=BDirectAttackInput.Digest(p).Substring(7);Assert.Throws<ArgumentException>(()=>Read(e));}
 [Test] public void RetainedStartCannotUseFactoryWithRoofRemoved(){
  var e=Export("RailV4Compatibility800");var retained=(JObject)e["originalStart"].DeepClone();var s=Read(e);string preview=(string)retained["operationPreviewId"];
  var response=new JObject{["accepted"]=true,["lookupStatus"]="COMMITTED",["runId"]=s.RunId,["runStatus"]="ACTIVE",["startResponse"]=retained,["errorCode"]=null};
  var original=new PendingAccountAction(s.AccountId,"departure-start",preview,0,"compat-qa",new JObject{["operationPreviewId"]=preview}.ToString(),response.ToString(Newtonsoft.Json.Formatting.None));
  ((JObject)e["originalStart"]).Remove("ordinaryRoofSource");((JObject)e["initializationResponse"]["initializationReceipt"]).Remove("ordinaryRoofBinding");var f=OrdinaryFiveSeraFactory.FromInitialization(Read(e),e["initializationResponse"].ToString(),500000);
  int writes=0;var store=new CombatCheckpointStore("D:/Project/LASTRAIL/LocalTemp/Unity/compat-test.json",()=>null,_=>writes++);
  Assert.Throws<ArgumentException>(()=>new OrdinaryFiveSeraCheckpoint(store,original,f,new AppBuildSnapshot("compat-qa",1)));Assert.AreEqual(0,writes);
 }
}
