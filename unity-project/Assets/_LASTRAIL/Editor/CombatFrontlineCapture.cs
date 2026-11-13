#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using TMPro;
public static class CombatFrontlineCapture {
 static JObject prepared;
 static RestoredCombatCheckpoint Snapshot(BasicCombatSimulation s,long revision)=>(RestoredCombatCheckpoint)Activator.CreateInstance(typeof(RestoredCombatCheckpoint),BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{s,revision},null);
 static BasicCombatSimulation Fresh(){var c=new Ce03CommandContract((JObject)prepared["commandContract"]);return c.CreateLocalTest(prepared,(string)prepared["ownedSelection"]["accountId"],c.Revision,1);}
 public static void Run(){
  var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-frontlineOutput");if(!Application.isBatchMode||at<0||at+1>=args.Length||Directory.Exists(args[at+1]))throw new Exception("Fresh explicit output required");var output=args[at+1];Directory.CreateDirectory(output);
  prepared=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-frontline-policy-UtU7yJ/prepared.json"));File.WriteAllText(output+"/prepared.json",prepared.ToString());
  var cameraGo=new GameObject("frontline-camera");var camera=cameraGo.AddComponent<Camera>();var texture=new RenderTexture(720,1280,24);camera.targetTexture=texture;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;var pixels=new Texture2D(720,1280,TextureFormat.RGB24,false);
  var encoder=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo{FileName="C:/Users/dlstj/AppData/Roaming/Python/Python39/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe",Arguments="-hide_banner -loglevel error -f rawvideo -pixel_format rgb24 -video_size 720x1280 -framerate 30 -i pipe:0 -vf vflip -an -c:v libx264 -crf 19 -pix_fmt yuv420p -movflags +faststart \""+output+"/video.mp4\"",UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true});
  var report=new JArray();GameObject go=null;var previous=RenderTexture.active;
  try{
   for(int scenario=0;scenario<2;scenario++){
    var raw=JObject.Parse(Fresh().CaptureLocalState());foreach(var a in (JArray)raw["actors"]){var id=(string)a["id"];if(!id.StartsWith("wave."))a["ready"]=20000;if(scenario==0&&(id=="op_sera"||id=="op_mira"))a["vitals"]["hp"]=1;}
    var model=BasicCombatSimulation.RestoreLocalState(raw.ToString(),Fresh);var reference=BasicCombatSimulation.RestoreLocalState(raw.ToString(),Fresh);
    string retained=model.CaptureLocalState();long revision=1;var driver=new CombatSceneDriver(Fresh,()=>Snapshot(BasicCombatSimulation.RestoreLocalState(retained,Fresh),revision),(s,r)=>{retained=s.CaptureLocalState();return revision=r+1;},()=>true);driver.Open();File.WriteAllText(output+"/case-"+scenario+"-initial.json",driver.StateJson);
    go=new GameObject("frontline",typeof(RectTransform),typeof(Canvas));var view=go.AddComponent<ApprovedCombatView>();double clock=0;view.SetCapturePresentationClock(()=>clock);view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
    var labelGo=new GameObject("DiagnosticLabel",typeof(RectTransform));labelGo.transform.SetParent(go.transform,false);var rect=labelGo.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-68);rect.sizeDelta=new Vector2(710,30);var label=labelGo.AddComponent<TextMeshProUGUI>();label.font=go.GetComponentInChildren<TMP_Text>().font;label.fontSize=15;label.alignment=TextAlignmentOptions.Center;label.color=Color.yellow;
    label.text=scenario==0?"LOCAL QA · 전열 소진 → 후열 피격 → 전열 복귀":"LOCAL QA · 카일 도발 → 도발 만료 → 전열 우선";
    using(var trace=new StreamWriter(output+"/case-"+scenario+"-frames.jsonl"))for(int frame=0;frame<360;frame++){
     clock=frame/30d;if(scenario==1&&frame==18){driver.RequestSkill("frontline-taunt","op_kyle","op_kyle.active");reference.RequestSkill("frontline-taunt","op_kyle","op_kyle.active");}
     if(frame==240){var before=driver.StateJson;driver.Open();if(before!=driver.StateJson)throw new Exception("Unexpected load reset");}
     long delta=(frame+1)*10000000L/30-frame*10000000L/30;driver.Advance(delta,1);reference.Advance(delta,1);view.Refresh();if(driver.StateJson!=reference.CaptureLocalState())throw new Exception("Reference mismatch "+frame);
     var state=driver.HudState;trace.WriteLine(new JObject{["frame"]=frame,["tick"]=driver.Tick,["referenceExact"]=true,["actors"]=state["actors"],["hits"]=state["hits"],["casts"]=state["casts"],["frontSlots"]=state["frontSlots"],["rigs"]=view.CaptureRigStates()}.ToString(Newtonsoft.Json.Formatting.None));
     Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=texture;pixels.ReadPixels(new Rect(0,0,720,1280),0,0);pixels.Apply();var bytes=pixels.GetRawTextureData<byte>().ToArray();encoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);if(frame%30==0||frame==359)File.WriteAllBytes(output+"/case-"+scenario+"-f"+frame+".png",pixels.EncodeToPNG());
    }
    File.WriteAllText(output+"/case-"+scenario+"-final.json",driver.StateJson);File.WriteAllText(output+"/case-"+scenario+"-reference.json",reference.CaptureLocalState());
    if(scenario==1&&reference.CommandStatus("frontline-taunt")!="APPLIED")throw new Exception("Actual taunt did not apply");report.Add(new JObject{["scenario"]=scenario,["frames"]=360,["referenceFrames"]=360,["tauntStatus"]=scenario==1?reference.CommandStatus("frontline-taunt"):null});UnityEngine.Object.DestroyImmediate(go);go=null;
   }
   encoder.StandardInput.Close();encoder.WaitForExit();if(encoder.ExitCode!=0)throw new Exception("Encoder failure");File.WriteAllText(output+"/complete.json",new JObject{["scope"]="Exact new normal prepared/command factory; LOCAL initial condition: allied basic readiness delayed in both cases, front HP1 only in case0; actual impacts/down/recovery/taunt thereafter. Diagnostic RequestSkill, not native button.",["frames"]=720,["seconds"]=24,["audio"]=false,["cases"]=report}.ToString());EditorApplication.Exit(0);
  }catch(Exception e){File.WriteAllText(output+"/failed.txt",e.ToString());EditorApplication.Exit(1);}
  finally{RenderTexture.active=previous;if(go!=null)UnityEngine.Object.DestroyImmediate(go);if(!encoder.HasExited){encoder.StandardInput.Close();encoder.WaitForExit(1000);}UnityEngine.Object.DestroyImmediate(cameraGo);texture.Release();}
 }
}
#endif
