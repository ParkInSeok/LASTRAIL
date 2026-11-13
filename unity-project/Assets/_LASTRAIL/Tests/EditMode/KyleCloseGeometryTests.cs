using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public sealed class KyleCloseGeometryTests {
 [Test] public void NearParallelQaStillPreservesOriginalKyleSameCarRestriction(){
  var input=OrdinaryLiveExecutionTests.CreateInput("D:/Project/LASTRAIL/unity-project/Assets/_LASTRAIL/Content/Battle/OrdinaryKyleCloseV01/definition.json");
  var factory=OrdinaryLiveExecutionTests.CreateFactory(input);Assert.AreEqual(18000,factory.Original.Basic.Profiles.Single(p=>p.OperatorId=="op_kyle").MaximumDistanceMm);
  Assert.AreEqual(0,factory.Original.Basic.Profiles.Single(p=>p.OperatorId=="op_kyle").AdjacentCars);
  var geometry=JObject.Parse((string)input["geometryDefinition"]);var layout=geometry["source"]["liveLayout"];double square=Enumerable.Range(0,3).Sum(i=>System.Math.Pow((double)layout["Gunner0"]["originMm"][i]-(double)layout["op_kyle"]["originMm"][i],2));Assert.Less(square,18000d*18000d);
  var driver=CombatSceneDriver.ForOrdinaryNode(factory,()=>true);driver.Open();Assert.IsTrue(driver.IsExecutionReady);
  bool launched=false,hit=false;for(int batch=0;batch<40&&!hit;batch++){
   driver.Advance(5100000,30);var state=JObject.Parse(driver.StateJson);
   foreach(var shot in (JArray)state["projectiles"]["shots"]){if((string)shot["actorId"]!="op_kyle")continue;launched=true;if((string)shot["status"]=="HIT"&&(long?)shot["result"]?["receipt"]?["hpLoss"]>0)hit=true;}
  }
  Assert.Greater(driver.Tick,600);Assert.IsFalse(launched,"Near distance cannot bypass the original same-car restriction.");Assert.IsFalse(hit,"An ineligible target must not receive a fabricated damage receipt.");
 }
}
