using System;
using LastRail.Auth;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell
{
    public sealed class MonthlyAttendanceDiscovery
    {
        public string ActivityId {get;}
        public string CycleId {get;}
        public string Status {get;}
        public string Error {get;}
        public bool CanRead=>Status=="AVAILABLE"||Status=="REWARD_BINDING_UNAVAILABLE";
        public MonthlyAttendanceDiscovery(string json,int httpStatus)
        {
            var data=GameApiJson.ReadObject(json);if(GameApiJson.String(data,"version")!="monthly-attendance-discovery-v1")throw new FormatException();
            Status=GameApiJson.String(data,"status");Error=RecruitmentJson.NullableString(data,"errorCode");
            var ok=Status=="AVAILABLE"||Status=="REWARD_BINDING_UNAVAILABLE"||Status=="NOT_CONFIGURED"||Status=="OUTSIDE_WINDOW";
            var expected=ok?200:Status=="AMBIGUOUS_CONFIGURATION"||Status=="PINNED_POLICY_UNAVAILABLE"?409:Status=="INVALID_CONFIGURATION"?503:0;
            if(expected==0||httpStatus!=expected||GameApiJson.Boolean(data,"accepted")!=ok||GameApiJson.Boolean(data,"canAttend")!=(Status=="AVAILABLE"))throw new FormatException();
            var pinned=GameApiJson.Boolean(data,"pinned");ActivityId=RecruitmentJson.NullableString(data,"activityId");if(RecruitmentJson.NullableString(data,"policyVersion")!=ActivityId)throw new FormatException();
            if(ActivityId!=null)MonthlyAttendanceContract.Id(data,"activityId");if(CanRead&&ActivityId==null||!CanRead&&!pinned&&ActivityId!=null)throw new FormatException();
            var now=DateTimeOffset.FromUnixTimeMilliseconds(AccountStateSnapshot.Number(data,"serverTime"));var service=now.AddHours(5);var start=new DateTimeOffset(service.Year,service.Month,1,0,0,0,TimeSpan.Zero).AddHours(-5);var end=start.AddHours(5).AddMonths(1).AddHours(-5);
            CycleId=GameApiJson.String(data,"cycleId");if(CycleId!=service.ToString("yyyy-MM")||AccountStateSnapshot.Number(data,"startAt")!=start.ToUnixTimeMilliseconds()||AccountStateSnapshot.Number(data,"endAt")!=end.ToUnixTimeMilliseconds()||AccountStateSnapshot.Number(data,"dayIndex")!=(int)(now-start).TotalDays)throw new FormatException();
            if(CanRead&&(AccountStateSnapshot.Number(data,"definitionStartAt")>now.ToUnixTimeMilliseconds()||AccountStateSnapshot.Number(data,"definitionEndAt")<=now.ToUnixTimeMilliseconds()))throw new FormatException();
        }
    }
}
