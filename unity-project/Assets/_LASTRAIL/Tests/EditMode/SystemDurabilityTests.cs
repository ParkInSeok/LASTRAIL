using System.Linq;
using LastRail.Battle;
using NUnit.Framework;
using UnityEngine;
namespace LastRail.Tests.EditMode
{
 public sealed class SystemDurabilityTests
 {
  [Test] public void ActualHudHasThreeUnavailableSystemsWithoutChangingTrainOwner()
  {
   int writes=0;var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>{writes++;return r+1;},()=>true);driver.Open();
   var go=new GameObject("SystemHudQA",typeof(RectTransform),typeof(Canvas));
   try{
    string before=driver.StateJson;int saved=writes;
    var view=go.AddComponent<ApprovedCombatView>();var art=LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved");view.Bind(driver,art);view.UseLandscapeLayout();view.BindOrRefresh(driver,art);view.BindOrRefresh(driver,art);
    var meters=go.GetComponentsInChildren<CombatMeter>(true);
    Assert.AreEqual(3,meters.Count(m=>m.name.StartsWith("SystemDurability")));Assert.IsTrue(meters.Where(m=>m.name.StartsWith("SystemDurability")).All(m=>m.Value==0));Assert.IsFalse(meters.Any(m=>m.name.StartsWith("CarriageHp")));
    var labels=go.GetComponentsInChildren<TMPro.TMP_Text>(true);Assert.IsTrue(labels.Any(t=>t.name=="ShieldAmount"));Assert.AreEqual(3,labels.Count(t=>t.name.StartsWith("SystemDurabilityLabel")));Assert.AreEqual(3,labels.Count(t=>t.name.StartsWith("SystemDurabilityValue")&&t.text=="미연결"));
    Assert.AreEqual(before,driver.StateJson);Assert.AreEqual(saved,writes);Assert.IsNull(typeof(BCombatLocalModel).GetMethod("ReadCarriageHealth"));
    // This retained legacy fixture has no modern five-card owner. Its unresolved cut-in must stay blank.
    var kyle=go.GetComponentsInChildren<UnityEngine.UI.RawImage>(true).Where(i=>i.name=="UnconnectedKyleCutinPortrait").ToArray();Assert.IsNotEmpty(kyle);Assert.IsTrue(kyle.All(i=>i.texture==null&&!i.enabled));
    Assert.IsFalse(go.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b=>b.name=="ConfirmRailRoute"));
    var lever=go.GetComponentInChildren<CombatBranchLever>();Assert.IsNotNull(lever.Commit);Assert.AreEqual(82,((RectTransform)lever.handleRect.parent).rect.width,.01f);Assert.AreEqual(77,lever.targetGraphic.rectTransform.rect.width,.01f);
   }finally{UnityEngine.Object.DestroyImmediate(go);}
  }
  [TestCase(1600,900)] [TestCase(2100,900)] [TestCase(1200,900)]
  public void SharedGaugeBoundsSurviveFitRebindDisplayChangesAndReentry(int width,int height)
  {
   int writes=0;var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>{writes++;return r+1;},()=>true);driver.Open();
   string before=driver.StateJson;int saved=writes;
   var art=LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved");
   for(int entry=0;entry<2;entry++)
   {
    var go=new GameObject("DurabilityLifecycleQA",typeof(RectTransform));
    try{
     var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,art);view.UseLandscapeLayout();
     view.BindOrRefresh(driver,art);view.BindOrRefresh(driver,art);
     go.GetComponent<CombatSafeArea>().enabled=false;
     var root=(RectTransform)go.transform.Find("ApprovedBattle");
     root.localScale=Vector3.one*(CombatSafeArea.FitDesign(new Rect(0,0,width,height),new Vector2(1600,900)).width/1600f);
     var meters=go.GetComponentsInChildren<CombatMeter>(true);
     var train=meters.Single(m=>m.name=="TrainHp");
     var systems=meters.Where(m=>m.name.StartsWith("SystemDurability")).OrderBy(m=>m.name).ToArray();
     Assert.AreEqual(3,systems.Length);
     Assert.IsTrue(systems.All(m=>m.transform.parent==train.transform.parent&&m.CyanBevel&&!m.RoundedEnds));
     var a=new Vector3[4];var b=new Vector3[4];var c=new Vector3[4];
     train.rectTransform.GetWorldCorners(a);systems[0].rectTransform.GetWorldCorners(b);systems[2].rectTransform.GetWorldCorners(c);
     Assert.AreEqual(a[0].x,b[0].x,.0001f);Assert.AreEqual(a[2].x,c[2].x,.0001f);
     var icon=go.GetComponentsInChildren<CombatSystemSymbol>(true).First();
     icon.SetLayerVisible("Symbol",false);icon.SetLayerRenderer("Symbol",(mesh,rect,tint)=>mesh.Clear());
     systems[0].SetLayerVisible("Frame",false);systems[0].SetLayerRenderer("Fill",(mesh,rect,tint)=>mesh.Clear());systems[0].SetValue(.75f);
     var label=go.GetComponentsInChildren<TMPro.TMP_Text>(true).Single(t=>t.name=="SystemDurabilityValue0");label.text="표시 시험";label.enabled=false;
     Assert.IsTrue(systems[1].GetLayer("Frame").enabled);Assert.AreEqual(0,systems[1].Value);Assert.AreEqual(0,systems[2].Value);
     Assert.IsTrue(systems[0].GetLayer("Track").enabled);Assert.AreEqual(.75f,systems[0].Value);
     icon.SetLayerRenderer("Symbol",null);icon.SetLayerVisible("Symbol",true);systems[0].SetLayerRenderer("Fill",null);systems[0].SetLayerVisible("Frame",true);systems[0].SetValue(0);
     go.SetActive(false);go.SetActive(true);view.BindOrRefresh(driver,art);
     Assert.AreEqual(3,go.GetComponentsInChildren<CombatSystemSymbol>(true).Length);
    }finally{Object.DestroyImmediate(go);}
    Assert.AreEqual(before,driver.StateJson);Assert.AreEqual(saved,writes);
   }
  }
 }
}
