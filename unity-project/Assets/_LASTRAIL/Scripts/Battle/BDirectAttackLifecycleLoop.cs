using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 public sealed partial class BDirectAttackBasicLoop {
  const string LifecycleVersion="unity.combat-b.mira-lifecycle-loop-local.v2";
  BTargetLifecycleJournal lifecycle;
  public static BDirectAttackBasicLoop CreateLifecycleValidation(JObject packet,string trustedHash,BCarriageBuffDefinitions definitions,int capacity){var original=BuffWire.Object(packet["originalSource"]);var journal=BTargetLifecycleJournal.Read(packet,original,trustedHash);BuffWire.Need(journal.Revision==0,"b_lifecycle_initial_cursor");var owner=CreateActiveValidation(original,(string)original["packetHash"],definitions,capacity);owner.lifecycle=journal;owner.activeOwner.BindLifecycle(journal);return owner;}
  // Read-only preparation by the actual lifetime/clock owner. No caller-supplied generation/tick/sequence.
  public JObject PrepareTargetLifetime(string eventId){BCombatFormation.Identity(eventId);BuffWire.Need(lifecycle!=null&&Outcome=="RUNNING","b_lifecycle_unavailable");return new JObject{["version"]=BTargetLifecycleJournal.Version,["kind"]="REPLACE_SAME_ID_FROM_ORIGINAL_PROFILE",["eventId"]=eventId,["rootPacketHash"]=activeSource.Hash,["contextId"]=activeSource.Context,["expectedRevision"]=lifecycle.Revision,["originalTick"]=Tick,["originalSequence"]=checked(sequence+1),["previous"]=session.CurrentActiveTarget,["resetProfileHash"]=lifecycle.ResetProfileHash};}
  public BDirectAttackBasicLoopResult RecordTargetLifetime(JObject originalEvent,JObject packet,string independentlyTrustedHash){
   BuffWire.Need(lifecycle!=null,"b_lifecycle_unbound");var verified=BTargetLifecycleJournal.Read(packet,activeSource.Capture(),independentlyTrustedHash);var input=(JObject)originalEvent.DeepClone();string id=BuffWire.Text(input["eventId"]);var entry=verified.Records.FirstOrDefault(r=>(string)r["input"]["eventId"]==id);BuffWire.Need(entry!=null,"b_lifecycle_missing_receipt");return Commit(new JObject{["kind"]="TARGET_LIFETIME",["eventId"]=id,["input"]=input,["receipt"]=entry["receipt"].DeepClone()},verified);
  }
  public static BDirectAttackBasicLoop RestoreLifecycleValidation(JObject cache,JObject packet,string trustedHash,BCarriageBuffDefinitions definitions,int capacity){
   BuffWire.Fields(cache,"version","bindingHash","capacity","sequence","commands","state","results","sourcePacketHash","active","activeOwner","lifecycle");BuffWire.Need((string)cache["version"]==LifecycleVersion&&BuffWire.Number(cache["capacity"],1,10000)==capacity,"b_lifecycle_cache_version");
   var original=BuffWire.Object(packet["originalSource"]);var trusted=BTargetLifecycleJournal.Read(packet,original,trustedHash);BuffWire.Need(JToken.DeepEquals(cache["lifecycle"],trusted.Capture()),"b_lifecycle_cache_journal");
   var initial=trusted.Prefix(0);var owner=CreateLifecycleValidation(initial.Capture(),(string)initial.Capture()["packetHash"],definitions,capacity);
   foreach(var row in BuffWire.Array(cache["commands"],0,capacity)){var command=BuffWire.Object(row);BTargetLifecycleJournal prefix=null;if((string)command["kind"]=="TARGET_LIFETIME")prefix=trusted.Prefix((int)BuffWire.Number(command["receipt"]["revision"],1,1024));owner.Commit(command,prefix);}
   BuffWire.Need(JToken.DeepEquals(owner.Capture(),cache),"b_lifecycle_cache_replay");return owner;
  }
 }
}
