using System;
using System.IO;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class Ce03AcceptedCombatTests {
 // Deliberately synthetic contract fixture. Never starts a simulation or view.
 static JObject Fixture()=>JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-local-host/synthetic-accepted-exchange.json"));
 [Test] public void SyntheticEnvelopeShapeOnlyDoesNotExecuteBattle(){var f=Fixture();Assert.IsFalse((bool)f["actualBattleExecuted"]);var r=(JObject)f["startResponse"];var parsed=new Ce03AcceptedCombat(r,Ce03CommandContractTests.Prepared(),"a",(string)r["operationPreviewId"]);Assert.AreEqual((string)r["runId"],parsed.RunId);}
 [TestCase("profile")][TestCase("contentManifest")][TestCase("snapshot")]
 public void AnyPcsByteChangeRejects(string field){var r=(JObject)Fixture()["startResponse"];r["combat"][field]["extra"]=true;Assert.Throws<FormatException>(()=>new Ce03AcceptedCombat(r,Ce03CommandContractTests.Prepared(),"a",(string)r["operationPreviewId"]));}
 [Test] public void DifferentOwnerQuoteAndUnwrappedR4Reject(){var r=(JObject)Fixture()["startResponse"];Assert.Throws<FormatException>(()=>new Ce03AcceptedCombat(r,Ce03CommandContractTests.Prepared(),"b",(string)r["operationPreviewId"]));Assert.Throws<FormatException>(()=>new Ce03AcceptedCombat(r,Ce03CommandContractTests.Prepared(),"a","other"));r["combat"]["profile"]=r["combat"]["profile"]["baseProfile"].DeepClone();r["combat"]["profileSha256"]=Ce03CommandContract.Sha(Ce03CommandContract.Canonical(r["combat"]["profile"]));Assert.Throws<FormatException>(()=>new Ce03AcceptedCombat(r,Ce03CommandContractTests.Prepared(),"a",(string)r["operationPreviewId"]));}
}
