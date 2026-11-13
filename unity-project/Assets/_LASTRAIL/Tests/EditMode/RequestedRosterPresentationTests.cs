#if UNITY_EDITOR
using System.Linq;
using LastRail.Battle;
using NUnit.Framework;
using UnityEditor;
using System.IO;
using UnityEngine;
namespace LastRail.Tests.EditMode {
 public class RequestedRosterPresentationTests {
  [Test] public void RightFacingSupplyPreservesOtherOperatorsAndOriginalPortrait(){
   var original=AssetDatabase.LoadAssetAtPath<CombatOperatorFrames>("Assets/_LASTRAIL/Content/Battle/Approved821/RequestedRoster01.asset");
   var right=AssetDatabase.LoadAssetAtPath<CombatOperatorFrames>("Assets/_LASTRAIL/Content/Battle/Approved821/RequestedRosterRight01.asset");
   Assert.NotNull(right);CollectionAssert.AreEqual(original.Entries.Select(e=>e.OperatorId),right.Entries.Select(e=>e.OperatorId));
   Assert.AreSame(original.Find("op_reina").Portrait,right.Find("op_reina").Portrait);
   foreach(var e in right.Entries){
    if(e.OperatorId!="op_reina")CollectionAssert.AreEqual(original.Find(e.OperatorId).IdleFrames,e.IdleFrames);
    foreach(var frame in e.IdleFrames.Distinct()){
     var readable=new Texture2D(2,2);try{Assert.IsTrue(readable.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(frame))));Assert.Greater(readable.GetPixel((int)e.FootAnchorTopLeft.x,readable.height-1-(int)e.FootAnchorTopLeft.y).a,.5f);}finally{Object.DestroyImmediate(readable);}
    }
   }
   Assert.AreEqual(480,right.Find("op_reina").DurationsMs.Sum());
   StringAssert.Contains("op_reina-right-idle",AssetDatabase.GetAssetPath(right.Find("op_reina").IdleFrames[0]));
  }
  [Test] public void RequestedFiveUseTheirOwnRegisteredBodiesAndPortraitsInDisplayOrder(){
   var frames=AssetDatabase.LoadAssetAtPath<CombatOperatorFrames>("Assets/_LASTRAIL/Content/Battle/Approved821/RequestedRoster01.asset");
   Assert.NotNull(frames,"Requested roster must have its own presentation supply, without changing the live START.");
   CollectionAssert.AreEqual(new[]{"op_reina","op_kyle","op_eve","op_sera","op_nyx"},frames.Entries.Select(e=>e.OperatorId));
   Assert.DoesNotThrow(frames.Validate);
   foreach(var e in frames.Entries){Assert.IsTrue(e.HasIdle,e.OperatorId);Assert.NotNull(e.Portrait);Assert.Greater(e.FootAnchorTopLeft.y,0);}
   var nyx=frames.Find("op_nyx");Assert.AreEqual(12,nyx.IdleFrames.Length);Assert.AreEqual(2160,nyx.DurationsMs.Sum());
   Assert.AreSame(nyx.IdleFrames[0],nyx.FrameAt(2160));Assert.IsNull(frames.Find("op_mira"));
  }
  [Test] public void EverySuppliedExposureHasOpaquePixelsAtTheRegisteredBoot(){
   var frames=AssetDatabase.LoadAssetAtPath<CombatOperatorFrames>("Assets/_LASTRAIL/Content/Battle/Approved821/RequestedRoster01.asset");Assert.NotNull(frames);
   foreach(var e in frames.Entries)foreach(var image in e.IdleFrames.Distinct()){
    var readable=new Texture2D(2,2);try{Assert.IsTrue(readable.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(image))));
     var alpha=readable.GetPixel(Mathf.RoundToInt(e.FootAnchorTopLeft.x),readable.height-1-Mathf.RoundToInt(e.FootAnchorTopLeft.y)).a;
     Assert.Greater(alpha,.5f,e.OperatorId+" registered support must touch the supplied boot, not transparent margin");
    }finally{Object.DestroyImmediate(readable);}
   }
  }
 }
}
#endif
