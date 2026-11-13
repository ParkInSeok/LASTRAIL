using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // BOUNDED validation owner only. Clone-stage both domains, then publish together.
 // No full-run/node rollover or original ability/timer/START authority is provided here.
 public sealed class BCarriageBuffLocalValidation {
  public const string Version="unity.combat-b.buff-validation-local.v1";
  readonly BCarriageBuffDefinitions definitions;readonly JObject departure,initial;readonly BCombatFormation formation;readonly BCombatHpBinding[] bindings;readonly int hpCapacity;
  BCarriageBuffModel buffs;BCombatLocalModel hp;readonly JArray events=new JArray();
  readonly Dictionary<string,(JObject command,BCombatSingleTargetReceipt receipt)> receipts=new Dictionary<string,(JObject,BCombatSingleTargetReceipt)>(StringComparer.Ordinal);
  public BCarriageBuffLocalValidation(BCarriageBuffDefinitions defs,JObject candidate,JObject originalState,BCombatFormation profile,IEnumerable<BCombatHpBinding> originalHp,int eventCapacity){
   definitions=defs;departure=(JObject)candidate.DeepClone();initial=(JObject)originalState.DeepClone();formation=profile;bindings=originalHp.ToArray();hpCapacity=eventCapacity;
   buffs=new BCarriageBuffModel(defs,departure,initial);var cars=(JArray)departure["cars"];
   BuffWire.Need(profile.Mode==BCombatMode.Solo&&profile.Battlefield==BCombatBattlefield.ExternalVertical&&cars.Count==profile.Slots.Count);
   for(int slot=0;slot<profile.Slots.Count;slot++){var car=cars.SingleOrDefault(c=>(int)c["slot"]==slot);BuffWire.Need(car!=null&&(string)car["instanceId"]==profile.Slots[slot].CarriageId);
    var o=car["occupant"];BuffWire.Need((string)o["kind"]=="CARGO"?profile.Slots[slot].IsCargo:(string)o["operatorId"]==profile.Slots[slot].OperatorId);
    var state=((JArray)initial["cars"]).Single(s=>(int)s["slot"]==slot);BuffWire.Need((string)state["body"]=="NORMAL"&&(string)state["actor"]==(profile.Slots[slot].IsCargo?"ABSENT":"UP"));}
   BuffWire.Need((string)initial["mode"]=="EXTERIOR");hp=BCombatLocalModel.CreateWithCarriageTransfer(profile,bindings,eventCapacity,BCombatAuthority.UnitySolo);
  }
  public JObject AcceptOriginal(JObject input){
   string id=BuffWire.Text(input?["eventId"]);var command=new JObject{["kind"]="ACCEPT",["eventId"]=id,["accepted"]=input.DeepClone()};
   if(Replay(id,command,out var old))return buffs.Source(id);BuffWire.Need(!hp.TrainDefeated,"combat_b_train_defeated");var source=buffs.Accept(input);events.Add(command);receipts.Add(id,(command,null));return source;
  }
  public int Hp(string target)=>hp.Hp(target);
  public JObject BuffSnapshot()=>buffs.Snapshot();public JObject Capacity()=>buffs.Capacity();
  bool Replay(string id,JObject command,out BCombatSingleTargetReceipt receipt){BCombatFormation.Identity(id);if(receipts.TryGetValue(id,out var old)){BuffWire.Need(JToken.DeepEquals(old.command,command),"combat_b_event_conflict");receipt=old.receipt;return true;}BuffWire.Need(events.Count<hpCapacity,"combat_b_validation_capacity");receipt=null;return false;}
  BCombatLocalModel StageHp()=>BCombatLocalModel.RestoreLocal(hp.CaptureLocal(),formation,bindings,hpCapacity,BCombatAuthority.UnitySolo);
  BCombatSingleTargetReceipt Publish(string id,JObject command,BCombatSingleTargetReceipt receipt,BCarriageBuffModel b,BCombatLocalModel v){buffs=b;hp=v;events.Add(command);receipts.Add(id,(command,receipt));return receipt;}
  static string StateId(string kind,string original){using var sha=SHA256.Create();return kind+":"+BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(original))).Replace("-","").ToLowerInvariant();}
  public BCombatSingleTargetReceipt ApplyIncomingDamage(string id,string target,JObject originalNumeric,CombatExactValue originalFlat,JArray percent,long tick){
   var command=new JObject{["kind"]="INCOMING",["eventId"]=id,["targetId"]=target,["numeric"]=originalNumeric.DeepClone(),["flat"]=originalFlat.Snapshot(),["percent"]=percent.DeepClone(),["tick"]=tick};
   if(Replay(id,command,out var old))return old;BuffWire.Need(!hp.TrainDefeated,"combat_b_train_defeated");
   int slot=-1;string recipient=null;for(int n=0;n<formation.Slots.Count;n++){if(formation.Slots[n].CarriageId==target){slot=n;recipient="BODY";}if(formation.Slots[n].OperatorId==target){slot=n;recipient="OPERATOR";}}
   BuffWire.Need(slot>=0,"combat_b_validation_target");var b=BCarriageBuffModel.Restore(buffs.Snapshot(),definitions);var v=StageHp();
   var numeric=(JObject)originalNumeric.DeepClone();numeric["defense"]=BCarriageBuffCalculator.DefenseStat(target,originalFlat,percent,b.Defense(slot,recipient)).Snapshot();
   int amount=CombatNumericDefenseBudget.Resolve(numeric).ToVitalsMagnitude();var r=v.ApplyResolvedDamage(id,target,amount,tick);
   if(hp.Hp(target)>0&&r.HpAfter==0)b.Transition(new JObject{["eventId"]=StateId(recipient=="BODY"?"BODY":"ACTOR",id),["expectedRevision"]=b.Revision,["kind"]=recipient=="BODY"?"BODY":"ACTOR",["slot"]=slot,["value"]=recipient=="BODY"?"BROKEN":"DOWN"});
   return Publish(id,command,r,b,v);
  }
  public BCombatSingleTargetReceipt ApplyShield(JObject acceptedInput,string target,CombatExactValue originalUnrounded,CombatShieldSource originalSource,long expiry,long tick){
   string id=BuffWire.Text(acceptedInput?["eventId"]);if(originalSource==null)throw new ArgumentNullException(nameof(originalSource));
   var command=new JObject{["kind"]="SHIELD",["eventId"]=id,["accepted"]=acceptedInput.DeepClone(),["targetId"]=target,["budget"]=originalUnrounded.Snapshot(),["sourceEntity"]=originalSource.EntityId,["sourceAbility"]=originalSource.AbilityId,["effectIndex"]=originalSource.EffectIndex,["expiry"]=expiry,["tick"]=tick};
   if(Replay(id,command,out var old))return old;BuffWire.Need(!hp.TrainDefeated,"combat_b_train_defeated");
   var b=BCarriageBuffModel.Restore(buffs.Snapshot(),definitions);var v=StageHp();BuffWire.Need((string)acceptedInput["kind"]=="SHIELD");int slot=(int)BuffWire.Number(acceptedInput["slot"],0,4);
   BuffWire.Need(slot<formation.Slots.Count&&formation.Slots[slot].OperatorId==originalSource.EntityId,"combat_b_effect_owner_invalid");b.Accept(acceptedInput);
   int amount=checked((int)new BCarriageBuffCalculator(b).Support(id,originalUnrounded).Floor());var r=v.ApplyResolvedShield(id,target,originalSource,amount,expiry,tick);return Publish(id,command,r,b,v);
  }
  public JObject Capture()=>new JObject{["version"]=Version,["definitionHash"]=definitions.Hash,["buffs"]=buffs.Snapshot(),["hp"]=JObject.Parse(hp.CaptureLocal()),["events"]=events.DeepClone()};
  public static BCarriageBuffLocalValidation Restore(JObject cache,BCarriageBuffDefinitions defs,BCombatFormation formation,IEnumerable<BCombatHpBinding> bindings,int capacity){
   BuffWire.Fields(cache,"version","definitionHash","buffs","hp","events");BuffWire.Need((string)cache["version"]==Version&&(string)cache["definitionHash"]==defs.Hash);var original=BuffWire.Object(cache["buffs"]);
   var r=new BCarriageBuffLocalValidation(defs,BuffWire.Object(original["departure"]),BuffWire.Object(original["initial"]),formation,bindings,capacity);
   foreach(var row in BuffWire.Array(cache["events"],0,capacity)){var e=BuffWire.Object(row);string kind=(string)e["kind"];
    if(kind=="ACCEPT"){BuffWire.Fields(e,"kind","eventId","accepted");BuffWire.Need((string)e["eventId"]==(string)e["accepted"]["eventId"]);r.AcceptOriginal(BuffWire.Object(e["accepted"]));}
    else if(kind=="INCOMING"){BuffWire.Fields(e,"kind","eventId","targetId","numeric","flat","percent","tick");r.ApplyIncomingDamage(BuffWire.Text(e["eventId"]),BuffWire.Text(e["targetId"]),BuffWire.Object(e["numeric"]),CombatExactValue.From(BuffWire.Object(e["flat"])),BuffWire.Array(e["percent"],0,1024),BuffWire.Number(e["tick"]));}
    else{BuffWire.Need(kind=="SHIELD");BuffWire.Fields(e,"kind","eventId","accepted","targetId","budget","sourceEntity","sourceAbility","effectIndex","expiry","tick");BuffWire.Need((string)e["eventId"]==(string)e["accepted"]["eventId"]);r.ApplyShield(BuffWire.Object(e["accepted"]),BuffWire.Text(e["targetId"]),CombatExactValue.From(BuffWire.Object(e["budget"])),new CombatShieldSource(BuffWire.Text(e["sourceEntity"]),BuffWire.Text(e["sourceAbility"]),(int)BuffWire.Number(e["effectIndex"],0,int.MaxValue)),BuffWire.Number(e["expiry"]),BuffWire.Number(e["tick"]));}
   }
   BuffWire.Need(JToken.DeepEquals(r.Capture(),cache),"combat_b_validation_snapshot_conflict");return r;
  }
 }
}