using System;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 public sealed partial class BDirectAttackBasicLoop {
  public const string MiraFirstVersion="unity.combat-b.mira-first-native-local.v1.20261015";
  public const string MiraEquippedVersion="unity.combat-b.mira-equipped-native-local.v1.20261016";
  BMiraFirstProjectileOwner miraFirst;
  public static BDirectAttackBasicLoop CreateMiraFirstTrialRuntime(BDirectAttackBasicLoopBinding binding,BBoardingEventsNodeSnapshot node,BWaveBoardingLocalPaths paths,BMiraFirstTrialContext context,IMiraFirstGeometrySource geometry,int capacity,BTravelRuntimeConfiguration motion){
   if(geometry==null)throw new ArgumentNullException(nameof(geometry));return CreateMiraFirstCore(binding,node,paths,new BMiraNativeTrialBinding(context),geometry.CaptureAuthoring(),geometry.CaptureNativeState(0,1),capacity,motion);
  }
  static BDirectAttackBasicLoop CreateMiraFirstCore(BDirectAttackBasicLoopBinding binding,BBoardingEventsNodeSnapshot node,BWaveBoardingLocalPaths paths,BMiraNativeTrialBinding context,JObject authoring,JObject state,int capacity,BTravelRuntimeConfiguration motion){
   if(motion==null)throw new ArgumentNullException(nameof(motion));var owner=CreateGunnerInterceptLocalRuntime(binding,node,paths,capacity);
   owner.session.ConfigureActiveTarget(new JObject{["targetId"]=node.OriginalSource.Waves[0].Instances[0].InstanceId,["generation"]="1",["battlefieldGeneration"]=node.OriginalSource.NativeContextId});
   owner.miraFirst=new BMiraFirstProjectileOwner(binding,node,context,authoring,state,capacity,owner.boarding,owner.session);
   owner.travel=new BTravelMotionOwner(binding,node,motion.Capture(),motion.Hash);owner.originalEvents=new BNativeOriginalEventJournal();return owner;
  }
  public static BDirectAttackBasicLoop CreateMiraEquippedTrialRuntime(BDirectAttackBasicLoopBinding binding,BBoardingEventsNodeSnapshot node,BWaveBoardingLocalPaths paths,BMiraEquippedTrialContext context,IMiraFirstGeometrySource geometry,int capacity,BTravelRuntimeConfiguration motion){
   if(geometry==null)throw new ArgumentNullException(nameof(geometry));return CreateMiraFirstCore(binding,node,paths,new BMiraNativeTrialBinding(context),geometry.CaptureAuthoring(),geometry.CaptureNativeState(0,1),capacity,motion);
  }
  public static BDirectAttackBasicLoop RestoreMiraEquippedTrialRuntime(JObject cache,string independentHash,BDirectAttackBasicLoopBinding binding,BBoardingEventsNodeSnapshot node,BWaveBoardingLocalPaths paths,BMiraEquippedTrialContext context,IMiraFirstGeometrySource geometry,int capacity,BTravelRuntimeConfiguration motion){
   BuffWire.Need(cache!=null&&BDirectAttackInput.Digest(cache)==independentHash&&(string)cache["version"]==MiraEquippedVersion,"mira_equipped_cache_pin");
   var saved=BuffWire.Object(cache["miraFirstTrial"]);BuffWire.Need(JToken.DeepEquals(saved["context"],context.Capture())&&(string)saved["contextHash"]==context.Hash&&(string)saved["targetPolicy"]==BMiraFirstProjectileOwner.TargetPolicy,"mira_equipped_cache_policy");
   var authoring=geometry.CaptureAuthoring();BuffWire.Need(JToken.DeepEquals(authoring,saved["authoring"]),"mira_equipped_restore_authoring");
   var owner=CreateMiraFirstCore(binding,node,paths,new BMiraNativeTrialBinding(context),authoring,BuffWire.Object(saved["initialNativeState"]),capacity,motion);
   foreach(var command in BuffWire.Array(cache["commands"],0,capacity))owner.Commit(BuffWire.Object(command));
   BuffWire.Need(JToken.DeepEquals(cache,owner.Capture()),"mira_equipped_cache_replay");return owner;
  }
  public BDirectAttackBasicLoopResult AdvanceMiraFirstFrame(string eventId,long elapsed,IMiraFirstGeometrySource geometry){
   if(miraFirst==null||geometry==null)throw new InvalidOperationException("mira_first_unbound");
   if(results.TryGetValue(eventId,out var old)){BuffWire.Need((string)old.command["kind"]=="MIRA_FRAME"&&(long)old.command["elapsed"]==elapsed,"mira_frame_replay_conflict");return old.result;}
   miraFirst.CheckAuthoring(geometry.CaptureAuthoring());var state=geometry.CaptureNativeState(Outcome=="RUNNING"?session.PreviewMiraTick(elapsed):Tick,miraFirst.Revision+1);
   return Commit(new JObject{["kind"]="MIRA_FRAME",["eventId"]=eventId,["elapsed"]=elapsed,["maximumSteps"]=1,["nativeState"]=state,["nativeStateHash"]=BDirectAttackInput.Digest(state)});
  }
  public BDirectAttackBasicLoopResult ObserveMiraFirstOriginal(string id,JObject observation)=>Commit(new JObject{["kind"]="MIRA_ORIGINAL",["eventId"]=id,["observation"]=observation?.DeepClone()});
  // Pure model replay. Physical publication belongs after the enclosing node/controller verifies.
  public static BDirectAttackBasicLoop RestoreMiraFirstTrialRuntime(JObject cache,string independentHash,BDirectAttackBasicLoopBinding binding,BBoardingEventsNodeSnapshot node,BWaveBoardingLocalPaths paths,BMiraFirstTrialContext context,IMiraFirstGeometrySource geometry,int capacity,BTravelRuntimeConfiguration motion){
   BuffWire.Need(cache!=null&&BDirectAttackInput.Digest(cache)==independentHash&&(string)cache["version"]==MiraFirstVersion,"mira_first_cache_pin");
   var saved=BuffWire.Object(cache["miraFirstTrial"]);BuffWire.Need(JToken.DeepEquals(saved["context"],context.Capture())&&(string)saved["contextHash"]==context.Hash&&(string)saved["targetPolicy"]==BMiraFirstProjectileOwner.TargetPolicy,"mira_first_cache_policy");
   var authoring=geometry.CaptureAuthoring();BuffWire.Need(JToken.DeepEquals(authoring,saved["authoring"]),"mira_first_restore_authoring");
   var owner=CreateMiraFirstCore(binding,node,paths,new BMiraNativeTrialBinding(context),authoring,BuffWire.Object(saved["initialNativeState"]),capacity,motion);
   foreach(var command in BuffWire.Array(cache["commands"],0,capacity))owner.Commit(BuffWire.Object(command));
   BuffWire.Need(JToken.DeepEquals(cache,owner.Capture()),"mira_first_cache_replay");return owner;
  }
 }
}