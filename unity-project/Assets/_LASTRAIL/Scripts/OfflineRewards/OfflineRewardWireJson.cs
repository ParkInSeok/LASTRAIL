using System;
using System.Linq;
using LastRail.Auth;
using Newtonsoft.Json.Linq;

namespace LastRail.OfflineRewards
{
    internal static class OfflineRewardWireJson
    {
        static string S(JObject r,string k)=>GameApiJson.String(r,k);
        static int N(JObject r,string k)
        {if(r[k]?.Type!=JTokenType.Integer)throw new FormatException();var n=(long)r[k];if(n<0||n>int.MaxValue)throw new FormatException();return (int)n;}
        public static OfflineRewardPreviewDto Preview(JObject r)
        {
            var status=S(r,"previewStatus");
            if(status!="AVAILABLE"&&status!="EMPTY")throw new FormatException();
            if(!(r["rewardLines"] is JArray rows))throw new FormatException();
            var rewards=rows.Select(t=>{
                if(!(t is JObject v))throw new FormatException();
                if(v["kind"]!=null){
                    if(S(v,"kind")!="CURRENCY")throw new FormatException();var id=S(v,"id");
                    var label=id=="cur_credits"?"크레딧":id=="cur_machine_parts"?"기계 부품":id=="cur_tactical_data"?"전술 데이터":id=="cur_operator_exp"?"대원 경험치":id;
                    return new OfflineRewardLineDto(id,id,label,N(v,"quantity"),"개",S(v,"source"),kind:S(v,"kind"));
                }
                return new OfflineRewardLineDto(S(v,"rewardLineId"),S(v,"rewardCode"),S(v,"displayName"),N(v,"quantity"),S(v,"unitLabel"),S(v,"source"));
            }).ToArray();
            var c=GameApiJson.Object(r,"calculation");
            if(r["claimableHours"]?.Type!=JTokenType.Integer&&r["claimableHours"]?.Type!=JTokenType.Float)throw new FormatException();
            var hours=(double)r["claimableHours"];if(double.IsNaN(hours)||double.IsInfinity(hours)||hours<0)throw new FormatException();
            return new OfflineRewardPreviewDto(GameApiJson.Boolean(r,"accepted"),status=="AVAILABLE"?OfflineRewardPreviewStatus.Available:OfflineRewardPreviewStatus.Empty,
                S(r,"storyProgressId"),N(r,"claimableSeconds"),hours,N(r,"offlineStorageCapHours"),N(r,"minimumClaimSeconds"),N(r,"maxClaimableHours"),rewards,
                new OfflineRewardPreviewCalculationDto(S(c,"storyProgressId"),S(c,"storyProgressMultiplierLabel"),N(c,"cappedOfflineSeconds"),S(c,"capSource")),
                S(r,"narrativeVersion"),GameApiJson.UtcTimestamp(r,"serverCalculatedAt").UtcDateTime,GameApiJson.String(r,"errorCode",true),GameApiJson.String(r,"message",true),OfflineEconomyMetadata.Read(r));
        }
    }
}
