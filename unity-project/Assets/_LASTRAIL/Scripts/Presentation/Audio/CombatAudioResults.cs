using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.Audio
{
    // Presentation meanings, never asset IDs or a second effect calculator.
    public static class CombatAudioResults
    {
        static readonly HashSet<string> support=new HashSet<string>(StringComparer.Ordinal){"Heal","Lifesteal","ShieldGain","ShieldTransfer","Cleanse","PreventionGranted","PreventionConsumed","DownSupport","Returned"};
        public static string Support(string confirmedKind,long actualApplied)
        {
            if(!support.Contains(confirmedKind)||actualApplied<0)throw new ArgumentException("Known confirmed support kind and actual applied amount/count required.");
            return actualApplied>0?confirmedKind:null;
        }
        public static bool Known(string kind)=>kind!=null&&(support.Contains(kind)||kind=="HpContact"||kind=="ShieldContact"||kind=="MixedContact"||kind=="ContactOnly"||kind=="DamageImmune"||kind=="Miss");
        static long Amount(JObject value,string key)
        {
            if(value?[key]?.Type!=JTokenType.Integer)throw new ArgumentException("Original integer "+key+" required.");
            long amount=(long)value[key];if(amount<0)throw new ArgumentException("Negative original "+key);return amount;
        }
        public static string Contact(long hpLoss,long absorbed)
        {
            if(hpLoss<0||absorbed<0)throw new ArgumentException("Original applied amounts required.");
            return hpLoss>0?(absorbed>0?"MixedContact":"HpContact"):(absorbed>0?"ShieldContact":"ContactOnly");
        }
        public static string[] ReadNative(JObject receipt)
        {
            if(receipt==null||receipt["hp"] is not JObject hp)throw new ArgumentException("Committed native receipt required.");
            var results=new List<string>{Contact(Amount(hp,"hpLoss"),Amount(hp,"absorbed"))};
            if(receipt["lifesteal"]!=null&&receipt["lifesteal"].Type!=JTokenType.Null)
            {
                if(receipt["lifesteal"] is not JObject recovery)throw new ArgumentException("Original recovery receipt required.");
                var effect=Support("Lifesteal",Amount(recovery,"actualHpGain"));if(effect!=null)results.Add(effect);
            }
            return results.ToArray();
        }
    }
}
