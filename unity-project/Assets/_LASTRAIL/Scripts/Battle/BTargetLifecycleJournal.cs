using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Immutable verified journal. HP, clock, command execution and lifetime publication remain in the native owners.
 public sealed class BTargetLifecycleJournal {
  public const string Version="combat.b-target-lifecycle-source.v1.20261003", Wire="combat.b-mira-lifecycle-wire.v2.20261003";
  readonly JObject packet,initial;readonly string initialHash;
  BTargetLifecycleJournal(JObject value,JObject target,string hash){packet=(JObject)value.DeepClone();initial=(JObject)target.DeepClone();initialHash=hash;}
  public JObject Capture()=>(JObject)packet.DeepClone();
  public JObject CurrentTarget=>(JObject)packet["currentTarget"].DeepClone();
  public long Revision=>(long)packet["revision"];
  public string ResetProfileHash=>(string)packet["resetProfileHash"];
  internal JArray Records=>(JArray)packet["snapshot"]["receipts"].DeepClone();
  internal BTargetLifecycleJournal Prefix(int count){
   BuffWire.Need(count>=0&&count<=Revision,"b_lifecycle_prefix");var p=Capture();var rows=new JArray(Records.Take(count));var target=count==0?initial:(JObject)rows[count-1]["receipt"]["current"];string hash=count==0?initialHash:(string)rows[count-1]["receipt"]["receiptHash"];
   p["revision"]=count;p["currentTarget"]=target.DeepClone();p["journalHash"]=hash;p["snapshot"]["revision"]=count;p["snapshot"]["currentTarget"]=target.DeepClone();p["snapshot"]["journalHash"]=hash;p["snapshot"]["receipts"]=rows;p.Remove("packetHash");p["packetHash"]=Digest(p);return new BTargetLifecycleJournal(p,initial,initialHash);
  }
  static void Need(bool value)=>BuffWire.Need(value,"b_lifecycle_verification");
  static string Digest(JObject value)=>BDirectAttackInput.Digest(value);
  static void Identity(JObject value){BuffWire.Fields(value,"targetId","generation","battlefieldGeneration");foreach(var k in new[]{"targetId","generation","battlefieldGeneration"})BuffWire.Text(value[k]);}
  public static BTargetLifecycleJournal Read(JObject value,JObject trustedOriginal,string independentlyTrustedHash){
   var p=(JObject)value.DeepClone();var unsigned=(JObject)p.DeepClone();unsigned.Remove("packetHash");Need(Digest(unsigned)==independentlyTrustedHash&&(string)p["packetHash"]==independentlyTrustedHash);
   BuffWire.Fields(p,"version","rootPacketHash","contextId","accountId","originalSource","revision","currentTarget","journalHash","snapshot","resetProfile","resetProfileHash","runtimeReset","wireVersion","wireSchemas","ownership","policy","canExecute","startReady","packetHash");
   var original=(JObject)trustedOriginal.DeepClone();var root=(JObject)original.DeepClone();root.Remove("packetHash");Need(Digest(root)==(string)original["packetHash"]&&JToken.DeepEquals(original,p["originalSource"]));
   Need((string)p["version"]==Version&&(string)p["wireVersion"]==Wire&&(string)p["rootPacketHash"]==(string)original["packetHash"]&&(string)p["contextId"]==(string)original["contextId"]&&(string)p["accountId"]==(string)original["accountId"]&&!BuffWire.Flag(p["runtimeReset"])&&!BuffWire.Flag(p["canExecute"])&&!BuffWire.Flag(p["startReady"]));
   var target=BuffWire.Object(original["initialTargetIdentity"]);Identity(target);var active=original["originalActiveSource"];var entity=((JArray)active["runtimeInput"]["entities"]).Single(e=>(string)e["id"]==(string)target["targetId"]&&(string)e["kind"]=="LOCAL_ENEMY");
   var profile=new JObject{["definitionVersion"]=active["definition"]["definitionVersion"].DeepClone(),["runtimeInputHash"]=active["runtimeInputHash"].DeepClone(),["entity"]=entity.DeepClone(),["initialShields"]=active["runtimeInput"]["initialShields"].DeepClone(),["statusPolicy"]="PRISTINE_ORIGINAL_NO_PREVIOUS_TARGET_IMMUNITY_OR_STATUS_COPY"};
   Need(JToken.DeepEquals(profile,p["resetProfile"])&&Digest(profile)==(string)p["resetProfileHash"]);
   string first=Digest(new JObject{["version"]=Version,["rootPacketHash"]=original["packetHash"].DeepClone(),["contextId"]=original["contextId"].DeepClone(),["initialTargetIdentity"]=target.DeepClone(),["resetProfileHash"]=p["resetProfileHash"].DeepClone()}),previousHash=first;
   var snapshot=BuffWire.Object(p["snapshot"]);BuffWire.Fields(snapshot,"version","rootPacketHash","contextId","revision","currentTarget","journalHash","receipts");var records=BuffWire.Array(snapshot["receipts"],0,1024);long revision=0,lastTick=-1,lastSequence=0;var ids=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
   foreach(var row in records){var entry=BuffWire.Object(row);BuffWire.Fields(entry,"input","receipt");var input=BuffWire.Object(entry["input"]);BuffWire.Fields(input,"version","kind","eventId","rootPacketHash","contextId","expectedRevision","originalTick","originalSequence","previous","resetProfileHash");
    var receipt=BuffWire.Object(entry["receipt"]);BuffWire.Fields(receipt,"version","eventId","rootPacketHash","contextId","revision","originalTick","originalSequence","previous","current","resetProfileHash","previousJournalHash","receiptHash","runtimeReset");
    string id=BuffWire.Text(input["eventId"]);Need(id.Length<=128&&ids.Add(id));long tick=BuffWire.Number(input["originalTick"]),seq=BuffWire.Number(input["originalSequence"],1);Need(tick>lastTick||tick==lastTick&&seq>lastSequence);
    Need((string)input["version"]==Version&&(string)input["kind"]=="REPLACE_SAME_ID_FROM_ORIGINAL_PROFILE"&&(string)input["rootPacketHash"]==(string)p["rootPacketHash"]&&(string)input["contextId"]==(string)p["contextId"]&&BuffWire.Number(input["expectedRevision"])==revision&&(string)input["resetProfileHash"]==(string)p["resetProfileHash"]&&JToken.DeepEquals(input["previous"],target));
    revision++;var current=(JObject)target.DeepClone();current["generation"]="LOCAL_LIFECYCLE:"+Digest(new JObject{["rootPacketHash"]=p["rootPacketHash"].DeepClone(),["contextId"]=p["contextId"].DeepClone(),["eventId"]=id,["revision"]=revision}).Substring(7);
    var body=new JObject{["version"]=Version,["eventId"]=id,["rootPacketHash"]=p["rootPacketHash"].DeepClone(),["contextId"]=p["contextId"].DeepClone(),["revision"]=revision,["originalTick"]=tick,["originalSequence"]=seq,["previous"]=target.DeepClone(),["current"]=current,["resetProfileHash"]=p["resetProfileHash"].DeepClone(),["previousJournalHash"]=previousHash,["runtimeReset"]=false};
    previousHash=Digest(new JObject{["input"]=input.DeepClone(),["receipt"]=body.DeepClone()});body["receiptHash"]=previousHash;Need(JToken.DeepEquals(body,receipt));target=current;lastTick=tick;lastSequence=seq;
   }
   var expected=new JObject{["version"]=Version,["rootPacketHash"]=p["rootPacketHash"].DeepClone(),["contextId"]=p["contextId"].DeepClone(),["revision"]=revision,["currentTarget"]=target.DeepClone(),["journalHash"]=previousHash,["receipts"]=records.DeepClone()};
   Need(JToken.DeepEquals(expected,snapshot)&&BuffWire.Number(p["revision"])==revision&&JToken.DeepEquals(p["currentTarget"],target)&&(string)p["journalHash"]==previousHash);
   return new BTargetLifecycleJournal(p,BuffWire.Object(original["initialTargetIdentity"]),first);
  }
  internal JObject CommandLock(string id,long tick,long sequence){
   var target=(JObject)initial.DeepClone();long revision=0;string hash=initialHash;
   foreach(var row in Records){var r=row["receipt"];long t=(long)r["originalTick"],s=(long)r["originalSequence"];if(t>tick||t==tick&&s>sequence)break;Need(t!=tick||s!=sequence);target=(JObject)r["current"].DeepClone();revision=(long)r["revision"];hash=(string)r["receiptHash"];}
   return new JObject{["version"]=Wire,["rootPacketHash"]=packet["rootPacketHash"].DeepClone(),["contextId"]=packet["contextId"].DeepClone(),["commandId"]=id,["acceptedTick"]=tick,["acceptedSequence"]=sequence,["target"]=target,["lifecycleRevision"]=revision,["receiptHash"]=hash};
  }
 }
}
