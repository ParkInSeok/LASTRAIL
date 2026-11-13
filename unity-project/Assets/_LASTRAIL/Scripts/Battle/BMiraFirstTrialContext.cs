using System;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Separate approved test policy. Original Server packets and their refusal flags are immutable.
 public sealed class BMiraFirstTrialContext {
  public const string Version="mira.first-trial-context.v1.20261015",CriticalPolicyVersion="mira-first-test-critical-na-20261015";
  readonly JObject original;
  internal BMiraFirstTestSource Source{get;}
  public string Hash{get;}
  public JObject Capture()=>(JObject)original.DeepClone();
  BMiraFirstTrialContext(BMiraFirstTestSource source,JObject policy){
   Source=source;original=new JObject{["version"]=Version,["profile"]=BMiraFirstTestSource.Profile,["accountId"]=source.AccountId,["runId"]=source.RunId,
    ["firstSourceHash"]=source.Hash,["critical"]=policy.DeepClone(),["firstSource"]=source.Capture()};
   Hash=BDirectAttackInput.Digest(original);
  }
  public static BMiraFirstTrialContext Read(BMiraFirstTestSource source,JObject critical){
   if(source==null || critical==null || critical.Count!=3 || (string)critical["profile"]!=BMiraFirstTestSource.Profile
    || (string)critical["status"]!="NOT_APPLICABLE" || (string)critical["policyVersion"]!=CriticalPolicyVersion)
    throw new ArgumentException("Explicit first-test critical NOT_APPLICABLE policy required.");
   return new BMiraFirstTrialContext(source,critical);
  }
  public static BMiraFirstTrialContext Restore(JObject saved,string independentHash,BMiraFirstTestSource source,JObject critical){
   if(saved==null || BDirectAttackInput.Digest(saved)!=independentHash)throw new ArgumentException("First-trial context pin.");
   var restored=Read(source,critical);
   if(!JToken.DeepEquals(saved,restored.Capture()))throw new ArgumentException("First-trial profile/policy/source restore conflict.");
   return restored;
  }
 }
}