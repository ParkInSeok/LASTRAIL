using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Trusted native effect port, not UI authorization or a new server packet. Vitals stay model-owned.
 internal sealed class OrdinaryDamageTargets {
  readonly CombatSeraInput input;readonly IReadOnlyDictionary<string,CombatVitals> health;readonly long stepMs;
  internal bool TrainDefeated=>health[input.Train.TrainId].Hp==0;
  internal OrdinaryDamageTargets(CombatSeraInput input,IReadOnlyDictionary<string,CombatVitals> health,long stepMs){this.input=input;this.health=health;this.stepMs=stepMs;}
  internal void Validate(JObject c){
   Need((string)c["accountId"]==input.OrdinaryAccount&&(string)c["runId"]==input.SimulationId);
   string id=CombatSeraInput.Text(c,"healthTargetId"),kind=(string)c["targetKind"];
   Need(kind=="TRAIN"?id==input.Train.TrainId:kind=="OPERATOR"&&input.Entities.Any(e=>e.Id==id));
   Need(health.ContainsKey(id)&&CombatSeraInput.Nat(c,"atMs")%stepMs==0);
   if((string)c["kind"]=="ORDINARY_DAMAGE"){
    var actor=kind=="OPERATOR"?input.Entities.Single(e=>e.Id==id):null;
    // Conditional/live stat transitions need their original owner before this port can consume them.
    Need(actor==null||actor.ConditionalDefenseFlat==0);
    var defense=actor?.Defense??CombatExactValue.Create(input.TrainDefense,1);
    Need(c["protected"]?.Type==JTokenType.Boolean&&JToken.DeepEquals(c["numeric"]?["defense"],defense.Snapshot()));
    _=CombatNumericDefenseBudget.Resolve(c["numeric"] as JObject);
   }else{
    Need((string)c["kind"]=="ORDINARY_SHIELD"&&health.ContainsKey(CombatSeraInput.Text(c,"sourceId")));
    CombatSeraInput.Text(c,"abilityId");CombatSeraInput.Nat(c,"effectIndex",0,int.MaxValue);CombatSeraInput.Nat(c,"amount",0,int.MaxValue);
    long expiry=CombatSeraInput.Nat(c,"expiryMs");Need(expiry>CombatSeraInput.Nat(c,"atMs")&&expiry%stepMs==0);
   }
  }
  internal JObject Apply(JObject c){
   Validate(c);var hp=health[(string)c["healthTargetId"]];long tick=(long)c["atMs"]/stepMs;
   var result=new JObject{["status"]="APPLIED",["targetKind"]=c["targetKind"].DeepClone(),["healthTargetId"]=c["healthTargetId"].DeepClone(),["beforeHp"]=hp.Hp};
   if((string)c["kind"]=="ORDINARY_DAMAGE"){
    var numeric=CombatNumericDefenseBudget.Resolve((JObject)c["numeric"]);var damage=hp.Damage(numeric.ToVitalsMagnitude(),tick,(bool)c["protected"]);
    result["calculation"]=numeric.Snapshot();result["hpLoss"]=damage.HpLoss;result["absorbed"]=damage.Absorbed;
   }else{
    result["resultingShield"]=hp.Shield(new CombatShieldSource((string)c["sourceId"],(string)c["abilityId"],(int)c["effectIndex"]),(int)c["amount"],(long)c["expiryMs"]/stepMs,tick);
   }
   result["afterHp"]=hp.Hp;return result;
  }
  static void Need(bool ok){if(!ok)throw new ArgumentException("ordinary_effect_target_or_source_conflict");}
 }
}
