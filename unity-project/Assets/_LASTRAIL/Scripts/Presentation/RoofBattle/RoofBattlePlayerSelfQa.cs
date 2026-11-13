#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Presentation.RoofBattle {
 // Development-only acceptance probe. It invokes actual UI/controller commands, never fabricates receipts.
 public sealed class RoofBattlePlayerSelfQa : MonoBehaviour {
  RoofBattleLocalScene scene; string directory; bool complete; float began;
  readonly JArray checks=new JArray(); Exception failure;
  public static string RequestedDirectory {
   get { const string prefix="--roof-qa-dir=";return Environment.GetCommandLineArgs().FirstOrDefault(a=>a.StartsWith(prefix,StringComparison.Ordinal))?.Substring(prefix.Length); }
  }
  public static string RequestedStoreDirectory { get {const string prefix="--roof-store-dir=";return Environment.GetCommandLineArgs().FirstOrDefault(a=>a.StartsWith(prefix,StringComparison.Ordinal))?.Substring(prefix.Length);} }
  public static bool RestoreOnly=>Environment.GetCommandLineArgs().Contains("--roof-qa-restore-only");
  public void Initialize(RoofBattleLocalScene source,string requested) {
   scene=source??throw new ArgumentNullException(nameof(source));began=Time.realtimeSinceStartup;
   try{directory=ValidateDirectory(requested);}catch(Exception error){failure=error;directory="D:/Project/LASTRAIL/QA/FiveAgentVisual-20261017/RoofQaFailure-"+Guid.NewGuid().ToString("N");}
  }
  public static string ValidateDirectory(string value) {
   if(string.IsNullOrWhiteSpace(value)||!Path.IsPathRooted(value))throw new ArgumentException("Explicit absolute D project QA directory required.");
   string full=Path.GetFullPath(value).Replace('\\','/').TrimEnd('/');
   if(!full.StartsWith("D:/Project/LASTRAIL/QA/",StringComparison.OrdinalIgnoreCase)||full.Contains("/../"))throw new ArgumentException("Roof QA outputs must remain under D:/Project/LASTRAIL/QA/.");
   return full;
  }
  void Check(string name,bool value,JToken evidence=null) {
   checks.Add(new JObject{["name"]=name,["passed"]=value,["evidence"]=evidence?.DeepClone()??JValue.CreateNull()});
   if(!value)throw new InvalidOperationException("Roof Player QA failed: "+name);
  }
  static long Tick(JObject s)=>(long)s["tick"];
  static int Hp(JObject s)=>(int)s["hp"]["enemy"];
  static long Cursor(JObject s)=>(long)s["cursor"];
  void RequireCommand(string label) {Check(label,scene.LastCommandSucceeded,new JObject{["error"]=scene.LastError});}
  void CheckComposition(){Check("explicit D diagnostic output supplied",Environment.GetEnvironmentVariable("LASTRAIL_LOCAL_QA_DIAGNOSTICS_DIRECTORY")==directory+"/diagnostics");var m=scene.Metadata();Check("zero Basic CE03 roots and one roof scene/native owner",(int)m["basicCe03RootCount"]==0&&(int)m["roofSceneCount"]==1&&(int)m["ownedNativeOwnerCount"]==1,m);}
  void Probe() {
   scene.SetAutoAdvance(false);
   Check("explicit LOCAL source opened",scene.Ready,scene.Metadata());
   CheckComposition();var initial=scene.Snapshot;
   Check("fresh original LOCAL context",Tick(initial)==0&&Cursor(initial)==0&&Hp(initial)==700,initial);
   Check("first supported LOCAL actor only",scene.View.NeutralActorCount==1&&scene.View.NeutralActorId=="op_eve"&&scene.View.NeutralSlot==2,scene.Metadata());
   Check("distance-only neutral body and no spatial FX",scene.View.SpatialFxCount==0&&scene.View.BodyPhase=="NEUTRAL_IDLE_NO_WORLD_ATTACK_PHASE",scene.Metadata());
   scene.View.QaClickAdvance();RequireCommand("UI Advance 1ms accepted");var first=scene.Snapshot;
   Check("first original execution and HIT",Tick(first)==1&&Cursor(first)==2&&Hp(first)==675&&scene.ExecutionCount==1&&scene.HitCount==1,first);
   scene.View.QaClickPause();RequireCommand("UI Pause accepted");long held=Tick(scene.Snapshot);
   scene.View.QaClickAdvance();RequireCommand("held UI Advance accepted");var paused=scene.Snapshot;
   Check("pause holds native clock HP and cursor",Tick(paused)==held&&Cursor(paused)==2&&Hp(paused)==675&&(bool)paused["nativePaused"],paused);
   scene.View.QaClickPause();RequireCommand("UI Resume accepted");Check("resume clears native pause",!(bool)scene.Snapshot["nativePaused"]);
   scene.View.QaClickSpeed();RequireCommand("UI speed 2x accepted");long before=Tick(scene.Snapshot);
   scene.View.QaClickAdvance();RequireCommand("speed UI Advance accepted");
   Check("native speed changes original clock",Tick(scene.Snapshot)==before+2,scene.Snapshot);
   scene.View.QaClickSpeed();RequireCommand("UI speed half-units 2 restored");
   var clock=scene.Snapshot["nativeSnapshot"]?["state"]?["direct"]?["clock"];
   Check("native speed restored to half-units 2",(int?)clock?["speedHalfUnits"]==2,clock);
   long remaining=2000-Tick(scene.Snapshot);Check("prepare tick 2000 requires forward steps",remaining>0);
   scene.AdvanceManual(checked(remaining*10000),checked((int)remaining));RequireCommand("same controller Advance reaches tick 2000");
   var at2000=scene.Snapshot;
   Check("second opportunity not consumed early",Tick(at2000)==2000&&Cursor(at2000)==2&&Hp(at2000)==675,at2000);
   long pulsesBefore=scene.PublishedEventCount;
   scene.QaFailNextSave();scene.View.QaClickAdvance();var failed=scene.Snapshot;
   Check("QA IOException reaches actual save boundary",!scene.LastCommandSucceeded&&scene.LastException is IOException,new JObject{["error"]=scene.LastError});
   Check("unsaved second HIT suppressed and previous state held",Tick(failed)==2000&&Cursor(failed)==2&&Hp(failed)==675&&(bool)failed["recoveryRequired"]&&scene.PublishedEventCount==pulsesBefore,failed);
   scene.View.QaClickRestore();RequireCommand("UI Restore original context accepted");var restored=scene.Snapshot;
   Check("restore checkpoint without historical pulse",Tick(restored)==2000&&Cursor(restored)==2&&Hp(restored)==675&&scene.LastDeliveredEventCount==0&&scene.PublishedEventCount==pulsesBefore&&!((bool)restored["recoveryRequired"]),restored);
   scene.View.QaClickAdvance();RequireCommand("UI retry 1ms accepted");var second=scene.Snapshot;
   Check("real second opportunity and HIT committed once",Tick(second)==2001&&Cursor(second)==4&&Hp(second)==650&&scene.ExecutionCount==2&&scene.HitCount==2&&scene.PublishedEventCount==pulsesBefore+2,second);
   Check("spatial effects remain suppressed after both original HITs",scene.View.SpatialFxCount==0&&scene.View.NeutralActorCount==1,scene.Metadata());
   var counts=scene.ExecutionCount;long publications=scene.PublishedEventCount;
   scene.enabled=false;Check("disable clears only owned visual",scene.View.VisibleLayers==0);
   scene.enabled=true;
   Check("same-context rebind keeps snapshot and does not replay events",scene.ExecutionCount==counts&&scene.PublishedEventCount==publications&&scene.LastDeliveredEventCount==0&&scene.View.NeutralActorCount==1,scene.Snapshot);
  }
  void ProbeRestoreOnly(){
   scene.SetAutoAdvance(false);Check("restore-only source opened",scene.Ready,scene.Metadata());CheckComposition();var state=scene.Snapshot;
   Check("second process restores original committed second HIT",Tick(state)==2001&&Cursor(state)==4&&Hp(state)==650,state);
   Check("initial snapshot emits no historical events",scene.LastDeliveredEventCount==0&&scene.PublishedEventCount==0&&scene.ExecutionCount==2&&scene.HitCount==2,scene.Metadata());
   Check("restore-only neutral body without spatial FX",scene.View.NeutralActorCount==1&&scene.View.SpatialFxCount==0&&scene.View.VisibleLayers==1,scene.Metadata());
  }
  IEnumerator Start() {
   yield return null;
   if(scene==null){failure=new InvalidOperationException("QA scene was not initialized.");Finish();yield break;}
   try { if(failure!=null)throw failure;if(RestoreOnly)ProbeRestoreOnly();else Probe();scene.View?.SetQaStatus("PLAYER CHECKS PASS — capture pending"); }
   catch(Exception error){failure=error;scene.View?.SetQaStatus("PLAYER CHECKS FAILED: "+error.Message);}
   yield return null;yield return new WaitForEndOfFrame();
   string screenshot=Path.Combine(directory,"roof-player.png");
   string captureError=null;
   Application.LogCallback captureLog=(message,stack,type)=>{if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)captureError=message;};
   Application.logMessageReceived+=captureLog;
   try {
    Directory.CreateDirectory(directory);
    if(failure==null){Debug.ClearDeveloperConsole();Debug.developerConsoleVisible=false;} // Expected injected error remains in the log/checks, not over the acceptance image.
    ScreenCapture.CaptureScreenshot(screenshot);
   }catch(Exception error){failure=failure==null?error:new AggregateException(failure,error);}
   float captureDeadline=Time.realtimeSinceStartup+15;Texture2D image=null;bool loaded=false;
   while(!complete&&captureError==null&&Time.realtimeSinceStartup<captureDeadline&&!loaded){
    yield return null;
    if(!File.Exists(screenshot))continue;
    try{
     if(image==null)image=new Texture2D(2,2,TextureFormat.RGB24,false);
     loaded=image.LoadImage(File.ReadAllBytes(screenshot));
    }catch(IOException){ /* Native screenshot writer may still own the fresh file. Retry within the same bounded capture. */ }
   }
   try{
    if(captureError!=null)throw new InvalidOperationException("Player screenshot capture logged an error: "+captureError);
    if(!loaded)throw new TimeoutException("Native Player screenshot was not readable within 15 seconds.");
    Check("actual Player screenshot dimensions match framebuffer",image.width==Screen.width&&image.height==Screen.height);
    var pixels=image.GetPixels32();var first=pixels[0];
    Check("actual Player framebuffer has rendered content",pixels.Any(p=>!p.Equals(first)));
   }catch(Exception error){failure=failure==null?error:new AggregateException(failure,error);}
   finally{Application.logMessageReceived-=captureLog;if(image!=null)Destroy(image);}   Finish();
  }
  void Update(){if(!complete&&scene!=null&&Time.realtimeSinceStartup-began>45){failure=new TimeoutException("Roof QA exceeded bounded 45 second frame deadline.");Finish();}}
  void Finish(){
   if(complete)return;complete=true;
   try {
    if(string.IsNullOrWhiteSpace(directory))directory="D:/Project/LASTRAIL/QA/FiveAgentVisual-20261017/RoofQaFailure-"+Guid.NewGuid().ToString("N");
    Directory.CreateDirectory(directory);
    var report=new JObject{["version"]="unity.roof-local-player-qa.v1.20261017",["status"]=failure==null?"PASS":"FAIL",["classification"]="LOCAL_NOT_OPERATIONAL",["restoreOnly"]=RestoreOnly,["persistentDataPath"]=Application.persistentDataPath,["explicitLocalDiagnosticsDirectory"]=Environment.GetEnvironmentVariable("LASTRAIL_LOCAL_QA_DIAGNOSTICS_DIRECTORY"),["checks"]=checks,["error"]=failure?.ToString(),["state"]=scene?.SnapshotOrNull(),["metadata"]=scene?.Metadata(),["screen"]=new JArray(Screen.width,Screen.height),["safeArea"]=new JArray(Screen.safeArea.x,Screen.safeArea.y,Screen.safeArea.width,Screen.safeArea.height),["screenshot"]=File.Exists(Path.Combine(directory,"roof-player.png"))?"roof-player.png":null};
    WriteNew(Path.Combine(directory,"roof-player-qa.json"),System.Text.Encoding.UTF8.GetBytes(report.ToString()));
   }catch(Exception error){Debug.LogError("Roof QA report failure: "+error);failure=failure??error;}
   if(failure!=null)Debug.LogError(failure);Application.Quit(failure==null?0:1);
  }
  static void WriteNew(string file,byte[] bytes){using(var stream=new FileStream(file,FileMode.CreateNew,FileAccess.Write,FileShare.Read))stream.Write(bytes,0,bytes.Length);}
 }
}
#endif