using System;
using System.Linq;
using System.Text.RegularExpressions;
using LastRail.Auth;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell
{
    // Optional trusted fixture override. Shipping uses authenticated discovery.
    public interface IMonthlyAttendanceRegistry { string CurrentActivityId {get;} }
    public sealed class MonthlyAttendanceSnapshot
    {
        readonly JObject data;
        public string ActivityId=>(string)data["activityId"];
        public string CycleId=>(string)data["cycleId"];
        public long Revision=>(long)data["revision"];
        public int Completed=>(int)data["completedCells"];
        public bool Active=>(bool)data["active"];
        public bool TodayAttended=>(bool)data["todayAttended"];
        public string UnavailableReason=>(string)data["unavailableReason"];
        public int Recoverable=>(int)data["recovery"]["recoverableCount"];
        public int RecoveryUsed=>(int)data["recovery"]["used"];
        public JArray Cells=>(JArray)data["cells"].DeepClone();
        public JObject Quote=>data["recovery"]["quote"] is JObject q?(JObject)q.DeepClone():null;
        public bool CanAuto=>Active&&UnavailableReason==null&&!TodayAttended;
        public bool CanRecover=>Active&&UnavailableReason==null&&TodayAttended&&Recoverable>0&&Quote!=null&&(bool)Quote["sufficientFunds"];
        public MonthlyAttendanceSnapshot(string json,string expectedActivity)
        {
            data=GameApiJson.ReadObject(json);MonthlyAttendanceContract.Envelope(data);
            if(MonthlyAttendanceContract.Id(data,"activityId")!=expectedActivity||MonthlyAttendanceContract.Id(data,"policyVersion")!=expectedActivity)throw new FormatException();
            MonthlyAttendanceContract.Cycle(data);MonthlyAttendanceContract.Revision(data,"revision");
            var now=DateTimeOffset.FromUnixTimeMilliseconds(AccountStateSnapshot.Number(data,"serverTime"));var service=now.AddHours(5);
            var start=new DateTimeOffset(service.Year,service.Month,1,0,0,0,TimeSpan.Zero).AddHours(-5);var end=start.AddHours(5).AddMonths(1).AddHours(-5);
            if(CycleId!=service.ToString("yyyy-MM")||AccountStateSnapshot.Number(data,"startAt")!=start.ToUnixTimeMilliseconds()||AccountStateSnapshot.Number(data,"endAt")!=end.ToUnixTimeMilliseconds())throw new FormatException();
            var day=MonthlyAttendanceContract.Range(data,"dayIndex",0,30);if(day!=(int)(now-start).TotalDays)throw new FormatException();
            GameApiJson.Boolean(data,"active");GameApiJson.Boolean(data,"todayAttended");MonthlyAttendanceContract.Range(data,"completedCells",0,28);
            if(GameApiJson.Boolean(data,"complete")!=(Completed==28))throw new FormatException();
            var reason=RecruitmentJson.NullableString(data,"unavailableReason");if(reason!=null&&reason!="elite_ticket_binding_missing")throw new FormatException();
            var cells=RecruitmentJson.Array(data,"cells");if(cells.Count!=28)throw new FormatException();var received=0;var recovered=0;
            for(int i=0;i<28;i++){
                var c=cells[i] as JObject;if(c==null||AccountStateSnapshot.Number(c,"cell")!=i+1)throw new FormatException();var rewards=RecruitmentJson.Array(c,"rewards");foreach(var r in rewards)MonthlyAttendanceContract.Reward((JObject)r);
                var missing=RecruitmentJson.NullableString(c,"unavailableReason");if(missing!=null&&(i!=27||missing!="elite_ticket_binding_missing"))throw new FormatException();
                if((missing!=null)!=(i==27&&reason!=null)||rewards.Count!=(missing==null?1:0))throw new FormatException();
                if(c["receipt"] is JObject receipt){received++;if(i>=Completed||AccountStateSnapshot.Number(receipt,"cell")!=i+1)throw new FormatException();var source=GameApiJson.String(receipt,"source");if(source!="NATURAL"&&source!="RECOVERY")throw new FormatException();if(source=="RECOVERY")recovered++;
                    MonthlyAttendanceContract.Range(receipt,"dayIndex",0,source=="RECOVERY"?day-1:day);GameApiJson.String(receipt,"claimId");
                }else if(c["receipt"]?.Type!=JTokenType.Null||i<Completed)throw new FormatException();
            }
            if(received!=Completed)throw new FormatException();var recovery=GameApiJson.Object(data,"recovery");
            if(AccountStateSnapshot.Number(recovery,"price")!=100||AccountStateSnapshot.Number(recovery,"maxPerCycle")!=7||MonthlyAttendanceContract.Range(recovery,"used",0,7)!=recovered||GameApiJson.Boolean(recovery,"todayAttendanceRequired")==TodayAttended)throw new FormatException();
            if(MonthlyAttendanceContract.Range(recovery,"recoverableCount",0,7)>Math.Min(7-recovered,28-Completed))throw new FormatException();
            if(recovery["quote"] is JObject q){MonthlyAttendanceContract.Id(q,"walletPolicyVersion");GameApiJson.String(q,"currencyId");GameApiJson.Boolean(q,"sufficientFunds");if(MonthlyAttendanceContract.Range(q,"freeSpent",0,100)+MonthlyAttendanceContract.Range(q,"paidSpent",0,100)!=100)throw new FormatException();}
            else if(recovery["quote"]?.Type!=JTokenType.Null)throw new FormatException();
        }
    }
    public static class MonthlyAttendanceContract
    {
        public const string Version="monthly-attendance-v1";
        internal static void Envelope(JObject r){if(!GameApiJson.Boolean(r,"accepted")||GameApiJson.String(r,"version")!=Version)throw new FormatException();}
        internal static string Id(JObject r,string field){var id=GameApiJson.String(r,field);if(!Regex.IsMatch(id,@"\A[A-Za-z0-9_-]{1,100}\z"))throw new FormatException();return id;}
        internal static void Cycle(JObject r){var text=GameApiJson.String(r,"cycleId");if(!Regex.IsMatch(text,@"\A\d{4}-(0[1-9]|1[0-2])\z"))throw new FormatException();}
        internal static long Revision(JObject r,string field){var n=AccountStateSnapshot.Number(r,field);if(n>=9007199254740991L)throw new FormatException();return n;}
        internal static int Range(JObject r,string field,int min,int max){var n=AccountStateSnapshot.Number(r,field);if(n<min||n>max)throw new FormatException();return (int)n;}
        internal static void Reward(JObject r){if(AccountStateSnapshot.Number(r,"quantity")<1)throw new FormatException();var kind=(string)r["kind"]??"ITEM";if(kind!="ITEM"&&kind!="CURRENCY"&&kind!="CRYSTAL")throw new FormatException();if(kind!="CRYSTAL")Id(r,"id");}
        public static JObject Request(string kind,string target,string json)
        {
            if(json==null||json.Length>2500||(kind!="monthly-auto"&&kind!="monthly-recovery"))throw new FormatException();var r=GameApiJson.ReadObject(json);
            if(r.Count!=(kind=="monthly-auto"?3:5)||Id(r,"policyVersion")!=target)throw new FormatException();Cycle(r);Revision(r,"expectedRevision");
            if(kind=="monthly-recovery"){Id(r,"walletPolicyVersion");Range(r,"confirmedPaid",0,100);}return r;
        }
        public static string RewardLabel(JObject r)
        {
            var kind=(string)r["kind"]??"ITEM";var id=(string)r["id"];var label=kind=="CRYSTAL"?"크리스탈":id=="cur_credits"?"크레딧":id=="cur_machine_parts"?"기계 부품":id=="cur_operator_exp"?"대원 경험치":id=="cur_tactical_data"?"전술 자료":"아이템";
            return label+" "+(long)r["quantity"];
        }
    }
    public sealed class MonthlyAttendanceReceipt
    {
        readonly JObject data;
        public string ClaimId=>(string)data["claimId"];
        public string Kind=>(string)data["kind"];
        public string CycleId=>(string)data["cycleId"];
        public int? Cell=>(int?)data["cell"];
        public JArray Rewards=>(JArray)data["rewardLines"].DeepClone();
        public MonthlyAttendanceReceipt(string json,PendingAccountAction action)
        {
            var request=MonthlyAttendanceContract.Request(action.Kind,action.Target,action.Payload);data=GameApiJson.ReadObject(json);MonthlyAttendanceContract.Envelope(data);MonthlyAttendanceContract.Cycle(data);
            if(MonthlyAttendanceContract.Id(data,"policyVersion")!=action.Target||CycleId!=(string)request["cycleId"]||Kind!=(action.Kind=="monthly-auto"?"NATURAL":"RECOVERY")||!GameApiJson.Boolean(data,"refreshRequired"))throw new FormatException();
            if(MonthlyAttendanceContract.Revision(data,"revision")<1)throw new FormatException();foreach(var field in new[]{"claimId","rewardTransactionId","accountMutationId","economyTransactionId"})GameApiJson.String(data,field);
            if((string)data["rewardTransactionId"]!=ClaimId)throw new FormatException();AccountStateSnapshot.Number(data,"serverClaimedAt");MonthlyAttendanceContract.Range(data,"sourceDayIndex",0,30);
            var completed=MonthlyAttendanceContract.Range(data,"completedCells",0,28);if(data["cell"]?.Type==JTokenType.Null){if(completed!=28||Kind!="NATURAL")throw new FormatException();}else if(MonthlyAttendanceContract.Range(data,"cell",1,28)!=completed)throw new FormatException();
            var rewards=RecruitmentJson.Array(data,"rewardLines");foreach(var r in rewards)MonthlyAttendanceContract.Reward((JObject)r);if((Cell==null)!=(rewards.Count==0))throw new FormatException();
            if(data["wallet"] is JObject wallet){if(!GameApiJson.Boolean(wallet,"accepted"))throw new FormatException();MonthlyAttendanceContract.Id(wallet,"policyVersion");GameApiJson.String(wallet,"currencyId");if(GameApiJson.String(wallet,"operationId")!=ClaimId)throw new FormatException();
                if(checked(AccountStateSnapshot.Number(wallet,"freeBalance")+AccountStateSnapshot.Number(wallet,"paidBalance"))!=AccountStateSnapshot.Number(wallet,"totalBalance")||AccountStateSnapshot.Number(wallet,"paidGranted")!=0||AccountStateSnapshot.Number(wallet,"freeGranted")!=rewards.Where(r=>(string)r["kind"]=="CRYSTAL").Sum(r=>(long)r["quantity"]))throw new FormatException();
                var paid=AccountStateSnapshot.Number(wallet,"paidSpent");var free=AccountStateSnapshot.Number(wallet,"freeSpent");
                if(Kind=="RECOVERY"?(paid!=(long)request["confirmedPaid"]||free+paid!=100||(string)wallet["policyVersion"]!=(string)request["walletPolicyVersion"]):(paid!=0||free!=0))throw new FormatException();
            }else if(data["wallet"]?.Type!=JTokenType.Null||Kind=="RECOVERY"||rewards.Any(r=>(string)r["kind"]=="CRYSTAL"))throw new FormatException();
        }
    }
}
