#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public static class CombatCanyonCoverageCapture {
 static int scenario;static JObject draft;
 public static void Run(){
  var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-coverageOutput");if(!Application.isBatchMode||at<0||Directory.Exists(args[at+1]))throw new Exception("Fresh explicit diagnostic output required");string output=args[at+1];Directory.CreateDirectory(output);
  var assets=LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved");if(assets.MapId!="canyon")throw new Exception("Installed canyon required");
  var cameraGo=new GameObject("coverage-camera");var camera=cameraGo.AddComponent<Camera>();var texture=new RenderTexture(720,1280,24);camera.targetTexture=texture;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;var pixels=new Texture2D(720,1280,TextureFormat.RGB24,false);
  var encoder=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo{FileName="C:/Users/dlstj/AppData/Roaming/Python/Python39/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe",Arguments="-hide_banner -loglevel error -f rawvideo -pixel_format rgb24 -video_size 720x1280 -framerate 30 -i pipe:0 -vf vflip -an -c:v libx264 -crf 19 -pix_fmt yuv420p -movflags +faststart \""+output+"/coverage.mp4\"",UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true});
  var report=new JArray();GameObject go=null;var previous=RenderTexture.active;
  try{
   for(scenario=Array.IndexOf(args,"-damageOnly")>=0?1:0;scenario<3;scenario++){
    draft=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-local-producer/draft.json"));draft["ownedSelection"]["accountId"]="canyon-coverage-diagnostic";
    if(scenario==0){
     // Local diagnostic configuration only. Never save to shipped encounter/profile.
     var template=draft["draft"]["template"];var defs=(JArray)template["enemyDefinitions"];

     var spawns=(JArray)template["waves"][0]["spawns"];var source=spawns.Single(s=>(string)s["lane"]=="EXTERIOR");
     for(int slot=1;slot<4;slot++){var row=source.DeepClone();row["spawnId"]="diagnostic.exterior."+slot;row["logicalSlot"]=slot;row["atTick"]=slot*4;spawns.Add(row);}
    }
    File.WriteAllText(output+"/case-"+scenario+"-fixture.json",draft.ToString());var initial=Fresh();var raw=JObject.Parse(initial.CaptureLocalState());var train=raw["actors"].Single(a=>(string)a["id"]=="train_a");if(scenario>0)train["vitals"]["hp"]=(scenario==1?35:15)*50;
    var model=BasicCombatSimulation.RestoreLocalState(raw.ToString(),Fresh);var reference=BasicCombatSimulation.RestoreLocalState(raw.ToString(),Fresh);var checkpoint=(RestoredCombatCheckpoint)Activator.CreateInstance(typeof(RestoredCombatCheckpoint),BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{model,1L},null);var driver=new CombatSceneDriver(Fresh,()=>checkpoint,(s,r)=>r+1,()=>true);driver.Open();File.WriteAllText(output+"/case-"+scenario+"-initial.json",driver.StateJson);
    go=new GameObject("coverage",typeof(RectTransform),typeof(Canvas));var view=go.AddComponent<ApprovedCombatView>();double clock=0;view.SetCapturePresentationClock(()=>clock);view.Bind(driver,assets);var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
    var labelGo=new GameObject("DiagnosticLabel",typeof(RectTransform));labelGo.transform.SetParent(go.transform,false);var rect=labelGo.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-68);rect.sizeDelta=new Vector2(710,30);var label=labelGo.AddComponent<TextMeshProUGUI>();label.font=go.GetComponentInChildren<TMP_Text>().font;label.fontSize=15;label.alignment=TextAlignmentOptions.Center;label.color=Color.yellow;label.text=scenario==0?"LOCAL 진단 · 버기/상단/4대 배치 (실전 편성 아님)":"LOCAL 진단 · 초기 HP "+(scenario==1?35:15)+"% → 실제 긴급 수리";
    int initialStage=go.GetComponentInChildren<CombatTrainDamageView>().Stage,beforeRepair=0,maxVisible=0;var hits=new System.Collections.Generic.HashSet<string>();using(var trace=new StreamWriter(output+"/case-"+scenario+"-frames.jsonl")){
     for(int frame=0;frame<240;frame++){
      clock=frame/30d;if(scenario==0&&(frame==100||frame==150)){string id="diagnostic.exterior."+(frame==100?1:2);AssertEqual(driver.SetEnemyFocus(id),reference.SetEnemyFocus(id),"focus result");}
      if(scenario>0&&frame==90){beforeRepair=(int)driver.HudState["trainHp"];driver.RequestSkill("coverage-repair","train_a","emergency_repair");reference.RequestSkill("coverage-repair","train_a","emergency_repair");}
      long delta=(frame+1)*10000000L/30-frame*10000000L/30;driver.Advance(delta,1);reference.Advance(delta,1);view.Refresh();AssertEqual(driver.StateJson,reference.CaptureLocalState(),"reference frame "+frame);
      var state=driver.HudState;var ext=(JArray)new JArray(((JArray)state["actors"]).Where(a=>(bool)a["exterior"]));maxVisible=Math.Max(maxVisible,ext.Count(a=>(long)a["availableAt"]<=driver.Tick&&(int)a["hp"]>0));foreach(var h in (JArray)state["hits"])if((string)h["Source"]=="train_a"||(string)h["Source"]=="car_gun")hits.Add((string)h["Target"]);
      var poses=new JArray();foreach(var a in ext){var body=go.transform.Find("ApprovedBattle/ExteriorViewport/ExteriorActors/"+(string)a["id"]) as RectTransform;if(body==null)continue;poses.Add(new JObject{["id"]=a["id"],["visual"]=a["visual"],["slot"]=a["slot"],["active"]=body.gameObject.activeSelf,["sibling"]=body.GetSiblingIndex(),["x"]=body.anchoredPosition.x,["y"]=body.anchoredPosition.y,["scale"]=body.localScale.x});}
      trace.WriteLine(new JObject{["frame"]=frame,["tick"]=driver.Tick,["trainHp"]=state["trainHp"],["damageStage"]=go.GetComponentInChildren<CombatTrainDamageView>().Stage,["referenceExact"]=true,["exterior"]=ext,["poses"]=poses,["effects"]=state["effectResults"],["hits"]=state["hits"]}.ToString(Newtonsoft.Json.Formatting.None));
      Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=texture;pixels.ReadPixels(new Rect(0,0,720,1280),0,0);pixels.Apply();encoder.StandardInput.BaseStream.Write(pixels.GetRawTextureData<byte>().ToArray(),0,720*1280*3);if(frame%30==0||frame==89||frame==130||frame==239)File.WriteAllBytes(output+"/case-"+scenario+"-f"+frame+".png",pixels.EncodeToPNG());
     }
    }
    if(scenario>0){AssertEqual(reference.CommandStatus("coverage-repair"),"APPLIED","repair actual status");if((int)driver.HudState["trainHp"]<=beforeRepair||go.GetComponentInChildren<CombatTrainDamageView>().Stage>=initialStage)throw new Exception("Actual repair did not reduce condition stage");}
    else if(maxVisible<4||!hits.Contains("diagnostic.exterior.1")||!hits.Contains("diagnostic.exterior.2"))throw new Exception("Multi/upper/actual-target coverage missing");
    File.WriteAllText(output+"/case-"+scenario+"-final.json",driver.StateJson);File.WriteAllText(output+"/case-"+scenario+"-reference.json",reference.CaptureLocalState());report.Add(new JObject{["scenario"]=scenario,["frames"]=240,["initialStage"]=initialStage,["finalStage"]=go.GetComponentInChildren<CombatTrainDamageView>().Stage,["beforeRepairHp"]=beforeRepair,["finalHp"]=driver.HudState["trainHp"],["repairStatus"]=scenario>0?reference.CommandStatus("coverage-repair"):null,["maxSimultaneousExterior"]=maxVisible,["actualTrainHitTargets"]=new JArray(hits),["referenceFrames"]=240});UnityEngine.Object.DestroyImmediate(go);go=null;
   }
   encoder.StandardInput.Close();encoder.WaitForExit();if(encoder.ExitCode!=0)throw new Exception("Encoder failure");File.WriteAllText(output+"/complete.json",new JObject{["scope"]="OFFLINE LOCAL DIAGNOSTIC INITIAL CONDITIONS; case0 cloned local spawns plus Editor-only Buggy visual override on Bike-stat model actors; cases1/2 original encounter with initialHP overrides; actual skill model thereafter",["frames"]=report.Count*240,["seconds"]=report.Count*8,["audio"]=false,["input"]="diagnostic RequestSkill/SetEnemyFocus, not native UI",["cases"]=report}.ToString());Ce03PresentationClosureExport.Run();EditorApplication.Exit(0);
  }catch(Exception e){File.WriteAllText(output+"/failed.txt",e.ToString());EditorApplication.Exit(1);}
  finally{RenderTexture.active=previous;if(go!=null)UnityEngine.Object.DestroyImmediate(go);if(!encoder.HasExited){encoder.StandardInput.Close();encoder.WaitForExit(1000);}UnityEngine.Object.DestroyImmediate(cameraGo);texture.Release();}
 }
 static void AssertEqual(object a,object b,string label){if(!Equals(a,b))throw new Exception(label+" mismatch");}
 static BasicCombatSimulation Fresh(){var m=BasicCombatSimulation.FromPreparedProfileForLocalTest(draft,(JObject)draft["ownedSelection"],"canyon-coverage-diagnostic",1);m.ConfigureLocalLeaderCharge(4);if(scenario==0){var visuals=(System.Collections.Generic.Dictionary<string,string>)typeof(BasicCombatSimulation).GetField("actorVisualIds",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(m);visuals["wave.1.exterior.0"]="enemy_spike_cart_gunner";visuals["diagnostic.exterior.1"]="enemy_spike_cart_gunner";}return m;}
}
#endif
