using System;

namespace LastRail.Presentation.Battle {
 /// <summary>
 /// Standard Reina presentation clock only. The existing session owns its hold and visibility;
 /// the view owner keeps this component alive until its skipped audio tail has finished.
 /// Deltas are unscaled presentation and actual audio-output TimeSpan ticks, never battle time.
 /// </summary>
 public sealed class ReinaCinematicPlayback : IDisposable {
  public const long DepthStartTicks=2500*TimeSpan.TicksPerMillisecond;
  public const long EndTicks=8950*TimeSpan.TicksPerMillisecond;
  public const long SkipFadeTicks=80*TimeSpan.TicksPerMillisecond;
  enum Phase { Ready, Playing, Fading, Finished, Disposed }
  readonly Action startDepth,stopDepth;
  readonly Action<bool> pauseDepth;
  readonly Action<float> ownedFadeGain;
  readonly long audioStartTicks;
  Action ended,skipped;Action<Exception> failed;
  Phase phase;long position,fadePosition;bool paused,ownsAudio;
  public Exception LastFailure {get;private set;}
  public bool IsFinished=>phase==Phase.Finished||phase==Phase.Disposed;

  /// <param name="ownedFadeGain">Multiplier of this voice's existing mix only; never changes BGM, user volume or pitch.</param>
  public ReinaCinematicPlayback(Action startDepth,Action<bool> pauseDepth,Action<float> ownedFadeGain,Action stopDepth):this(startDepth,pauseDepth,ownedFadeGain,stopDepth,DepthStartTicks){}
  ReinaCinematicPlayback(Action startDepth,Action<bool> pauseDepth,Action<float> ownedFadeGain,Action stopDepth,long audioStartTicks){
   this.audioStartTicks=audioStartTicks;
   this.startDepth=startDepth??throw new ArgumentNullException(nameof(startDepth));this.pauseDepth=pauseDepth??throw new ArgumentNullException(nameof(pauseDepth));
   this.ownedFadeGain=ownedFadeGain??throw new ArgumentNullException(nameof(ownedFadeGain));this.stopDepth=stopDepth??throw new ArgumentNullException(nameof(stopDepth));
  }
  public static ReinaCinematicPlayback ForFullVoiceFx(Action start,Action<bool> pause,Action<float> fade,Action stop)=>new ReinaCinematicPlayback(start,pause,fade,stop,0);
  public IDisposable Begin(Action ended,Action skipped,Action<Exception> failed){
   if(phase==Phase.Disposed)throw new ObjectDisposedException(nameof(ReinaCinematicPlayback));
   if(phase!=Phase.Ready)throw new InvalidOperationException("An accepted cinematic cannot restart.");
   this.ended=ended??throw new ArgumentNullException(nameof(ended));this.skipped=skipped??throw new ArgumentNullException(nameof(skipped));this.failed=failed??throw new ArgumentNullException(nameof(failed));
   phase=Phase.Playing;if(audioStartTicks==0)try{StartOwned();}catch(Exception error){Fail(error);}return new SessionLease(this);
  }
  public void Advance(long presentationDeltaTicks,long audioOutputDeltaTicks){
   if(presentationDeltaTicks<0)throw new ArgumentOutOfRangeException(nameof(presentationDeltaTicks));
   if(audioOutputDeltaTicks<0)throw new ArgumentOutOfRangeException(nameof(audioOutputDeltaTicks));
   if(paused||phase==Phase.Ready||phase==Phase.Finished||phase==Phase.Disposed)return;
   try{
    if(phase==Phase.Fading){
     fadePosition+=Math.Min(audioOutputDeltaTicks,SkipFadeTicks-fadePosition);ownedFadeGain(1f-(float)fadePosition/SkipFadeTicks);
     if(phase==Phase.Fading&&fadePosition==SkipFadeTicks){phase=Phase.Finished;StopOwned();ClearCallbacks();}
     return;
    }
    position+=Math.Min(presentationDeltaTicks,EndTicks-position);
    if(!ownsAudio&&position>=audioStartTicks){
     // shortcut: clock notifications are frame-quantized; native binding must schedule original 6.45s audio at DSP onset and preserve its full tail before claiming audio synchronization.
     StartOwned();if(phase!=Phase.Playing)return;
    }
    if(phase==Phase.Playing&&position==EndTicks){
     phase=Phase.Finished;StopOwned();var callback=ended;ClearCallbacks();callback?.Invoke();
    }
   }catch(Exception error){Fail(error);}
  }
  void StartOwned(){ownsAudio=true;startDepth();if(phase==Phase.Playing)ownedFadeGain(1);}
  public void SetPaused(bool value){
   if(phase==Phase.Finished||phase==Phase.Disposed||paused==value)return;paused=value;
   if(ownsAudio)try{pauseDepth(value);}catch(Exception error){Fail(error);}
  }
  public void Skip(){
   if(phase!=Phase.Playing)return;
   phase=ownsAudio?Phase.Fading:Phase.Finished;var callback=skipped;ended=null;skipped=null;
   try{callback?.Invoke();}catch(Exception error){Fail(error);}
   if(phase==Phase.Finished)ClearCallbacks();
  }
  // The session disposes its lease synchronously on Skip; only that lease preserves an owned fade.
  void ReleaseSession(){if(phase!=Phase.Fading)Dispose();}
  public void Dispose(){
   if(phase==Phase.Disposed)return;phase=Phase.Disposed;
   try{StopOwned();}catch(Exception error){Report(error);}finally{ClearCallbacks();}
  }
  void StopOwned(){if(!ownsAudio)return;ownsAudio=false;stopDepth();}
  void Fail(Exception error){
   if(phase!=Phase.Disposed)phase=Phase.Finished;
   try{StopOwned();}catch(Exception cleanup){error=new AggregateException(error,cleanup);}
   Report(error);ClearCallbacks();
  }
  void Report(Exception error){LastFailure=error;try{failed?.Invoke(error);}catch(Exception callback){LastFailure=new AggregateException(error,callback);}}
  void ClearCallbacks(){ended=null;skipped=null;failed=null;}
  sealed class SessionLease:IDisposable {
   ReinaCinematicPlayback owner;
   internal SessionLease(ReinaCinematicPlayback owner){this.owner=owner;}
   public void Dispose(){var previous=owner;owner=null;previous?.ReleaseSession();}
  }
 }
}
