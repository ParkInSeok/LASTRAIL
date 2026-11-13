#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LastRail.Diagnostics {
 // Authored local input exercises native owners; captures are observations, never admission or resume data.
 public sealed class KyleGuardLocalPlayerProbe : MonoBehaviour {
  CombatSceneDriver driver;
  ApprovedCombatView view;
  [SerializeField] CombatPlayerAssetBinding content;
  CombatPlayerAssetBinding.Lease contentLease;
  public void Initialize(CombatPlayerAssetBinding binding){content=binding??throw new ArgumentNullException(nameof(binding));}
  bool current=true;
  readonly System.Collections.Generic.List<Texture2D> localTextures=new System.Collections.Generic.List<Texture2D>();
  static void Need(bool ok,string reason){if(!ok)throw new InvalidOperationException(reason);}
  static string LocalPath(string value){
   var path=Path.GetFullPath(value??throw new ArgumentException("Explicit local QA path required."));
   Need(path.StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase),"D project QA path required.");return path;
  }
  internal static int LiveKyleTaunts(JArray rows,long tick)=>rows.Where(t=>(string)t["source"]=="op_kyle"&&(long)t["expiry"]>tick).Select(t=>(string)t["target"]).Distinct().Count();
  public static void SelfCheck(){
   var rows=new JArray();foreach(string id in new[]{"a","b","c","c"})rows.Add(new JObject{["source"]="op_kyle",["target"]=id,["expiry"]=20});
   Need(LiveKyleTaunts(rows,19)==3&&LiveKyleTaunts(rows,20)==0,"Taunt observation expiry/distinct check failed.");
  }
  async void Start(){
   string output=null;StreamWriter frames=null,ticks=null;LocalReviewVideoRecorder video=null;int exit=2;
   try{
    SelfCheck();Application.runInBackground=true;Application.targetFrameRate=30;QualitySettings.vSyncCount=0;
    string inputPath=LocalPath(Environment.GetEnvironmentVariable("LASTRAIL_KYLE_LOCAL_INPUT"));
    string requestedOutput=LocalPath(Environment.GetEnvironmentVariable("LASTRAIL_KYLE_LOCAL_OUTPUT"));
    Need(!Directory.Exists(requestedOutput),"Fresh capture directory required.");Directory.CreateDirectory(requestedOutput);output=requestedOutput;Directory.CreateDirectory(Path.Combine(output,"frames"));
    int limit=int.Parse(Environment.GetEnvironmentVariable("LASTRAIL_KYLE_LOCAL_TICKS")??"1000");Need(limit>0&&limit<=10000,"Tick limit must be 1..10000.");
    var input=JObject.Parse(File.ReadAllText(inputPath));var source=OrdinaryLocalExecutionReplay.ReadInput(input);
    if(source.Original.Rail.LocalFiveCar)Need(limit%2==0,"Local 10fps capture requires an even native tick limit.");
    Need(source.Commands.Count==0,"This probe starts at the node beginning; prior input replay is forbidden.");
    long step=source.Node.ReadActions(source.Original).StepTimeSpanTicks;Need(step==500000,"Expected native 50 ms step.");
    var factory=OrdinaryFiveCombatFactory.FromNodeStart(source.Original,source.Node,source.Scaling,step,source.Attempt).WithLiveSources(source.GeometryJson);
    if(source.BoardingSource!=null)factory=factory.WithBoardingSource(source.BoardingSource,source.Node.BoardingSourceHash);
    factory=factory.WithLocalUninstalledInterceptor();driver=CombatSceneDriver.ForOrdinaryNode(factory,()=>current);driver.Open();driver.SetRailThrottle(7000);
    Need(driver.IsExecutionReady,"Native execution sources unavailable.");
    Need(content!=null,"Explicit current field binding required; legacy Resources fallback is not a current battlefield.");
    var formation=((JArray)driver.HudState["actors"]).Where(a=>!(bool)a["enemy"]&&!(bool)a["train"]&&!(bool)a["device"]).Select(a=>(string)a["id"]).ToArray();
    contentLease=content.Acquire(content.ManifestHash,formation);var assets=contentLease.ReadAssets();
    File.WriteAllText(Path.Combine(output,"presentation-binding.json"),content.ManifestJson);
    var host=new GameObject("KyleLocalNativeCombat",typeof(RectTransform));view=host.AddComponent<ApprovedCombatView>();
    if(source.Original.Rail.LocalFiveCar){
     string folder=LocalPath(Environment.GetEnvironmentVariable("LASTRAIL_LOCAL_TRAIN_DIR"));
     Texture2D Load(string name){var t=new Texture2D(2,2,TextureFormat.RGBA32,false);Need(t.LoadImage(File.ReadAllBytes(Path.Combine(folder,name+".png"))),"Local train image decode failed.");t.name=name;t.wrapMode=TextureWrapMode.Clamp;localTextures.Add(t);return t;}
     var engine=Load("engine");var front=Load("front-wagon");var middle=Load("middle-wagon");var rear=Load("rear-wagon");view.ConfigureLocalFiveCarField(new[]{engine,front,middle,middle,middle,rear});
    }
    view.Bind(driver,assets);view.UseLandscapeLayout();
    File.WriteAllText(Path.Combine(output,"source.json"),input.ToString());
    File.WriteAllText(Path.Combine(output,"scope.json"),new JObject{["classification"]="LOCAL_AUTHORED_INPUT_NOT_SERVER_ADMISSION",["interceptor"]="UNINSTALLED",["automaticSupport"]="NATIVE_UNMODIFIED",["sourcePath"]=inputPath,["tickLimit"]=limit,["approval"]="NOT_ASSERTED"}.ToString());
    if(source.Original.Rail.LocalFiveCar)video=new LocalReviewVideoRecorder(Path.Combine(output,"review.mp4"));
    frames=new StreamWriter(Path.Combine(output,"frames.jsonl"));ticks=new StreamWriter(Path.Combine(output,"ticks.jsonl"));
    int localTransferCount=0;long nextLocalTransferTick=-1;
    int frame=0,previousShield=0,issueCount=0,threeCount=0,endCount=0,previousTaunts=0,effectCount=0,actionCount=0;long expiry=0,lastTick=-1,totalAbsorbed=0;
    double next=Time.realtimeSinceStartupAsDouble+.05,deadline=Time.realtimeSinceStartupAsDouble+Math.Max(600,limit*.5);
    while(current){
     Need(Time.realtimeSinceStartupAsDouble<deadline,"Bounded local Player deadline exceeded.");
     if(driver.Tick<limit&&Time.realtimeSinceStartupAsDouble>=next&&driver.Outcome==BasicCombatOutcome.Running){int stride=source.Original.Rail.LocalFiveCar?2:1;driver.Advance(step*stride,stride);next+=.05*stride;}
     if(source.Original.Rail.LocalFiveCar&&driver.Outcome==BasicCombatOutcome.Running){var before=driver.HudState;string phase=(string)before["ordinaryObstacle"]?["phase"];string party=(string)before["localParty"]?["phase"];if(localTransferCount==1&&party=="Complete"&&nextLocalTransferTick<0)nextLocalTransferTick=driver.Tick+100;if(phase=="COMBAT"&&(localTransferCount==0||localTransferCount==1&&party=="Complete"&&driver.Tick>=nextLocalTransferTick)){driver.RequestLocalPartyEnd(localTransferCount==0);localTransferCount++;}}
     view.Refresh();Debug.developerConsoleVisible=false;await Awaitable.EndOfFrameAsync();if(!current)break;
     var hud=driver.HudState;long tick=driver.Tick;if(video!=null&&tick==lastTick){await Awaitable.NextFrameAsync();continue;}if(video!=null&&lastTick>=0)Need(tick==lastTick+2,"Video requires consecutive two-tick native samples.");var state=JObject.Parse(driver.StateJson);
     var actor=((JArray)hud["actors"]).Single(a=>(string)a["id"]=="op_kyle");int shield=(int)actor["kyleGuardShield"];
     var body=view.GetComponentsInChildren<RectTransform>(true).Single(t=>t.name=="op_kyle");
     var art=body.Find("Unit").GetComponent<UnityEngine.UI.RawImage>();
     var guard=body.Find("Shield");
     Need(body.gameObject.activeInHierarchy&&(art.isActiveAndEnabled||(bool?)actor["partyMoving"]==true)&&art.texture!=null,"Deployed Kyle artwork disappeared.");
     Need(guard.gameObject.activeInHierarchy==(shield>0&&(int)actor["hp"]>0&&driver.Outcome==BasicCombatOutcome.Running),"Kyle shield presentation differs from native health state.");
     int taunts=LiveKyleTaunts((JArray)state["taunts"],tick);string image=(frame++).ToString("D6")+".jpg";
     byte[] captured;var texture=ScreenCapture.CaptureScreenshotAsTexture();Need(texture!=null,"Visible Player capture is unavailable.");try{captured=texture.EncodeToJPG(85);}finally{Destroy(texture);}
     bool keepFrame=video==null||tick%20==0;if(keepFrame)File.WriteAllBytes(Path.Combine(output,"frames",image),captured);video?.Write(captured);
     frames.WriteLine(new JObject{["file"]=keepFrame?"frames/"+image:null,["videoFrame"]=frame-1,["localPartyPhase"]=hud["localParty"]?["phase"]?.DeepClone(),["tick"]=tick,["frame"]=Time.frameCount,["realtimeSeconds"]=Time.realtimeSinceStartupAsDouble,["shield"]=shield,["liveKyleTauntTargets"]=taunts}.ToString(Formatting.None));
     if(tick!=lastTick){
      var effects=(JArray)state["effects"];var actions=(JArray)state["actions"];
      var addedEffects=effects.Skip(effectCount).ToArray();
      long absorbed=addedEffects.Where(e=>(string)e["targetId"]=="op_kyle").Sum(e=>(long?)e["actualResult"]?["absorbed"]??0);totalAbsorbed+=absorbed;
      if(video==null||tick%20==0||addedEffects.Length>0||shield!=previousShield||taunts!=previousTaunts)ticks.WriteLine(new JObject{["tick"]=tick,["hud"]=hud,["actions"]=actions,["taunts"]=state["taunts"],["newEffects"]=new JArray(addedEffects),["kyleAbsorbedThisTick"]=absorbed,["kyleAbsorbedTotal"]=totalAbsorbed,["lastObservedShieldExpiry"]=expiry}.ToString(Formatting.None));
      void Snapshot(string name){File.WriteAllBytes(Path.Combine(output,name+".jpg"),captured);File.WriteAllText(Path.Combine(output,name+".json"),new JObject{["observationOnly"]=true,["tick"]=tick,["hud"]=hud.DeepClone(),["state"]=state.DeepClone()}.ToString());}
      if(lastTick<0)Snapshot("entry");
      foreach(var effect in effects.Skip(effectCount).Where(e=>(string)e["sourceId"]=="op_kyle"&&(string)e["kind"]=="SHIELD_ISSUE")){
       expiry=(long)effect["actualResult"]["expiry"];Snapshot("shield-issue-"+(++issueCount));
      }
      if(taunts>=3&&previousTaunts<3)Snapshot("three-live-taunts-"+(++threeCount));
      if(previousShield>0&&shield==0){string reason=(int)actor["hp"]<=0?"down":tick>=expiry?"expiry":absorbed>0?"depletion":"early-zero-unclassified";Snapshot("shield-ended-"+reason+"-"+(++endCount));}
      previousShield=shield;previousTaunts=taunts;effectCount=effects.Count;actionCount=actions.Count;lastTick=tick;
     }
     if(tick>=limit||driver.Outcome!=BasicCombatOutcome.Running){
      File.WriteAllText(Path.Combine(output,"final-state.json"),state.ToString());
      File.WriteAllText(Path.Combine(output,"observations.json"),new JObject{["recordingCompleted"]=true,["approval"]="NOT_ASSERTED",["visualReview"]="PENDING",["tick"]=tick,["outcome"]=driver.Outcome.ToString(),["localTransfersRequested"]=localTransferCount,["localPartyPhase"]=hud["localParty"]?["phase"]?.DeepClone(),["localBothDirectionsCompleted"]=localTransferCount==2&&(string)hud["localParty"]?["phase"]=="Complete",["shieldIssuesObserved"]=issueCount,["threeLiveTauntsObserved"]=threeCount,["shieldEndsObserved"]=endCount,["kyleAbsorbedTotal"]=totalAbsorbed,["actions"]=actionCount,["frames"]=frame}.ToString());exit=0;break;
     }
     await Awaitable.NextFrameAsync();
    }
   video?.Finish();if(video!=null)File.WriteAllText(Path.Combine(output,"video.json"),new JObject{["finalized"]=true,["fps"]=10,["nativeStepMs"]=50,["captureIntervalTicks"]=2,["consecutiveNativeSamples"]=true,["jpegFallbackIntervalTicks"]=20}.ToString());
   }catch(Exception error){exit=2;Debug.LogException(error);if(output!=null&&Directory.Exists(output))File.WriteAllText(Path.Combine(output,"failure.txt"),error.ToString());}
   finally{video?.Dispose();frames?.Dispose();ticks?.Dispose();Release();}
   Application.Quit(exit);
  }
  void Release(){current=false;if(view!=null){view.enabled=false;view.ReleaseOwnedPresentationResources();Destroy(view.gameObject);view=null;}foreach(var t in localTextures)Destroy(t);localTextures.Clear();contentLease?.Dispose();contentLease=null;driver?.Retire();driver=null;}
  void OnDestroy(){Release();}
 }
}
#endif
