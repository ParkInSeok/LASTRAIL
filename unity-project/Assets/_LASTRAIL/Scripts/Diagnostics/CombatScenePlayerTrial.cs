#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
namespace LastRail.Diagnostics
{
    // Actual rendered Player model checks before admission; no synthetic START or accepting verifier.
    public static class CombatScenePlayerTrial
    {
        static void Check(bool condition,string reason){if(!condition)throw new InvalidOperationException(reason);}
        static string ActorEffectsHash(string snapshot)
        {
            var state=JObject.Parse(snapshot);var result=new JObject();
            foreach(string key in new[]{"actors","statuses","effectJournal","attacks","skillReady","commands","rng","completedWaves"})result[key]=state[key]?.DeepClone();
            return Ce03CommandContract.Sha(result.ToString(Newtonsoft.Json.Formatting.None));
        }
        internal static void Capture(Camera camera,string path)
        {
            int width=Screen.width,height=Screen.height;var target=new RenderTexture(width,height,24);var previous=camera.targetTexture;var active=RenderTexture.active;Texture2D image=null;
            try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
            finally{camera.targetTexture=previous;RenderTexture.active=active;if(image!=null)UnityEngine.Object.Destroy(image);target.Release();UnityEngine.Object.Destroy(target);}
        }
        public static async Task<JObject> Run(CombatPlayerAssetBinding binding,string prepareJson,string requestedOutput)
        {
            Application.runInBackground=true;
            var restoreFrom=Environment.GetEnvironmentVariable("LASTRAIL_COMBAT_RESTORE_FROM");
            if(!string.IsNullOrEmpty(restoreFrom))return await RestoreInNewProcess(binding,prepareJson,requestedOutput,restoreFrom);
            string output=null;bool ownsOutput=false;SceneMemoryRecorder memory=null;CombatPlayerScene scene=null;GameObject inputObject=null;
            try
            {
                Application.runInBackground=true;Application.targetFrameRate=30;QualitySettings.vSyncCount=0;
                output=Path.GetFullPath(requestedOutput??throw new InvalidOperationException("Explicit output required"));
                Check(output.StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase)&&!Directory.Exists(output),"Fresh D QA output required");
                Directory.CreateDirectory(output);ownsOutput=true;
                var prepared=JObject.Parse(prepareJson);var commands=new Ce03CommandContract((JObject)prepared["commandContract"]);string owner=(string)prepared["ownedSelection"]["accountId"];
                commands.ValidatePrepared(prepared,owner,commands.Revision);
                var squad=Ce03PlayerAssetContract.Validate((JObject)prepared["playerAssetBinding"],(JObject)prepared["ownedSelection"]);
                inputObject=new GameObject("ProbeOwnedInput",typeof(EventSystem),typeof(InputSystemUIInputModule));
                var input=inputObject.GetComponent<EventSystem>();scene=new GameObject("ProbeOwnedScene").AddComponent<CombatPlayerScene>();
                memory=SceneMemoryRecorder.CreateExternal(output,true);var results=new JArray();
                using(var spectator=binding.Acquire(Ce03PlayerAssetContract.ManifestHash,squad))
                for(int cycle=0;cycle<3;cycle++)
                {
                    memory.BeginWindow();await Task.Delay(1000);memory.Checkpoint(cycle+"-before");
                    Func<BasicCombatSimulation> factory=()=>commands.CreateLocalTest(prepared,owner,commands.Revision,1);
                    string saved=null;long revision=0;CombatSceneResult result=null;Exception failure=null;
                    var driver=new CombatSceneDriver(factory,()=>null,(model,expected)=>{Check(expected==revision,"Checkpoint revision");saved=model.CaptureLocalState();return ++revision;},()=>true);driver.Open();
                    string initial=driver.StateJson;File.WriteAllText(Path.Combine(output,cycle+"-stage.txt"),"Model opened");
                    memory.BeginWindow();scene.Bind(driver,binding.Acquire(Ce03PlayerAssetContract.ManifestHash,squad),input,()=>true,r=>result=r,e=>failure=e);
                    File.AppendAllText(Path.Combine(output,cycle+"-stage.txt"),"; binding returned");Check(driver.StateJson==initial&&!driver.RecoveryRequired,"Binding changed model");
                    var ownedCamera=scene.SceneCamera;var ownedView=scene.View;var ownedRoot=ownedCamera.transform.parent.gameObject;
                    Capture(scene.SceneCamera,Path.Combine(output,cycle+"-first-frame.png"));
                    File.WriteAllText(Path.Combine(output,cycle+"-bound.json"),new JObject{["running"]=scene.IsRunning,["tick"]=driver.Tick,["recovery"]=driver.RecoveryRequired,["cameraActive"]=scene.SceneCamera!=null&&scene.SceneCamera.isActiveAndEnabled,["renderMode"]="EXPLICIT_OFFSCREEN_CAMERA_RENDER"}.ToString());
                    double deadline=Time.realtimeSinceStartupAsDouble+300;
                    while(!scene.IsRunning&&failure==null){Check(Time.realtimeSinceStartupAsDouble<deadline,"First frame timeout");await Task.Yield();}
                    if(failure!=null)throw failure;
                    Check(driver.Tick==0,"Simulation ran before first presentation");
                    
                    var speed=scene.View.GetComponentsInChildren<Button>().Single(b=>b.name=="Speed");
                    // Exercise the installed input component, without claiming physical touch/raycast QA.
                    var first=new PointerEventData(input){pointerId=1,button=PointerEventData.InputButton.Left};
                    var second=new PointerEventData(input){pointerId=2,button=PointerEventData.InputButton.Left};
                    speed.OnPointerDown(first);speed.OnPointerDown(second);speed.OnPointerUp(second);speed.OnPointerClick(second);Check(driver.StateJson==initial,"Second pointer executed speed");
                    speed.OnPointerUp(first);speed.OnPointerClick(first);Check(driver.StateJson!=initial,"First pointer did not execute speed");
                    string afterFirst=driver.StateJson;speed.OnPointerDown(first);speed.OnPointerExit(first);speed.OnPointerEnter(first);speed.OnPointerUp(first);speed.OnPointerClick(first);Check(driver.StateJson==afterFirst,"Canceled exit rearmed");
                    speed.OnPointerDown(first);scene.View.CancelPendingInput();speed.OnPointerUp(first);speed.OnPointerClick(first);Check(driver.StateJson==afterFirst,"Canceled gesture executed");
                    speed.OnPointerDown(second);speed.OnPointerUp(second);speed.OnPointerClick(second);
                    var staleClick=speed.onClick;bool hitSeen=false;int peakHits=0;string checkpointHash=null;
                    while(result==null&&failure==null)
                    {
                        Check(Time.realtimeSinceStartupAsDouble<deadline,"Battle terminal timeout");await Task.Delay(100);
                        int hits=((JArray)driver.HudState["hits"]).Count;peakHits=Math.Max(peakHits,hits);
                        if(!hitSeen&&hits>0){hitSeen=true;var checkpoint=driver.CaptureCommittedState().StateJson;File.WriteAllText(Path.Combine(output,cycle+"-checkpoint.json"),checkpoint);checkpointHash=Ce03CommandContract.Sha(checkpoint);memory.Checkpoint(cycle+"-first-hit",continueWindow:true);if(scene.SceneCamera!=null)Capture(scene.SceneCamera,Path.Combine(output,cycle+"-first-hit.png"));}
                    }
                    if(failure!=null)throw failure;
                    Check(result!=null&&hitSeen&&driver.Outcome!=BasicCombatOutcome.Running,"Missing actual hit/terminal");
                    Check(saved==driver.StateJson,"Published state not saved");string terminal=driver.StateJson;
                    staleClick.Invoke();Check(driver.StateJson==terminal,"Late input changed ended combat");
                    Check(scene.transform.childCount==0&&spectator.ReadAssets().ExteriorMap!=null,"Scene/shared asset lifetime");
                    memory.Checkpoint(cycle+"-terminal");
                    results.Add(new JObject{["cycle"]=cycle,["outcome"]=result.Outcome.ToString(),["tick"]=result.Tick,["peakVisibleHits"]=peakHits,["sourceReuse"]=cycle>0,["lateInputBlocked"]=true,["firstFrameTick"]=0,["terminalStateSha256"]=Ce03CommandContract.Sha(terminal),["checkpointSha256"]=checkpointHash,["actorEffectsSha256"]=ActorEffectsHash(terminal),["resolvedHitsSha256"]=Ce03CommandContract.Sha(driver.HudState["hits"].ToString(Newtonsoft.Json.Formatting.None))});
                    driver=null;saved=null;initial=null;terminal=null;staleClick=null;speed=null;factory=null;
                    await Task.Delay(100);Check(ownedCamera==null&&ownedView==null&&ownedRoot==null,"Owned presentation objects survived delayed destruction");ownedCamera=null;ownedView=null;ownedRoot=null;
                    memory.BeginWindow();await Task.Delay(1000);Check(spectator.ReadAssets().ExteriorMap!=null,"Shared asset destroyed");memory.Checkpoint(cycle+"-after");
                    File.WriteAllText(Path.Combine(output,"progress.json"),results.ToString());
                }
                var summary=new JObject{["renderMode"]="EXPLICIT_OFFSCREEN_CAMERA_RENDER_NOT_VISIBLE_WINDOW_QA",["status"]="Passed",["scope"]="PRE_ADMISSION_REAL_PLAYER_SCENE_FROM_SERVER_PREPARE",["cycles"]=results,
                    ["manifestHash"]=binding.ManifestHash,["prepareSha256"]=prepared["prepareSha256"],["startSent"]=false,["admissionGranted"]=false,["httpTransportComposed"]=false,
                    ["pointerInput"]="INSTALLED_COMPONENT_CALLBACKS_FIRST_OWNER_EXIT_CANCEL_NEW_DOWN_NOT_OS_OR_RAYCAST_QA",["checkpointStorage"]="IN_MEMORY_OWNER_WITH_EXPORTED_QA_CHECKPOINT_NOT_PRODUCT_DURABLE_STORE",["processId"]=System.Diagnostics.Process.GetCurrentProcess().Id,["claimTested"]=false,["forcedGc"]=false,["frameRate"]=30,["gameSpeedHalfUnits"]=4};
                File.WriteAllText(Path.Combine(output,"result.json"),summary.ToString());return summary;
            }
            catch(Exception error){if(ownsOutput)File.WriteAllText(Path.Combine(output,"failure.txt"),error.ToString());throw;}
            finally{if(scene!=null){scene.Close();UnityEngine.Object.Destroy(scene.gameObject);}if(memory!=null)UnityEngine.Object.Destroy(memory.gameObject);if(inputObject!=null)UnityEngine.Object.Destroy(inputObject);}
        }

        static async Task<JObject> RestoreInNewProcess(CombatPlayerAssetBinding binding,string prepareJson,string requestedOutput,string restoreFrom)
        {
            var input=Path.GetFullPath(restoreFrom);var output=Path.GetFullPath(requestedOutput??throw new ArgumentException("Explicit output required"));
            const string qaRoot="D:\\Project\\LASTRAIL\\QA\\";
            Check(input.StartsWith(qaRoot,StringComparison.OrdinalIgnoreCase)&&output.StartsWith(qaRoot,StringComparison.OrdinalIgnoreCase)&&!Directory.Exists(output),"Existing source and fresh D QA output required");
            var prior=JObject.Parse(File.ReadAllText(Path.Combine(input,"result.json")));
            var prepared=JObject.Parse(prepareJson);var commands=new Ce03CommandContract((JObject)prepared["commandContract"]);string owner=(string)prepared["ownedSelection"]["accountId"];
            commands.ValidatePrepared(prepared,owner,commands.Revision);
            int processId=System.Diagnostics.Process.GetCurrentProcess().Id;
            Check((string)prior["status"]=="Passed"&&(string)prior["manifestHash"]==binding.ManifestHash&&JToken.DeepEquals(prior["prepareSha256"],prepared["prepareSha256"])&&(int)prior["processId"]!=processId,"Same source and separate Player process required");
            Directory.CreateDirectory(output);var results=new JArray();
            try
            {
                foreach(var expected in (JArray)prior["cycles"])
                {
                    int cycle=(int)expected["cycle"];string checkpoint=File.ReadAllText(Path.Combine(input,cycle+"-checkpoint.json"));
                    Check(Ce03CommandContract.Sha(checkpoint)==(string)expected["checkpointSha256"],"Exported checkpoint hash mismatch");
                    var model=BasicCombatSimulation.RestoreLocalState(checkpoint,()=>commands.CreateLocalTest(prepared,owner,commands.Revision,1));
                    Check(model.CaptureLocalState()==checkpoint&&model.Tick>0&&model.Outcome==BasicCombatOutcome.Running,"Exact running restore required");
                    long restoredTick=model.Tick;
                    for(int step=0;step<20000&&model.Outcome==BasicCombatOutcome.Running;step++)
                    {
                        model.Advance(500000,8);
                        if(step%100==0)await Task.Yield();
                    }
                    string hits=Ce03CommandContract.Sha(model.ReadHudState()["hits"].ToString(Newtonsoft.Json.Formatting.None));
                    Check(model.Outcome.ToString()==(string)expected["outcome"]&&model.Tick==(long)expected["tick"]&&hits==(string)expected["resolvedHitsSha256"],"Restored outcome/tick/resolved damage history diverged");
                    string actorEffects=ActorEffectsHash(model.CaptureLocalState());
                    Check(actorEffects==(string)expected["actorEffectsSha256"],"Restored HP/effects/actions/random/wave state diverged");
                    results.Add(new JObject{["cycle"]=cycle,["restoredTick"]=restoredTick,["terminalTick"]=model.Tick,["outcome"]=model.Outcome.ToString(),["resolvedHitsSha256"]=hits,["actorEffectsSha256"]=actorEffects});
                }
                Check(results.Count==3,"Three restored combats required");
                var result=new JObject{["status"]="Passed",["scope"]="NEW_PROCESS_EXPORTED_LOCAL_MODEL_RESTORE_NOT_PRODUCT_START_OR_DURABLE_STORE",["sourceProcessId"]=prior["processId"],["processId"]=processId,["cycles"]=results,["httpTransportComposed"]=false,["startSent"]=false};
                File.WriteAllText(Path.Combine(output,"result.json"),result.ToString());return result;
            }
            catch(Exception error){File.WriteAllText(Path.Combine(output,"failure.txt"),error.ToString());throw;}
        }
    }
}
#endif



