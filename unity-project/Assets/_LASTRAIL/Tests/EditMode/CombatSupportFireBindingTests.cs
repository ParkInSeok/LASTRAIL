using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
public class CombatSupportFireBindingTests {
 static BasicCombatSimulation Fresh(){var p=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-frontline-policy-UtU7yJ/prepared.json"));var c=new Ce03CommandContract((JObject)p["commandContract"]);return c.CreateLocalTest(p,(string)p["ownedSelection"]["accountId"],c.Revision,1);}
 [Test] public void ApprovedRuntimeUsesThreeVisualMissilesForOneActualSkillEffect(){
  var asset=LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved");var field=typeof(CombatPresentationAssets).GetField("SupportFire");Assert.IsNotNull(field);Assert.IsNotNull(field.GetValue(asset));
  var d=new CombatSceneDriver(Fresh,()=>null,(s,r)=>r+1,()=>true);d.Open();var reference=Fresh();var go=new GameObject("missile-test",typeof(RectTransform),typeof(Canvas));
  try{var view=go.AddComponent<ApprovedCombatView>();view.Bind(d,asset);var component=go.GetComponent("CombatSupportFireView");Assert.IsNotNull(component);d.Advance(6000000,12);reference.Advance(6000000,12);view.Refresh();
   var button=go.GetComponentsInChildren<Button>().Single(b=>b.name=="Command1");Assert.IsTrue(button.interactable);button.onClick.Invoke();var command=((JArray)JObject.Parse(d.StateJson)["commands"]).Single();reference.RequestSkill((string)command["id"],"train_a","support_fire");int maxRockets=0;
   for(int i=0;i<50;i++){d.Advance(500000,1);reference.Advance(500000,1);view.Refresh();Assert.AreEqual(reference.CaptureLocalState(),d.StateJson);var points=(JArray)component.GetType().GetProperty("FlightPoints").GetValue(component);maxRockets=Math.Max(maxRockets,points.Count(p=>(bool)p["visible"]));}
   Assert.AreEqual(3,maxRockets);Assert.AreEqual(1,(int)component.GetType().GetProperty("ImpactReceipts").GetValue(component));Assert.AreEqual(1,((JArray)JObject.Parse(d.StateJson)["effectJournal"]["rows"]).Count(r=>(string)r["abilityId"]=="support_fire"&&(string)r["kind"]=="DAMAGE"));
  }finally{UnityEngine.Object.DestroyImmediate(go);}
 }
}
