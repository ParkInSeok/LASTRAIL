#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
// Trusted process entry, not an HTTP endpoint or a standalone admission issuer.
// All positive model cases consume the supplied full prepare. Numeric effect
// cases explicitly isolate actors/initial vitals; they do not execute a fake run.
public static class Ce03ConsumerEvidenceRunner {
 const long Step=500000;
 static readonly UTF8Encoding Utf8=new UTF8Encoding(false,true);
 static JObject prepared;static Ce03CommandContract commands;static string owner;
 static readonly JArray cases=new JArray();static readonly JArray results=new JArray();
 static string C(JToken j)=>Ce03CommandContract.Canonical(j);
 static string H(string s)=>Ce03CommandContract.Sha(s);
 static void Check(bool ok,string reason){if(!ok)throw new InvalidOperationException(reason);}
 static void Reject(Action action){try{action();}catch(ArgumentException){return;}catch(FormatException){return;}catch(NotSupportedException){return;}throw new InvalidOperationException("Expected rejection did not occur.");}
 static BasicCombatSimulation Fresh()=>commands.CreateLocalTest(prepared,owner,commands.Revision,1);
 static void Case(string id,string coverage,JObject input,JObject expected,Action check){check();cases.Add(new JObject{["id"]=id,["coverage"]=coverage,["input"]=input,["expected"]=expected});results.Add(new JObject{["caseId"]=id,["coverage"]=coverage,["status"]="PASS"});}
 static JObject Values(params string[] values)=>new JObject{["checks"]=new JArray(values)};
 static string Arg(string key){var args=Environment.GetCommandLineArgs();var matches=Enumerable.Range(0,args.Length).Where(i=>args[i]==key).ToArray();if(matches.Length!=1||matches[0]+1>=args.Length||!Path.IsPathRooted(args[matches[0]+1]))throw new ArgumentException("Missing absolute runner argument: "+key);return Path.GetFullPath(args[matches[0]+1]);}
 public static void Run(){
  string output=null;
  try{
   output=Arg("-ce03Output");string bundlePath=Arg("-ce03SourceBundle"),preparePath=Arg("-ce03Prepare");
   Check(Directory.Exists(output)&&Directory.GetFileSystemEntries(output).Length==0,"Output must be a new empty directory.");
   cases.Clear();results.Clear();string bundle=Utf8.GetString(File.ReadAllBytes(bundlePath));var source=JObject.Parse(bundle);
   Check((string)source["schemaVersion"]=="ce03-source-bundle-v1"&&C(source)==bundle,"Noncanonical source bundle.");
   VerifySources(source);
   prepared=JObject.Parse(Utf8.GetString(File.ReadAllBytes(preparePath)));owner=(string)prepared["ownedSelection"]?["accountId"];
   commands=new Ce03CommandContract(prepared["commandContract"] as JObject);commands.ValidatePrepared(prepared,owner,commands.Revision);
   var manifest=new Ce03PresentationManifest(prepared["presentationManifest"] as JObject);
   var shared=new LastRail.Diagnostics.Ce03SharedConsumerChecks(prepared);shared.Run(output,File.ReadAllText("QA/Ce03CommandIntegration/synthetic-r6-transport.json"));
   foreach(var row in shared.Cases)cases.Add(row);foreach(var row in shared.Results)results.Add(row);
   Case("typed-presentation-load","PRESENTATION_BINDINGS",new JObject{["manifestSha256"]=manifest.Hash},Values("all typed locators, source hashes and serialized roles","tampered asset digest rejected"),()=>{Ce03PresentationManifestLoader.Validate(manifest);var m=manifest.Copy();m["bindings"][0]["assetRawSha256"]=new string('0',64);Reject(()=>Ce03PresentationManifestLoader.Validate(new Ce03PresentationManifest(m)));});
   Check(results.Count==11&&results.Select(x=>(string)x["coverage"]).Distinct().Count()==11,"Incomplete consumer coverage.");
   VerifySources(source);commands.ValidatePrepared(prepared,owner,commands.Revision);Ce03PresentationManifestLoader.Validate(manifest);
   var schema=shared.CheckpointSchema;
   string checkpoint=C(schema),vectors=C(new JObject{["schemaVersion"]="ce03-executed-vectors-v1",["prepareSha256"]=prepared["prepareSha256"].DeepClone(),["actualAccountRun"]=false,["testSeed"]=1,["cases"]=cases});
   var report=new JObject{["schemaVersion"]="ce03-consumer-runner-report-v3",["scope"]="FULL_LOCAL_CONSUMER",["runId"]="unity-"+Guid.NewGuid().ToString("N"),["simulationVersion"]=prepared["draft"]["simulationVersion"].DeepClone(),["draftSha256"]=H(C(prepared["draft"])),["algorithmSourceSha256"]=H(bundle),["vectorsSha256"]=H(vectors),["checkpointSchemaSha256"]=H(checkpoint),["commandContractSha256"]=H(C(prepared["commandContract"])),["presentationManifestSha256"]=manifest.Hash,["prepareSha256"]=prepared["prepareSha256"].DeepClone(),["results"]=results,["passed"]=results.Count,["failed"]=0,["skipped"]=0};
   File.WriteAllBytes(Path.Combine(output,"algorithm-source.json"),File.ReadAllBytes(bundlePath));File.WriteAllText(Path.Combine(output,"checkpoint-schema.json"),checkpoint,Utf8);File.WriteAllText(Path.Combine(output,"vectors.json"),vectors,Utf8);File.WriteAllText(Path.Combine(output,"runner-report-v3.json"),C(report),Utf8);
   Debug.Log("CE03 actual consumer checks passed: "+results.Count);EditorApplication.Exit(0);
  }catch(Exception e){Debug.LogException(e);if(output!=null&&Directory.Exists(output))File.WriteAllText(Path.Combine(output,"failure.json"),C(new JObject{["passedBeforeFailure"]=results.Count,["errorType"]=e.GetType().Name,["message"]=e.Message,["actualAdmissionReady"]=false}),Utf8);EditorApplication.Exit(1);}
 }
 public static void VerifySources(JObject source){
  var files=source["files"] as JArray;Check(files!=null&&files.Count>0,"Empty source bundle.");string root=Path.GetFullPath(Path.Combine(Application.dataPath,".."))+Path.DirectorySeparatorChar;var names=new HashSet<string>(StringComparer.Ordinal);
  foreach(var f in files){string name=(string)f["path"];Check(name!=null&&!Path.IsPathRooted(name)&&!name.Contains('\\')&&!name.Split('/').Any(x=>x==".."||x=="."||x=="")&&names.Add(name),"Invalid source path.");string full=Path.GetFullPath(Path.Combine(root,name));Check(full.StartsWith(root,StringComparison.OrdinalIgnoreCase),"Source path escape.");string text=Utf8.GetString(File.ReadAllBytes(full));Check(text==(string)f["content"]&&H(text)==(string)f["sha256"],"Source mismatch: "+name);}
  foreach(string path in Directory.GetFiles(Path.Combine(Application.dataPath,"_LASTRAIL/Scripts"),"*.cs",SearchOption.AllDirectories)){string relative=path.Substring(root.Length).Replace('\\','/');Check(names.Contains(relative),"Runtime source omitted: "+relative);}
  foreach(string required in new[]{"Assets/_LASTRAIL/Editor/Ce03ConsumerEvidenceRunner.cs","Assets/_LASTRAIL/Editor/Ce03ConsumerEffectChecks.cs","Assets/_LASTRAIL/Editor/Ce03AcceptedRunConsumer.cs","QA/Ce03CommandIntegration/synthetic-r6-transport.json","Assets/_LASTRAIL/Scripts/LastRail.Runtime.asmdef","Packages/manifest.json","Packages/packages-lock.json","ProjectSettings/ProjectVersion.txt"})Check(names.Contains(required),"Runner dependency omitted: "+required);
 }
}
#endif
