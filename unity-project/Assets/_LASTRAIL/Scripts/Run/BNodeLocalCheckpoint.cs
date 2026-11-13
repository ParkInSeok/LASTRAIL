using System;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 public sealed partial class BNodeSessionController {
  public const string CheckpointVersion="unity.b-node-local-checkpoint.v1.20261019";
  Func<JObject> checkpointLoad;Action<JObject,long> checkpointSave;Func<JObject,BDirectAttackBasicLoop> checkpointRestore;
  string checkpointContext;long checkpointRevision;bool checkpointUncertain,checkpointBusy;
  public bool CheckpointRecoveryRequired=>checkpointUncertain;
  public void ConfigureLocalCheckpoint(string context,Func<JObject> load,Action<JObject,long> save,Func<JObject,BDirectAttackBasicLoop> restore){
   Alive();RequireNativeUnchanged();if(checkpointSave!=null||string.IsNullOrWhiteSpace(context)||load==null||save==null||restore==null)throw new ArgumentException("One original node checkpoint binding required");
   checkpointContext=context;checkpointLoad=load;checkpointSave=save;checkpointRestore=restore;checkpointUncertain=true;checkpointBusy=true;long stamp=generation;
   // First write is also uncertain until acknowledged; an ACK loss must restore
   // the same file, never recreate an empty simulation or issue a fresh action.
   try{var saved=load();RequireCheckpointLifetime(stamp);
    if(saved==null){save(Checkpoint(Capture(),1),0);RequireCheckpointLifetime(stamp);checkpointRevision=1;checkpointUncertain=false;}
    else RestoreCheckpointEnvelope(saved,stamp);
   }finally{checkpointBusy=false;}
  }
  JObject Checkpoint(JObject state,long rev){var p=new JObject{["version"]=CheckpointVersion,["contextId"]=checkpointContext,["revision"]=rev,["node"]=state};p["hash"]=BDirectAttackInput.Digest(p);return p;}
  BNodeSessionController StageCheckpoint(JObject state)=>Restore(state,BDirectAttackInput.Digest(state),BRouteBinding.Trust(node.Snapshot.Capture()),checkpointRestore,account,run);
  void RequireCheckpointLifetime(long stamp){Alive();if(generation!=stamp)throw new InvalidOperationException("Node owner changed during checkpoint callback");}
  void AdoptCheckpoint(BNodeSessionController candidate){
   var old=observation;runtime=candidate.runtime;observation=candidate.observation;destination=candidate.destination;revision=candidate.revision;generation=checked(generation+1);
   candidate.runtime=null;candidate.observation=null;old?.Dispose();
  }
  public void RecoverLocalCheckpoint(){
   Alive();if(checkpointSave==null||checkpointBusy)throw new InvalidOperationException("Original checkpoint unavailable");checkpointUncertain=true;checkpointBusy=true;
   try{long stamp=generation;var p=checkpointLoad()??throw new System.IO.IOException("Original checkpoint missing");RequireCheckpointLifetime(stamp);RestoreCheckpointEnvelope(p,stamp);
   }finally{checkpointBusy=false;}Notify();
  }
  void RestoreCheckpointEnvelope(JObject p,long stamp){
    BDirectAttackInput.Fields(p,"version","contextId","revision","node","hash");var body=(JObject)p.DeepClone();body.Remove("hash");
    if((string)p["version"]!=CheckpointVersion||(string)p["contextId"]!=checkpointContext||BDirectAttackInput.Digest(body)!=(string)p["hash"])throw new ArgumentException("Node checkpoint identity mismatch");
    long rev=BDirectAttackInput.Nat(p,"revision",1);if(rev<checkpointRevision)throw new ArgumentException("Node checkpoint revision regressed");
    using(var candidate=StageCheckpoint((JObject)p["node"])){RequireCheckpointLifetime(stamp);AdoptCheckpoint(candidate);}checkpointRevision=rev;checkpointUncertain=false;
  }
  void ExecutePersisted(string eventId,Action<BDirectAttackBasicLoop> execute){
   checkpointBusy=true;bool changedState=false;long stamp=generation;
   try{var before=Capture();using(var candidate=StageCheckpoint(before)){RequireCheckpointLifetime(stamp);
    candidate.ExecuteOriginal(eventId,candidate.revision,execute);var next=candidate.Capture();if(JToken.DeepEquals(before,next))return;
    long rev=checked(checkpointRevision+1);checkpointUncertain=true;checkpointSave(Checkpoint(next,rev),checkpointRevision);RequireCheckpointLifetime(stamp);
    AdoptCheckpoint(candidate);checkpointRevision=rev;checkpointUncertain=false;changedState=true;
   }}finally{checkpointBusy=false;}if(changedState)Notify();
  }
 }
}
