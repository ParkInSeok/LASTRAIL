using System;
using System.IO;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public sealed class OrdinaryTrainSourceTests {
 static JObject Export()=>JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/OperatorInformation-20261019/TrainSource772/consumed-export.json"));
 static OrdinaryFiveStartInput Read(JObject start)=>OrdinaryFiveStartInput.Read(start,"a",(string)start["runId"]);
 [TestCase("maxHp")][TestCase("accountId")][TestCase("loadoutRevision")][TestCase("ownedSourceHash")]
 public void RehashedTrainSourceMustMatchOriginalOwnershipAndDefinition(string field){var start=(JObject)Export()["originalStart"];var source=start["ordinaryTrainSource"];Assert.NotNull(source);var packet=source["packet"];if(field=="maxHp")packet["permanentStats"][field]=1;else if(field=="loadoutRevision")packet[field]=999;else packet[field]="different";source["sourceHash"]=BDirectAttackInput.Digest(packet).Substring(7);Assert.Throws<ArgumentException>(()=>Read(start));}
 [Test] public void InitializationTrainBindingCannotReferenceAnotherSource(){var export=Export();var response=export["initializationResponse"];response["initializationReceipt"]["ordinaryTrainBinding"]["sourceHash"]=new string('0',64);Assert.Throws<FormatException>(()=>OrdinaryInitializationReceipt.Validate(response.ToString(),Read((JObject)export["originalStart"])));}
 [TestCase(1,5000,60)][TestCase(10,8600,92)] public void FrozenPermanentGrowthDoesNotSupplyCurrentHp(int level,int maxHp,int attack){var export=Export();var start=(JObject)export["originalStart"];var source=start["ordinaryTrainSource"];source["packet"]["train"]["level"]=level;source["packet"]["permanentStats"]["maxHp"]=maxHp;source["packet"]["permanentStats"]["attack"]=attack;source["sourceHash"]=BDirectAttackInput.Digest(source["packet"]).Substring(7);var train=Read(start).Train;Assert.AreEqual(maxHp,train.PermanentMaxHp);Assert.AreEqual(attack,train.PermanentAttack);Assert.AreEqual(111,train.PermanentDefense);Assert.AreEqual(1000,train.BasicIntervalMs);}
 [Test] public void OriginalInitializationConsumesTrainBindingWithoutAddingHealthTarget(){var export=Export();var start=Read((JObject)export["originalStart"]);var response=export["initializationResponse"];Assert.NotNull(start.Train);Assert.NotNull(OrdinaryInitializationReceipt.Validate(response.ToString(),start));Assert.AreEqual(5,((JArray)response["initializationReceipt"]["health"]["state"]["targets"]).Count);}
 [Test] public void MissingHistoricalFieldsRemainAbsentAndExplicitNullIsRejected(){var export=Export();var start=(JObject)export["originalStart"];var response=(JObject)export["initializationResponse"];start.Remove("ordinaryTrainSource");Assert.IsNull(Read(start).Train);Assert.Throws<FormatException>(()=>OrdinaryInitializationReceipt.Validate(response.ToString(),Read(start)));((JObject)response["initializationReceipt"]).Remove("ordinaryTrainBinding");Assert.NotNull(OrdinaryInitializationReceipt.Validate(response.ToString(),Read(start)));start["ordinaryTrainSource"]=JValue.CreateNull();Assert.Throws<ArgumentException>(()=>Read(start));}
}
