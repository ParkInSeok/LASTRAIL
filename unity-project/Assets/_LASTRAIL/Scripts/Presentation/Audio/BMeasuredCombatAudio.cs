#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Presentation.Audio {
 // Explicit LOCAL scene composition only. Neutral cues are trial mappings, not approved final effects.
 public sealed class BMeasuredCombatAudio:MonoBehaviour {
  public const string HitCue="combat_hit",HealCue="combat_heal",MusicCue="bgm_act1_rail_loop";
  [SerializeField] LastRailAudioCuePlayer player;
  [SerializeField] AudioSource[] voices;
  [SerializeField] AudioSource music;
  [SerializeField] LocalAudioOutputCapture output;
  [SerializeField] TextAsset manifest;
  readonly JArray events=new JArray();readonly HashSet<string> failures=new HashSet<string>();
  int resultCursor;long order;bool started,finished,missingHit,silentOutput,captureStarted;
  IDisposable nativePause;
  readonly JArray lifecycle=new JArray();
  public void Initialize(LastRailAudioCuePlayer owner,AudioSource[] sources,AudioSource bgm,LocalAudioOutputCapture capture,TextAsset provenance){
   player=owner;voices=sources;music=bgm;output=capture;manifest=provenance;
  }
  public void Begin(bool silent,bool omitHitMapping=false){
   missingHit=omitHitMapping;
   silentOutput=silent;
   if(started)throw new InvalidOperationException("LOCAL audio already started");
   started=true;player.BeginCombatScope(this,0);player.ApplyMixSettings(silent?0:1,silent?0:1);
   foreach(var voice in voices)voice.GetComponent<LocalAudioOutputCapture>().Begin(0);
   music.GetComponent<LocalAudioOutputCapture>().Begin(0);output.Begin(30);
   player.SelectBattleMusic(0,MusicCue,false);
  }
  public void Observe(JObject native,bool snapshot){
   try{ObserveOriginal(native,snapshot);}
   catch(Exception error){string key=error.GetType().Name+": "+error.Message;if(failures.Add(key))events.Add(new JObject{["status"]="audio_consumer_failure",["error"]=key});}
  }
  void ObserveOriginal(JObject native,bool snapshot){
   if(!started||finished)return;
   var paused=native?["state"]?["direct"]?["clock"]?["paused"];
   if(paused?.Type==JTokenType.Boolean){
    if((bool)paused&&nativePause==null){nativePause=player.AcquireCombatLoopHold();lifecycle.Add("native_pause");}
    else if(!(bool)paused&&nativePause!=null){nativePause.Dispose();nativePause=null;lifecycle.Add("native_resume");}
   }
   var results=native?["results"] as JArray??throw new ArgumentException("Original native results required.");
   if(snapshot){resultCursor=results.Count;return;}
   if(results.Count<resultCursor)throw new InvalidOperationException("Original result history changed without snapshot.");
   for(;resultCursor<results.Count;resultCursor++){
    foreach(JObject hit in (JArray)results[resultCursor]["hits"]){
     if(!captureStarted){output.Begin(10);captureStarted=true;}
     long current=++order;string id=(string)hit["eventId"];long tick=(long)hit["tick"];
     foreach(string outcome in CombatAudioResults.ReadNative(hit)){
      string cue=outcome=="Lifesteal"?HealCue:HitCue;
      if(outcome!="HpContact"&&outcome!="Lifesteal"){events.Add(new JObject{["eventId"]=id,["outcome"]=outcome,["status"]="unmapped"});continue;}
      var mapping=missingHit&&outcome=="HpContact"?null:new CombatAudioResultBinding(outcome,cue,outcome,3,0,true);
      // This explicit LOCAL trial shows its supplied combat actors. It grants no operating disclosure.
      string status=player.TryPlayCombatResult(current,id,"HIT",current.ToString(System.Globalization.CultureInfo.InvariantCulture),tick,outcome,mapping,
       ()=>started&&!finished&&isActiveAndEnabled,null,out _);
      var actual=status=="played"?player.LastResultSource:null;
      events.Add(new JObject{["eventId"]=id,["tick"]=tick,["resultIndex"]=resultCursor,["outcome"]=outcome,["cue"]=cue,["status"]=status,
       ["hpLoss"]=hit["hp"]["hpLoss"].DeepClone(),["actualHpGain"]=hit["lifesteal"]?["actualHpGain"]?.DeepClone()??new JValue(0),
       ["source"]=actual?actual.name:null,["sourcePlayingAtDispatch"]=actual&&actual.isPlaying,["sourceVolume"]=actual?actual.volume:0,
       ["location"]="EXPLICIT_LOCAL_NEUTRAL_NO_EVENT_POINT",["assetStatus"]=outcome=="Lifesteal"?"PROCEDURAL_PLACEHOLDER_NOT_FINAL":"APPROVED_CONTACT_TIMBRE_LOCAL_MAPPING_TRIAL"});}
    }
   }
  }
  public JObject Finish(string directory,bool requireAttack=false){
   if(finished)throw new InvalidOperationException("LOCAL audio already finished");finished=true;
   var traces=new JArray();foreach(var voice in voices){var trace=voice.GetComponent<LocalAudioOutputCapture>().Finish();trace["source"]=voice.name;traces.Add(trace);}
   var bgm=music.GetComponent<LocalAudioOutputCapture>().Finish();
   int processId=System.Diagnostics.Process.GetCurrentProcess().Id;
   var mixed=output.Finish(Path.Combine(directory,"actual-player-output-"+processId+".wav"));
   nativePause?.Dispose();nativePause=null;player.EndCombatScope();
   var result=new JObject{["classification"]="ACTUAL_LOCAL_PLAYER_TEMPORARY_SFX_TRIAL",["events"]=events,["voices"]=traces,["music"]=bgm,["output"]=mixed,
    ["allSourcesStopped"]=voices.All(v=>!v.isPlaying&&v.clip==null)&&!music.isPlaying,["manifest"]=JObject.Parse(manifest.text),
    ["processId"]=processId,["silentRequested"]=silentOutput,["missingHitMappingInjected"]=missingHit,["lifecycle"]=lifecycle,["captureResetAtFirstOriginalHit"]=captureStarted,
    ["launch"]="NOT_EXECUTED_NO_ORIGINAL_LAUNCH_EVENT",["humanListening"]=false,["mobile"]="NOT_EXECUTED"};
   File.WriteAllText(Path.Combine(directory,"audio-evidence-"+processId+".json"),result.ToString());
   if(Application.isPlaying&&!silentOutput&&(!(bool)mixed["fileCreated"]||(double)mixed["peak"]<=0))throw new IOException("Actual Player audio output was not captured; evidence retained.");
   if(failures.Count>0)throw new InvalidOperationException("Audio consumer failure; original gameplay preserved, diagnostics retained.");
   if(requireAttack){
    var recovered=events.OfType<JObject>().Where(e=>(long?)e["actualHpGain"]>0).ToArray();
    string recoveryStatus=silentOutput?"muted":"played",contactStatus=silentOutput?"muted":missingHit?"missing_result_mapping":"played";
    if(recovered.Count(e=>(string)e["outcome"]=="Lifesteal"&&(string)e["status"]==recoveryStatus)!=1||
       recovered.Count(e=>(string)e["outcome"]=="HpContact"&&(string)e["status"]==contactStatus)!=1)
     throw new InvalidOperationException("Actual attack/contact/recovery audio trial did not meet its expected statuses; evidence retained.");
   }
   return result;
  }
  public async Task VerifyPauseAndReturn(){
   int prior=events.Count;
   var other=player.AcquireAudioHold();
   try{
    player.SendMessage("OnApplicationPause",true);
    player.ApplyMixSettings(0,0);
    await Task.Delay(150);
    player.SendMessage("OnApplicationPause",false);
    if(music.isPlaying)throw new InvalidOperationException("App return released another audio hold.");
    other.Dispose();other=null;
    if(music.volume!=0||voices.Any(v=>v.clip!=null&&v.volume!=0))throw new InvalidOperationException("Return overwrote latest mute.");
    player.ApplyMixSettings(silentOutput?0:1,silentOutput?0:1);
    if(voices.Any(v=>v.pitch!=1)||music.pitch!=1||events.Count!=prior)throw new InvalidOperationException("Return changed pitch or replayed events.");
    lifecycle.Add("Player lifecycle callback injection: app return preserved other hold/latest mute; no replay; pitch1");
   }finally{other?.Dispose();}
  }
  void OnDisable(){nativePause?.Dispose();nativePause=null;player?.EndCombatScope();}
 }
}
#endif
