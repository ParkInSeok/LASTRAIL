using System;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class CombatRailWorldTests {
 [TestCase(false)][TestCase(true)] public void ActualViewBuildsEverySelectedCarriage(bool four){
  var p=new Ce03RailPreparation(CombatRailPreparationTests.Golden(four),"a",1);var go=new UnityEngine.GameObject("rail-world-test",typeof(UnityEngine.RectTransform),typeof(UnityEngine.Canvas));
  try{var d=new CombatSceneDriver(()=>p.CreateModel(1),()=>null,(s,r)=>r+1,()=>true);d.Open();go.AddComponent<ApprovedCombatView>().Bind(d,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));var world=go.transform.Find("ApprovedBattle/ExteriorViewport/SelectedRailConsist");Assert.NotNull(world);foreach(var c in (JArray)d.HudState["railJourney"]["carriages"])Assert.NotNull(world.Find((string)c["id"]));Assert.NotNull(world.Find("Locomotive/Body"));Assert.NotNull(world.Find("ActualRouteTrack"));}
  finally{UnityEngine.Object.DestroyImmediate(go);}
 }
 [TestCase(false,2)][TestCase(true,4)] public void ConsistUsesVerifiedSlotsTypesAndIndependentCarriages(bool four,int count){var c=CombatRailPreparationTests.Golden(four);var type=typeof(ApprovedCombatView).Assembly.GetType("LastRail.Battle.CombatRailWorldLayout");Assert.NotNull(type);var cars=(JArray)type.GetMethod("BuildConsist").Invoke(null,new object[]{c["railJourney"]["carriages"]});Assert.AreEqual(count,cars.Count);for(int i=0;i<count;i++){Assert.AreEqual((string)c["railJourney"]["carriages"][i]["id"],(string)cars[i]["id"]);Assert.AreEqual((string)c["railJourney"]["carriages"][i]["type"],(string)cars[i]["type"]);Assert.GreaterOrEqual((float)cars[i]["x"],0);if(i>0)Assert.Less((float)cars[i]["x"],(float)cars[i-1]["x"]);}}
 [Test] public void RouteMovementUsesDistanceAndCarriageLag(){var type=typeof(ApprovedCombatView).Assembly.GetType("LastRail.Battle.CombatRailWorldLayout");Assert.NotNull(type);var method=type.GetMethod("RouteOffset");float At(long d,int car)=>(float)method.Invoke(null,new object[]{d,100000L,car,1});Assert.AreEqual(0,At(100000,0));Assert.Greater(At(110000,0),At(110000,2));Assert.That(At(1000000,3),Is.EqualTo(18).Within(.01));}
}
