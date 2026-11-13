#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public class BMiraEquippedTrialContextTests {
  static JObject Packet()=>JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/CommanderS1-20260922/en17-formal-local-trial-packet-20261015.json"));
  static Type ContextType(){var t=typeof(BMiraFirstTrialContext).Assembly.GetType("LastRail.Battle.BMiraEquippedTrialContext");Assert.That(t,Is.Not.Null,"Separate strict equipped context is missing");return t;}
  static object Read(JObject p,JObject trusted=null,string pin=null){trusted=trusted??Packet();return ContextType().GetMethod("Read").Invoke(null,new object[]{p,trusted,pin??(string)trusted["startHash"],"a",(string)trusted["runId"],trusted["source"]["originalComponent"]["originalMira"],(string)trusted["originalMiraHash"],(string)trusted["originalRunSnapshotHash"]});}
  [Test]public void ReadsActualFormalSavedSourceWithoutGrantingOperatingSupport(){var p=Packet();var c=Read(p);var saved=(JObject)ContextType().GetMethod("Capture").Invoke(c,null);Assert.That(JToken.DeepEquals(saved["start"],p),Is.True);Assert.That((string)saved["profile"],Is.EqualTo("mira-equipped-multishot-local.v1.20261015"));Assert.That((bool)saved["start"]["source"]["canStart"],Is.False);}
  [TestCase("profile")][TestCase("selection")][TestCase("critical")][TestCase("effectRevision")][TestCase("stats")]
  public void RefusesExternallyRehashedOrUnpinnedMutation(string kind){var p=Packet();switch(kind){case "profile":p["profile"]=BMiraFirstTestSource.Profile;break;case "selection":p["source"]["selection"]["definitionSnapshot"]["coefficientBps"]=2500;break;case "critical":p["source"]["critical"]["rngDraws"]=1;break;case "effectRevision":p["source"]["slots"][0]["effect"]["effectRevision"]=9;break;case "stats":p["source"]["effectiveStats"]["attack"]["numerator"]="999";break;}Assert.Throws<TargetInvocationException>(()=>Read(p));}
  [TestCase("candidate")][TestCase("old-version")][TestCase("critical-zero")][TestCase("other-wearer")][TestCase("unsupported")]
  public void EvenPinnedOriginalMustSatisfyExplicitSupportedContract(string kind){var p=Packet();var source=(JObject)p["source"];switch(kind){case "candidate":source["startBinding"]="LOCAL_TRIAL_CANDIDATE_REQUIRES_ATOMIC_COMMIT";break;case "old-version":source["version"]="equipment.multishot-equipped-12-source.v1.20261015";break;case "critical-zero":source["critical"]["chanceBps"]=0;break;case "other-wearer":source["slots"][0]["instance"]["wearer_id"]="op_kyle";break;case "unsupported":source["unsupported"]=new JArray("UNSUPPORTED_SET_EFFECT");break;}var body=(JObject)source.DeepClone();body.Remove("packetHash");source["packetHash"]=BDirectAttackInput.Digest(body).Substring(7);p["sourceHash"]=source["packetHash"].DeepClone();body=(JObject)p.DeepClone();body.Remove("source");body.Remove("startHash");p["startHash"]=BDirectAttackInput.Digest(body).Substring(7);Assert.Throws<TargetInvocationException>(()=>Read(p,p,(string)p["startHash"]));}
  [Test]public void ReturnsDetachedSnapshot(){var c=Read(Packet());var t=ContextType();var a=(JObject)t.GetMethod("Capture").Invoke(c,null);a["start"]["source"]["selection"]=null;Assert.That(t.GetMethod("Capture").Invoke(c,null).ToString(),Does.Contain("FORMAL_CATALOG_SELECTED"));}
 }
}
#endif
