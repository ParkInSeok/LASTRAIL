using System;
using System.Collections.Generic;
using LastRail.Battle;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.Battle {
 // Owns presentation leases only; the committed combat owner supplies every impact.
 public sealed class ReinaBarragePresentation:IDisposable {
  readonly CombatSceneDriver driver;
  readonly Func<bool> ready;
  readonly Func<Action,IDisposable> play;
  readonly Action<JObject> impact;
  readonly Action<int> progress;
  readonly Dictionary<string,int> pulses=new Dictionary<string,int>(StringComparer.Ordinal);
  IDisposable film,hold;bool disposed,playing;long playbackGeneration;
  public ReinaBarragePresentation(CombatSceneDriver driver,Func<bool> ready,Func<Action,IDisposable> play,Action<JObject> impact,Action<int> progress){
   this.driver=driver??throw new ArgumentNullException(nameof(driver));this.ready=ready??throw new ArgumentNullException(nameof(ready));
   this.play=play??throw new ArgumentNullException(nameof(play));this.impact=impact??throw new ArgumentNullException(nameof(impact));this.progress=progress??throw new ArgumentNullException(nameof(progress));
   if(driver.HudState?["reinaBarrages"] is JArray active)foreach(JObject row in active)pulses.Add((string)row["commandId"],(int)row["nextPulse"]);
   driver.BarragePresentationEvent+=Receive;PublishProgress();
  }
  void Receive(JObject row){
   if(disposed||driver.IsRetired||driver.RecoveryRequired)return;
   string command=(string)row["commandId"],kind=(string)row["kind"];
   if(string.IsNullOrWhiteSpace(command))throw new ArgumentException("Committed barrage command required.");
   if(kind=="COMMIT"){
    if(pulses.ContainsKey(command))return;pulses.Add(command,0);PublishProgress();
    if(playing||!ready())return;
    hold=driver.AcquirePresentationHold();if(hold==null)return;playing=true;
    long generation=++playbackGeneration;
    try{var lease=play(()=>{if(!disposed&&generation==playbackGeneration)FinishFilm();});if(playing)film=lease;else lease?.Dispose();}
    catch{FinishFilm();throw;}
   }else if(kind=="IMPACT"){
    if(!pulses.TryGetValue(command,out var next))return;
    int index=(int)row["pulseIndex"];if(index<next)return;
    if(index!=next||index>3)throw new ArgumentException("Ordered native barrage impact required.");
    pulses[command]=index+1;PublishProgress();impact(row);
   }else if(kind=="CANCEL"){
    if(pulses.ContainsKey(command)){pulses[command]=4;PublishProgress();}
   }
  }
  void PublishProgress(){int left=0;foreach(var count in pulses.Values)left+=4-count;progress(left);}
  public void FinishFilm(){
   if(!playing)return;playing=false;var old=film;film=null;var owned=hold;hold=null;
   try{old?.Dispose();}finally{owned?.Dispose();}
  }
  public void Dispose(){if(disposed)return;disposed=true;driver.BarragePresentationEvent-=Receive;try{FinishFilm();}finally{pulses.Clear();progress(0);}}
 }
}
