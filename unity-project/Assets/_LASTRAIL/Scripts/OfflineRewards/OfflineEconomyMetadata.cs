using System;
using System.Text.RegularExpressions;
using LastRail.Auth;
using Newtonsoft.Json.Linq;
namespace LastRail.OfflineRewards
{
    public sealed class OfflineEconomyMetadata
    {
        public long? BalanceVersion {get;} public string EconomyVersion {get;} public string EconomyHash {get;}
        OfflineEconomyMetadata(long? balance,string version,string hash){BalanceVersion=balance;EconomyVersion=version;EconomyHash=hash;}
        public static OfflineEconomyMetadata Read(JObject r)
        {
            // Old S01/legacy replies may omit all three. New contract requires nullable fields.
            if(r["balanceVersion"]==null&&r["economyVersion"]==null&&r["economyHash"]==null)return new OfflineEconomyMetadata(null,null,null);
            if(r["balanceVersion"]==null||r["economyVersion"]==null||r["economyHash"]==null)throw new FormatException();
            long? balance=null;if(r["balanceVersion"].Type!=JTokenType.Null){if(r["balanceVersion"].Type!=JTokenType.Integer)throw new FormatException();balance=(long)r["balanceVersion"];if(balance<0)throw new FormatException();}
            var version=GameApiJson.String(r,"economyVersion",true);var hash=GameApiJson.String(r,"economyHash",true);
            if((version==null)!=(hash==null)||hash!=null&&!Regex.IsMatch(hash,"^[0-9a-fA-F]{64}$"))throw new FormatException();
            return new OfflineEconomyMetadata(balance,version,hash);
        }
    }
}
