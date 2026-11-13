#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
// Actual disposable-host accepted run consumer. No network sender is installed
// here. Server composition supplies authenticated retained START bytes; the
// original accepted algorithm-source identity must equal the executing bundle.
public static class Ce03AcceptedRunConsumer {
 static readonly UTF8Encoding Utf8=new UTF8Encoding(false,true);
 static string C(JToken j)=>Ce03CommandContract.Canonical(j);
 static string H(string s)=>Ce03CommandContract.Sha(s);
 static string Arg(string key){var a=Environment.GetCommandLineArgs();var indices=Enumerable.Range(0,a.Length).Where(i=>a[i]==key).ToArray();if(indices.Length!=1||indices[0]+1>=a.Length)throw new ArgumentException("Missing argument: "+key);return a[indices[0]+1];}
 static string Full(string key){string p=Arg(key);if(!Path.IsPathRooted(p))throw new ArgumentException("Absolute path required.");return Path.GetFullPath(p);}
 static void Check(bool b,string error){if(!b)throw new InvalidOperationException(error);}
 static void Atomic(string path,string raw){string temporary=path+".pending";using(var stream=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){byte[] bytes=Utf8.GetBytes(raw);stream.Write(bytes,0,bytes.Length);stream.Flush(true);}if(File.Exists(path))File.Replace(temporary,path,null);else File.Move(temporary,path);Check(File.ReadAllText(path,Utf8)==raw,"Durable readback mismatch.");}
 public static void Run(){string output=null;GameObject canvas=null;
  try{
   output=Full("-ce03Output");Check(Directory.Exists(output),"Output directory missing.");
   string bundle=Utf8.GetString(File.ReadAllBytes(Full("-ce03SourceBundle")));var source=JObject.Parse(bundle);Check(C(source)==bundle,"Noncanonical source bundle.");Ce03ConsumerEvidenceRunner.VerifySources(source);
   var original=new Ce03OriginalStart(Utf8.GetString(File.ReadAllBytes(Full("-ce03Start"))),Arg("-ce03Owner"),new Uri(Arg("-ce03Origin")));
   var response=JObject.Parse((string)original.Copy()["responseRaw"]);Check((string)response["combat"]["profile"]["baseProfile"]["executionEvidence"]["algorithmSourceSha256"]==H(bundle),"Original accepted algorithm differs from executing code.");
   var prepared=original.Prepare();var assets=Ce03PresentationManifestLoader.Validate(new Ce03PresentationManifest(prepared["presentationManifest"] as JObject));
   string identityPath=Path.Combine(output,"original-start.json");if(File.Exists(identityPath))Check(File.ReadAllText(identityPath,Utf8)==original.Json,"Existing original START differs.");else Atomic(identityPath,original.Json);
   string checkpoint=Path.Combine(output,"local-editor-checkpoint.json");var store=new CombatCheckpointStore(checkpoint,()=>File.Exists(checkpoint)?File.ReadAllText(checkpoint,Utf8):null,raw=>Atomic(checkpoint,raw));
   // Asset validation done above and at final boundary; original model factory
   // below is assembly-internal and only consumes this parsed accepted seed.
   Func<BasicCombatSimulation> fresh=()=>BasicCombatSimulation.FromPreparedProfileForLocalTest(prepared,(JObject)prepared["ownedSelection"],original.Owner,original.Seed);
   Func<BasicCombatSimulation> charged=()=>{var s=fresh();s.ConfigureLocalLeaderCharge(4);return s;};
   Func<CombatSceneDriver> open=()=>new CombatSceneDriver(charged,()=>store.RestoreLocalStartWithRevision(original,charged),(s,r)=>store.SaveLocalStart(original,s,r),()=>true);
   var driver=open();driver.Open();
   canvas=new GameObject("CE03 accepted local combat",typeof(RectTransform),typeof(Canvas));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvas.GetComponent<RectTransform>().sizeDelta=new Vector2(720,1280);
   var view=canvas.AddComponent<ApprovedCombatView>();view.Bind(driver,assets);
   bool restoredDuringRun=false;var commandLog=new JArray();
   for(int block=0;block<2000&&driver.Outcome==BasicCombatOutcome.Running;block++){
    if(block%50==1){var hud=driver.HudState;foreach(var skill in (JArray)hud["skills"])Request(driver,commandLog,"active-"+driver.Tick+"-"+(string)skill["actor"],skill);Request(driver,commandLog,"ultimate-"+driver.Tick,hud["ultimate"]);Request(driver,commandLog,"support-"+driver.Tick,((JArray)hud["manuals"]).Single(x=>(string)x["ability"]=="support_fire"));}
    driver.Advance(500000L*10,10);view.Refresh();
    if(block==5){string before=driver.StateJson;driver.Open();Check(driver.StateJson==before,"Actual original run checkpoint restore differs.");view.Refresh();restoredDuringRun=true;}
   }
   Check(driver.Outcome!=BasicCombatOutcome.Running,"Actual run did not reach terminal outcome.");
   var final=store.RestoreLocalStartWithRevision(original,charged);Check(final!=null&&final.Simulation.CaptureLocalState()==driver.StateJson,"Final durable ACK mismatch.");
   Ce03ConsumerEvidenceRunner.VerifySources(source);Ce03PresentationManifestLoader.Validate(new Ce03PresentationManifest(prepared["presentationManifest"] as JObject));
   string model=driver.StateJson;Atomic(Path.Combine(output,"model-final.json"),model);Atomic(Path.Combine(output,"commands.json"),C(commandLog));
   var completion=new JObject{["schemaVersion"]="ce03-actual-local-completion-v1",["actualAccountRun"]=true,["scope"]="DISPOSABLE_LOCAL",["runId"]=original.RunId,["seed"]=original.Seed,["originalSnapshotSha256"]=original.SnapshotHash,["algorithmSourceSha256"]=H(bundle),["prepareSha256"]=prepared["prepareSha256"].DeepClone(),["completedWaveCount"]=final.Simulation.CompletedWaves,["outcome"]=driver.Outcome==BasicCombatOutcome.Victory?"VICTORY":"FAILURE",["tick"]=driver.Tick,["eventCount"]=final.Simulation.Events.Count,["modelSha256"]=H(model),["durableRevision"]=final.Revision,["restoredDuringRun"]=restoredDuringRun,["approvedViewBound"]=true,["visualCapturePerformed"]=false,["checkpointStorage"]="EXPLICIT_LOCAL_EDITOR_PLAINTEXT_PORT_NOT_ANDROID_KEYSTORE"};
   Atomic(Path.Combine(output,"completion.json"),C(completion));
   // This is an unsent intent derived from the actual terminal model. Preserve
   // its key/body on rerun; no reward claim or second journal sender is created.
   string finishPath=Path.Combine(output,"finish-intent.json");if(!File.Exists(finishPath)){
    var body=new JObject{["runId"]=original.RunId,["encounterType"]="ORDINARY_15_WAVE",["currentStage"]="Complete",["currentWaveNumber"]=0,["completedWaveCount"]=final.Simulation.CompletedWaves,["outcome"]=completion["outcome"].DeepClone(),["runtimeStateJson"]=C(completion),["tacticSelections"]=new JArray(),["clientCompletedAt"]=DateTime.UtcNow.ToString("O")};
    Atomic(finishPath,C(new JObject{["method"]="POST",["path"]="/v1/runs/"+original.RunId+"/finish",["idempotencyKey"]=Guid.NewGuid().ToString("N"),["body"]=body,["sent"]=false}));
   }else{var intent=JObject.Parse(File.ReadAllText(finishPath,Utf8));Check((string)intent["body"]?["runId"]==original.RunId&&(string)intent["body"]?["outcome"]==(string)completion["outcome"]&&(int?)intent["body"]?["completedWaveCount"]==final.Simulation.CompletedWaves,"Existing finish intent mismatch.");}
   UnityEngine.Object.DestroyImmediate(canvas);canvas=null;Debug.Log("CE03 actual accepted run completed and checkpoint acknowledged.");EditorApplication.Exit(0);
  }catch(Exception e){Debug.LogException(e);if(canvas!=null)UnityEngine.Object.DestroyImmediate(canvas);if(output!=null&&Directory.Exists(output))Atomic(Path.Combine(output,"consumer-failure.json"),C(new JObject{["type"]=e.GetType().Name,["message"]=e.Message,["finishAuthorized"]=false}));EditorApplication.Exit(1);}
 }
 static void Request(CombatSceneDriver d,JArray log,string id,JToken skill){string actor=(string)skill["actor"],ability=(string)skill["ability"];string status=d.RequestSkill(id,actor,ability);log.Add(new JObject{["id"]=id,["actor"]=actor,["ability"]=ability,["tick"]=d.Tick,["status"]=status});}
}
#endif
