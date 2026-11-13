using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class Ce03VerifiedFactoryTests {
 [Test] public void SourceChangeStillFailsFinalValidation(){
  var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Ce03ConsumerEvidenceRunner")).First(t=>t!=null);var method=type.GetMethod("VerifySources");var source=Ce03R6RecoveryChecks.ReadJson(File.ReadAllText("D:/Project/LASTRAIL/Worktrees/5f93/LASTRAIL/QA/Ce03CommandIntegration/full-closure-20260912T105330/source-input.json"));foreach(var file in (JArray)source["files"]){string content=System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(UnityEngine.Application.dataPath,"..",(string)file["path"])));file["content"]=content;file["sha256"]=Ce03CommandContract.Sha(content);}Assert.DoesNotThrow(()=>method.Invoke(null,new object[]{source}));source["files"][0]["content"]="tampered";Assert.Catch(()=>method.Invoke(null,new object[]{source}));
 }
 // Mechanical model-factory test using retained original identity. No network/admission.
 [Test] public void VerifiedFactoryCreatesIndependentModelsFromImmutableOriginal(){
  var path="D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-local-host/session-EoOAuc/original-start.json";
  var raw=File.ReadAllText(path);var identity=Ce03R6RecoveryChecks.ReadJson(raw);var original=new Ce03OriginalStart(raw,(string)identity["owner"],new Uri((string)identity["origin"]+"/"));var factory=original.CreateVerifiedEditorFactory();var first=factory();var second=factory();Assert.AreNotSame(first,second);Assert.AreEqual(first.CaptureLocalState(),second.CaptureLocalState());var journal=JObject.Parse(first.CaptureLocalState())["effectJournal"];Assert.AreEqual(original.RunId,(string)journal["runId"]);var copy=original.Copy();copy["owner"]="mutated-copy";Assert.AreEqual(first.CaptureLocalState(),factory().CaptureLocalState());Assert.AreEqual(Ce03CommandContract.Canonical(identity),original.Json);
 }
}
