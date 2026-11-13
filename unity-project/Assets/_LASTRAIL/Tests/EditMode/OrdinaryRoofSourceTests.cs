using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public sealed class OrdinaryRoofSourceTests {
 static JObject Export()=>JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/OperatorInformation-20261019/PlayerBudget798/source.json"));
 static OrdinaryFiveStartInput Read(JObject e)=>OrdinaryFiveStartInput.Read((JObject)e["originalStart"],(string)e["accountId"],(string)e["runId"]);
 [Test] public void OriginalFiveBindMeasuredAnchorsAndRestoreSameCheckpoint(){
  var e=Export();var s=Read(e);var anchors=JObject.Parse(File.ReadAllText("Assets/_LASTRAIL/Content/Battle/OrdinaryRoofSpawnV01/definition.json"))["slots"];
  Assert.AreEqual(5,s.Roof.Slots.Count);
  for(int i=0;i<5;i++){var slot=s.Roof.Slots[i];Assert.AreEqual(s.Actors[i].OperatorId,slot.OperatorId);Assert.AreEqual((int)anchors[i]["vehicleOrder"],slot.PhysicalOrder);Assert.AreEqual((int)anchors[i]["localPositionMm"][0],slot.LocalXmm);Assert.AreEqual(s.Rail.PhysicalInstanceIds[slot.PhysicalOrder],slot.PhysicalInstanceId);}
  var f=OrdinaryFiveSeraFactory.FromInitialization(s,e["initializationResponse"].ToString(),500000);var snapshot=f.Create().CaptureLocalState();Assert.AreEqual(snapshot,BasicCombatSimulation.RestoreLocalState(snapshot,f.Create).CaptureLocalState());
 }
 [TestCase("order"),TestCase("duplicate"),TestCase("rail"),TestCase("definition"),TestCase("status")]
 public void RehashedConflictsReject(string kind){var e=Export();var source=e["originalStart"]["ordinaryRoofSource"];var p=source["packet"];
  if(kind=="order"){var ids=(JArray)p["operatorIds"];var a=ids[0].DeepClone();ids[0]=ids[1].DeepClone();ids[1]=a;}
  if(kind=="duplicate")p["operatorIds"][1]=p["operatorIds"][0].DeepClone();
  if(kind=="rail")p["railSourceHash"]="foreign";
  if(kind=="definition")p["definitionVersion"]="unknown";
  if(kind=="status")p["status"]="READY";
  source["sourceHash"]=BDirectAttackInput.Digest(p).Substring(7);Assert.Throws<ArgumentException>(()=>Read(e));
 }
 [Test] public void MissingReceiptBindingRejects(){var e=Export();var s=Read(e);((JObject)e["initializationResponse"]["initializationReceipt"]).Remove("ordinaryRoofBinding");Assert.Throws<FormatException>(()=>OrdinaryInitializationReceipt.Validate(e["initializationResponse"].ToString(),s));}
 [Test] public void ExplicitNullRejects(){var e=Export();e["originalStart"]["ordinaryRoofSource"]=null;Assert.Catch(()=>Read(e));}
 [Test] public void HistoricalAbsenceWorksButCannotRestoreRoofBoundState(){var e=Export();var f=OrdinaryFiveSeraFactory.FromInitialization(Read(e),e["initializationResponse"].ToString(),500000);var snapshot=f.Create().CaptureLocalState();((JObject)e["originalStart"]).Remove("ordinaryRoofSource");((JObject)e["initializationResponse"]["initializationReceipt"]).Remove("ordinaryRoofBinding");var old=Read(e);Assert.IsNull(old.Roof);var oldFactory=OrdinaryFiveSeraFactory.FromInitialization(old,e["initializationResponse"].ToString(),500000);Assert.Catch(()=>BasicCombatSimulation.RestoreLocalState(snapshot,oldFactory.Create));}
}
