using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class Ce03CommandContractTests {
 public static JObject Prepared()=>JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-local-host/prepare-v5.json"));
 [Test] public void ActualPrepareBindsSavedOrderAndExplicitGauge(){var p=Prepared();var c=new Ce03CommandContract((JObject)p["commandContract"]);var s=c.CreateLocalTest(p,"a",1,1);CollectionAssert.AreEqual(new[]{"barrier","support_fire","emergency_repair"},((JArray)s.ReadHudState()["manuals"]).Select(r=>(string)r["ability"]));Assert.IsTrue((bool)s.ReadHudState()["ultimate"]["chargeMode"]);Assert.AreEqual(0,(int)s.ReadHudState()["ultimate"]["charge"]);}
 [Test] public void ChangedOwnerRevisionOrderAndBytesReject(){var p=Prepared();var c=new Ce03CommandContract((JObject)p["commandContract"]);Assert.Throws<FormatException>(()=>c.ValidatePrepared(p,"other",1));Assert.Throws<FormatException>(()=>c.ValidatePrepared(p,"a",2));p["draft"]["template"]["train"]["manual"][0]["id"]="support_fire";Assert.Throws<FormatException>(()=>c.ValidatePrepared(p,"a",1));p=Prepared();p["prepareSha256"]=new string('0',64);Assert.Throws<FormatException>(()=>c.ValidatePrepared(p,"a",1));}
 [TestCase("version")][TestCase("scope")][TestCase("overrides")]
 public void UnknownLeaderSemanticsReject(string key){var data=(JObject)Prepared()["commandContract"];data["leader"][key]="unknown";Assert.Throws<FormatException>(()=>new Ce03CommandContract(data));}
 [Test] public void UnknownEffectDuplicateAndExtraFieldsReject(){var data=(JObject)Prepared()["commandContract"];data["support"]["slots"][0]["skillId"]="ce03.role.smoke";Assert.Throws<NotSupportedException>(()=>new Ce03CommandContract(data));data=(JObject)Prepared()["commandContract"];data["support"]["slots"][0]["skillId"]="support_fire";Assert.Throws<FormatException>(()=>new Ce03CommandContract(data));data=(JObject)Prepared()["commandContract"];data["ready"]=true;Assert.Throws<FormatException>(()=>new Ce03CommandContract(data));}
}
