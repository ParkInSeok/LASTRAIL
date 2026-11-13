#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LastRail.Presentation.RoofBattle;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
namespace LastRail.Presentation.Battle {
 // Development-only rendering fixture, followed by the existing original Eve LOCAL composition.
 public sealed class FourAgentV03PlayerProbe:MonoBehaviour {
  [SerializeField]FourAgentV03Assets assets;[SerializeField]FiveAgentVisualAssets legacy;[SerializeField]TextAsset source;[SerializeField]string hash;[SerializeField]TMP_FontAsset font;
  GameObject canvasObject,cameraObject,roofObject;FiveAgentVisualView view;string output;int exit=1;readonly JArray checks=new JArray(),captures=new JArray();
  public void Initialize(FourAgentV03Assets a,FiveAgentVisualAssets old,TextAsset s,string digest,TMP_FontAsset f){assets=a;legacy=old;source=s;hash=digest;font=f;}
  void Check(bool ok,string name){if(!ok)throw new InvalidOperationException(name);checks.Add(name);}
  IEnumerator Start(){
   Application.runInBackground=true;
   var arg=Environment.GetCommandLineArgs().SingleOrDefault(a=>a.StartsWith("--v03-qa-dir=",StringComparison.Ordinal));
   if(arg==null){Debug.LogError("Explicit v03 D QA directory required.");yield break;}
   output=Path.GetFullPath(arg.Substring("--v03-qa-dir=".Length)).Replace('\\','/');
   if(!output.StartsWith("D:/Project/LASTRAIL/QA/",StringComparison.OrdinalIgnoreCase)){Application.Quit(2);yield break;}
   Directory.CreateDirectory(output);var work=new Stack<IEnumerator>();work.Push(Run());bool failed=false;
   // Advance nested captures here too, so a capture failure follows the same cleanup/exit path.
   while(work.Count>0){object pending=null;bool suspend=false;try{var current=work.Peek();if(!current.MoveNext()){(work.Pop() as IDisposable)?.Dispose();continue;}if(current.Current is IEnumerator nested)work.Push(nested);else{pending=current.Current;suspend=true;}}catch(Exception e){failed=true;File.WriteAllText(output+"/failure.txt",e.ToString());Debug.LogException(e);}if(failed)break;if(suspend)yield return pending;}
   while(work.Count>0)(work.Pop() as IDisposable)?.Dispose();Close();Application.Quit(exit);
  }
  IEnumerator Run(){
   Check(assets!=null,"imported_v03_catalog_bound");var composer=new FourAgentV03FrameComposer(assets);
   cameraObject=new GameObject("V03FixtureClearCamera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.055f,.075f,1);camera.cullingMask=0;
   canvasObject=new GameObject("V03ExplicitVisualFixtureNotWorldProjection",typeof(RectTransform),typeof(Canvas));var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
   var design=new GameObject("941x1060ReferenceCoordinates",typeof(RectTransform));design.transform.SetParent(canvasObject.transform,false);var rt=design.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.pivot=new Vector2(.5f,.5f);rt.sizeDelta=new Vector2(941,1060);float scale=Mathf.Min(Screen.width/941f,Screen.height/1060f);rt.localScale=new Vector3(scale,scale,1);
   view=design.AddComponent<FiveAgentVisualView>();view.ConfigureV03(assets);
   var ids=new[]{"op_kyle","op_reina","op_eve","op_sera"};var feet=new[]{new Vector2(570,529),new Vector2(570,618),new Vector2(570,705),new Vector2(570,801)};
   for(int d=1;d<=4;d++)foreach(string phase in new[]{"idle","attack","effects-off"}){
    bool attack=phase!="idle",fx=phase!="effects-off";long[] ages={420,480,1330,1150};var frames=ids.Select((id,i)=>composer.Compose(id,d,attack,attack?ages[i]:0,777,feet[i],new Vector2(170,300+i*100),fx,fx,referenceToDisplayScale:scale)).ToArray();var combined=FourAgentV03FrameComposer.Combine(frames);view.Present(combined);
    Check(((JArray)combined["layers"]).Count(l=>(string)l["role"]=="actor")==4,"four_independent_bodies_d"+d+"_"+phase);
    if(!fx)Check(!combined["layers"].Any(l=>new[]{"beam","charge","muzzle","projectile","smoke"}.Contains((string)l["role"])),"effects_off_preserves_only_body_companions_d"+d);
    File.WriteAllText(output+"/d"+d+"-"+phase+".json",combined.ToString());yield return Capture("d"+d+"-"+phase);
   }
   view.Clear();Check(view.VisibleLayers==0,"visual_fixture_clear");Destroy(canvasObject);Destroy(cameraObject);yield return null;
   roofObject=new GameObject("OriginalEveLocalWithV03NeutralBody");var scene=roofObject.AddComponent<RoofBattleLocalScene>();
   scene.Initialize(source,hash,"v03-player-"+Guid.NewGuid().ToString("N"),legacy,font,output+"/native-store");scene.ConfigureV03(assets);scene.SetAutoAdvance(false);yield return null;yield return null;
   Check(scene.Ready,"original_eve_local_ready");Check((int)scene.Metadata()["ownedNativeOwnerCount"]==1,"exactly_one_original_native_owner");
   scene.AdvanceManual(10000,2);Check(scene.LastCommandSucceeded,"original_advance_committed");Check((long)scene.Snapshot["tick"]==1&&(int)scene.Snapshot["hp"]["enemy"]==675,"original_tick_and_hp_preserved");Check(scene.View.SpatialFxCount==0&&scene.View.NeutralActorCount==1,"native_instant_receipt_does_not_fabricate_v03_attack");
   scene.AdvanceManual(2000000,200);Check(scene.LastCommandSucceeded,"native_clock_drives_idle_body");var before=scene.Snapshot;
   scene.View.QaClickPause();long held=(long)scene.Snapshot["tick"];scene.AdvanceManual(1000000,100);Check((long)scene.Snapshot["tick"]==held,"native_hold_freezes_original_clock");yield return Capture("native-eve-v03-held");
   var heldSnapshot=scene.Snapshot;roofObject.SetActive(false);Check(scene.View.VisibleLayers==0,"native_disable_clears_body");scene.View.QaClickAdvance();Check(JToken.DeepEquals(heldSnapshot,scene.Snapshot),"unbound_ui_cannot_advance_owner");roofObject.SetActive(true);yield return null;Check(scene.LastDeliveredEventCount==0&&scene.View.NeutralActorCount==1,"native_rebind_snapshot_without_attack_replay");scene.RestoreOriginal();Check(scene.LastCommandSucceeded&&scene.LastDeliveredEventCount==0,"native_restore_without_replay");
   File.WriteAllText(output+"/player.json",new JObject{["status"]="Passed",["version"]=FourAgentV03Catalog.Version,["visualScope"]="FOUR_ACTOR_EXPLICIT_DISPLAY_FIXTURE_NOT_NATIVE_FOUR_COMBAT",["nativeScope"]="EXISTING_EVE_LOCAL_ORIGINAL_CLOCK_HP_HOLD_RESTORE_NEUTRAL_BODY_ONLY",["nativeMetadata"]=scene.Metadata(),["nativeSnapshot"]=scene.Snapshot,["sourceHash"]=hash,["checks"]=checks,["captures"]=captures,["screen"]=new JArray(Screen.width,Screen.height),["graphics"]=SystemInfo.graphicsDeviceType.ToString(),["captureMode"]="PLAYER_EXPLICIT_CAMERA_RENDER_TARGET_HIDDEN_WINDOW_NOT_OS_FRAMEBUFFER",["hardwareInput"]=false,["cameraWorldProjectionSupplied"]=false}.ToString());exit=0;
  }
  IEnumerator Capture(string name){
   yield return new WaitForEndOfFrame();
   var canvas=canvasObject!=null?canvasObject.GetComponent<Canvas>():roofObject.GetComponentInChildren<Canvas>();
   var camera=cameraObject!=null?cameraObject.GetComponent<Camera>():roofObject.GetComponentInChildren<Camera>();
   var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;float oldPlane=canvas.planeDistance;int oldMask=camera.cullingMask;var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
   var target=new RenderTexture(Screen.width,Screen.height,24);Texture2D pixels=null;
   try{
    target.Create();camera.targetTexture=target;camera.cullingMask=-1;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
    pixels=new Texture2D(Screen.width,Screen.height,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);pixels.Apply();
    var distinct=new HashSet<uint>();var colors=pixels.GetPixels32();for(int n=0;n<colors.Length&&distinct.Count<64;n+=7){var c=colors[n];distinct.Add((uint)(c.r|(c.g<<8)|(c.b<<16)));}Check(distinct.Count>=32,"nonblank_player_camera_render_"+name);
    File.WriteAllBytes(output+"/"+name+".png",pixels.EncodeToPNG());captures.Add(name+".png");
   }finally{RenderTexture.active=oldActive;camera.targetTexture=oldTarget;camera.cullingMask=oldMask;canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldPlane;if(pixels!=null)Destroy(pixels);target.Release();Destroy(target);Canvas.ForceUpdateCanvases();}
   yield return null;
  }
  void Close(){if(roofObject!=null)Destroy(roofObject);if(canvasObject!=null)Destroy(canvasObject);if(cameraObject!=null)Destroy(cameraObject);}
  void OnDestroy()=>Close();
 }
}
#endif
