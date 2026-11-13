using System;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 public sealed partial class BDirectAttackSession {
  JObject o22Application;
  internal void ApplyO22(JObject application){
   if(!modified)throw new InvalidOperationException("Native staged owner required");
   if(o22Application!=null){if(!JToken.DeepEquals(o22Application,application))throw new InvalidOperationException("O22 original application changed");return;}
   Room();o22Application=(JObject)application.DeepClone();events.Add(new JObject{["kind"]="O22_APPLIED",["application"]=application.DeepClone(),["tick"]=Tick});
  }
  JObject ResolveNativeLifesteal(string action,string actor,string target,BCombatSingleTargetReceipt damage){
   if(o22Application==null||input.Entities[actor].Kind!="OPERATOR"||input.Entities[target].Kind!="LOCAL_ENEMY")return null;
   // This first executor has immutable original operators and no replacement,
   // revival or healing-modifier actions. Down is read from the real HP owner.
   // Delayed/projectile executors are rejected at APPLIED, not silently mapped.
   int before=allies.Hp(actor),maximum=allies.MaximumHp(actor);
   int request=CombatAttributedLifesteal.BoundedRecovery(damage.HpLoss,10000,before,maximum);
   string healId="o22-heal:"+BasicCombatSimulation.Digest(BasicCombatSimulation.StableJson(new JArray(input.OriginalHash,o22Application["applicationId"],action,actor)));
   var healed=allies.ApplyLifesteal(healId,actor,request,Tick);
   return new JObject{["version"]="combat.b-native-lifesteal.v1.20261019",["applicationId"]=o22Application["applicationId"].DeepClone(),["definitionPacketHash"]=o22Application["definitionPacketHash"].DeepClone(),["runId"]=o22Application["runId"].DeepClone(),["nodeId"]=o22Application["nodeId"].DeepClone(),["nativeContextId"]=o22Application["nativeContextId"].DeepClone(),["inputHash"]=input.OriginalHash,["actionId"]=action,["logicalActorId"]=actor,["logicalGeneration"]=o22Application["logicalGeneration"].DeepClone(),["tick"]=Tick,["actualEnemyHpLoss"]=damage.HpLoss,["hpBefore"]=before,["hpAfter"]=healed.HpAfter,["actualHpGain"]=healed.HpGain,["derivedExcess"]=0,["outcome"]=before==0?"DOWN":damage.HpLoss==0?"NO_HP_DAMAGE":before==maximum?"FULL_HP":"HEALED"};
  }
 }
 public sealed partial class BDirectAttackBasicLoop {
  public const string O22ExecutorVersion="combat.b-native-instant-basic-o22.v1.20261019";
  public BDirectAttackBasicLoopResult ApplyO22(BRouteBinding route,CombatO22ApplicationOriginal original){
   if(route==null||original==null)throw new ArgumentNullException();
   return Commit(new JObject{["kind"]="O22_APPLY",["eventId"]=original.ApplicationId,["route"]=route.Capture(),["original"]=JObject.Parse(original.Capture())});
  }
  void ApplyOriginalO22(JObject command,BDirectAttackBuffSession staged){
   BuffWire.Fields(command,"kind","eventId","route","original");
   // Explicit supported executor boundary. Preserve no-effect/active/projectile
   // profiles rather than granting them support through this new command.
   if(waveSource==null||miraFirst!=null||activeOwner!=null||OutcomeOf(staged)!="RUNNING")throw new InvalidOperationException("O22 instant-basic executor unavailable for this profile");
   var raw=BuffWire.Object(command["route"]);var origin=waveSource.Capture()["origin"];
   var route=BRouteBinding.Read(raw.ToString(),(string)raw["packetHash"],(string)origin["accountId"],waveSource.RunId);
   if((string)raw["phase"]!="ACTIVE"||(string)raw["runStatus"]!="ACTIVE"||(string)raw["currentNode"]?["nodeId"]!=waveSource.NodeId)throw new InvalidOperationException("O22 requires the original active node, not a pending arrival");
   if(route.NativeNode==null||!JToken.DeepEquals(route.NativeNode.Capture(),waveSource.Capture())||binding.Composition.Attack.OriginalAccountId!=route.AccountId)throw new ArgumentException("O22 native node origin mismatch");
   var p=BuffWire.Object(command["original"]);
   var original=CombatO22ApplicationOriginal.Read(p.ToString(),route.AccountId,route.RunId,(string)p["grantEventId"],(string)p["environment"],(string)raw["startSnapshotHash"]);
   if((string)command["eventId"]!=original.ApplicationId)throw new ArgumentException("O22 command identity mismatch");
   // Generation is the actual immutable original node/source incarnation; a
   // different node/input cannot inherit this application by slot or pool index.
   staged.ApplyO22(new JObject{["executorVersion"]=O22ExecutorVersion,["applicationId"]=original.ApplicationId,["definitionPacketHash"]=original.DefinitionPacketHash,["runId"]=route.RunId,["nodeId"]=waveSource.NodeId,["nativeContextId"]=waveSource.NativeContextId,["logicalGeneration"]=(string)waveSource.Capture()["sourceHash"],["inputHash"]=binding.Composition.Attack.OriginalHash});
  }
 }
}
