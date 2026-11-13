#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using LastRail.Battle;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public sealed class CombatDefinitionIntakeTests {
  const string Root="D:/Project/LASTRAIL/QA/CommanderS1-20260922/";
  static JObject Original()=>JObject.Parse(File.ReadAllText(Root+"c3-definition-intake-20261001.json"));
  static JObject Provenance()=>JObject.Parse(File.ReadAllText(Root+"c3-definition-provenance-20261001.json"));
  static JToken Order(JToken t)=>t is JObject o?new JObject(o.Properties().OrderBy(p=>p.Name,StringComparer.Ordinal).Select(p=>new JProperty(p.Name,Order(p.Value)))):t is JArray a?new JArray(a.Select(Order)):t.DeepClone();
  static void Rehash(JObject data){using var sha=SHA256.Create();data["contentHash"]="sha256:"+BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(Order(data["definition"]).ToString(Formatting.None)))).Replace("-","").ToLowerInvariant();}
  static void Reject(JObject data){var before=data.DeepClone();Assert.Throws<ArgumentException>(()=>CombatDefinitionIntake.Read(data));Assert.IsTrue(JToken.DeepEquals(before,data));}
  [Test] public void PublishedIndexIsConsumedWithoutGrantingExecutionOrInventingRuntime(){
   var input=Original();var before=input.DeepClone();var dto=CombatDefinitionIntake.Read(input);
   CollectionAssert.AreEqual(new[]{"op_kyle","op_reina","op_eve","op_sera","op_mira"},dto.OperatorDefinitionIds);
   Assert.AreEqual(9,dto.RequiredEffectKinds.Count);Assert.AreEqual(7,dto.MissingRuntimeInputs.Count);
   Assert.IsFalse(dto.CanExecute);Assert.AreEqual("AVAILABLE",dto.DefinitionSupport);Assert.AreEqual("MISSING",dto.RuntimeBindingSupport);
   foreach(var key in new[]{"effectiveStatProfiles","startSnapshot","effectRuntime"})Assert.AreEqual(JTokenType.Null,dto.Capture()["definition"][key].Type);
   Assert.Throws<InvalidOperationException>(()=>dto.RequireExecutable());Assert.IsTrue(JToken.DeepEquals(before,input));
  }
  [TestCase("schemaVersion")][TestCase("definitionVersion")][TestCase("contentHash")][TestCase("purpose")][TestCase("canExecute")][TestCase("support")][TestCase("blockingReasons")][TestCase("definition")]
  public void MissingEnvelopeFieldsReject(string field){var d=Original();d.Remove(field);Reject(d);}
  [TestCase("schemaVersion")][TestCase("definitionVersion")][TestCase("purpose")][TestCase("contentHash")]
  public void UnapprovedIdentityRejects(string field){var d=Original();d[field]="UNISSUED";Reject(d);}
  [Test] public void CanExecuteForgeryRejects(){var d=Original();d["canExecute"]=true;Reject(d);}
  [TestCase("definitions")][TestCase("runtimeBinding")][TestCase("executionEvidence")]
  public void ForgedSupportRejects(string field){var d=Original();d["support"][field]="AVAILABLE_AND_EXECUTABLE";Reject(d);}
  [Test] public void MissingBlockersReject(){var d=Original();d["blockingReasons"]=new JArray();Reject(d);}
  [Test] public void CorruptDefinitionRejects(){var d=Original();d["definition"]["operators"][0]["abilities"][0]["shieldBps"]=9999;Reject(d);}
  [Test] public void AttackerRehashDoesNotApproveChangedEffect(){var d=Original();d["definition"]["operators"][0]["abilities"][0]["shieldBps"]=9999;Rehash(d);Reject(d);}
  [TestCase("effectiveStatProfiles")][TestCase("startSnapshot")][TestCase("effectRuntime")]
  public void InventedRuntimeRemainsUnapprovedEvenWhenRehashed(string key){var d=Original();d["definition"][key]=new JObject();Rehash(d);Reject(d);}
  [Test] public void UnknownEnvelopeFieldRejects(){var d=Original();d["formation"]=new JArray();Reject(d);}
  [Test] public void KeyOrderDoesNotChangeApprovedContent(){var d=(JObject)Order(Original());Assert.IsNotNull(CombatDefinitionIntake.Read(d));}
  [Test] public void InputAndReturnedSnapshotsCannotChangeAcceptedDefinition(){
   var input=Original();var dto=CombatDefinitionIntake.Read(input);var captured=dto.Capture();input["canExecute"]=true;captured["definition"]["operators"][0]["operatorId"]="FORGED";
   Assert.IsFalse(dto.CanExecute);Assert.AreEqual("op_kyle",(string)dto.Capture()["definition"]["operators"][0]["operatorId"]);Assert.Throws<InvalidOperationException>(()=>dto.RequireExecutable());
  }
  [Test] public void ActualProvenancePinsMatchSourceBytes(){
   var p=Provenance();foreach(var row in p["sources"]){using var sha=SHA256.Create();var digest=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes((string)row["path"]))).Replace("-","").ToLowerInvariant();Assert.AreEqual((string)row["sha256"],digest);}
   Assert.IsNotNull(CombatDefinitionIntake.ReadWithProvenance(Original(),p));
  }
  [TestCase("sha256")][TestCase("path")]
  public void ForgedSourcePinRejects(string key){var p=Provenance();p["sources"][0][key]="FORGED";Assert.Throws<ArgumentException>(()=>CombatDefinitionIntake.ReadWithProvenance(Original(),p));}
  [Test] public void DuplicateSourceCannotReplaceMissingPin(){var p=Provenance();p["sources"][1]=p["sources"][0].DeepClone();Assert.Throws<ArgumentException>(()=>CombatDefinitionIntake.ReadWithProvenance(Original(),p));}
  [Test] public void DefinitionIndexCannotBeUsedAsExistingProducer(){var d=Original();Assert.Throws<ArgumentException>(()=>BasicCombatSimulation.FromPreparedProfileForLocalTest(d,new JObject(),"a",1u));}
 }
}
#endif