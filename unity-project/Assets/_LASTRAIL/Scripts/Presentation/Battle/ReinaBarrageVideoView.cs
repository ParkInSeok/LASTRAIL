using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LastRail.Battle;
using LastRail.Presentation.Settings;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace LastRail.Presentation.Battle {
 // Video, its own audio and native-impact visuals share one display lifetime. No gameplay timers.
 public sealed class ReinaBarrageVideoView:MonoBehaviour {
  sealed class Burst {public RawImage Image;public AudioSource Audio;public ReinaImpactPlayback.Scheduled Item;public double AudioDue;public bool Visible,AudioStarted;}
  readonly List<Burst> bursts=new List<Burst>();
  readonly ReinaImpactPlayback impactPlayback=new ReinaImpactPlayback();
  double impactTime,impactClockSample,impactSpeed=1,dspOrigin,displayOrigin,audioPausedTime;bool hasAudioClock;
  public double MaximumAudioScheduleLateness {get;private set;}
  public int ImpactPoolSize=>bursts.Count;
  public double LastImpactAudioDsp {get;private set;}
  public double LastImpactDisplayDsp {get;private set;}
  public event Action<JObject> ImpactPresented;
  ReinaDecorativeBarrageView decoration;
  CombatSceneDriver driver;ReinaBarragePresentation owner;VideoPlayer video;
  AudioSource movieAudio;AudioClip impactClip;CombatSupportFireAssets impactAssets;Rect impactUv;
  RawImage screen;Image shade;CanvasGroup opacity;TMP_Text status;Button skip;RectTransform parent;
  Func<JObject,Vector2?> project;Func<bool> wantsFull;Func<JObject> readDisplayState;Action finish;
  bool playing,paused,applicationPaused;double lastProgress,lastVideoTime;float fadeRemaining;int remaining;
  Coroutine pendingPlayback;
  public bool FilmVisible=>playing;
  public int ImpactPresentations{get;private set;}
  public bool ImpactAudioPlaying=>bursts.Any(b=>b.Audio.isPlaying);
  public void Bind(CombatSceneDriver source,RectTransform parent,TMP_FontAsset font,CombatSupportFireAssets explosion,Func<JObject,Vector2?> projection,Func<bool> selectFull,Func<JObject> displayState=null){
   if(driver!=null)throw new InvalidOperationException("Bind once per presentation owner.");
   driver=source??throw new ArgumentNullException(nameof(source));this.parent=parent;project=projection;wantsFull=selectFull;impactAssets=explosion;readDisplayState=displayState??(()=>driver.HudState);
   impactClockSample=Time.realtimeSinceStartupAsDouble;
   if(explosion!=null){var items=(JArray)JObject.Parse(explosion.FxMetadata.text)["items"];Rect Region(string id){var crop=items.Single(x=>(string)x["id"]==id)["sourceRectTopLeftPx"];return new Rect((float)crop[0]/explosion.Effects.width,1-((float)crop[1]+(float)crop[3])/explosion.Effects.height,(float)crop[2]/explosion.Effects.width,(float)crop[3]/explosion.Effects.height);}impactUv=Region("impact_fire");}
   shade=Child<Image>("ReinaCinematicBackdrop",parent);Stretch(shade.rectTransform);shade.color=Color.black;shade.gameObject.SetActive(false);
   screen=Child<RawImage>("ReinaCinematic",parent);Stretch(screen.rectTransform);screen.color=Color.white;
   opacity=screen.gameObject.AddComponent<CanvasGroup>();screen.gameObject.SetActive(false);
   var fit=screen.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=16f/9;
   skip=Child<Button>("SkipReina",parent);var skipImage=skip.gameObject.AddComponent<Image>();skipImage.color=new Color(0,0,0,.8f);
   var r=(RectTransform)skip.transform;r.anchorMin=r.anchorMax=new Vector2(1,1);r.pivot=new Vector2(1,1);r.anchoredPosition=new Vector2(-24,-24);r.sizeDelta=new Vector2(160,50);
   var label=Child<TextMeshProUGUI>("Label",r);Stretch(label.rectTransform);label.font=font;label.text="건너뛰기";label.fontSize=22;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;
   skip.onClick.AddListener(Skip);skip.gameObject.SetActive(false);
   status=Child<TextMeshProUGUI>("ReinaBarrageStatus",parent);status.font=font;status.fontSize=24;status.alignment=TextAlignmentOptions.Center;status.raycastTarget=false;
   var sr=status.rectTransform;sr.anchorMin=sr.anchorMax=new Vector2(.5f,.8f);sr.sizeDelta=new Vector2(450,48);status.gameObject.SetActive(false);
   movieAudio=gameObject.AddComponent<AudioSource>();movieAudio.playOnAwake=false;movieAudio.spatialBlend=0;movieAudio.loop=false;
   impactClip=LastRail.Resources.PreparedContent.Load<AudioClip>("CombatPresentation/ReinaFinal/ImpactV03");
   video=gameObject.AddComponent<VideoPlayer>();video.playOnAwake=false;video.isLooping=false;video.waitForFirstFrame=true;video.skipOnDrop=false;video.renderMode=VideoRenderMode.APIOnly;
   video.audioOutputMode=VideoAudioOutputMode.AudioSource;video.controlledAudioTrackCount=1;video.SetTargetAudioSource(0,movieAudio);
   video.clip=LastRail.Resources.PreparedContent.Load<VideoClip>("CombatPresentation/ReinaFinal/CinematicV03");video.loopPointReached+=Ended;video.errorReceived+=Error;
   if(video.clip!=null)video.Prepare();
   for(int i=0;i<4;i++)CreateBurst();
   decoration=new ReinaDecorativeBarrageView(parent,project);driver.BarragePresentationEvent+=DecorativeEvent;
   Subscribe();
  }
  public bool DecorativeActive=>decoration?.Active??false;
  public int DecorativeShown=>decoration?.ShownCount??0;
  public int DecorativeLanded=>decoration?.LandedCount??0;
  public int DecorativeMaxAirborne=>decoration?.MaximumAirborne??0;
  public int DecorativeSeed=>decoration?.LastSeed??0;
  void DecorativeEvent(JObject row){if((string)row["kind"]!="CANCEL")return;string id=(string)row["commandId"];decoration.Cancel(id);impactPlayback.Cancel(id);foreach(var b in bursts)if((string)b.Item?.Row["commandId"]==id)ResetBurst(b);}
  void Subscribe(){owner=new ReinaBarragePresentation(driver,()=>video.isPrepared&&impactClip!=null&&PlayerPrefs.GetInt(UnitySettingsBackend.ReducedKey,0)==0&&wantsFull(),Play,QueueImpact,Progress);}
  static T Child<T>(string name,Transform parent) where T:Component {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);return go.AddComponent<T>();}
  static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
  IDisposable Play(Action end){
   decoration.Hide();hasAudioClock=false;finish=end;playing=true;fadeRemaining=0;lastProgress=Time.realtimeSinceStartupAsDouble;lastVideoTime=-1;opacity.alpha=1;
   shade.gameObject.SetActive(true);shade.transform.SetAsLastSibling();screen.gameObject.SetActive(true);screen.transform.SetAsLastSibling();skip.gameObject.SetActive(true);skip.transform.SetAsLastSibling();status.transform.SetAsLastSibling();
   movieAudio.volume=UnitySettingsBackend.SavedEffects;video.time=0;screen.enabled=false;pendingPlayback=StartCoroutine(PlayAfterCommitFrame());return new PlaybackLease(StopFilm);
  }
  IEnumerator PlayAfterCommitFrame(){
   // Decode the opening at a stopped clock before allowing real-time frame catch-up.
   yield return null;
   video.playbackSpeed=0;video.skipOnDrop=false;video.Play();
   while(playing&&(video.frame<0||video.texture==null||paused||applicationPaused)){
    if(!paused&&!applicationPaused&&!video.isPlaying)video.Play();
    yield return null;
   }
   if(!playing)yield break;
   video.Pause();video.time=0;video.playbackSpeed=1;video.skipOnDrop=true;
   pendingPlayback=null;lastProgress=Time.realtimeSinceStartupAsDouble;video.Play();
  }
  public void Skip(){if(!playing)return;finish?.Invoke();}
  void Ended(VideoPlayer _){finish?.Invoke();}
  void Error(VideoPlayer _,string message){Debug.LogError("Reina cinematic: "+message);finish?.Invoke();}
  void StopFilm(){if(pendingPlayback!=null){StopCoroutine(pendingPlayback);pendingPlayback=null;}playing=false;finish=null;shade.gameObject.SetActive(false);screen.gameObject.SetActive(false);skip.gameObject.SetActive(false);status.gameObject.SetActive(remaining>0);fadeRemaining=.08f;}
  void Progress(int remaining){this.remaining=remaining;int pending=remaining+impactPlayback.PendingCount;status.text=pending>0?"지원 포격 진행 중 · "+pending:"";status.gameObject.SetActive(pending>0);}
  Burst CreateBurst(){
   var image=Child<RawImage>("ReinaImpactPool",parent);image.raycastTarget=false;image.texture=impactAssets?.Effects;image.uvRect=impactUv;image.material=impactAssets?.Additive;
   image.rectTransform.anchorMin=image.rectTransform.anchorMax=Vector2.zero;image.rectTransform.pivot=new Vector2(.5f,.27f); // Visible flame base in the existing 512px atlas region.
   image.gameObject.SetActive(false);
   var audio=gameObject.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=0;audio.loop=false;audio.clip=impactClip;
   var burst=new Burst{Image=image,Audio=audio};bursts.Add(burst);return burst;
  }
  void ResetBurst(Burst b){b.Audio.Stop();b.Image.gameObject.SetActive(false);b.Item=null;b.Visible=false;b.AudioStarted=false;}
  double AudioPresentationTime=>!hasAudioClock?impactTime:paused?audioPausedTime:displayOrigin+(AudioSettings.dspTime-dspOrigin)*impactSpeed;
  void AnchorAudioClock(double now){displayOrigin=now;dspOrigin=AudioSettings.dspTime;hasAudioClock=true;}
  void ScheduleAudio(Burst b){
   double due=dspOrigin+(b.Item.Due-displayOrigin)/impactSpeed;
   double delay=due-AudioSettings.dspTime;
   MaximumAudioScheduleLateness=Math.Max(MaximumAudioScheduleLateness,Math.Max(0,-delay));
   b.Audio.Stop();b.Audio.volume=UnitySettingsBackend.SavedEffects;b.AudioDue=Math.Max(AudioSettings.dspTime+.01,due);b.Audio.PlayScheduled(b.AudioDue);
  }
  void QueueImpact(JObject row){
   // Receive only committed impacts; the short buffer absorbs receipt/frame jitter without moving damage.
   if(!project(row).HasValue)return;
   double now=impactTime+(!playing&&!paused?Math.Max(0,Time.realtimeSinceStartupAsDouble-impactClockSample)*impactSpeed:0);
   if(hasAudioClock)now=AudioPresentationTime;
   var b=bursts.FirstOrDefault(x=>x.Item==null)??CreateBurst();b.Item=impactPlayback.Enqueue(row,now);
   if(!paused&&!playing){if(!hasAudioClock)AnchorAudioClock(now);ScheduleAudio(b);}
   Progress(remaining);
  }
  void Impact(ReinaImpactPlayback.Scheduled item){
   var b=bursts.Single(x=>ReferenceEquals(x.Item,item));var row=item.Row;var point=project(row);
   if(!point.HasValue){ResetBurst(b);return;}
   if(impactAssets==null||impactClip==null)throw new InvalidOperationException("Reina impact resources missing.");
   b.Visible=true;var image=b.Image;image.name="ReinaImpact_"+row["pulseIndex"];image.rectTransform.anchoredPosition=point.Value;
   image.rectTransform.sizeDelta=Vector2.one*(PlayerPrefs.GetInt(UnitySettingsBackend.ReducedKey,0)==1?64:180);image.gameObject.SetActive(true);
   LastImpactAudioDsp=b.AudioDue;LastImpactDisplayDsp=AudioSettings.dspTime;ImpactPresentations++;ImpactPresented?.Invoke(row);
  }
  void Update(){
   if(driver==null)return;
   if(driver.IsRetired||driver.RecoveryRequired||driver.Outcome!=BasicCombatOutcome.Running){Release();return;}
   var state=readDisplayState();bool wasPaused=paused,pauseNow=applicationPaused||(bool)state["paused"];
   double now=Time.realtimeSinceStartupAsDouble,impactElapsed=Math.Max(0,now-impactClockSample);impactClockSample=now;
   if(pauseNow!=paused){if(pauseNow)audioPausedTime=AudioPresentationTime;paused=pauseNow;if(paused){if(playing||fadeRemaining>0)video.Pause();foreach(var b in bursts)if(b.Item!=null){b.AudioStarted=b.AudioDue<=AudioSettings.dspTime;if(!b.AudioStarted)b.Audio.Stop();else b.Audio.Pause();}}else{AnchorAudioClock(audioPausedTime);lastProgress=Time.realtimeSinceStartupAsDouble;if(pendingPlayback==null&&(playing||fadeRemaining>0))video.Play();foreach(var b in bursts)if(b.Item!=null){if(!b.AudioStarted)ScheduleAudio(b);else b.Audio.UnPause();}}}
   if(paused)return;
   if(!playing){
    if(!wasPaused)impactTime+=impactElapsed*impactSpeed;
    double speed=(int)state["speedHalfUnits"]/2d;
    if(speed!=impactSpeed){double phase=AudioPresentationTime;impactSpeed=speed;AnchorAudioClock(phase);foreach(var b in bursts)if(b.Item!=null&&!b.Visible&&b.AudioDue>AudioSettings.dspTime)ScheduleAudio(b);}
    var next=impactPlayback.Take(AudioPresentationTime);if(next!=null){Impact(next);Progress(remaining);}
   }
   if(playing){
    screen.texture=video.texture;screen.enabled=video.texture!=null;
    if(video.time>lastVideoTime){lastVideoTime=video.time;lastProgress=now;}
    if(now-lastProgress>3){Debug.LogError("Reina video stalled; releasing only its presentation hold.");finish?.Invoke();}
    if(playing){opacity.alpha=(float)Math.Max(0,Math.Min(1,(video.length-video.time)/.24));shade.color=new Color(0,0,0,opacity.alpha);status.gameObject.SetActive(video.time>=video.length-.4);movieAudio.volume=UnitySettingsBackend.SavedEffects;}
   }
   if(fadeRemaining>0){fadeRemaining=Mathf.Max(0,fadeRemaining-Time.unscaledDeltaTime);movieAudio.volume=UnitySettingsBackend.SavedEffects*fadeRemaining/.08f;if(fadeRemaining==0){video.Stop();video.Prepare();}}
   if(!playing)decoration.Refresh(state["reinaBarrages"] as JArray,impactTime,PlayerPrefs.GetInt(UnitySettingsBackend.ReducedKey,0)==1);
   double battleTime=AudioPresentationTime;
   foreach(var b in bursts){if(b.Item==null||!b.Visible)continue;float age=(float)(battleTime-b.Item.Due);if(age>=.7f&&!b.Audio.isPlaying)ResetBurst(b);else{b.Image.color=new Color(1,1,1,Mathf.Clamp01(1-age/.7f));b.Image.rectTransform.localScale=Vector3.one*(1+age*.5f);}}
  }
  void OnApplicationPause(bool value){applicationPaused=value;}
  void OnEnable(){impactClockSample=Time.realtimeSinceStartupAsDouble;if(driver!=null&&owner==null&&!driver.IsRetired){Subscribe();video.Prepare();}}
  void OnDisable(){
   hasAudioClock=false;impactPlayback.Clear();
   owner?.Dispose();owner=null;finish=null;playing=false;fadeRemaining=0;
   if(video!=null)video.Stop();if(movieAudio!=null)movieAudio.Stop();foreach(var b in bursts)b.Audio.Stop();
   if(screen!=null)screen.gameObject.SetActive(false);if(shade!=null)shade.gameObject.SetActive(false);if(skip!=null)skip.gameObject.SetActive(false);if(status!=null)status.gameObject.SetActive(false);
   foreach(var b in bursts)ResetBurst(b);
   decoration?.Clear();
  }
  void OnDestroy(){Release();}
  void Release(){
   impactPlayback.Clear();ImpactPresented=null;
   if(driver==null)return;driver.BarragePresentationEvent-=DecorativeEvent;decoration?.Dispose();decoration=null;owner?.Dispose();owner=null;driver=null;readDisplayState=null;
   video.loopPointReached-=Ended;video.errorReceived-=Error;video.Stop();video.clip=null;movieAudio.Stop();movieAudio.clip=null;impactClip=null;
   foreach(var b in bursts)ResetBurst(b);
   decoration?.Clear();
   if(shade!=null)ReleaseObject(shade.gameObject);if(screen!=null)ReleaseObject(screen.gameObject);if(skip!=null){skip.onClick.RemoveListener(Skip);ReleaseObject(skip.gameObject);}if(status!=null)ReleaseObject(status.gameObject);
   foreach(var b in bursts){ReleaseObject(b.Image.gameObject);if(Application.isPlaying)Destroy(b.Audio);else DestroyImmediate(b.Audio);}bursts.Clear();
   if(Application.isPlaying){Destroy(video);Destroy(movieAudio);}else{DestroyImmediate(video);DestroyImmediate(movieAudio);}
  }
  static void ReleaseObject(GameObject go){if(Application.isPlaying)Destroy(go);else DestroyImmediate(go);}
  sealed class PlaybackLease:IDisposable {Action stop;public PlaybackLease(Action stop){this.stop=stop;}public void Dispose(){var old=stop;stop=null;old?.Invoke();}}
 }
}
