#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public class EquipmentMultishotModelTests {
  static JObject Example()=>JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/CommanderS1-20260922/en17-presentation-example-20261016.json"));
  static Type Model(){var t=typeof(BMiraFirstTrialContext).Assembly.GetType("LastRail.Presentation.MissionShell.EquipmentMultishotModel");Assert.That(t,Is.Not.Null,"Actual presentation source consumer missing");return t;}
  static object Read(JObject p,JObject trusted=null,string pin=null){trusted=trusted??(JObject)p.DeepClone();return Model().GetMethod("Read").Invoke(null,new object[]{p,trusted,pin??BDirectAttackInput.Digest(trusted),(string)trusted["accountId"],(string)trusted["runId"]});}
  static JObject Render(object m)=>(JObject)Model().GetMethod("Render").Invoke(m,null);
  [Test]public void ActualFrozenProjectionShowsCurrentStarTwoAndOriginalProcedureOne15Percent(){var p=(JObject)Example()["departure"];var frame=Render(Read(p));Assert.That((string)frame["basisLabel"],Is.EqualTo("출발 시 적용"));Assert.That((string)frame["slots"][0]["currentStar"],Is.EqualTo("장비 ★2"));Assert.That((string)frame["slots"][0]["appliedStar"],Is.EqualTo("시술 당시 ★1"));Assert.That((string)frame["slots"][0]["coefficient"],Is.EqualTo("추가 피해15%"));Assert.That(frame.ToString(),Does.Not.Contain("mira-equipped"));Assert.That((bool)frame["replacementPreviewAvailable"],Is.False);}
  [Test]public void ActualCurrentEmptyIsDifferentFromFrozenApplied(){var e=Example();var frame=Render(Read((JObject)e["current"]));Assert.That((string)frame["summary"],Is.EqualTo("다중 발사 없음"));Assert.That((string)frame["basisLabel"],Is.EqualTo("현재 장착"));Assert.That(((JArray)frame["slots"]).Count,Is.EqualTo(3));Assert.That((bool)frame["slots"][0]["empty"],Is.True);}
  [Test]public void ProjectionSourceHashCannotBeUsedAsProjectionByteHash(){var p=(JObject)Example()["departure"];Assert.Throws<TargetInvocationException>(()=>Read(p,p,"sha256:"+(string)p["sourceHash"]));}
  [Test]public void ExternallyChangedSelectionIsRefused(){var t=(JObject)Example()["departure"];var p=(JObject)t.DeepClone();p["coefficientBps"]=2500;Assert.Throws<TargetInvocationException>(()=>Read(p,t));}
  [TestCase("afterHighestRemoval","다중 발사 · 추가 피해15%")][TestCase("otherWearer","다중 발사 · 추가 피해15%")][TestCase("unavailable","효과 상태 확인 불가")][TestCase("empty","다중 발사 없음")]
  public void ActualExpandedOwnerStatesAreNotRecomputedOrSummed(string state,string label){var p=(JObject)JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/MiraEquipped-20261016/PresentationSourceExpanded/server-packets.json"))["packets"]["grown"]["presentations"][state];var frame=Render(Read(p));Assert.That((string)frame["summary"],Is.EqualTo(label));if(state=="otherWearer")Assert.That(((JArray)frame["slots"]).Count(x=>!(bool)x["empty"]),Is.EqualTo(1));}
  [Test]public void IndependentSnapshotCannotBeMutatedByView(){var p=(JObject)Example()["departure"];var m=Read(p);var first=Render(m);first["summary"]="fake";Assert.That((string)Render(m)["summary"],Is.Not.EqualTo("fake"));}
 }
}
#endif
