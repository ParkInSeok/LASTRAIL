using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle
{
    public sealed class CombatNumericDefenseResult
    {
        readonly CombatExactValue defense,exact;readonly string version;
        internal CombatNumericDefenseResult(CombatExactValue defense,CombatExactValue exact,string version=CombatNumericDefenseBudget.Version) { this.defense=defense;this.exact=exact;this.version=version; }
        public JObject Snapshot()=>new JObject { ["version"]=version,["effectiveDefense"]=defense.Snapshot(),
            ["exact"]=exact.Snapshot(),["ordinaryBeforeShield"]=CombatExactValue.Text(exact.Floor()) };
        // Existing Vitals uses Int32. Reject an unsupported magnitude; never silently clamp it.
        public int ToVitalsMagnitude()=>checked((int)exact.Floor());
    }
    // Ordinary/story arithmetic only. Caller supplies already eligible source identities and DEF.
    // This boundary does not authorize effects, adapt START, change HP, or convert legacy armor.
    public static class CombatNumericDefenseBudget
    {
        public const string Version="combat.numeric-defense-budget.v1";
        public const string MitigationVersion="combat.numeric-defense-budget.mitigation-rate.v2.20261107";
        const int Scale=10000,DefenseK=1000,WeakeningCap=3000,PenetrationCap=6000,EquipmentReductionCap=5000,OutgoingCap=3000,MarkCap=2000;
        const long MaxSafeInteger=9007199254740991;
        const int MaxSources=1024;
        sealed class Identity { internal string Group; internal BigInteger Value; }
        public static CombatNumericDefenseResult Resolve(JObject input)=>ResolveCore(input,new JArray(),Version);
        // Separate arithmetic input preserves the legacy wire; eligibility belongs to the effect owner.
        public static CombatNumericDefenseResult ResolveWithMitigationRateReduction(JObject input,JArray sources)=>ResolveCore(input,sources,MitigationVersion);
        static CombatNumericDefenseResult ResolveCore(JObject input,JArray mitigationRateSources,string resultVersion)
        {
            Fields(input,"version","base","effects","defense","weakening","penetration","reductions");
            if(input["version"]?.Type!=JTokenType.String || (string)input["version"]!=Version) throw new ArgumentException("Explicit numeric defense version required.");
            var basis=CombatExactValue.From(Object(input["base"]));
            var defense=CombatExactValue.From(Object(input["defense"]));
            var identities=new Dictionary<string,Identity>(StringComparer.Ordinal);
            BigInteger equipment=0,outgoing=0,mark=0;
            foreach(var effect in Array(input["effects"])) {
                var e=Object(effect); Fields(e,"sourceId","group","valueBps");
                var group=Id(e["group"]);
                if(group!="EQUIPMENT_DIRECT"&&group!="NON_EQUIPMENT_OUTGOING"&&group!="MARK") throw new ArgumentException("Unsupported offense group.");
                var value=Number(e["valueBps"],MaxSafeInteger);
                if(!Register(identities,Id(e["sourceId"]),group,value)) continue;
                if(group=="EQUIPMENT_DIRECT") equipment+=value;
                else if(group=="NON_EQUIPMENT_OUTGOING") outgoing+=value;
                else mark=BigInteger.Max(mark,value);
            }
            var weakening=BigInteger.Min(Strongest(Sources(input["weakening"],"DEF_WEAKENING",identities,MaxSafeInteger)),WeakeningCap);
            var penetration=BigInteger.Min(Sum(Sources(input["penetration"],"DEF_PENETRATION",identities,MaxSafeInteger)),PenetrationCap);
            var mitigation=BigInteger.Min(Strongest(Sources(mitigationRateSources,"MITIGATION_RATE_REDUCTION",identities,MaxSafeInteger)),1500);
            var reductions=Object(input["reductions"]); Fields(reductions,"equipment","guardStance","independent");
            var reduction=BigInteger.Min(Sum(Sources(reductions["equipment"],"EQ06",identities,MaxSafeInteger)),EquipmentReductionCap);
            var stance=Strongest(Sources(reductions["guardStance"],"guard_stance",identities,Scale));
            var independent=new List<BigInteger>(); var groups=new HashSet<string>(StringComparer.Ordinal);
            foreach(var item in Array(reductions["independent"])) {
                var g=Object(item); Fields(g,"groupId","valueBps"); var id=Id(g["groupId"]);
                if(id=="EQ06" || id=="guard_stance" || !groups.Add(id)) throw new ArgumentException("Invalid independent reduction group.");
                independent.Add(Number(g["valueBps"],Scale));
            }
            var effectiveDefense=defense.Multiply(Factor(Scale-weakening)).Multiply(Factor(Scale-penetration));
            var kd=DefenseK*effectiveDefense.Denominator;
            var defenseFactor=CombatExactValue.From(new JObject{["numerator"]=CombatExactValue.Text(kd),
                ["denominator"]=CombatExactValue.Text(kd+effectiveDefense.Numerator)});
            if(mitigation>0){
                var denominator=(kd+effectiveDefense.Numerator)*Scale;
                defenseFactor=CombatExactValue.Create(BigInteger.Min(denominator,kd*Scale+mitigation*(kd+effectiveDefense.Numerator)),denominator);
            }
            var result=basis.Multiply(Factor(Scale+equipment)).Multiply(Factor(Scale+BigInteger.Min(outgoing,OutgoingCap)))
                .Multiply(Factor(Scale+BigInteger.Min(mark,MarkCap))).Multiply(defenseFactor)
                .Multiply(Factor(Scale-reduction)).Multiply(Factor(Scale-stance));
            foreach(var value in independent) result=result.Multiply(Factor(Scale-value));
            return new CombatNumericDefenseResult(effectiveDefense,result,resultVersion);
        }
        static CombatExactValue Factor(BigInteger numerator)=>CombatExactValue.Create(numerator,Scale);
        static IEnumerable<BigInteger> Sources(JToken token,string group,Dictionary<string,Identity> identities,long maximum) {
            var values=new List<BigInteger>();
            foreach(var item in Array(token)) {
                var source=Object(item); Fields(source,"sourceId","valueBps"); var value=Number(source["valueBps"],maximum);
                if(Register(identities,Id(source["sourceId"]),group,value)) values.Add(value);
            }
            return values;
        }
        static bool Register(Dictionary<string,Identity> identities,string id,string group,BigInteger value) {
            if(identities.TryGetValue(id,out var old)) {
                if(old.Group!=group || old.Value!=value) throw new ArgumentException("Combat effect source conflict.");
                return false;
            }
            identities.Add(id,new Identity{Group=group,Value=value}); return true;
        }
        static BigInteger Sum(IEnumerable<BigInteger> values)=>values.Aggregate(BigInteger.Zero,(sum,value)=>sum+value);
        static BigInteger Strongest(IEnumerable<BigInteger> values)=>values.Aggregate(BigInteger.Zero,BigInteger.Max);
        static JObject Object(JToken token)=>token as JObject ?? throw new ArgumentException("Combat object required.");
        static JArray Array(JToken token) {
            var array=token as JArray; if(array==null||array.Count>MaxSources) throw new ArgumentException("Invalid combat source array."); return array;
        }
        static void Fields(JObject item,params string[] names) {
            if(item==null || item.Count!=names.Length || names.Any(name=>item[name]==null)) throw new ArgumentException("Invalid combat object fields.");
        }
        static string Id(JToken token) {
            if(token?.Type!=JTokenType.String || ((string)token).Length<1 || ((string)token).Length>400) throw new ArgumentException("Invalid combat source identity.");
            return (string)token;
        }
        static BigInteger Number(JToken token,long maximum) {
            BigInteger value;
            if(token?.Type==JTokenType.Integer) {
                if(!BigInteger.TryParse(token.ToString(),out value)) throw new ArgumentException("Invalid combat basis points.");
            } else if(token?.Type==JTokenType.Float) {
                // JSON numbers are IEEE doubles on the Server, including integral decimals/exponents.
                double number=(double)token;
                if(double.IsNaN(number)||double.IsInfinity(number)||number<0||number>maximum||Math.Floor(number)!=number)
                    throw new ArgumentException("Invalid combat basis points.");
                value=new BigInteger(number);
            } else throw new ArgumentException("Invalid combat basis points.");
            if(value<0||value>maximum) throw new ArgumentException("Invalid combat basis points.");
            return value;
        }
    }
}

