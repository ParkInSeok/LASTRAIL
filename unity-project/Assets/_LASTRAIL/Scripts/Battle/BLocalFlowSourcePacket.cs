using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Verified released LOCAL wire packet. The trusted hash comes from the authenticated supplier owner,
 // independently of this body; producer provenance is not established by a self-declared digest.
 public sealed class BLocalFlowSourcePacket {
  public const string Version="combat.b-local-flow-source.v1.20261003",Path="/_local/combat-b/source";
  readonly JObject original;public string OriginalHash {get;}public BDirectAttackInput Attack {get;}public BDirectAttackBasicLoopBinding Binding {get;}public bool StartReady=>false;
  public JObject Capture()=>(JObject)original.DeepClone();
  static void Need(bool ok,string code="b_local_source_invalid"){if(!ok)throw new ArgumentException(code);}
  public BLocalFlowSourcePacket(BLocalFlowCandidate candidate,JObject packet,string independentlyTrustedPacketHash){
   if(candidate==null||packet==null)throw new ArgumentNullException("local_source");
   BuffWire.Fields(packet,"version","provenance","accountId","selectionRevision","refitRevision","rankRevision","sourceHash","definitionHash","departure","ownedProfile","ownedProfileHash","definition","attack","attackHash","buffBinding","buffBindingHash","routes","loopBinding","loopBindingHash","targetCategories","contextId","startReady","packetHash");
   var unsigned=(JObject)packet.DeepClone();unsigned.Remove("packetHash");
   Need(independentlyTrustedPacketHash!=null&&BDirectAttackInput.Digest(unsigned)==independentlyTrustedPacketHash&&(string)packet["packetHash"]==independentlyTrustedPacketHash,"b_local_source_trusted_hash");
   Need((string)packet["version"]==Version&&(string)packet["provenance"]=="ISOLATED_LOCAL_FIXTURE"&&packet["startReady"]?.Type==JTokenType.Boolean&&!(bool)packet["startReady"]);
   var frozen=candidate.Capture();var current=frozen["refitRead"];
   Need(BuffWire.Text(packet["accountId"])==candidate.AccountId&&BuffWire.Number(packet["selectionRevision"],1)==(long)current["selectionRevision"]&&BuffWire.Number(packet["refitRevision"],1)==(long)current["revision"]&&BuffWire.Number(packet["rankRevision"],1)==(long)current["rankSource"]["revision"],"b_local_source_current_revision");
   Need(BRefitWire.Hash(packet["sourceHash"])==(string)current["sourceHash"]&&BRefitWire.Hash(packet["definitionHash"])==(string)current["definitionHash"]&&JToken.DeepEquals(packet["departure"],frozen["receipt"]["refit"]),"b_local_source_current_departure");
   var profile=BuffWire.Object(packet["ownedProfile"]);Need((string)profile["accountId"]==candidate.AccountId&&BDirectAttackInput.Digest(profile)==(string)packet["ownedProfileHash"],"b_local_source_owned_profile");
   var definition=CombatDefinitionIntake.Read(BuffWire.Object(packet["definition"]));var attack=BuffWire.Object(packet["attack"]);Need(BDirectAttackInput.Digest(attack)==(string)packet["attackHash"]&&(string)attack["version"]==BDirectAttackInput.EveVersion,"b_local_source_attack_hash");
   var f=BuffWire.Object(attack["formation"]);BuffWire.Fields(f,"version","trainId","mode","battlefield","internalNodeId","slots");Need((string)f["version"]==BCombatFormation.LocalVersion&&(string)f["mode"]=="Solo"&&(string)f["battlefield"]=="ExternalVertical"&&f["internalNodeId"]?.Type==JTokenType.Null&&(string)f["trainId"]==(string)profile["selectedTrain"]["trainId"]);
   var slots=BuffWire.Array(f["slots"],1,5).Select(t=>{var row=BuffWire.Object(t);BuffWire.Fields(row,"carriageId","operatorId");Need(row["operatorId"]?.Type==JTokenType.Null||row["operatorId"]?.Type==JTokenType.String);return new BCombatSlot(BuffWire.Text(row["carriageId"]),row["operatorId"].Type==JTokenType.Null?null:BuffWire.Text(row["operatorId"]));}).ToArray();
   var formation=new BCombatFormation(BuffWire.Text(f["trainId"]),BCombatMode.Solo,BCombatBattlefield.ExternalVertical,slots);Attack=new BDirectAttackInput(attack,(string)packet["attackHash"],formation,profile,(string)packet["ownedProfileHash"],definition);
   var actors=formation.Slots.Where(slot=>!slot.IsCargo).ToArray();var routes=BuffWire.Array(packet["routes"],1,1);var categories=BuffWire.Array(packet["targetCategories"],1,1);
   BuffWire.Fields(BuffWire.Object(routes[0]),"sourceId","targetId");BuffWire.Fields(BuffWire.Object(categories[0]),"targetId","category");
   Need(actors.Length==1&&actors[0].OperatorId=="op_eve"&&(string)routes[0]["sourceId"]=="op_eve"&&(string)routes[0]["targetId"]==(string)categories[0]["targetId"]&&(string)categories[0]["category"]=="ENEMY"&&((JArray)attack["entities"]).Any(e=>(string)e["id"]==(string)routes[0]["targetId"]&&(string)e["kind"]=="LOCAL_ENEMY"),"b_local_source_action_scope");
   var buff=BuffWire.Object(packet["buffBinding"]);var loop=BuffWire.Object(packet["loopBinding"]);
   Need(BDirectAttackInput.Digest(buff)==(string)packet["buffBindingHash"]&&BDirectAttackInput.Digest(loop)==(string)packet["loopBindingHash"],"b_local_source_binding_hash");
   BuffWire.Fields(buff,"version","provenance","attackInputHash","definitionHash","departure","initial","targetCategories");BuffWire.Fields(loop,"version","provenance","compositionHash","targets");
   Need((string)buff["definitionHash"]=="sha256:"+(string)packet["definitionHash"]&&(string)buff["attackInputHash"]==Attack.OriginalHash&&JToken.DeepEquals(buff["departure"],packet["departure"])&&JToken.DeepEquals(buff["targetCategories"],packet["targetCategories"])&&JToken.DeepEquals(loop["targets"],packet["routes"])&&(string)loop["compositionHash"]==(string)packet["buffBindingHash"]&&BuffWire.Text(packet["contextId"])==(string)buff["initial"]["contextId"],"b_local_source_binding_identity");
   var context=new JObject{["version"]=BLocalFlowCandidate.Version,["candidateHash"]=candidate.OriginalHash,["attackInputHash"]=Attack.OriginalHash,["initial"]=buff["initial"].DeepClone(),["targetCategories"]=packet["targetCategories"].DeepClone(),["targets"]=packet["routes"].DeepClone()};
   Binding=candidate.Bind(Attack,context,BDirectAttackInput.Digest(context));
   // The source wire uses sha256: for object references; existing buff definitions use bare64 hex.
   // Preserve original wire hashes; compare the explicitly normalized semantic binding separately.
   var normalizedBuff=(JObject)buff.DeepClone();normalizedBuff["definitionHash"]=packet["definitionHash"].DeepClone();Need(JToken.DeepEquals(normalizedBuff,Binding.Composition.Capture()),"b_local_source_buff_normalization");
   var normalizedLoop=(JObject)loop.DeepClone();normalizedLoop["compositionHash"]=Binding.Composition.OriginalHash;Need(JToken.DeepEquals(normalizedLoop,Binding.Capture()),"b_local_source_loop_normalization");
   original=(JObject)packet.DeepClone();OriginalHash=independentlyTrustedPacketHash;
  }
  public BDirectAttackBasicLoop CreateLoop(int boundedCapacity)=>new BDirectAttackBasicLoop(Binding,boundedCapacity);
 }
}