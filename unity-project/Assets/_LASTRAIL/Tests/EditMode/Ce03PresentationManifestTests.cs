using System;
using System.IO;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class Ce03PresentationManifestTests {
 const string Root="D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-local-host/self-check-AEk3o8/";
 public static JObject Prepared()=>JObject.Parse(File.ReadAllText(Root+"prepare-r6.json"));
 static JObject Manifest()=>(JObject)Prepared()["presentationManifest"];
 [Test] public void ActualR6PrepareIncludesFullManifestInDigest(){var p=Prepared();var c=new Ce03CommandContract((JObject)p["commandContract"]);c.ValidatePrepared(p,(string)p["ownedSelection"]["accountId"],c.Revision);Assert.AreEqual("6ec8829c98e35c7c356d5e2dff010ff61a0a77477df8327e462819d503aa8d88",(string)p["prepareSha256"]);p["presentationManifest"]["sourceFiles"][0]["rawSha256"]=new string('0',64);Assert.Throws<FormatException>(()=>c.ValidatePrepared(p,(string)p["ownedSelection"]["accountId"],c.Revision));}
 [TestCase("extra")][TestCase("locator")][TestCase("role")][TestCase("frame")][TestCase("dependency")][TestCase("duplicate")][TestCase("source")][TestCase("order")]
 public void ShapeAndClosureRejectTampering(string mode){var m=Manifest();var b=(JArray)m["bindings"];switch(mode){case "extra":m["extra"]=true;break;case "locator":b[0]["locator"]["localFileId"]="9223372036854775808";break;case "role":m["roles"]["font"]=m["roles"]["configuration"].DeepClone();break;case "frame":m["roles"]["frames"][0]["key"]="unknown/attack/1";break;case "dependency":b[0]["dependencies"]=new JArray("absent");break;case "duplicate":b[1]=b[0].DeepClone();break;case "source":m["sourceFiles"][0]["logicalName"]="unknown";break;case "order":var first=b[0].DeepClone();b[0]=b[1].DeepClone();b[1]=first;break;}Assert.Throws<FormatException>(()=>new Ce03PresentationManifest(m));}
 [Test] public void ManifestRetainsImmutableCopy(){var m=Manifest();var parser=new Ce03PresentationManifest(m);string hash=parser.Hash;m["extra"]=true;var copy=parser.Copy();copy["extra"]=true;Assert.AreEqual(hash,parser.Hash);}
 [TestCase("policyVersion")][TestCase("policySha256")][TestCase("extra")]
 public void ActualPreparedAdapterRejectsUnknownExecutionPolicy(string field){var p=Prepared();p["draft"]["template"]["execution"][field]="unknown";Assert.Throws<ArgumentException>(()=>BasicCombatSimulation.FromPreparedProfileForLocalTest(p,(JObject)p["ownedSelection"],(string)p["ownedSelection"]["accountId"],1));}
}
