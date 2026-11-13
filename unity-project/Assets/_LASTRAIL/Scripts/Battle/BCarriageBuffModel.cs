using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Owns only fixed departure and operating bonus-source state. HP/CD/effects belong to their existing owners.
 public sealed class BCarriageBuffModel {
  readonly BCarriageBuffDefinitions definitions;readonly JObject departure,initial;
  readonly Dictionary<int,(string body,string actor)> states=new Dictionary<int,(string,string)>();
  readonly Dictionary<string,JObject> entries=new Dictionary<string,JObject>(StringComparer.Ordinal),actions=new Dictionary<string,JObject>(StringComparer.Ordinal);
  readonly JArray journal=new JArray();string mode;
  public long Revision {get;private set;}
  public const int JournalLimit=1024;
  public BCarriageBuffModel(BCarriageBuffDefinitions source,JObject fixedDeparture,JObject originalInitial) {
   definitions=source??throw new ArgumentNullException(nameof(source));ValidateDeparture(fixedDeparture);departure=(JObject)fixedDeparture.DeepClone();
   BuffWire.Fields(originalInitial,"contextId","mode","cars");BuffWire.Text(originalInitial["contextId"]);mode=BuffWire.One(originalInitial["mode"],"EXTERIOR","INTERIOR");
   var rows=BuffWire.Array(originalInitial["cars"],1,5);BuffWire.Need(rows.Count==((JArray)departure["cars"]).Count,"combat_b_initial_invalid");
   foreach(var row in rows){var s=BuffWire.Object(row);BuffWire.Fields(s,"slot","body","actor");int slot=(int)BuffWire.Number(s["slot"],0,4);
    var car=Car(slot);var body=BuffWire.One(s["body"],"NORMAL","BROKEN");var actor=BuffWire.One(s["actor"],"UP","DOWN","ABSENT");Actor(car,actor);
    BuffWire.Need(!states.ContainsKey(slot),"combat_b_initial_invalid");states.Add(slot,(body,actor));}
   initial=(JObject)originalInitial.DeepClone();
  }
  // Only this encapsulated owner can stage its trusted state; persisted/untrusted snapshots still replay fully.
  internal BCarriageBuffModel CloneOwned(){
   var copy=new BCarriageBuffModel(definitions,departure,initial);copy.mode=mode;copy.Revision=Revision;
   foreach(var state in states)copy.states[state.Key]=state.Value;
   foreach(var row in journal){var entry=(JObject)row.DeepClone();copy.journal.Add(entry);copy.entries.Add((string)entry["input"]["eventId"],entry);}
   foreach(var action in actions)copy.actions.Add(action.Key,(JObject)action.Value.DeepClone());return copy;
  }
  internal BCarriageBuffModel RestoreOwned(JObject snapshot)=>Restore(snapshot,definitions);
  internal string DefinitionHash=>definitions.Hash;
  public static void ValidateDeparture(JObject d)=>ValidateDepartureShape(d,"ISOLATED_LOCAL_FIXTURE");
  // Preparation accepts the strict adapter's immutable source. Runtime construction
  // still requires its existing LOCAL contract until full original inputs exist.
  public static JObject ValidatePreparedDeparture(BCombatPhysicalPlacement placement){
   if(placement==null)throw new ArgumentNullException(nameof(placement));
   var d=placement.CaptureDeparture();ValidateDepartureShape(d,"PERSISTED_ACCOUNT_SOURCE");
   BuffWire.Need((string)d["accountId"]==placement.AccountId,"combat_b_departure_account_conflict");
   foreach(JObject car in (JArray)d["cars"]){var fixedCar=placement.RequireCar((int)car["slot"],(string)car["instanceId"]);BuffWire.Need(JToken.DeepEquals(fixedCar.Capture(),car),"combat_b_departure_placement_conflict");}
   return d;
  }
  static void ValidateDepartureShape(JObject d,string sourceStatus){
   BuffWire.Fields(d,"version","accountId","selectionRevision","rankRevision","sourceStatus","cars");BuffWire.Version(d);
   BuffWire.Text(d["accountId"]);BuffWire.Number(d["selectionRevision"],1,BuffWire.Safe-1);BuffWire.Number(d["rankRevision"],1,BuffWire.Safe-1);
   BuffWire.Need((string)d["sourceStatus"]==sourceStatus,"combat_b_source_unavailable");
   var slots=new HashSet<int>();var instances=new HashSet<string>(StringComparer.Ordinal);var operators=new HashSet<string>(StringComparer.Ordinal);var ranks=new Dictionary<string,long>();
   foreach(var row in BuffWire.Array(d["cars"],1,5)){var c=BuffWire.Object(row);BuffWire.Fields(c,"slot","instanceId","carriageId","occupant","type","rank");
    BuffWire.Need(slots.Add((int)BuffWire.Number(c["slot"],0,4))&&instances.Add(BuffWire.Text(c["instanceId"])),"combat_b_departure_duplicate");BuffWire.Text(c["carriageId"]);
    var o=BuffWire.Object(c["occupant"]);var kind=BuffWire.One(o["kind"],"OPERATOR","CARGO");if(kind=="OPERATOR"){BuffWire.Fields(o,"kind","operatorId");BuffWire.Need(operators.Add(BuffWire.Text(o["operatorId"])),"combat_b_departure_duplicate");}else BuffWire.Fields(o,"kind");
    var type=BuffWire.Type(c["type"]);long rank=BuffWire.Number(c["rank"],1,5);BuffWire.Need(!ranks.TryGetValue(type,out var old)||old==rank,"combat_b_shared_rank_conflict");ranks[type]=rank;
   }
  }
  JObject Car(int slot)=>((JArray)departure["cars"]).SingleOrDefault(c=>(long)c["slot"]==slot) as JObject??throw new ArgumentException("combat_b_slot_invalid");
  static void Actor(JObject car,string actor){BuffWire.Need(((string)car["occupant"]["kind"]=="CARGO")== (actor=="ABSENT"),"combat_b_actor_invalid");}
  static JObject Command(JObject input,bool action){
   BuffWire.Text(input?["eventId"]);BuffWire.Number(input["expectedRevision"],0,BuffWire.Safe-1);
   if(action){BuffWire.Fields(input,"eventId","expectedRevision","slot","kind","ability","timing","target","direct","originalEligible");BuffWire.Number(input["slot"],0,4);
    BuffWire.One(input["kind"],"BASIC","ACTIVE","ULTIMATE","SENTRY","COMMANDER","DERIVED","INTERCEPT","HEAL","SHIELD","REPAIR","ITEM","OVERHEAL","EXPIRY_HEAL","RETURN","SUPPORT3");
    BuffWire.One(input["ability"],"BASIC","ACTIVE","ULTIMATE","INTERCEPT","EXTERNAL");BuffWire.One(input["timing"],"TIME_COOLDOWN","INTERCEPTION_PHASE","NONE");
    BuffWire.One(input["target"],"STRUCTURE","ATTACHED","CLIMBING","APPROACH","ROOFTOP","ENEMY_TRAIN_BODY","ENEMY_TRAIN_PART","ENEMY","NONE");BuffWire.Flag(input["direct"]);BuffWire.Flag(input["originalEligible"]);
   }else{var kind=BuffWire.One(input["kind"],"BODY","ACTOR","MODE");if(kind=="MODE"){BuffWire.Fields(input,"eventId","expectedRevision","kind","value");BuffWire.One(input["value"],"EXTERIOR","INTERIOR");}
    else{BuffWire.Fields(input,"eventId","expectedRevision","kind","slot","value");BuffWire.Number(input["slot"],0,4);if(kind=="BODY")BuffWire.One(input["value"],"NORMAL","BROKEN");else BuffWire.One(input["value"],"UP","DOWN","ABSENT");}}
   var c=(JObject)input.DeepClone();c["expectedRevision"]=BuffWire.Number(input["expectedRevision"],0,BuffWire.Safe-1);if(c["slot"]!=null)c["slot"]=BuffWire.Number(c["slot"],0,4);return c;
  }
  bool Replay(JObject entry){var id=(string)entry["input"]["eventId"];if(!entries.TryGetValue(id,out var old))return false;BuffWire.Need(JToken.DeepEquals(old,entry),"combat_b_event_conflict");return true;}
  void Preflight(JObject entry){BuffWire.Need((long)entry["input"]["expectedRevision"]==Revision,"combat_b_state_revision_conflict");BuffWire.Need(journal.Count<JournalLimit,"combat_b_journal_capacity");}
  void Record(JObject entry){entries.Add((string)entry["input"]["eventId"],entry);journal.Add(entry);}
  public void Transition(JObject input){var c=Command(input,false);var e=new JObject{["tag"]="TRANSITION",["input"]=c};if(Replay(e))return;Preflight(e);
   string kind=(string)c["kind"],value=(string)c["value"];if(kind=="MODE")mode=value;else{int slot=(int)c["slot"];var car=Car(slot);var state=states[slot];if(kind=="ACTOR"){Actor(car,value);state.actor=value;}else state.body=value;states[slot]=state;}
   Revision++;Record(e);
  }
  public JObject Accept(JObject input){var a=Command(input,true);var e=new JObject{["tag"]="ACTION",["input"]=a};string id=(string)a["eventId"];if(Replay(e))return Source(id);Preflight(e);
   int slot=(int)a["slot"];var car=Car(slot);var state=states[slot];string kind=(string)a["kind"],ability=(string)a["ability"],timing=(string)a["timing"],target=(string)a["target"],type=(string)car["type"];
   bool simple=new[]{"BASIC","ACTIVE","ULTIMATE","INTERCEPT"}.Contains(kind),support=kind=="HEAL"||kind=="SHIELD";
   BuffWire.Need(simple?ability==kind:support?(ability=="ACTIVE"||ability=="ULTIMATE"):ability=="EXTERNAL","combat_b_ability_mismatch");
   BuffWire.Need(mode=="EXTERIOR","combat_b_interior_unsupported");BuffWire.Need((string)car["occupant"]["kind"]=="OPERATOR"&&state.actor=="UP","combat_b_actor_unavailable");
   BuffWire.Need((timing!="TIME_COOLDOWN"||ability=="ACTIVE")&&(timing!="INTERCEPTION_PHASE"||ability=="INTERCEPT"),"combat_b_timing_mismatch");
   bool original=(bool)a["originalEligible"];BuffWire.Need(kind!="INTERCEPT"||original,"combat_b_interception_ineligible");BuffWire.Need(kind!="BASIC"||target!="NONE","combat_b_target_required");
   bool operating=state.body=="NORMAL"&&original,active=operating&&(bool)a["direct"];long damage=0,amp=0,h=0,i=0;var rank=definitions.Rank(type,(int)car["rank"]);
   if(active&&new[]{"BASIC","ACTIVE","ULTIMATE"}.Contains(kind)){
    if(slot==0&&target=="STRUCTURE")damage+=2000;
    if(type=="BCB01"&&kind=="BASIC")damage+=rank.value;
    if(type=="BCB04"){if(target=="STRUCTURE")damage+=rank.value;else if(target=="ATTACHED"||target=="CLIMBING")damage+=rank.secondary;}
    if(type=="BCB07"&&(target=="ENEMY_TRAIN_BODY"||target=="ENEMY_TRAIN_PART"))damage+=rank.value;
   }
   if(active&&type=="BCB05"&&support)amp=rank.value;
   if(operating&&type=="BCB02"&&ability=="ACTIVE"&&timing=="TIME_COOLDOWN")h=rank.value;
   if(operating&&type=="BCB03"&&kind=="INTERCEPT"&&timing=="INTERCEPTION_PHASE")i=rank.value;
   var source=new JObject{["version"]=BCarriageBuffDefinitions.Version,["sourceId"]=BuffWire.ServerSourceId(BCarriageBuffDefinitions.Version,(string)initial["contextId"],(string)departure["accountId"],(string)car["instanceId"],id),
    ["slot"]=slot,["instanceId"]=car["instanceId"].DeepClone(),["type"]=type,["rank"]=car["rank"].DeepClone(),["operatingRevision"]=Revision,["damageBps"]=damage,["supportBps"]=amp,["skillHasteBps"]=h,["interceptionHasteBps"]=i,["kind"]=kind,["ability"]=ability,["timing"]=timing,["target"]=target};
   BuffWire.Need(((string)source["sourceId"]).Length<=4096);actions.Add(id,new JObject{["input"]=a.DeepClone(),["source"]=source});Record(e);return (JObject)source.DeepClone();
  }
  public JObject Source(string eventId)=>actions.TryGetValue(eventId,out var a)?(JObject)a["source"].DeepClone():throw new ArgumentException("combat_b_action_missing");
  public long Defense(int slot,string target){BuffWire.Need(slot>=0&&slot<=4);BuffWire.One(new JValue(target),"BODY","OPERATOR");var car=Car(slot);var state=states[slot];if(mode!="EXTERIOR"||state.body!="NORMAL"||(string)car["type"]!="BCB06")return 0;
   var rank=definitions.Rank("BCB06",(int)car["rank"]);return target=="BODY"?rank.value:((string)car["occupant"]["kind"]=="OPERATOR"&&state.actor=="UP"?rank.secondary:0);}
  public JObject Capacity()=>new JObject{["limit"]=JournalLimit,["recorded"]=journal.Count,["remaining"]=JournalLimit-journal.Count,["scope"]="BOUNDED_LOCAL_NOT_FULL_RUN"};
  public JObject Snapshot()=>new JObject{["version"]=BCarriageBuffDefinitions.Version,["departure"]=departure.DeepClone(),["initial"]=initial.DeepClone(),["journal"]=journal.DeepClone(),["actions"]=new JArray(journal.Where(e=>(string)e["tag"]=="ACTION").Select(e=>actions[(string)e["input"]["eventId"]].DeepClone()))};
  public static BCarriageBuffModel Restore(JObject snapshot,BCarriageBuffDefinitions definitions){
   BuffWire.Fields(snapshot,"version","departure","initial","journal","actions");BuffWire.Version(snapshot);BuffWire.Array(snapshot["actions"],0,JournalLimit);var rows=BuffWire.Array(snapshot["journal"],0,JournalLimit);
   var model=new BCarriageBuffModel(definitions,BuffWire.Object(snapshot["departure"]),BuffWire.Object(snapshot["initial"]));
   foreach(var row in rows){var e=BuffWire.Object(row);BuffWire.Fields(e,"tag","input");var tag=BuffWire.One(e["tag"],"ACTION","TRANSITION");if(tag=="ACTION")model.Accept(BuffWire.Object(e["input"]));else model.Transition(BuffWire.Object(e["input"]));}
   BuffWire.Need(JToken.DeepEquals(model.Snapshot(),snapshot),"combat_b_snapshot_conflict");return model;
  }
 }
}
