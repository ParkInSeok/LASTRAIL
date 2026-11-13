#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.Collections;
using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Diagnostics {
 // Owns a display-only preview and its captures; no combat driver, START, health or skill state.
 public sealed class RequestedRosterPreview:MonoBehaviour {
  [SerializeField] CombatOperatorFrames frames;
  [SerializeField] CombatPresentationAssets art;
  [SerializeField] Texture2D[] masks;
  [SerializeField] Vector4[] sourceRects;
  [SerializeField] string[] physicalIds;
  OrdinaryRoofPresentation roof;GameObject canvasObject;RectTransform root;
  readonly RawImage[] bodies=new RawImage[5];readonly RawImage[] emblems=new RawImage[5];
  RawImage roofImage;GameObject maskRoot,environmentRoot;TextMeshProUGUI heading;
  readonly Vector2[] targets=new Vector2[5];
  readonly string[] names={"레이나","카일","이브","세라","닉스"};
  public void Initialize(CombatOperatorFrames f,CombatPresentationAssets a,Texture2D[] m,Vector4[] r,string[] ids){frames=f;art=a;masks=m;sourceRects=r;physicalIds=ids;}
  RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
  RawImage Image(string name,Transform parent,Texture texture,float x,float y,float w,float h){var i=Rect(name,parent,x,y,w,h).gameObject.AddComponent<RawImage>();i.texture=texture;i.raycastTarget=false;return i;}
  TextMeshProUGUI Label(string value,float x,float y,float w,float h,int size){var t=Rect(value,root,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.font=art.Font;t.fontSize=size;t.text=value;t.color=Color.white;return t;}
  void Layout(bool source){
   roofImage.gameObject.SetActive(!source);environmentRoot.SetActive(!source);maskRoot.SetActive(source);
   heading.text=source?"승인 시안 4개 시각층 · 참고 배치 / 물리 편성 아님":"기존 3량 · 5인 표시 배치 / 전투 실행 아님";
   double[] xs={-35000,-29000,-22000,-19000,-16000};
   Vector2[] sourceFeet={new Vector2(292,374),new Vector2(412,375),new Vector2(507,376),new Vector2(609,382),new Vector2(738,391)};
   for(int i=0;i<5;i++){
    var point=roof.ProjectRelativeToFront(xs[i],3213,0);
    targets[i]=source?sourceFeet[i]*(1600f/1672):new Vector2(80+point.x*1260,-roofImage.rectTransform.anchoredPosition.y+(1-point.y)*282);
    var entry=frames.Entries[i];float height=source?65:155*.7f*.6f,scale=height/entry.BodySpan;var size=entry.SizeForHeight(height);
    bodies[i].rectTransform.sizeDelta=size;bodies[i].rectTransform.anchoredPosition=new Vector2(targets[i].x-entry.FootAnchorTopLeft.x*scale,-targets[i].y+entry.FootAnchorTopLeft.y*scale);
    if(emblems[i]!=null){var r=entry.EmblemRectTopLeft;emblems[i].rectTransform.anchoredPosition=new Vector2(r.x*scale,-r.y*scale);emblems[i].rectTransform.sizeDelta=new Vector2(r.width*scale,r.height*scale);}
   }
  }
  IEnumerator Start(){
   string output=Environment.GetEnvironmentVariable("LASTRAIL_ROSTER_PREVIEW_QA");
   if(string.IsNullOrWhiteSpace(output)||!Path.GetFullPath(output).StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D QA output required.");
   Directory.CreateDirectory(output);Directory.CreateDirectory(output+"/sequence");frames.Validate();
   if(!frames.Entries.Select(e=>e.OperatorId).SequenceEqual(new[]{"op_reina","op_kyle","op_eve","op_sera","op_nyx"}))throw new InvalidOperationException("Requested display order differs.");
   Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=30;
   canvasObject=new GameObject("RequestedRosterDisplayOnly",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));canvasObject.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
   var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=1;
   root=Rect("PreviewRoot",canvasObject.transform,0,0,1600,900);root.anchorMin=root.anchorMax=new Vector2(.5f,.5f);root.pivot=new Vector2(.5f,.5f);
   canvasObject.AddComponent<CombatSafeArea>().Bind(root,canvasObject.GetComponent<Canvas>());
   var backdrop=Image("NeutralBackground",root,Texture2D.whiteTexture,0,0,1600,900);backdrop.color=new Color(.08f,.10f,.13f);
   environmentRoot=Rect("ExistingFieldLayers",root,0,0,1600,900).gameObject;
   float fieldScale=Mathf.Max(1600f/1672,490f/480,410f/461),uw=1600/(1672*fieldScale),uh=900/(941*fieldScale);
   var uv=new Rect((1-uw)/2,1-uh-(480-490/fieldScale)/941,uw,uh);
   Image("ExistingBackground",environmentRoot.transform,art.OrdinaryFieldBackground,0,0,1600,900).uvRect=uv;
   Image("ExistingTrack",environmentRoot.transform,art.OrdinaryFieldTrack,0,0,1600,900).uvRect=uv;
   roof=new OrdinaryRoofPresentation(art.OrdinaryRoof,new JObject{["physicalInstanceIds"]=new JArray(physicalIds)});
   float top=490-(1-roof.WheelViewportY)*282;roofImage=Image("OriginalThreePhysicalCars",root,roof.Texture,80,top,1260,282);
   maskRoot=Rect("FourSourceLayersReferenceOnly",root,0,0,1600,900).gameObject;
   for(int i=0;i<masks.Length;i++){var r=sourceRects[i];Image("SourceMask"+i,maskRoot.transform,masks[i],r.x*1600/1672,r.y*1600/1672,r.z*1600/1672,r.w*1600/1672);}
   for(int i=0;i<5;i++){
    var e=frames.Entries[i];bodies[i]=Image(e.OperatorId,root,e.IdleFrames[0],0,0,1,1);
    if(e.Emblem!=null)emblems[i]=Image("IndependentEmblem",bodies[i].transform,e.Emblem,0,0,1,1);
    float x=260+i*220;var panel=Image("Card"+i,root,Texture2D.whiteTexture,x,650,205,220);panel.color=new Color(.12f,.16f,.21f);
    var portrait=Image("Portrait_"+e.OperatorId,root,e.Portrait,x+10,660,185,145);portrait.uvRect=e.PortraitUv;
    Label((i+1)+"  "+names[i],x+12,810,190,32,24);Label("표시 전용 · 전투값 없음",x+12,850,190,24,15);
   }
   heading=Label("",30,20,1500,42,28);Label("레이나 우향 승인본 적용 | 카일·이브·세라·닉스 우향 미공급",30,65,1500,35,22);
   Label("편성·능력·피해 원천 미연결 / HP·게이지·공격 판정을 만들지 않음",30,110,1500,30,20);
   float start=Time.realtimeSinceStartup;int index=0;bool source=false;Layout(false);
   while(Time.realtimeSinceStartup-start<10){
    float elapsed=Time.realtimeSinceStartup-start;if(elapsed>=5&&!source){source=true;Layout(true);}
    for(int i=0;i<5;i++)bodies[i].texture=frames.Entries[i].FrameAt(elapsed*1000);
    yield return new WaitForEndOfFrame();
    if(Time.frameCount%3!=0)continue;
    Canvas.ForceUpdateCanvases();var pixels=ScreenCapture.CaptureScreenshotAsTexture();string file=(index++).ToString("D5")+".jpg";File.WriteAllBytes(output+"/sequence/"+file,pixels.EncodeToJPG(90));Destroy(pixels);
    var placements=new JArray();
    for(int i=0;i<5;i++){var e=frames.Entries[i];var b=bodies[i].rectTransform;var actual=b.TransformPoint(new Vector2(b.rect.width*e.FootAnchorTopLeft.x/e.CanvasSize.x,-b.rect.height*e.FootAnchorTopLeft.y/e.CanvasSize.y));var expected=root.TransformPoint(new Vector2(targets[i].x-800,450-targets[i].y));if(Vector3.Distance(actual,expected)>.05f)throw new InvalidOperationException("Foot registration drift.");placements.Add(new JObject{["id"]=e.OperatorId,["slot"]=i,["footErrorPx"]=Vector3.Distance(actual,expected),["x"]=actual.x,["y"]=actual.y});}
    File.AppendAllText(output+"/sequence/frames.jsonl",new JObject{["file"]=file,["frame"]=Time.frameCount,["realtimeSeconds"]=Time.realtimeSinceStartupAsDouble,["phase"]=source?"SOURCE_MASK_REFERENCE":"THREE_CAR_PRESENTATION",["actors"]=placements}.ToString(Newtonsoft.Json.Formatting.None)+Environment.NewLine);
   }
   File.WriteAllText(output+"/result.json",new JObject{["status"]="DISPLAY_ONLY_ROSTER_CAPTURED",["width"]=Screen.width,["height"]=Screen.height,["order"]=new JArray(frames.Entries.Select(e=>e.OperatorId)),["gameplayVerified"]=false,["samples"]=index,["facing"]="REINA_APPROVED_RIGHT_OTHER_FOUR_UNSUPPLIED"}.ToString());Application.Quit(0);
  }
  void OnDestroy(){roof?.Dispose();roof=null;if(canvasObject!=null)Destroy(canvasObject);}
 }
}
#endif
