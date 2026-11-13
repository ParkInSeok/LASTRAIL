using System;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 internal static class BActiveStateCapture {
  internal static JObject WithoutInactiveEntityState(this JObject state,bool active){if(!active)state.Remove("activeEntityState");return state;}
 }
 public sealed partial class BDirectAttackSession {
  // Live entity/stat observations belong to the existing HP/clock owner, not to a shot or View.
  JObject activeState;
  internal void ConfigureActiveTarget(JObject identity){if(activeState!=null||Tick!=0)throw new InvalidOperationException("b_active_entity_already_bound");var e=input.Entities[(string)identity["targetId"]];activeState=new JObject{["target"]=identity.DeepClone(),["present"]=true,["range"]=true,["immune"]=false,["defense"]=e.Defense.Snapshot(),["attack"]=input.Entities["op_mira"].Attack.Snapshot()};}
  internal long DebtHalfTicks=>clock.Capture().DebtHalfTicks;
  internal void RecordTargetLifetime(JObject originalEvent,JObject target){
   Room();BuffWire.Need(activeState!=null&&(long)originalEvent["originalTick"]==Tick&&JToken.DeepEquals(originalEvent["previous"],activeState["target"]),"b_lifecycle_native_lifetime");
   string id=(string)target["targetId"];BuffWire.Need(id==(string)activeState["target"]["targetId"]&&input.Entities[id].Kind=="LOCAL_ENEMY","b_lifecycle_target_profile");var e=input.Entities[id];var pristine=new CombatVitals(e.MaxHp,0,true);
   foreach(var s in (JArray)input.Capture()["initialShields"])if((string)s["targetId"]==id)pristine.Shield(new CombatShieldSource((string)s["sourceEntity"],(string)s["sourceAbility"],(int)s["effectIndex"]),(int)s["amount"],(long)s["expiryTick"],Tick);
   enemies[id]=pristine;activeState["target"]=target.DeepClone();activeState["defense"]=e.Defense.Snapshot();activeState["immune"]=false;activeState["present"]=true;activeState["range"]=true;events.Add(new JObject{["kind"]="TARGET_LIFETIME",["input"]=originalEvent.DeepClone(),["target"]=target.DeepClone()});
  }
  internal JObject ActiveTarget=>(JObject)activeState["target"].DeepClone();
  internal CombatExactValue ActiveAttack=>CombatStatResolver.Attack("op_mira",CombatExactValue.From((JObject)activeState["attack"]),(JArray)input.Entities["op_mira"].Original["attackSources"]);
  internal bool ActiveTargetEligible(JObject locked,bool emitting){
   string target=(string)locked["targetId"];if(activeState==null||!JToken.DeepEquals(locked,activeState["target"])||!(bool)activeState["present"]||Hp(target)==0)return false;
   if(!emitting)return true;
   return Hp("op_mira")>0&&(bool)activeState["range"]&&input.TryGetGeometry("op_mira",target,out var g)&&(bool)g["eligible"]&&Tick>=(long)g["observedTick"]&&Tick<=(long)g["validUntilTick"]&&(long)g["distanceMm"]<=(long)g["maximumDistanceMm"]&&BCombatRoleRanges.For("op_mira").BasicAttackIncludesSlot(input.Entities["op_mira"].Slot,input.Entities[target].Slot,input.Formation.Slots.Count);
  }
  // These are admitted original LOCAL domain events; they never alter the frozen source packet.
  internal BCombatSingleTargetReceipt ApplyActiveObservation(JObject command){
   Room();BuffWire.Need(activeState!=null,"b_active_state_unbound");BuffWire.Need((string)command["kind"]=="LOCAL_ORIGINAL","b_active_observation_kind");string kind=BuffWire.Text(command["operation"]);BCombatSingleTargetReceipt result=null;string id=BuffWire.Text(command["eventId"]);BCombatFormation.Identity(id);
   if(kind=="DAMAGE"){BuffWire.Fields(command,"kind","eventId","operation","targetId","amount");string target=BuffWire.Text(command["targetId"]);int amount=(int)BuffWire.Number(command["amount"],0,int.MaxValue);if(enemies.TryGetValue(target,out var v)){var hit=v.Damage(amount,Tick,false);result=new BCombatSingleTargetReceipt(id,target,Tick,hit.HpLoss,v.Hp,hit.Absorbed);}else result=allies.ApplyResolvedDamage(id,target,amount,Tick);}
   else if(kind=="TARGET"){BuffWire.Fields(command,"kind","eventId","operation","identity","present","range");var t=BuffWire.Object(command["identity"]);BuffWire.Fields(t,"targetId","generation","battlefieldGeneration");foreach(string k in new[]{"targetId","generation","battlefieldGeneration"})BuffWire.Text(t[k]);BuffWire.Need((string)t["targetId"]==(string)activeState["target"]["targetId"],"b_active_target_unknown");bool present=BuffWire.Flag(command["present"]),range=BuffWire.Flag(command["range"]);if(!JToken.DeepEquals(t,activeState["target"])){var e=input.Entities[(string)t["targetId"]];enemies[e.Id]=new CombatVitals(e.MaxHp,0,true);activeState["defense"]=e.Defense.Snapshot();activeState["immune"]=false;}activeState["target"]=t.DeepClone();activeState["present"]=present;activeState["range"]=range;}
   else if(kind=="ATTACK"||kind=="DEFENSE"){BuffWire.Fields(command,"kind","eventId","operation","value");var value=CombatExactValue.From(BuffWire.Object(command["value"]));BuffWire.Need(value.Numerator>=0,"b_active_negative_stat");activeState[kind=="ATTACK"?"attack":"defense"]=value.Snapshot();}
   else if(kind=="IMMUNITY"){BuffWire.Fields(command,"kind","eventId","operation","value");activeState["immune"]=BuffWire.Flag(command["value"]);}
   else if(kind=="SHIELD"){BuffWire.Fields(command,"kind","eventId","operation","amount","expiry");string target=(string)activeState["target"]["targetId"];enemies[target].Shield(new CombatShieldSource("LOCAL_ORIGINAL","U14_SHIELD",0),(int)BuffWire.Number(command["amount"],0,int.MaxValue),BuffWire.Number(command["expiry"],Tick+1),Tick);}
   else throw new ArgumentException("b_active_observation_unsupported");events.Add(command.DeepClone());return result;
  }
  internal BDirectAttackReceipt ApplyActiveImpact(string id,string target,CombatExactValue attack,BCarriageBuffModel buffs,string shotId){
   Room();BuffWire.Need(activeState!=null&&!Paused&&!TrainDefeated&&Hp(target)>0,"b_active_impact_unavailable");var baseValue=attack.Multiply(CombatExactValue.Create(4500,10000));
   var numeric=input.NumericInput(input.Entities["op_mira"],input.Entities[target],baseValue,false);numeric["defense"]=activeState["defense"].DeepClone();var calculation=new BCarriageBuffCalculator(buffs).Damage(shotId,numeric);int amount=calculation.ToVitalsMagnitude();var v=enemies[target];var hit=v.Damage(amount,Tick,(bool)activeState["immune"]);var hp=new BCombatSingleTargetReceipt(id,target,Tick,hit.HpLoss,v.Hp,hit.Absorbed);var receipt=new BDirectAttackReceipt(id,"op_mira",Tick,amount,calculation.Snapshot(),hp);var command=new JObject{["kind"]="ACTIVE_IMPACT",["eventId"]=id,["targetId"]=target,["shotId"]=shotId,["tick"]=Tick,["attack"]=attack.Snapshot()};events.Add(command);originals.Add(id,(command,receipt));return receipt;
  }
 }
}
