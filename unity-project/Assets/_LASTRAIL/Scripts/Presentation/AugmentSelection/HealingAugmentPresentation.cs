using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Presentation.AugmentSelection
{
    // Presentation of an already pinned definition, never eligibility or ownership.
    public static class HealingAugmentPresentation
    {
        static JObject copy;
        static decimal Number(JToken root,string path){var t=root?.SelectToken(path);if(t?.Type!=JTokenType.Integer||(decimal)t<0)throw new ArgumentException("Pinned numeric definition missing: "+path);return (decimal)t;}
        static string N(decimal value)=>value.ToString("0.##",CultureInfo.InvariantCulture);
        static string Fill(string template,Dictionary<string,decimal> values){foreach(var v in values)template=template.Replace("{"+v.Key+"}",N(v.Value));if(template.Contains("{"))throw new ArgumentException("Unbound copy value");return template;}
        public static AugmentCardPresentation FromPinned(string version,JObject definition,JObject composition)
        {
            // Historical/unknown definitions remain with their original provider.
            if(!LastRail.Run.CombatUtilityCatalogVersions.Supports(version))return null;
            string id=(string)definition?["id"];if(id!="O22"&&id!="O23")return null;
            if(version==LastRail.Run.CombatUtilityCatalogVersions.Current)LastRail.Run.CombatUtilityCatalogVersions.RequireCurrentDefinition(definition);
            var rules=definition["rules"]??throw new ArgumentException("Pinned rules missing");
            var values=new Dictionary<string,decimal>();
            if(id=="O22"){
                if((string)rules["eligible"]!="ALL_CONFIRMED_ENEMY_HP_DAMAGE_ATTRIBUTED_TO_THIS_OPERATOR"||rules["addedRecoveryCap"]?.Type!=JTokenType.Null||Number(rules,"internalCooldownMs")!=0)throw new ArgumentException("Different lifesteal policy");
                values["healPercent"]=Number(rules,"damageToHealBps")/100;
            }else{
                values["damagePercent"]=Number(rules,"excessToDamageBps")/100;
                values["parentCapPercent"]=Number(rules,"parentCasterAttackCapBps")/100;
                values["speedMeters"]=Number(rules,"projectileProfile.speedMmPerSecond")/1000;
                values["rangeMeters"]=Number(rules,"projectileProfile.maximumRangeMm")/1000;
                if(composition?["o02AndBackflowAllocationBps"] is not JArray split||split.Count!=2||Number(split,"[0]")!=5000||Number(split,"[1]")!=5000)throw new ArgumentException("Original equal allocation required");
                values["shieldCombinedPercent"]=Number(composition,"o02ConversionBps")/200;
                values["damageCombinedPercent"]=Number(composition,"backflowConversionBps")/200;
                if(Number(composition,"backflowConversionBps")!=Number(rules,"excessToDamageBps"))throw new ArgumentException("Mixed definition versions");
                values["soloExample"]=700*values["damagePercent"]/100;
                values["shieldExample"]=700*values["shieldCombinedPercent"]/100;
                values["combinedExample"]=700*values["damageCombinedPercent"]/100;
            }
            if(copy==null){var asset=LastRail.Resources.PreparedContent.Load<TextAsset>("HealingAugmentCopy.ko");if(asset==null)throw new InvalidOperationException("Healing copy asset missing");copy=JObject.Parse(asset.text);}
            var text=copy["cards"][id];string name=(string)definition["name"];if(string.IsNullOrWhiteSpace(name))throw new ArgumentException("Pinned name missing");
            string summary=Fill((string)text["summary"],values);
            string details=summary+"\n\n적용 대상\n"+(string)text["targets"]+"\n\n발동 조건\n"+(string)text["trigger"]+"\n\n"+Fill((string)text["details"],values);
            return new AugmentCardPresentation(name,"증강 · "+(string)text["scope"],summary,null,details,text["examples"]==null?null:Fill((string)text["examples"],values));
        }
    }
}
