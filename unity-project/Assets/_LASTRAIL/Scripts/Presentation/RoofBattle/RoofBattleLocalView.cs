using System;
using System.Linq;
using LastRail.Presentation.Battle;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace LastRail.Presentation.RoofBattle {
 // View owns display objects and forwards intentions. It has no combat, persistence or target authority.
 public sealed class RoofBattleLocalView : MonoBehaviour {
  Canvas canvas;Camera ownedClearCamera;RectTransform design;FiveAgentVisualView actor;GameObject eventSystemOwned;
  UnityEngine.Object[] ownedInputReferences=Array.Empty<UnityEngine.Object>();
  TextMeshProUGUI status,error,qa,pauseLabel,speedLabel,autoLabel;TMP_FontAsset font;
  Button advanceButton,pauseButton,speedButton,restoreButton,autoButton;
  Action advanceRequested,pauseRequested,speedRequested,restoreRequested,autoRequested;
  Rect previousSafe;int previousWidth,previousHeight;bool configured;
  public int NeutralActorCount {get;private set;}
  public string NeutralActorId {get;private set;}
  public int NeutralSlot {get;private set;}=-1;
  public int SpatialFxCount {get;private set;}
  public string BodyPhase=>"NEUTRAL_IDLE_NO_WORLD_ATTACK_PHASE";
  public int VisibleLayers=>actor==null?0:actor.VisibleLayers;
  Action operatorClicked,emptyWorldClicked;
  public OperatorInformationView CreateInformationView(){var container=Rect("OperatorInformationComposition",design,110,365,720,1280);var information=container.gameObject.AddComponent<OperatorInformationView>();information.Configure(container,font);return information;}
  public void BindOperatorInformation(Action clicked,Action empty){operatorClicked=clicked;emptyWorldClicked=empty;}
  public void Configure(FiveAgentVisualAssets assets,TMP_FontAsset suppliedFont)=>ConfigureCore(assets,null,suppliedFont);
  public void ConfigureV03(FourAgentV03Assets assets,TMP_FontAsset suppliedFont)=>ConfigureCore(null,assets,suppliedFont);
  void ConfigureCore(FiveAgentVisualAssets assets,FourAgentV03Assets v03,TMP_FontAsset suppliedFont) {
   if(configured)throw new InvalidOperationException("View already configured.");
   if((assets==null&&v03==null)||suppliedFont==null)throw new ArgumentException("Explicit approved catalog and TMP font required.");
   configured=true;font=suppliedFont;
   // An explicit clear-only camera establishes the Player framebuffer. Overlay UI still uses no world camera or PPU.
   var clearObject=new GameObject("RoofLocalOwnedClearCamera",typeof(Camera));clearObject.transform.SetParent(transform,false);
   var clear=clearObject.GetComponent<Camera>();ownedClearCamera=clear;clear.clearFlags=CameraClearFlags.SolidColor;clear.backgroundColor=new Color(.008f,.012f,.018f,1);clear.cullingMask=0;clear.depth=-100;
   var go=new GameObject("RoofLocalOwnedCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.transform.SetParent(transform,false);
   var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;scaler.scaleFactor=1;
   canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
   design=Rect("Design941x1672",go.transform,0,0,941,1672);design.anchorMin=design.anchorMax=Vector2.zero;design.pivot=new Vector2(0,1);
   Panel("PlainDiagnosticSurface",design,0,0,941,1672,new Color(.035f,.055f,.075f,1));
   Text("Title",design,24,20,890,48,"ROOF B LOCAL — one supported actor",36,Color.white);
   Text("Classification",design,24,77,890,55,"LOCAL_NOT_OPERATIONAL | source-owned clock / HP | no operating START",23,new Color(.6f,.9f,1));
   Panel("ViewportTopGuide",design,0,150,941,2,new Color(.2f,.4f,.5f));
   Panel("ForwardGuide",design,0,415,941,2,new Color(.2f,.4f,.5f));
   Text("ForwardGuideLabel",design,24,367,450,42,"Forward guide y415 — mapping pending",22,new Color(.6f,.75f,.85f));
   Panel("ViewportBottomGuide",design,0,1210,941,2,new Color(.2f,.4f,.5f));
   Text("Limitations",design,24,520,420,260,"Only op_eve is bound.\nApproved visual slot index 2.\nBody: neutral direction 0 / idle.\nNative facing/phase: not supplied.\nNo aimed attack or spatial FX.\nWorld muzzle / contact / roof mapping: unsupplied.\nNo background composite.",25,new Color(.75f,.85f,.9f));
   var ar=Rect("OwnedNeutralEve",design,0,0,941,1672);actor=ar.gameObject.AddComponent<FiveAgentVisualView>();if(v03!=null)actor.ConfigureV03(v03);else actor.Configure(assets);
   Panel("ApprovedFootAnchor",design,565,1000,10,10,new Color(.4f,.85f,.8f,.6f));
   Text("ActorLabel",design,635,970,280,100,"op_eve\nfoot (570,1005)\nneutral visual only",22,new Color(.7f,.9f,.9f));
   status=Text("NativeCommittedState",design,24,1232,890,150,"Waiting for explicit LOCAL source…",25,Color.white);
   error=Text("CommandError",design,24,1390,890,100,"",22,new Color(1,.65f,.55f));
   advanceButton=Button("Advance1ms",24,"Advance 1ms",()=>advanceRequested?.Invoke(),out _);
   pauseButton=Button("PauseResume",208,"Pause",()=>pauseRequested?.Invoke(),out pauseLabel);
   speedButton=Button("Speed",392,"Speed 2x",()=>speedRequested?.Invoke(),out speedLabel);
   restoreButton=Button("Restore",576,"Restore",()=>restoreRequested?.Invoke(),out _);
   autoButton=Button("Auto",760,"Auto on",()=>autoRequested?.Invoke(),out autoLabel);
   qa=Text("QaStatus",design,24,1600,890,55,"",21,new Color(.7f,.9f,.65f));
   if(EventSystem.current==null){
    eventSystemOwned=new GameObject("RoofLocalOwnedEventSystem",typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
    var moduleType=Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem",true);
    var module=eventSystemOwned.AddComponent(moduleType);
    // OnEnable already creates default action references. Keep only this owned module's
    // allocations so its teardown can release them; never collect another consumer's refs.
    ownedInputReferences=new[]{"cancel","submit","move","leftClick","rightClick","middleClick","point","scrollWheel","trackedDeviceOrientation","trackedDevicePosition"}
     .Select(name=>(UnityEngine.Object)(moduleType.GetProperty(name)??throw new MissingMemberException(moduleType.FullName,name)).GetValue(module)).Where(reference=>reference!=null).Distinct().ToArray();
#else
    eventSystemOwned.AddComponent<StandaloneInputModule>();
#endif
   }
   Fit();
  }
  static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
  void Panel(string name,Transform parent,float x,float y,float w,float h,Color color){var r=Rect(name,parent,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.color=color;im.raycastTarget=false;}
  TextMeshProUGUI Text(string name,Transform parent,float x,float y,float w,float h,string text,float size,Color color){var r=Rect(name,parent,x,y,w,h);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.fontSize=size;t.color=color;t.text=text;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;return t;}
  Button Button(string name,float x,string caption,Action clicked,out TextMeshProUGUI label){var r=Rect(name,design,x,1508,160,70);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.12f,.24f,.31f);var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(()=>clicked());label=Text("Caption",r,4,7,152,56,caption,24,Color.white);label.alignment=TextAlignmentOptions.Center;return button;}
  public void Bind(Action advance,Action pause,Action speed,Action restore,Action auto){Unbind();advanceRequested=advance;pauseRequested=pause;speedRequested=speed;restoreRequested=restore;autoRequested=auto;if(canvas!=null)canvas.gameObject.SetActive(true);}
  public void Unbind(){advanceRequested=null;pauseRequested=null;speedRequested=null;restoreRequested=null;autoRequested=null;}
  public void PresentNeutral(JObject frame){
   if(!isActiveAndEnabled||actor==null)return;
   var actors=frame?["actors"] as JArray;var layers=frame?["layers"] as JArray;
   if(actors==null||actors.Count!=1||(int)actors[0]["slot"]!=2||(string)actors[0]["runtimeId"]!="op_eve"||(int)frame["direction"]!=0||layers==null)throw new ArgumentException("Only neutral op_eve at approved slot index 2 is supported.");
   // Existing neutral baseline includes cosmetic smoke. This bounded LOCAL view consumes only the body image. No drone summon/phase is supplied.
   var copy=(JObject)frame.DeepClone();copy["layers"]=new JArray(layers.Where(l=>(string)l["kind"]=="image"&&(string)l["role"]=="actor").Select(l=>l.DeepClone()));
   if(((JArray)copy["layers"]).Any(l=>(int)l["slot"]!=2||(float)l["angle"]!=0))throw new ArgumentException("Neutral unrotated body layer required.");
   if(((JArray)copy["layers"]).Count!=1)throw new ArgumentException("Exactly one neutral body image required.");
   actor.Present(copy);NeutralActorCount=1;NeutralActorId="op_eve";NeutralSlot=2;SpatialFxCount=0;
   var body=actor.GetComponentsInChildren<RawImage>().Single();body.raycastTarget=true;
   if(body.GetComponent<Button>()==null){var input=body.gameObject.AddComponent<Button>();input.targetGraphic=body;input.transition=Selectable.Transition.None;input.onClick.AddListener(()=>operatorClicked?.Invoke());}
   var background=design.Find("PlainDiagnosticSurface").GetComponent<Image>();background.raycastTarget=true;
   if(background.GetComponent<Button>()==null){var input=background.gameObject.AddComponent<Button>();input.targetGraphic=background;input.transition=Selectable.Transition.None;input.onClick.AddListener(()=>emptyWorldClicked?.Invoke());}
  }
  public void PresentState(JObject frame,int executions,int hits,bool automatic){
   if(status==null)return;
   status.text="Committed tick "+frame["tick"]+" | cursor "+frame["cursor"]+" | revision "+frame["revision"]+"\nNative enemy HP "+frame["hp"]?["enemy"]+" | executions "+executions+" | HIT "+hits+"\n"+frame["outcome"]+" | spatial FX suppressed | neutral idle body";
   advanceButton.interactable=!automatic;
   pauseLabel.text=(bool)frame["nativePaused"]?"Resume":"Pause";
   int speed=(int?)frame["nativeSnapshot"]?["state"]?["direct"]?["clock"]?["speedHalfUnits"]??2;
   speedLabel.text=speed==4?"Speed 1x":"Speed 2x";autoLabel.text=automatic?"Auto off":"Auto on";
  }
  public void ReportError(string value){if(error!=null)error.text=value??"";}
  public void SetQaStatus(string value){if(qa!=null)qa.text=value??"";}
  public void Clear(){if(actor!=null)actor.Clear();NeutralActorCount=0;NeutralActorId=null;NeutralSlot=-1;SpatialFxCount=0;}
  public void SetVisible(bool value){if(canvas!=null)canvas.gameObject.SetActive(value);if(ownedClearCamera!=null)ownedClearCamera.enabled=value;}
  void Fit(){if(design==null)return;var safe=Screen.safeArea;float s=Mathf.Min(safe.width/941f,safe.height/1672f);design.localScale=new Vector3(s,s,1);design.anchoredPosition=new Vector2(safe.x+(safe.width-941*s)/2,safe.y+safe.height-(safe.height-1672*s)/2);previousSafe=safe;previousWidth=Screen.width;previousHeight=Screen.height;}
  void LateUpdate(){if(previousWidth!=Screen.width||previousHeight!=Screen.height||previousSafe!=Screen.safeArea)Fit();}
  public JObject GeometryMetadata(){
   if(design==null)return new JObject{["available"]=false};
   var foot=design.Find("ApprovedFootAnchor") as RectTransform;
   Vector2 screenFoot=RectTransformUtility.WorldToScreenPoint(null,foot.TransformPoint(new Vector3(5,-5,0)));
   return new JObject{["available"]=true,["canvasMode"]=canvas.renderMode.ToString(),["canvasScaleFactor"]=canvas.scaleFactor,["designSize"]=new JArray(design.sizeDelta.x,design.sizeDelta.y),["designScale"]=new JArray(design.localScale.x,design.localScale.y),["designAnchoredPosition"]=new JArray(design.anchoredPosition.x,design.anchoredPosition.y),["approvedArtFootDesign"]=new JArray(570,1005),["actualFootMarkerScreen"]=new JArray(screenFoot.x,screenFoot.y),["viewportMaskApplied"]=false,["viewportGuideOnly"]=true,["actorSurfaceFullDesignCoordinates"]=true,["spatialFxWithheld"]=true,["nativeFacingSupplied"]=false};
  }
  void OnDisable(){Unbind();Clear();SetVisible(false);}
  void OnDestroy(){Unbind();if(ownedClearCamera!=null)Destroy(ownedClearCamera.gameObject);if(eventSystemOwned!=null){eventSystemOwned.SetActive(false);Destroy(eventSystemOwned);}foreach(var reference in ownedInputReferences)if(reference!=null)Destroy(reference);ownedInputReferences=Array.Empty<UnityEngine.Object>();if(canvas!=null)Destroy(canvas.gameObject);}
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  public void QaClickAdvance()=>advanceButton.onClick.Invoke();
  public void QaClickPause()=>pauseButton.onClick.Invoke();
  public void QaClickSpeed()=>speedButton.onClick.Invoke();
  public void QaClickRestore()=>restoreButton.onClick.Invoke();
#endif
 }
}
