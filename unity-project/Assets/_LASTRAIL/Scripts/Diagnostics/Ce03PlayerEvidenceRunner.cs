#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.IO;
using System.Linq;
using System.Text;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Diagnostics {
// Explicit QA Player entry; it never issues START or installs a verifier.
public sealed class Ce03PlayerEvidenceRunner:MonoBehaviour {
 [SerializeField] CombatPlayerAssetBinding binding;
 [SerializeField] TextAsset compiledSources,recoveryFixture;
 static readonly UTF8Encoding Utf8=new UTF8Encoding(false,true);
 public void Initialize(CombatPlayerAssetBinding assets,TextAsset sources,TextAsset fixture){binding=assets;compiledSources=sources;recoveryFixture=fixture;}
 static string C(JToken value)=>Ce03CommandContract.Canonical(value);
 static string H(string value)=>Ce03CommandContract.Sha(value);
 static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
 static void EnableExplicitBufferTrace(){if(Environment.GetCommandLineArgs().Contains("-ce03BufferProbe"))Unity.Collections.NativeLeakDetection.Mode=Unity.Collections.NativeLeakDetectionMode.EnabledWithStackTrace;}
 static string Arg(string key){var args=Environment.GetCommandLineArgs();var indices=Enumerable.Range(0,args.Length).Where(i=>args[i]==key).ToArray();if(indices.Length!=1||indices[0]+1>=args.Length||!Path.IsPathRooted(args[indices[0]+1]))throw new ArgumentException("Absolute argument required: "+key);return Path.GetFullPath(args[indices[0]+1]);}
 async void Start(){string output=null;bool ownsOutput=false;
  try{
   Application.runInBackground=true;Application.targetFrameRate=30;QualitySettings.vSyncCount=0;
   output=Arg("-ce03Output");Check(output.StartsWith("D:\\Project\\LASTRAIL\\",StringComparison.OrdinalIgnoreCase)&&Directory.Exists(output)&&Directory.GetFileSystemEntries(output).Length==0,"Fresh empty D output required");ownsOutput=true;
   string bundle=File.ReadAllText(Arg("-ce03SourceBundle"),Utf8);var source=JObject.Parse(bundle);Check(C(source)==bundle,"Canonical source bundle required");
   var sourcePin=new Ce03PlayerSourcePin(JObject.Parse(compiledSources.text));sourcePin.Verify(source);
   if(Environment.GetCommandLineArgs().Contains("-ce03BufferProbe")){
    // Read-only package owner probe: never create the lazy manager, dispose a
    // borrowed buffer, suppress warnings, or force a collection.
    await System.Threading.Tasks.Task.Delay(1000);
    var types=UnityEngine.Resources.FindObjectsOfTypeAll<MonoBehaviour>().Where(x=>x!=null).Select(x=>x.GetType().FullName).Distinct().OrderBy(x=>x).ToArray();
    // Inspect only: the package owns this buffer and its release. Observe at
    // quitting after package initialization, without creating its lazy manager.
    var package=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Unity.2D.Animation.Runtime");
    var gpu=package.GetType("UnityEngine.U2D.Animation.GpuDeformationSystem",true);
    var fallback=gpu.GetField("s_FallbackBuffer",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
    var manager=package.GetType("UnityEngine.U2D.Animation.DeformationManager",true).GetField("s_Instance",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
    JObject Observe()=>new JObject{["fallbackPresent"]=fallback.GetValue(null)!=null,["fallbackValid"]=(fallback.GetValue(null) as ComputeBuffer)?.IsValid()??false,["managerPresent"]=(manager.GetValue(null) as UnityEngine.Object)!=null,["managedBytes"]=GC.GetTotalMemory(false),["unityAllocatedBytes"]=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),["graphicsDriverBytes"]=UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver(),["forcedGc"]=false};
    File.WriteAllText(Path.Combine(output,"buffer-owner-start.json"),C(Observe()),Utf8);
    Action onQuit=null;
    onQuit=()=>{Application.quitting-=onQuit;File.WriteAllText(Path.Combine(output,"buffer-owner-quitting.json"),C(Observe()),Utf8);};
    Application.quitting+=onQuit;
    File.WriteAllText(Path.Combine(output,"buffer-probe.json"),C(new JObject{["sourceHash"]=H(bundle),["supportsComputeShaders"]=SystemInfo.supportsComputeShaders,["graphicsDeviceType"]=SystemInfo.graphicsDeviceType.ToString(),["componentTypes"]=new JArray(types),["forcedGc"]=false,["battleStarted"]=false}),Utf8);
    Application.Quit(0);return;
   }
   if(Environment.GetCommandLineArgs().Contains("-ce03Continuity")){
    Ce03ContinuityPlayerTrial.Run(binding,Arg("-ce03Continuity"),output,H(bundle));sourcePin.Verify(source);Application.Quit(0);return;
   }
   if(Environment.GetCommandLineArgs().Contains("-ce03AcceptedQueue")){
    await Ce03AcceptedPlayerTrial.Run(binding,Arg("-ce03AcceptedQueue"),output,H(bundle));
    sourcePin.Verify(source);Application.Quit(0);return;
   }
   string raw=File.ReadAllText(Arg("-ce03Prepare"),Utf8);var prepared=JObject.Parse(raw);
   var shared=new Ce03SharedConsumerChecks(prepared);shared.Run(output,recoveryFixture.text);
   var results=shared.Results;var cases=shared.Cases;
   void Record(string id,string coverage,JObject observed){cases.Add(new JObject{["id"]=id,["coverage"]=coverage,["observed"]=observed});results.Add(new JObject{["caseId"]=id,["coverage"]=coverage,["status"]="PASS"});}
   var formation=Ce03PlayerAssetContract.Validate((JObject)prepared["playerAssetBinding"],(JObject)prepared["ownedSelection"]);
   string manifest=File.ReadAllText(Arg("-ce03PlayerAssetManifest"),Utf8);Check(manifest==binding.ManifestJson&&H(manifest)==Ce03PlayerAssetContract.ManifestHash,"Supplied Player manifest differs from built binding");
   using(var lease=binding.Acquire(Ce03PlayerAssetContract.ManifestHash,formation)){Check(lease.ReadAssets().ExteriorMap!=null,"Missing map");}
   bool rejected=false;try{using var invalid=binding.Acquire(new string('0',64),formation);}catch(InvalidOperationException){rejected=true;}Check(rejected,"Changed manifest accepted");
   Record("player-pinned-serialized-assets","PLAYER_ASSET_BINDINGS",new JObject{["manifestSha256"]=binding.ManifestHash,["tamperRejected"]=true});
   var scene=await CombatScenePlayerTrial.Run(binding,raw,Path.Combine(output,"scene-cycles"));var cycles=(JArray)scene["cycles"];
   Check(cycles.Count==3&&cycles.All(c=>(long)c["firstFrameTick"]==0),"First frame was advanced");
   Record("player-first-render-gate","PLAYER_FIRST_FRAME",new JObject{["cycles"]=cycles.DeepClone(),["renderMode"]=scene["renderMode"].DeepClone()});
   Check(cycles.All(c=>(bool)c["lateInputBlocked"]&&(int)c["peakVisibleHits"]>0&&(string)c["outcome"]!="Running"),"Incomplete rendered terminal evidence");
   Record("player-three-terminal-releases","PLAYER_EXIT_RELEASE",new JObject{["cycles"]=cycles.DeepClone(),["delayedDestructionChecked"]=true,["sharedAssetPreserved"]=true});
   sourcePin.Verify(source);using(var lease=binding.Acquire(Ce03PlayerAssetContract.ManifestHash,formation))lease.ReadAssets();
   Check(results.Count==13&&results.Select(r=>(string)r["coverage"]).Distinct().Count()==13,"Incomplete v6 coverage");
   string checkpoint=C(shared.CheckpointSchema),vectors=C(new JObject{["schemaVersion"]="ce03-executed-vectors-v1",["prepareSha256"]=prepared["prepareSha256"].DeepClone(),["actualAccountRun"]=false,["testSeed"]=1,["cases"]=cases});
   var report=new JObject{["schemaVersion"]="ce03-consumer-runner-report-v6",["scope"]="FULL_LOCAL_CONSUMER",["runId"]="player-"+Guid.NewGuid().ToString("N"),["simulationVersion"]=prepared["draft"]["simulationVersion"].DeepClone(),["draftSha256"]=H(C(prepared["draft"])),["algorithmSourceSha256"]=H(bundle),["vectorsSha256"]=H(vectors),["checkpointSchemaSha256"]=H(checkpoint),["commandContractSha256"]=H(C(prepared["commandContract"])),["playerAssetManifestSha256"]=binding.ManifestHash,["prepareSha256"]=prepared["prepareSha256"].DeepClone(),["results"]=results,["passed"]=results.Count,["failed"]=0,["skipped"]=0};
   File.WriteAllText(Path.Combine(output,"algorithm-source.json"),bundle,Utf8);File.WriteAllText(Path.Combine(output,"checkpoint-schema.json"),checkpoint,Utf8);File.WriteAllText(Path.Combine(output,"vectors.json"),vectors,Utf8);File.WriteAllText(Path.Combine(output,"runner-report-v6.json"),C(report),Utf8);Application.Quit(0);
  }catch(Exception error){if(ownsOutput)File.WriteAllText(Path.Combine(output,"failure.json"),C(new JObject{["type"]=error.GetType().Name,["message"]=error.Message,["admissionGranted"]=false}),Utf8);Debug.LogException(error);Application.Quit(1);}
 }
}
}
#endif
