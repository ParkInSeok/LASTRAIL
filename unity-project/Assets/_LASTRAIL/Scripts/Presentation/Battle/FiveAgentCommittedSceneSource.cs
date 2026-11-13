using System;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.Battle {
 // Consumes this exact scene driver. No independent simulation, save, polling or admission authority.
 // Basic/CE03 pending attacks are not B-native receipts or approved charge/launch/drone animation phases.
 public sealed class FiveAgentCommittedSceneSource {
  readonly CombatSceneDriver driver;
  public FiveAgentCommittedSceneSource(CombatSceneDriver driver){this.driver=driver??throw new ArgumentNullException(nameof(driver));}
  public FiveAgentCommittedSceneObservation Observe(){
   // The existing owner rejects stale ownership/recovery before returning committed bytes.
   var committed=driver.CaptureCommittedState();
   return new FiveAgentCommittedSceneObservation(committed.Revision,committed.StateJson);
  }
  // Explicit upper-controller call only. No Unity Update or hidden event subscription.
  public FiveAgentCommittedSceneObservation PublishClock(FiveAgentVisualFeed target){
   if(target==null)throw new ArgumentNullException(nameof(target));
   FiveAgentCommittedSceneObservation observation;
   try{observation=Observe();}catch{target.PublishHold(true);throw;}
   target.PublishHold(observation.Paused);target.PublishClock(observation.ClockMilliseconds);
   return observation;
  }
 }
 public sealed class FiveAgentCommittedSceneObservation {
  public const string Version="unity.five-agent-basic-scene-observation.v1.20261017";
  readonly string original;
  public long Revision{get;} public long Tick{get;} public long StepTimeSpanTicks{get;}
  public long ClockMilliseconds{get;} public bool Paused{get;} public string ConfigurationHash{get;}
  internal FiveAgentCommittedSceneObservation(long revision,string originalState){
   if(revision<0||originalState==null||originalState.Length>8*1024*1024)throw new ArgumentException("Committed basic scene observation required.");
   var state=JObject.Parse(originalState);var clock=state["clock"] as JObject;
   if(!((string)state["schema"]??"").StartsWith("ce03-",StringComparison.Ordinal)||clock==null||clock["completed"]?.Type!=JTokenType.Integer||clock["step"]?.Type!=JTokenType.Integer||clock["paused"]?.Type!=JTokenType.Boolean||!(state["actors"] is JArray)||!(state["attacks"] is JArray)||!(state["events"] is JArray)||state["configurationSha256"]?.Type!=JTokenType.String)throw new ArgumentException("Unsupported basic scene source; no fallback to B-native or another owner.");
   Revision=revision;Tick=(long)clock["completed"];StepTimeSpanTicks=(long)clock["step"];Paused=(bool)clock["paused"];ConfigurationHash=(string)state["configurationSha256"];
   if(Tick<0||StepTimeSpanTicks<=0||string.IsNullOrWhiteSpace(ConfigurationHash))throw new ArgumentException("Invalid original basic scene clock/pin.");
   ClockMilliseconds=checked(Tick*StepTimeSpanTicks)/TimeSpan.TicksPerMillisecond;
   original=originalState;
  }
  public JObject Capture()=>new JObject{["version"]=Version,["classification"]="COMMITTED_BASIC_SCENE_NOT_B_NATIVE_OR_PHASE_MAPPING",["revision"]=Revision,["tick"]=Tick,["stepTimeSpanTicks"]=StepTimeSpanTicks,["clockMilliseconds"]=ClockMilliseconds,["paused"]=Paused,["configurationSha256"]=ConfigurationHash,["originalState"]=JObject.Parse(original)};
 }
}
