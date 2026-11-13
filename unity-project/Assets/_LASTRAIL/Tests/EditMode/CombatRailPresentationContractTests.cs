using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class CombatRailPresentationContractTests {
 public static JObject Golden(){
  string dir="D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/SecondFinal-20260913/FinalClosure01/";
  var a=JObject.Parse(File.ReadAllText(dir+"presentation-closure-audit.json"));var e=JObject.Parse(File.ReadAllText(dir+"second-final-closure-extension.json"));
  var m=new JObject{["profile"]="second-final-selected-v3",["root"]=a["root"],["auditSha256"]=Hash(dir+"presentation-closure-audit.json"),["extensionSha256"]=Hash(dir+"second-final-closure-extension.json"),["selectionManifestSha256"]=new string('a',64),["roles"]=a["roles"],["bindings"]=a["rows"],["selectedResources"]=e["selectedResources"],["sourceFiles"]=e["sourceFiles"]};
  const string project="D:/Project/LASTRAIL/unity-project/";m["sourceFiles"]=new JArray(Directory.GetFiles(project+"Assets/_LASTRAIL","*.cs",SearchOption.AllDirectories).OrderBy(p=>p,StringComparer.Ordinal).Select(p=>new JObject{["path"]=p.Substring(project.Length).Replace(Path.DirectorySeparatorChar,'/'),["sha256"]=Hash(p)}));
  var p=CombatRailPreparationTests.Golden();p["railPresentation"]=new JObject{["version"]="ce03-rail-presentation-v1",["manifestSha256"]="",["manifest"]=m};Rehash(p);return p;
 }
 static string Hash(string path){using var h=System.Security.Cryptography.SHA256.Create();return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
 static void Rehash(JObject p){var r=(JObject)p["railPresentation"];r["manifestSha256"]=Ce03CommandContract.Sha(Ce03CommandContract.Canonical(r["manifest"]));var copy=(JObject)p.DeepClone();copy.Remove("preparationSha256");p["preparationSha256"]=Ce03CommandContract.Sha(Ce03CommandContract.Canonical(copy));}
 [Test] public void OptionalCurrentPresentationKeepsGameplayAndLegacyPreparedBytesExact(){var legacy=new Ce03RailPreparation(CombatRailPreparationTests.Golden(),"a",1);var current=new Ce03RailPreparation(Golden(),"a",1);Assert.AreEqual(legacy.CreateModel(1).CaptureLocalState(),current.CreateModel(1).CaptureLocalState());Assert.AreEqual(Ce03CommandContract.Canonical(legacy.Copy()["basePreparation"]),Ce03CommandContract.Canonical(current.Copy()["basePreparation"]));Assert.AreNotEqual(legacy.PreparationSha256,current.PreparationSha256);}
 [TestCase("root")][TestCase("selected")][TestCase("role")][TestCase("dependency")][TestCase("source")][TestCase("unknown")][TestCase("digest")]
 public void RehashedMalformedCurrentManifestIsRejected(string kind){var p=Golden();var m=(JObject)p["railPresentation"]["manifest"];switch(kind){case "root":m["root"]="Assets/other.asset";break;case "selected":m["selectedResources"][0]["resourceKey"]="CombatSecondFinal/unapproved";break;case "role":m["roles"][0]["guid"]=new string('0',32);break;case "dependency":((JArray)m["bindings"][0]["directDependencies"]).Add("Assets/missing.asset");break;case "source":m["sourceFiles"][0]["path"]="../outside.cs";break;case "unknown":m["acceptAnything"]=true;break;case "digest":m["auditSha256"]="invalid";break;}Rehash(p);Assert.Throws<ArgumentException>(()=>new Ce03RailPreparation(p,"a",1));}
 static void Verify(Ce03RailPreparation prep){var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Ce03RailPresentationBinding")).FirstOrDefault(t=>t!=null);Assert.NotNull(type,"Current approved object/file binding verifier required");try{type.GetMethod("Verify").Invoke(null,new object[]{prep});}catch(TargetInvocationException e){throw e.InnerException;}}
 [Test] public void CurrentFilesAndRootObjectsAreVerified(){Verify(new Ce03RailPreparation(CombatRailZone02ContractTests.Golden(),"a",1));}
 [Test] public void ExactServerFixtureParsesWithoutChangingGameplay(){var p=CombatRailPreparationTests.Golden();p["railPresentation"]=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Server2-FinalClosure-20260914/rail-presentation-sample.json"));Rehash(p);var next=new Ce03RailPreparation(p,"a",1);Assert.AreEqual("f60f08e57120fe97e02e3a83c71134f6cb566a6c937ae81fe3d5f353db8e459f",next.Presentation.Hash);Assert.AreEqual(new Ce03RailPreparation(CombatRailPreparationTests.Golden(),"a",1).CreateModel(1).CaptureLocalState(),next.CreateModel(1).CaptureLocalState());}
 [Test] public void RehashedStaleSourceHashFailsPhysicalBinding(){var p=CombatRailZone02ContractTests.Golden();p["railPresentation"]["manifest"]["sourceFiles"][0]["sha256"]=new string('0',64);Rehash(p);Assert.Throws<InvalidOperationException>(()=>Verify(new Ce03RailPreparation(p,"a",1)));}
 [Test] public void ExtraTypedSiblingCannotUseEditorIconException(){var p=Golden();var rows=(JArray)p["railPresentation"]["manifest"]["bindings"];var extra=(JObject)rows.Single(b=>(string)b["path"]=="Packages/com.unity.ugui/Editor Resources/Gizmos/TMP - Font Asset Icon.psd").DeepClone();extra["localFileId"]="2800001";rows.Add(extra);Rehash(p);Assert.Throws<ArgumentException>(()=>new Ce03RailPreparation(p,"a",1));}
}
