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
public static class CombatTrainConditionCapture {
 public static void Run(){
  const string output="D:/Project/LASTRAIL/unity-project/QA/ScreenIntegration/train-condition-v05-20260912";
  if(!Application.isBatchMode||Directory.Exists(output))throw new InvalidOperationException("Fresh batch output required");Directory.CreateDirectory(output);
  var cameraGo=new GameObject("condition-camera");var camera=cameraGo.AddComponent<Camera>();var target=new RenderTexture(720,1280,24);camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;var pixels=new Texture2D(720,1280,TextureFormat.RGB24,false);var previous=RenderTexture.active;
  var start=new System.Diagnostics.ProcessStartInfo{FileName="C:/Users/dlstj/AppData/Roaming/Python/Python39/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe",Arguments="-hide_banner -loglevel error -y -f rawvideo -pixel_format rgb24 -video_size 720x1280 -framerate 30 -i pipe:0 -vf vflip -an -c:v libx264 -crf 19 -pix_fmt yuv420p -movflags +faststart \""+output+"/train-condition.mp4\"",UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true};
  var encoder=System.Diagnostics.Process.Start(start);GameObject go=null;var report=new JArray();
  try{
   // Initial-condition diagnostic only. Runtime view still consumes actual HP.
   foreach(int percent in new[]{100,65,35,15,100}){
    var initial=Fresh();initial.Advance(500000*50,50);var hud=initial.ReadHudState();string id=(string)((JArray)hud["actors"]).Single(a=>(bool)a["train"])["id"];var raw=JObject.Parse(initial.CaptureLocalState());((JArray)raw["actors"]).Single(a=>(string)a["id"]==id)["vitals"]["hp"]=(int)hud["trainMaxHp"]*percent/100;
    var model=BasicCombatSimulation.RestoreLocalState(raw.ToString(),Fresh);var reference=BasicCombatSimulation.RestoreLocalState(raw.ToString(),Fresh);var checkpoint=(RestoredCombatCheckpoint)Activator.CreateInstance(typeof(RestoredCombatCheckpoint),BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{model,1L},null);var driver=new CombatSceneDriver(Fresh,()=>checkpoint,(s,r)=>r+1,()=>true);driver.Open();
    go=new GameObject("condition-preview",typeof(RectTransform),typeof(Canvas));var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
    for(int frame=0;frame<45;frame++){
     long delta=(frame+1)*10000000L/30-frame*10000000L/30;reference.Advance(delta,1);driver.Advance(delta,1);view.Refresh();if(reference.CaptureLocalState()!=driver.StateJson)throw new Exception("Decoration changed gameplay");Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,1280),0,0);pixels.Apply();encoder.StandardInput.BaseStream.Write(pixels.GetRawTextureData<byte>().ToArray(),0,720*1280*3);if(frame==24)File.WriteAllBytes(output+"/stage-"+report.Count+"-"+percent+".png",pixels.EncodeToPNG());
    }
    report.Add(new JObject{["initialHpPercent"]=percent,["actualHp"]=driver.HudState["trainHp"],["stage"]=go.GetComponentInChildren<CombatTrainDamageView>().Stage,["referenceStateExact"]=true});UnityEngine.Object.DestroyImmediate(go);go=null;
   }
   encoder.StandardInput.Close();encoder.WaitForExit();if(encoder.ExitCode!=0)throw new Exception("Encoder failed");File.WriteAllText(output+"/complete.json",new JObject{["scope"]="OFFLINE INITIAL HEALTH FIXTURE, not a damage/repair replay",["frames"]=225,["fps"]=30,["audio"]=false,["segments"]=report}.ToString());Ce03PresentationClosureExport.Run();EditorApplication.Exit(0);
  }catch(Exception e){File.WriteAllText(output+"/failed.txt",e.ToString());EditorApplication.Exit(1);}
  finally{RenderTexture.active=previous;if(go!=null)UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(cameraGo);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);}
 }
 static BasicCombatSimulation Fresh(){var draft=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-local-producer/draft.json"));draft["ownedSelection"]["accountId"]="train-condition-qa";return BasicCombatSimulation.FromPreparedProfileForLocalTest(draft,(JObject)draft["ownedSelection"],"train-condition-qa",1);}
}
#endif
