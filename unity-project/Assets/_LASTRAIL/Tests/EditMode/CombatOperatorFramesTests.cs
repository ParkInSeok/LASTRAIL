#if UNITY_EDITOR
using System;
using LastRail.Battle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
namespace LastRail.Tests.EditMode {
 public class CombatOperatorFramesTests {
  CombatOperatorFrames registry;
  Texture2D first, second;
  [SetUp] public void SetUp(){
   registry=ScriptableObject.CreateInstance<CombatOperatorFrames>();
   first=new Texture2D(10,20);second=new Texture2D(10,20);
   registry.Entries=new[]{new CombatOperatorFrames.Entry{OperatorId="op_reina",CanvasSize=new Vector2(10,20),FootAnchorTopLeft=new Vector2(7,18),BodySpan=16,IdleFrames=new[]{first,second},DurationsMs=new[]{120,340}}};
  }
  [TearDown] public void TearDown(){UnityEngine.Object.DestroyImmediate(registry);UnityEngine.Object.DestroyImmediate(first);UnityEngine.Object.DestroyImmediate(second);}
  [TestCase(0,0)][TestCase(119.99,0)][TestCase(120,1)][TestCase(459.99,1)][TestCase(460,0)][TestCase(1040,1)]
  public void TimingUsesOriginalExposureBoundariesAndWrap(double milliseconds,int expected){registry.Validate();Assert.AreSame(expected==0?first:second,registry.Find("op_reina").FrameAt(milliseconds));}
  [Test] public void EveryFrameKeepsSameAuthoredSupportAtOriginalRootAnchor(){
   registry.Validate();var entry=registry.Find("op_reina");
   var size=entry.SizeForHeight(155);var top=entry.TopLeftForHeight(155);
   foreach(double t in new[]{0d,120d,460d}){
    Assert.NotNull(entry.FrameAt(t));
    var sole=top+new Vector2(size.x*7/10,-size.y*18/20);
    Assert.That(sole.x,Is.EqualTo(75).Within(.001));Assert.That(sole.y,Is.EqualTo(-160).Within(.001));
   }
  }
  [TestCase("duration-count")][TestCase("zero-duration")][TestCase("null-texture")][TestCase("canvas")][TestCase("foot")][TestCase("body")][TestCase("duplicate")]
  public void InvalidSupplyIsRejectedBeforeRendering(string failure){
   var e=registry.Entries[0];switch(failure){
    case "duration-count":e.DurationsMs=new[]{120};break;
    case "zero-duration":e.DurationsMs[0]=0;break;
    case "null-texture":e.IdleFrames[0]=null;break;
    case "canvas":e.CanvasSize=new Vector2(11,20);break;
    case "foot":e.FootAnchorTopLeft=new Vector2(float.NaN,18);break;
    case "body":e.BodySpan=0;break;
    case "duplicate":registry.Entries=new[]{e,e};break;
   }
   Assert.Throws<InvalidOperationException>(()=>registry.Validate());
  }
  [Test] public void PortraitOnlyEntryDoesNotClaimBodySupplyAndBorrowsTexture(){
   registry.Entries=new[]{new CombatOperatorFrames.Entry{OperatorId="op_mira",Portrait=first,PortraitUv=new Rect(0,0,1,1)}};
   registry.Validate();Assert.IsFalse(registry.Find("op_mira").HasIdle);Assert.IsNull(registry.Find("unknown"));
   UnityEngine.Object.DestroyImmediate(registry);registry=null;Assert.IsTrue(first!=null,"Shared source texture must not be destroyed with the registry");
  }
  [Test] public void StaticApprovedBodyStaysOneFrameAtAnyPresentationTime(){
   var e=registry.Entries[0];e.IdleFrames=new[]{first};e.DurationsMs=new[]{1000};registry.Validate();
   Assert.AreSame(first,e.FrameAt(0));Assert.AreSame(first,e.FrameAt(987654321));
  }
  [Test] public void ActiveUsesAuthoredClockAndStopsWithoutLooping(){
   var e=registry.Entries[0];e.ActiveFrames=new[]{first,second};e.ActiveDurationsMs=new[]{400,2600};registry.Validate();
   Assert.AreEqual(3000,e.ActiveDurationMs);Assert.AreSame(first,e.ActiveFrameAt(399.99));Assert.AreSame(second,e.ActiveFrameAt(400));Assert.AreSame(second,e.ActiveFrameAt(2999.99));Assert.IsNull(e.ActiveFrameAt(3000));Assert.IsNull(e.ActiveFrameAt(6000));
   Assert.Throws<ArgumentOutOfRangeException>(()=>e.ActiveFrameAt(double.NaN));Assert.Throws<ArgumentOutOfRangeException>(()=>e.ActiveFrameAt(-1));
  }
  [TestCase("count")][TestCase("duration")][TestCase("texture")]
  public void InvalidActiveSupplyIsRejected(string failure){
   var e=registry.Entries[0];e.ActiveFrames=new[]{first};e.ActiveDurationsMs=new[]{400};
   if(failure=="count")e.ActiveDurationsMs=Array.Empty<int>();else if(failure=="duration")e.ActiveDurationsMs[0]=0;else e.ActiveFrames[0]=null;
   Assert.Throws<InvalidOperationException>(()=>registry.Validate());
  }
  [Test] public void ActualViewRendersBorrowedFramesAtFixedFootWithoutInstallingLegacyRig(){
   registry.Validate();var go=new GameObject("frame-view-test",typeof(RectTransform));go.SetActive(false);
   var assets=ScriptableObject.CreateInstance<CombatPresentationAssets>();assets.OperatorFrames=registry;
   try{
    var view=go.AddComponent<ApprovedCombatView>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
    typeof(ApprovedCombatView).GetField("assets",flags).SetValue(view,assets);
    var unitType=typeof(ApprovedCombatView).GetNestedType("Unit",BindingFlags.NonPublic);var unit=Activator.CreateInstance(unitType,true);
    var artNode=new GameObject("source",typeof(RectTransform),typeof(RawImage));artNode.transform.SetParent(go.transform,false);var art=artNode.GetComponent<RawImage>();artNode.SetActive(false);
    var meterNode=new GameObject("health",typeof(RectTransform));meterNode.transform.SetParent(go.transform,false);
    unitType.GetField("Art").SetValue(unit,art);unitType.GetField("Root").SetValue(unit,go.GetComponent<RectTransform>());unitType.GetField("Hp").SetValue(unit,meterNode.AddComponent<CombatMeter>());unitType.GetField("Visual").SetValue(unit,"op_reina");
    var apply=typeof(ApprovedCombatView).GetMethod("ApplyApprovedIdle",flags);
    Assert.AreEqual(true,apply.Invoke(view,new object[]{unit,0d,null,0L,null,50d}));var position=art.rectTransform.anchoredPosition;
    Assert.AreSame(first,art.texture);Assert.IsTrue(artNode.activeSelf);
    Assert.AreEqual(true,apply.Invoke(view,new object[]{unit,120d,null,0L,null,50d}));Assert.AreSame(second,art.texture);Assert.AreEqual(position,art.rectTransform.anchoredPosition);
    Assert.AreEqual(new Rect(0,0,1,1),art.uvRect);Assert.AreEqual(Vector3.one,art.rectTransform.localScale);
    Assert.DoesNotThrow(()=>view.SetCaptureOperatorRig("op_reina",null,null));Assert.AreEqual(0,view.CaptureRigTriangles);
   }finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(assets);}
   Assert.IsTrue(first!=null&&second!=null,"View teardown must not unload borrowed source images");
  }
 }
}
#endif
