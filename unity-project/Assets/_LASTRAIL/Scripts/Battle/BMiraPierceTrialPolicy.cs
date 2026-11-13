using System;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 /// <summary>Exact approved Server logical trial policy; never an owned-effect/START authority.</summary>
 public sealed class BMiraPierceTrialPolicy {
  public const string Version="combat.pierce-local-trial-policy.v1.20261016";
  public const string Profile="mira-pierce-comparison-local.v1.20261016";
  public const string ApprovedHash="a0de500d0c455d3332a5307097344712b540964db08a4e20f0db7c5a2167cdf5";
  readonly JObject original;
  public string Hash=>ApprovedHash;
  BMiraPierceTrialPolicy(JObject original){this.original=(JObject)original.DeepClone();}
  public JObject Capture()=>(JObject)original.DeepClone();
  public static BMiraPierceTrialPolicy Read(JObject packet,string independentlyPinnedPolicyHash){
   if(packet==null||independentlyPinnedPolicyHash!=ApprovedHash||(string)packet["policyHash"]!=ApprovedHash)throw new ArgumentException("Approved independent trial policy pin required.");
   var body=(JObject)packet.DeepClone();body.Remove("policyHash");
   // The fixed approved content hash binds every field, including unknown additions,
   // logical-definition pin, four source requirements, null chance and zero draws.
   if(BDirectAttackInput.Digest(body)!="sha256:"+ApprovedHash||(string)packet["version"]!=Version||(string)packet["profile"]!=Profile)
    throw new ArgumentException("Trial policy content/version conflict.");
   return new BMiraPierceTrialPolicy(packet);
  }
 }
}