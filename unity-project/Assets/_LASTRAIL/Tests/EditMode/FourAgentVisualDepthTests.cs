using System.Linq;
using LastRail.Presentation.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Tests.EditMode
{
 public sealed class FourAgentVisualDepthTests
 {
  static JObject Layer(string role,int slot,int order=0)=>new JObject{["kind"]="rect",["role"]=role,["slot"]=slot,["order"]=order,["x"]=20,["y"]=30,["w"]=10,["h"]=10,["angle"]=0,["color"]=new JArray(1,1,1,1)};
  static JObject Frame(int direction,JArray layers)=>new JObject{["version"]="unity.five-agent-reference-frame.v1.20261017",["direction"]=direction,["layers"]=layers};
  [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)]
  public void ActualViewPlacesEveryForegroundAfterAllBodies(int direction)
  {
   var go=new GameObject("depth",typeof(RectTransform),typeof(FiveAgentVisualView));var assets=ScriptableObject.CreateInstance<FiveAgentVisualAssets>();
   try{
    var view=go.GetComponent<FiveAgentVisualView>();view.Configure(assets);
    var layers=new JArray(Layer("beam",3),Layer("projectile",0),Layer("drone",2),Layer("actor",0,10),Layer("actor",1,12),Layer("muzzle",1,13),Layer("actor",2,14),Layer("smoke",2,15),Layer("actor",3,16),Layer("impact",0),Layer("glove",2));
    var f=Frame(direction,layers);string original=f.ToString();view.Present(f);
    var graphics=go.GetComponentsInChildren<Graphic>();
    int lastBody=graphics.Where(g=>g.name.StartsWith("actor_")||g.name.StartsWith("drone_")||g.name.StartsWith("smoke_")).Max(g=>g.transform.GetSiblingIndex());
    foreach(var g in graphics.Where(g=>g.name.StartsWith("beam_")||g.name.StartsWith("projectile_")||g.name.StartsWith("muzzle_")||g.name.StartsWith("impact_")||g.name.StartsWith("glove_")))Assert.That(g.transform.GetSiblingIndex(),Is.GreaterThan(lastBody),g.name);
    var actor=graphics.Single(g=>g.name=="actor_slot2");var smoke=graphics.Single(g=>g.name=="smoke_slot2");
    Assert.That(smoke.transform.GetSiblingIndex()<actor.transform.GetSiblingIndex(),Is.EqualTo(direction==3));
    Assert.That(graphics.All(g=>!g.raycastTarget),Is.True);Assert.That(f.ToString(),Is.EqualTo(original));
    view.Present(Frame(direction,new JArray(Layer("actor",2,14))));Assert.That(view.VisibleLayers,Is.EqualTo(1));Assert.That(go.GetComponentsInChildren<Graphic>().Length,Is.EqualTo(1));
   }finally{Object.DestroyImmediate(go);Object.DestroyImmediate(assets);}
  }
  [Test] public void SameBeamGlowCoreOrderRemainsStable()
  {
   var go=new GameObject("depth",typeof(RectTransform),typeof(FiveAgentVisualView));var assets=ScriptableObject.CreateInstance<FiveAgentVisualAssets>();
   try{var view=go.GetComponent<FiveAgentVisualView>();view.Configure(assets);var outer=Layer("beam",3);outer["w"]=5;var core=Layer("beam",3);core["w"]=1;view.Present(Frame(3,new JArray(outer,core,Layer("actor",0,10))));var beam=go.GetComponentsInChildren<Graphic>().Where(g=>g.name=="beam_slot3").ToArray();Assert.That(beam[0].rectTransform.sizeDelta.x,Is.EqualTo(5));Assert.That(beam[1].rectTransform.sizeDelta.x,Is.EqualTo(1));Assert.That(beam[0].transform.GetSiblingIndex(),Is.GreaterThan(go.transform.Find("actor_slot0").GetSiblingIndex()));}finally{Object.DestroyImmediate(go);Object.DestroyImmediate(assets);}
  }
  [Test] public void MixedDirectionsUseEachActorsSmokeSide()
  {
   var a=Layer("actor",0,10);var b=Layer("actor",1,10);var front=Layer("smoke",0,15);front["direction"]=0;var back=Layer("smoke",1,15);back["direction"]=3;
   var ordered=FourAgentVisualDepth.Order(new JArray(a,b,front,back),0).ToList();
   Assert.That(ordered.IndexOf(front),Is.GreaterThan(ordered.IndexOf(a)));Assert.That(ordered.IndexOf(back),Is.LessThan(ordered.IndexOf(b)));
  }
 }
}
