using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Owns launched immutable payloads and terminal state. HP/clock stay in Session.
 internal sealed class BGunnerProjectileOwner {
  internal sealed class Shot {
   internal string ReservedBy;internal string Id,Source,Target,Terminal="FLYING";internal long Launch,Contact,Sequence,SourceGeneration,TargetGeneration;internal JObject Attack,LaunchGeometry;
   internal Shot Copy(){var s=(Shot)MemberwiseClone();s.Attack=(JObject)Attack.DeepClone();s.LaunchGeometry=(JObject)LaunchGeometry?.DeepClone();return s;}
   internal JObject Capture(){var p=new JObject{["id"]=Id,["sourceId"]=Source,["targetId"]=Target,["launchTick"]=Launch,["contactTick"]=Contact,["sequence"]=Sequence,["sourceGeneration"]=SourceGeneration,["targetGeneration"]=TargetGeneration,["attack"]=Attack.DeepClone(),["terminal"]=Terminal,["reservedBy"]=ReservedBy==null?JValue.CreateNull():new JValue(ReservedBy)};if(LaunchGeometry!=null){p["sourcePointMm"]=LaunchGeometry["sourcePointMm"].DeepClone();p["targetPointMm"]=LaunchGeometry["targetPointMm"].DeepClone();p["geometryHash"]=LaunchGeometry["geometryHash"].DeepClone();p["geometryRevision"]=LaunchGeometry["revision"].DeepClone();}return p;}
  }
#if !LASTRAIL_ORDINARY_CORE
  readonly BDirectAttackBasicLoopBinding binding;
#endif
  readonly int capacity;readonly long stepMicroseconds;readonly List<Shot> shots=new List<Shot>();readonly JArray history=new JArray();bool terminal;
#if !LASTRAIL_ORDINARY_CORE
  internal BGunnerProjectileOwner(BDirectAttackBasicLoopBinding b,int cap):this(b.Composition.Attack.StepTimeSpanTicks/10,cap){binding=b;}
#endif
  internal BGunnerProjectileOwner(long stepMicroseconds,int cap){BuffWire.Need(stepMicroseconds>0&&cap>0,"b_gunner_projectile_boundary");this.stepMicroseconds=stepMicroseconds;capacity=cap;}
  BGunnerProjectileOwner(BGunnerProjectileOwner p){
#if !LASTRAIL_ORDINARY_CORE
   binding=p.binding;
#endif
   capacity=p.capacity;stepMicroseconds=p.stepMicroseconds;terminal=p.terminal;shots.AddRange(p.shots.Select(s=>s.Copy()));foreach(var e in p.history)history.Add(e.DeepClone());}
  internal BGunnerProjectileOwner Clone()=>new BGunnerProjectileOwner(this);
  internal IEnumerable<Shot> All=>shots;
  internal bool Flying=>shots.Any(s=>s.Terminal=="FLYING");
  internal IEnumerable<Shot> Pending=>shots.Where(s=>s.Terminal=="FLYING").OrderBy(s=>s.Contact).ThenBy(s=>s.Sequence);
#if !LASTRAIL_ORDINARY_CORE
  sealed class NativeContext : IBGunnerProjectileContext {
   readonly BDirectAttackBasicLoopBinding binding;readonly BDirectAttackBuffSession session;
   internal NativeContext(BDirectAttackBasicLoopBinding binding,BDirectAttackBuffSession session){this.binding=binding;this.session=session;}
   public long Tick=>session.Tick;
   public int Hp(string id)=>session.Hp(id);
   public long ProjectileTargetGeneration(string id)=>session.ProjectileTargetGeneration(id);
   public bool HasSpatial=>session.HasSpatial;
   public string GunnerTarget(string source)=>session.HasSpatial?session.SpatialTarget(source):binding.Target(source);
   public bool EnemyBasicTargetEligible(string source,string target)=>session.EnemyBasicTargetEligible(source,target);
   public bool TryGeometry(string source,string target,out JObject geometry)=>session.TryGeometry(source,target,out geometry);
   public JObject CaptureGunnerAttack(string source){var actor=binding.Composition.Attack.Entities[source];return CombatStatResolver.Attack(source,actor.Attack,(JArray)actor.Original["attackSources"]).Snapshot();}
  }
  internal bool Launch(string source,long tick,BEnemyBoardingOwner board,BDirectAttackBuffSession session,Func<long> next)=>Launch(source,tick,board.Generation(source),new NativeContext(binding,session),next);
#endif
  internal bool Launch(string source,long tick,long sourceGeneration,IBGunnerProjectileContext session,Func<long> next){
   BuffWire.Need(!terminal,"b_gunner_terminal");string target=session.GunnerTarget(source);if(target==null)return false;
   if(!session.EnemyBasicTargetEligible(source,target)||!session.TryGeometry(source,target,out var geometry))return false;
   BuffWire.Need(shots.Count<capacity,"b_gunner_projectile_capacity");
   long distance=(long)geometry["distanceMm"],step=stepMicroseconds;
   long flight=checked((long)System.Numerics.BigInteger.Max(1,((System.Numerics.BigInteger)distance*1000000+24000*step-1)/(24000*step)));
   long seq=next();
   shots.Add(new Shot{Id="local-gunner:"+seq,Source=source,Target=target,Launch=tick,Contact=checked(tick+flight),Sequence=seq,SourceGeneration=sourceGeneration,TargetGeneration=session.ProjectileTargetGeneration(target),LaunchGeometry=session.HasSpatial?(JObject)geometry.DeepClone():null,Attack=(JObject)session.CaptureGunnerAttack(source).DeepClone()});
   history.Add(new JObject{["kind"]="LAUNCH",["sourceId"]=source,["tick"]=tick,["sequence"]=seq});return true;
  }
#if !LASTRAIL_ORDINARY_CORE
  internal void Resolve(BDirectAttackBuffSession session,Func<long> next,List<BDirectAttackBuffReceipt> hits)=>Resolve((IBBoardingActors)session,next,(id,source,target,attack)=>hits.Add(session.ResolveGunnerImpact(id,source,target,attack)));
#endif
  internal void Resolve(IBBoardingActors session,Func<long> next,Action<string,string,string,JObject> impact){foreach(var s in Pending.ToArray()){if(session.Tick<s.Contact)continue;if(session.Hp(s.Target)==0||s.TargetGeneration!=session.ProjectileTargetGeneration(s.Target)){s.Terminal="MISS";}else{impact(s.Id,s.Source,s.Target,s.Attack);s.Terminal="HIT";}history.Add(new JObject{["kind"]=s.Terminal,["id"]=s.Id,["tick"]=session.Tick,["sequence"]=next()});}}
#if !LASTRAIL_ORDINARY_CORE
  internal void Terminal(BDirectAttackBuffSession session,Func<long> next)=>Terminal((IBBoardingActors)session,next);
#endif
  internal void Terminal(IBBoardingActors session,Func<long> next){if(terminal)return;terminal=true;foreach(var shot in Pending.ToArray()){shot.Terminal="MISS";shot.ReservedBy=null;history.Add(new JObject{["kind"]="TERMINAL_MISS",["id"]=shot.Id,["tick"]=session.Tick,["sequence"]=next()});}}
  internal JObject Capture()=>new JObject{["terminal"]=terminal,["projectiles"]=new JArray(shots.Select(s=>s.Capture())),["events"]=history.DeepClone()};
 }
}



