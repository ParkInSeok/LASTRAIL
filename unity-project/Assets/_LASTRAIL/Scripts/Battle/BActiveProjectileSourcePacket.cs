using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Exact authenticated LOCAL publication. A trusted hash is supplied independently of this body.
 internal sealed class BActiveProjectileSourcePacket {
  internal const string Version="combat.b-active-projectile-source.v1.20261003",Wire="combat.b-mira-projectile-wire.v1.20261003";
  readonly JObject original;
  internal JObject Capture()=>(JObject)original.DeepClone();
  internal JObject Actor=>(JObject)original["actor"].DeepClone();
  internal JObject Ability=>(JObject)original["ability"].DeepClone();
  internal JObject Target=>(JObject)original["initialTargetIdentity"].DeepClone();
  internal string Hash=>(string)original["packetHash"];
  internal string Context=>(string)original["contextId"];
  internal BDirectAttackBasicLoopBinding Binding {get;}
  internal long Step=>Binding.Composition.Attack.StepTimeSpanTicks;
  internal long Travel=>(long)original["projectile"]["travelTicks"];
  internal long[] ShotOffsets=>((JArray)original["timeline"]["shotTicks"]).Select(t=>(long)t).ToArray();
  internal long RecoveryTicks=>Ticks((long)Ability["recoveryMs"]);
  internal long Ticks(long milliseconds)=>checked((milliseconds*10000+Step-1)/Step);
  internal BActiveProjectileSourcePacket(JObject packet,string trustedHash,BCarriageBuffDefinitions definitions){
   BuffWire.Fields(packet,"version","provenance","accountId","contextId","initialTargetIdentity","originalActiveSource","originalPacketHash","actor","ability","departure","buffInitial","projectile","timing","phase","timeline","initial","wireSchemas","restore","sourceEvidence","supplyComplete","canExecute","startReady","remaining","packetHash");
   var unsigned=(JObject)packet.DeepClone();unsigned.Remove("packetHash");BuffWire.Need(BDirectAttackInput.Digest(unsigned)==trustedHash&&(string)packet["packetHash"]==trustedHash,"b_active_trusted_source_hash");
   BuffWire.Need((string)packet["version"]==Version&&(string)packet["provenance"]=="ISOLATED_LOCAL_FIXTURE"&&!(bool)packet["canExecute"]&&!(bool)packet["startReady"]&&(bool)packet["supplyComplete"],"b_active_source_version");
   var inner=BuffWire.Object(packet["originalActiveSource"]);var innerUnsigned=(JObject)inner.DeepClone();innerUnsigned.Remove("packetHash");BuffWire.Need((string)inner["version"]=="combat.b-active-source.v1.20261003"&&(string)inner["packetHash"]==(string)packet["originalPacketHash"]&&BDirectAttackInput.Digest(innerUnsigned)==(string)inner["packetHash"],"b_active_original_hash");
   foreach(string k in new[]{"actor","departure","timing"})BuffWire.Need(JToken.DeepEquals(inner[k],packet[k]),"b_active_original_binding");
   var initial=BuffWire.Object(packet["buffInitial"]);BuffWire.Fields(initial,"contextId","mode","cars");BuffWire.Need((string)initial["contextId"]==(string)packet["contextId"]&&JToken.DeepEquals(initial["mode"],inner["buffInitial"]["mode"])&&JToken.DeepEquals(initial["cars"],inner["buffInitial"]["cars"]),"b_active_initial_context_binding");
   BuffWire.Need((string)inner["accountId"]==(string)packet["accountId"]&&BDirectAttackInput.Digest(definitions.Capture())=="sha256:"+(string)inner["refitDefinitionHash"],"b_active_definition_binding");
   var actor=BuffWire.Object(packet["actor"]);var ability=BuffWire.Object(packet["ability"]);var projectile=BuffWire.Object(packet["projectile"]);var timing=BuffWire.Object(packet["timing"]);
   BuffWire.Need((string)actor["actorId"]=="op_mira"&&(string)actor["operatorId"]=="op_mira"&&(string)ability["id"]=="op_mira.active"&&(string)ability["kind"]=="ACTIVE"&&(string)ability["timing"]=="TIME_COOLDOWN"&&(string)ability["effect"]=="FIXED_TARGET_BURST"&&(long)ability["cooldownMs"]==10000&&(long)ability["windupMs"]==300&&(long)ability["recoveryMs"]==300&&(long)ability["shots"]==6&&(long)ability["intervalMs"]==200&&(long)ability["damageBps"]==4500,"b_active_ability_unsupported");
   foreach(var property in inner["ability"].Children<JProperty>())BuffWire.Need(JToken.DeepEquals(property.Value,ability[property.Name]),"b_active_ability_original_mismatch");
   BuffWire.Need((string)projectile["id"]=="combat.b.mira.active.projectile.v1"&&(string)projectile["effectId"]=="combat.b.mira.active.impact.v1"&&(string)projectile["version"]==Wire&&(long)projectile["travelMs"]==120&&(long)projectile["coefficientApplications"]==1&&(string)packet["phase"]["attackSnapshot"]=="FIRST_ACTUAL_SHOT_ONCE"&&(string)timing["cooldownAt"]=="CAST_ACCEPTANCE"&&((JArray)timing["generalHaste"]).Count==0&&((JArray)timing["cooldownReduction"]).Count==0&&timing["en08"].Type==JTokenType.Null,"b_active_projectile_policy_unsupported");
   BuffWire.Need(JToken.DeepEquals(ability["projectileId"],projectile["id"])&&JToken.DeepEquals(ability["effectId"],projectile["effectId"])&&BuffWire.Number(timing["firstReadyMs"])==0,"b_active_effect_binding");
   var input=BuffWire.Object(inner["runtimeInput"]);var f=BuffWire.Object(input["formation"]);var formation=new BCombatFormation((string)f["trainId"],BCombatMode.Solo,BCombatBattlefield.ExternalVertical,((JArray)f["slots"]).Select(s=>new BCombatSlot((string)s["carriageId"],s["operatorId"].Type==JTokenType.Null?null:(string)s["operatorId"])).ToArray());
   var attack=BDirectAttackInput.ReadActiveRuntime(input,(string)inner["runtimeInputHash"],formation,BuffWire.Object(inner["ownedProfile"]),(string)inner["ownedProfileHash"],CombatDefinitionIntake.Read(BuffWire.Object(inner["definition"])),BuffWire.Number(timing["stepTimeSpanTicks"],10,10000000));
   BuffWire.Need(attack.OriginalAccountId==(string)packet["accountId"]&&attack.StepTimeSpanTicks==(long)timing["stepTimeSpanTicks"]&&attack.Entities.Values.Count(e=>e.Kind=="OPERATOR")==1,"b_active_actor_profile");
   var target=BuffWire.Object(packet["initialTargetIdentity"]);BuffWire.Fields(target,"targetId","generation","battlefieldGeneration");foreach(var k in new[]{"targetId","generation","battlefieldGeneration"})BuffWire.Text(target[k]);
   int actorSlot=(int)BuffWire.Number(actor["slot"],0,attack.Formation.Slots.Count-1);
   BuffWire.Need(attack.Entities.TryGetValue((string)target["targetId"],out var enemy)&&enemy.Kind=="LOCAL_ENEMY"&&(string)inner["target"]["candidateIds"][0]==enemy.Id&&attack.Formation.Slots[actorSlot].CarriageId==(string)actor["instanceId"]&&attack.Formation.Slots[actorSlot].OperatorId=="op_mira"&&JToken.DeepEquals(actor["stats"]["attack"],attack.Entities["op_mira"].Original["attack"]),"b_active_initial_target");
   var buff=new JObject{["version"]=BDirectAttackBuffBinding.Version,["provenance"]="BOUNDED_LOCAL_NOT_FULL_RUN",["attackInputHash"]=attack.OriginalHash,["definitionHash"]=definitions.Hash,["departure"]=packet["departure"].DeepClone(),["initial"]=packet["buffInitial"].DeepClone(),["targetCategories"]=new JArray(new JObject{["targetId"]=enemy.Id,["category"]="ENEMY"})};
   var composition=new BDirectAttackBuffBinding(attack,definitions,buff,BDirectAttackInput.Digest(buff));var loop=new JObject{["version"]=BDirectAttackBasicLoopBinding.Version,["provenance"]="BOUNDED_LOCAL_NOT_FULL_RUN",["compositionHash"]=composition.OriginalHash,["targets"]=new JArray(new JObject{["sourceId"]="op_mira",["targetId"]=enemy.Id})};Binding=new BDirectAttackBasicLoopBinding(composition,loop,BDirectAttackInput.Digest(loop));original=(JObject)packet.DeepClone();
   BuffWire.Need(Travel==checked((1200000+Step-1)/Step)&&ShotOffsets.SequenceEqual(new long[]{Ticks(300),Ticks(500),Ticks(700),Ticks(900),Ticks(1100),Ticks(1300)})&&(long)original["timeline"]["recoveryUntilTick"]==Ticks(1600),"b_active_timeline_mismatch");
  }
 }
}
