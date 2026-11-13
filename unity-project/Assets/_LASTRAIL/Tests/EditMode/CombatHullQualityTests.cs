using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class CombatHullQualityTests {
 [TestCase(5000)][TestCase(3127)][TestCase(1500)][TestCase(700)][TestCase(0)]
 public void HullUsesActualHealthAndSeparatesCurrentFromMaximum(int hp){
  var prep=new Ce03RailPreparation(CombatRailPreparationTests.Golden(),"a",1);
  var state=JObject.Parse(prep.CreateModel(1).CaptureLocalState());((JArray)state["actors"]).Single(a=>(string)a["id"]=="train_a")["vitals"]["hp"]=hp==0?1:hp;
  if(hp==0){var model=BasicCombatSimulation.RestoreLocalState(state.ToString(),()=>prep.CreateModel(1));for(int i=0;i<1000&&model.Outcome==BasicCombatOutcome.Running;i++)model.Advance(1000000,8);Assert.AreEqual(BasicCombatOutcome.Failure,model.Outcome);state=JObject.Parse(model.CaptureLocalState());}
  var d=new CombatSceneDriver(()=>BasicCombatSimulation.RestoreLocalState(state.ToString(),()=>prep.CreateModel(1)),()=>null,(s,r)=>r+1,()=>true);d.Open();
  var go=new GameObject("hull-test",typeof(RectTransform),typeof(Canvas));
  try{go.AddComponent<ApprovedCombatView>().Bind(d,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));var root=go.transform.Find("ApprovedBattle");
   var label=root.Find("TrainHpText").GetComponent<TMP_Text>();label.ForceMeshUpdate();Assert.AreEqual(hp+" / 5000",label.GetParsedText());Assert.That(label.text,Does.Contain("<color=#"));Assert.That(label.text,Does.Contain("<color=#858F91> / 5000</color>"));
   Assert.AreEqual(hp/5000f,root.Find("TrainHp").GetComponent<CombatMeter>().Value,.00001);
   Assert.NotNull(root.Find("HullNameplateWing"));Assert.NotNull(root.Find("HullIntegrityHousing/MachinedHullFrame"));Assert.NotNull(root.Find("HullNameplate/MachinedHullFrame"));
   Assert.AreEqual(state.ToString(Newtonsoft.Json.Formatting.None),JObject.Parse(d.StateJson).ToString(Newtonsoft.Json.Formatting.None));
  }finally{Object.DestroyImmediate(go);}
 }
}
