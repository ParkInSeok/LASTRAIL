using Newtonsoft.Json.Linq;
using static LastRail.Run.RunWaveManifestReader;
namespace LastRail.Run {
 // New immutable policy wrapper; the original reader still owns every original receipt invariant.
 public sealed class BBoardingEventsNodeSnapshot {
  readonly JObject packet;
  public string SourceHash=>(string)packet["sourceHash"];public string OriginalSourceHash=>(string)packet["originalSourceHash"];
  public BScoutGunnerNodeSnapshot OriginalSource {get;}public BBoardingEventsDefinitionSnapshot Definition {get;}
  public bool RuntimeSupported=>false;public bool CanStart=>false;
  BBoardingEventsNodeSnapshot(JObject p,BScoutGunnerNodeSnapshot original){packet=(JObject)p.DeepClone();OriginalSource=original;Definition=BBoardingEventsDefinitionSnapshot.FromValidatedDefinition((JObject)packet["definition"]);}
  public JObject Capture()=>(JObject)packet.DeepClone();
  public static BBoardingEventsNodeSnapshot Read(JObject input,string independentlyExpectedSourceHash,JObject independentlyTrustedOriginal){BBoardingEventsDefinitionSnapshot.Bounded(input);var p=(JObject)input.DeepClone();Fields(p,"version","originalSource","originalSourceHash","definition","definitionHash","definitionAvailable","runtimeSupported","canStart","sourceHash");Need((string)p["version"]==BBoardingEventsDefinitionSnapshot.Version&&Hash(new JValue(independentlyExpectedSourceHash))==(string)p["sourceHash"]&&(string)p["definitionHash"]==BBoardingEventsDefinitionSnapshot.ApprovedDefinitionHash);SelfHash(p,"sourceHash");BBoardingEventsDefinitionSnapshot.ValidateDefinition(Obj(p["definition"]));BBoardingEventsDefinitionSnapshot.Flags(p);Need(independentlyTrustedOriginal!=null&&(string)p["originalSourceHash"]==(string)independentlyTrustedOriginal["sourceHash"]);var original=BScoutGunnerNodeSnapshot.Read(Obj(p["originalSource"]),independentlyTrustedOriginal);return new BBoardingEventsNodeSnapshot(p,original);}
 }
}
