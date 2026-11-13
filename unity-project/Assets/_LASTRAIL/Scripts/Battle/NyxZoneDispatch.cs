using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Read-only in-process ports from the registered zone owner; not a server wire format or a policy approval.
 internal sealed class NyxZoneSources {
  internal readonly string BindingId,PolicyHash;
  internal readonly Func<string,bool,long,JObject> Select;
  internal readonly Func<string,JObject,long,JObject> ConfirmCreation;
  internal readonly Func<JObject,long,JObject> Observe;
  internal NyxZoneSources(string bindingId,string policyHash,Func<string,bool,long,JObject> select,Func<string,JObject,long,JObject> confirm,Func<JObject,long,JObject> observe){
   BCombatFormation.Identity(bindingId);if(policyHash==null||!Regex.IsMatch(policyHash,"\\A[a-f0-9]{64}\\z"))throw new ArgumentException("Independently trusted zone selection policy hash required.");
   BindingId=bindingId;PolicyHash=policyHash;Select=select??throw new ArgumentNullException(nameof(select));ConfirmCreation=confirm??throw new ArgumentNullException(nameof(confirm));Observe=observe??throw new ArgumentNullException(nameof(observe));
  }
 }
 // Selection and observed generation binding only. HP, passive and periodic budgets remain in NyxZoneExecution.
 internal sealed class NyxZoneDispatch {
  readonly NyxZoneExecution core;readonly NyxZoneSources sources;
  JObject selected,active;string pending;bool ultimate;long observedTick=-1,revision=-1;string observedHash;
  internal string ActiveCommandId{get;private set;}
  internal NyxZoneDispatch(NyxZoneExecution core,NyxZoneSources sources){this.core=core??throw new ArgumentNullException(nameof(core));this.sources=sources??throw new ArgumentNullException(nameof(sources));Need(core.BindingId==sources.BindingId);}
  static void Need(bool condition){if(!condition)throw new ArgumentException("Original Nyx zone observation differs.");}
  static long Nat(JObject row,string key,long min=0)=>BDirectAttackInput.Nat(row,key,min);
  static bool Flag(JObject row,string key){Need(row[key]?.Type==JTokenType.Boolean);return (bool)row[key];}
  static long[] Point(JToken value){Need(value is JArray a&&a.Count==3&&a.All(v=>v.Type==JTokenType.Integer));var p=value.Select(v=>(long)v).ToArray();Need(p.All(v=>v>=-1000000000&&v<=1000000000));return p;}
  JObject Read(JObject value,long tick){
   Need(value!=null&&tick>=0);BDirectAttackInput.Fields(value,"bindingId","policyHash","tick","revision","present","eligible","footWorldMm","centerWorldMm","lineOfSight","zone","members","focusTargetId");
   Need(value["focusTargetId"].Type==JTokenType.Null||value["focusTargetId"].Type==JTokenType.String);if(value["focusTargetId"].Type==JTokenType.String)BCombatFormation.Identity((string)value["focusTargetId"]);
   Need((string)value["bindingId"]==sources.BindingId&&(string)value["policyHash"]==sources.PolicyHash&&Nat(value,"tick")==tick);Nat(value,"revision",1);Flag(value,"present");Flag(value,"eligible");Point(value["footWorldMm"]);Point(value["centerWorldMm"]);Flag(value,"lineOfSight");
   var zone=value["zone"] as JObject;BDirectAttackInput.Fields(zone,"id","ownerGeneration","frameId","frameGeneration","carId","scope","minimumMm","maximumMm","representativeCenterMm");
   foreach(string key in new[]{"id","frameId","carId"})BCombatFormation.Identity(BDirectAttackInput.Text(zone,key));Nat(zone,"ownerGeneration",1);Nat(zone,"frameGeneration",1);
   Need(new[]{"ALLIED_INTRUDERS","ENEMY_CARRIAGE"}.Contains((string)zone["scope"]));var min=Point(zone["minimumMm"]);var max=Point(zone["maximumMm"]);Need(min[0]<max[0]&&min[1]<=max[1]&&min[2]<max[2]);
   var localCenter=Point(zone["representativeCenterMm"]);Need(Enumerable.Range(0,3).All(i=>localCenter[i]>=min[i]&&localCenter[i]<=max[i]));
   Need(value["members"] is JArray rows&&rows.Count<=1000);var ids=new HashSet<string>(StringComparer.Ordinal);
   foreach(JObject member in (JArray)value["members"]){BDirectAttackInput.Fields(member,"id","generation","delayImmune","protected","mitigationRateImmune");string id=BDirectAttackInput.Text(member,"id");BCombatFormation.Identity(id);Need(id!="op_nyx"&&ids.Add(id));Nat(member,"generation",1);Flag(member,"delayImmune");Flag(member,"protected");Flag(member,"mitigationRateImmune");}
   return (JObject)value.DeepClone();
  }
  void ObserveOrder(JObject observation,long tick){long next=(long)observation["revision"];string hash=BDirectAttackInput.Digest(observation);Need(tick>=observedTick&&next>=revision&&(next!=revision||observedHash==hash));observedTick=tick;revision=next;observedHash=hash;}
  internal bool Prepare(string commandId,bool ultimate,long tick){
   BCombatFormation.Identity(commandId);if(pending!=null)throw new InvalidOperationException("Nyx cast already pending.");if(!core.CasterAlive)return false;
   var raw=sources.Select(commandId,ultimate,tick);if(raw==null)return false;var view=Read(raw,tick);var foot=Point(view["footWorldMm"]);var center=Point(view["centerWorldMm"]);
   decimal squared=0;for(int i=0;i<3;i++){decimal delta=center[i]-foot[i];squared+=delta*delta;}
   const long SelectionDistanceMm=48000;
   if(!Flag(view,"present")||!Flag(view,"eligible")||squared>SelectionDistanceMm*SelectionDistanceMm||!Flag(view,"lineOfSight"))return false;
   selected=(JObject)view["zone"].DeepClone();pending=commandId;this.ultimate=ultimate;return true;
  }
  internal bool Commit(string commandId,long tick){
   if(pending!=commandId)throw new InvalidOperationException("Original Nyx cast required.");
   var choice=selected;Cancel();if(!core.CasterAlive)return false;var raw=sources.ConfirmCreation(commandId,(JObject)choice.DeepClone(),tick);if(raw==null)return false;
   // Acceptance owns distance and LOS; creation rechecks the frozen owner/zone, never retargets.
   var view=Read(raw,tick);if(!JToken.DeepEquals(choice,view["zone"])||!Flag(view,"present"))return false;
   // New successful creation owns a new zone identity; failed creation leaves the previous zone intact.
   core.CommitCreatedZone((string)choice["id"],tick,ultimate);active=choice;ActiveCommandId=commandId;observedTick=-1;revision=-1;observedHash=null;Apply(view,tick,true);return true;
  }
  internal void Cancel(){pending=null;selected=null;}
  IReadOnlyList<BCombatSingleTargetReceipt> Apply(JObject view,long tick,bool running){
   Need(JToken.DeepEquals(active,view["zone"]));ObserveOrder(view,tick);
   var members=((JArray)view["members"]).Select(m=>new NyxZoneMember((string)m["id"],(long)m["generation"],(bool)m["delayImmune"],(bool)m["protected"],(bool)m["mitigationRateImmune"])).ToArray();
   var result=core.Step(tick,members,(string)view["focusTargetId"],running,(bool)view["present"]);if(!core.Active)active=null;return result;
  }
  internal IReadOnlyList<BCombatSingleTargetReceipt> Step(long tick,bool nodeRunning){
   if(!nodeRunning||!core.CasterAlive){Cancel();active=null;return core.Step(tick,Array.Empty<NyxZoneMember>(),null,false,false);}
   if(active==null)return Array.Empty<BCombatSingleTargetReceipt>();return Apply(Read(sources.Observe((JObject)active.DeepClone(),tick),tick),tick,true);
  }
  internal NyxZoneDispatch CloneOwned(NyxZoneExecution clonedCore)=>new NyxZoneDispatch(clonedCore,sources){selected=(JObject)selected?.DeepClone(),active=(JObject)active?.DeepClone(),pending=pending,ultimate=ultimate,observedTick=observedTick,revision=revision,observedHash=observedHash,ActiveCommandId=ActiveCommandId};
 }
}
