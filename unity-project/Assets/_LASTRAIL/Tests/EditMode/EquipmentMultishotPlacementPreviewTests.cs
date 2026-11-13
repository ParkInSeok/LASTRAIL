#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using LastRail.Battle;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public class EquipmentMultishotPlacementPreviewTests {
  static JObject Packet()=>JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/CommanderS1-20260922/en17-placement-preview-example-20261016.json"));
  static JObject Command()=>new JObject{["commandId"]="preview-placement",["instanceId"]="aa",["expectedInventoryRevision"]=4,["expectedInstanceRevision"]=1,["target"]=new JObject{["kind"]="operator",["wearerId"]="op_mira",["slotIndex"]=1},["expectedTargetInstanceId"]="z"};
  static Type Model(){var t=typeof(BMiraFirstTrialContext).Assembly.GetType("LastRail.Presentation.MissionShell.EquipmentMultishotPlacementPreviewModel");Assert.That(t,Is.Not.Null,"Strict proposed placement consumer missing");return t;}
  static object Read(JObject p,JObject trusted=null,JObject command=null){trusted=trusted??p;return Model().GetMethod("Read").Invoke(null,new object[]{p,trusted,BDirectAttackInput.Digest(trusted),(string)trusted["before"]?["accountId"]??"a",(string)trusted["before"]?["runId"]??(string)Packet()["before"]["runId"],command??Command()});}
  [Test]public void ActualOwnedThreeSlotPreviewIsExplicitlyUncommitted(){var p=Packet();var m=Read(p);var render=(JObject)Model().GetMethod("Render").Invoke(m,null);Assert.That((string)render["statusLabel"],Is.EqualTo("확정 전 미리보기"));Assert.That((string)render["before"]["basisLabel"],Is.EqualTo("장착 전"));Assert.That((string)render["after"]["basisLabel"],Is.EqualTo("장착 후 예상"));Assert.That((string)render["after"]["summary"],Does.Contain("25%"));Assert.That((bool)render["appliesToCombat"],Is.False);Assert.That(JToken.DeepEquals((JObject)Model().GetMethod("OriginalCommand").Invoke(m,null),Command()),Is.True);}
  [Test]public void PreparedNestedBasisCannotEnterOrdinaryCurrentOrDepartureModel(){var p=(JObject)Packet()["after"];Assert.Throws<ArgumentException>(()=>EquipmentMultishotModel.Read(p,p,BDirectAttackInput.Digest(p),(string)p["accountId"],(string)p["runId"]));}
  [TestCase("receipt")][TestCase("denied")][TestCase("candidate-hash")][TestCase("future-revision")][TestCase("committed")]
  public void PinnedOriginalStillMustSatisfyPreviewSemantics(string kind){var p=Packet();switch(kind){case "receipt":p["receipt"]=p["placementProposal"].DeepClone();break;case "denied":p["proof"]["allowed"]=false;break;case "candidate-hash":p["candidateSourceHash"]=new string('0',64);break;case "future-revision":p["after"]["inventoryRevision"]=5;break;case "committed":p["committed"]=true;break;}Assert.Throws<TargetInvocationException>(()=>Read(p));}
  [Test]public void ExternallyRewrittenPreviewOrDifferentOriginalCommandIsRefused(){var trusted=Packet();var p=(JObject)trusted.DeepClone();p["after"]["coefficientBps"]=1500;Assert.Throws<TargetInvocationException>(()=>Read(p,trusted));var command=Command();command["target"]["slotIndex"]=0;Assert.Throws<TargetInvocationException>(()=>Read(trusted,trusted,command));}
 }
}
#endif
