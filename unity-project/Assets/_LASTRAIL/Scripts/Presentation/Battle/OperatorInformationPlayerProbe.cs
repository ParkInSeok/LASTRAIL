#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LastRail.Battle;
using LastRail.Presentation.RoofBattle;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Presentation.Battle {
 public sealed class OperatorInformationPlayerProbe:MonoBehaviour {
  [SerializeField]TextAsset eveSource,miraSource;[SerializeField]string eveHash,miraHash;[SerializeField]FourAgentV03Assets assets;[SerializeField]TMP_FontAsset font;
  GameObject roof,display,captureCamera;OperatorInformationController controller;string output;readonly JArray checks=new JArray();
  public void Initialize(TextAsset eve,TextAsset mira,FourAgentV03Assets art,TMP_FontAsset suppliedFont){eveSource=eve;miraSource=mira;eveHash=BDirectAttackInput.Digest(JObject.Parse(eve.text));miraHash=BDirectAttackInput.Digest(JObject.Parse(mira.text));assets=art;font=suppliedFont;}
  void Check(bool value,string name){if(!value)throw new InvalidOperationException(name);checks.Add(name);}
  IEnumerator Start(){
   Application.runInBackground=true;var arg=Environment.GetCommandLineArgs().FirstOrDefault(x=>x.StartsWith("--operator-information-qa="));
   if(arg==null)yield break;output=Path.GetFullPath(arg.Substring("--operator-information-qa=".Length));if(!output.StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("D QA required.");Directory.CreateDirectory(output);
   var stack=new Stack<IEnumerator>();stack.Push(Run());Exception failure=null;while(stack.Count>0){var run=stack.Peek();bool more;try{more=run.MoveNext();}catch(Exception error){failure=error;break;}if(!more){stack.Pop();continue;}if(run.Current is IEnumerator nested){stack.Push(nested);continue;}yield return run.Current;}
   File.WriteAllText(output+"/player.json",new JObject{["status"]=failure==null?"Passed":"Failed",["error"]=failure?.ToString(),["checks"]=checks,["screen"]=new JArray(Screen.width,Screen.height),["graphics"]=SystemInfo.graphicsDeviceType.ToString(),["scope"]="ACTUAL_EVE_LOCAL_READ_ONLY_INFO_AND_SEPARATE_PINNED_MIRA_EQUIPMENT_DISPLAY",["equipmentCombatSceneIntegrated"]=false,["worldRangeSupplied"]=false,["hardwareInput"]=false,["capture"]="EXPLICIT_CAMERA_RENDER_TARGET_NOT_OS_FRAMEBUFFER"}.ToString());controller?.Dispose();Application.Quit(failure==null?0:1);
  }
  IEnumerator Run(){
   roof=new GameObject("OriginalEveInformationQa");var scene=roof.AddComponent<RoofBattleLocalScene>();scene.Initialize(eveSource,eveHash,"info-player-"+Guid.NewGuid().ToString("N"),null,font,output+"/native-store");scene.ConfigureV03(assets);scene.SetAutoAdvance(false);yield return null;yield return null;
   Check(scene.Ready,"eve_original_owner_ready");var before=scene.Snapshot;var body=roof.GetComponentsInChildren<Button>().Single(b=>b.name=="actor_slot2");body.onClick.Invoke();Check(scene.InformationFrame.Visible&&!scene.InformationFrame.Snapshot.EquipmentKnown,"eve_actual_actor_click_unknown_not_empty");Check(JToken.DeepEquals(before,scene.Snapshot),"eve_information_does_not_mutate_native_snapshot");yield return Capture(roof.GetComponentInChildren<Canvas>(),roof.GetComponentInChildren<Camera>(),"eve-info");
   body.onClick.Invoke();Check(!scene.InformationFrame.Visible,"eve_same_actor_closes");body.onClick.Invoke();roof.SetActive(false);Check(!scene.InformationFrame.Visible,"eve_disable_unbinds");body.onClick.Invoke();Check(!scene.InformationFrame.Visible,"eve_unbound_callback_no_effect");roof.SetActive(true);yield return null;body=roof.GetComponentsInChildren<Button>().Single(b=>b.name=="actor_slot2");body.onClick.Invoke();Check(scene.InformationFrame.Visible,"eve_rebind_once");Check(JToken.DeepEquals(before,scene.Snapshot),"eve_rebind_keeps_native_state");scene.AdvanceManual(10000,2);Check((long)scene.Snapshot["tick"]==1&&(int)scene.Snapshot["hp"]["enemy"]==675,"original_combat_continues_with_info_open");Check(scene.InformationFrame.Snapshot.Revision==(long)scene.Snapshot["revision"],"eve_committed_notification_updates_open_information");Check(!roof.GetComponentsInChildren<Button>(true).Any(b=>b.name=="RetryInformation"),"information_has_no_retry_ui");Destroy(roof);yield return null;
   var packet=JObject.Parse(miraSource.text);Check(BDirectAttackInput.Digest(packet)==miraHash,"original_mira_pin");var context=BMiraEquippedTrialContext.Read(packet,packet,(string)packet["startHash"],(string)packet["accountId"],(string)packet["runId"],(JObject)packet["source"]["originalComponent"]["originalMira"],(string)packet["originalMiraHash"],(string)packet["originalRunSnapshotHash"]);string unchanged=context.Capture().ToString();
   display=new GameObject("PinnedMiraInformationDisplay",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));var canvas=display.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;var scaler=display.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(720,1280);scaler.matchWidthOrHeight=1;
   var root=new GameObject("Design",typeof(RectTransform));root.transform.SetParent(display.transform,false);var rect=root.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(720,1280);
   var view=root.AddComponent<OperatorInformationView>();view.Configure(rect,font);controller=new OperatorInformationController((target,ct)=>System.Threading.Tasks.Task.FromResult(OperatorInformationMiraSource.Capture(context,target)));controller.Bind(view.Present);view.Bind(controller.OpenDetail,controller.CloseDetail,controller.Back);
   var selected=new OperatorInformationTarget(context.RunId,"mira-local","op_mira",1);var select=controller.SelectAsync(selected);while(!select.IsCompleted)yield return null;
   captureCamera=new GameObject("CaptureCamera",typeof(Camera));var camera=captureCamera.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.018f,.027f,.035f);camera.cullingMask=-1;
   // Keep the standalone QA canvas on the same camera between captures; avoid overlay/camera layout churn.
   canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();yield return null;
   display.GetComponentsInChildren<Button>(true).Single(b=>b.name=="EquippedSlot0").onClick.Invoke();Check(view.DetailVisible,"mira_real_frozen_slot_opens_detail");Check(controller.Frame.Snapshot.Slot(0).Description.Contains("15%"),"mira_original_applied_star_coefficient");yield return Capture(canvas,camera,"mira-detail");
   display.GetComponentsInChildren<Button>(true).Single(b=>b.name=="CloseEquipmentDetail").onClick.Invoke();Check(!view.DetailVisible&&controller.Frame.Target.Equals(selected),"detail_close_preserves_mira_selection");yield return Capture(canvas,camera,"mira-closed");Check(context.Capture().ToString()==unchanged,"mira_read_only_original_unchanged");
  }
  IEnumerator Capture(Canvas canvas,Camera camera,string name){
   yield return new WaitForEndOfFrame();var mode=canvas.renderMode;var previousCamera=canvas.worldCamera;float plane=canvas.planeDistance;int mask=camera.cullingMask;var old=camera.targetTexture;var active=RenderTexture.active;var target=new RenderTexture(Screen.width,Screen.height,24);Texture2D pixels=null;
   try{target.Create();camera.targetTexture=target;camera.cullingMask=-1;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;pixels=new Texture2D(Screen.width,Screen.height,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);pixels.Apply();File.WriteAllBytes(output+"/"+name+".png",pixels.EncodeToPNG());var colors=pixels.GetPixels32();var distinct=new HashSet<uint>();for(int i=0;i<colors.Length&&distinct.Count<32;i+=7){var c=colors[i];distinct.Add((uint)(c.r|(c.g<<8)|(c.b<<16)));}Check(distinct.Count>=16,"nonblank_capture_"+name);}
   finally{RenderTexture.active=active;camera.targetTexture=old;camera.cullingMask=mask;canvas.renderMode=mode;canvas.worldCamera=previousCamera;canvas.planeDistance=plane;if(pixels!=null)Destroy(pixels);target.Release();Destroy(target);}yield return null;
  }
  void OnDestroy(){controller?.Dispose();if(roof!=null)Destroy(roof);if(display!=null)Destroy(display);if(captureCamera!=null)Destroy(captureCamera);}
 }
}
#endif
