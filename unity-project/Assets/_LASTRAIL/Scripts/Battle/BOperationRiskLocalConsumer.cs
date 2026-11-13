using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Explicit LOCAL validation composition. The existing kernel remains the sole HP/shield/router owner.
 public sealed class BOperationRiskLocalConsumer {
  readonly BOperationRiskSource source;readonly BCombatFormation formation;readonly BCombatHpBinding[] bindings;readonly int capacity;
  BCombatLocalModel native;readonly JArray commands=new JArray();readonly Dictionary<string,BCombatSingleTargetReceipt> receipts=new Dictionary<string,BCombatSingleTargetReceipt>();
  BOperationRiskLocalConsumer(BOperationRiskSource source,BCombatFormation formation,IEnumerable<BCombatHpBinding> bindings,int capacity){this.source=source??throw new ArgumentNullException(nameof(source));this.formation=formation;this.bindings=bindings.ToArray();this.capacity=capacity;native=BCombatLocalModel.CreateWithCarriageTransfer(formation,this.bindings,capacity,BCombatAuthority.UnitySolo);}
  public static BOperationRiskLocalConsumer CreateLocalValidation(BOperationRiskSource source,BCombatFormation formation,IEnumerable<BCombatHpBinding> bindings,int capacity)=>new BOperationRiskLocalConsumer(source,formation,bindings,capacity);
  public int Hp(string target)=>native.Hp(target);
  bool Replay(JObject command,out BCombatSingleTargetReceipt old){string id=(string)command["eventId"];BCombatFormation.Identity(id);if(receipts.TryGetValue(id,out old)){if(!JToken.DeepEquals(commands.Single(c=>(string)c["eventId"]==id),command))throw new ArgumentException("Original risk command changed");return true;}if(commands.Count>=capacity)throw new ArgumentException("Local risk capacity");return false;}
  BCombatLocalModel Stage()=>BCombatLocalModel.RestoreLocal(native.CaptureLocal(),formation,bindings,capacity,BCombatAuthority.UnitySolo);
  BCombatSingleTargetReceipt Publish(JObject command,BCombatLocalModel staged,BCombatSingleTargetReceipt receipt){commands.Add(command);receipts.Add(receipt.EventId,receipt);native=staged;return receipt;}
  public BCombatSingleTargetReceipt ApplyOriginalAction(string eventId,string target,long tick,JObject originalAction,bool protectedFromDamage=false){
   if(originalAction==null)throw new ArgumentNullException(nameof(originalAction));var command=new JObject{["kind"]="ACTION",["eventId"]=eventId,["target"]=target,["tick"]=tick,["action"]=originalAction.DeepClone(),["protected"]=protectedFromDamage};if(Replay(command,out var old))return old;
   int magnitude=source.ResolveOriginalAction((JObject)command["action"]).ToVitalsMagnitude();var staged=Stage();var receipt=staged.ApplyResolvedDamage(eventId,target,magnitude,tick,protectedFromDamage);return Publish(command,staged,receipt);
  }
  public BCombatSingleTargetReceipt ApplyOriginalShield(string eventId,string target,CombatShieldSource shield,int magnitude,long expiry,long tick){
   if(shield==null)throw new ArgumentNullException(nameof(shield));var command=new JObject{["kind"]="SHIELD",["eventId"]=eventId,["target"]=target,["tick"]=tick,["entity"]=shield.EntityId,["ability"]=shield.AbilityId,["index"]=shield.EffectIndex,["magnitude"]=magnitude,["expiry"]=expiry};if(Replay(command,out var old))return old;var staged=Stage();var receipt=staged.ApplyResolvedShield(eventId,target,shield,magnitude,expiry,tick);return Publish(command,staged,receipt);
  }
  public JObject Capture()=>new JObject{["schema"]="unity.combat-b.operation-risk-local.v1",["packetHash"]=source.Capture()["packetHash"],["binding"]=source.Binding,["capacity"]=capacity,["commands"]=commands.DeepClone(),["native"]=JObject.Parse(native.CaptureLocal())};
  public static BOperationRiskLocalConsumer RestoreLocalValidation(JObject input,BOperationRiskSource verifiedSource,BCombatFormation formation,IEnumerable<BCombatHpBinding> bindings,int capacity){
   if(input==null)throw new ArgumentNullException(nameof(input));var cache=(JObject)input.DeepClone();BuffWire.Fields(cache,"schema","packetHash","binding","capacity","commands","native");var owner=CreateLocalValidation(verifiedSource,formation,bindings,capacity);var empty=owner.Capture();foreach(var key in new[]{"schema","packetHash","binding","capacity"})BuffWire.Need(JToken.DeepEquals(cache[key],empty[key]),"risk_restore_binding_conflict");var rows=cache["commands"] as JArray;BuffWire.Need(rows!=null&&rows.Count<=capacity,"risk_restore_commands");
   foreach(var token in rows){var r=BuffWire.Object(token);string id=BuffWire.Text(r["eventId"]),target=BuffWire.Text(r["target"]);BuffWire.Need(!owner.receipts.ContainsKey(id),"risk_restore_duplicate");long tick=BuffWire.Number(r["tick"]);if((string)r["kind"]=="ACTION"){BuffWire.Fields(r,"kind","eventId","target","tick","action","protected");owner.ApplyOriginalAction(id,target,tick,BuffWire.Object(r["action"]),BuffWire.Flag(r["protected"]));}else{BuffWire.Fields(r,"kind","eventId","target","tick","entity","ability","index","magnitude","expiry");BuffWire.Need((string)r["kind"]=="SHIELD","risk_restore_kind");owner.ApplyOriginalShield(id,target,new CombatShieldSource(BuffWire.Text(r["entity"]),BuffWire.Text(r["ability"]),checked((int)BuffWire.Number(r["index"]))),checked((int)BuffWire.Number(r["magnitude"])),BuffWire.Number(r["expiry"]),tick);}}
   BuffWire.Need(JToken.DeepEquals(cache,owner.Capture()),"risk_restore_state_conflict");return owner;
  }
 }
}
