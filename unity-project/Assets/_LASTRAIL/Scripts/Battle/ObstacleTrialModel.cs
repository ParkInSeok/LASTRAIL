#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle {
 // Isolated approved numeric trial. No server admission or product encounter registration.
 public sealed class ObstacleTrialModel {
  public const int TrackLength=600000,InitialFront=58000,Length=38000,TargetX=338000;
  public const int ZoneStart=335000,ZoneEnd=341000,MaximumHp=100;
  public const int PreparationMs=150,LaunchIntervalMs=1650,RangeMm=100000,ShotSpeed=300000;
  public sealed class Shot {
   public int Id {get;internal set;} public long Launched {get;internal set;}
   public double X {get;internal set;} public double Y {get;internal set;}
   internal double OriginX,OriginY,Vx,Vy;
   public bool Flying {get;internal set;}=true;
  }
  readonly List<Shot> shots=new List<Shot>();
  readonly JArray events=new JArray();
  readonly CombatVitals train;
  readonly CombatDamageRouter damage;
  readonly string[] vehicles;
  readonly int defense,shotLimit;
  readonly bool landed;
  long remainder,nextLaunch=-1;
  int launchCount;
  bool contact,retired,tailRecorded;
  public long Milliseconds {get;private set;}
  public long Front {get;private set;}=InitialFront;
  public long Tail=>Front-Length;
  public long Speed {get;private set;}=20000;
  public long TargetSpeed {get;private set;}=20000;
  public int Hp {get;private set;}=MaximumHp;
  public int TrainHp=>train.Hp;
  public int Shield=>train.ShieldRemaining(Milliseconds);
  public IReadOnlyList<Shot> Shots=>shots.AsReadOnly();
  public double CenterY=>Height(Milliseconds);
  public string Phase=>Hp==0?"DESTROYED":contact?"BROKEN":landed||Milliseconds>=16320?"LANDED":Milliseconds>=15400?"FALLING":"WARNING";
  public bool Finished=>TrainHp==0||Tail>ZoneEnd&&Milliseconds>=16320&&!shots.Any(s=>s.Flying);
  public string CaseId {get;}
  public ObstacleTrialModel(string caseId,string[] vehicleIds,int armor,int trainHp=5000,int shield=0,int maximumShots=int.MaxValue,bool initiallyLanded=false) {
   if(string.IsNullOrWhiteSpace(caseId)||vehicleIds==null||vehicleIds.Length!=3||vehicleIds.Distinct().Count()!=3||vehicleIds.Any(string.IsNullOrWhiteSpace)||armor<0||maximumShots<0)throw new ArgumentException("Trial source");
   CaseId=caseId;vehicles=(string[])vehicleIds.Clone();defense=armor;shotLimit=maximumShots;landed=initiallyLanded;
   train=new CombatVitals(trainHp,10000,true);
   // Explicitly collision-eligible test layer, not a newly extended product support contract.
   if(shield>0)train.Shield(new CombatShieldSource(caseId,"trial-collision-eligible",0),shield,60000,0);
   damage=new CombatDamageRouter("train_a",train,new Dictionary<string,CombatVitals>(),8);
  }
  void Active(){if(retired)throw new ObjectDisposedException(nameof(ObstacleTrialModel));}
  public void Retire(){retired=true;}
  public void SetThrottle(int bps){
   Active();if(bps<7000||bps>13000)throw new ArgumentOutOfRangeException(nameof(bps));
   TargetSpeed=20000L*bps/10000;Event("THROTTLE",Milliseconds, new JObject{["target"]=TargetSpeed});
  }
  double Height(double ms)=>landed?3000:OrdinaryObstacleRules.FallingHeight(ms,12200,3000,15400,10000);
  static long[] Point(double x,double y)=>new[]{checked((long)Math.Round(x)),checked((long)Math.Round(y)),0L};
  void Event(string kind,double at,JObject detail=null){
   var row=detail??new JObject();row["kind"]=kind;row["atMs"]=at;row["obstacleHp"]=Hp;row["trainHp"]=TrainHp;row["front"]=Front;row["tail"]=Tail;row["speed"]=Speed;events.Add(row);
  }
  void Launch(){
   double x=Front-6000,y=4000;
   // Solve the authored target trajectory at impact; the frozen straight ray never homes.
   var aim=OrdinaryObstacleRules.Aim(TargetX-x,y,t=>Height(Milliseconds+t*1000),ShotSpeed,.4);
   var shot=new Shot{Id=++launchCount,Launched=Milliseconds,OriginX=x,OriginY=y,X=x,Y=y,Vx=aim.X,Vy=aim.Y};
   shots.Add(shot);nextLaunch=Milliseconds+LaunchIntervalMs;
   Event("LAUNCH",Milliseconds,new JObject{["shot"]=shot.Id,["muzzle"]=new JArray(x,y,0),["velocity"]=new JArray(shot.Vx,shot.Vy,0),["predictedFlightMs"]=aim.FlightSeconds*1000});
  }
  // One-ms authoritative trial grid. Rendering may batch steps but cannot skip contact sweeps.
  public void Advance(int milliseconds){
   Active();if(milliseconds<0||milliseconds>1000)throw new ArgumentOutOfRangeException(nameof(milliseconds));
   for(int i=0;i<milliseconds&&!Finished;i++)Step();
  }
  void Step(){
   if(Front+27>=TrackLength-20000)throw new InvalidOperationException("Trial track end margin exhausted");
   double dx=TargetX-(Front-6000),dy=Height(Milliseconds)-4000;
   bool inRange=dx>=0&&dx*dx+dy*dy<=RangeMm*(double)RangeMm;
   if(Hp>0&&!contact&&launchCount<shotLimit&&inRange){
    if(nextLaunch<0){nextLaunch=Milliseconds+PreparationMs;Event("PREPARE",Milliseconds);}
    if(Milliseconds>=nextLaunch)Launch();
   }
   long from=Milliseconds,to=from+1;
   var motion=OrdinaryRailKinematics.Advance(Front,Speed,TargetSpeed,remainder,1);
   long startFront=Front,startSpeed=Speed;
   var intersections=new List<(double Time,Shot Shot,int Vehicle)>();
   foreach(var shot in shots.Where(s=>s.Flying)){
    double dt=(to-shot.Launched)/1000.0;
    double nx=shot.OriginX+shot.Vx*dt,ny=shot.OriginY+shot.Vy*dt;
    if(Hp>0&&!contact){
     var hit=MiraSweepKernel.Box(Point(shot.X,shot.Y),Point(nx,ny),Point(TargetX,Height(from)),Point(TargetX,Height(to)),new long[]{6000,6000,6000},50);
     double? blocker=null;
     for(int car=0;car<3;car++){
      var body=MiraSweepKernel.Box(Point(shot.X,shot.Y),Point(nx,ny),Point(startFront-6000-car*13000,1600),Point(motion.Front-6000-car*13000,1600),new long[]{12000,3200,3000},50);
      if(body.HasValue&&(!blocker.HasValue||body.Value<blocker.Value))blocker=body;
     }
     if(blocker.HasValue&&(!hit.HasValue||blocker.Value<=hit.Value)){
      shot.Flying=false;Event("BLOCKED",from+blocker.Value,new JObject{["shot"]=shot.Id});
     }else if(hit.HasValue)intersections.Add((hit.Value,shot,-1));
    }
    shot.X=nx;shot.Y=ny;
    if(dt>=.4){shot.Flying=false;Event("EXPIRED",to,new JObject{["shot"]=shot.Id});}
   }
   if(Hp>0&&!contact&&(landed||to>=16000)){
    for(int car=0;car<3;car++){
     var hit=MiraSweepKernel.Box(Point(startFront-6000-car*13000,1600),Point(motion.Front-6000-car*13000,1600),Point(TargetX,Height(from)),Point(TargetX,Height(to)),new long[]{18000,9200,9000},0);
     if(hit.HasValue)intersections.Add((hit.Value,null,car));
    }
   }
   // Exact segment fractions establish order; hits win only an exact same-time tie.
   foreach(var item in intersections.OrderBy(x=>x.Time).ThenBy(x=>x.Shot==null?1:0).ThenBy(x=>x.Vehicle)){
    if(Hp==0||contact)break;
    double at=from+item.Time;
    if(item.Shot!=null){
     item.Shot.Flying=false;Hp=Math.Max(0,Hp-25);
     Event("HIT",at,new JObject{["shot"]=item.Shot.Id,["flightMs"]=at-item.Shot.Launched,["phaseAtHit"]=Height(at)>3000?(at<15400?"WARNING":"FALLING"):"LANDED",["targetCenter"]=new JArray(TargetX,Height(at),0)});
     if(Hp==0){nextLaunch=-1;Event("DESTROYED",at);}
    }else{
     double speedAt=startSpeed+(motion.Speed-startSpeed)*item.Time;
     bool falling=!landed&&Height(at)>3000;
     var basis=OrdinaryObstacleRules.RawDamage(falling,Hp,MaximumHp,(long)Math.Round(speedAt));
     var resolved=CombatNumericDefenseBudget.Resolve(new JObject{
      ["version"]=CombatNumericDefenseBudget.Version,["base"]=basis.Snapshot(),["effects"]=new JArray(),["defense"]=CombatExactValue.Create(defense,1).Snapshot(),
      ["weakening"]=new JArray(),["penetration"]=new JArray(),["reductions"]=new JObject{["equipment"]=new JArray(),["guardStance"]=new JArray(),["independent"]=new JArray()}
     }).ToVitalsMagnitude();
     var applied=damage.ApplyDirectTrainDamage(CaseId+":contact",resolved,to,false);
     var duplicate=damage.ApplyDirectTrainDamage(CaseId+":contact",resolved,to,false);
     if(!applied.Applied||duplicate.Applied)throw new InvalidOperationException("Contact receipt duplication");
     contact=true;nextLaunch=-1;
     long slowed=OrdinaryObstacleRules.CollisionSpeed(speedAt,Hp,MaximumHp);
     // Reintegrate the remaining fraction at the collision velocity, not the old-speed endpoint.
     double contactFront=startFront+(motion.Front-startFront)*item.Time;
     long endFront=(long)Math.Floor(contactFront+slowed*(1-item.Time)/1000);
     motion=(endFront,slowed,0);
     Event(falling?"FALLING_CONTACT":"HORIZONTAL_CONTACT",at,new JObject{["vehicle"]=vehicles[item.Vehicle],["vehicleOrder"]=item.Vehicle,["beforeDefense"]=basis.Snapshot(),["resolved"]=resolved,["absorbed"]=applied.Receipt.Absorbed,["hpLoss"]=applied.Receipt.TrainHpLoss,["slowedSpeed"]=slowed,["duplicateApplied"]=duplicate.Applied,["speedAtContact"]=speedAt});
    }
   }
   Front=motion.Front;Speed=motion.Speed;remainder=motion.Remainder;Milliseconds=to;
   if(!tailRecorded&&Tail>ZoneEnd){tailRecorded=true;Event("TAIL_CLEAR",to,new JObject{["beforeHazard"]=to<16000});}
   if(from<15400&&to>=15400&&!landed)Event("FALL_START",to);
   if(from<16320&&to>=16320&&!landed)Event("LAND",to);
  }
  public JObject Capture()=>new JObject{["case"]=CaseId,["timeMs"]=Milliseconds,["frontMm"]=Front,["tailMm"]=Tail,["speedMmPerSecond"]=Speed,["targetMmPerSecond"]=TargetSpeed,["obstacleHp"]=Hp,["trainHp"]=TrainHp,["shield"]=Shield,["phase"]=Phase,["finished"]=Finished,["events"]=events.DeepClone()};
 }
}
#endif
