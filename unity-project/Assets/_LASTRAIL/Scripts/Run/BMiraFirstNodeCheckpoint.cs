using System;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 // Only this explicit LOCAL Solo trial. Not CE03, co-op host migration or a server cursor.
 // The whole original node/controller candidate must verify before publishing native poses.
 public static class BMiraFirstNodeCheckpoint {
  public static BNodeSessionController RestoreEquipped(JObject saved,string independentHash,JObject trustedNode,BDirectAttackBasicLoopBinding binding,BBoardingEventsNodeSnapshot node,BWaveBoardingLocalPaths paths,BMiraEquippedTrialContext context,IMiraFirstGeometrySource geometry,int capacity,BTravelRuntimeConfiguration motion){
   if(saved==null||geometry==null||context==null)throw new ArgumentNullException();
   var candidate=BNodeSessionController.Restore(saved,independentHash,trustedNode,
    p=>BDirectAttackBasicLoop.RestoreMiraEquippedTrialRuntime(p,BDirectAttackInput.Digest(p),binding,node,paths,context,geometry,capacity,motion),context.AccountId,context.RunId);
   try{var state=(JObject)candidate.Capture()["runtime"]["miraFirstTrial"]["nativeState"];geometry.RestoreNativeState(state,BDirectAttackInput.Digest(state));return candidate;}
   catch{candidate.Dispose();throw;}
  }
  public static BNodeSessionController Restore(JObject saved,string independentHash,JObject trustedNode,BDirectAttackBasicLoopBinding binding,BBoardingEventsNodeSnapshot node,BWaveBoardingLocalPaths paths,BMiraFirstTrialContext context,IMiraFirstGeometrySource geometry,int capacity,BTravelRuntimeConfiguration motion){
   if(saved==null||geometry==null)throw new ArgumentNullException();
   var candidate=BNodeSessionController.Restore(saved,independentHash,trustedNode,
    p=>BDirectAttackBasicLoop.RestoreMiraFirstTrialRuntime(p,BDirectAttackInput.Digest(p),binding,node,paths,context,geometry,capacity,motion),context.Source.AccountId,context.Source.RunId);
   try{var state=(JObject)candidate.Capture()["runtime"]["miraFirstTrial"]["nativeState"];geometry.RestoreNativeState(state,BDirectAttackInput.Digest(state));return candidate;}
   catch{candidate.Dispose();throw;}
  }
 }
}