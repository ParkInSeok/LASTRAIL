#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
namespace LastRail.Tests.EditMode {
 public class FiveAgentVisualReferenceTests {
  public const string Source="D:/Project/LASTRAIL/QA/DirectionImpact-20260921/eve-combat-v01/five-agent-combat-fx-v02/";
  public static JObject FileJson(string name)=>JObject.Parse(File.ReadAllText(Source+name));
  public static Vector2 Size(string path){var b=File.ReadAllBytes(Source+path);int W(int i)=>(b[i]<<24)|(b[i+1]<<16)|(b[i+2]<<8)|b[i+3];return new Vector2(W(16),W(20));}
  public static Type Type(string name){var t=typeof(BDirectAttackInput).Assembly.GetType("LastRail.Presentation.Battle."+name);Assert.That(t,Is.Not.Null,name+" missing");return t;}
  public static object Call(object value,string method,params object[] args){try{return value.GetType().GetMethod(method).Invoke(value,args);}catch(TargetInvocationException e){throw e.InnerException;}}
  public static object Model(){var c=FileJson("unity-reference-contract.json");try{return Type("FiveAgentVisualReference").GetMethod("Read").Invoke(null,new object[]{c,FileJson("manifest.json"),FileJson("projectile.json"),BDirectAttackInput.Digest(c),(Func<string,Vector2>)Size});}catch(TargetInvocationException e){throw e.InnerException;}}
  public static JObject Sample(object m,int direction,long age,bool fx=true)=>(JObject)Call(m,"SampleReference",direction,age,fx);
  [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)]public void FourDirectionsKeepFiveFixedFootAnchorsAndExplicitComparisonCopies(int d){var s=Sample(Model(),d,1850);Assert.That((int)s["direction"],Is.EqualTo(d));Assert.That((string)s["targetKind"],Is.EqualTo(d==3?"FORWARD_OBSTACLE_REFERENCE":"ENEMY_TRAIN_REFERENCE"));Assert.That(s["actors"].Select(a=>(float)a["foot"][1]),Is.EqualTo(new[]{829f,918f,1005f,1101f,1190f}));Assert.That(s["actors"].Count(a=>(bool)a["placeholder"]),Is.EqualTo(2));Assert.That(s["actors"].All(a=>a["runtimeId"].Type==JTokenType.Null),Is.True);Assert.That((bool)s["gameplayAuthority"],Is.False);Assert.That(s["layers"].Any(a=>(string)a["path"]=="background.png"),Is.False);}
  [Test]public void DisabledFxPreservesActorPixelsAndDirectionNeverCyclesFrom6400Ms(){var m=Model();var on=Sample(m,3,1850);var off=Sample(m,3,1850,false);Assert.That(JToken.DeepEquals(on["actors"],off["actors"]),Is.True);Assert.That(on["layers"].Count(a=>(string)a["role"]=="beam"),Is.GreaterThan(0));Assert.That(off["layers"].Any(a=>(string)a["role"]=="beam"),Is.False);Assert.That((int)Sample(m,3,12801)["direction"],Is.EqualTo(3));}
  [Test]public void ActorExposureUsesManifestCoordinatesAndNativeBaselineRatherThanPerFrameCenter(){var m=Model();var s=Sample(m,0,1850);var manifest=FileJson("manifest.json");var layer=s["layers"].First(l=>(string)l["role"]=="actor"&&(int)l["slot"]==0);var asset=manifest["directions"][0]["kyle"][17];Assert.That((string)layer["path"],Is.EqualTo((string)asset["src"]));float x=(float)asset["x"]-1.412300424808791f;var size=Size((string)asset["src"]);Assert.That((float)layer["x"],Is.EqualTo(570+.9f*(x-550+size.x/2)).Within(.001));Assert.That((float)layer["y"],Is.EqualTo(829+.9f*((float)asset["y"]-577.7f+size.y/2)).Within(.001));}
  [Test]public void EveHasIndependentDroneChargeGloveAndReturnExposures(){var m=Model();var charge=Sample(m,0,1700);Assert.That(charge["layers"].Any(l=>(string)l["role"]=="charge"),Is.True);Assert.That(charge["layers"].Any(l=>(string)l["role"]=="glove"),Is.True);Assert.That(charge["layers"].Count(l=>(string)l["role"]=="drone"),Is.EqualTo(4));var ret=Sample(m,3,2500);Assert.That(ret["layers"].Single(l=>(string)l["role"]=="actor"&&(int)l["slot"]==2)["path"].Value<string>(),Is.EqualTo("eve4/mid1.png"));}
  [Test]public void GloveUsesOriginalClockAndStopsAtPostFireHold(){
   var m=Model();var lateCharge=Sample(m,0,1700);
   var glow=lateCharge["layers"].Single(l=>(string)l["role"]=="glove");
   Assert.That((float)glow["w"],Is.EqualTo(2*8*.75f*.9f).Within(.001));
   Assert.That((float)glow["color"][3],Is.EqualTo(.25f).Within(.001));
   Assert.That(Sample(m,0,1940)["layers"].Any(l=>((string)l["role"]).StartsWith("glove")),Is.False);
   Assert.That(Sample(m,0,1400)["layers"].Count(l=>(string)l["role"]=="gloveParticle"),Is.EqualTo(6));
  }
  [Test]public void OriginalChargeRingsAndRoundedBeamAreIndependentLayers(){
   var m=Model();var charge=Sample(m,3,1700);
   Assert.That(charge["layers"].Count(l=>(string)l["role"]=="chargeRing"),Is.EqualTo(2));
   Assert.That(charge["layers"].Count(l=>(string)l["role"]=="chargeCore"),Is.EqualTo(1));
   Assert.That(Sample(m,3,1850)["layers"].Where(l=>(string)l["role"]=="beam").All(l=>(string)l["kind"]=="roundedLine"),Is.True);
  }
  [Test]public void KyleImpactPreservesIndependentGradientStarAndFiveSparks(){
   var impact=Sample(Model(),3,2130)["layers"].Where(l=>(int)l["slot"]==0).ToArray();
   Assert.That(impact.Count(l=>(string)l["role"]=="impactSpark"),Is.EqualTo(5));
   Assert.That(impact.Count(l=>(string)l["role"]=="impactStar"),Is.EqualTo(1));
   Assert.That(impact.Single(l=>(string)l["role"]=="impact")["gradient"],Is.Not.Null);
  }
  [Test]public void ReferencePinAndDirectionBoundsRejectInsteadOfInventingGeometry(){var m=Model();Assert.Throws<ArgumentOutOfRangeException>(()=>Sample(m,4,0));Assert.Throws<ArgumentOutOfRangeException>(()=>Sample(m,0,-1));var c=FileJson("unity-reference-contract.json");string hash=BDirectAttackInput.Digest(c);c["coordinate_system"]["scale_relative_to_manifest"]=1;var e=Assert.Throws<TargetInvocationException>(()=>Type("FiveAgentVisualReference").GetMethod("Read").Invoke(null,new object[]{c,FileJson("manifest.json"),FileJson("projectile.json"),hash,(Func<string,Vector2>)Size}));Assert.That(e.InnerException,Is.TypeOf<ArgumentException>());}
 }
}
#endif