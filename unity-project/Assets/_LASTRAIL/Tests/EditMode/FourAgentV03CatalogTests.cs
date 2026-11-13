using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
namespace LastRail.Tests.EditMode {
 public class FourAgentV03CatalogTests {
  public const string Root="D:/Project/LASTRAIL/QA/DirectionImpact-20260921/four-agent-final-review-v01/";
  public const string Crops="D:/Project/LASTRAIL/QA/PlanImpact-20261015/eve-drone-motion-review-v01/five-agent-combat-fx-v02/";
  public static JObject Json(string p)=>JObject.Parse(File.ReadAllText(p));
  public static Vector2 Size(string p){var b=File.ReadAllBytes(p);int N(int i)=>(b[i]<<24)|(b[i+1]<<16)|(b[i+2]<<8)|b[i+3];return new Vector2(N(16),N(20));}
  public static object Catalog(){var t=typeof(BDirectAttackInput).Assembly.GetType("LastRail.Presentation.Battle.FourAgentV03Catalog");Assert.That(t,Is.Not.Null,"v03 clip consumer missing");return t.GetMethod("Read").Invoke(null,new object[]{Json(Root+"unity-clips.json"),Json(Crops+"manifest.json"),Crops,(Func<string,Vector2>)Size});}
  static JObject Sample(object c,string id,int d,bool attack,long age,long continuous=0)=>(JObject)c.GetType().GetMethod("Sample").Invoke(c,new object[]{id,d,attack,age,continuous,new Vector2(570,800)});
  [Test]public void ExactThirtyTwoClipsKeepApprovedExceptions(){var c=Catalog();var data=(JObject)c.GetType().GetMethod("Capture").Invoke(c,null);Assert.That(data["clips"].Count(),Is.EqualTo(32));foreach(var row in data["clips"]){string id=(string)row["actorId"],motion=(string)row["motion"];int expected=id=="op_reina"?(motion=="idle"?4:24):12;Assert.That(row["frames"].Count(),Is.EqualTo(expected));Assert.That(row["frames"].Sum(f=>(int)f["durationMs"]),Is.EqualTo((int)row["durationMs"]));}}
  [TestCase("op_kyle",1040)][TestCase("op_reina",960)][TestCase("op_eve",2400)][TestCase("op_sera",2160)]
  public void AttackIsOneShotAndReturnsToCurrentIdle(string id,int duration){var c=Catalog();Assert.That((bool)Sample(c,id,1,true,duration-1)["attacking"],Is.True);Assert.That((bool)Sample(c,id,1,true,duration,3120)["attacking"],Is.False);Assert.That((string)Sample(c,id,1,true,duration,3120)["path"],Is.EqualTo((string)Sample(c,id,1,false,0,3120)["path"]));}
  [Test]public void KyleStartsAtAttackFrameTwelveWithoutPreviewDelay(){var c=Catalog();var raw=Json(Crops+"manifest.json");var f=Sample(c,"op_kyle",1,true,0);Assert.That((string)f["path"],Is.EqualTo(Crops+(string)raw["directions"][0]["kyle"][12]["src"]));Assert.That((int)f["frameIndex"],Is.Zero);}
  [Test]public void ReinaAttackUsesSeparateCroppedBodyNotRawComposite(){var c=Catalog();var f=Sample(c,"op_reina",4,true,400);Assert.That((string)f["path"],Is.EqualTo(Crops+(string)Json(Crops+"manifest.json")["directions"][3]["reina"][10][0]["src"]));}
  [Test]public void EveUsesOneIdleAnchorAcrossBothStates(){var c=Catalog();var idle=Sample(c,"op_eve",2,false,0);var attack=Sample(c,"op_eve",2,true,1000);Assert.That((float)attack["scale"],Is.EqualTo((float)idle["scale"]));Assert.That(JToken.DeepEquals(attack["anchor"],idle["anchor"]),Is.True);}
  [Test]public void SeraPaddingChangesAnchorNotScale(){var c=Catalog();var idle=Sample(c,"op_sera",3,false,0);var attack=Sample(c,"op_sera",3,true,1000);Assert.That((float)idle["scale"],Is.EqualTo(114f/1536));Assert.That((float)attack["scale"],Is.EqualTo((float)idle["scale"]));Assert.That((float)attack["anchor"][0]-(float)idle["anchor"][0],Is.EqualTo(100));Assert.That((float)attack["anchor"][1]-(float)idle["anchor"][1],Is.EqualTo(170));}
  [Test]public void FrameCoordinatesDoNotFitEachChangingBbox(){var c=Catalog();foreach(var id in new[]{"op_eve","op_sera","op_reina"}){var a=Sample(c,id,1,false,0,0);var b=Sample(c,id,1,false,0,240);Assert.That(JToken.DeepEquals(a["anchor"],b["anchor"]),Is.True);Assert.That((float)a["scale"],Is.EqualTo((float)b["scale"]));}}
 }
}
