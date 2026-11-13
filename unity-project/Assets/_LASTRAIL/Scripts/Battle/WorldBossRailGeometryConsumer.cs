using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Owns client composition and subscriptions. Existing command controller and motion owner
 // retain their responsibilities; server snapshots/results are trusted boundary inputs.
 public sealed class WorldBossRailGeometryConsumer:IDisposable {
  readonly JObject profile,definition;readonly string attempt,account;readonly Func<RailConsistMotion> originalFactory;
  WorldBossRailController controller;RailConsistMotion motion;WorldBossRailGeometrySnapshot geometry;JObject bound;WorldBossRailTraversalEvidence traversal;
  Action<JObject> commandSink,sampleSink;Action changedSink;bool disposed,emitting;
  public WorldBossRailSnapshot Rail=>controller.State;
  public WorldBossRailGeometrySnapshot Geometry=>geometry;
  public WorldBossRailTraversalEvidence Traversal=>traversal;
  public JObject Pending=>controller.Pending;
  public string SelectedPath=>motion.SelectedPath;
  public bool AwaitingAuthorityRefresh=>controller.AwaitingAuthorityRefresh;
  public bool NextBranchPermitted=>geometry!=null&&geometry.NextBranchAllowed&&motion.NextBranchPermitted;
  public JObject BoundResult=>bound==null?null:(JObject)bound.DeepClone();
  public JObject ReadSample()=>motion.ReadSample();
  public System.Collections.Generic.IReadOnlyList<RailVehicleOccupancy> Occupancy()=>motion.Occupancy();
  public WorldBossRailGeometryConsumer(WorldBossRailSnapshot initial,string owner,JObject trustedProfile,Func<RailConsistMotion> freshOriginalFactory){
   if(initial==null||trustedProfile==null||freshOriginalFactory==null)throw new ArgumentNullException();
   profile=(JObject)trustedProfile.DeepClone();definition=initial.Definition;attempt=initial.AttemptId;account=owner;originalFactory=freshOriginalFactory;
   var path=new MainlineDetourPath(profile);WorldBossRailGeometrySnapshot.Check(path.GeometryId==new JArray(attempt,path.RoundId).ToString(Newtonsoft.Json.Formatting.None)&&(string)profile["contentVersion"]==(string)definition["contentVersion"]&&((JArray)definition["rounds"]).Any(r=>(string)r["roundId"]==path.RoundId));
   motion=originalFactory();WorldBossRailGeometrySnapshot.Check(motion!=null&&WorldBossRailGeometrySnapshot.Same(motion.OriginalProfile(),profile)&&((JArray)motion.CaptureLocal()["operations"]).Count==0);
   controller=new WorldBossRailController(initial,owner);Attach(controller);
  }
  void Live(){if(disposed)throw new ObjectDisposedException(nameof(WorldBossRailGeometryConsumer));}
  void Mutable(){Live();if(emitting)throw new InvalidOperationException("Motion sample emission is in progress");}
  void Attach(WorldBossRailController c){c.CommandRequested+=ForwardCommand;c.Changed+=ForwardChanged;}
  void Detach(WorldBossRailController c){c.CommandRequested-=ForwardCommand;c.Changed-=ForwardChanged;}
  void ForwardCommand(JObject body){if(!disposed)commandSink?.Invoke((JObject)body.DeepClone());}
  void ForwardChanged(){if(!disposed)changedSink?.Invoke();}
  // One explicit caller binding. Replacement owns only these delegates, no global listener removal.
  public void Bind(Action<JObject> commands,Action<JObject> samples,Action changed){Live();commandSink=commands;sampleSink=samples;changedSink=changed;}
  public void Unbind(){commandSink=null;sampleSink=null;changedSink=null;}
  public void PreviewRoute(string round,string side){Mutable();if(round!=(string)profile["roundId"]){if(!NextBranchPermitted)throw new InvalidOperationException("Tail merge required before next branch");throw new InvalidOperationException("Bind next round geometry before its route input");}controller.PreviewRoute(round,side);}
  public JObject ConfirmPreview(string id){Mutable();return controller.ConfirmPreview(id);}
  public JObject Internal(string round,string action,string id){Mutable();return controller.Internal(round,action,id);}
  public JObject OriginalForReconciliation(){Live();return controller.OriginalForReconciliation();}
  public void SetTargetSpeed(long target){Mutable();motion.SetTargetSpeed(target);}
  public void SetForcedSlowdown(long bps){Mutable();motion.SetForcedSlowdown(bps);}
  public void Advance(long milliseconds){Mutable();if(AwaitingAuthorityRefresh)throw new InvalidOperationException("Authority refresh required before motion advance");emitting=true;try{var samples=motion.Advance(milliseconds);foreach(var sample in samples){if(disposed)break;sampleSink?.Invoke((JObject)sample.DeepClone());}if(!disposed)changedSink?.Invoke();}finally{emitting=false;}}
  WorldBossRailController CloneController()=>WorldBossRailController.RestoreLocal(controller.CaptureLocal(),definition,attempt,account);
  public void ApplyAuthority(JObject railSnapshot,JObject geometrySnapshot,JObject originalBound=null)=>Apply(null,railSnapshot,geometrySnapshot,originalBound);
  public void AcceptReceipt(JObject originalReceipt,JObject railSnapshot,JObject geometrySnapshot,JObject originalBound=null){if(originalReceipt==null)throw new ArgumentNullException(nameof(originalReceipt));Apply(originalReceipt,railSnapshot,geometrySnapshot,originalBound);}
  void Apply(JObject receipt,JObject railSnapshot,JObject geometrySnapshot,JObject originalBound){
   Mutable();var candidateGeometry=new WorldBossRailGeometrySnapshot(geometrySnapshot,profile);var candidate=CloneController();
   try{
    if(receipt!=null&&candidate.Pending!=null)candidate.AcceptOriginalReceipt(receipt,railSnapshot);
    else{if(receipt!=null)WorldBossRailGeometrySnapshot.Check(((JArray)railSnapshot["receipts"]).Any(r=>WorldBossRailGeometrySnapshot.Same(r["receipt"],receipt)));candidate.ApplyAuthoritativeSnapshot(railSnapshot);}
    candidateGeometry.ValidateAssociation(candidate.State,originalBound);
    if(geometry!=null){var old=geometry.Capture();var next=candidateGeometry.Capture();WorldBossRailGeometrySnapshot.Check(candidateGeometry.AtMs>=geometry.AtMs&&((JArray)next["samples"]).Count>=((JArray)old["samples"]).Count);for(int i=0;i<((JArray)old["samples"]).Count;i++)WorldBossRailGeometrySnapshot.Check(WorldBossRailGeometrySnapshot.Same(old["samples"][i],next["samples"][i]));if(geometry.SelectedRoute!="ENTRY")WorldBossRailGeometrySnapshot.Check(geometry.SelectedRoute==candidateGeometry.SelectedRoute);}
    if(bound!=null)WorldBossRailGeometrySnapshot.Check(originalBound!=null&&WorldBossRailGeometrySnapshot.Same(bound["result"],originalBound["result"]));
    var g=candidateGeometry.Capture();long? acceptedAt=null;if(g["selectedAtSequence"].Type!=JTokenType.Null)acceptedAt=(long)g["samples"][(int)g["selectedAtSequence"]]["atMs"];
    var replay=Replay(motion.CaptureLocal(),acceptedAt,candidateGeometry.SelectedRoute);
    CheckLocalHistory(replay.samples,replay.selectedSequence,g);
    bool unchanged=controller.AwaitingAuthorityRefresh==candidate.AwaitingAuthorityRefresh&&controller.CaptureLocal()==candidate.CaptureLocal()&&WorldBossRailGeometrySnapshot.Same(motion.CaptureLocal(),replay.owner.CaptureLocal())&&geometry!=null&&WorldBossRailGeometrySnapshot.Same(geometry.Capture(),g)&&WorldBossRailGeometrySnapshot.Same(bound,originalBound);
    if(unchanged){candidate.Dispose();return;}
    var oldController=controller;Detach(oldController);controller=candidate;motion=replay.owner;geometry=candidateGeometry;bound=originalBound==null?null:(JObject)originalBound.DeepClone();Attach(controller);oldController.Dispose();if(!disposed)changedSink?.Invoke();
   }catch{if(!ReferenceEquals(controller,candidate))candidate.Dispose();throw;}
  }
  // Replay the original local journal and insert the trusted selection at its original timestamp.
  // This also handles a late response without changing target/slowdown commands or issuing a retry.
  (RailConsistMotion owner,JArray samples,long? selectedSequence) Replay(JObject original,long? acceptedAt,string selected){
   RailConsistMotion.RestoreLocal(original,originalFactory);var m=originalFactory();var frames=new JArray(m.ReadSample());long? selectedSequence=null;bool inserted=!acceptedAt.HasValue;
   void Select(string side){bool first=!m.RouteConfirmed;m.SelectPath(side);if(first)selectedSequence=(long)m.ReadSample()["sequence"];}
   void Inject(){if(!inserted&&m.ElapsedMs==acceptedAt.Value){Select(selected);inserted=true;}}
   Inject();foreach(JObject op in (JArray)original["operations"]){Inject();string kind=(string)op["kind"];
    if(kind=="ADVANCE"){long remaining=(long)op["milliseconds"];if(!inserted&&acceptedAt.Value>m.ElapsedMs&&acceptedAt.Value<=m.ElapsedMs+remaining){long before=acceptedAt.Value-m.ElapsedMs;foreach(var s in m.Advance(before))frames.Add(s);remaining-=before;Inject();}foreach(var s in m.Advance(remaining))frames.Add(s);}
    else if(kind=="SELECT")Select((string)op["route"]);else if(kind=="TARGET")m.SetTargetSpeed((long)op["value"]);else if(kind=="SLOWDOWN")m.SetForcedSlowdown((long)op["value"]);else throw new ArgumentException("Unknown original motion command");
   }Inject();if(!inserted){WorldBossRailGeometrySnapshot.Check(acceptedAt.Value>m.ElapsedMs);foreach(var s in m.Advance(acceptedAt.Value-m.ElapsedMs))frames.Add(s);Inject();}return(m,frames,selectedSequence);
  }
  static void CheckLocalHistory(JArray local,long? selection,JObject authority){var supplied=(JArray)authority["samples"];WorldBossRailGeometrySnapshot.Check(local.Count>=supplied.Count);for(int i=0;i<supplied.Count;i++)WorldBossRailGeometrySnapshot.Check(WorldBossRailGeometrySnapshot.Same(local[i],supplied[i]));WorldBossRailGeometrySnapshot.Check(selection.HasValue?authority["selectedAtSequence"].Type==JTokenType.Integer&&(long)authority["selectedAtSequence"]==selection.Value:authority["selectedAtSequence"].Type==JTokenType.Null);}
  public void ApplyTraversalEvidence(JObject trustedProfile,JObject report){Mutable();WorldBossRailGeometrySnapshot.Check(WorldBossRailGeometrySnapshot.Same(profile,trustedProfile));var next=new WorldBossRailTraversalEvidence(report);if(traversal!=null&&WorldBossRailGeometrySnapshot.Same(traversal.Capture(),next.Capture()))return;traversal=next;changedSink?.Invoke();}
  public void RejectOriginal(JObject original,string error,JObject railSnapshot){Mutable();var c=CloneController();try{c.RejectOriginal(original,error,railSnapshot);if(geometry!=null)geometry.ValidateAssociation(c.State,bound);var old=controller;Detach(old);controller=c;Attach(c);old.Dispose();changedSink?.Invoke();}catch{if(!ReferenceEquals(c,controller))c.Dispose();throw;}}
  public string CaptureLocal(){Live();return new JObject{["schema"]="world-boss.rail-geometry-client.v1",["profile"]=profile.DeepClone(),["controller"]=JObject.Parse(controller.CaptureLocal()),["motion"]=motion.CaptureLocal(),["geometry"]=geometry==null?JValue.CreateNull():(JToken)geometry.Capture(),["bound"]=bound==null?JValue.CreateNull():(JToken)BoundResult,["traversal"]=traversal==null?JValue.CreateNull():(JToken)traversal.Capture()}.ToString(Newtonsoft.Json.Formatting.None);}
  public static WorldBossRailGeometryConsumer RestoreLocal(string json,JObject trustedDefinition,string expectedAttempt,string expectedAccount,JObject trustedProfile,Func<RailConsistMotion> factory){
   if(json==null||json.Length>16777216)throw new ArgumentException("Geometry client checkpoint limit");var x=JObject.Parse(json);WorldBossRailSnapshot.Fields(x,"schema","profile","controller","motion","geometry","bound","traversal");WorldBossRailGeometrySnapshot.Check((string)x["schema"]=="world-boss.rail-geometry-client.v1"&&WorldBossRailGeometrySnapshot.Same(x["profile"],trustedProfile));
   var c=WorldBossRailController.RestoreLocal(x["controller"].ToString(Newtonsoft.Json.Formatting.None),trustedDefinition,expectedAttempt,expectedAccount);WorldBossRailGeometryConsumer restored=null;
   try{restored=new WorldBossRailGeometryConsumer(c.State,expectedAccount,trustedProfile,factory);restored.Detach(restored.controller);restored.controller.Dispose();restored.controller=c;restored.Attach(c);restored.motion=RailConsistMotion.RestoreLocal(x["motion"] as JObject,factory);
    if(x["geometry"].Type!=JTokenType.Null){restored.geometry=new WorldBossRailGeometrySnapshot(x["geometry"] as JObject,trustedProfile);restored.bound=x["bound"].Type==JTokenType.Null?null:(JObject)x["bound"].DeepClone();restored.geometry.ValidateAssociation(c.State,restored.bound);var replay=restored.Replay(restored.motion.CaptureLocal(),null,"ENTRY");CheckLocalHistory(replay.samples,replay.selectedSequence,restored.geometry.Capture());}else WorldBossRailGeometrySnapshot.Check(x["bound"].Type==JTokenType.Null);
    if(x["traversal"].Type!=JTokenType.Null)restored.traversal=new WorldBossRailTraversalEvidence(x["traversal"] as JObject);return restored;
   }catch{if(restored!=null)restored.Dispose();else c.Dispose();throw;}
  }
  public void Dispose(){if(disposed)return;disposed=true;Unbind();Detach(controller);controller.Dispose();}
 }
}


