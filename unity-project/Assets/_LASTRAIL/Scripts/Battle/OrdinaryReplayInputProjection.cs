using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 public sealed partial class BasicCombatSimulation {
  // References the existing owned operations; rebuilt by checkpoint replay, never a second saved ledger.
  readonly List<(long Tick,JObject Command)> ordinaryReplayInputs=new List<(long,JObject)>();
  bool ordinaryReplaying;
  void ObserveOrdinaryInput(JObject operation,bool meaningful){
   string kind=(string)operation["kind"];
   if(meaningful&&(ordinaryLiveSources!=null?kind=="ADVANCE"||kind=="PAUSE"||kind=="SPEED"||kind=="THROTTLE"||kind=="ULTIMATE_AUTO":kind!="ADVANCE"))ordinaryReplayInputs.Add((Tick,operation));
  }
  public JObject ReadOrdinaryReplayInputs(){
   if(ordinaryFactory==null)throw new InvalidOperationException("Original ordinary execution owner required.");
   return new JObject{["schema"]=ordinaryLiveSources==null?"unity.ordinary-replay-input-projection.v1.20261028":"unity.ordinary-replay-input-projection.v2.20261106",
    ["accountId"]=ordinaryFactory.Original.AccountId,["runId"]=ordinaryFactory.Original.RunId,
    ["startSnapshotHash"]=ordinaryFactory.Original.StartSnapshotHash,["configurationSha256"]=Digest(ConfigurationVersion),
    ["checkpointSha256"]=Digest(CaptureOrdinary()),["serverAcceptanceVerified"]=false,
    ["inputs"]=new JArray(ordinaryReplayInputs.Select((entry,index)=>new JObject{
     ["sequence"]=index+1,["tick"]=entry.Tick,["command"]=entry.Command.DeepClone()}))};
  }
 }
}
