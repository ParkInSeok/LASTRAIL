#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
public sealed partial class Ce03NormalUiRecorder {
 async Task VisualReplay(JObject sources){
  // Offline visual reproduction only. No HTTP, durable run writes, finish or claim.
  var inputPath=Ce03NormalUiCapture.Arg("-ce03OriginalStart");var inputBytes=File.ReadAllBytes(inputPath);string inputRaw=new System.Text.UTF8Encoding(false,true).GetString(inputBytes);var identity=JObject.Parse(inputRaw);
  var original=new Ce03OriginalStart(inputRaw,(string)identity["owner"],new Uri((string)identity["origin"]+"/"));
  var assets=Ce03PresentationManifestLoader.Validate(new Ce03PresentationManifest((JObject)original.Prepare()["presentationManifest"]));var factory=original.CreateVerifiedEditorFactory();
  var driver=new CombatSceneDriver(factory,()=>null,(model,revision)=>revision+1,()=>true);driver.Open();
  canvas=new GameObject("Offline battle visual",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));var c=canvas.GetComponent<Canvas>();var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(720,1280);
  var view=canvas.AddComponent<ApprovedCombatView>();int frame=0;view.SetCapturePresentationClock(()=>frame/30d);view.Bind(driver,assets);c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=1;
  StartVideo();for(frame=0;frame<360;frame++){driver.Advance(333333L,4);view.Refresh();if(frame==60||frame==120||frame==180){var name="Command"+((frame/60)-1);var button=Find(name);if(button!=null&&button.interactable)Click(name);}await Pause(1);}StopVideo();
  if(!inputBytes.SequenceEqual(File.ReadAllBytes(inputPath)))throw new InvalidOperationException("Offline input changed during capture");Ce03ConsumerEvidenceRunner.VerifySources(sources);Ce03PresentationManifestLoader.Validate(new Ce03PresentationManifest((JObject)original.Prepare()["presentationManifest"]));
  Ce03NormalUiCapture.Atomic(Path.Combine(output,"visual-replay.json"),Canon(new JObject{["scope"]="OFFLINE_VISUAL_REPRODUCTION_NOT_NEW_ADMISSION",["originalRunId"]=original.RunId,["seed"]=original.Seed,["originalInputSha256"]=Ce03CommandContract.Sha(inputRaw),["generatedAt"]=DateTimeOffset.UtcNow.ToString("O"),["sourceBundleSha256"]=bundleHash,["frames"]=frames,["visibleFrames"]=visibleFrames,["tick"]=driver.Tick,["modelSha256"]=Ce03CommandContract.Sha(driver.StateJson),["networkRequests"]=0,["durableRunWrites"]=0,["finishSent"]=false,["claimSent"]=false}));
 }
 async Task Battle(bool resume,JObject sources,Uri origin){
  var original=new Ce03OriginalStart(File.ReadAllText(SessionPath("original-start.json")),(string)connection["accountId"],origin);
  var response=JObject.Parse((string)original.Copy()["responseRaw"]);if((string)response["combat"]["profile"]["baseProfile"]["executionEvidence"]["algorithmSourceSha256"]!=bundleHash)throw new InvalidOperationException("Original source differs");
  var prepared=original.Prepare();var assets=Ce03PresentationManifestLoader.Validate(new Ce03PresentationManifest((JObject)prepared["presentationManifest"]));
  string checkpoint=SessionPath("ui-battle-checkpoint.json");var store=new CombatCheckpointStore(checkpoint,()=>File.Exists(checkpoint)?File.ReadAllText(checkpoint):null,raw=>Ce03NormalUiCapture.Atomic(checkpoint,raw));
  Func<BasicCombatSimulation> fresh=original.CreateVerifiedEditorFactory();
  var driver=new CombatSceneDriver(fresh,()=>store.RestoreLocalStartWithRevision(original,fresh),(model,revision)=>store.SaveLocalStart(original,model,revision),()=>true);driver.Open();
  if(resume){var previous=Ce03NormalUiCapture.Read(Path.Combine(output,"before-process-exit.json"));if((int)previous["processId"]==System.Diagnostics.Process.GetCurrentProcess().Id||(string)previous["runId"]!=original.RunId||(long)previous["tick"]!=driver.Tick||(string)previous["modelSha256"]!=Ce03CommandContract.Sha(driver.StateJson))throw new InvalidOperationException("Whole process checkpoint equality failed");Ce03NormalUiCapture.Atomic(Path.Combine(output,"process-restart.json"),Canon(new JObject{["oldProcessId"]=previous["processId"].DeepClone(),["newProcessId"]=System.Diagnostics.Process.GetCurrentProcess().Id,["runId"]=original.RunId,["tick"]=driver.Tick,["modelSha256"]=Ce03CommandContract.Sha(driver.StateJson),["checkpointEqual"]=true,["sameOrigin"]=original.Origin}));}
  else if(driver.Tick!=0)throw new InvalidOperationException("Fresh START has existing checkpoint");
  if(canvas!=null)canvas.SetActive(false);
  var battleCanvas=new GameObject("Actual admitted battle",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));var c=battleCanvas.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=1;var scaler=battleCanvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(720,1280);
  var view=battleCanvas.AddComponent<ApprovedCombatView>();view.Bind(driver,assets);c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=1;string oldStage=stage;stage=resume?"battle-resume":"battle-entry";frames=0;StartVideo();
  for(int i=0;i<180&&driver.Outcome==BasicCombatOutcome.Running;i++){driver.Advance(500000L,1);view.Refresh();await Pause(1);}StopVideo();stage=oldStage;
  if(!resume){var acknowledged=store.RestoreLocalStartWithRevision(original,fresh);if(acknowledged==null||acknowledged.Simulation.CaptureLocalState()!=driver.StateJson)throw new IOException("Pre-exit checkpoint differs");Ce03NormalUiCapture.Atomic(Path.Combine(output,"before-process-exit.json"),Canon(new JObject{["processId"]=System.Diagnostics.Process.GetCurrentProcess().Id,["runId"]=original.RunId,["tick"]=driver.Tick,["modelSha256"]=Ce03CommandContract.Sha(driver.StateJson),["durableRevision"]=acknowledged.Revision}));return;}
  // Unrecorded deterministic continuation uses the same accepted model and durable driver.
  for(int i=0;i<3000&&driver.Outcome==BasicCombatOutcome.Running;i++){driver.Advance(5000000L,10);if(i%20==0)await Task.Yield();}
  if(driver.Outcome==BasicCombatOutcome.Running)throw new InvalidOperationException("Terminal bound exceeded");
  var final=store.RestoreLocalStartWithRevision(original,fresh);if(final==null||final.Simulation.CaptureLocalState()!=driver.StateJson)throw new IOException("Final checkpoint differs");Ce03ConsumerEvidenceRunner.VerifySources(sources);Ce03PresentationManifestLoader.Validate(new Ce03PresentationManifest((JObject)prepared["presentationManifest"]));
  Ce03NormalUiCapture.Atomic(Path.Combine(output,"original-start.json"),original.Json);Ce03NormalUiCapture.Atomic(Path.Combine(output,"model-final.json"),driver.StateJson);
  var completion=new JObject{["schemaVersion"]="ce03-actual-local-completion-v1",["actualAccountRun"]=true,["scope"]="DISPOSABLE_LOCAL",["runId"]=original.RunId,["seed"]=original.Seed,["originalSnapshotSha256"]=original.SnapshotHash,["algorithmSourceSha256"]=bundleHash,["prepareSha256"]=prepared["prepareSha256"].DeepClone(),["completedWaveCount"]=final.Simulation.CompletedWaves,["outcome"]=driver.Outcome==BasicCombatOutcome.Victory?"VICTORY":"FAILURE",["tick"]=driver.Tick,["eventCount"]=final.Simulation.Events.Count,["modelSha256"]=Ce03CommandContract.Sha(driver.StateJson),["durableRevision"]=final.Revision,["restoredDuringRun"]=true,["approvedViewBound"]=true,["visualCapturePerformed"]=true,["checkpointStorage"]="EXPLICIT_LOCAL_EDITOR_PLAINTEXT_PORT_NOT_ANDROID_KEYSTORE"};
  Ce03NormalUiCapture.Atomic(Path.Combine(output,"completion.json"),Canon(completion));string intentPath=Path.Combine(output,"finish-intent.json");if(File.Exists(intentPath))throw new IOException("Existing finish intent must be retained and reconciled");
  var body=new JObject{["runId"]=original.RunId,["encounterType"]="ORDINARY_15_WAVE",["currentStage"]="Complete",["currentWaveNumber"]=0,["completedWaveCount"]=final.Simulation.CompletedWaves,["outcome"]=completion["outcome"].DeepClone(),["runtimeStateJson"]=Canon(completion),["tacticSelections"]=new JArray(),["clientCompletedAt"]=DateTime.UtcNow.ToString("O")};Ce03NormalUiCapture.Atomic(intentPath,Canon(new JObject{["method"]="POST",["path"]="/v1/runs/"+original.RunId+"/finish",["idempotencyKey"]=Guid.NewGuid().ToString("N"),["body"]=body,["sent"]=false}));
 }
}
#endif
