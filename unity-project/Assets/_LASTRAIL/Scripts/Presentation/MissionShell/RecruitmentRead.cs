using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using LastRail.Run;
using LastRail.Auth;
namespace LastRail.Presentation.MissionShell
{
    // Immutable public snapshots; quantities and policy versions always come from server.
    public sealed class RecruitmentBanner
    {
        readonly JObject row;
        public string Id=>RecruitmentJson.Id(row,"bannerId");
        public string Type=>(string)row["bannerType"];
        public string Availability=>(string)row["availability"];
        public string Version=>(string)row["balanceVersion"];
        public string Pickup=>(string)row["pickupOperatorId"];
        public string Guarantee=>(string)row["nextGuarantee"];
        public JObject Pity=>(JObject)row["pityState"].DeepClone();
        public JObject Thresholds=>(JObject)row["pityConfig"].DeepClone();
        public JArray Probabilities=>(JArray)row["probabilities"].DeepClone();
        public long Price(int count)=>row["prices"].OfType<JObject>().Where(p=>(long)p["pullCount"]==count).Select(p=>(long)p["quantity"]).DefaultIfEmpty(-1).Single();
        public RecruitmentBanner(JObject source)
        {
            row=(JObject)source.DeepClone();RecruitmentJson.Id(row,"bannerId");GameApiJson.String(row,"balanceVersion");RecruitmentJson.NullableString(row,"pickupOperatorId");
            RecruitmentJson.Enum(row,"bannerType","STANDARD","PICKUP");RecruitmentJson.Enum(row,"availability","ACTIVE","UPCOMING","ENDED");
            RecruitmentJson.Enum(row,"nextGuarantee","NONE","RARE_OR_HIGHER_10","ELITE_30","PICKUP_ELITE_60");RecruitmentJson.Utc(row,"startsAt");if(row["endsAt"]?.Type!=JTokenType.Null)RecruitmentJson.Utc(row,"endsAt");
            RecruitmentJson.Pity(GameApiJson.Object(row,"pityState"));foreach(var key in new[]{"rare","elite","pickup"})AccountStateSnapshot.Number(GameApiJson.Object(row,"pityConfig"),key);
            var prices=RecruitmentJson.Array(row,"prices");var counts=new HashSet<long>();foreach(var token in prices){var p=(JObject)token;var count=AccountStateSnapshot.Number(p,"pullCount");if((count!=1&&count!=10)||!counts.Add(count)||GameApiJson.String(p,"itemId")!="cur_command_ticket"||AccountStateSnapshot.Number(p,"quantity")!=count)throw new FormatException();}
            if(counts.Count!=2)throw new FormatException();var probabilities=RecruitmentJson.Array(row,"probabilities");if(probabilities.Count==0)throw new FormatException();
            foreach(var token in probabilities){var p=(JObject)token;RecruitmentJson.Id(p,"resultId");GameApiJson.String(p,"resultType");GameApiJson.String(p,"resultRarity");if(AccountStateSnapshot.Number(p,"quantity")<1)throw new FormatException();GameApiJson.Boolean(p,"isPickup");foreach(var key in new[]{"baseProbability","nextPullProbability"}){if(p[key]?.Type!=JTokenType.Float&&p[key]?.Type!=JTokenType.Integer)throw new FormatException();var n=(double)p[key];if(double.IsNaN(n)||n<0||n>1)throw new FormatException();}}
        }
    }
    public sealed class RecruitmentBatch
    {
        readonly JObject row;
        public string Id=>(string)row["batchId"];public string BannerId=>(string)row["bannerId"];public int Count=>(int)row["pullCount"];
        public string Version=>(string)row["balanceVersion"];public string CreatedAt=>(string)row["createdAt"];
        public bool Complete=>(string)row["recoveryStatus"]=="COMPLETE";
        public JArray Results=>(JArray)row["results"].DeepClone();public JArray Missing=>(JArray)row["missingSequences"].DeepClone();public JArray Unknown=>(JArray)row["unknownQuantitySequences"].DeepClone();
        public RecruitmentBatch(JObject source)
        {
            row=(JObject)source.DeepClone();RecruitmentJson.Id(row,"batchId");RecruitmentJson.Id(row,"bannerId");var count=AccountStateSnapshot.Number(row,"pullCount");if(count!=1&&count!=10)throw new FormatException();
            RecruitmentJson.NullableString(row,"balanceVersion");RecruitmentJson.Utc(row,"createdAt");var cost=GameApiJson.Object(row,"cost");GameApiJson.String(cost,"itemId");AccountStateSnapshot.Number(cost,"quantity");
            if(row["postPityState"]?.Type!=JTokenType.Null)RecruitmentJson.Pity(GameApiJson.Object(row,"postPityState"));RecruitmentJson.Enum(row,"recoveryStatus","COMPLETE","INCOMPLETE");
            var seen=new HashSet<long>();var unknown=new HashSet<long>();foreach(var token in RecruitmentJson.Array(row,"results")){
                var r=(JObject)token;var sequence=AccountStateSnapshot.Number(r,"pullSequence");if(sequence<1||sequence>count||!seen.Add(sequence))throw new FormatException();
                RecruitmentJson.Id(r,"resultId");GameApiJson.String(r,"resultRarity");GameApiJson.String(r,"resultType");GameApiJson.Boolean(r,"isPickup");var duplicate=GameApiJson.Boolean(r,"wasDuplicate");
                var reward=GameApiJson.Object(r,"reward");var kind=GameApiJson.String(reward,"kind");var id=RecruitmentJson.NullableString(reward,"id");
                if(reward["quantity"]?.Type==JTokenType.Null||kind=="UNKNOWN"){if(kind=="UNKNOWN"&&(id!=null||reward["quantity"]?.Type!=JTokenType.Null))throw new FormatException();unknown.Add(sequence);}
                else{if(id==null||AccountStateSnapshot.Number(reward,"quantity")<1)throw new FormatException();}
                if(duplicate&&kind=="OPERATOR")throw new FormatException("Conversion cannot be an operator count.");
            }
            var missing=RecruitmentJson.Sequences(row,"missingSequences",(int)count);var listedUnknown=RecruitmentJson.Sequences(row,"unknownQuantitySequences",(int)count);
            if(!missing.SetEquals(Enumerable.Range(1,(int)count).Select(i=>(long)i).Where(i=>!seen.Contains(i)))||!listedUnknown.SetEquals(unknown)||Complete!=(missing.Count==0&&unknown.Count==0))throw new FormatException();
        }
        public string ResultLine(int sequence)
        {
            var result=row["results"].OfType<JObject>().SingleOrDefault(r=>(int)r["pullSequence"]==sequence);if(result==null)return sequence+" · 결과 확인 필요";
            var reward=(JObject)result["reward"];if((string)reward["kind"]=="UNKNOWN"||reward["quantity"].Type==JTokenType.Null)return sequence+" · 보상 수량 확인 필요";
            var id=(string)reward["id"];var label=(string)reward["kind"]=="OPERATOR"?OperatorVisualCatalog.Label(id):id;
            return sequence+" · "+label+" × "+(long)reward["quantity"]+((bool)result["wasDuplicate"]?" (중복 전환)":"");
        }
    }
    internal static class RecruitmentJson
    {
        public static string Id(JObject r,string key){var id=GameApiJson.String(r,key);if(!Regex.IsMatch(id,"^[A-Za-z0-9_-]{1,128}$"))throw new FormatException();return id;}
        public static string NullableString(JObject r,string key){if(r[key]==null)throw new FormatException();return r[key].Type==JTokenType.Null?null:GameApiJson.String(r,key);}
        public static JArray Array(JObject r,string key)=>r[key] as JArray??throw new FormatException();
        public static void Enum(JObject r,string key,params string[] values){if(!values.Contains(GameApiJson.String(r,key)))throw new FormatException();}
        public static void Utc(JObject r,string key){var value=GameApiJson.String(r,key);if(!value.EndsWith("Z")||!DateTimeOffset.TryParse(value,out _))throw new FormatException();}
        public static void Pity(JObject r){foreach(var key in new[]{"totalPullCount","elitePityCount","pickupElitePityCount","rareOrHigherPityCount","revision"})AccountStateSnapshot.Number(r,key);}
        public static HashSet<long> Sequences(JObject r,string key,int count){var result=new HashSet<long>();foreach(var t in Array(r,key)){if(t.Type!=JTokenType.Integer||(long)t<1||(long)t>count||!result.Add((long)t))throw new FormatException();}return result;}
        public static string PathId(string value){if(value==null||!Regex.IsMatch(value,"^[A-Za-z0-9_-]{1,128}$"))throw new FormatException();return value;}
    }
}
