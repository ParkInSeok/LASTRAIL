#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
namespace LastRail.Diagnostics {
 // Opt-in development instrumentation; no game state or transport authority.
 public sealed class SceneMemoryRecorder:MonoBehaviour {
  string output;ProfilerRecorder allocations;long peakUsed,peakManaged,peakGraphics,allocatedFrames;int frames;bool measuring;
  readonly List<string> checkpoints=new List<string>();
  bool externalScenario,detailedInventory=true;
  public static SceneMemoryRecorder CreateExternal(string directory,bool inventory=true){
   string path=Path.GetFullPath(directory);if(!path.StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("D QA output required");
   Directory.CreateDirectory(path);var recorder=new GameObject("OwnedDataLifecycleMemoryRecorder").AddComponent<SceneMemoryRecorder>();recorder.output=path;recorder.externalScenario=true;recorder.detailedInventory=inventory;
   recorder.allocations=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);recorder.WriteEnvironment();return recorder;
  }
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Install(){
   string arg=Environment.GetCommandLineArgs().FirstOrDefault(s=>s.StartsWith("--scene-memory-qa="));if(arg==null)return;
   string path=Path.GetFullPath(arg.Substring("--scene-memory-qa=".Length));
   if(!path.StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("D project QA output required.");
   var go=new GameObject("OptInSceneMemoryRecorder");DontDestroyOnLoad(go);go.AddComponent<SceneMemoryRecorder>().output=path;
  }
  public string Output=>output;
  IEnumerator Start(){
   if(externalScenario)yield break;
   Directory.CreateDirectory(output);Application.runInBackground=true;Application.targetFrameRate=60;
   allocations=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
   WriteEnvironment();
   var stack=new Stack<IEnumerator>();stack.Push(Run());Exception error=null;
   while(stack.Count>0){var current=stack.Peek();bool more;try{more=current.MoveNext();}catch(Exception failure){error=failure;break;}if(!more){stack.Pop();continue;}if(current.Current is IEnumerator nested){stack.Push(nested);continue;}yield return current.Current;}
   measuring=false;Write("result.json",new JObject{["status"]=error==null?"Passed":"Failed",["error"]=error?.ToString(),["checkpoints"]=new JArray(checkpoints),["fullRunMeasured"]=false,["choiceClaimPeaksMeasured"]=false,["externalHttpCountMeasured"]=false});allocations.Dispose();Application.Quit(error==null?0:1);
  }
  void WriteEnvironment(){
   Write("environment.json",new JObject{["unity"]=Application.unityVersion,["buildGuid"]=Application.buildGUID,["development"]=Debug.isDebugBuild,["os"]=SystemInfo.operatingSystem,["cpu"]=SystemInfo.processorType,["systemMemoryMB"]=SystemInfo.systemMemorySize,["gpu"]=SystemInfo.graphicsDeviceName,["graphicsAPI"]=SystemInfo.graphicsDeviceType.ToString(),["graphicsMemoryMBReported"]=SystemInfo.graphicsMemorySize,["screen"]=new JArray(Screen.width,Screen.height),["quality"]=QualitySettings.GetQualityLevel(),["vSync"]=QualitySettings.vSyncCount,["targetFps"]=Application.targetFrameRate,["profilerEnabled"]=Profiler.enabled,["gcFrameCounterValid"]=allocations.Valid,["forcedGC"]=false,["unloadUnusedAssets"]=false,["previousComparableBaseline"]=false,["scope"]=externalScenario?"EXPLICIT_EXTERNAL_SCENARIO_SEE_RUN_RESULT":"REAL_BOOTSTRAP_OBSERVATION_THEN_ISOLATED_LOBBY_DISPLAY_AND_EVE_LOCAL_NOT_AUTHENTICATED_FULL_RUN",["measurementCost"]="Development build, per-frame counters, checkpoint inventory/JSON allocation; no deep managed-reference heap snapshot. Inventory memory can affect later GC/reserved measurements.", ["metricRule"]="Unity used/reserved, managed, process and estimated graphics overlap; do not sum. Frame sampled peaks may miss intra-frame peaks."});
  }
  IEnumerator Run(){BeginWindow();yield return new WaitForSecondsRealtime(15);Checkpoint("00-bootstrap-observed-15s");
   BeginWindow();yield return SceneManager.LoadSceneAsync(1,LoadSceneMode.Single);Checkpoint("00b-diagnostic-scene-loaded");var scenario=UnityEngine.Object.FindFirstObjectByType<SceneMemoryLifecycleProbe>();if(scenario==null)throw new InvalidOperationException("Memory scenario not present.");yield return scenario.Run(this);
  }
  void Update(){if(!measuring)return;frames++;peakUsed=Math.Max(peakUsed,Profiler.GetTotalAllocatedMemoryLong());peakManaged=Math.Max(peakManaged,Profiler.GetMonoUsedSizeLong());peakGraphics=Math.Max(peakGraphics,Profiler.GetAllocatedMemoryForGraphicsDriver());if(allocations.Valid)allocatedFrames+=allocations.LastValue;}
  public void BeginWindow(){peakUsed=peakManaged=peakGraphics=allocatedFrames=0;frames=0;measuring=true;}
  public void Checkpoint(string stage,bool includeSampledWindow=true,bool continueWindow=false){
   long allocatedBefore=GC.GetAllocatedBytesForCurrentThread();double started=Time.realtimeSinceStartupAsDouble;
   bool wasMeasuring=measuring;measuring=false;using var process=System.Diagnostics.Process.GetCurrentProcess();process.Refresh();
   var metrics=new JObject{["stage"]=stage,["elapsedSeconds"]=Time.realtimeSinceStartupAsDouble,["frame"]=Time.frameCount,["unityUsedBytes"]=Profiler.GetTotalAllocatedMemoryLong(),["unityReservedBytes"]=Profiler.GetTotalReservedMemoryLong(),["unityUnusedReservedBytes"]=Profiler.GetTotalUnusedReservedMemoryLong(),["managedUsedBytes"]=Profiler.GetMonoUsedSizeLong(),["managedReservedBytes"]=Profiler.GetMonoHeapSizeLong(),["graphicsEstimatedBytes"]=Profiler.GetAllocatedMemoryForGraphicsDriver(),["processPrivateBytes"]=process.PrivateMemorySize64>0?(JToken)new JValue(process.PrivateMemorySize64):JValue.CreateNull(),["processWorkingSetBytes"]=process.WorkingSet64>0?(JToken)new JValue(process.WorkingSet64):JValue.CreateNull(),["gcCollections"]=new JArray(GC.CollectionCount(0),GC.CollectionCount(1),GC.CollectionCount(2)),["sampledWindowFrames"]=frames,["sampledPeakUnityUsedBytes"]=peakUsed,["sampledPeakManagedBytes"]=peakManaged,["sampledPeakGraphicsEstimatedBytes"]=peakGraphics,["sampledFrameAllocatedBytes"]=allocations.Valid?(JToken)new JValue(allocatedFrames):JValue.CreateNull()};
   metrics["pacing"]=new JObject{["targetFrameRate"]=Application.targetFrameRate,["vSync"]=QualitySettings.vSyncCount,["captureFramerate"]=Time.captureFramerate,["captureDeltaTime"]=Time.captureDeltaTime,["timeScale"]=Time.timeScale,["deltaTime"]=Time.unscaledDeltaTime,["focused"]=Application.isFocused};
   metrics["sampledWindowIncluded"]=includeSampledWindow;
   if(!includeSampledWindow)foreach(string key in new[]{"sampledWindowFrames","sampledPeakUnityUsedBytes","sampledPeakManagedBytes","sampledPeakGraphicsEstimatedBytes","sampledFrameAllocatedBytes"})metrics[key]=JValue.CreateNull();
   metrics["inventoryEnabled"]=detailedInventory;
   if(detailedInventory)metrics["assets"]=SceneMemoryAssetInventory.Capture();
   long allocatedAfter=GC.GetAllocatedBytesForCurrentThread();
   metrics["threadAllocationCounterAvailable"]=allocatedAfter>allocatedBefore;
   metrics["checkpointConstructionThreadAllocatedBytes"]=allocatedAfter>allocatedBefore?(JToken)new JValue(allocatedAfter-allocatedBefore):JValue.CreateNull();
   metrics["checkpointConstructionMilliseconds"]=(Time.realtimeSinceStartupAsDouble-started)*1000;
   metrics["checkpointCostScope"]="Current-thread construction only; excludes final JSON serialization/file write. This overhead may affect subsequent natural GC.";
   Write(stage+".json",metrics);checkpoints.Add(stage);measuring=continueWindow&&wasMeasuring;
  }
  void Write(string name,JObject value)=>File.WriteAllText(Path.Combine(output,name),value.ToString());
  void OnDestroy(){if(allocations.Valid)allocations.Dispose();}
 }
}
#endif
