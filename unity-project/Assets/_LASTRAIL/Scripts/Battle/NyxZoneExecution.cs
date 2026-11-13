using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 public sealed class NyxZoneMember {
  public string Id {get;} public long Generation {get;}
  public bool DelayImmune {get;} public bool Protected {get;} public bool MitigationRateImmune {get;}
  public NyxZoneMember(string id,long generation,bool delayImmune=false,bool protectedFromDamage=false,bool mitigationRateImmune=false){
   BCombatFormation.Identity(id);if(generation<=0)throw new ArgumentOutOfRangeException(nameof(generation));
   Id=id;Generation=generation;DelayImmune=delayImmune;Protected=protectedFromDamage;MitigationRateImmune=mitigationRateImmune;
  }
 }
 public sealed class NyxZoneExecution:IDisposable {
  // Local effect owner only: successful creation, eligible zone membership and numeric authority
  // are required inputs. This does not admit a START or select a geometric zone.
  public const string Version="unity.nyx-zone-core.v1.20261107";
  public const string TimingSourceVersion="combat.nyx-approved-requirements.v2.20261107";
  const string Caster="op_nyx";
  readonly string binding;readonly long stepMs;
  BCombatLocalModel health;Func<string,long,JObject> attack,defense;
  readonly HashSet<string> created=new HashSet<string>(StringComparer.Ordinal);
  readonly Dictionary<string,int> streak=new Dictionary<string,int>(StringComparer.Ordinal);
  NyxZoneMember[] current=Array.Empty<NyxZoneMember>();
  JObject zoneAttack;
  string zone;bool ultimate,disposed,faulted;long lastTick=-1,nextTick,createdAt;int pulse;
  public NyxZoneExecution(string bindingId,BCombatLocalModel health,long stepMs,Func<string,long,JObject> attack,Func<string,long,JObject> defense){
   BCombatFormation.Identity(bindingId);if(stepMs<=0||stepMs>1000)throw new ArgumentException("Supported logical step required.");
   binding=bindingId;this.stepMs=stepMs;this.health=health??throw new ArgumentNullException(nameof(health));
   this.attack=attack??throw new ArgumentNullException(nameof(attack));this.defense=defense??throw new ArgumentNullException(nameof(defense));_=health.Hp(Caster);
  }
  public bool Active=>!disposed&&!faulted&&zone!=null;
  internal string BindingId=>binding;
  internal int PulseCount=>pulse;
  internal bool CasterAlive=>!disposed&&health.Hp(Caster)>0&&!health.TrainDefeated;
  // Ceil each absolute deadline once; a 17ms step must not accumulate rounded one-second intervals.
  long Due(int index)=>checked(createdAt+(index*1000L+stepMs-1)/stepMs);
  public JArray CaptureCurrentEffects(long tick){
   RequireUsable();var rows=new JArray();if(!Active)return rows;
   if(tick!=lastTick)throw new InvalidOperationException("Current zone observation must precede numeric consumers.");
   long expiry=Due(ultimate?8:6);
   foreach(var m in current){
    void Add(string kind,int value){if(value>0)rows.Add(new JObject{["sourceId"]="nyx-zone:"+BDirectAttackInput.Digest(new JArray(binding,zone,kind)).Substring(7),["sourceKind"]="NYX_ZONE",["ownerId"]=Caster,["targetId"]=m.Id,["targetGeneration"]=m.Generation,["kind"]=kind,["valueBps"]=value,["expiresAtTick"]=expiry});}
    Add("MITIGATION_RATE_REDUCTION",MitigationRateReductionSources(m.Id,m.Generation).Count==0?0:ultimate?1500:800);
    Add("NEW_BASIC_PERIOD_INCREASE",BasicPeriodIncreaseBps(m.Id,m.Generation));
   }
   return rows;
  }
  public JObject Capture(){
   RequireUsable();
   return new JObject{["version"]=Version,["sourceVersion"]=TimingSourceVersion,["binding"]=binding,["stepMs"]=stepMs,["createdAt"]=createdAt,
    ["zone"]=zone,["ultimate"]=ultimate,["attackSnapshot"]=zoneAttack?.DeepClone(),
    ["lastTick"]=lastTick,["nextTick"]=nextTick,["pulse"]=pulse,
    ["created"]=new JArray(created.OrderBy(x=>x,StringComparer.Ordinal)),
    ["streaks"]=new JArray(streak.OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>new JObject{["key"]=x.Key,["count"]=x.Value})),
    ["members"]=new JArray(current.Select(m=>new JObject{["id"]=m.Id,["generation"]=m.Generation,["delayImmune"]=m.DelayImmune,["protected"]=m.Protected,["mitigationRateImmune"]=m.MitigationRateImmune}))};
  }
  // In-memory command staging only; leaving the node creates a fresh owner, never restores this capture.
  public NyxZoneExecution CloneOwned(BCombatLocalModel clonedHealth,Func<string,long,JObject> clonedAttack,Func<string,long,JObject> clonedDefense){
   RequireUsable();if(clonedHealth==null||ReferenceEquals(health,clonedHealth)||!JToken.DeepEquals(JObject.Parse(health.CaptureLocal()),JObject.Parse(clonedHealth.CaptureLocal())))throw new ArgumentException("Independent matching health owner required.");
   var clone=new NyxZoneExecution(binding,clonedHealth,stepMs,clonedAttack,clonedDefense){zone=zone,ultimate=ultimate,lastTick=lastTick,nextTick=nextTick,createdAt=createdAt,pulse=pulse,current=current.ToArray(),zoneAttack=(JObject)zoneAttack?.DeepClone()};
   foreach(string id in created)clone.created.Add(id);foreach(var item in streak)clone.streak.Add(item.Key,item.Value);return clone;
  }
  void RequireUsable(){if(disposed)throw new ObjectDisposedException(nameof(NyxZoneExecution));if(faulted)throw new InvalidOperationException("Failed zone owner requires authoritative recovery.");}
  void Clear(){zone=null;zoneAttack=null;current=Array.Empty<NyxZoneMember>();streak.Clear();}
  // Called only after the authority has successfully created the replacement zone.
  public void CommitCreatedZone(string zoneId,long tick,bool ultimate){
   RequireUsable();BCombatFormation.Identity(zoneId);
   if(tick<0||tick<lastTick||created.Contains(zoneId)||created.Count>=10000||health.Hp(Caster)==0||health.TrainDefeated)throw new InvalidOperationException("Invalid or duplicate successful zone creation.");
   long first=checked(tick+(1000+stepMs-1)/stepMs);Clear();created.Add(zoneId);zone=zoneId;this.ultimate=ultimate;pulse=0;createdAt=tick;nextTick=first;lastTick=tick-1;
  }
  static string Key(NyxZoneMember m)=>m.Id+":"+m.Generation;
  public IReadOnlyList<BCombatSingleTargetReceipt> Step(long tick,IReadOnlyList<NyxZoneMember> members,string focus=null,bool nodeRunning=true,bool zonePresent=true){
   RequireUsable();if(tick<0||tick<lastTick)throw new ArgumentOutOfRangeException(nameof(tick));
   if(!nodeRunning||!zonePresent||health.Hp(Caster)==0||health.TrainDefeated){Clear();lastTick=tick;return Array.Empty<BCombatSingleTargetReceipt>();}
   if(!Active)return Array.Empty<BCombatSingleTargetReceipt>();
   if(tick>nextTick)throw new InvalidOperationException("A due zone tick needs its original observation; cannot backfill from a later frame.");
   if(members==null||members.Count>10000||members.Any(m=>m==null||m.Id==Caster)||members.Select(m=>m.Id).Distinct(StringComparer.Ordinal).Count()!=members.Count)throw new ArgumentException("Distinct authoritative living-zone members required.");
   var living=members.Where(m=>health.Hp(m.Id)>0).ToArray();
   var keys=new HashSet<string>(living.Select(Key),StringComparer.Ordinal);
   foreach(string key in streak.Keys.Where(k=>!keys.Contains(k)).ToArray())streak.Remove(key);
   current=living;
   if(tick==lastTick)return Array.Empty<BCombatSingleTargetReceipt>();
   if(tick<nextTick){lastTick=tick;return Array.Empty<BCombatSingleTargetReceipt>();}
   lastTick=tick;
   var selected=living.OrderBy(m=>m.Id==focus?0:1).ThenBy(m=>m.Id,StringComparer.Ordinal).Take(ultimate?3:2).ToArray();
   var results=new List<BCombatSingleTargetReceipt>();
   try{
    // Resolve every numeric input before applying any target's HP command.
    var magnitudes=new int[selected.Length];
    if(selected.Length>0){
     if(zoneAttack==null){var supplied=attack(Caster,tick)??throw new InvalidOperationException("Original attack budget missing.");_=CombatProjectileAttackSnapshot.Capture(supplied);zoneAttack=(JObject)supplied.DeepClone();}
     for(int i=0;i<selected.Length;i++){
      var m=selected[i];var launch=(JObject)zoneAttack.DeepClone();int bonus=streak.TryGetValue(Key(m),out var count)&&count>=2?11000:10000;
      launch["completedAttack"]=CombatExactValue.From((JObject)launch["completedAttack"]).Multiply(CombatExactValue.Create(ultimate?12000:6000,10000*selected.Length)).Multiply(CombatExactValue.Create(bonus,10000)).Snapshot();
      var arrival=defense(m.Id,tick)??throw new InvalidOperationException("Original defense missing.");
      var captured=CombatProjectileAttackSnapshot.Capture(launch);
      magnitudes[i]=((string)arrival["version"]==CombatProjectileAttackSnapshot.MitigationArrivalVersion?captured.Evaluate(arrival):captured.EvaluateWithMitigationRateReduction(arrival,MitigationRateReductionSources(m.Id,m.Generation))).ScaleAndFloor(10000);
     }
    }
    var selectedKeys=new HashSet<string>(selected.Select(Key),StringComparer.Ordinal);
    foreach(string key in streak.Keys.Where(k=>!selectedKeys.Contains(k)).ToArray())streak.Remove(key);
    for(int i=0;i<selected.Length;i++){
     var m=selected[i];string id="nyx-zone:"+BDirectAttackInput.Digest(new JArray(binding,zone,pulse,m.Id,m.Generation)).Substring(7);
     var receipt=health.ApplyResolvedDamage(id,m.Id,magnitudes[i],tick,m.Protected);results.Add(receipt);
     streak.TryGetValue(Key(m),out int count);streak[Key(m)]=receipt.HpLoss>0?Math.Min(2,count+1):0;
    }
    pulse++;lastTick=tick;if(pulse==(ultimate?8:6))Clear();else nextTick=Due(pulse+1);
    return results.AsReadOnly();
   }catch{faulted=true;Clear();throw;}
  }
  // Read only when reserving a NEW basic opportunity; never rewrite an existing schedule.
  public int BasicPeriodIncreaseBps(string id,long generation)=>Active&&health.Hp(Caster)>0&&current.Any(m=>m.Id==id&&m.Generation==generation&&!m.DelayImmune&&health.Hp(id)>0)?(ultimate?2500:1500):0;
  // All eligible living members, independent of the periodic damage target cap.
  public JArray MitigationRateReductionSources(string id,long generation){
   var rows=new JArray();if(Active&&health.Hp(Caster)>0&&current.Any(m=>m.Id==id&&m.Generation==generation&&!m.MitigationRateImmune&&health.Hp(id)>0))
    rows.Add(new JObject{["sourceId"]="nyx-zone:"+BDirectAttackInput.Digest(new JArray(binding,zone)).Substring(7),["valueBps"]=ultimate?1500:800});
   return rows;
  }
  public void Dispose(){Clear();created.Clear();disposed=true;health=null;attack=null;defense=null;}
 }
}

