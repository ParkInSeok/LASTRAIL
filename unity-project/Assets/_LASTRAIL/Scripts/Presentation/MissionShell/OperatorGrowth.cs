using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.OfflineRewards;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell
{
    public sealed class OperatorGrowthConfig
    {
        readonly JObject growth;public OfflineEconomyMetadata Policy {get;}
        OperatorGrowthConfig(JObject growth,OfflineEconomyMetadata policy){this.growth=(JObject)growth.DeepClone();Policy=policy;}
        public static OperatorGrowthConfig Parse(string json)
        {
            var r=GameApiJson.ReadObject(json);var policy=OfflineEconomyMetadata.Read(r);if(policy.EconomyVersion==null||policy.BalanceVersion==null)throw new FormatException();
            var g=GameApiJson.Object(r,"growth");var max=AccountStateSnapshot.Number(g,"operatorMaxLevel");if(max<1||max>50)throw new FormatException();
            foreach(var name in new[]{"operatorExpSteps1to10","skillStepsCreditsData","promotionStepsCreditsDataCores","trainCoreStepsCreditsPartsCores"})if(!(g[name] is JArray))throw new FormatException();
            return new OperatorGrowthConfig(g,policy);
        }
        public IReadOnlyDictionary<string,long> Cost(string kind,string target,AccountStateSnapshot account)
        {
            try{
                if(account==null)return null;var cost=new Dictionary<string,long>();
                if(kind=="growth-train"){
                    if(account.Train==null||account.Train.Text("trainId")!=target)return null;
                    var index=account.Train.Number("level")-1;var steps=(JArray)growth["trainCoreStepsCreditsPartsCores"];if(index<0||index>=steps.Count)return null;
                    var row=steps[(int)index] as JArray;if(row==null||row.Count!=3)return null;
                    cost.Add("CREDITS",Amount(row[0]));cost.Add("MACHINE_PARTS",Amount(row[1]));cost.Add("SPECIAL_CORE",Amount(row[2]));
                    return new System.Collections.ObjectModel.ReadOnlyDictionary<string,long>(cost);
                }
                if(kind=="growth-sync"){
                    if(target!="all"||account.TrainingCoreSlots.Count!=5||account.TrainingCoreSlots.Select(s=>s.Text("operatorId")).Distinct().Count()!=5)return null;
                    var lowest=account.TrainingCoreSlots.OrderBy(s=>s.Number("trainedLevel")).ThenBy(s=>s.Number("slot")).First();
                    return Cost("growth-core",lowest.Number("slot").ToString(),account);
                }
                if(kind=="growth-core"){
                    var core=account.TrainingCoreSlots.FirstOrDefault(s=>s.Number("slot").ToString()==target);if(core==null)return null;var level=core.Number("trainedLevel");
                    if(level<1||level>=AccountStateSnapshot.Number(growth,"operatorMaxLevel"))return null;
                    JToken value=level<10?growth["operatorExpSteps1to10"][(int)level-1]:growth[level<20?"operatorExpStep10to20":level<30?"operatorExpStep20to30":level<40?"operatorExpStep30to40":"operatorExpStep40to50"];
                    cost.Add("OPERATOR_EXP",Amount(value));
                }else{
                    var op=account.Operators.FirstOrDefault(o=>o.Text("operatorId")==target);if(op==null)return null;
                    if(kind!="growth-skill"&&kind!="growth-promotion")return null;bool promotion=kind=="growth-promotion";
                    var index=op.Number(promotion?"promotion":"skillTraining")-(promotion?0:1);var steps=(JArray)growth[promotion?"promotionStepsCreditsDataCores":"skillStepsCreditsData"];
                    if(index<0||index>=steps.Count)return null;var row=steps[(int)index] as JArray;if(row==null||row.Count!=(promotion?3:2))return null;
                    cost.Add("CREDITS",Amount(row[0]));cost.Add("TACTICAL_DATA",Amount(row[1]));if(promotion)cost.Add("SPECIAL_CORE",Amount(row[2]));
                }
                return new System.Collections.ObjectModel.ReadOnlyDictionary<string,long>(cost);
            }catch{return null;}
        }
        static long Amount(JToken value){if(value?.Type!=JTokenType.Integer)throw new FormatException();var n=(long)value;if(n<0||n>9007199254740991L)throw new FormatException();return n;}
        public static string CostLabel(IReadOnlyDictionary<string,long> cost)=>cost==null?"성장 조건 확인 필요":string.Join("\n",cost.Where(p=>p.Value>0).Select(p=>(p.Key=="CREDITS"?"크레딧":p.Key=="MACHINE_PARTS"?"기계 부품":p.Key=="TACTICAL_DATA"?"전술 데이터":p.Key=="SPECIAL_CORE"?"특수 코어":"대원 경험치")+" "+p.Value.ToString("N0")));
    }
    public sealed class GrowthReceipt
    {
        public string Id {get;} public OfflineEconomyMetadata Policy {get;}
        internal GrowthReceipt(string id,OfflineEconomyMetadata policy){Id=id;Policy=policy;}
    }
    public sealed class GrowthWriteResult
    {
        public GrowthReceipt Receipt {get;} public string Error {get;} public bool Terminal {get;}
        internal GrowthWriteResult(GrowthReceipt receipt,string error,bool terminal){Receipt=receipt;Error=error;Terminal=terminal;}
    }
    public static class OperatorGrowthWrite
    {
        public static JObject Validate(string kind,string target,string payload)
        {
            if(payload==null||payload.Length>2000||target==null)throw new FormatException();bool core=kind=="growth-core",replace=kind=="growth-replace",sync=kind=="growth-sync";
            if(!core&&!replace&&!sync&&kind!="growth-skill"&&kind!="growth-promotion"&&kind!="growth-train")throw new FormatException();
            if(!Regex.IsMatch(target,core||replace?"^[0-4]$":sync?"^all$":"^[A-Za-z0-9_-]{1,100}$"))throw new FormatException();
            var r=GameApiJson.ReadObject(payload);if(r.Properties().Count()!=(core||replace?2:1))throw new FormatException();var version=GameApiJson.String(r,"economyVersion");if(version.Length>128||!Regex.IsMatch(version,"^[A-Za-z0-9_.-]+$"))throw new FormatException();
            if(core){var level=AccountStateSnapshot.Number(r,"targetLevel");if(level<2||level>50)throw new FormatException();}
            if(replace&&!Regex.IsMatch(GameApiJson.String(r,"operatorId"),"^[A-Za-z0-9_-]{1,100}$"))throw new FormatException();return r;
        }
        public static async Task<GrowthWriteResult> SendAsync(IRunApiHttpTransport wire,GameApiAccessToken token,PendingAccountAction action)
        {
            try{
                var request=Validate(action.Kind,action.Target,action.Payload);bool core=action.Kind=="growth-core",replace=action.Kind=="growth-replace",sync=action.Kind=="growth-sync";
                var path=action.Kind=="growth-train"?"/v1/trains/"+action.Target+"/level-up":sync?"/v1/operators/training-core/sync-up":core||replace?"/v1/operators/training-core/"+action.Target+(replace?"/replace":"/level-up"):"/v1/operators/"+action.Target+(action.Kind=="growth-skill"?"/skill/level-up":"/promotion");
                var response=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST",path,action.Payload,new[]{new RunApiHttpHeader("Idempotency-Key",action.Key)}),token));
                if(response.IsNetworkError||response.StatusCode>=500)return new GrowthWriteResult(null,"unknown",false);var r=GameApiJson.ReadObject(response.BodyJson);
                if(response.StatusCode==200&&GameApiJson.Boolean(r,"accepted")){
                    var policy=OfflineEconomyMetadata.Read(r);if(policy.EconomyVersion!=(string)request["economyVersion"]||GameApiJson.String(r,"kind")!=(core?"CORE_LEVEL":replace?"CORE_REPLACE":sync?"CORE_SYNC":action.Kind=="growth-train"?"TRAIN":action.Kind=="growth-skill"?"SKILL":"PROMOTION"))throw new FormatException();
                    if(!core&&!replace&&!sync&&GameApiJson.String(r,"targetId")!=action.Target)throw new FormatException();
                    if(action.Kind=="growth-train"&&AccountStateSnapshot.Number(r,"level")<2)throw new FormatException();
                    if(core&&(!(r["slots"] is JArray slots)||!slots.OfType<JObject>().Any(s=>(long?)s["slot_index"]==long.Parse(action.Target)&&(long?)s["trained_level"]==(long)request["targetLevel"])))throw new FormatException();
                    if(replace||sync){
                        var rows=r["slots"] as JArray;if(rows==null||rows.Count>5||(sync&&rows.Count!=5))throw new FormatException();
                        var indices=new HashSet<long>();var ids=new HashSet<string>();
                        foreach(var row in rows){if(!(row is JObject s))throw new FormatException();var slot=AccountStateSnapshot.Number(s,"slot_index");if(slot>4||!indices.Add(slot)||!ids.Add(GameApiJson.String(s,"operator_id"))||AccountStateSnapshot.Number(s,"trained_level")<1)throw new FormatException();AccountStateSnapshot.Number(s,"invested_operator_exp");}
                        if(replace&&!rows.OfType<JObject>().Any(s=>(long)s["slot_index"]==long.Parse(action.Target)&&(string)s["operator_id"]==(string)request["operatorId"]&&(long)s["trained_level"]==1&&(long)s["invested_operator_exp"]==0))throw new FormatException();
                    }
                    return new GrowthWriteResult(new GrowthReceipt(GameApiJson.String(r,"receiptId"),policy),null,true);
                }
                return new GrowthWriteResult(null,GameApiJson.String(r,"errorCode"),!GameApiJson.Boolean(r,"accepted")&&(response.StatusCode==400||response.StatusCode==409));
            }catch{return new GrowthWriteResult(null,"invalid_response",false);}
        }
    }
}
