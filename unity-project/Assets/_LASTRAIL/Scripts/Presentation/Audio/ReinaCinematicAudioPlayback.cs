using System;
using LastRail.Presentation.Battle;
using UnityEngine;

namespace LastRail.Presentation.Audio {
 // One explicitly bound full voice+FX stem. Its baked offsets replace separate depth/voice/flare playback.
 public sealed class ReinaCinematicAudioPlayback:IDisposable {
  readonly AudioSource source;readonly AudioClip clip;readonly ReinaCinematicPlayback clock;
  double startDsp,pauseDsp,pausedDsp,lastObservedDsp;long position;
  float effectsGain,fadeGain=1;bool begun,paused,sawPlaying,disposed;
  Action<Exception> failed;
  public long PositionTicks=>position;
  public bool IsFinished=>clock.IsFinished;
  public IDisposable SessionLease {get;private set;}
  public ReinaCinematicAudioPlayback(AudioSource ownedSource,AudioClip originalFullStem,float effectsGain){
   source=ownedSource!=null?ownedSource:throw new ArgumentNullException(nameof(ownedSource));clip=originalFullStem!=null?originalFullStem:throw new ArgumentNullException(nameof(originalFullStem));
   if(clip.samples<=0||clip.frequency<=0||(long)clip.samples*TimeSpan.TicksPerSecond/clip.frequency!=ReinaCinematicPlayback.EndTicks)throw new ArgumentException("Original complete 8.95-second voice+FX stem required.");
   if(source.spatialBlend!=0||source.spatialize)throw new ArgumentException("Owned cinematic source must be 2D.");
   this.effectsGain=Gain(effectsGain);
   clock=ReinaCinematicPlayback.ForFullVoiceFx(Start,PauseSource,g=>{fadeGain=g;ApplyGain();},Stop);
  }
  public IDisposable Begin(Action ended,Action skipped,Action<Exception> failed){
   if(disposed)throw new ObjectDisposedException(nameof(ReinaCinematicAudioPlayback));if(begun)throw new InvalidOperationException("Original cinematic already began.");
   if(ended==null||skipped==null||failed==null)throw new ArgumentNullException("callbacks");this.failed=failed;begun=true;startDsp=AudioSettings.dspTime;
   SessionLease=clock.Begin(ended,skipped,failed);return SessionLease;
  }
  void Start(){
   if(source==null||!source.isActiveAndEnabled||clip.loadState!=AudioDataLoadState.Loaded)throw new InvalidOperationException("Original cinematic source and loaded full stem are required.");
   source.playOnAwake=false;source.loop=false;source.pitch=1;source.clip=clip;source.timeSamples=0;ApplyGain();source.Play();sawPlaying=source.isPlaying;
  }
  void PauseSource(bool value){if(source==null)return;if(value)source.Pause();else source.UnPause();}
  void Stop(){if(source==null)return;source.Stop();source.clip=null;}
  void ApplyGain(){if(source!=null)source.volume=effectsGain*fadeGain;}
  static float Gain(float value){if(float.IsNaN(value)||float.IsInfinity(value)||value<0||value>1)throw new ArgumentOutOfRangeException(nameof(value));return value;}
  public void SetEffectsGain(float value){effectsGain=Gain(value);if(!disposed)ApplyGain();}
  public void SetPaused(bool value){
   if(disposed||clock.IsFinished||paused==value)return;
   double now=AudioSettings.dspTime;
   SetPausedAt(value,(paused?pauseDsp:now)-startDsp-pausedDsp);
   if(value)pauseDsp=now;else pausedDsp+=now-pauseDsp;
  }
  public void Skip(){if(disposed||!begun||clock.IsFinished)return;SkipAt((paused?pauseDsp:AudioSettings.dspTime)-startDsp-pausedDsp);}
  // Explicit phase observations keep the same output-time basis as native sample observations.
  internal void SkipAt(double outputSeconds){if(disposed||!begun||clock.IsFinished)return;clock.Advance(0,OutputDelta(outputSeconds));clock.Skip();}
  internal void SetPausedAt(bool value,double outputSeconds){
   if(disposed||clock.IsFinished||paused==value)return;
   clock.Advance(0,OutputDelta(outputSeconds));paused=value;clock.SetPaused(value);
  }
  long OutputDelta(double outputSeconds){
   if(double.IsNaN(outputSeconds)||double.IsInfinity(outputSeconds)||outputSeconds<lastObservedDsp)throw new ArgumentException("Original audio output clock differs.");
   long elapsed=checked((long)Math.Round((outputSeconds-lastObservedDsp)*TimeSpan.TicksPerSecond));lastObservedDsp=outputSeconds;return elapsed;
  }
  public void Tick(){
   if(disposed||!begun||clock.IsFinished)return;
   if(source==null||source.clip!=clip){Fail(new InvalidOperationException("Owned cinematic source was replaced."));return;}
   ObserveSource(source.timeSamples,source.isPlaying,(paused?pauseDsp:AudioSettings.dspTime)-startDsp-pausedDsp);
  }
  // Sample/isPlaying/DSP observations are the native boundary; tests supply explicitly synthetic device observations.
  internal void ObserveSource(int samplePosition,bool playing,double outputSeconds){
   if(disposed||!begun||clock.IsFinished)return;
   if(samplePosition<0||samplePosition>clip.samples||double.IsNaN(outputSeconds)||double.IsInfinity(outputSeconds)||outputSeconds<lastObservedDsp){Fail(new ArgumentException("Original audio output clock/sample position differs."));return;}
   long outputDelta=OutputDelta(outputSeconds);if(paused)return;
   sawPlaying|=playing;
   long next=(long)samplePosition*TimeSpan.TicksPerSecond/clip.frequency;
   // A coarse rendering frame may observe the source after it naturally ended and reset samples to zero.
   if(!playing&&sawPlaying&&outputSeconds>=8.95){next=ReinaCinematicPlayback.EndTicks;}
   else if(!playing){Fail(new InvalidOperationException(sawPlaying?"Owned cinematic stopped before its full duration.":"Owned cinematic did not start on its first device observation."));return;}
   next=Math.Max(position,Math.Min(next,ReinaCinematicPlayback.EndTicks-1));
   if(!playing&&sawPlaying&&outputSeconds>=8.95)next=ReinaCinematicPlayback.EndTicks;
   long delta=next-position;position=next;clock.Advance(delta,outputDelta);
  }
  void Fail(Exception error){clock.Dispose();try{failed?.Invoke(error);}finally{failed=null;}}
  public void Dispose(){if(disposed)return;disposed=true;clock.Dispose();failed=null;}
 }
}
