using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Owns only native roof locomotion. Health, attacks, clock and geometry observations are borrowed.
 internal sealed class OrdinaryLiveGunnerMotion {
  sealed class Body { internal string Id; internal long X,Y,Z,Radius,Min,Max; }
  readonly Body[] bodies;
  readonly HashSet<string> allies;
  readonly Dictionary<string,JObject> states=new Dictionary<string,JObject>(StringComparer.Ordinal);
  readonly long step,range,speed;
  long lastTick=-1;
  internal OrdinaryLiveGunnerMotion(OrdinaryFiveCombatFactory factory,string definitionJson){
   if(factory==null||factory.NodeInput==null)throw new ArgumentException("Original node required.");
   var definition=OrdinaryLiveCombatSources.ReadDefinition(definitionJson,factory.NodeInput.Source.GeometryBinding.Definition.DefinitionHash);
   var spatial=factory.NodeInput.Source.Enemies.CaptureActionDefinition()["spatial"];
   Need((long?)spatial["maximumDistanceMm"]==24000&&(long?)spatial["movementSpeedMmPerSecond"]==1500&&(bool?)spatial["connectedPathRequired"]==true&&(bool?)spatial["stopDuringPreparationAndRecovery"]==true&&(string)spatial["noTarget"]=="MOVE_TO_REACHABLE_ATTACK_POSITION"&&(string)spatial["noReachableTarget"]=="ORIGINAL_SAFE_STOP_OWNER","Original movement contract differs");
   step=factory.Step;range=(long)spatial["maximumDistanceMm"];speed=(long)spatial["movementSpeedMmPerSecond"];
   Need(step>0&&step<=10000000,"Unsupported original step");
   allies=new HashSet<string>(factory.Original.Actors.Select(a=>a.OperatorId),StringComparer.Ordinal);
   var entities=((JArray)definition["entities"]).OfType<JObject>().ToDictionary(e=>(string)e["authoringId"]);
   var layout=definition["source"]["liveLayout"];var roof=entities["Enemy"];var box=((JArray)roof["shapes"]).Single(s=>(string)s["kind"]=="BOX"&&(string)s["id"]=="BodyCollider");
   var origin=Point(layout["Enemy"]["originMm"]);var center=Point(box["centerMm"]);var size=Point(box["sizeMm"]);
   Need(size.All(n=>n>0&&n%2==0),"Integral measured roof bounds required");
   bodies=factory.NodeInput.Source.Enemies.Gunners.Select((g,i)=>{
    string authored="Gunner"+i;var e=entities[authored];var p=Point(layout[authored]["originMm"]);var foot=Point(((JArray)e["anchors"]).Single(a=>(string)a["role"]=="FOOT")["positionMm"]);
    for(int n=0;n<3;n++)p[n]+=foot[n];
    var capsule=((JArray)e["shapes"]).Single(s=>(string)s["kind"]=="CAPSULE_Y");long radius=(long)capsule["radiusMm"];var c=Point(capsule["centerMm"]);
    Need(c[0]==foot[0]&&c[2]==foot[2]&&c[1]-(long)capsule["heightMm"]/2==foot[1],"Foot and capsule support differ");
    // Authored FOOT plane sits 13mm above the measured body top in v02. Preserve that authored support offset.
    Need(((JArray)roof["anchors"]).Any(a=>(string)a["role"]=="FOOT"&&Point(a["positionMm"])[1]+origin[1]==p[1]),"Measured roof support plane missing");
    Need(p[1]>=origin[1]+center[1]+size[1]/2&&p[1]-(origin[1]+center[1]+size[1]/2)<=radius,"Foot is not supported by measured roof");
    Need(Math.Abs(p[2]-origin[2]-center[2])+radius<=size[2]/2,"Capsule outside roof width");
    var body=new Body{Id=g.InstanceId,X=p[0],Y=p[1],Z=p[2],Radius=radius,Min=origin[0]+center[0]-size[0]/2+radius,Max=origin[0]+center[0]+size[0]/2-radius};
    Need(body.X>=body.Min&&body.X<=body.Max,"Capsule outside roof length");
    states.Add(body.Id,State(body.Id));return body;
   }).ToArray();
   Need(bodies.Length==2&&bodies[0].Y==bodies[1].Y&&bodies[0].Z==bodies[1].Z&&Math.Abs(bodies[0].X-bodies[1].X)>=bodies[0].Radius+bodies[1].Radius,"Single connected nonoverlapping roof lane required");
  }
  OrdinaryLiveGunnerMotion(OrdinaryLiveGunnerMotion other){bodies=other.bodies;allies=other.allies;step=other.step;range=other.range;speed=other.speed;lastTick=other.lastTick;foreach(var pair in other.states)states.Add(pair.Key,(JObject)pair.Value.DeepClone());}
  static JObject State(string id)=>new JObject{["id"]=id,["offsetXmm"]=0,["distanceRemainder"]=0,["motion"]="STATIONARY",["noTargetStatus"]="UNRESOLVED",["reason"]="NOT_STEPPED",["goalXmm"]=null};
  static void Need(bool condition,string message){if(!condition)throw new InvalidOperationException("Ordinary gunner motion: "+message);}
  static long[] Point(JToken point){Need(point is JArray a&&a.Count==3&&a.All(v=>v.Type==JTokenType.Integer),"Native integer point required");return point.Select(v=>(long)v).ToArray();}
  JObject StateAt(string id,long tick){Need(tick>=0&&tick>=lastTick,"Stale motion tick");Need(states.ContainsKey(id),"Unknown gunner");return states[id];}
  internal long[] Offset(string id,long tick){if(allies.Contains(id)){Need(tick>=0&&tick>=lastTick,"Stale motion tick");return new long[]{0,0,0};}return new[]{(long)StateAt(id,tick)["offsetXmm"],0L,0L};}
  internal string MotionStatus(string id,long tick)=>(string)StateAt(id,tick)["motion"];
  internal string NoTargetStatus(string id,long tick)=>(string)StateAt(id,tick)["noTargetStatus"];
  internal OrdinaryLiveGunnerMotion CloneOwned()=>new OrdinaryLiveGunnerMotion(this);
  internal JObject Capture()=>new JObject{["version"]="ordinary.live-gunner-roof-motion.v1.20261106",["tick"]=lastTick,["actors"]=new JArray(bodies.Select(b=>states[b.Id].DeepClone()))};
  static BigInteger Square(long[] a,long[] b){BigInteger value=0;for(int i=0;i<3;i++){BigInteger d=(BigInteger)a[i]-b[i];value+=d*d;}return value;}
  static long Root(BigInteger value){long root=(long)Math.Sqrt((double)value);while((BigInteger)root*root>value)root--;while((BigInteger)(root+1)*(root+1)<=value)root++;return root;}
  internal void Step(long tick,Func<string,JObject> readGunnerState,Func<string,long,JObject> observe,Func<string,bool> alive,Func<long,JArray> blockers){
   Need(tick>=0&&(lastTick<0||tick==lastTick||tick==lastTick+1),"Consecutive owned combat ticks required");if(tick==lastTick)return;
   Need(readGunnerState!=null&&observe!=null&&alive!=null&&blockers!=null,"Actual owner ports required");
   bool first=lastTick<0;lastTick=tick;var obstacles=blockers(tick);Need(obstacles!=null,"Actual current blockers required");
   foreach(var body in bodies){
    var state=states[body.Id];state["motion"]="STATIONARY";state["noTargetStatus"]="UNRESOLVED";state["goalXmm"]=null;
    void Stop(string reason){state["reason"]=reason;state["distanceRemainder"]=0;state["noTargetStatus"]="ORIGINAL_SAFE_STOP_OWNER";}
    if(!alive(body.Id)){Stop("DOWN");continue;}
    var actor=readGunnerState(body.Id);Need(actor!=null,"Current attack state missing");string phase=(string)actor["phase"];
    if(phase=="WINDUP"||phase=="RECOVERY"&&tick<(long)actor["recoveryEnd"]){Stop("ATTACK_PHASE_HOLD");continue;}
    if(phase=="RECOVERY")phase="IDLE";
    Need(phase=="IDLE","Unsupported gunner attack phase");
    var observation=observe(body.Id,tick);Need((long?)observation?["tick"]==tick&&(string)observation["source"]?["id"]==body.Id,"Current source observation required");
    var muzzle=Point(observation["source"]["muzzleMm"]);var targets=((JArray)observation["targets"]).OfType<JObject>().Where(t=>alive((string)t["id"])).OrderBy(t=>(string)t["id"],StringComparer.Ordinal).ToArray();
    long current=body.X+(long)state["offsetXmm"];
    bool CanAttack(long x,JObject target){var p=(long[])muzzle.Clone();p[0]+=x-current;var q=Point(target["pointMm"]);return Square(p,q)<=(BigInteger)range*range&&!obstacles.Any(o=>MiraSweepKernel.Box(p,q,Point(o["centerMm"]),Point(o["centerMm"]),Point(o["sizeMm"]),0).HasValue);}
    if(targets.Any(t=>CanAttack(current,t))){Stop("TARGET_AVAILABLE");continue;}
    long min=body.Min,max=body.Max;
    // Occupants cut the same-height straight lane into connected components. Never cross an occupied capsule.
    foreach(var other in bodies.Where(b=>b!=body&&alive(b.Id))){long x=other.X+(long)states[other.Id]["offsetXmm"],gap=body.Radius+other.Radius;Need(Math.Abs(current-x)>=gap,"Occupied capsule overlap");if(x<current)min=Math.Max(min,x+gap);else max=Math.Min(max,x-gap);}
    long? goal=null;
    foreach(var target in targets){var q=Point(target["pointMm"]);BigInteger y=(BigInteger)muzzle[1]-q[1],z=(BigInteger)muzzle[2]-q[2],remaining=(BigInteger)range*range-y*y-z*z;if(remaining<0)continue;
     long dx=Root(remaining),muzzleOffset=muzzle[0]-current,lo=Math.Max(min,q[0]-muzzleOffset-dx),hi=Math.Min(max,q[0]-muzzleOffset+dx);if(lo>hi)continue;
     long nearest=Math.Max(lo,Math.Min(hi,current));
     // Exhaustive integer-mm search only inside the range interval; exact LOS is reused, never assumed.
     for(long distance=0;distance<=Math.Max(nearest-lo,hi-nearest);distance++){
      long left=nearest-distance,right=nearest+distance;long? candidate=null;
      if(left>=lo&&CanAttack(left,target))candidate=left;else if(right<=hi&&CanAttack(right,target))candidate=right;
      if(candidate.HasValue){if(!goal.HasValue||Math.Abs(candidate.Value-current)<Math.Abs(goal.Value-current)||Math.Abs(candidate.Value-current)==Math.Abs(goal.Value-current)&&candidate.Value<goal.Value)goal=candidate;break;}
     }
    }
    if(!goal.HasValue){Stop("NO_REACHABLE_ATTACK_POINT");continue;}
    state["goalXmm"]=goal.Value;state["reason"]="CONNECTED_ROOF_PATH";state["motion"]="MOVING";state["noTargetStatus"]="MOVING_ON_CONFIRMED_PATH";
    if(first)continue;long numerator=checked(speed*step+(long)state["distanceRemainder"]),amount=numerator/10000000;state["distanceRemainder"]=numerator%10000000;
    long delta=goal.Value-current,travel=Math.Min(Math.Abs(delta),amount);state["offsetXmm"]=(long)state["offsetXmm"]+Math.Sign(delta)*travel;
    if(travel==Math.Abs(delta)){Stop("TARGET_AVAILABLE");state["motion"]="STATIONARY";}
   }
  }
 }
}
