using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.RoofBattle {
 // One accepted B node/native state. Candidate replay borrows the original main-thread geometry only as input.
 public sealed class RoofMiraLocalOwner:IDisposable {
  public const string Version="unity.roof-mira-local-checkpoint.v1.20261017";
  readonly RoofMiraLocalSource source;readonly IMiraFirstGeometrySource geometry;readonly string context;readonly Func<JObject> load;readonly Action<JObject,long> save;readonly int capacity,thread;
  readonly List<Subscription> subscriptions=new List<Subscription>();
  RoofMiraLocalSource.State current;JArray journal=new JArray();long revision;bool recovery,busy,publishing,disposed;
  public bool RecoveryRequired=>recovery;
  public RoofMiraLocalOwner(RoofMiraLocalSource source,IMiraFirstGeometrySource geometry,string context,Func<JObject> load,Action<JObject,long> save,int capacity){
   this.source=source??throw new ArgumentNullException(nameof(source));this.geometry=geometry??throw new ArgumentNullException(nameof(geometry));this.load=load??throw new ArgumentNullException(nameof(load));this.save=save??throw new ArgumentNullException(nameof(save));
   if(string.IsNullOrWhiteSpace(context)||capacity<1||capacity>10000)throw new ArgumentException("Explicit bounded Mira LOCAL context.");this.context=context;this.capacity=capacity;thread=Thread.CurrentThread.ManagedThreadId;source.RequireGeometry(geometry);
  }
  void ThreadCheck(){if(disposed)throw new ObjectDisposedException(nameof(RoofMiraLocalOwner));if(Thread.CurrentThread.ManagedThreadId!=thread)throw new InvalidOperationException("Mira owner requires its original Unity thread.");}
  void Writable(){ThreadCheck();if(busy||publishing)throw new InvalidOperationException("No reentrant Mira owner command/binding.");}
  JObject Envelope(RoofMiraLocalSource.State state,JArray facts,long rev){
   var body=new JObject{["version"]=Version,["contract"]=RoofBattleLocalOwner.Contract,["contextId"]=context,["sourceHash"]=source.SourceHash,["bindingHash"]=source.Binding.OriginalHash,["revision"]=rev,["cursor"]=facts.Count,["node"]=state.Capture(),["journal"]=facts.DeepClone()};body["hash"]=BDirectAttackInput.Digest(body);return body;
  }
  public void Open(){
   Writable();RoofMiraLocalSource.State candidate=null;JArray facts;long rev;
   try{
    var cached=load();
    if(cached==null){
     if(current!=null)throw new System.IO.IOException("Original Mira LOCAL checkpoint missing; do not restart context.");
     candidate=source.CreateState(geometry,capacity);facts=Facts(candidate.Capture());rev=1;save(Envelope(candidate,facts,rev),0);
    }else{
     BDirectAttackInput.Fields(cached,"version","contract","contextId","sourceHash","bindingHash","revision","cursor","node","journal","hash");
     var body=(JObject)cached.DeepClone();body.Remove("hash");
     if((string)cached["version"]!=Version||(string)cached["contract"]!=RoofBattleLocalOwner.Contract||(string)cached["contextId"]!=context||(string)cached["sourceHash"]!=source.SourceHash||(string)cached["bindingHash"]!=source.Binding.OriginalHash||BDirectAttackInput.Digest(body)!=(string)cached["hash"])throw new ArgumentException("Original Mira checkpoint context/source/hash.");
     candidate=source.RestoreState((JObject)cached["node"],geometry,capacity);facts=Facts(candidate.Capture());rev=(long)cached["revision"];
     if(rev!=((JArray)cached["node"]["runtime"]["commands"]).Count+1||(long)cached["cursor"]!=facts.Count||!JToken.DeepEquals(cached["journal"],facts))throw new ArgumentException("Original Mira command/journal/cursor replay mismatch.");
     // Original adapter validates all identities/poses in staging before publishing any live transform.
     RoofMiraLocalSource.RestoreGeometry(candidate.Capture(),geometry);
    }
    var old=current;current=candidate;candidate=null;journal=facts;revision=rev;recovery=false;old?.Dispose();
   }catch(Exception original){candidate?.Dispose();Hold(original);throw;}
   Publish("SNAPSHOT",new JArray());
  }
  JArray Facts(JObject node){
   var native=(JObject)node["runtime"];var trial=(JObject)native["miraFirstTrial"];var shots=(JArray)trial["shots"];var flight=(JObject)trial["flight"];
   var facts=new JArray();
   JObject Shot(string id)=>(JObject)shots.Single(s=>(string)s["id"]==id);
   JObject Result(string id)=>(JObject)flight["state"]["opportunities"].Single(o=>(string)o["id"]==id)["branches"][0]["results"][0]["result"];
   void Add(JObject shot,string phase,long tick,long sequence,JObject raw,JObject result=null){
    var ray=(JObject)shot["ray"];JToken point=phase=="FIRE"?ray["originMm"]:shot["contact"]?["pointMm"];
    var row=new JObject{["eventId"]=(string)shot["id"]+":"+phase,["actionId"]=shot["id"].DeepClone(),["phase"]=phase,["contextId"]=context,["sourceHash"]=source.SourceHash,["bindingHash"]=source.Binding.OriginalHash,["cursor"]=facts.Count+1,["tick"]=tick,["sequence"]=sequence,["stepTimeSpanTicks"]=source.Binding.Composition.Attack.StepTimeSpanTicks,["sourceId"]="op_mira",["sourceGeneration"]=shot["sourceGeneration"].DeepClone(),["targetId"]=shot["targetId"].DeepClone(),["targetGeneration"]=shot["targetGeneration"].DeepClone(),["profileHash"]=ray["profileHash"].DeepClone(),["layoutHash"]=ray["launchObservation"]["layoutHash"].DeepClone(),["basisHash"]=ray["launchObservation"]["basisHash"].DeepClone(),["launchObservationRevision"]=ray["launchObservation"]["revision"].DeepClone(),["pointMm"]=point?.DeepClone()??JValue.CreateNull(),["rawNativeCommand"]=raw.DeepClone()};
    if(phase=="FIRE"){row["ray"]=ray.DeepClone();row["attack"]=shot["attack"].DeepClone();}
    else{row["trace"]=shot["contact"]?.DeepClone()??JValue.CreateNull();row["originalResult"]=result?.DeepClone()??JValue.CreateNull();var hp=((JArray)trial["contacts"]).SingleOrDefault(c=>(string)c["eventId"]==(string)shot["id"]+":hp");row["hpReceipt"]=hp?.DeepClone()??JValue.CreateNull();}
    facts.Add(row);
   }
   foreach(JObject command in (JArray)flight["commands"]){
    string kind=(string)command["kind"];
    if(kind=="LAUNCH"&&(bool)command["sourceEligible"]){var p=(JObject)command["packet"];var shot=Shot((string)p["opportunityId"]);Add(shot,"FIRE",(long)p["tick"],(long)p["sequence"],command);}
    else if(kind=="CONTACT"){var p=(JObject)command["packet"];var shot=Shot((string)p["opportunityId"]);var result=Result((string)shot["id"]);Add(shot,(string)result["outcome"]=="HIT"?"HIT":"MISS",(long)result["tick"],(long)result["sequence"],command,result);}
    else if(kind=="STOP"){var shot=Shot((string)command["opportunityId"]);var result=Result((string)shot["id"]);Add(shot,(string)command["reason"]=="WALL"?"BLOCKED":"MISS",(long)result["tick"],(long)result["sequence"],command,result);}
    else if(kind=="END")foreach(JObject shot in shots){var result=Result((string)shot["id"]);if(result!=null&&(string)result["outcome"]=="INTERRUPTED"&&(long)result["sequence"]==(long)command["sequence"])Add(shot,"INTERRUPTED",(long)result["tick"],(long)result["sequence"],command,result);}
   }
   if(facts.Select(f=>(string)f["eventId"]).Distinct(StringComparer.Ordinal).Count()!=facts.Count)throw new ArgumentException("Original Mira fact identity conflict.");return facts;
  }
  public JObject Advance(string id,long elapsed)=>Commit(s=>s.Advance(id,elapsed,geometry));
  public JObject SetPaused(string id,bool paused)=>Commit(s=>s.SetPaused(id,paused));
  JObject Commit(Action<RoofMiraLocalSource.State> apply){
   Writable();if(current==null||recovery)throw new InvalidOperationException("Restore original Mira context first.");busy=true;RoofMiraLocalSource.State candidate=null;JArray added;
   try{
    var before=current.Capture();candidate=source.RestoreState(before,geometry,capacity);apply(candidate);var after=candidate.Capture();
    if(JToken.DeepEquals(before,after))return Snapshot();
    var facts=Facts(after);
    if(facts.Count<journal.Count||!JToken.DeepEquals(new JArray(facts.Take(journal.Count)),journal))throw new ArgumentException("Original Mira event prefix changed.");
    added=new JArray(facts.Skip(journal.Count));
    try{save(Envelope(candidate,facts,checked(revision+1)),revision);}catch(Exception original){Hold(original);throw;}
    var old=current;current=candidate;candidate=null;journal=facts;revision++;old.Dispose();
   }finally{candidate?.Dispose();busy=false;}
   Publish("COMMIT",added);return Snapshot();
  }
  public JObject Snapshot(){ThreadCheck();if(current==null)throw new InvalidOperationException("Open original Mira context first.");return Frame("SNAPSHOT",new JArray());}
  JObject Frame(string kind,JArray events){
   var node=current.Capture();var native=(JObject)node["runtime"];var trial=(JObject)native["miraFirstTrial"];long step=source.Binding.Composition.Attack.StepTimeSpanTicks;
   var flights=new JArray(((JArray)trial["shots"]).Where(s=>(string)s["status"]=="FLYING").Select(s=>new JObject{["id"]=s["id"].DeepClone(),["targetId"]=s["targetId"].DeepClone(),["sourceGeneration"]=s["sourceGeneration"].DeepClone(),["targetGeneration"]=s["targetGeneration"].DeepClone(),["launchTick"]=s["launchTick"].DeepClone(),["positionMm"]=new JArray(MiraJudgementGeometry.Position((JObject)s["ray"],checked((current.Tick-(long)s["launchTick"])*step/10))),["ray"]=s["ray"].DeepClone()}));
   return new JObject{["version"]="unity.roof-mira-frame.v1.20261017",["contract"]=RoofBattleLocalOwner.Contract,["kind"]=kind,["contextId"]=context,["sourceHash"]=source.SourceHash,["bindingHash"]=source.Binding.OriginalHash,["revision"]=revision,["cursor"]=journal.Count,["tick"]=current.Tick,["stepTimeSpanTicks"]=step,["nativePaused"]=current.Paused,["paused"]=recovery||current.Paused,["recoveryRequired"]=recovery,["outcome"]=current.Outcome,["hp"]=new JObject(source.Binding.Composition.Attack.Entities.Keys.Select(id=>new JProperty(id,current.Hp(id)))),["geometry"]=trial["lastObservation"].DeepClone(),["nativePoses"]=trial["nativeState"]["poses"].DeepClone(),["shots"]=trial["shots"].DeepClone(),["flights"]=flights,["events"]=events.DeepClone(),["node"]=node};
  }
  public IDisposable Subscribe(Action<JObject> observer){
   Writable();if(observer==null)throw new ArgumentNullException(nameof(observer));var sub=new Subscription(this,observer);subscriptions.Add(sub);
   try{publishing=true;try{observer(Snapshot());}finally{publishing=false;}return sub;}catch{sub.Dispose();throw;}
  }
  void Hold(Exception original){recovery=true;busy=false;try{Publish("HOLD",new JArray());}catch(Exception notification){throw new AggregateException("Original Mira persistence/recovery failure and presentation failure.",original,notification);}}
  void Publish(string kind,JArray events){if(current==null)return;publishing=true;var failures=new List<Exception>();try{foreach(var sub in subscriptions.ToArray())if(sub.Active)try{sub.Observer(Frame(kind,events));}catch(Exception error){failures.Add(error);}}finally{publishing=false;}if(failures.Count>0)throw new AggregateException("Mira save succeeded but presentation failed.",failures);}
  sealed class Subscription:IDisposable{
   readonly RoofMiraLocalOwner owner;internal readonly Action<JObject> Observer;internal bool Active=true;
   internal Subscription(RoofMiraLocalOwner owner,Action<JObject> observer){this.owner=owner;Observer=observer;}
   public void Dispose(){if(!Active)return;owner.ThreadCheck();Active=false;owner.subscriptions.Remove(this);}
  }
  public void Dispose(){Writable();foreach(var sub in subscriptions.ToArray())sub.Dispose();current?.Dispose();current=null;journal.Clear();disposed=true;}
 }
}