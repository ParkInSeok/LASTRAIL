#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using LastRail.Presentation.Audio;
using LastRail.Presentation.Battle;
using LastRail.Presentation.Settings;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LastRail.Diagnostics {
 // Explicit local audio-device probe, not combat, server admission, listening approval or an OS pause test.
 public static class ReinaCinematicAudioPlayerCheck {
  const string Resource="CombatPresentation/Audio/ReinaCinematicVoiceFxV15";
  const string RawPath="D:/Project/LASTRAIL/QA/AudioProduction-20261020/reina-battle-audio-v15/cinematic-voice-fx-no-bgm.wav";
  const string RawSha="f874d72396c4219aa78b5faee88cb8ec9f96656cdc0dde435f55b75d74712a08";
  static void Require(bool condition,string reason){if(!condition)throw new InvalidOperationException(reason);}
  static string Hash(byte[] bytes){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
  static async Task Until(Func<bool> done,double limitSeconds,string description){var timer=Stopwatch.StartNew();while(!done()){if(timer.Elapsed.TotalSeconds>limitSeconds)throw new TimeoutException("Audio QA deadline: "+description);await Task.Yield();}}
  sealed class Lease:IDisposable {Action release;internal Lease(Action release){this.release=release;}public void Dispose(){var previous=release;release=null;previous?.Invoke();}}
  public static async Task Run(string output){
   output=Path.GetFullPath(output);Require(output.StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase),"D project QA output required.");Directory.CreateDirectory(output);
   var report=new JObject{["scope"]="AUDIO_NATIVE_PLAYER_CHECK",["serverCombat"]=false,["fullFiveCombat"]=false,["listeningPerformed"]=false,["actualOsApplicationPauseTested"]=false,["menuPauseIsScriptedExistingOwnerLease"]=true,["sessionHoldCounterIsSyntheticNoBattleClock"]=true,["resourceKey"]=Resource,["rawSourceExpectedSha256"]=RawSha,["importedPcmHashIsNotRawWavHash"]=true};
   var rows=new JArray();GameObject root=null;LastRailAudioCuePlayer player=null;AudioSource source=null;Exception failure=null;bool background=Application.runInBackground;int errors=0;string stage="load";double lastRecorded=-1,lastPolled=-1,maxPollGap=0;int dropped=0;
   void Record(bool force=false){double now=AudioSettings.dspTime;if(lastPolled>=0)maxPollGap=Math.Max(maxPollGap,now-lastPolled);lastPolled=now;if(!force&&now-lastRecorded<.025)return;lastRecorded=now;if(rows.Count>=2048){dropped++;return;}rows.Add(new JObject{["stage"]=stage,["dspTime"]=now,["realtime"]=Time.realtimeSinceStartupAsDouble,["timeSamples"]=source==null?0:source.timeSamples,["isPlaying"]=source!=null&&source.isPlaying,["hasClip"]=source!=null&&source.clip!=null,["volume"]=source==null?0:source.volume,["pitch"]=source==null?0:source.pitch});}
   Action<Exception> Failed=error=>{errors++;report["lastPlaybackError"]=error.ToString();};
   try{
    Application.runInBackground=true;var clip=LastRail.Resources.PreparedContent.Load<AudioClip>(Resource);Require(clip!=null&&clip.loadState==AudioDataLoadState.Loaded,"Supplied full cinematic resource must be loaded.");
    double duration=(double)clip.samples/clip.frequency;Require(Math.Abs(duration-8.95)<1.0/clip.frequency,"Full imported sample duration differs.");
    report["clip"]=new JObject{["name"]=clip.name,["samplesPerChannel"]=clip.samples,["channels"]=clip.channels,["frequency"]=clip.frequency,["seconds"]=duration,["loadState"]=clip.loadState.ToString()};
    var pcm=new float[checked(clip.samples*clip.channels)];Require(clip.GetData(pcm,0),"Imported PCM unavailable.");var bytes=new byte[checked(pcm.Length*sizeof(float))];Buffer.BlockCopy(pcm,0,bytes,0,bytes.Length);report["importedFloat32PcmSha256"]=Hash(bytes);pcm=null;bytes=null;
    if(File.Exists(RawPath)){using var raw=File.OpenRead(RawPath);using var sha=SHA256.Create();string actual=BitConverter.ToString(sha.ComputeHash(raw)).Replace("-","").ToLowerInvariant();report["rawSourceObservedSha256"]=actual;Require(actual==RawSha,"Preserved source WAV changed.");}else report["rawSourceObservedSha256"]=null;
    AudioSettings.GetDSPBufferSize(out int bufferLength,out int bufferCount);report["device"]=new JObject{["outputSampleRate"]=AudioSettings.outputSampleRate,["dspBufferLength"]=bufferLength,["dspBufferCount"]=bufferCount,["speakerMode"]=AudioSettings.speakerMode.ToString()};
    root=new GameObject("AUDIO_NATIVE_PLAYER_CHECK");if(!UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Any(l=>l.isActiveAndEnabled))root.AddComponent<AudioListener>();player=root.AddComponent<LastRailAudioCuePlayer>();var voice=new GameObject("OwnedFullVoiceFxOnly");voice.transform.SetParent(root.transform,false);source=voice.AddComponent<AudioSource>();source.playOnAwake=false;
    player.ConfigureReinaCinematicSource(source);player.ConfigureCombatSources(Array.Empty<AudioSource>(),null);player.BeginCombatScope(root,1);player.ApplyMixSettings(UnitySettingsBackend.SavedMusic,UnitySettingsBackend.SavedEffects);report["effectsSetting"]=UnitySettingsBackend.SavedEffects;
    stage="full-start";int ended=0;double started=AudioSettings.dspTime;player.BeginReinaCinematic(clip,()=>ended++,()=>{},Failed);Record(true);
    await Until(()=>{Record();return errors>0||source.timeSamples>=clip.frequency*.8;},5,"first full-clip progress");Require(errors==0&&source.isPlaying,"Full cinematic failed before menu pause.");
    stage="menu-pause";int pausedCheckpoint;double pauseStart=AudioSettings.dspTime;using(var hold=player.AcquireAudioHold()){
     pausedCheckpoint=source.timeSamples;Record(true);var wait=Stopwatch.StartNew();await Until(()=>{Record();return wait.Elapsed.TotalSeconds>=.3;},2,"menu pause observation");
     report["menuPause"]=new JObject{["sampleBefore"]=pausedCheckpoint,["sampleAfter"]=source.timeSamples,["preserved"]=pausedCheckpoint==source.timeSamples,["isPlayingWhileHeld"]=source.isPlaying};Require(pausedCheckpoint==source.timeSamples&&!source.isPlaying,"Menu hold did not preserve paused samples.");
    }
    double pausedDuration=AudioSettings.dspTime-pauseStart;stage="full-resumed";report["menuPause"]["sampleImmediatelyAfterRelease"]=source.timeSamples;Require(source.timeSamples>=pausedCheckpoint,"Resume restarted before the preserved sample checkpoint.");
    await Until(()=>{Record();Require(source.timeSamples>=pausedCheckpoint,"Resume rewound the preserved sample checkpoint.");return errors>0||source.timeSamples>pausedCheckpoint;},3,"resume without restart");Require(errors==0&&source.timeSamples>pausedCheckpoint,"Resume did not advance from preserved position.");
    await Until(()=>{Record();return errors>0||ended>0;},15,"full natural end");Record(true);Require(errors==0&&ended==1&&source.clip==null,"Full clip did not terminate exactly once.");
    report["naturalEnd"]=new JObject{["callbackCount"]=ended,["observedOutputSecondsExcludingMenuHold"]=AudioSettings.dspTime-started-pausedDuration,["clipReleased"]=source.clip==null};
    stage="skip-start";ReinaCinematicAudioPlayback skippedOwner=null;int holdReleases=0;
    using(var session=new UltimatePresentationSession("AUDIO_NATIVE_PLAYER_CHECK_SKIP",1,LastRail.Battle.CombatPresentationMode.Full,()=>true,()=>true,()=>true,()=>true,()=>{},()=>new Lease(()=>holdReleases++),(kind,visible,end,skip,fail)=>{skippedOwner=player.BeginReinaCinematic(clip,end,skip,fail);return skippedOwner.SessionLease;},valid=>{},Failed)){
     session.Start();await Until(()=>{Record();return errors>0||source.timeSamples>=clip.frequency*.5;},5,"skip source progress");Require(errors==0&&source.isPlaying,"Skip source did not start.");
     stage="skip-fade";maxPollGap=0;lastPolled=AudioSettings.dspTime;double skipAt=AudioSettings.dspTime;skippedOwner.Skip();Record(true);Require(holdReleases==1,"Skip did not release its session hold synchronously.");
     await Until(()=>{Record(true);return errors>0||source.clip==null;},2,"owned 80ms fade cleanup");double elapsed=AudioSettings.dspTime-skipAt;
     const double floatingArithmeticEpsilonSeconds=1e-9;
     report["skip"]=new JObject{["sessionHoldReleasedSynchronously"]=holdReleases==1,["observedDspSecondsToCleanup"]=elapsed,["approvedFadeSeconds"]=.08,["floatingArithmeticEpsilonSeconds"]=floatingArithmeticEpsilonSeconds,["notEarlierThan80ms"]=elapsed+floatingArithmeticEpsilonSeconds>=.08,["lateCleanupSeconds"]=Math.Max(0,elapsed-.08),["maximumObservedPollGapSeconds"]=maxPollGap,["measurementIsFrameObserved"]=true,["exactSampleEnvelopeVerified"]=false,["clipReleased"]=source.clip==null};Require(errors==0&&source.clip==null,"Skip tail failed to clean up.");Require(elapsed+floatingArithmeticEpsilonSeconds>=.08,"Owned fade ended before its approved 80ms output duration.");
    }
    stage="owner-dispose";var final=player.BeginReinaCinematic(clip,()=>ended++,()=>{},Failed);await Until(()=>{Record();return errors>0||source.timeSamples>=clip.frequency*.2;},5,"disposal source progress");Require(errors==0,"Disposal source failed.");final.Dispose();Record(true);Require(source.clip==null&&!source.isPlaying,"Owner disposal left a playing source.");player.EndCombatScope();report["ownerDispose"]=new JObject{["clipReleasedSynchronously"]=source.clip==null,["sourceStopped"]=!source.isPlaying};report["passed"]=true;
   }catch(Exception error){failure=error;report["passed"]=false;report["failure"]=error.ToString();}
   finally{player?.EndCombatScope();if(root!=null)UnityEngine.Object.Destroy(root);Application.runInBackground=background;report["playbackErrors"]=errors;report["sampleRows"]=rows.Count;report["omittedRows"]=dropped;File.WriteAllText(Path.Combine(output,"audio-native-player-check.json"),report.ToString());File.WriteAllText(Path.Combine(output,"audio-native-player-samples.json"),rows.ToString());}
   if(failure!=null)throw new InvalidOperationException("AUDIO_NATIVE_PLAYER_CHECK failed; inspect its bounded report.",failure);
  }
 }
}
#endif
