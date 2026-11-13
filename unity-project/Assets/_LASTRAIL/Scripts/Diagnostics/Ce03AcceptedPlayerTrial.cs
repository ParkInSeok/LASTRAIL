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
namespace LastRail.Diagnostics
{
    // Trusted disposable-host QA queue; never creates an admission or substitutes a START.
    // The host retains HTTP/economic authority. Each entry supplies its exact original identity.
    public static class Ce03AcceptedPlayerTrial
    {
        static void Check(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
        static string C(JToken value)=>Ce03CommandContract.Canonical(value);
        static string H(string value)=>Ce03CommandContract.Sha(value);
        internal static void Atomic(string path,string value){
            string pending=path+".pending";var bytes=new System.Text.UTF8Encoding(false,true).GetBytes(value);
            using(var s=new FileStream(pending,FileMode.Create,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){s.Write(bytes,0,bytes.Length);s.Flush(true);}
            if(File.Exists(path))File.Replace(pending,path,null);else File.Move(pending,path);
            Check(File.ReadAllText(path)==value,"QA durable readback differs");
        }
        static async Task WaitFor(string path){double deadline=Time.realtimeSinceStartupAsDouble+300;while(!File.Exists(path)){Check(Time.realtimeSinceStartupAsDouble<deadline,"Host entry timeout");await Task.Delay(100);}}
        public static async Task Run(CombatPlayerAssetBinding binding,string queue,string output,string sourceHash)
        {
            queue=Path.GetFullPath(queue);Check(queue.StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase),"Explicit D QA queue required");
            var config=JObject.Parse(File.ReadAllText(Path.Combine(queue,"config.json")));
            string owner=(string)config["owner"],mode=(string)config["mode"];var origin=new Uri((string)config["origin"]+"/");
            int cycles=(int)config["cycles"];Check((mode=="normal"&&cycles==3)||((mode=="single"||mode=="pause"||mode=="resume"||mode=="journal-pause"||mode=="journal-checkpoint-pause"||mode=="journal-resume")&&cycles==1),"Explicit normal/recovery scenario required");
            var inputObject=new GameObject("AcceptedOwnedInput",typeof(EventSystem),typeof(InputSystemUIInputModule));
            var sceneObject=new GameObject("AcceptedOwnedScene");var scene=sceneObject.AddComponent<CombatPlayerScene>();
            var memory=SceneMemoryRecorder.CreateExternal(output,true);long generation=0;string activeRun=null,activeKey=null;Ce03PlayerBattleController controller=null;Ce03PlayerRunOwner runOwner=null;
            var results=new JArray();
            var releasedControllers=new System.Collections.Generic.List<WeakReference>();
            var releasedViews=new System.Collections.Generic.List<WeakReference>();
            try{for(int i=0;i<cycles;i++){
                Ce03LocalClient departureClient=null;
                if((mode=="single"||mode=="normal"||mode.StartsWith("journal-",StringComparison.Ordinal))&&File.Exists(Path.Combine(queue,"ui.private.json")))
                    if((departureClient=await Ce03DeparturePlayerTrial.CreateEntry(binding,queue,i))==null)return;
                await WaitFor(Path.Combine(queue,"entry-"+i+".json"));var entry=JObject.Parse(File.ReadAllText(Path.Combine(queue,"entry-"+i+".json")));
                // Fixed index-derived storage locations; untrusted JSON cannot redirect filesystem writes.
                string runFolder=Path.Combine(queue,"run-"+i);string originalRaw=File.ReadAllText(Path.Combine(runFolder,"original-start.json"));
                var original=new Ce03OriginalStart(originalRaw,owner,origin);
                Check(original.IsPlayer&&original.RunId==(string)entry["runId"]&&original.Key==(string)entry["key"],"Exact run/key owner mismatch");
                var response=JObject.Parse((string)original.Copy()["responseRaw"]);
                Check((string)response["combat"]["profile"]["baseProfile"]["executionEvidence"]["algorithmSourceSha256"]==sourceHash,"Accepted source differs from running build");
                string checkpoint=Path.Combine(runFolder,"checkpoint.json");var store=CombatCheckpointStore.CreateExplicitLocalQaFile(checkpoint);var factory=original.CreateVerifiedPlayerFactory(binding);
                long currentGeneration=++generation;activeRun=original.RunId;activeKey=original.Key;
                Func<bool> current=()=>generation==currentGeneration&&activeRun==original.RunId&&activeKey==original.Key;
                memory.BeginWindow();await Task.Delay(1000);memory.Checkpoint(i+"-before");
                if(mode=="journal-resume"){
                    string checkpointStop=new[]{"checkpoint-saved.json","cleanup-stopped.json"}.Select(name=>Path.Combine(runFolder,name)).FirstOrDefault(File.Exists);
                    if(checkpointStop!=null){var stopped=JObject.Parse(File.ReadAllText(checkpointStop));var durable=store.RestoreLocalStartWithRevision(original,factory);
                        Check(durable!=null&&durable.Revision==(long)stopped["revision"]&&H(durable.Simulation.CaptureLocalState())==(string)stopped["stateHash"],"Saved checkpoint disappeared or changed before resumed owner opened");}
                }
                bool combatAllowed=true;
                try{if(departureClient!=null){runOwner=new Ce03PlayerRunOwner(original,binding,store,departureClient,current,()=>combatAllowed);controller=runOwner.Resolve(original);Check(ReferenceEquals(controller,runOwner.Resolve(new Ce03OriginalStart(originalRaw,owner,origin))),"Recovery and Continue created two controllers");}else controller=new Ce03PlayerBattleController(original,binding,store,current);}
                catch(IOException) when(mode=="journal-pause"&&departureClient?.AwaitingCheckpointKey==original.Key){
                    var durable=store.RestoreLocalStartWithRevision(original,factory);Check(durable!=null,"Cleanup stop requires a durable checkpoint");
                    Atomic(Path.Combine(runFolder,"cleanup-stopped.json"),C(new JObject{["processId"]=System.Diagnostics.Process.GetCurrentProcess().Id,["key"]=original.Key,["runId"]=original.RunId,["revision"]=durable.Revision,["stateHash"]=H(durable.Simulation.CaptureLocalState())}));return;
                }
                departureClient=null;
                var before=store.RestoreLocalStartWithRevision(original,factory);Check(before!=null,"Initial checkpoint was not durably saved");
                string beforeState=before.Simulation.CaptureLocalState();long firstTick=before.Simulation.Tick;
                if(mode=="journal-checkpoint-pause"){
                    Atomic(Path.Combine(runFolder,"checkpoint-saved.json"),C(new JObject{["processId"]=System.Diagnostics.Process.GetCurrentProcess().Id,["key"]=original.Key,["runId"]=original.RunId,["revision"]=before.Revision,["stateHash"]=H(beforeState)}));return;
                }
                if(mode=="resume"){
                    Check(before.Simulation.Outcome==BasicCombatOutcome.Running,"Recovery must resume an unfinished combat");
                    var paused=JObject.Parse(File.ReadAllText(Path.Combine(runFolder,"paused.json")));
                    Check((int)paused["processId"]!=System.Diagnostics.Process.GetCurrentProcess().Id,"Recovery must use a new process");
                    Check((string)paused["runId"]==original.RunId&&(string)paused["key"]==original.Key&&(string)paused["snapshotHash"]==original.SnapshotHash&&(long)paused["revision"]==before.Revision&&(string)paused["stateHash"]==H(beforeState),"New process original/state/revision differs");
                    Atomic(Path.Combine(output,"restored.json"),C(new JObject{["priorProcessId"]=paused["processId"],["processId"]=System.Diagnostics.Process.GetCurrentProcess().Id,["runId"]=original.RunId,["key"]=original.Key,["revision"]=before.Revision,["tick"]=firstTick,["stateHash"]=H(beforeState),["snapshotHash"]=original.SnapshotHash}));
                }else Check(firstTick==0&&before.Revision==1,"Fresh run reused another checkpoint");
                CombatSceneResult result=null;Exception failure=null;int completionCallbacks=0;
                Action<CombatSceneResult> completed=r=>{result=r;completionCallbacks++;};Action<Exception> failed=e=>failure=e;
                memory.BeginWindow();
                if(runOwner!=null){Check(runOwner.Present(original,scene,inputObject.GetComponent<EventSystem>(),completed,failed)==Ce03BattleEntry.Presented,"Initial presentation unavailable");Check(runOwner.Present(original,scene,inputObject.GetComponent<EventSystem>(),completed,failed)==Ce03BattleEntry.Joined,"Duplicate entry rebound presentation");}
                else controller.Present(scene,inputObject.GetComponent<EventSystem>(),completed,failed);
                var camera=scene.SceneCamera;var view=scene.View;var root=camera.transform.parent.gameObject;
                CombatScenePlayerTrial.Capture(camera,Path.Combine(output,i+"-first-frame.png"));
                Check(store.RestoreLocalStartWithRevision(original,factory).Simulation.CaptureLocalState()==beforeState,"First render advanced saved combat");
                double deadline=Time.realtimeSinceStartupAsDouble+300;
                while(!scene.IsRunning&&failure==null){Check(Time.realtimeSinceStartupAsDouble<deadline,"First render timeout");await Task.Yield();}
                if(failure!=null)throw failure;
                // Same existing UI speed command used by the pre-admission three-cycle baseline.
                var speed=System.Linq.Enumerable.Single(view.GetComponentsInChildren<UnityEngine.UI.Button>(),b=>b.name=="Speed").onClick;
                if(mode!="resume"){speed.Invoke();speed.Invoke();}
                if(mode=="pause"){
                    await Task.Delay(3000);controller.ClosePresentation();
                    var paused=store.RestoreLocalStartWithRevision(original,factory);Check(paused.Simulation.Tick>firstTick&&paused.Simulation.Outcome==BasicCombatOutcome.Running,"Recovery checkpoint must be progressed and nonterminal");
                    Atomic(Path.Combine(runFolder,"paused.json"),C(new JObject{["processId"]=System.Diagnostics.Process.GetCurrentProcess().Id,["runId"]=original.RunId,["key"]=original.Key,["snapshotHash"]=original.SnapshotHash,["revision"]=paused.Revision,["tick"]=paused.Simulation.Tick,["stateHash"]=H(paused.Simulation.CaptureLocalState())}));
                    controller.Dispose();controller=null;generation++;activeRun=null;activeKey=null;
                    await Task.Delay(100);Check(camera==null&&view==null&&root==null,"Paused scene resources survived release");
                    return;
                }
                while(result==null&&failure==null){Check(Time.realtimeSinceStartupAsDouble<deadline,"Accepted battle terminal timeout");await Task.Delay(100);}
                if(failure!=null)throw failure;
                var final=store.RestoreLocalStartWithRevision(original,factory);var simulation=final.Simulation;string state=simulation.CaptureLocalState();
                if(mode=="resume")Check(simulation.Tick>firstTick&&final.Revision>before.Revision,"Restored combat did not continue");
                Check(result!=null&&simulation.Outcome!=BasicCombatOutcome.Running&&simulation.Tick==result.Tick,"Terminal model and presentation differ");
                speed.Invoke();Check(store.RestoreLocalStartWithRevision(original,factory).Simulation.CaptureLocalState()==state,"Closed view mutated checkpoint");
                releasedControllers.Add(new WeakReference(controller));releasedViews.Add(new WeakReference(view));
                if(runOwner!=null){Check(runOwner.Present(original,null,null,completed,failed)==Ce03BattleEntry.Result&&completionCallbacks==1,"Terminal reentry replayed battle or callback");}
                memory.Checkpoint(i+"-terminal");await Task.Delay(100);Check(camera==null&&view==null&&root==null,"Ended scene resources survived release");
                Atomic(Path.Combine(runFolder,"model-final.json"),state);
                var completion=new JObject{["schemaVersion"]="ce03-actual-local-completion-v1",["actualAccountRun"]=true,["scope"]="DISPOSABLE_LOCAL",["runId"]=original.RunId,["seed"]=original.Seed,["originalSnapshotSha256"]=original.SnapshotHash,["algorithmSourceSha256"]=sourceHash,["prepareSha256"]=original.Prepare()["prepareSha256"],["completedWaveCount"]=simulation.CompletedWaves,["outcome"]=simulation.Outcome==BasicCombatOutcome.Victory?"VICTORY":"FAILURE",["tick"]=simulation.Tick,["modelSha256"]=H(state),["durableRevision"]=final.Revision,["restoredDuringRun"]=mode=="resume",["processId"]=System.Diagnostics.Process.GetCurrentProcess().Id,["originalKey"]=original.Key,["firstFrameTick"]=firstTick,["lateInputBlocked"]=true,["checkpointStorage"]="EXPLICIT_LOCAL_QA_FILE_NOT_PRODUCT_SECURE_STORAGE"};
                Atomic(Path.Combine(runFolder,"completion.json"),C(completion));
                if(runOwner!=null){
                    var dedup=new JObject{["sameController"]=true,["duplicatePresentationJoined"]=true,["completionCallbacks"]=completionCallbacks,["terminalReentryWithoutScene"]=true};
                    if((bool?)config["verifyClaimedReentry"]==true){
                        await WaitFor(Path.Combine(runFolder,"claim-response.json"));
                        // This disposable host's confirmed claim envelope differs from
                        // the legacy product reward-line DTO. Consume only the existing
                        // lifecycle identity needed here; never remap reward balances.
                        var claim=JObject.Parse(File.ReadAllText(Path.Combine(runFolder,"claim-response.json")));
                        var finish=JObject.Parse(File.ReadAllText(Path.Combine(runFolder,"finish-response.json")));
                        Check((string)claim["runId"]==original.RunId&&(string)claim["rewardPreviewId"]==(string)finish["rewardPreviewId"]&&(string)claim["status"]=="CLAIMED"&&!string.IsNullOrWhiteSpace((string)claim["rewardClaimId"]),"Confirmed claim identity differs");
                        combatAllowed=false;
                        Check(runOwner.Present(original,null,null,completed,failed)==Ce03BattleEntry.KnownLifecycle&&completionCallbacks==1,"Known claimed lifecycle reopened battle");
                        Check(store.RestoreLocalStartWithRevision(original,factory).Simulation.CaptureLocalState()==state,"Late claimed recovery changed state");
                        dedup["confirmedClaimBlockedReentry"]=true;dedup["claimId"]=claim["rewardClaimId"];
                    }
                    Atomic(Path.Combine(runFolder,"reentry.json"),C(dedup));runOwner.Dispose();runOwner=null;
                }else controller.Dispose();
                controller=null;generation++;activeRun=null;activeKey=null;
                results.Add(completion.DeepClone());
                before=null;beforeState=null;final=null;simulation=null;state=null;speed=null;camera=null;view=null;root=null;
                memory.BeginWindow();await Task.Delay(1000);memory.Checkpoint(i+"-after");
                Atomic(Path.Combine(output,"progress.json"),C(results));
                Check(File.ReadAllText(Path.Combine(runFolder,"original-start.json"))==originalRaw,"Original START changed");
            }
            if((bool?)config["diagnosticGcAfterCycles"]==true){
                memory.Checkpoint("final-natural-before-diagnostic-gc",false);
                GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();await Task.Delay(100);
                memory.Checkpoint("final-diagnostic-forced-gc",false);
                Atomic(Path.Combine(output,"released-owner-probe.json"),C(new JObject{["scope"]="DIAGNOSTIC_FORCED_GC_AFTER_ALL_NATURAL_SAMPLES",["controllersAlive"]=System.Linq.Enumerable.Count(releasedControllers,r=>r.IsAlive),["viewsAlive"]=System.Linq.Enumerable.Count(releasedViews,r=>r.IsAlive),["observedCycles"]=cycles,["wholeHeapLeakFreeClaim"]=false}));
            }
            Atomic(Path.Combine(output,"result.json"),C(new JObject{["status"]="Passed",["mode"]=mode,["cycles"]=results,["sourceHash"]=sourceHash,["renderMode"]="EXPLICIT_OFFSCREEN_CAMERA_RENDER",["httpOwner"]=File.Exists(Path.Combine(queue,"ui.private.json"))?"UNITY_DEPARTURE_PREVIEW_START; HOST_FINISH_CLAIM":"TRUSTED_DISPOSABLE_HOST_ORCHESTRATION"}));
            }finally{generation++;activeRun=null;activeKey=null;if(runOwner!=null)runOwner.Dispose();else controller?.Dispose();scene.Close();UnityEngine.Object.Destroy(sceneObject);UnityEngine.Object.Destroy(inputObject);UnityEngine.Object.Destroy(memory.gameObject);}
        }
    }
}
#endif
