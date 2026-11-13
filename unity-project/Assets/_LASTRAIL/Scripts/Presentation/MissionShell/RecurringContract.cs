using System;
using System.Linq;
using System.Text.RegularExpressions;
using LastRail.Auth;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell
{
 public sealed class RecurringSnapshot
 {
  readonly JObject data;
  public long ServerTime=>(long)data["serverTime"];
  public JArray Policies=>(JArray)data["policies"].DeepClone();
  public JArray Awards=>(JArray)data["awards"].DeepClone();
  public JArray Mail=>(JArray)data["mail"].DeepClone();
  public JObject Periods=>(JObject)data["periods"].DeepClone();
  public bool SyncPending=>(bool)data["progressSyncPending"];
  public string CooperativeProgress=>(string)data["capabilities"]["cooperativeProgress"];
  public ObjectiveProgress Progress {get;}
  public RecurringSnapshot(string json,string account)
  {
   data=GameApiJson.ReadObject(json);RecurringContract.Accept(data);if(GameApiJson.String(data,"version")!="recurring-v1")throw new FormatException();AccountStateSnapshot.Number(data,"serverTime");GameApiJson.Boolean(data,"progressSyncPending");
   var period=GameApiJson.Object(data,"periods");var service=DateTimeOffset.FromUnixTimeMilliseconds(ServerTime).AddHours(5);var day=new DateTimeOffset(service.Year,service.Month,service.Day,0,0,0,TimeSpan.Zero).AddHours(-5);var start=new DateTimeOffset(service.Year,service.Month,1,0,0,0,TimeSpan.Zero).AddHours(-5);var end=start.AddHours(5).AddMonths(1).AddHours(-5);var dayIndex=(int)(day-start).TotalDays;
   if(GameApiJson.String(period,"cycleId")!=service.ToString("yyyy-MM")||AccountStateSnapshot.Number(period,"startAt")!=start.ToUnixTimeMilliseconds()||AccountStateSnapshot.Number(period,"endAt")!=end.ToUnixTimeMilliseconds()||AccountStateSnapshot.Number(period,"dayStart")!=day.ToUnixTimeMilliseconds()||AccountStateSnapshot.Number(period,"dayEnd")!=day.AddDays(1).ToUnixTimeMilliseconds()||AccountStateSnapshot.Number(period,"dayIndex")!=dayIndex||AccountStateSnapshot.Number(period,"weekStart")!=day.AddDays(-(((int)service.DayOfWeek+6)%7)).ToUnixTimeMilliseconds())throw new FormatException();
   if(dayIndex<28?period["passWeek"]?.Type!=JTokenType.Integer||(int)period["passWeek"]!=dayIndex/7:period["passWeek"]?.Type!=JTokenType.Null)throw new FormatException();
   var caps=GameApiJson.Object(data,"capabilities");foreach(var name in new[]{"ordinaryOperationBinding","storyDefeat","cooperativeParticipantProducer","cooperativeProgress"})GameApiJson.String(caps,name);GameApiJson.Boolean(caps,"existingOwnerFinishProducer");
   var policies=RecruitmentJson.Array(data,"policies");RecurringContract.Unique(policies,"policyId");foreach(JObject p in policies){MonthlyAttendanceContract.Id(p,"policyId");var system=RecurringContract.One(p,"system","QUESTS","PASS50");var from=AccountStateSnapshot.Number(p,"startAt");var to=AccountStateSnapshot.Number(p,"endAt");if(from>=to||GameApiJson.Boolean(p,"active")!=(from<=ServerTime&&ServerTime<to))throw new FormatException();
    var xp=MonthlyAttendanceContract.Range(p,"xp",0,5000);var earned=AccountStateSnapshot.Number(p,"earnedXp");if(xp!=Math.Min(5000,earned)||MonthlyAttendanceContract.Range(p,"level",0,50)!=xp/100)throw new FormatException();foreach(var flag in new[]{"premium","cosmeticBindingsComplete","purchaseConfigured"})GameApiJson.Boolean(p,flag);RecurringContract.One(p,"settlementStatus","NOT_ENDED","SOURCE_SYNC_PENDING","SETTLEMENT_PENDING","SETTLED_FOR_RECORDED_SOURCES");
    var rows=RecruitmentJson.Array(p,"rewardRows");if(rows.Count!=(system=="PASS50"?50:0))throw new FormatException();for(int i=0;i<rows.Count;i++){var row=(JObject)rows[i];if(AccountStateSnapshot.Number(row,"level")!=i+1||AccountStateSnapshot.Number(row,"xp")!=(i+1)*100)throw new FormatException();foreach(var track in new[]{"free","premium"}){RecurringContract.Rewards(RecruitmentJson.Array(row,track));RecurringContract.One(row,track+"State","EARNED","LOCKED","CLAIMED","PREMIUM_REQUIRED");}RecruitmentJson.NullableString(row,"cosmeticSlot");RecruitmentJson.NullableString(row,"cosmeticItemId");}
   }
   var awards=RecruitmentJson.Array(data,"awards");RecurringContract.Unique(awards,"id");foreach(JObject a in awards){MonthlyAttendanceContract.Id(a,"id");if(GameApiJson.String(a,"account_id")!=account)throw new FormatException();var id=MonthlyAttendanceContract.Id(a,"policy_id");if(!policies.Any(p=>(string)p["policyId"]==id&&(string)p["system"]==(string)a["system"]))throw new FormatException();RecurringContract.One(a,"objective","D1","D2","D3","W1","W2","W3");RecurringContract.One(a,"status","XP_APPLIED","CLAIMED","MAILED","MAIL_PENDING","EARNED");GameApiJson.String(a,"period_id");AccountStateSnapshot.Number(a,"earned_at");AccountStateSnapshot.Number(a,"period_end");AccountStateSnapshot.Number(a,"xp");RecurringContract.Rewards(RecruitmentJson.Array(a,"rewards"));}
   var mail=RecruitmentJson.Array(data,"mail");RecurringContract.Unique(mail,"award_id");foreach(JObject m in mail){var id=MonthlyAttendanceContract.Id(m,"award_id");if((string)m["account_id"]!=account||!awards.Any(a=>(string)a["id"]==id&&(string)a["system"]=="QUESTS"))throw new FormatException();var sent=AccountStateSnapshot.Number(m,"sent_at");var expires=AccountStateSnapshot.Number(m,"expires_at");if(expires-sent!=1209600000L)throw new FormatException();var status=RecurringContract.One(m,"status","CLAIMED","EXPIRED","CLAIMABLE");if(status=="CLAIMABLE"&&expires<=ServerTime||status=="EXPIRED"&&expires>ServerTime)throw new FormatException();}
   foreach(JObject e in RecruitmentJson.Array(data,"eventStatus")){GameApiJson.String(e,"status");AccountStateSnapshot.Number(e,"count");}
   foreach(JObject r in RecruitmentJson.Array(data,"grants"))RecurringContract.Grant(r,null);
   if(data["objectiveProgress"] is JObject progress)Progress=new ObjectiveProgress(progress);else if(data["objectiveProgress"]!=null)throw new FormatException();
  }
 }
 public static class RecurringContract
 {
  internal static void Accept(JObject r){if(!GameApiJson.Boolean(r,"accepted"))throw new FormatException();}
  internal static string One(JObject r,string field,params string[] allowed){var value=GameApiJson.String(r,field);if(!allowed.Contains(value))throw new FormatException();return value;}
  internal static void Unique(JArray rows,string field){if(rows.Select(r=>(string)r[field]).Distinct().Count()!=rows.Count)throw new FormatException();}
  internal static void Rewards(JArray lines){foreach(JObject r in lines)MonthlyAttendanceContract.Reward(r);}
  public static string Endpoint(string kind,string target,string payload)
  {
   if(kind=="recurring-login"){if(target!="login"||payload!=null)throw new FormatException();return "/v1/recurring/login";}
   MonthlyAttendanceContract.Id(new JObject{["id"]=target},"id");
   if(kind=="recurring-quest"||kind=="recurring-mail"){if(payload!=null)throw new FormatException();return (kind=="recurring-quest"?"/v1/quests/":"/v1/quest-mail/")+target+"/claim";}
   if(kind!="recurring-pass"||payload==null||payload.Length>300)throw new FormatException();var body=GameApiJson.ReadObject(payload);if(body.Count!=2)throw new FormatException();MonthlyAttendanceContract.Range(body,"level",1,50);One(body,"track","FREE","PREMIUM");return "/v1/passes/"+target+"/levels/claim";
  }
  public static JObject Grant(JObject r,PendingAccountAction action)
  {
   Accept(r);var right=GameApiJson.String(r,"rightKey");var policy=MonthlyAttendanceContract.Id(r,"policyId");var claim=GameApiJson.String(r,"claimId");if(GameApiJson.String(r,"rewardTransactionId")!=claim||!GameApiJson.Boolean(r,"refreshRequired"))throw new FormatException();GameApiJson.String(r,"accountMutationId");AccountStateSnapshot.Number(r,"serverClaimedAt");
   if(action!=null){if(action.Kind=="recurring-pass"){var b=GameApiJson.ReadObject(action.Payload);if(policy!=action.Target||!Regex.IsMatch(right,@"\Apass:[0-9]+:"+(int)b["level"]+":"+(string)b["track"]+@"\z"))throw new FormatException();}else if(right!="quest:"+action.Target)throw new FormatException();}
   var lines=RecruitmentJson.Array(r,"rewardLines");Rewards(lines);if(lines.Count==0)throw new FormatException();var free=lines.Where(l=>(string)l["kind"]=="CRYSTAL").Sum(l=>(long)l["quantity"]);
   if(r["wallet"] is JObject w){Accept(w);MonthlyAttendanceContract.Id(w,"policyVersion");GameApiJson.String(w,"currencyId");if(GameApiJson.String(w,"operationId")!=claim||AccountStateSnapshot.Number(w,"freeSpent")!=0||AccountStateSnapshot.Number(w,"paidSpent")!=0||AccountStateSnapshot.Number(w,"paidGranted")!=0||AccountStateSnapshot.Number(w,"freeGranted")!=free||checked(AccountStateSnapshot.Number(w,"freeBalance")+AccountStateSnapshot.Number(w,"paidBalance"))!=AccountStateSnapshot.Number(w,"totalBalance"))throw new FormatException();}else if(r["wallet"]?.Type!=JTokenType.Null||free!=0)throw new FormatException();return (JObject)r.DeepClone();
  }
  public static string Objective(string id)=>id=="D1"?"오늘 접속":id=="D2"?"작전 1회 종료":id=="D3"?"작전 2회 종료":id=="W1"?"3일 접속":id=="W2"?"작전 6회 종료":id=="W3"?"60웨이브 완료":"임무";
  public static string RewardText(JArray rewards)=>string.Join(" · ",rewards.Cast<JObject>().Select(MonthlyAttendanceContract.RewardLabel));
 }
}
