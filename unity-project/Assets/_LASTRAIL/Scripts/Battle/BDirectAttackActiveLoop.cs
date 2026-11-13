using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 public sealed partial class BDirectAttackBasicLoop {
  const string ActiveVersion="unity.combat-b.mira-active-loop-local.v1";
  BActiveProjectileSourcePacket activeSource;BMiraActiveCommandOwner activeOwner;
  public static BDirectAttackBasicLoop CreateActiveValidation(JObject packet,string trustedHash,BCarriageBuffDefinitions definitions,int capacity){var source=new BActiveProjectileSourcePacket(packet,trustedHash,definitions);var owner=new BDirectAttackBasicLoop(source.Binding,capacity){activeSource=source,activeOwner=new BMiraActiveCommandOwner(source)};owner.session.ConfigureActiveTarget(source.Target);return owner;}
  public JObject RequestActive(string id)=>Commit(new JObject{["kind"]="REQUEST_ACTIVE",["eventId"]=id}).ActiveResult;
  public BDirectAttackBasicLoopResult SetActiveAuto(string id,bool value)=>Commit(new JObject{["kind"]="ACTIVE_AUTO",["eventId"]=id,["value"]=value});
  public BDirectAttackBasicLoopResult ApplyOriginalActiveObservation(JObject observation)=>Commit(new JObject{["kind"]="ACTIVE_OBSERVE",["eventId"]=observation["eventId"].DeepClone(),["observation"]=observation.DeepClone()});
  public static BDirectAttackBasicLoop RestoreActiveValidation(JObject cache,JObject packet,string trustedHash,BCarriageBuffDefinitions definitions,int capacity){
   BuffWire.Fields(cache,"version","bindingHash","capacity","sequence","commands","state","results","sourcePacketHash","active","activeOwner");
   BuffWire.Need((string)cache["version"]==ActiveVersion&&BuffWire.Number(cache["capacity"],1,10000)==capacity,"b_active_cache_version");
   var owner=CreateActiveValidation(packet,trustedHash,definitions,capacity);BuffWire.Need((string)cache["sourcePacketHash"]==owner.activeSource.Hash&&(string)cache["bindingHash"]==owner.binding.OriginalHash,"b_active_cache_source");
   foreach(var row in BuffWire.Array(cache["commands"],0,capacity))owner.Commit(BuffWire.Object(row));
   BuffWire.Need(JToken.DeepEquals(owner.Capture(),cache),"b_active_cache_replay");return owner;
  }
 }
}
