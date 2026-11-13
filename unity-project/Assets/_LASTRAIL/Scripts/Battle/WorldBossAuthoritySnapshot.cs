using System;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Read-only transport replica. The server journal and HP/shield owners remain authoritative.
 public sealed class WorldBossAuthoritySnapshot {
  readonly JObject state;
  public WorldBossRailSnapshot Rail {get;} public WorldBossRailGeometrySnapshot Geometry {get;}
  public long JournalRevision=>(long)state["journal"]["ownerRevision"];
  public long HealthRevision=>(long)state["health"]["healthRevision"];
  public long ShieldRevision=>(long)state["health"]["shieldRevision"];
  public long Hp=>(long)state["health"]["health"]["hp"];
  public string ProcessingStatus=>(string)state["health"]["processingStatus"];
  public JObject Capture()=>(JObject)state.DeepClone();
  internal static void Need(bool b){if(!b)throw new ArgumentException("world_authority_dto_invalid");}
  internal static bool Same(JToken a,JToken b)=>WorldBossRailGeometrySnapshot.Same(a,b);
  internal static long Nat(JToken t,string key)=>WorldBossRailSnapshot.Nat(t,key);
  static long Positive(JToken t,string key){long value=Nat(t,key);Need(value>0);return value;}
  static void Version(JObject o,string expected)=>Need(WorldBossRailSnapshot.Text(o,"version")==expected);
  static void Provenance(JObject o,string status){Need((string)o["status"]==status&&(string)o["inputProvenance"]=="TRUSTED_LOCAL_TRIAL");}
  static string Hash(JToken value)=>BasicCombatSimulation.Digest(WorldBossRailSnapshot.Canonical(value));
  public WorldBossAuthoritySnapshot(JObject input,JObject originalDefinition,JObject originalProfile,string attempt,string run,string target){
   Need(input!=null&&WorldBossRailSnapshot.Canonical(input).Length<=16777216);state=(JObject)input.DeepClone();WorldBossRailSnapshot.Fields(state,"transportVersion","status","inputProvenance","journal","health");Need((string)state["transportVersion"]==WorldBossAuthorityRequest.Version);Provenance(state,"LOCAL_TEST_PROFILE");
   var j=state["journal"] as JObject;WorldBossRailSnapshot.Fields(j,"version","status","inputProvenance","ownerRevision","frame","rail","geometry","binding","entries");Version(j,WorldBossAuthorityRequest.JournalVersion);Provenance(j,"LOCAL_DURABLE_TRIAL");Positive(j,"ownerRevision");Frame(j["frame"] as JObject);
   Rail=new WorldBossRailSnapshot(j["rail"] as JObject,originalDefinition,attempt);Geometry=new WorldBossRailGeometrySnapshot(j["geometry"] as JObject,originalProfile);Geometry.ValidateAssociation(Rail,j["binding"].Type==JTokenType.Null?null:j["binding"] as JObject);
   Entries(j);
   var h=state["health"] as JObject;WorldBossRailSnapshot.Fields(h,"version","status","inputProvenance","runId","healthRevision","health","shieldRevision","shield","processingStatus","receipt");Version(h,WorldBossAuthorityRequest.HealthVersion);Provenance(h,"LOCAL_TEST_PROFILE");Need(WorldBossRailSnapshot.Text(h,"runId")==run);Positive(h,"healthRevision");Positive(h,"shieldRevision");Health(h["health"] as JObject);Shield(h["shield"] as JObject,target);
   string round=(string)originalProfile["roundId"];var result=((JArray)j["rail"]["results"]).SingleOrDefault(r=>(string)r["roundId"]==round);Need(new[]{"PENDING_SOURCE","PENDING_HEALTH","APPLIED"}.Contains(ProcessingStatus));
   if(h["receipt"].Type==JTokenType.Null){Need(ProcessingStatus==(result==null?"PENDING_SOURCE":"PENDING_HEALTH"));return;}
   Need(ProcessingStatus=="APPLIED"&&result!=null);Receipt(h["receipt"] as JObject,h,j,result,attempt,round,target);
  }
  static void Frame(JObject f){WorldBossRailSnapshot.Fields(f,"availability","observedAtMs","validUntilMs");Need(Nat(f,"validUntilMs")>Nat(f,"observedAtMs"));var a=f["availability"] as JObject;WorldBossRailSnapshot.Fields(a,"trainAvailable","operatorAvailable","trainConnected","operatorConnected");Need(a.Properties().All(p=>p.Value.Type==JTokenType.Boolean));}
  static void Entries(JObject j){var entries=j["entries"] as JArray;Need(entries!=null&&entries.Count>0&&entries.Count==Nat(j,"ownerRevision"));JObject previous=null;foreach(var token in entries){var e=token as JObject;WorldBossRailSnapshot.Fields(e,"version","ownerRevision","kind","key","requestDigest","previousHash","railHash","geometryHash","frameHash","receipt","inheritedReceipts","frame","ack","sample","crossings","attack","binding");Version(e,WorldBossAuthorityRequest.JournalVersion);long revision=Positive(e,"ownerRevision");Need(revision==(previous==null?1:Nat(previous,"ownerRevision")+1));Need(previous==null?e["previousHash"].Type==JTokenType.Null:(string)e["previousHash"]==Hash(previous));Need(new[]{"ATTACH","COMMAND","FRAME"}.Contains((string)e["kind"]));WorldBossRailSnapshot.Text(e,"key",3000);var digest=WorldBossRailSnapshot.Text(e,"requestDigest",1048576);Need(WorldBossRailSnapshot.Canonical(JObject.Parse(digest))==digest);foreach(string key in new[]{"railHash","geometryHash","frameHash"})Need(e[key].Type==JTokenType.String&&Regex.IsMatch((string)e[key],"^[a-f0-9]{64}$"));Frame(e["frame"] as JObject);Need((string)e["frameHash"]==Hash(e["frame"]));var ack=e["ack"] as JObject;WorldBossRailSnapshot.Fields(ack,"version","ownerRevision","sampleSequence");Version(ack,WorldBossAuthorityRequest.JournalVersion);Need(Nat(ack,"ownerRevision")==revision);Nat(ack,"sampleSequence");Need(e["crossings"] is JArray&&e["inheritedReceipts"] is JArray);previous=e;}
   Need(Same(previous["frame"],j["frame"])&&(string)previous["railHash"]==Hash(j["rail"])&&(string)previous["geometryHash"]==Hash(j["geometry"]));
  }
  static void Health(JObject h){WorldBossRailSnapshot.Fields(h,"version","hp","maxHp","anchorHp","anchorMaxHp");Version(h,"health.max-hp-rebase.v1");Need(Nat(h,"hp")<=Positive(h,"maxHp")&&Nat(h,"anchorHp")<=Positive(h,"anchorMaxHp"));}
  static void Shield(JObject s,string target){WorldBossRailSnapshot.Fields(s,"version","targetId","lastAtMs","protectedUntilMs","layer");Version(s,"world-boss.rail-personal-shield.v1.20261002");Need(WorldBossRailSnapshot.Text(s,"targetId",128)==target);Nat(s,"lastAtMs");Nat(s,"protectedUntilMs");if(s["layer"].Type==JTokenType.Null)return;var l=s["layer"] as JObject;WorldBossRailSnapshot.Fields(l,"sourceId","recipientId","amount","expiresAtMs");WorldBossRailSnapshot.Text(l,"sourceId",400);Need(WorldBossRailSnapshot.Text(l,"recipientId",128)==target);Nat(l,"amount");Nat(l,"expiresAtMs");}
  static void Receipt(JObject r,JObject h,JObject j,JToken result,string attempt,string round,string target){
   WorldBossRailSnapshot.Fields(r,"version","resolutionId","attemptId","contentVersion","roundId","targetId","sourceJournalRevision","sourceAttackEventId","sourceIntentEventId","resolvedAtMs","reason","calculation","resolvedDamage","shieldBefore","shieldAfter","absorbed","hpBefore","hpLoss","hpAfter","healthRevisionBefore","healthRevisionAfter","shieldRevisionBefore","shieldRevisionAfter","healthReceipt");Version(r,WorldBossAuthorityRequest.HealthVersion);
   Need((string)r["attemptId"]==attempt&&(string)r["roundId"]==round&&(string)r["targetId"]==target&&(string)r["contentVersion"]==(string)j["rail"]["definition"]["contentVersion"]&&(string)r["resolutionId"]==new JArray(attempt,round,"HEALTH").ToString(Newtonsoft.Json.Formatting.None));
   bool aborted=new[]{"CANCELLED","SKIPPED"}.Contains((string)result["outcome"]);long sourceRevision=Positive(r,"sourceJournalRevision");var source=((JArray)j["entries"]).SingleOrDefault(e=>(long)e["ownerRevision"]==sourceRevision);Need(source!=null&&sourceRevision<=Nat(j,"ownerRevision"));
   Need(aborted?source["sample"] is JObject&&(long)source["sample"]["atMs"]==(long)result["resolvedAt"]:source["binding"] is JObject&&Same(source["binding"]["result"],result));
   JToken attack=aborted?null:j["geometry"]["attackResult"],intent=aborted?null:((JArray)result["failureAttacks"]).FirstOrDefault();Need(Same(r["sourceAttackEventId"],attack==null?JValue.CreateNull():attack["eventId"])&&Same(r["sourceIntentEventId"],intent==null?JValue.CreateNull():intent["eventId"])&&Nat(r,"resolvedAtMs")==Nat(result,"resolvedAt"));
   Need(new[]{"EVASION","HIT","TARGET_DOWN","PROTECTED","CANCELLED","SKIPPED"}.Contains((string)r["reason"]));Need(r["resolvedDamage"].Type==JTokenType.String&&Regex.IsMatch((string)r["resolvedDamage"],"^(0|[1-9][0-9]*)$"));
   Shield(r["shieldBefore"] as JObject,target);Shield(r["shieldAfter"] as JObject,target);Need(Same(r["shieldAfter"],h["shield"]));Nat(r,"absorbed");long before=Nat(r,"hpBefore"),loss=Nat(r,"hpLoss"),after=Nat(r,"hpAfter");Need(loss<=before&&before-loss==after&&after==Nat(h["health"],"hp"));
   Need(Positive(r,"healthRevisionAfter")==Nat(h,"healthRevision")&&Positive(r,"shieldRevisionAfter")==Nat(h,"shieldRevision")&&Positive(r,"shieldRevisionBefore")+1==Nat(r,"shieldRevisionAfter"));Positive(r,"healthRevisionBefore");
   if(r["healthReceipt"].Type==JTokenType.Null)Need(loss==0&&Nat(r,"healthRevisionBefore")==Nat(r,"healthRevisionAfter"));
   else{var hr=r["healthReceipt"] as JObject;WorldBossRailSnapshot.Fields(hr,"version","eventId","targetId","change","before","after","revision");Version(hr,"combat.health-receipt.v2");WorldBossRailSnapshot.Text(hr,"eventId",400);Need((string)hr["targetId"]==target&&Nat(hr,"revision")==Nat(r,"healthRevisionAfter")&&Nat(r,"healthRevisionBefore")+1==Nat(hr,"revision"));Health(hr["before"] as JObject);Health(hr["after"] as JObject);var change=hr["change"] as JObject;WorldBossRailSnapshot.Fields(change,"kind","reason","hp");Need((string)change["kind"]=="ACTUAL_HP"&&(string)change["reason"]=="DAMAGE"&&Nat(change,"hp")==after&&Nat(hr["before"],"hp")==before&&Same(hr["after"],h["health"]));}
  }
 }
}
