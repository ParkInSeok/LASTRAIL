using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public class OrdinaryFiveSeraFactoryTests {
 public static (JObject Start,OrdinaryFiveStartInput Input,OrdinaryFiveCoreStatSnapshot Stats) Sources(){
  var fixture=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/OperatorInformation-20261019/Stats589/fixture.json"));var origin=(JObject)fixture["origin"];var start=OrdinaryFiveStartInputTests.Fixture();
  start["runId"]=origin["runId"].DeepClone();start["operationPreviewId"]="preview";start["ordinaryStatSource"]=new JObject{["origin"]=origin.DeepClone(),["originHash"]=fixture["originHash"].DeepClone()};
  var admission=start["admission"];admission["formationRevision"]=origin["formationRevision"].DeepClone();admission["loadoutRevision"]=origin["loadoutRevision"].DeepClone();admission["formation"]["operators"]=new JArray(origin["actors"].Select(a=>new JObject{["operatorId"]=a["operatorId"].DeepClone(),["slot"]=a["slot"].DeepClone()}));
  var packet=start["ordinaryKitSource"]["packet"];packet["accountId"]=origin["accountId"].DeepClone();packet["runId"]=origin["runId"].DeepClone();packet["originHash"]=fixture["originHash"].DeepClone();foreach(var op in packet["operators"])op["slot"]=origin["actors"].Single(a=>(string)a["operatorId"]==(string)op["operatorId"])["slot"].DeepClone();
  start["ordinaryKitSource"]["kitHash"]=BDirectAttackInput.Digest(packet).Substring(7);
  var row=fixture["first"];var input=OrdinaryFiveStartInput.Read(start,"a","future-run");var stats=OrdinaryFiveCoreStatSnapshot.Read((JObject)row["packet"],(JObject)row["state"],(string)row["packet"]["packetHash"],"a","future-run",input.OriginHash);return (start,input,stats);
 }
 static object Factory(){var f=Sources();var type=typeof(BasicCombatSimulation).Assembly.GetType("LastRail.Battle.OrdinaryFiveSeraFactory");Assert.NotNull(type,"Original START native factory missing");return Activator.CreateInstance(type,new object[]{f.Input,f.Stats,500000L});}
 static BasicCombatSimulation Create(object factory)=>(BasicCombatSimulation)factory.GetType().GetMethod("Create").Invoke(factory,null);
 public static JObject Cast()=>new JObject{["command"]=new JObject{["kind"]="CAST",["eventId"]="cast-one",["actionId"]="heal-one",["atMs"]=0,["sourceId"]="op_sera",["abilityId"]="op_sera.active"},["sequence"]=1,["effectSequence"]=2};
 [Test] public void OriginalFiveNativeHpAndPendingHealSurviveRestore(){var f=Factory();var original=Sources();var model=Create(f);foreach(var a in original.Input.Actors)Assert.AreEqual(original.Stats.Actor(a.OperatorId).CurrentHp,model.GetHp(a.OperatorId));model.ScheduleResolvedSeraCommand(Cast());model.Advance(2000000,20);var saved=model.CaptureLocalState();var restored=BasicCombatSimulation.RestoreLocalState(saved,()=>Create(f));model.Advance(2000000,20);restored.Advance(2000000,20);Assert.AreEqual(model.CaptureLocalState(),restored.CaptureLocalState());Assert.AreEqual(2,model.ReadSeraEmissions().Count);Assert.AreEqual("APPLIED",(string)model.ReadSeraEmissions().Last()["receipt"]["status"]);}
 [Test] public void OrdinaryFactoryDoesNotExposeLocalHpObservationCommand(){var m=Create(Factory());var request=new JObject{["command"]=new JObject{["kind"]="OBSERVE_DAMAGE_HP",["eventId"]="d",["actionId"]="d",["atMs"]=0,["actorId"]="op_kyle",["hp"]=0},["sequence"]=1,["effectSequence"]=null};Assert.Throws<InvalidOperationException>(()=>m.ScheduleResolvedSeraCommand(request));}
}
