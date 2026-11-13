using System;
using System.Collections.Generic;
using System.Numerics;
using System.Globalization;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle
{
    // Pure calculation over original base or already-grown stats and already-eligible sources.
    // This local input is not an issued Server catalog/START contract.
    public static class CombatStatResolver
    {
        public const string Policy="combat-stat.44-v10-v12-13-v06-35-turret-v01";
        const int Scale=10000,MaxSources=1024;
        const long MaxSafeInteger=9007199254740991L;
        public static CombatExactValue OperatorLevelAttack(int originalBase,int level)=>Growth(originalBase,level,50,350,true);
        public static CombatExactValue OperatorLevelMaxHp(int originalBase,int level)=>Growth(originalBase,level,50,400,true);
        // Caller selects this only from the version-pinned Anchor definition, never by UI name.
        public static CombatExactValue LevelDefense(int originalBase,int level,bool anchorGrowth)=>Growth(originalBase,level,50,anchorGrowth?350:0,true);
        public static CombatExactValue TurretLevelAttack(int originalBase,int ownLevel)=>Growth(originalBase,ownLevel,10,600,false);
        public static CombatExactValue TurretLevelMaxHp(int originalBase,int ownLevel)=>Growth(originalBase,ownLevel,10,800,true);
        static CombatExactValue Growth(int originalBase,int level,int maximum,int perLevel,bool floor)
        {
            if(originalBase<=0||level<1||level>maximum) throw new ArgumentException("Original positive base and supported level required.");
            var value=CombatExactValue.Create((BigInteger)originalBase*(Scale+perLevel*(level-1)),Scale);
            return floor?CombatExactValue.Create(value.Floor(),1):value;
        }
        public static CombatExactValue Attack(string ownerId,CombatExactValue grownAttack,JArray eligibleSources)=>Resolve(ownerId,grownAttack,eligibleSources,new[]{"EQUIPMENT_ATK","AUGMENT_ATK","COMMANDER_ATK"},false);
        public static CombatExactValue EquipmentMaxHp(string ownerId,CombatExactValue grownMaxHp,JArray eligibleSources)=>Resolve(ownerId,grownMaxHp,eligibleSources,new[]{"EQUIPMENT_HP"},true);
        public static CombatExactValue EquipmentDefense(string ownerId,CombatExactValue grownDefense,JArray eligibleSources)=>Resolve(ownerId,grownDefense,eligibleSources,new[]{"EQUIPMENT_DEF"},false);
        static CombatExactValue Resolve(string ownerId,CombatExactValue grown,JArray sources,string[] allowed,bool floor)
        {
            if(!ValidId(ownerId)||grown==null||sources==null||sources.Count>MaxSources) throw new ArgumentException("Explicit owner, grown stat and eligible sources required.");
            var sums=new Dictionary<string,BigInteger>(StringComparer.Ordinal);
            foreach(var group in allowed) sums.Add(group,BigInteger.Zero);
            var identities=new Dictionary<string,(string group,long bps)>(StringComparer.Ordinal);
            foreach(var token in sources)
            {
                if(!(token is JObject source)||source.Count!=4) throw new ArgumentException("Invalid stat source.");
                var id=Id(source["sourceId"]);var owner=Id(source["ownerId"]);var group=Id(source["group"]);
                if(owner!=ownerId||!sums.ContainsKey(group)) throw new ArgumentException("Ineligible stat owner or group.");
                var bps=BasisPoints(source["valueBps"]);
                if(identities.TryGetValue(id,out var prior)) {
                    if(prior.group!=group||prior.bps!=bps) throw new ArgumentException("Stat source identity conflict.");
                    continue;
                }
                identities.Add(id,(group,bps));sums[group]+=bps;
            }
            var result=grown;
            foreach(var group in allowed) result=result.Multiply(CombatExactValue.Create(Scale+sums[group],Scale));
            return floor?CombatExactValue.Create(result.Floor(),1):result;
        }
        static bool ValidId(string text)=>!string.IsNullOrWhiteSpace(text)&&text.Length<=400;
        static string Id(JToken token) {
            if(token==null||token.Type!=JTokenType.String||!ValidId((string)token)) throw new ArgumentException("Stat source ID required.");
            return (string)token;
        }
        static long BasisPoints(JToken token) {
            if(token==null||(token.Type!=JTokenType.Integer&&token.Type!=JTokenType.Float)) throw new ArgumentException("Integral stat bonus required.");
            if(token.Type==JTokenType.Integer) {
                var integer=BigInteger.Parse(token.ToString(),NumberStyles.Integer,CultureInfo.InvariantCulture);
                if(integer<0||integer>MaxSafeInteger) throw new ArgumentException("Invalid stat bonus.");
                return (long)integer;
            }
            if(((JValue)token).Value is decimal exact) {
                if(exact<0||exact>MaxSafeInteger||exact!=decimal.Truncate(exact)) throw new ArgumentException("Invalid stat bonus.");
                return (long)exact;
            }
            var value=(double)token;
            if(double.IsNaN(value)||double.IsInfinity(value)||value<0||value>MaxSafeInteger||value!=Math.Truncate(value)) throw new ArgumentException("Invalid stat bonus.");
            return checked((long)value);
        }
    }
}
