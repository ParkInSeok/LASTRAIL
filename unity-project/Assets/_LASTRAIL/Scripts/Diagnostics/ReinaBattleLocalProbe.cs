#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using LastRail.Presentation.Battle;
using LastRail.Presentation.Settings;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Video;

namespace LastRail.Diagnostics {
 public sealed class ReinaBattleLocalProbe:MonoBehaviour {
  CombatSceneDriver driver;bool current=true;
  [SerializeField] CombatPlayerAssetBinding content;
  CombatPlayerAssetBinding.Lease contentLease;
  public void Initialize(CombatPlayerAssetBinding binding){content=binding??throw new ArgumentNullException(nameof(binding));}
  bool previousBackground;
  void Awake(){previousBackground=Application.runInBackground;Application.runInBackground=true;}
  static void Need(bool ok,string reason){if(!ok)throw new InvalidOperationException(reason);}
  async void Start(){
   string output=null;int exit=1;var events=new JArray();bool sawFilm=false,sawEnd=false;long frozen=-1;double filmStart=0,filmEnd=0,firstDecodedTime=-1,firstDecodedAt=-1;int sounds=0,frame=0;
   int reduced=PlayerPrefs.GetInt(UnitySettingsBackend.ReducedKey,0);bool hadReduced=PlayerPrefs.HasKey(UnitySettingsBackend.ReducedKey);
   try{
    output=Path.GetFullPath(Environment.GetEnvironmentVariable("LASTRAIL_REINA_OUTPUT")??throw new ArgumentException("Capture path required"));
    Need(output.StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase)&&!Directory.Exists(output),"Fresh project capture path required");Directory.CreateDirectory(output);Directory.CreateDirectory(Path.Combine(output,"frames"));
    var listeners=UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Where(x=>x.isActiveAndEnabled).ToArray();
    Need(listeners.Length<=1,"One listener required for native audio capture");var listener=listeners.Length==1?listeners[0]:gameObject.AddComponent<AudioListener>();
    var audioCapture=listener.gameObject.AddComponent<ReinaReviewAudioCapture>();audioCapture.Begin();
    var input=JObject.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("LASTRAIL_REINA_INPUT")));var source=OrdinaryLocalExecutionReplay.ReadInput(input);Need(source.Commands.Count==0&&source.AugmentOwnership!=null,"Original node and ownership required");
    var factory=OrdinaryFiveCombatFactory.FromNodeStart(source.Original,source.Node,source.Scaling,500000,source.Attempt).WithLiveSources(source.GeometryJson);
    if(source.BoardingSource!=null)factory=factory.WithBoardingSource(source.BoardingSource,source.Node.BoardingSourceHash);
    factory=factory.WithLocalUninstalledInterceptor().WithAugmentOwnership(source.AugmentOwnership);
    driver=CombatSceneDriver.ForOrdinaryNode(factory,()=>current);driver.Open();driver.SetRailThrottle(7000);driver.SetUltimateAuto(true);
    File.WriteAllText(Path.Combine(output,"entry.json"),new JObject{["sourcePath"]=Environment.GetEnvironmentVariable("LASTRAIL_REINA_INPUT"),["hud"]=driver.HudState}.ToString());
    PlayerPrefs.SetInt(UnitySettingsBackend.ReducedKey,0);
    Need(content!=null,"Explicit current field binding required.");
    var formation=((JArray)driver.HudState["actors"]).Where(a=>!(bool)a["enemy"]&&!(bool)a["train"]&&!(bool)a["device"]).Select(a=>(string)a["id"]).ToArray();
    contentLease=content.Acquire(content.ManifestHash,formation);
    File.WriteAllText(Path.Combine(output,"presentation-binding.json"),content.ManifestJson);
    var host=new GameObject("ReinaActualBattle",typeof(RectTransform));var view=host.AddComponent<ApprovedCombatView>();view.Bind(driver,contentLease.ReadAssets());view.UseLandscapeLayout();
    bool consumed=false;view.ConfigurePresentation(new CombatPresentationPolicy(()=>consumed,()=>{consumed=true;return true;},false));
    var film=host.GetComponent<ReinaBarrageVideoView>();var video=host.GetComponent<VideoPlayer>();
    driver.BarragePresentationEvent+=row=>{var observed=(JObject)row.DeepClone();observed["realtime"]=Time.realtimeSinceStartupAsDouble;
     if((string)row["kind"]=="IMPACT"){
      Need(sawEnd&&!film.FilmVisible,"Impact occurred before movie ended");
     }events.Add(observed);};
    double previousDisplay=-1,previousCombatTime=0;string presentationError=null;
    film.ImpactPresented+=row=>{
     double now=Time.realtimeSinceStartupAsDouble,combatTime=(double)row["combatSeconds"];
     if(film.FilmVisible||!film.ImpactAudioPlaying)presentationError="Displayed impact and audio must start together after the movie";
     if(previousDisplay>=0){double interval=now-previousDisplay,expected=combatTime-previousCombatTime;if(interval<expected-.03)presentationError="Impact display/audio spacing collapsed";if(interval>expected+.15)presentationError="Impact display/audio spacing stretched under load";}
     previousDisplay=now;previousCombatTime=combatTime;sounds++;
     var observed=(JObject)row.DeepClone();observed["kind"]="PRESENTED";observed["realtime"]=now;observed["audioPlaying"]=film.ImpactAudioPlaying;observed["scheduledDsp"]=film.LastImpactAudioDsp;observed["displayDsp"]=film.LastImpactDisplayDsp;observed["visuals"]=film.ImpactPresentations;events.Add(observed);
    };
    int stressMs=int.TryParse(Environment.GetEnvironmentVariable("LASTRAIL_REINA_STALL_MS"),out var requestedStall)?requestedStall:0;Need(stressMs>=0&&stressMs<=200,"Bounded local render stall required");
    double previous=Time.realtimeSinceStartupAsDouble,capture=0,deadline=Time.realtimeSinceStartupAsDouble+120;
    using var frames=new StreamWriter(Path.Combine(output,"frames.jsonl"));
    using var timings=new StreamWriter(Path.Combine(output,"timings.jsonl"));
    while(current&&Time.realtimeSinceStartupAsDouble<deadline){
     if(sawEnd&&sounds<4&&stressMs>0)System.Threading.Thread.Sleep(stressMs);
     double now=Time.realtimeSinceStartupAsDouble;long elapsed=(long)(Math.Min(1,now-previous)*TimeSpan.TicksPerSecond);previous=now;var watch=System.Diagnostics.Stopwatch.StartNew();if(elapsed>0)driver.Advance(elapsed,20);
     double advanceMs=watch.Elapsed.TotalMilliseconds;watch.Restart();view.Refresh();double refreshMs=watch.Elapsed.TotalMilliseconds;
     if(driver.Tick>=1340&&driver.Tick<=1430)timings.WriteLine(new JObject{["tick"]=driver.Tick,["advanceMs"]=advanceMs,["refreshMs"]=refreshMs}.ToString(Newtonsoft.Json.Formatting.None));
     Debug.developerConsoleVisible=false;await Awaitable.EndOfFrameAsync();

     if(film.FilmVisible&&video.frame>=0&&firstDecodedTime<0){firstDecodedTime=video.time;firstDecodedAt=Time.realtimeSinceStartupAsDouble;}
     if(film.FilmVisible){if(!sawFilm){sawFilm=true;frozen=driver.Tick;filmStart=Time.realtimeSinceStartupAsDouble;}Need(driver.Tick==frozen,"Combat advanced during full cinematic");}
     else if(sawFilm&&!sawEnd){sawEnd=true;filmEnd=Time.realtimeSinceStartupAsDouble;Need(!driver.HasPresentationHold,"Movie clock lease was not released");}
     if((frame==0||driver.Tick>=1300||sawFilm)&&Time.realtimeSinceStartupAsDouble>=capture){
      capture=Time.realtimeSinceStartupAsDouble+1d/30;string name=(frame++).ToString("D5")+".jpg";var image=ScreenCapture.CaptureScreenshotAsTexture();try{File.WriteAllBytes(Path.Combine(output,"frames",name),image.EncodeToJPG(85));}finally{Destroy(image);}
      frames.WriteLine(new JObject{["file"]="frames/"+name,["tick"]=driver.Tick,["realtime"]=Time.realtimeSinceStartupAsDouble,["film"]=film.FilmVisible,["videoTime"]=video.time,["videoFrame"]=video.frame,["impacts"]=film.ImpactPresentations,["decorativeShown"]=film.DecorativeShown,["decorativeLanded"]=film.DecorativeLanded,["decorativeActive"]=film.DecorativeActive}.ToString(Newtonsoft.Json.Formatting.None));
     }
     if(sounds==4&&!film.DecorativeActive&&film.DecorativeLanded==62&&Time.realtimeSinceStartupAsDouble-(double)events.Last["realtime"]>.8){
      Need(film.DecorativeShown==62&&film.DecorativeMaxAirborne<=5,"Every decorative round must be shown, at most five airborne");
      Need(sawFilm&&sawEnd&&filmEnd-filmStart>4.5&&film.ImpactPresentations==4,"Incomplete native cinematic/impact sequence");
      Need(firstDecodedTime>=0&&firstDecodedTime<=.25,"Opening video frames were skipped");
      Need(filmEnd-firstDecodedAt>=4.5&&filmEnd-firstDecodedAt<=6.5,"Five-second movie playback speed changed");
      File.WriteAllText(Path.Combine(output,"audio-capture.json"),audioCapture.Save(Path.Combine(output,"native-audio.wav")).ToString());
      File.WriteAllText(Path.Combine(output,"result.json"),new JObject{["passed"]=presentationError==null,["sequenceCompleted"]=true,["cadenceFailure"]=presentationError,["scope"]="LOCAL_INPUT_NATIVE_PLAYER_NOT_ACCOUNT_ADMISSION",["filmSeconds"]=filmEnd-filmStart,["frozenTick"]=frozen,["events"]=events,["impacts"]=film.ImpactPresentations,["soundStarts"]=sounds,["decorativeShown"]=film.DecorativeShown,["decorativeLanded"]=film.DecorativeLanded,["decorativeMaxAirborne"]=film.DecorativeMaxAirborne,["decorativeSeed"]=film.DecorativeSeed,["injectedMainThreadStallMs"]=stressMs,["impactPoolSize"]=film.ImpactPoolSize,["maximumAudioScheduleLateness"]=film.MaximumAudioScheduleLateness,["frames"]=frame,["state"]=JObject.Parse(driver.StateJson)}.ToString());exit=0;break;
     }
     await Awaitable.NextFrameAsync();
    }
    Need(exit==0,"Battle sequence timed out");
   }catch(Exception error){Debug.LogException(error);if(output!=null){File.WriteAllText(Path.Combine(output,"failure.txt"),error+"\n"+events);if(driver!=null)File.WriteAllText(Path.Combine(output,"failure-state.json"),driver.StateJson??"null");}}
   finally{current=false;driver?.Retire();if(hadReduced)PlayerPrefs.SetInt(UnitySettingsBackend.ReducedKey,reduced);else PlayerPrefs.DeleteKey(UnitySettingsBackend.ReducedKey);}
   Application.Quit(exit);
  }
  void OnDestroy(){current=false;driver?.Retire();contentLease?.Dispose();contentLease=null;Application.runInBackground=previousBackground;}
 }
}
#endif
