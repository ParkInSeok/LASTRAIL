using System;
using Newtonsoft.Json.Linq;
namespace LastRail.Qa {
 // Reproduces pinned observed-v1 ONLY. Known onset swept-time disagreement remains unresolved.
 public sealed class MapGrayboxFixtureRun {
  public const string Scope="TEST_ONLY_OBSERVED_V1_BOUNDARY_UNRESOLVED";
  readonly int fallDelay,fallDuration,trainHp;
  readonly MapGrayboxFixtureGeometry geometry;
  sealed class State {public int Tick,Route=1,RockHp=100,Hits,Hp,Actual=10000,Target=10000,Recovery,AugmentOpened;public long Distance;public decimal Remainder;public int? WarnTick;public bool Locked,Paused,Pending,Joined;public string Rock="dormant",Phase="running";public State Copy()=>(State)MemberwiseClone();}
  State state;
  public sealed class TrustedCheckpoint {internal readonly MapGrayboxFixtureRun Owner;readonly State saved;internal TrustedCheckpoint(MapGrayboxFixtureRun owner){Owner=owner;saved=owner.state.Copy();}internal void Restore(MapGrayboxFixtureRun owner){if(!ReferenceEquals(Owner,owner))throw new ArgumentException("Only this in-process fixture checkpoint is trusted");owner.state=saved.Copy();}}
  public MapGrayboxFixtureRun(MapGrayboxFixtureGeometry geometry,int fallDelay=320,int fallDuration=5,int trainHp=1000){this.geometry=geometry??throw new ArgumentNullException(nameof(geometry));foreach(var value in new[]{fallDelay,fallDuration,trainHp})if(value<1||value>1000000)throw new ArgumentOutOfRangeException("Bounded test fixture only");this.fallDelay=fallDelay;this.fallDuration=fallDuration;this.trainHp=trainHp;state=new State{Hp=trainHp};}
  public TrustedCheckpoint Checkpoint()=>new TrustedCheckpoint(this);
  public void Restore(TrustedCheckpoint checkpoint){if(checkpoint==null)throw new ArgumentNullException();checkpoint.Restore(this);}
  public MapGrayboxFixtureGeometry.Point[] Path=>geometry.Assemble(new[]{"FORK_ROCKFALL","SAFE_STRAIGHT"},new[]{state.Route,0});
  public double ExitDistance=>MapGrayboxFixtureGeometry.Length(geometry.PathFor("FORK_ROCKFALL",state.Route))+100000;
  public int Tick=>state.Tick;public long Distance=>state.Distance;public int Route=>state.Route;public string Phase=>state.Phase;public string Rock=>state.Rock;public bool Paused=>state.Paused;
  public bool Reserve(int? route){if(state.Phase!="running"||state.Paused||state.WarnTick==null||state.Locked)return false;if(route==null)return true;if(route!=0&&route!=1)return false;state.Route=route.Value;return true;}
  public void Throttle(int value){if(value!=7000&&value!=10000&&value!=13000)throw new ArgumentException("Fixture throttle");if(state.Phase=="running"&&!state.Paused)state.Target=value;}
  public void Pause(bool value)=>state.Paused=value;
  public void CompleteWave(){if(state.Phase=="running")state.Pending=true;}
  public bool DamageRock(int damage){if(damage<0)throw new ArgumentOutOfRangeException();if(state.Phase!="running"||state.Paused||state.Rock!="rubble"||state.Route!=0)return false;state.RockHp=Math.Max(0,state.RockHp-damage);if(state.RockHp==0)state.Rock="destroyed";return true;}
  public bool ConfirmAugment(){if(state.Phase!="selecting"||state.Paused)return false;state.Phase="complete";return true;}
  void Hit(){if(state.Hits!=0)return;state.Hits++;state.Hp=Math.Max(0,state.Hp-state.RockHp);state.Rock="consumed";state.Recovery=30;if(state.Hp==0)state.Phase="defeated";}
  public void Step(){
   if(state.Paused||state.Phase!="running")return;
   state.Tick=checked(state.Tick+1);state.Actual+=Math.Sign(state.Target-state.Actual)*Math.Min(200,Math.Abs(state.Target-state.Actual));
   long previous=state.Distance;decimal delta=1000m*state.Actual*(state.Recovery>0?.5m:1m)/10000m+state.Remainder;long whole=checked((long)decimal.Floor(delta));state.Distance=checked(state.Distance+whole);state.Remainder=delta-whole;if(state.Recovery>0)state.Recovery--;
   if(state.WarnTick==null&&state.Distance>=40000){state.WarnTick=state.Tick;state.Rock="warned";}if(state.Distance>=80000)state.Locked=true;
   if(state.WarnTick!=null&&state.Rock!="consumed"&&state.Rock!="destroyed"&&state.Rock!="skipped"){
    int elapsed=state.Tick-state.WarnTick.Value;state.Rock=elapsed<fallDelay?"warned":elapsed<fallDelay+fallDuration?"falling":"rubble";
    if(state.Route==0&&state.Locked){if(state.Rock=="falling"&&state.Distance>=260000&&previous-100000<=340000)Hit();else if(state.Rock=="rubble"&&previous<260000&&state.Distance>=260000)Hit();}
   }
   state.Joined=state.Distance>=ExitDistance;if(state.Joined&&state.Route==1&&state.Rock!="consumed"&&state.Rock!="destroyed")state.Rock="skipped";
   if(state.Phase=="running"&&state.Pending&&state.Joined&&state.Recovery==0){state.Pending=false;state.Phase="selecting";state.AugmentOpened++;}
  }
  public JObject Capture()=>new JObject{["version"]="mechanics-lab-v1",["config"]=new JObject{["fallDelay"]=fallDelay,["fallDuration"]=fallDuration,["trainHp"]=trainHp},["state"]=new JObject{["tick"]=state.Tick,["distance"]=state.Distance,["remainder"]=state.Remainder,["route"]=state.Route,["locked"]=state.Locked,["warnTick"]=state.WarnTick.HasValue?new JValue(state.WarnTick.Value):JValue.CreateNull(),["rock"]=state.Rock,["rockHp"]=state.RockHp,["hits"]=state.Hits,["hp"]=state.Hp,["actual"]=state.Actual,["target"]=state.Target,["recovery"]=state.Recovery,["paused"]=state.Paused,["pending"]=state.Pending,["phase"]=state.Phase,["augmentOpened"]=state.AugmentOpened,["joined"]=state.Joined}};
  public JToken Command(string command,JToken value){
   int Integer(){if(value?.Type!=JTokenType.Integer)throw new FormatException();return checked((int)value);}
   switch(command){case "reserve":return new JValue(Reserve(value==null||value.Type==JTokenType.Null?(int?)null:Integer()));case "throttle":Throttle(Integer());break;case "pause":if(value?.Type!=JTokenType.Boolean)throw new FormatException();Pause((bool)value);break;case "completeWave":CompleteWave();break;case "damageRock":return new JValue(DamageRock(Integer()));case "confirmAugment":return new JValue(ConfirmAugment());case "roundtrip":Restore(Checkpoint());return new JValue("own-state-copy");default:throw new ArgumentException("Unknown fixture command");}return JValue.CreateNull();
  }
 }
}
