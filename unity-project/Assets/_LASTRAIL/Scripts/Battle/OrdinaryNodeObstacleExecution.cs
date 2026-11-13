using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle {
 // Owns only obstacle/projectile state. The host owns time, motion and the original train HP.
 internal sealed class OrdinaryNodeObstacleExecution {
  readonly OrdinaryFiveCombatFactory factory;
  readonly BCombatLocalModel health;
  readonly Func<long,JObject> defense;
  readonly JObject geometry;
  readonly List<Shot> shots=new List<Shot>();
  readonly JArray events=new JArray();
  sealed class Shot {
   internal int Id;internal long Launched;internal double X,Y,OriginX,OriginY,Vx,Vy;internal bool Flying=true;
   internal Shot Copy()=>(Shot)MemberwiseClone();
  }
  int hp=100,launchCount;long nextLaunch=-1;bool contact;
  readonly long targetX,zoneEnd,initialY,landedY,fallStart,fallSpeed,muzzleOffset,muzzleY;
  readonly long[] size;
  internal OrdinaryNodeObstacleExecution(OrdinaryFiveCombatFactory factory,BCombatLocalModel health,JObject measured,Func<long,JObject> defense){
   this.factory=factory??throw new ArgumentNullException(nameof(factory));this.health=health??throw new ArgumentNullException(nameof(health));this.defense=defense??throw new ArgumentNullException(nameof(defense));
   if(factory.NodeInput?.Source.Obstacle==null)throw new ArgumentException("Issued obstacle required.");
   geometry=(JObject)(measured??throw new ArgumentNullException(nameof(measured))).DeepClone();
   var source=geometry["source"]?["obstacle"]??throw new ArgumentException("Measured obstacle required.");
   targetX=(long)source["targetXmm"];zoneEnd=(long)source["zoneEnd"];initialY=(long)source["initialCenterMm"][1];landedY=(long)source["landedCenterY"];
   fallStart=(long)source["fallStartMs"];fallSpeed=(long)source["fallSpeedMmPerSecond"];muzzleOffset=(long)source["muzzleRelativeToInitialFrontMm"][0];muzzleY=(long)source["muzzleRelativeToInitialFrontMm"][1];size=source["sizeMm"].Values<long>().ToArray();
   long initial=factory.Original.Rail.InitialFrontArcMm.Value;
   if((long)source["initialFront"]!=initial||targetX!=initial+280000||zoneEnd!=initial+283000||size.Length!=3||size.Any(n=>n<=0)||fallSpeed<=0||initialY<landedY||fallStart<0)
    throw new ArgumentException("Measured obstacle differs from issued local layout.");
  }
  internal OrdinaryNodeObstacleExecution CloneOwned(BCombatLocalModel owner,Func<long,JObject> newDefense=null){
   var clone=new OrdinaryNodeObstacleExecution(factory,owner,geometry,newDefense??defense){hp=hp,launchCount=launchCount,nextLaunch=nextLaunch,contact=contact};
   clone.shots.AddRange(shots.Select(s=>s.Copy()));foreach(var e in events)clone.events.Add(e.DeepClone());return clone;
  }
  double Height(double ms)=>OrdinaryObstacleRules.FallingHeight(ms,initialY,landedY,fallStart,fallSpeed);
  long LandAt=>checked(fallStart+(initialY-landedY)*1000/fallSpeed);
  static long[] Point(double x,double y)=>new[]{checked((long)Math.Round(x)),checked((long)Math.Round(y)),0L};
  void Event(string kind,double at,JObject detail=null){var row=detail??new JObject();row["kind"]=kind;row["atMs"]=at;row["obstacleHp"]=hp;row["trainHp"]=health.Hp(factory.TrainId);events.Add(row);}
  void Launch(long at,long front){
   double x=front+muzzleOffset;var aim=OrdinaryObstacleRules.Aim(targetX-x,muzzleY,t=>Height(at+t*1000),300000,.4);
   var shot=new Shot{Id=++launchCount,Launched=at,OriginX=x,OriginY=muzzleY,X=x,Y=muzzleY,Vx=aim.X,Vy=aim.Y};shots.Add(shot);nextLaunch=at+1650;
   Event("LAUNCH",at,new JObject{["shot"]=shot.Id,["muzzleMm"]=new JArray(x,muzzleY,0),["velocityMmPerSecond"]=new JArray(aim.X,aim.Y,0)});
  }
  // Sweeps every one-ms segment inside the existing fixed step; there is no second clock.
  internal (long Front,long Speed,long Remainder) Advance(long fromMs,long duration,long tick,long front,long speed,long target,long remainder){
   if(fromMs<0||duration<=0||duration>1000)throw new ArgumentOutOfRangeException(nameof(duration));
   for(long from=fromMs;from<fromMs+duration&&health.Hp(factory.TrainId)>0;from++){
    long to=from+1;double dx=targetX-(front+muzzleOffset),dy=Height(from)-muzzleY;
    if(hp>0&&!contact&&dx>=0&&dx*dx+dy*dy<=100000d*100000){
     if(nextLaunch<0){nextLaunch=from+150;Event("PREPARE",from);}if(from>=nextLaunch)Launch(from,front);
    }
    var motion=OrdinaryRailKinematics.Advance(front,speed,target,remainder,1);
    var hits=new List<(double Time,Shot Shot,int Car)>();
    foreach(var shot in shots.Where(s=>s.Flying)){
     double dt=(to-shot.Launched)/1000d,nx=shot.OriginX+shot.Vx*dt,ny=shot.OriginY+shot.Vy*dt;
     if(hp>0&&!contact){
      var hit=MiraSweepKernel.Box(Point(shot.X,shot.Y),Point(nx,ny),Point(targetX,Height(from)),Point(targetX,Height(to)),size,50);
      double? blocked=null;
      foreach(var body in Bodies(front,motion.Front)){
       var value=MiraSweepKernel.Box(Point(shot.X,shot.Y),Point(nx,ny),body.From,body.To,body.Size,50);
       if(value.HasValue&&(!blocked.HasValue||value.Value<blocked.Value))blocked=value;
      }
      if(blocked.HasValue&&(!hit.HasValue||blocked.Value<=hit.Value)){shot.Flying=false;Event("BLOCKED",from+blocked.Value,new JObject{["shot"]=shot.Id});}
      else if(hit.HasValue)hits.Add((hit.Value,shot,-1));
     }
     shot.X=nx;shot.Y=ny;
     // Expiry follows contacts in the final swept segment.
    }
    if(hp>0&&!contact&&to>=16000){
     int car=0;foreach(var body in Bodies(front,motion.Front)){
      var combined=body.Size.Zip(size,(a,b)=>checked(a+b)).ToArray();
      var hit=MiraSweepKernel.Box(body.From,body.To,Point(targetX,Height(from)),Point(targetX,Height(to)),combined,0);
      if(hit.HasValue)hits.Add((hit.Value,null,car));car++;
     }
    }
    foreach(var hit in hits.OrderBy(h=>h.Time).ThenBy(h=>h.Shot==null?1:0).ThenBy(h=>h.Car)){
     if(hp==0||contact)break;double at=from+hit.Time;
     if(hit.Shot!=null){hit.Shot.Flying=false;hp=Math.Max(0,hp-25);Event("HIT",at,new JObject{["shot"]=hit.Shot.Id});if(hp==0){nextLaunch=-1;Event("DESTROYED",at);}continue;}
     double atSpeed=speed+(motion.Speed-speed)*hit.Time;
     var raw=OrdinaryObstacleRules.RawDamage(Height(at)>landedY,hp,100,(long)Math.Round(atSpeed));
     var launch=CombatProjectileAttackSnapshot.Capture(new JObject{["version"]=CombatProjectileAttackSnapshot.Version,["completedAttack"]=raw.Snapshot(),["appliedOffense"]=new JArray(),["penetration"]=new JArray()});
     // This first local owner supports an explicitly shield-free train only; never consume unrelated layers.
     if(health.BorrowVitals(factory.TrainId).ShieldRemaining(tick)!=0)throw new InvalidOperationException("Collision shield eligibility source required.");
     var arrival=defense(tick);int damage=launch.Evaluate(arrival).ScaleAndFloor(10000);
     string id="ordinary-obstacle:"+factory.Attempt.Generation+":contact";
     var receipt=health.ApplyResolvedDamage(id,factory.TrainId,damage,tick);
     contact=true;nextLaunch=-1;long slowed=OrdinaryObstacleRules.CollisionSpeed(atSpeed,hp,100);
     motion=((long)Math.Floor(front+(motion.Front-front)*hit.Time+slowed*(1-hit.Time)/1000),slowed,0);
     Event(Height(at)>landedY?"FALLING_CONTACT":"HORIZONTAL_CONTACT",at,new JObject{["vehicleOrder"]=hit.Car,["raw"]=raw.Snapshot(),["defense"]=arrival.DeepClone(),["hpLoss"]=receipt.HpLoss,["absorbed"]=receipt.Absorbed,["hpAfter"]=receipt.HpAfter,["speedAtContact"]=atSpeed,["slowedSpeed"]=slowed});
    }
    foreach(var shot in shots.Where(s=>s.Flying&&to-s.Launched>=400)){shot.Flying=false;Event("EXPIRED",to,new JObject{["shot"]=shot.Id});}
    front=motion.Front;speed=motion.Speed;remainder=motion.Remainder;
   }
   return(front,speed,remainder);
  }
  IEnumerable<(long[] From,long[] To,long[] Size)> Bodies(long from,long to){
   foreach(string id in factory.Original.Rail.LocalFiveCar?new[]{"Engine","Car0","Car1","Car2","Car3","Car4"}:new[]{"Engine","Combat","Gun"}){
    var entity=geometry["entities"].Single(e=>(string)e["authoringId"]==id);var box=entity["shapes"].Single(s=>(string)s["kind"]=="BOX");
    var offset=geometry["source"]["liveLayout"][id]["relativeToInitialFrontMm"].Values<long>().ToArray();var center=box["centerMm"].Values<long>().ToArray();
    yield return(new[]{from+offset[0]+center[0],offset[1]+center[1],offset[2]+center[2]},new[]{to+offset[0]+center[0],offset[1]+center[1],offset[2]+center[2]},box["sizeMm"].Values<long>().ToArray());
   }
  }
  internal JObject Resolution(long atMs,long front){
   long tail=Bodies(front,front).Min(b=>b.From[0]-b.Size[0]/2);int alive=shots.Count(s=>s.Flying);bool clear=tail>zoneEnd;
   if(health.Hp(factory.TrainId)>0&&(!clear||atMs<LandAt||alive!=0))return null;
   var a=factory.Attempt;return new JObject{["accountId"]=a.AccountId,["runId"]=a.RunId,["nodeId"]=a.NodeId,["attemptGeneration"]=a.Generation,["snapshotHash"]=a.SnapshotHash,
    ["eventId"]="ordinary-obstacle:"+a.Generation+":resolved",["atMs"]=atMs,["outcome"]=health.Hp(factory.TrainId)==0?"DEFEAT":hp==0?"DESTROYED":contact?"COLLISION":"PASSED",["obstacleHp"]=hp,["trainHp"]=health.Hp(factory.TrainId),["tailBeyondZone"]=clear,["remainingHazards"]=atMs<LandAt?1:0,["unresolvedProjectiles"]=alive,["collisionCommitted"]=contact};
  }
  internal JObject CaptureView(long atMs)=>new JObject{["hp"]=hp,["maxHp"]=100,["centerMm"]=new JArray(targetX,Height(atMs),0),["sizeMm"]=new JArray(size),["phase"]=hp==0?"DESTROYED":contact?"BROKEN":atMs>=LandAt?"LANDED":atMs>=fallStart?"FALLING":"WARNING",["shots"]=new JArray(shots.Where(s=>s.Flying).Select(s=>new JObject{["id"]=s.Id,["x"]=s.X,["y"]=s.Y,["flying"]=s.Flying}))};
  internal JObject Capture()=>new JObject{["hp"]=hp,["nextLaunchMs"]=nextLaunch,["collisionCommitted"]=contact,["launchCount"]=launchCount,["events"]=events.DeepClone(),["shots"]=new JArray(shots.Select(s=>new JObject{["id"]=s.Id,["launchedMs"]=s.Launched,["x"]=s.X,["y"]=s.Y,["originX"]=s.OriginX,["originY"]=s.OriginY,["vx"]=s.Vx,["vy"]=s.Vy,["flying"]=s.Flying}))};
 }
}
