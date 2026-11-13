using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle
{
    // Exact server contract; an asset artifact alone never grants admission.
    public static class Ce03PlayerAssetContract
    {
        public const string ManifestHash="59e07dcc9cf4c2dc0c41b45871fa36740b7996e4a7f26e7247030fea4dbe4e7c";
        public static string[] Validate(JObject binding,JObject ownedSelection)
        {
            Ce03CommandContract.Fields(binding,"version","manifestVersion","manifestSha256","unityVersion","configurationPath","formation","admissionGranted");
            Ce03CommandContract.Equal(binding,"version","ce03.player-asset-binding.v1.20261022");
            Ce03CommandContract.Equal(binding,"manifestVersion","combat-player-assets-v1");
            Ce03CommandContract.Equal(binding,"manifestSha256",ManifestHash);
            Ce03CommandContract.Equal(binding,"unityVersion","6000.3.22f1");
            Ce03CommandContract.Equal(binding,"configurationPath","Assets/_LASTRAIL/Resources/CombatPresentation/approved.asset");
            if(binding["admissionGranted"]?.Type!=JTokenType.Boolean||(bool)binding["admissionGranted"])throw new FormatException("Assets cannot grant admission.");
            var expected=new[]{"op_kyle","op_reina","op_eve","op_sera","op_mira"};
            if(binding["formation"] is not JArray formation||formation.Any(v=>v.Type!=JTokenType.String)||!formation.Values<string>().SequenceEqual(expected))throw new FormatException("Unsupported Player formation.");
            if(ownedSelection?["formation"]?["operators"] is not JArray operators||operators.Count!=5||
                !operators.OrderBy(v=>(int)v["slot"]).Select(v=>(string)v["operatorId"]).SequenceEqual(expected)||
                !operators.Select(v=>(int)v["slot"]).OrderBy(v=>v).SequenceEqual(Enumerable.Range(0,5)))throw new FormatException("Player formation differs from original selection.");
            if((string)ownedSelection["operationId"]!="opn_01_last_departure"||(string)ownedSelection["risk"]!="I")throw new FormatException("Unsupported local Player operation.");
            return expected;
        }
    }
}
