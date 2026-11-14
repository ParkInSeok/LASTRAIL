using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LastRail.Presentation.MissionShell;
namespace LastRail.Battle {
public sealed partial class ApprovedCombatView:MonoBehaviour {
 readonly List<Sprite> ownedPresentationSprites=new List<Sprite>();
 Sprite OwnPresentationSprite(Sprite sprite){ownedPresentationSprites.Add(sprite);return sprite;}
 void ReleasePresentationSprites(){foreach(var sprite in ownedPresentationSprites)if(sprite!=null){if(Application.isPlaying)Destroy(sprite);else DestroyImmediate(sprite);}ownedPresentationSprites.Clear();}
 Func<bool> commandAvailability;ApprovedCombatInputController inputController;
 public void SetCommandAvailability(Func<bool> available){commandAvailability=available;}
 bool CommandsAvailable=>this!=null&&isActiveAndEnabled&&(commandAvailability?.Invoke()??true);
 public void CancelPendingInput(){foreach(var button in GetComponentsInChildren<CombatInputButton>(true))button.CancelGesture();foreach(var slider in GetComponentsInChildren<CombatPointerSlider>(true))slider.CancelGesture();}
 sealed class Unit {public RectTransform Root;public RawImage Art,Shield,Emblem,ActionEffect;public bool? FacesRight;public CombatSeraCrystalView SeraCrystals;public CombatResultGlyph ShieldContour;public CombatMeter Hp;public TMP_Text Text,Fallback;public int PreviousHp;public float HitUntil,AttackUntil,RemoveAt,Born;public CanvasGroup Visibility;public CombatGroundShadow Shadow;public string Visual;public bool Enemy,Vehicle;public Vector2 Home;public float ProjectedGround;}
 sealed class Control {public CombatPanelChrome Chrome;public RawImage Portrait,AbilityIcon;public Button Button;public TMP_Text Text;public CombatMeter Fill;public string Group;public int Index;public float FillWidth;public TMP_Text CooldownText,Title;public CombatCooldownWipe Wipe;public CombatReadyFrame Rim;public long PreviousCooldown;public float ReadyAt=-1;public bool Charged;}
 sealed class Flash {public GameObject Go;public float End;public RawImage Art;}
 readonly Dictionary<string,Unit> units=new Dictionary<string,Unit>();readonly List<Control> controls=new List<Control>();readonly List<Flash> flashes=new List<Flash>();
 readonly HashSet<string> casts=new HashSet<string>();CombatSceneDriver driver;CombatPresentationAssets assets;TMP_FontAsset font;RectTransform root;CombatMeter trainHp;TMP_Text wave,trainText,notice,cutText;RawImage cutPortrait;GameObject cutin;Button pause,speed;int seenHits;float time,cutEnd;bool initial=true;int lastCharge;
 RectTransform exteriorViewport,exteriorActors;CombatDistantRail distantRail;CombatDistantDust distantDust;CombatTravelGround travelGround,travelForeground;float worldTravel;
 RawImage mapLayer,groundLayer,trainLayer,cabinLayer;CombatTrainDamageView trainDamage;RectTransform actorLayer;readonly List<CombatWindowView> windows=new List<CombatWindowView>();
 readonly List<CombatWheelDisc> wheelDiscs=new List<CombatWheelDisc>();
 readonly List<KeyValuePair<CombatSunView,Rect>> suns=new List<KeyValuePair<CombatSunView,Rect>>();
 void AddSun(RawImage surface,Rect farViewport){
  surface.gameObject.AddComponent<RectMask2D>();
  var sun=Rect("SingleWorldSun",surface.transform,0,0,surface.rectTransform.sizeDelta.x,surface.rectTransform.sizeDelta.y).gameObject.AddComponent<CombatSunView>();sun.raycastTarget=false;
  suns.Add(new KeyValuePair<CombatSunView,Rect>(sun,farViewport));
 }
 public int EntrancePresentations{get;private set;}
 CombatPresentationPolicy presentation;
 Func<double> presentationClock=()=>Time.unscaledTimeAsDouble;
#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
 public void SetCapturePresentationClock(Func<double> clock){presentationClock=clock??throw new ArgumentNullException(nameof(clock));}
#endif
 CanvasGroup cutAlpha;Button cutSkip;string presentationCommand;
 AudioSource presentationAudio;CombatAbilityAudioSet abilityAudio;float cueVolume=0.35f;
 public event Action<string,string> PresentationCue;
 public event Action<string,string> EffectPresentationCue;
 readonly CombatEffectFeedback effectFeedback=new CombatEffectFeedback();CombatEffectAudio effectAudio;
 public bool FullCutinVisible=>(cutin!=null&&cutin.activeSelf)||(reinaBarrageView!=null&&reinaBarrageView.FilmVisible);
 public string PresentationToken=>presentation?.Token;
 public void ConfigurePresentation(CombatPresentationPolicy policy){presentation=policy??throw new ArgumentNullException(nameof(policy));SkipCurrentPresentation();}
 public void ConfigurePresentationAudio(CombatAbilityAudioSet cues,float volume){
  abilityAudio=cues??throw new ArgumentNullException(nameof(cues));cueVolume=Mathf.Clamp01(volume);effectAudio=CombatEffectAudio.LoadResources();
  if(presentationAudio==null){presentationAudio=gameObject.AddComponent<AudioSource>();presentationAudio.playOnAwake=false;presentationAudio.spatialBlend=0;}
 }
 public void SetPresentationMode(int value){
  if(value<0||value>2)throw new ArgumentOutOfRangeException(nameof(value));
  presentation.Mode=(CombatPresentationMode)value;
 }
 public void SkipCurrentPresentation(){reinaBarrageView?.Skip();presentation?.Skip();if(cutin!=null)cutin.SetActive(false);if(cutSkip!=null)cutSkip.gameObject.SetActive(false);}
 void CastCue(string kind,string token,string ability){
  PresentationCue?.Invoke(kind,token);var clip=abilityAudio?.FindCastClip(ability);
  if(clip!=null&&presentationAudio!=null&&cueVolume>0)presentationAudio.PlayOneShot(clip,cueVolume*abilityAudio.CastGain(ability));
 }
 public float TravelOffset=>worldTravel;
 sealed class Mounted {public string ActorId,Type;public RawImage Image,Barrel,Muzzle;public Vector2 Home;public float KickUntil,Aim;}
 readonly List<Mounted> mounts=new List<Mounted>();
 public void SetTurret(string actorId,string type,Texture2D texture,Vector2 point){
  if(texture==null||string.IsNullOrWhiteSpace(actorId)||string.IsNullOrWhiteSpace(type))throw new ArgumentException();
  var old=mounts.FirstOrDefault(m=>m.ActorId==actorId);
  if(old!=null){DisposeVisual(old.Image.gameObject);mounts.Remove(old);}
  bool rig=type=="default-cannon"&&assets.TurretRig!=null;
  point=new Vector2(point.x+112,point.y-103);
  var image=Raw("Turret_"+actorId,trainLayer.transform,rig?assets.TurretRig:texture,point.x,point.y,rig?75:135,rig?54:90);image.transform.SetAsLastSibling();
  var mount=new Mounted{ActorId=actorId,Type=type,Image=image,Home=point};
  if(rig){image.uvRect=new Rect(0,1-470f/1024,650f/1536,470f/1024);
   mount.Barrel=Raw("RecoilBarrel",image.transform,assets.TurretRig,56,20,79,22);mount.Barrel.uvRect=new Rect(650f/1536,1-365f/1024,886f/1536,235f/1024);
   mount.Barrel.rectTransform.pivot=new Vector2(.08f,.5f);mount.Barrel.rectTransform.anchoredPosition=new Vector2(56,-28);
   mount.Muzzle=Raw("EventMuzzle",mount.Barrel.transform,assets.TurretRig,78,0,30,22);mount.Muzzle.uvRect=new Rect(770f/1536,1-1000f/1024,766f/1536,500f/1024);mount.Muzzle.gameObject.SetActive(false);
  }mounts.Add(mount);
 }

 public string MapId{get;private set;} public string SkinId{get;private set;}
 public void SetAppearance(string mapId,Texture2D map,string skinId,Texture2D exterior,Texture2D interior){if(map==null||exterior==null||interior==null||string.IsNullOrWhiteSpace(mapId)||string.IsNullOrWhiteSpace(skinId))throw new ArgumentException("Complete appearance required");map.wrapMode=TextureWrapMode.Clamp;bool canyon=mapId==assets.MapId&&map==assets.ExteriorMap&&assets.DistantFar!=null;mapLayer.texture=canyon?assets.DistantFar:map;RefreshCanyonVisibility(canyon);RefreshPursuitVisibility(canyon);if(groundLayer!=null)groundLayer.texture=map;foreach(var window in windows){window.Texture=map;window.SetMaterialDirty();}trainLayer.texture=exterior;cabinLayer.texture=interior;MapId=mapId;SkinId=skinId;}
 public int AllyDownPresentations{get;private set;}
 public int HitPresentations{get;private set;}public int DownPresentations{get;private set;}public int RecoveryPresentations{get;private set;}public int UltimatePresentations{get;private set;}
 RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
 RawImage Raw(string name,Transform p,Texture texture,float x,float y,float w,float h){var r=Rect(name,p,x,y,w,h);var a=r.gameObject.AddComponent<RawImage>();a.texture=texture;a.raycastTarget=false;return a;}
 Image Box(string name,Transform p,float x,float y,float w,float h,Color c,Sprite sprite=null){var a=Rect(name,p,x,y,w,h).gameObject.AddComponent<Image>();a.color=c;a.sprite=sprite;if(sprite!=null)a.type=Image.Type.Sliced;a.raycastTarget=false;return a;}
 CombatMeter Meter(string name,Transform parent,float x,float y,float w,float h,Color tint){var m=Rect(name,parent,x,y,w,h).gameObject.AddComponent<CombatMeter>();m.color=tint;m.raycastTarget=false;m.Divisions=name=="Fill"&&h>=12?7:1;m.RoundedEnds=name=="TrainHp";return m;}
 TMP_Text Text(string name,Transform p,string txt,float x,float y,float w,float h,int size=22){var t=Rect(name,p,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=txt;t.fontSize=size;t.color=new Color(.9f,.95f,.96f);t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;}
 Button Button(string name,string label,float x,float y,float w,float h,Action action){var image=Box(name,root,x,y,w,h,new Color(.02f,.08f,.1f,.12f));image.raycastTarget=true;var b=image.gameObject.AddComponent<CombatInputButton>();b.targetGraphic=image;var colors=b.colors;colors.disabledColor=new Color(.55f,.6f,.65f,1);b.colors=colors;b.onClick.AddListener(()=>{if(!CommandsAvailable)return;try{action();}catch{notice.text="기록 복구 필요";}});Text("Text",image.transform,label,5,3,w-10,h-6,22);return b;}
 RawImage Portrait(Transform parent,string actor,float x,float y,float w,float h){
  // A separate cut-in source is not supplied for current Kyle. Never revive the old officer fallback.
  if(actor=="op_kyle"){
   var missing=Raw("UnconnectedKyleCutinPortrait",parent,null,x,y,w,h);missing.enabled=false;return missing;
  }
  var supplied=assets.OperatorFrames?.Find(actor);
  if(supplied?.Portrait!=null){var image=Raw("ApprovedSourcePortraitCrop",parent,supplied.Portrait,x,y,w,h);var uv=supplied.PortraitUv;float aspect=(w/h)/(uv.width*supplied.Portrait.width/(uv.height*supplied.Portrait.height));image.uvRect=aspect<1?new Rect(uv.x+uv.width*(1-aspect)/2,uv.y,uv.width*aspect,uv.height):new Rect(uv.x,uv.y+uv.height*(1-1/aspect)/2,uv.width,uv.height/aspect);return image;}
  var atlas=assets.ActorAtlas(actor);var fallback=Raw("Portrait",parent,atlas??OperatorVisualCatalog.Portrait(actor),x,y,w,h);if(atlas!=null){var uv=assets.PoseUv(actor,5);float aspect=(w/h)/(uv.width*(assets.CardPolishEnabled?1536f:atlas.width)/(uv.height*(assets.CardPolishEnabled?1024f:atlas.height)));fallback.uvRect=aspect<1?new Rect(uv.x+uv.width*(1-aspect)/2,uv.y,uv.width*aspect,uv.height):new Rect(uv.x,uv.y+uv.height*(1-1/aspect)/2,uv.width,uv.height/aspect);}else fallback.uvRect=OperatorVisualCatalog.Crop((Texture2D)fallback.texture,w,h);return fallback;
 }
 RawImage Icon(Transform parent,int cell,float x,float y,float size){var i=Raw("SkillIcon",parent,assets.IconAtlas,x,y,size,size);int[] left={117,104,46,71,88,68},top={93,65,66,74,8,41},width={380,302,373,440,338,362},height={353,374,378,320,404,363};float side=Mathf.Max(width[cell],height[cell])+16;float cx=cell%3*512+left[cell]+width[cell]/2f,cy=cell/3*512+top[cell]+height[cell]/2f;i.uvRect=new Rect((cx-side/2)/1536f,1-(cy+side/2)/1024f,side/1536f,side/1024f);return i;}
 CombatLandscapeDLayout landscapeLayout;
 public void UseLandscapeLayout(){if(root==null)throw new InvalidOperationException("Bind the original combat owner first.");if(landscapeLayout!=null)return;landscapeLayout=new CombatLandscapeDLayout(root,GetComponent<Canvas>());BuildApprovedDHeader((string)driver.ReadDisplayState()["displayOperationId"]);ApplyApprovedDConsole();BuildThreatDisplay();Refresh();}
 public void BindOrRefresh(CombatSceneDriver candidate,CombatPresentationAssets art){if(root==null){Bind(candidate,art);return;}if(driver!=candidate)throw new InvalidOperationException("A different combat driver requires a fresh HUD.");Refresh();}
 public void Bind(CombatSceneDriver d,CombatPresentationAssets a){
  if(root!=null)throw new InvalidOperationException("Bind once per view.");a.OperatorFrames?.Validate();driver=d;inputController=new ApprovedCombatInputController(d,()=>CommandsAvailable);assets=a;if(d.HudState==null)throw new InvalidOperationException("Open first.");
  var canvas=GetComponent<Canvas>();if(canvas==null)canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
  var scaler=GetComponent<CanvasScaler>();if(scaler==null)scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(720,1280);scaler.matchWidthOrHeight=1;
  if(GetComponent<GraphicRaycaster>()==null)gameObject.AddComponent<GraphicRaycaster>();
  font=CombatEditorFonts.Resolve(a.Font??TMP_Settings.defaultFontAsset);
  root=Rect("ApprovedBattle",transform,0,0,720,1280);root.anchorMin=root.anchorMax=new Vector2(.5f,.5f);root.pivot=new Vector2(.5f,.5f);root.anchoredPosition=Vector2.zero;
  Box("Base",root,0,0,720,1280,new Color(.025f,.035f,.04f));
  exteriorViewport=Rect("ExteriorViewport",root,0,66,720,273);exteriorViewport.gameObject.AddComponent<RectMask2D>();
  mapLayer=Raw("MapLayer",exteriorViewport,a.ExteriorMap,0,0,720,273);mapLayer.uvRect=new Rect(0,.2f,1,.8f);
  AddSun(mapLayer,new Rect(0,0,1,1));AddCanyonLayers(false);AddPursuitWorld();
  groundLayer=Raw("MovingTrack",exteriorViewport,a.ExteriorMap,0,244,720,32);groundLayer.gameObject.SetActive(false);
  travelGround=Rect("MovingBallast",exteriorViewport,0,0,720,273).gameObject.AddComponent<CombatTravelGround>();travelGround.TerrainTexture=a.DistantTerrain??a.ExteriorMap;travelGround.raycastTarget=false;
  distantRail=Rect("DistantRails",exteriorViewport,0,0,720,273).gameObject.AddComponent<CombatDistantRail>();distantRail.raycastTarget=false;
  AddContactShadow();trainLayer=Raw("TrainSkinLayer",exteriorViewport,a.ExteriorTrain,18,133,625,290);trainLayer.rectTransform.localScale=Vector3.one*CombatExteriorLayout.TrainScale;trainLayer.rectTransform.localRotation=Quaternion.Euler(0,0,-1.75f);
  foreach(var disc in a.WheelDiscs){float sx=625f/a.ExteriorTrain.width,sy=290f/a.ExteriorTrain.height;var wheel=Rect("DriveWheel",trainLayer.transform,(disc.x-disc.z)*sx,(disc.y-disc.z)*sy,disc.z*2*sx,disc.z*2*sy).gameObject.AddComponent<CombatWheelDisc>();wheel.Texture=a.ExteriorTrain;wheel.Region=new Rect((disc.x-disc.z)/a.ExteriorTrain.width,1-(disc.y+disc.z)/a.ExteriorTrain.height,disc.z*2/a.ExteriorTrain.width,disc.z*2/a.ExteriorTrain.height);wheel.raycastTarget=false;wheelDiscs.Add(wheel);}
  trainDamage=Rect("TrainCondition",trainLayer.transform,0,0,625,290).gameObject.AddComponent<CombatTrainDamageView>();trainDamage.raycastTarget=false;
  cabinLayer=Raw("InteriorSkinLayer",root,a.Interior,0,389,720,447);
  for(int i=0;i<a.WindowOpenings.Length;i++){var opening=a.WindowOpenings[i];var glass=Rect("PassingWindow"+i,root,opening.x,opening.y,opening.width,opening.height).gameObject.AddComponent<CombatWindowView>();glass.Texture=a.ExteriorMap;glass.color=new Color(.7f,.78f,.85f,.88f);glass.raycastTarget=false;windows.Add(glass);}
  actorLayer=Rect("BattleActors",root,0,0,720,1280);exteriorActors=Rect("ExteriorActors",exteriorViewport,0,-66,720,1280);AddCanyonLayers(true);travelForeground=Rect("MovingNearRocks",exteriorViewport,0,0,720,273).gameObject.AddComponent<CombatTravelGround>();travelForeground.Foreground=true;travelForeground.TerrainTexture=a.DistantTerrain??a.ExteriorMap;travelForeground.raycastTarget=false;distantDust=Rect("TrainGroundDust",exteriorViewport,0,0,720,273).gameObject.AddComponent<CombatDistantDust>();distantDust.raycastTarget=false;distantDust.gameObject.SetActive(false);AddContactDust();
  foreach(var glass in windows)AddSun(glass,new Rect(glass.rectTransform.anchoredPosition.x/720f,.38f,glass.rectTransform.sizeDelta.x/720f,.42f));
  Box("Header",root,0,0,720,66,new Color(.035f,.055f,.065f,.96f));Text("Title",root,"LAST RAIL",15,10,225,40,32);
  wave=Text("Wave",root,"",245,10,220,36,22);speed=Button("Speed","1×",510,12,87,40,inputController.CycleSpeed);
  var pauseMenu=gameObject.AddComponent<CombatPauseController>();pauseMenu.Initialize(driver,root,font);
  pause=Button("Pause","Ⅱ",610,12,90,40,pauseMenu.Open);
  Box("TrainPanel",root,0,339,720,53,new Color(.05f,.07f,.075f));Box("TrainEdge",root,0,339,720,2,new Color(.3f,.4f,.42f));Box("TrainEdgeBottom",root,0,390,720,2,new Color(.3f,.4f,.42f));Text("TrainLabel",root,"열차 내구도",16,353,150,34,20);trainHp=Meter("TrainHp",root,173,362,340,12,new Color(.38f,.91f,.97f));trainText=Text("TrainHpText",root,"",524,355,178,32,20);
  Raw("CommandConsole",root,a.Console,0,810,720,470);
  var state=d.HudState;var rows=(JArray)state["skills"];
  if(state["ultimates"] is JArray)BuildOperatorCards(state);else{for(int i=0;i<5;i++)AddCard("skills",i,(string)rows[i]["actor"],28+i*102,867,88,180);
  AddCard("ultimate",0,(string)state["ultimate"]["actor"],541,867,151,180); }
  var manuals=(JArray)state["manuals"];
  if(manuals.Count!=3&&!(manuals.Count==0&&(string)state["manualsAvailability"]=="UNAVAILABLE"))throw new InvalidOperationException("Prepared support selection must contain three abilities.");
  if(manuals.Count==0)Text("SupportUnavailable",root,"지원 명령을 사용할 수 없습니다.",458,1010,238,66,17);
  for(int i=0;i<manuals.Count;i++){
   int index=i;string ability=(string)manuals[i]["ability"];int cell=ability=="support_fire"?3:ability=="barrier"?4:ability=="emergency_repair"?5:-1;
   if(cell<0)throw new InvalidOperationException("Missing support icon mapping: "+ability);
   var b=Button("Command"+i,"",158+i*143,1097,118,119,()=>Use("manuals",index));var well=Box("PressWell",b.transform,8,7,102,104,new Color(.015f,.04f,.055f,.05f),a.Primary);well.transform.SetAsFirstSibling();var commandIcon=Icon(b.transform,cell,26,20,66);
   var label=b.GetComponentInChildren<TMP_Text>();label.rectTransform.anchoredPosition=new Vector2(3,-88);label.rectTransform.sizeDelta=new Vector2(112,25);label.fontSize=18;
   var control=new Control{Button=b,AbilityIcon=commandIcon,Text=label,Group="manuals",Index=index};AddCooldown(control,26,20,66);controls.Add(control);b.gameObject.AddComponent<CombatCommandPress>().Configure(well,()=>presentationClock());
  }
  DressHeader();BuildUltimateAuto();BuildSecondFinalConsole();
  notice=Text("Notice",root,"",15,1230,690,30,17);
  // Top-right sky pocket: keep both lanes, actor silhouettes and command cards visible.
  cutin=Rect("LeaderCutin",root,300,78,406,132).gameObject;cutAlpha=cutin.AddComponent<CanvasGroup>();
  Box("CutFrame",cutin.transform,0,0,406,132,new Color(.025f,.06f,.09f,.96f),a.Primary);
  Box("CutAccent",cutin.transform,0,0,3,132,new Color(.23f,.85f,1));
  var initialUltimate=state["ultimates"] is JArray allUltimates?allUltimates[0]:state["ultimate"];
  cutPortrait=Portrait(cutin.transform,(string)((JArray)state["actors"]).Single(actor=>(string)actor["id"]==(string)initialUltimate["actor"])["visual"],8,6,112,120);
  Text("CutLabel",cutin.transform,operatorCards.Count>0?"궁극기":"분대장",133,12,255,25,16);
  cutText=Text("CutText",cutin.transform,CombatSkillNames.Get((string)initialUltimate["ability"]),130,38,264,55,27);
  cutin.SetActive(false);
  cutSkip=Button("SkipPresentation","건너뛰기",605,177,94,28,SkipCurrentPresentation);cutSkip.GetComponentInChildren<TMP_Text>().fontSize=14;cutSkip.gameObject.SetActive(false);
  if(presentation==null){
   // Unscoped/restored callers fail toward simplified. The host supplies owner/run marker ports.
   bool used=false;presentation=new CombatPresentationPolicy(()=>used,()=>{used=true;return true;},(long)state["tick"]>0);
   presentation.Mode=LastRail.Presentation.Settings.UnitySettingsBackend.ReadCombatPresentationMode();
  }
  seenHits=((JArray)state["hits"]).Count;foreach(var c in (JArray)state["casts"])casts.Add((string)c["id"]);effectFeedback.Seed((JArray)state["effectResults"]);artilleryOutcome.Seed(state);dangerFeedback.Seed(state);
  if(abilityAudio==null)ConfigurePresentationAudio(CombatAbilityAudioSet.LoadResources(),.65f);
  BindReinaBarrage();
  SetAppearance(a.MapId,a.ExteriorMap,a.SkinId,a.ExteriorTrain,a.Interior);int mountIndex=0;foreach(var actor in (JArray)state["actors"])if((bool)actor["train"]||((string)actor["deviceType"]=="Gun"))SetTurret((string)actor["id"],"default-cannon",a.DefaultTurret,new Vector2(90+mountIndex++*125,138));Refresh();if(a.OperatorRigs!=null)a.OperatorRigs.Install(this);initial=false;BindSupportFire(state);BindElectromagnetic(state);BindRepairDrone(state);displayBound=true;BindDisplayChanges();
 }
 void AddCooldown(Control c,float x,float y,float size){
  c.Wipe=Rect("CooldownWipe",c.Button.transform,x,y,size,size).gameObject.AddComponent<CombatCooldownWipe>();c.Wipe.color=new Color(0,.015f,.025f,.82f);c.Wipe.raycastTarget=false;
  c.CooldownText=Text("CooldownSeconds",c.Button.transform,"",x,y,size,size,20);c.CooldownText.fontStyle=FontStyles.Bold;
  c.Rim=Rect("ReadyFrame",c.Button.transform,x-2,y-2,size+4,size+4).gameObject.AddComponent<CombatReadyFrame>();c.Rim.raycastTarget=false;
 }
 void AddCard(string group,int index,string actor,float x,float y,float w,float h){
  bool leader=group=="ultimate";var b=Button(group+index,"",x,y,w,h,()=>Use(group,index));
  var portrait=Portrait(b.transform,actor,3,leader?31:2,w-6,leader?87:83);
  TMP_Text title=null;if(leader)title=Text("LeaderTitle",b.transform,"",3,1,w-6,28,17);
  else Text("OperatorName",b.transform,OperatorVisualCatalog.Label(actor),0,85,w,20,14);
  float fy=leader?130:107;var fill=Meter("Fill",b.transform,4,fy,w-8,leader?15:7,new Color(.38f,.93f,.97f));
  var txt=b.GetComponentInChildren<TMP_Text>();txt.rectTransform.anchoredPosition=new Vector2(1,leader?-149:-157);txt.rectTransform.sizeDelta=new Vector2(w-2,leader?27:25);txt.fontSize=leader?22:13;txt.enableAutoSizing=true;txt.fontSizeMin=11;txt.fontSizeMax=leader?22:13;
  var control=new Control{Button=b,Portrait=portrait,Text=txt,Title=title,Fill=fill,FillWidth=w-8,Group=group,Index=index};
  DressCard(control,leader,w,h);
  if(!leader){control.AbilityIcon=ActiveIcon(b.transform,(string)Row(driver.ReadDisplayState(),group,index)["ability"],(w-41)/2,116,41);AddCooldown(control,(w-41)/2,116,41);}
  else{control.Rim=Rect("LeaderReadyFrame",b.transform,-5,-10,w+10,h+12).gameObject.AddComponent<CombatReadyFrame>();control.Rim.raycastTarget=false;}
  controls.Add(control);
 }
 JToken Row(JObject s,string group,int i)=>group=="ultimate"?s[group]:s[group][i];
 void Use(string group,int i)=>inputController.Use(group,i);
 void Effect(Unit u,Texture tex,Color color,float duration=.28f){
  if(tex==null)return;
  var bounds=u.Art.rectTransform.rect;
  var art=Raw("ResolvedEffect",u.Art.transform,tex,bounds.width*.5f-50,bounds.height*.5f-50,100,100);art.color=color;flashes.Add(new Flash{Go=art.gameObject,Art=art,End=time+duration});
 }
 Texture2D SelectActorFigure(string schema,string visual,bool vehicle,string animation,int frame,bool lethalHit,out Texture2D atlas){
  atlas=null;
  // shortcut: ordinary gunner art is unavailable; bind only a role-approved independent source when supplied.
  if(schema==OrdinaryFiveCombatFactory.Version&&visual=="enemy_spike_cart_gunner")return null;
  atlas=assets.ActorAtlas(visual);
  return atlas??(vehicle?(lethalHit&&visual=="rail_artillery_emplacement_v1"?SecondArt("artillery_emplacement-v01"):visual.Contains("railbike")?assets.Bike:assets.Buggy):assets.Find(visual+"/"+animation+"/"+frame)??assets.Find(visual+"/idle/1"));
 }
 void RefreshFigureFallback(Unit u){
  bool missing=u.Art.texture==null&&!captureRigs.ContainsKey(u.Visual);
  u.Art.enabled=u.Art.texture!=null;
  if(missing&&u.Fallback==null){u.Fallback=Text("MissingFigure",u.Root,OperatorCardName(u.Visual),0,146,150,30,18);u.Fallback.raycastTarget=false;}
  if(u.Fallback!=null)u.Fallback.gameObject.SetActive(missing);
 }
 static JToken LatestPresentationCast(JArray casts,string actor,string visual){
  var latest=casts?.Where(c=>(string)c["actor"]==actor&&((string)c["ability"]==visual+".active"||(string)c["ability"]==visual+".ultimate")).OrderByDescending(c=>(long)c["due"]-(long)c["windupTicks"]).FirstOrDefault();
  return (string)latest?["status"]=="CASTING"||(string)latest?["status"]=="APPLIED"?latest:null;
 }
 bool ApplyApprovedIdle(Unit unit,double milliseconds,JToken state=null,long tick=0,JArray casts=null,double stepMilliseconds=50){
  if(unit.Enemy||unit.Vehicle)return false;
  var entry=assets.OperatorFrames?.Find(unit.Visual);if(entry==null||!entry.HasIdle)return false;
  bool alive=state!=null&&(int)state["hp"]>0;
  bool basic=alive&&((string)state["basicStatus"]=="PREPARING"||(string)state["basicStatus"]=="RECOVERY");
  var cast=alive?LatestPresentationCast(casts,(string)state["id"],unit.Visual):null;
  double elapsed=cast==null?-1:milliseconds-((long)cast["due"]-(long)cast["windupTicks"])*stepMilliseconds;
  bool ultimate=(string)cast?["ability"]==unit.Visual+".ultimate";
  if(unit.FacesRight.HasValue)entry=assets.OperatorFrames.Find(unit.Visual,unit.FacesRight.Value);
  Texture2D active=elapsed<0?null:ultimate?entry.UltimateFrameAt(elapsed):entry.ActiveFrameAt(elapsed);
  string targetId=basic?(string)state["attackTarget"]:active!=null?(string)cast?["target"]:null;
  if(targetId!=null&&units.TryGetValue(targetId,out var target)&&target!=unit&&Mathf.Abs(target.Root.position.x-unit.Root.position.x)>.01f){
   entry=assets.OperatorFrames.Find(unit.Visual,target.Root.position.x>unit.Root.position.x);unit.FacesRight=entry.FacesRight;
   active=elapsed<0?null:ultimate?entry.UltimateFrameAt(elapsed):entry.ActiveFrameAt(elapsed);
  }
  // Authored exposures follow committed action state; they never schedule gameplay effects.
  CombatOperatorFrames.Entry.Registration registration=active==null?null:ultimate?entry.UltimateRegistration:entry.ActiveRegistration;
  var art=unit.Art;art.gameObject.SetActive(true);
  art.texture=basic?entry.BasicFrameAt(tick,(long?)state["basicStartedAt"]??-1,(long?)state["basicCommitAt"]??-1,(long?)state["recoverAt"]??-1,milliseconds,state["attackTarget"]?.Type==JTokenType.String):active??entry.FrameAt(milliseconds);
  if(basic)registration=entry.BasicFrames.Contains(art.texture)?entry.BasicRegistration:null;
  art.uvRect=new Rect(0,0,1,1);
  art.rectTransform.sizeDelta=entry.SizeForHeight(155,registration);art.rectTransform.anchoredPosition=entry.TopLeftForHeight(155,registration);art.rectTransform.localScale=Vector3.one;art.rectTransform.localRotation=Quaternion.identity;
  var effect=!basic&&alive&&elapsed>=0?entry.EffectFrameAt(ultimate,elapsed):null;
  if(effect!=null&&unit.ActionEffect==null)unit.ActionEffect=Raw("ApprovedIndependentActionEffect",unit.Root,effect,0,0,1,1);
  if(unit.ActionEffect!=null){unit.ActionEffect.gameObject.SetActive(effect!=null);unit.ActionEffect.texture=effect;if(effect!=null){var r=ultimate?entry.UltimateRegistration:entry.ActiveRegistration;unit.ActionEffect.rectTransform.sizeDelta=entry.SizeForHeight(155,r);unit.ActionEffect.rectTransform.anchoredPosition=entry.TopLeftForHeight(155,r);}}
  if(unit.Visual=="op_sera"&&assets.SeraCastingCrystal!=null&&assets.SeraIdleCrystal!=null&&assets.SeraCrystalShader!=null){
   if(unit.SeraCrystals==null){unit.SeraCrystals=Rect("ApprovedSeraCrystals",unit.Root,0,0,150,155).gameObject.AddComponent<CombatSeraCrystalView>();unit.SeraCrystals.Bind(assets.SeraCastingCrystal,assets.SeraIdleCrystal,assets.SeraCrystalShader);}
   string motion=basic&&entry.BasicFrames.Contains(art.texture)?"basic":active!=null?(ultimate?"ultimate":"active"):"idle";
   double age=elapsed;
   if(motion=="basic"){
    long start=(long)state["basicStartedAt"],commit=(long)state["basicCommitAt"],end=(long)state["recoverAt"];
    double before=entry.BasicDurationsMs.Take(entry.BasicFireFrame).Sum(),after=entry.BasicDurationsMs.Skip(entry.BasicFireFrame).Sum();
    age=tick<commit?before*(tick-start)/(commit-start):before+after*(tick-commit)/(end-commit);
   }
   RectTransform targetArt=targetId!=null&&units.TryGetValue(targetId,out var recipient)?recipient.Art.rectTransform:null;
   unit.SeraCrystals.Render(alive,entry.FacesRight,motion,Math.Max(0,age),milliseconds,art.rectTransform,targetArt);
  }
  if(entry.Emblem!=null){float scale=155/entry.BodySpan;var region=entry.EmblemRectTopLeft;if(unit.Emblem==null)unit.Emblem=Raw("ApprovedIndependentEmblem",art.transform,entry.Emblem,region.x*scale,region.y*scale,region.width*scale,region.height*scale);}
  unit.Hp.rectTransform.anchoredPosition=new Vector2(22,2);
  RefreshFigureFallback(unit);return true;
 }
 bool displayBound,transientRendering;
 public void Refresh(){
  GetComponent<CombatSafeArea>()?.Apply();
  if(driver==null||driver.IsRetired)return;var s=driver.ReadDisplayState();reinaDisplayState=s;time=(long)s["tick"]*.05f;bool paused=(bool)s["paused"];if(paused!=wasInputPaused)lastVisualClock=presentationClock();if(paused&&!wasInputPaused)CancelPendingInput();wasInputPaused=paused;RefreshDangerAudio(s);
  RefreshRailControls(s);float combatTime=(float)s["motionSeconds"];float travelTime=s["railJourney"] is JObject rail?(float)rail["distanceMm"]/10000f:(float)s["motionSeconds"];worldTravel=Mathf.Min(.06f,travelTime*.0007f);mapLayer.uvRect=new Rect(worldTravel,0,.94f,1);float travelDistance=travelTime*CombatTravelGround.Speed;distantRail.SetTravel(travelDistance);travelGround.SetDistance(travelDistance);travelForeground.SetDistance(travelDistance*1.25f);
  foreach(var sun in suns){sun.Key.gameObject.SetActive(assets.SeparateSun&&MapId==assets.MapId&&(mapLayer.texture==assets.ExteriorMap||mapLayer.texture==assets.DistantFar));sun.Key.SetProjection(time,sun.Value);}
  float trainBob=Mathf.Sin(travelTime*15)*.6f+Mathf.Sin(travelTime*6.7f)*.3f;trainLayer.rectTransform.anchoredPosition=new Vector2(CombatExteriorLayout.TrainTopLeft.x,-(CombatExteriorLayout.TrainTopLeft.y-66)+trainBob*CombatExteriorLayout.TrainScale);
  var dustPoint=exteriorViewport.InverseTransformPoint(trainLayer.rectTransform.TransformPoint(new Vector2(130f/assets.ExteriorTrain.width*625,-640f/assets.ExteriorTrain.height*290)));distantDust.SetState(travelTime,dustPoint);
  foreach(var wheel in wheelDiscs){wheel.gameObject.SetActive(SkinId==assets.SkinId);wheel.SetDistance(travelDistance/CombatExteriorLayout.TrainScale);}RefreshContacts(travelTime,CombatTravelGround.Speed);RefreshPursuit(travelTime);
  foreach(var window in windows)window.SetProjection(new Rect(worldTravel,.25f,.9f,.75f),window.rectTransform.anchoredPosition.x/720f,window.rectTransform.rect.width/720f);
  wave.text=(string)s["ordinaryObstacle"]?["phase"]=="OBSTACLE"?"장애물 구간":(string)s["executionPhase"]!=null?"전투 준비 대기":"웨이브 "+s["wave"]+"/"+s["totalWaves"];if(hasDConsole)trainHp.SetValue((float)(int)s["trainHp"]/Mathf.Max(1,(int)s["trainMaxHp"]));else trainHp.SetHealth((int)s["trainHp"],(int)s["trainMaxHp"],(int)((JArray)s["actors"]).Single(a=>(bool)a["train"])["shield"]);trainText.text=s["trainHp"]+" / "+s["trainMaxHp"];
  trainDamage.SetCondition((int)s["trainHp"],(int)s["trainMaxHp"],time);RefreshHeaderPolish((int)s["wave"],(int)s["totalWaves"],(string)s["outcome"]=="Victory",(float)s["trainHp"]/(float)s["trainMaxHp"]);
  RefreshHullText((int)s["trainHp"],(int)s["trainMaxHp"]);
  speed.GetComponentInChildren<TMP_Text>().text=((int)s["speedHalfUnits"]/2f).ToString("0.#")+"×";pause.GetComponentInChildren<TMP_Text>().text=paused?"▶":"Ⅱ";pauseGlyph?.SetSymbol(paused?CombatHeaderGlyph.Symbol.Play:CombatHeaderGlyph.Symbol.Pause);
  if(dPauseBars!=null){dPauseBars.SetActive(!paused);pauseGlyph.gameObject.SetActive(paused);}
  EnsureOrdinaryRoof(s);RefreshOrdinaryRoofEnemies(s);RefreshOrdinaryObstacle(s);var actors=(JArray)s["actors"];ValidateInformation(actors);var active=new HashSet<string>();
  foreach(var r in actors){
   if((bool)r["train"]||(bool)r["device"])continue;string id=(string)r["id"];active.Add(id);
   bool enemy=(bool)r["enemy"],ext=(bool)r["exterior"];int slot=(int)r["slot"],hp=(int)r["hp"];
   if(!units.TryGetValue(id,out var u)){
    int index=slot;Vector2 pos;
    if(ext)pos=new Vector2(470+(index%2)*85,218+(index/2)*35);
    else if(enemy)pos=new Vector2(465+(index%2)*91,534+(index/2)*71+(index%2)*24);
    else pos=slot<3?new Vector2(8,515+slot*57):new Vector2(169,544+(slot-3)*76);
    var body=Rect(id,ext?exteriorActors:actorLayer,pos.x,pos.y,150,155);
    var shadow=Rect("GroundContact",body,29,150,102,19).gameObject.AddComponent<CombatGroundShadow>();shadow.color=new Color(0,0,0,.65f);shadow.raycastTarget=false;
    u=new Unit{Root=body,Shadow=shadow,Visibility=body.gameObject.AddComponent<CanvasGroup>(),Born=CombatExteriorLayout.EntryBorn(initial,time,(long)r["availableAt"]*.05f),Art=Raw("Unit",body,null,0,0,150,150),Hp=Meter("Health",body,22,2,105,7,enemy?new Color(.965f,.455f,.376f):new Color(.38f,.93f,.94f)),Shield=Raw("Shield",body,assets.Shield,0,0,150,150),Text=Text("State",body,"",0,127,150,27,16),PreviousHp=hp,Enemy=enemy,Visual=(string)r["visual"],Vehicle=ext,Home=pos};if(!ext){u.Art.rectTransform.sizeDelta=new Vector2(180,180);u.Shield.rectTransform.sizeDelta=new Vector2(180,180);}else{u.Art.rectTransform.sizeDelta=new Vector2(170,113);u.Hp.rectTransform.anchoredPosition=new Vector2(25,0);}
    u.ShieldContour=Rect("ActualShieldContour",body,10,10,28,28).gameObject.AddComponent<CombatResultGlyph>();u.ShieldContour.Shape=CombatResultShape.ShieldIssue;u.ShieldContour.raycastTarget=false;u.ShieldContour.Opacity=.32f;u.ShieldContour.SetProgress(.35f);if(enemy)EntrancePresentations++;units.Add(id,u);BindActorInformation(id,u);
   }
   if(!enemy&&!ext)u.Home=slot<3?new Vector2(8,515+slot*57):new Vector2(169,544+(slot-3)*76);
   u.Root.gameObject.SetActive((long)r["availableAt"]<=(long)s["tick"]);u.Hp.SetHealth(hp,(int)r["maxHp"],(int)r["shield"]);
   if(!initial&&u.PreviousHp>0&&hp==0){DownPresentations++;if(!enemy)AllyDownPresentations++;}
   u.ShieldContour.gameObject.SetActive(false);
   bool guard=(int?)r["kyleGuardShield"]>0&&hp>0&&(string)s["outcome"]=="Running";
   var guardView=u.Shield.GetComponent<CombatKyleShieldView>();
   if(guard&&guardView==null)guardView=u.Shield.gameObject.AddComponent<CombatKyleShieldView>();
   u.Shield.gameObject.SetActive(guard);
   if(guardView!=null)guardView.Render(guard,combatTime,assets.OperatorFrames?.Find(u.Visual)?.FacesRight==true);
   // HP increases alone do not identify a heal/repair effect. Await typed effect results.
   if(!initial&&u.PreviousHp==0&&hp>0){RecoveryPresentations++;Effect(u,assets.Shield,new Color(.2f,1,1,.8f),.4f);}
   u.PreviousHp=hp;
   float entrance=enemy?Mathf.Clamp01((time-u.Born)/(u.Vehicle?1.6f:.72f)):1;u.Visibility.alpha=u.Vehicle?Mathf.Lerp(.35f,1,Mathf.Clamp01(entrance*5)):Mathf.Clamp01(entrance*5);
   long attackDue=(long)r["attackDue"];float windup=attackDue>=0?1-Mathf.Clamp01((attackDue-combatTime/.05f)/(float)Mathf.Max(1,(int)r["attackWindup"])):0;
   float returnUntil=0;foreach(var recent in (JArray)s["casts"])if((string)recent["actor"]==id&&(string)recent["status"]=="APPLIED")returnUntil=Mathf.Max(returnUntil,(long)recent["due"]*.05f+.14f);
   string anim=hp==0?"down_or_defeated":((bool)r["casting"]||returnUntil>combatTime)?"skill_or_special":((bool)r["attacking"]&&windup>(u.Visual=="op_kyle"?.68f:.30f))||u.AttackUntil>time?"attack":"idle";
   if(enemy&&!u.Vehicle&&entrance<1&&hp>0)anim="idle";
   int frame=hp==0?4:1+(int)(time*10)%4;
   bool suppliedIdle=ApplyApprovedIdle(u,hp<=0?0:combatTime*1000d,r,(long)s["tick"],(JArray)s["casts"],(long)s["stepTimeSpanTicks"]/10000d);
   if(!suppliedIdle){u.Art.texture=SelectActorFigure((string)s["schema"],u.Visual,u.Vehicle,anim,frame,false,out var atlas);
   if(atlas==null)u.Art.uvRect=new Rect(0,0,1,1);
   RefreshFigureFallback(u);
   if(atlas!=null){int cell=anim=="attack"?1:anim=="down_or_defeated"?3:anim=="skill_or_special"?4:0;var uv=assets.PoseUv(u.Visual,cell);u.Art.uvRect=uv;u.Art.rectTransform.sizeDelta=new Vector2(155*uv.width*atlas.width/(uv.height*atlas.height),155);u.Art.rectTransform.localScale=new Vector3(u.Enemy&&cell==4?-1:1,1,1);}
   if(atlas!=null){int cell=anim=="attack"?1:anim=="down_or_defeated"?3:anim=="skill_or_special"?4:0;float top=160-155*assets.FootBottom(u.Visual,cell);u.Art.rectTransform.anchoredPosition=new Vector2(u.Enemy&&cell==4?u.Art.rectTransform.sizeDelta.x:0,-top);}
   if(atlas!=null&&!u.Vehicle){int leanCell=anim=="attack"?1:anim=="down_or_defeated"?3:anim=="skill_or_special"?4:0;float kick=Mathf.Clamp01((u.AttackUntil-combatTime)/.22f);CabinPoseLean(u,leanCell,hp==0?0:(enemy?-1:1)*(kick*1.3f-windup*.7f)+Mathf.Sin(combatTime*2.1f+slot)*.12f);}}
   if(guard){u.Shield.rectTransform.sizeDelta=new Vector2(180,180);u.Shield.rectTransform.anchoredPosition=new Vector2(-15,15);}
   u.ShieldContour.rectTransform.anchoredPosition=u.Hp.rectTransform.anchoredPosition+new Vector2(u.Hp.rectTransform.sizeDelta.x+2,-u.Hp.rectTransform.sizeDelta.y/2+14);u.Shadow.gameObject.SetActive(!u.Vehicle);
   u.Art.color=hp==0?new Color(.6f,.6f,.65f,.7f):u.HitUntil>time?Color.Lerp(Color.white,new Color(1,.4f,.35f),Mathf.Clamp01((u.HitUntil-time)/.16f)):Color.white;
   if(u.Emblem!=null)u.Emblem.color=u.Art.color;
   RefreshCaptureRig(u,windup,combatTime,hp);
   float release=Mathf.Clamp01((u.AttackUntil-time)/.22f);
   float lunge=hp==0?0:(attackDue>=0?(windup<.68f?-3*windup:Mathf.SmoothStep(0,12,(windup-.68f)/.32f)):12*release*release);
   float entryX=enemy?(760-u.Home.x)*Mathf.Pow(1-entrance,3):0;
   u.Root.anchoredPosition=new Vector2(u.Home.x+entryX+(u.Vehicle?(enemy?-lunge:lunge):0),-u.Home.y+(u.Vehicle&&hp>0?Mathf.Sin(time*16)*.8f:0));
   ScaleCabinUnit(u);if(u.Vehicle){float depth=CombatExteriorLayout.ActorScale(slot,entrance,u.Visual);var ground=PursuitActive?CombatPursuitLayout.Contact(slot,entrance,travelTime):CombatExteriorLayout.ApproachGround(slot,entrance);u.ProjectedGround=ground.y;u.Root.localScale=Vector3.one*depth;u.Root.anchoredPosition=new Vector2(ground.x-85*depth,-ground.y+CombatExteriorLayout.WheelBottom(u.Visual)*depth);}
   if(!u.Vehicle&&!PlaceOrdinaryRoofActor(r,u,anim=="attack"?1:anim=="down_or_defeated"?3:anim=="skill_or_special"?4:0))landscapeLayout?.PlaceActor(u.Root,slot,enemy,new Vector2(entryX,0));
   u.Text.text=hp==0?(enemy?"":"회복 "+Math.Max(0,((long)r["recoverAt"]-(long)s["tick"])*.05f).ToString("0.0")):(bool)r["protected"]?"보호 중":"";
   if((bool?)r["partyMoving"]==true)u.Text.text="이동 "+(slot+1);
  }
  foreach(var id in units.Keys.Where(k=>!active.Contains(k)).ToArray()){var stale=units[id];if(stale.RemoveAt==0)stale.RemoveAt=time+.4f;else if(time>=stale.RemoveAt){DisposeVisual(stale.Root.gameObject);units.Remove(id);actorIncarnations.Remove(id);}}
  foreach(var unit in units.Values.OrderBy(u=>u.Vehicle?u.ProjectedGround:u.Home.y))unit.Root.SetAsLastSibling();
  landscapeLayout?.ApplyOverlays();

  var hits=(JArray)s["hits"];
  for(int i=seenHits;i<hits.Count;i++){
   var hit=hits[i];var mount=mounts.FirstOrDefault(m=>m.ActorId==(string)hit["Source"]);if(mount!=null)mount.KickUntil=time+.3f;
   if((long)s["tick"]-(long)hit["Tick"]>8)continue;CannonContact(s,hit,mount);
   OnCaptureRigShot(hit);
   if(units.TryGetValue((string)hit["Source"],out var source))source.AttackUntil=(long)hit["Tick"]*.05f+.22f;
   if(units.TryGetValue((string)hit["Target"],out var target)&&((int)hit["HpLoss"]>0||(int)hit["Absorbed"]>0)){
    if((int)hit["RemainingHp"]==0){
     if(target.PreviousHp>0){DownPresentations++;target.PreviousHp=0;}target.Hp.SetValue(0);target.Shield.gameObject.SetActive(false);
     if(!ApplyApprovedIdle(target,0)){target.Art.texture=SelectActorFigure((string)s["schema"],target.Visual,target.Vehicle,"down_or_defeated",4,true,out var deathAtlas);RefreshFigureFallback(target);if(deathAtlas!=null){target.Art.uvRect=assets.PoseUv(target.Visual,3);target.Art.rectTransform.localScale=Vector3.one;target.Art.rectTransform.anchoredPosition=new Vector2(0,-(160-155*assets.FootBottom(target.Visual,3)));target.Art.rectTransform.sizeDelta=new Vector2(155*target.Art.uvRect.width*deathAtlas.width/(target.Art.uvRect.height*deathAtlas.height),155);}if(!target.Vehicle)CabinPoseLean(target,3,0);}
     target.Art.color=new Color(.6f,.6f,.65f,.7f);
    }
    target.HitUntil=(long)hit["Tick"]*.05f+.16f;if((int)hit["RemainingHp"]>0)target.Art.color=new Color(1,.4f,.35f);if(target.Emblem!=null)target.Emblem.color=target.Art.color;if(target.Vehicle)Effect(target,(int)hit["Absorbed"]>0?assets.Shield:assets.Hit,Color.white);HitPresentations++;
   }
  }seenHits=hits.Count;
  foreach(var m in mounts){float kick=Mathf.Clamp01((m.KickUntil-time)/.3f);float recoil=8*kick*kick;var actor=actors.FirstOrDefault(v=>(string)v["id"]==m.ActorId);bool aiming=actor!=null&&(bool)actor["attacking"];
   m.Image.rectTransform.anchoredPosition=new Vector2(m.Home.x,-m.Home.y);m.Image.color=actor!=null&&(int)actor["hp"]>0?Color.white:Color.gray;
   if(m.Barrel!=null){var targetId=(string)actor?["attackTarget"];if(targetId!=null&&units.TryGetValue(targetId,out var targetUnit)){var targetPosition=(Vector2)m.Image.rectTransform.InverseTransformPoint(targetUnit.Art.rectTransform.TransformPoint(targetUnit.Art.rectTransform.rect.center));var pivot=new Vector2(56,-28);float angle=Mathf.Atan2(targetPosition.y-pivot.y,targetPosition.x-pivot.x)*Mathf.Rad2Deg;m.Aim=railWorld==null?Mathf.Clamp(angle,-35,65):angle;}m.Barrel.rectTransform.anchoredPosition=new Vector2(56-recoil,-28);m.Barrel.rectTransform.localRotation=Quaternion.Euler(0,0,m.Aim);m.Muzzle.gameObject.SetActive(kick>.7f);}
   else m.Image.rectTransform.anchoredPosition+=new Vector2(-recoil,0);
  }
  foreach(var c in (JArray)s["casts"]){
   if(!casts.Add((string)c["id"]))continue;
   // Barrage presentation starts from the published commit, never this preparation decoration.
   if((bool?)c["reinaBarrage"]==true)continue;
   if((string)c["status"]!="CASTING")continue;
   CastCue((string)c["kind"],(string)c["id"],(string)c["ability"]);
   // Preparation decoration only; APPLIED is not an effect-success receipt.
   if((string)c["kind"]=="ULTIMATE"){
    if(operatorCards.Count>0)UpdateCastPortrait((string)c["actor"]);
    UltimatePresentations++;presentationCommand=(string)c["id"];
    bool warning=actors.Any(x=>(bool)x["enemy"]&&(bool)x["attacking"]&&(long)x["attackDue"]<=(long)s["tick"]+4);
    presentation.Begin(presentationCommand,presentationClock(),Math.Max(0,((long)c["due"]-(long)s["tick"])*.05/(Math.Max(2,(int)s["speedHalfUnits"])/2d)),warning);
    cutText.text=CombatSkillNames.Get((string)c["ability"]);
   }
  }
  foreach(JObject result in (JArray)s["effectResults"]){
   if(!effectFeedback.Observe(result)||(string)s["outcome"]!="Running")continue;
   if(effectAudio!=null&&effectAudio.Find(result,out var clip,out var gain)&&effectFeedback.ReserveAudio(result)){
    EffectPresentationCue?.Invoke((string)result["kind"],(string)result["id"]);if(presentationAudio!=null&&cueVolume>0)presentationAudio.PlayOneShot(clip,Mathf.Clamp01(gain*cueVolume/.65f));
   }
   ResultVisual(s,result);
  }
  UpdateResultVisuals(s);RefreshSupportFire(s);RefreshElectromagnetic(s);RefreshRepairDrone(s);
  var currentCast=((JArray)s["casts"]).FirstOrDefault(c=>(string)c["id"]==presentationCommand);
  if(driver.RecoveryRequired||(string)s["outcome"]!="Running"||currentCast!=null&&(string)currentCast["status"]!="CASTING")presentation.Skip();
  if(ImminentEnemyWarning(s))presentation.Skip();
  RenderCutin();
  foreach(var f in flashes.ToArray()){if(time>=f.End){DisposeVisual(f.Go);flashes.Remove(f);}else f.Art.color=new Color(f.Art.color.r,f.Art.color.g,f.Art.color.b,Mathf.Clamp01((f.End-time)*4));}
  foreach(var c in controls){
   var row=Row(s,c.Group,c.Index);c.Button.interactable=!driver.RecoveryRequired&&(bool)row["enabled"];
   bool leader=c.Group=="ultimate";string status=(string)row["status"];
   long? knownRemaining=(long?)row["cooldownMs"];int? knownCharge=(int?)row["charge"];long remaining=knownRemaining??0;RefreshCardChrome(c,status,c.Button.interactable,leader?knownCharge??0:0,time);
   c.Text.text=leader?(knownCharge.HasValue?knownCharge.Value+"%":"미확인"):CombatSkillNames.Get((string)row["ability"]);
   if(c.Title!=null){c.Title.text=CombatSkillNames.Get((string)row["ability"]);cutText.text=c.Title.text;}
   if(c.Fill!=null){if(leader){c.Fill.gameObject.SetActive(knownCharge.HasValue);if(knownCharge.HasValue)c.Fill.SetValue(knownCharge.Value/100f);}else c.Fill.SetHealth((int)row["hp"],(int)row["maxHp"],(int)row["shield"]);}
   if(leader){
    bool charged=knownCharge==100&&c.Button.interactable;if(charged&&!c.Charged)c.ReadyAt=time;c.Charged=charged;
    c.Rim.SetLight(charged?Mathf.Clamp01((time-c.ReadyAt)/.45f):0,.7f+.15f*Mathf.Sin(time*2.6f));
   }else{
    c.Wipe.gameObject.SetActive(knownRemaining.HasValue);if(knownRemaining.HasValue)c.Wipe.SetRemaining(remaining/(float)Math.Max(1,(long)row["cooldownTotalMs"]));
    c.CooldownText.text=CombatCooldownText.Format(knownRemaining);
    if(knownRemaining.HasValue&&c.Button.interactable&&c.PreviousCooldown>0&&remaining==0)c.ReadyAt=time;
    float glow=c.Button.interactable&&c.ReadyAt>=0?Mathf.Clamp01(1-(time-c.ReadyAt)/.6f):0;c.Rim.SetLight(glow>0?1:0,glow);c.PreviousCooldown=remaining;
   }

  }
  
  RefreshCaptureRigShots(combatTime);RefreshThreatDisplay(s);
  landscapeLayout?.ApplyOverlays();
  RefreshRailWorld(s,travelTime);RefreshOperatorCards(s);RefreshApprovedDConsole(s);lastCharge=s["ultimate"] is JObject legacyUltimate?(int)legacyUltimate["charge"]:0;notice.text=driver.RecoveryRequired?"전투 기록 복구 필요":(string)s["outcome"]=="Failure"?"작전 실패":(string)s["ordinaryObstacle"]?["phase"]=="OBSTACLE"?((bool?)s["fullCombatReady"]==true?(paused?"일시 정지":""):"장애물 구간 준비 중"):(string)s["executionPhase"]=="AWAITING_ENCOUNTER"?"전투 진입 정보 대기 중":(string)s["executionPhase"]=="AWAITING_NATIVE_GEOMETRY"?"전투 공간 정보 대기 중":paused?"일시 정지":(string)s["outcome"]=="Running"?"":(string)s["outcome"]=="Victory"?"작전 완료":"작전 실패";
  ScheduleTransientRendering();
 }
 static void DisposeVisual(GameObject go){if(Application.isPlaying)Destroy(go);else DestroyImmediate(go);}
 void RenderCutin(){
  presentation.Update(presentationClock());
  bool show=presentation.Active&&presentation.Full;
  if(show&&!cutin.activeSelf){CancelPendingInput();SetThreatDetails(false);}cutin.SetActive(show);cutSkip.gameObject.SetActive(show);
  if(show){
   float age=(float)(presentationClock()-presentation.Started),duration=(float)presentation.Duration;
   float enter=Mathf.Clamp01(age/.055f),leave=Mathf.Clamp01((duration-age)/.12f);
   cutAlpha.alpha=Mathf.Min(enter,leave);
   cutin.GetComponent<RectTransform>().anchoredPosition=new Vector2(300+24*(1-enter)*(1-enter)+12*(1-leave),-78);
   cutin.transform.SetAsLastSibling();cutSkip.transform.SetAsLastSibling();
  }
 }
 void BindDisplayChanges(){if(!displayBound||!isActiveAndEnabled)return;driver.StateChanged-=OwnerDisplayChanged;driver.StateChanged+=OwnerDisplayChanged;}
 void UnbindDisplayChanges(){StopDangerAudio();if(driver!=null)driver.StateChanged-=OwnerDisplayChanged;StopTransientRendering();}
 void OwnerDisplayChanged(){if(this==null){UnbindDisplayChanges();return;}if(!displayBound||!isActiveAndEnabled)return;if(driver.IsRetired){UnbindDisplayChanges();CancelPendingInput();SkipCurrentPresentation();return;}Refresh();}
 // Only active, procedural real-time fades need render samples; no state reads or idle polling here.
 bool HasTransientRendering()=>presentation?.Active==true ||
   (reinaDisplayState!=null&&!(bool)reinaDisplayState["paused"]&&(resultMarks.Count>0||sourceLines.Count>0||kyleGrants.Count>0)) ||
   operatorCards.Any(card=>card.DCharge!=null&&card.DReady&&presentationClock()-card.DReadyAt<.35);
 void ScheduleTransientRendering(){if(!isActiveAndEnabled||!HasTransientRendering()) {StopTransientRendering();return;}if(!transientRendering){transientRendering=true;Canvas.willRenderCanvases+=RenderTransientPresentation;}}
 void StopTransientRendering(){Canvas.willRenderCanvases-=RenderTransientPresentation;transientRendering=false;}
 void RenderTransientPresentation(){
  if(this==null||!displayBound||!isActiveAndEnabled||driver==null||driver.IsRetired){StopTransientRendering();return;}
  RenderCutin();
  if(reinaDisplayState!=null&&!(bool)reinaDisplayState["paused"]&&(resultMarks.Count>0||sourceLines.Count>0||kyleGrants.Count>0))UpdateResultVisuals(reinaDisplayState);
  foreach(var card in operatorCards)if(card.DCharge!=null&&card.DReady)RenderDCardLight(card);
  landscapeLayout?.ApplyOverlays();
  if(!HasTransientRendering())StopTransientRendering();
 }

}
}

