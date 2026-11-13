using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Owns participation, waiting order and non-refundable slot budgets. Borrows native clock/HP/next.
 internal sealed class BEnemyAttackAdmissionOwner {
  sealed class Life {internal string Id,Target;internal int Ordinal;internal long SourceGeneration,TargetGeneration,FieldGeneration;internal bool Eligible=true;internal long? Waiting;internal Life Copy()=>(Life)MemberwiseClone();}
  sealed class Lease {internal string Source,TargetKey;internal long ReuseAt,HazardUntil;internal bool Pending;internal Lease Copy()=>(Lease)MemberwiseClone();}
  readonly BEnemyAttackAdmissionPolicy policy;readonly long gap;
  readonly List<Life> lives=new List<Life>();readonly List<Lease> leases=new List<Lease>();readonly Dictionary<string,long> targetNext=new Dictionary<string,long>(StringComparer.Ordinal);
#if !LASTRAIL_ORDINARY_CORE
  internal BEnemyAttackAdmissionOwner(BEnemyAttackAdmissionPolicy approved,BDirectAttackBasicLoopBinding binding,IEnumerable<string> sources){
   policy=approved??throw new ArgumentNullException(nameof(approved));gap=CombatActionTiming.BasicAttackPeriodTicks(approved.TargetGapMilliseconds*1000L,binding.Composition.Attack.StepTimeSpanTicks/10,Array.Empty<CombatTimingSource>(),Array.Empty<CombatTimingSource>(),Array.Empty<CombatTimingSource>());
   // Explicit LOCAL fixed roster: original entity array supplies ordinal; zero is this validation lifetime.
   var original=((JArray)binding.Composition.Attack.Capture()["entities"]).Select(e=>(string)e["id"]).ToArray();
   foreach(var source in sources)lives.Add(new Life{Id=source,Target=binding.Target(source),Ordinal=Array.IndexOf(original,source)});
  }
#endif
  internal BEnemyAttackAdmissionOwner(BEnemyAttackAdmissionPolicy approved,long stepMicroseconds,IEnumerable<BEnemyBasicTimingSource> sources){
   policy=approved??throw new ArgumentNullException(nameof(approved));gap=CombatActionTiming.BasicAttackPeriodTicks(approved.TargetGapMilliseconds*1000L,stepMicroseconds,Array.Empty<CombatTimingSource>(),Array.Empty<CombatTimingSource>(),Array.Empty<CombatTimingSource>());
   foreach(var source in sources)lives.Add(new Life{Id=source.Id,Target=source.Target,Ordinal=source.Ordinal});
  }
  BEnemyAttackAdmissionOwner(BEnemyAttackAdmissionOwner value){policy=value.policy;gap=value.gap;lives.AddRange(value.lives.Select(x=>x.Copy()));leases.AddRange(value.leases.Select(x=>x.Copy()));foreach(var pair in value.targetNext)targetNext.Add(pair.Key,pair.Value);}
  internal BEnemyAttackAdmissionOwner Clone()=>new BEnemyAttackAdmissionOwner(this);
  static long Add(long a,long b){BuffWire.Need(a>=0&&b>=0&&a<=9007199254740991-b,"b_admission_overflow");return a+b;}
  Life Find(string id){var value=lives.SingleOrDefault(x=>x.Id==id);BuffWire.Need(value!=null,"b_admission_source");return value;}
  static string Key(Life x)=>BasicCombatSimulation.StableJson(new JArray(x.Target,x.TargetGeneration,x.FieldGeneration));
  internal void Retarget(string id,string target,long targetGeneration,long now){var x=Find(id);BuffWire.Need(!string.IsNullOrEmpty(target)&&targetGeneration>=0,"b_admission_retarget");if(x.Target==target)return;ClearWaiting(id);Finish(id,now);x.Target=target;x.TargetGeneration=targetGeneration;x.Eligible=false;}
  internal bool Eligible(string id)=>Find(id).Eligible;
  internal bool SameLifetime(string id,long sourceGeneration,long targetGeneration,long fieldGeneration,bool eligible){var x=Find(id);return x.SourceGeneration==sourceGeneration&&x.TargetGeneration==targetGeneration&&x.FieldGeneration==fieldGeneration&&x.Eligible==eligible;}
  internal void Waiting(string id,long tick){var x=Find(id);if(!x.Waiting.HasValue)x.Waiting=tick;}
  internal void ClearWaiting(string id)=>Find(id).Waiting=null;
  internal string[] OrderedWaiting()=>lives.Where(x=>x.Eligible&&x.Waiting.HasValue).OrderBy(x=>x.Waiting.Value).ThenBy(x=>x.Ordinal).Select(x=>x.Id).ToArray();
  void Retire(long now)=>leases.RemoveAll(x=>!x.Pending&&now>=Math.Max(x.ReuseAt,x.HazardUntil));
  internal bool TryStart(string id,long now,long period){
   var x=Find(id);Retire(now);var key=Key(x);
   if(!x.Eligible||!x.Waiting.HasValue||leases.Any(l=>l.Source==id)||leases.Count>=policy.GlobalSlots||leases.Count(l=>l.TargetKey==key)>=policy.TargetSlots||targetNext.TryGetValue(key,out var due)&&now<due)return false;
   var reuse=Add(now,period);var next=Add(now,gap);leases.Add(new Lease{Source=id,TargetKey=key,ReuseAt=reuse,Pending=true});targetNext[key]=next;x.Waiting=null;return true;
  }
  internal void Finish(string id,long now){foreach(var lease in leases.Where(x=>x.Source==id))lease.Pending=false;Retire(now);}
  internal void Occupy(string id,long until,long now){ClearWaiting(id);foreach(var lease in leases.Where(x=>x.Source==id))lease.HazardUntil=Math.Max(lease.HazardUntil,until);Finish(id,now);}
  internal string[] Observe(string id,long sourceGeneration,long targetGeneration,long fieldGeneration,bool eligible,long hazardUntil,long now){
   var x=Find(id);BuffWire.Need(sourceGeneration>=x.SourceGeneration&&targetGeneration>=x.TargetGeneration&&fieldGeneration>=x.FieldGeneration&&sourceGeneration<=9007199254740991&&targetGeneration<=9007199254740991&&fieldGeneration<=9007199254740991&&hazardUntil>=now&&hazardUntil<=9007199254740991,"b_admission_lifetime");
   bool sourceChanged=sourceGeneration!=x.SourceGeneration,targetChanged=targetGeneration!=x.TargetGeneration,fieldChanged=fieldGeneration!=x.FieldGeneration;
   foreach(var lease in leases.Where(l=>l.Source==id))lease.HazardUntil=Math.Max(lease.HazardUntil,hazardUntil);
   var cancelled=new List<string>();
   foreach(var life in lives){bool cancel=fieldChanged||targetChanged&&life.Target==x.Target||life.Id==id&&(sourceChanged||!eligible);if(fieldChanged)life.FieldGeneration=fieldGeneration;if(targetChanged&&life.Target==x.Target)life.TargetGeneration=targetGeneration;if(cancel){life.Waiting=null;Finish(life.Id,now);cancelled.Add(life.Id);}}
   x.SourceGeneration=sourceGeneration;x.Eligible=eligible;return cancelled.ToArray();
  }
  internal void Terminal(){leases.Clear();foreach(var x in lives)x.Waiting=null;}
  internal JObject Capture()=>new JObject{["policy"]=policy.Capture(),["provenance"]="EXPLICIT_LOCAL_FIXED_ROSTER_NOT_SERVER_MANIFEST",["lives"]=new JArray(lives.Select(x=>new JObject{["sourceId"]=x.Id,["targetId"]=x.Target,["ordinal"]=x.Ordinal,["sourceGeneration"]=x.SourceGeneration,["targetGeneration"]=x.TargetGeneration,["battlefieldGeneration"]=x.FieldGeneration,["eligible"]=x.Eligible,["waitingSince"]=x.Waiting.HasValue?(JToken)x.Waiting.Value:JValue.CreateNull()})),["leases"]=new JArray(leases.Select(x=>new JObject{["sourceId"]=x.Source,["targetKey"]=x.TargetKey,["reuseAt"]=x.ReuseAt,["hazardUntil"]=x.HazardUntil,["actionPending"]=x.Pending})),["targetNext"]=JObject.FromObject(targetNext)};
 }
}
