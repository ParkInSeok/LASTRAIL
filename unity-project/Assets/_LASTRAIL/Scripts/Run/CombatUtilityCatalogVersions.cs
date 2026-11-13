using System;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 // Exact immutable server definitions consumed here, not operating eligibility.
 // v8 hashes come from captureAugmentUtilityDefinitions, exported in
 // QA/OperatorInformation-20261019/utility-definitions-v8-source.json.
 public static class CombatUtilityCatalogVersions {
  public const string Legacy="augment.utility-definitions.v7.20261019";
  public const string Current="augment.utility-definitions.v8.20261020";
  public const string CurrentCatalogHash="5eb2c9d669f7fb43ca5955371ced5d9d6581df1b533ecf35049dfc0d773f1813";
  public static bool Supports(string version)=>version==Legacy||version==Current;
  public static void RequireCurrentDefinition(JObject definition){
   string expected=(string)definition?["id"] switch{
    "O22"=>"2e089220f84434bd20da82d2877fd2cd02ec82a8fd60c1ef27076c4eed20e5c0",
    "O23"=>"0aa08bebed057a3591052882f12a92657fa58873a00d1deedfc47588057c6f25",
    _=>throw new FormatException("Unsupported v8 definition consumer")};
   var body=(JObject)definition.DeepClone();body.Remove("definitionHash");
   if((string)definition["definitionHash"]!=expected||BDirectAttackInput.Digest(body).Substring(7)!=expected)throw new FormatException("Changed v8 definition; explicit consumer review required");
  }
 }
}
