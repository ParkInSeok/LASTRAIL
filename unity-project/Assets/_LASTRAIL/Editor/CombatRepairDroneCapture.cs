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
using UnityEngine.UI;
using UnityEngine.EventSystems;
public static class CombatRepairDroneCapture {
 static JObject prepared;
 static RestoredCombatCheckpoint Snapshot(BasicCombatSimulation s,long revision)=>(RestoredCombatCheckpoint)Activator.CreateInstance(typeof(RestoredCombatCheckpoint),BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{s,revision},null);
 static BasicCombatSimulation Fresh(){var c=new Ce03CommandContract((JObject)prepared["commandContract"]);return c.CreateLocalTest(prepared,(string)prepared["ownedSelection"]["accountId"],c.Revision,1);}
 public static void Run(){
  var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-repairDroneOutput");if(!Application.isBatchMode||at<0||at+1>=args.Length||Directory.Exists(args[at+1]))throw new Exception("Fresh explicit output required");var output=args[at+1];Directory.CreateDirectory(output);
  prepared=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-frontline-policy-UtU7yJ/prepared.json"));File.WriteAllText(output+"/prepared.json",prepared.ToString());
  var cameraGo=new GameObject("frontline-camera");var camera=cameraGo.AddComponent<Camera>();var texture=new RenderTexture(720,1280,24);camera.targetTexture=texture;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;var pixels=new Texture2D(720,1280,TextureFormat.RGB24,false);
  var encoder=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo{FileName="C:/Users/dlstj/AppData/Roaming/Python/Python39/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe",Arguments="-hide_banner -loglevel error -f rawvideo -pixel_format rgb24 -video_size 720x1280 -framerate 30 -i pipe:0 -vf vflip -an -c:v libx264 -crf 19 -pix_fmt yuv420p -movflags +faststart \""+output+"/video.mp4\"",UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true});
  var report=new JArray();GameObject go=null;var previous=RenderTexture.active;
  try{
   for(int scenario=2;scenario<3;scenario++){
    var warm=Fresh();warm.Advance(20000000,40);var raw=JObject.Parse(warm.CaptureLocalState());
    {var train=((JArray)raw["actors"]).Single(a=>(string)a["id"]=="train_a");train["vitals"]["hp"]=750;}
    var model=BasicCombatSimulation.RestoreLocalState(raw.ToString(),Fresh);var reference=BasicCombatSimulation.RestoreLocalState(raw.ToString(),Fresh);
    string retained=model.CaptureLocalState();long revision=1;var driver=new CombatSceneDriver(Fresh,()=>Snapshot(BasicCombatSimulation.RestoreLocalState(retained,Fresh),revision),(s,r)=>{retained=s.CaptureLocalState();return revision=r+1;},()=>true);driver.Open();File.WriteAllText(output+"/case-"+scenario+"-initial.json",driver.StateJson);
    go=new GameObject("frontline",typeof(RectTransform),typeof(Canvas));var view=go.AddComponent<ApprovedCombatView>();double clock=0;view.SetCapturePresentationClock(()=>clock);view.ConfigurePresentation(new CombatPresentationPolicy(()=>false,()=>true){Mode=CombatPresentationMode.Full});view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
    var labelGo=new GameObject("DiagnosticLabel",typeof(RectTransform));labelGo.transform.SetParent(go.transform,false);var rect=labelGo.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-68);rect.sizeDelta=new Vector2(710,30);var label=labelGo.AddComponent<TextMeshProUGUI>();label.font=go.GetComponentInChildren<TMP_Text>().font;label.fontSize=15;label.alignment=TextAlignmentOptions.Center;label.color=Color.yellow;
    label.text=scenario==0?"LOCAL QA · 지원사격 · 실제 단일 명중":scenario==1?"LOCAL QA · 전자기 필드 · 실제 흡수/만료":"LOCAL QA · 수리드론 · 실제 회복/복귀";
    string requestedAbility=scenario==0?"support_fire":scenario==1?"barrier":"emergency_repair";
    var events=new GameObject("NativeInput").AddComponent<EventSystem>();var pointer=new PointerEventData(events){button=PointerEventData.InputButton.Left,pointerId=1};var button=go.GetComponentsInChildren<Button>().Single(b=>b.name=="Command"+(scenario==0?1:scenario==1?0:2));var nativeInputs=new JArray();string commandId=null;int peakRockets=0;
    using(var trace=new StreamWriter(output+"/case-"+scenario+"-frames.jsonl"))for(int frame=0;frame<(scenario==1?360:180);frame++){
     clock=frame/30d;if(frame==200||frame==260){var speedButton=go.GetComponentsInChildren<Button>().Single(b=>b.name=="Speed");ExecuteEvents.Execute(speedButton.gameObject,pointer,ExecuteEvents.pointerClickHandler);reference.SetSpeed(frame==200?3:4);nativeInputs.Add(new JObject{["frame"]=frame,["event"]="pointerClick",["button"]="Speed"});}if(frame==18){if(!button.interactable)throw new Exception("Native support button not ready");ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);nativeInputs.Add(new JObject{["frame"]=frame,["event"]="pointerDown",["button"]=button.name});}if(frame==19){ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);var command=((JArray)JObject.Parse(driver.StateJson)["commands"]).Single();commandId=(string)command["id"];reference.RequestSkill(commandId,"train_a",requestedAbility);nativeInputs.Add(new JObject{["frame"]=frame,["event"]="pointerUp+pointerClick",["button"]=button.name,["command"]=commandId});}if(frame==27||frame==42){driver.SetPaused(frame==27);reference.SetPaused(frame==27);}
     if(scenario==1&&frame==80){var active=go.GetComponentsInChildren<Button>().Single(b=>b.name=="skills0");if(!active.interactable)throw new Exception("Active not ready");ExecuteEvents.Execute(active.gameObject,pointer,ExecuteEvents.pointerClickHandler);var last=((JArray)JObject.Parse(driver.StateJson)["commands"]).Last();reference.RequestSkill((string)last["id"],((string)last["skill"]).Split('/')[0],((string)last["skill"]).Split('/')[1]);nativeInputs.Add(new JObject{["frame"]=frame,["event"]="pointerClick",["button"]=active.name,["command"]=last["id"]});}
     if(frame==65){var before=driver.StateJson;driver.Open();if(before!=driver.StateJson)throw new Exception("Unexpected load reset");}
     long delta=(frame+1)*10000000L/30-frame*10000000L/30;driver.Advance(delta,1);reference.Advance(delta,1);view.Refresh();if(driver.StateJson!=reference.CaptureLocalState())throw new Exception("Reference mismatch "+frame);
     var missile=go.GetComponent<CombatSupportFireView>();peakRockets=Math.Max(peakRockets,missile.FlightPoints.Count(p=>(bool)p["visible"]));var state=driver.HudState;trace.WriteLine(new JObject{["frame"]=frame,["tick"]=driver.Tick,["referenceExact"]=true,["actors"]=state["actors"],["hits"]=state["hits"],["casts"]=state["casts"],["frontSlots"]=state["frontSlots"],["skills"]=state["skills"],["trainHp"]=state["trainHp"],["trainMaxHp"]=state["trainMaxHp"],["drone"]=go.GetComponent<CombatRepairDroneView>().Frame,["tipError"]=go.GetComponent<CombatRepairDroneView>().ContactError,["field"]=go.GetComponentInChildren<CombatElectromagneticField>().Frame,["supportFire"]=missile.Frame,["flights"]=missile.FlightPoints,["effectResults"]=state["effectResults"],["receipts"]=missile.ImpactReceipts}.ToString(Newtonsoft.Json.Formatting.None));
     Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=texture;pixels.ReadPixels(new Rect(0,0,720,1280),0,0);pixels.Apply();var bytes=pixels.GetRawTextureData<byte>().ToArray();encoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);if(frame%15==0||new[]{18,19,23,27,28,42,46,47,48,49,50,55,65,70,89,119,179}.Contains(frame))File.WriteAllBytes(output+"/case-"+scenario+"-f"+frame+".png",pixels.EncodeToPNG());
    }
    File.WriteAllText(output+"/case-"+scenario+"-final.json",driver.StateJson);File.WriteAllText(output+"/case-"+scenario+"-reference.json",reference.CaptureLocalState());
    if(reference.CommandStatus(commandId)!="APPLIED")throw new Exception("Actual fire did not apply");File.WriteAllText(output+"/case-"+scenario+"-inputs.json",nativeInputs.ToString());report.Add(new JObject{["scenario"]=scenario,["frames"]=scenario==1?360:180,["referenceFrames"]=scenario==1?360:180,["commandStatus"]=reference.CommandStatus(commandId),["peakRockets"]=peakRockets,["impactReceipts"]=go.GetComponent<CombatSupportFireView>().ImpactReceipts});UnityEngine.Object.DestroyImmediate(events.gameObject);UnityEngine.Object.DestroyImmediate(go);go=null;
   }
   encoder.StandardInput.Close();encoder.WaitForExit();if(encoder.ExitCode!=0)throw new Exception("Encoder failure");File.WriteAllText(output+"/complete.json",new JObject{["scope"]="LOCAL repair-drone-only6s. Normal prepared40tick warmup; diagnostic HP750. Native emergency_repair18/19. Pause27..41, retainedOpen65. Actual repair/tip contact/return; reference every frame. No backend calls.",["frames"]=180,["seconds"]=6,["audio"]=false,["cases"]=report}.ToString());EditorApplication.Exit(0);
  }catch(Exception e){File.WriteAllText(output+"/failed.txt",e.ToString());EditorApplication.Exit(1);}
  finally{RenderTexture.active=previous;if(go!=null)UnityEngine.Object.DestroyImmediate(go);if(!encoder.HasExited){encoder.StandardInput.Close();encoder.WaitForExit(1000);}UnityEngine.Object.DestroyImmediate(cameraGo);texture.Release();}
 }
}
#endif
