using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Frozen trusted LOCAL binding. No operating source authentication, inferred target category, or START.
 public sealed class BDirectAttackBuffBinding {
  public const string WaveVersion="unity.combat-b.wave-buff-binding-local.v1.20261004";
  public const string Version="unity.combat-b.direct-buff-binding-local.v1";
  readonly JObject original;readonly Dictionary<string,string> categories=new Dictionary<string,string>(StringComparer.Ordinal);
  public BDirectAttackInput Attack {get;}public BCarriageBuffDefinitions Definitions {get;}public string OriginalHash {get;}
  internal JObject Departure=>(JObject)original["departure"].DeepClone();internal JObject Initial=>(JObject)original["initial"].DeepClone();
  public JObject Capture()=>(JObject)original.DeepClone();
  public BDirectAttackBuffBinding(BDirectAttackInput attack,BCarriageBuffDefinitions definitions,JObject binding,string trustedBindingHash){
   bool wave=attack?.IsWaveRuntime==true;
   Attack=attack??throw new ArgumentNullException(nameof(attack));Definitions=definitions??throw new ArgumentNullException(nameof(definitions));
   BuffWire.Need(binding!=null&&BasicCombatSimulation.StableJson(binding).Length<=65536&&BDirectAttackInput.Digest(binding)==trustedBindingHash,"b_attack_buff_binding_hash");
   BuffWire.Fields(binding,"version","provenance","attackInputHash","definitionHash","departure","initial","targetCategories");
   BuffWire.Need((string)binding["version"]==(wave?WaveVersion:Version)&&(string)binding["provenance"]=="BOUNDED_LOCAL_NOT_FULL_RUN"&&(string)binding["attackInputHash"]==attack.OriginalHash&&(string)binding["definitionHash"]==definitions.Hash);
   var d=BuffWire.Object(binding["departure"]);var i=BuffWire.Object(binding["initial"]);new BCarriageBuffModel(definitions,d,i);
   BuffWire.Need((string)d["accountId"]==attack.OriginalAccountId,"b_attack_buff_account");var cars=(JArray)d["cars"];var profile=attack.Formation;
   BuffWire.Need(cars.Count==profile.Slots.Count&&profile.Mode==BCombatMode.Solo&&profile.Battlefield==BCombatBattlefield.ExternalVertical);
   for(int slot=0;slot<profile.Slots.Count;slot++){var car=cars.SingleOrDefault(c=>(long)c["slot"]==slot);BuffWire.Need(car!=null&&(string)car["instanceId"]==profile.Slots[slot].CarriageId,"b_attack_buff_physical_binding");
    var o=car["occupant"];BuffWire.Need((string)o["kind"]=="CARGO"?profile.Slots[slot].IsCargo:(string)o["operatorId"]==profile.Slots[slot].OperatorId,"b_attack_buff_occupant_binding");
    var state=((JArray)i["cars"]).Single(c=>(long)c["slot"]==slot);BuffWire.Need((string)state["body"]=="NORMAL"&&(string)state["actor"]==(profile.Slots[slot].IsCargo?"ABSENT":"UP"),"b_attack_buff_initial_hp_state");}
   BuffWire.Need((string)i["mode"]=="EXTERIOR");
   foreach(var row in BuffWire.Array(binding["targetCategories"],0,wave?32:5)){var c=BuffWire.Object(row);BuffWire.Fields(c,"targetId","category");string id=BuffWire.Text(c["targetId"]);BuffWire.Need(attack.Entities.TryGetValue(id,out var e)&&e.Kind=="LOCAL_ENEMY"&&!categories.ContainsKey(id));
    categories.Add(id,BuffWire.One(c["category"],"STRUCTURE","ATTACHED","CLIMBING","APPROACH","ROOFTOP","ENEMY_TRAIN_BODY","ENEMY_TRAIN_PART","ENEMY"));}
   BuffWire.Need(new HashSet<string>(categories.Keys).SetEquals(attack.Entities.Values.Where(e=>e.Kind=="LOCAL_ENEMY").Select(e=>e.Id)),"b_attack_buff_category_missing");
   original=(JObject)binding.DeepClone();OriginalHash=trustedBindingHash;
  }
  internal string Category(string target)=>categories.TryGetValue(target,out var c)?c:throw new ArgumentException("b_attack_buff_category_missing");
 }
}
