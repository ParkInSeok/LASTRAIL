using System;
using System.Linq;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Owns opportunities and clear gates, never actor HP or the simulation clock.
 internal sealed class BWaveEntryOwner {
  readonly BScoutGunnerNodeSnapshot source;readonly BWaveBoardingPolicy policy;readonly long interval,gap;
  int wave,cursor;long nextOpportunity,gapUntil;bool started,finished,unknownProjectile;readonly JArray history=new JArray();
  internal BWaveEntryOwner(BScoutGunnerNodeSnapshot original,BWaveBoardingPolicy approved,long step){source=original;policy=approved;interval=(long)BScoutGunnerTiming.EntryAtClock((string)source.Capture()["origin"]["selectedRisk"],source.VisitDepth,step,1).IntervalTicks;gap=policy.DurationTicks(policy.WaveGapMilliseconds,step);}
  BWaveEntryOwner(BWaveEntryOwner x){source=x.source;policy=x.policy;interval=x.interval;gap=x.gap;wave=x.wave;cursor=x.cursor;nextOpportunity=x.nextOpportunity;gapUntil=x.gapUntil;started=x.started;finished=x.finished;unknownProjectile=x.unknownProjectile;foreach(var e in x.history)history.Add(e.DeepClone());}
  internal BWaveEntryOwner Clone()=>new BWaveEntryOwner(this);
  internal bool Victory=>finished;
  internal int CompletedWaves=>history.Count(e=>(string)e["kind"]=="WAVE_CLEAR");
  internal void UnboundProjectile(string id,long tick,Func<long> sequence){unknownProjectile=true;history.Add(new JObject{["kind"]="PROJECTILE_EXECUTOR_MISSING",["id"]=id,["tick"]=tick,["sequence"]=sequence()});}
  void Register(int maximum,BEnemyBoardingOwner boarding,IBBoardingActors native,Func<long> sequence){var actors=source.Waves[wave].Instances;for(int n=0;n<maximum&&cursor<actors.Count&&boarding.Alive<policy.AliveCap;n++){if(!boarding.Enter(actors[cursor].InstanceId,native,sequence))break;cursor++;}}
  internal void BeforeStep(BEnemyBoardingOwner boarding,IBBoardingActors native,Func<long> sequence){
   if(finished)return;
   if(gapUntil!=0){if(native.Tick<gapUntil)return;wave++;cursor=0;started=false;gapUntil=0;}
   if(!started){Register(policy.FirstGroup,boarding,native,sequence);if(cursor==0)return;started=true;nextOpportunity=checked(native.Tick+interval);return;}
   if(native.Tick<nextOpportunity)return;
   // A saturated opportunity is consumed. Death does not spawn a catch-up burst.
   Register(policy.NextGroupMaximum,boarding,native,sequence);nextOpportunity=checked(nextOpportunity+interval);
  }
  internal void AfterStep(BEnemyBoardingOwner boarding,IBBoardingActors native,Func<long> sequence,bool flying=false){if(finished||gapUntil!=0||!started||cursor!=source.Waves[wave].Count||!boarding.AllDead(source.Waves[wave].Instances.Select(x=>x.InstanceId))||unknownProjectile||flying)return;history.Add(new JObject{["kind"]="WAVE_CLEAR",["waveOrdinal"]=wave+1,["tick"]=native.Tick,["sequence"]=sequence()});if(wave==1)finished=true;else gapUntil=checked(native.Tick+gap);}
  internal JObject Capture()=>new JObject{["waveOrdinal"]=wave+1,["cursor"]=cursor,["started"]=started,["nextOpportunity"]=nextOpportunity,["gapUntil"]=gapUntil,["finished"]=finished,["unknownProjectile"]=unknownProjectile,["intervalTicks"]=interval,["events"]=history.DeepClone()};
 }
}

