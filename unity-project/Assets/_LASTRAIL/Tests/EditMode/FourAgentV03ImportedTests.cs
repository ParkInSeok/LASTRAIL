#if UNITY_EDITOR
using System;
using System.Linq;
using LastRail.Presentation.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace LastRail.Tests.EditMode {
 public sealed class FourAgentV03ImportedTests {
  static FourAgentV03Assets Assets(){var a=AssetDatabase.LoadAssetAtPath<FourAgentV03Assets>("Assets/_LASTRAIL/UI/CombatAnimations/FourAgentV03/ApprovedFourAgentV03.asset");Assert.NotNull(a);return a;}
  [Test]public void AllImportedCanvasSizesAndClipBindingsPreserveOriginals(){var assets=Assets();var catalog=assets.CreateCatalog();Assert.That(catalog.Capture()["clips"].Count(),Is.EqualTo(32));foreach(var path in catalog.TexturePaths.Concat(FourAgentV03Effects.TexturePaths)){var t=assets.Texture(path);var s=assets.Size(path);Assert.That(t.width,Is.EqualTo((int)s.x),path);Assert.That(t.height,Is.EqualTo((int)s.y),path);}}
  [Test]public void AllDirectionsAndEffectBoundariesReachActualViewWithoutUnboundTextures(){var assets=Assets();var composer=new FourAgentV03FrameComposer(assets);var go=new GameObject("ImportedV03View",typeof(RectTransform),typeof(FiveAgentVisualView));try{var view=go.GetComponent<FiveAgentVisualView>();view.ConfigureV03(assets);foreach(var id in new[]{"op_kyle","op_reina","op_eve","op_sera"})for(int d=1;d<=4;d++)foreach(long age in new long[]{0,200,380,400,480,620,900,1080,1150,1300,1330,1420,1460,2160,2400}){var frame=composer.Compose(id,d,true,age,777,new Vector2(570,700),new Vector2(150,300));Assert.That(frame["layers"].Count(l=>(string)l["role"]=="actor"),Is.EqualTo(1));view.Present(frame);Assert.That(view.VisibleLayers,Is.GreaterThan(0));}}finally{UnityEngine.Object.DestroyImmediate(go);}}
  [TestCase("op_kyle")][TestCase("op_reina")]public void NoContactIsInferredFromVisualFlight(string id){var composer=new FourAgentV03FrameComposer(Assets());var frame=composer.Compose(id,1,true,800,0,Vector2.zero,Vector2.one);Assert.That(frame["layers"].Any(l=>((string)l["role"]).StartsWith("impact",StringComparison.Ordinal)),Is.False);var contact=composer.Compose(id,1,false,0,0,Vector2.zero,Vector2.one,contactAgeMs:20);Assert.That(contact["layers"].Any(l=>((string)l["role"]).StartsWith("impact",StringComparison.Ordinal)),Is.True);}
  [Test]public void MissingTargetSuppressesEveryAimedProjectileAndBeam(){var composer=new FourAgentV03FrameComposer(Assets());foreach(var id in new[]{"op_kyle","op_reina","op_eve","op_sera"})foreach(long age in new long[]{420,480,1150,1330}){var frame=composer.Compose(id,1,true,age,777,Vector2.zero,null);Assert.That(frame["layers"].Any(l=>new[]{"projectile","beam","impact"}.Contains((string)l["role"])),Is.False);}}
  [Test]public void KyleContactUsesApprovedScaleAroundSuppliedPoint(){var target=new Vector2(130,270);var frame=new FourAgentV03FrameComposer(Assets()).Compose("op_kyle",1,false,0,0,Vector2.zero,target,contactAgeMs:0);var glow=frame["layers"].Single(l=>(string)l["role"]=="impact");Assert.That((float)glow["x"],Is.EqualTo(target.x));Assert.That((float)glow["y"],Is.EqualTo(target.y));Assert.That((float)glow["w"],Is.EqualTo(17*.4f*1.3f*2*.9f).Within(.001));}
  [TestCase(1f)][TestCase(.5f)][TestCase(.8076514f)]public void SeraBeamKeepsFinalDisplayPixelWidths(float scale){var frame=new FourAgentV03FrameComposer(Assets()).Compose("op_sera",1,true,1150,777,Vector2.zero,new Vector2(200,300),referenceToDisplayScale:scale);var widths=frame["layers"].Where(l=>(string)l["role"]=="beam").Select(l=>(float)l["h"]*scale).ToArray();Assert.That(widths.Length,Is.EqualTo(9));for(int n=0;n<widths.Length;n++)Assert.That(widths[n],Is.EqualTo(new[]{5f,2.4f,1f}[n%3]).Within(.001));}
 }
}
#endif
