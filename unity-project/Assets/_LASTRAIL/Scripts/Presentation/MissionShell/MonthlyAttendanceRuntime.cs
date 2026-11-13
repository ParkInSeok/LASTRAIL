using System;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell
{
    public sealed class MonthlyRecoveryConfirmation
    {
        internal readonly object Owner;internal readonly long Generation;internal readonly string Activity,Body;
        public int Paid {get;}public int Free=>100-Paid;
        internal MonthlyRecoveryConfirmation(object owner,long generation,string activity,string body,int paid){Owner=owner;Generation=generation;Activity=activity;Body=body;Paid=paid;}
    }
    public sealed partial class AccountScreenRuntime
    {
        readonly IMonthlyAttendanceRegistry monthlyRegistry;
        long monthlyRequest,monthlyEntry;bool monthlyEntering;
        public MonthlyAttendanceSnapshot MonthlyAttendance {get;private set;}
        public MonthlyAttendanceReceipt LastMonthlyReceipt {get;private set;}
        public bool MonthlyLoading {get;private set;}
        public bool MonthlyReady {get;private set;}
        public string MonthlyNotice {get;private set;}="출석 정보를 불러올 수 없습니다.";
        public string MonthlyError {get;private set;}
        string MonthlyActivityId=>monthlyRegistry?.CurrentActivityId;
        void ResetMonthlyAttendance(){monthlyRequest++;monthlyEntry++;monthlyEntering=false;MonthlyAttendance=null;LastMonthlyReceipt=null;MonthlyReady=false;MonthlyLoading=false;MonthlyNotice="출석 정보를 불러올 수 없습니다.";MonthlyError=null;}
        public async Task RefreshMonthlyAttendanceAsync()
        {
            SynchronizeSession();var original=owner;var generation=ownerGeneration;var request=++monthlyRequest;if(!Current(original,generation))return;
            MonthlyReady=false;MonthlyLoading=true;MonthlyError=null;Changed?.Invoke();
            try{
                var activity=MonthlyActivityId;MonthlyAttendanceDiscovery discovery=null;
                if(monthlyRegistry==null){
                    var found=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("GET","/v1/attendance/monthly/current",null,null),original.AccessToken));
                    if(!Current(original,generation)||request!=monthlyRequest)return;
                    if(found.IsNetworkError){MonthlyError="network_error";MonthlyNotice="출석 서버에 연결하지 못했습니다. 다시 확인해 주세요.";return;}
                    discovery=new MonthlyAttendanceDiscovery(found.BodyJson,found.StatusCode);
                    if(!discovery.CanRead){MonthlyAttendance=null;MonthlyError=discovery.Error??discovery.Status;MonthlyNotice=discovery.Status=="NOT_CONFIGURED"||discovery.Status=="OUTSIDE_WINDOW"?"진행 중인 출석이 없습니다.":"출석 정보를 확인하지 못했습니다. 다시 확인해 주세요.";return;}
                    activity=discovery.ActivityId;
                }
                if(string.IsNullOrEmpty(activity)){MonthlyAttendance=null;MonthlyError="activity_registry_unavailable";MonthlyNotice="진행 중인 출석 정보를 불러올 수 없습니다.";return;}
                MonthlyAttendanceContract.Id(new JObject{["id"]=activity},"id");
                var reply=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("GET","/v1/attendance/"+activity+"/monthly",null,null),original.AccessToken));
                if(!Current(original,generation)||request!=monthlyRequest||monthlyRegistry!=null&&MonthlyActivityId!=activity)return;
                if(reply.IsNetworkError||reply.StatusCode!=200){try{MonthlyError=GameApiJson.String(GameApiJson.ReadObject(reply.BodyJson),"errorCode");}catch{MonthlyError="read_failed";}MonthlyNotice="출석 정보를 불러오지 못했습니다. 다시 확인해 주세요.";return;}
                var parsed=new MonthlyAttendanceSnapshot(reply.BodyJson,activity);if(discovery!=null&&parsed.CycleId!=discovery.CycleId){MonthlyAttendance=null;MonthlyError="attendance_cycle_changed";MonthlyNotice="출석 기간이 변경되었습니다. 다시 확인해 주세요.";return;}
                MonthlyAttendance=parsed;MonthlyReady=true;
                MonthlyNotice=MonthlyAttendance.UnavailableReason!=null?"출석 보상을 준비하고 있습니다.":!MonthlyAttendance.Active?"진행 중인 출석 기간이 아닙니다.":MonthlyAttendance.TodayAttended?"오늘 출석을 확인했습니다.":"오늘 출석을 확인하고 있습니다.";
            }catch{if(Current(original,generation)&&request==monthlyRequest){MonthlyError="invalid_response";MonthlyNotice="출석 정보를 확인하지 못했습니다.";}}
            finally{if(Current(original,generation)&&request==monthlyRequest){MonthlyLoading=false;Changed?.Invoke();}}
        }
        // Lobby entry initiates natural attendance; opening/reading the calendar never grants.
        public async Task EnterMonthlyAttendanceAsync()
        {
            SynchronizeSession();if(monthlyEntering||Busy)return;var original=owner;var generation=ownerGeneration;var entry=++monthlyEntry;monthlyEntering=true;
            try{
                if(Pending?.Kind=="monthly-auto"){await SubmitMonthlyAttendanceAsync(Pending.Kind,Pending.Target,Pending.Payload);if(!Current(original,generation)||Pending!=null||ActionErrorCode!=null)return;}
                await RefreshMonthlyAttendanceAsync();if(!Current(original,generation)||Busy||Pending!=null||!MonthlyReady||MonthlyAttendance?.CanAuto!=true)return;
                var data=MonthlyAttendance;var body=new JObject{["policyVersion"]=data.ActivityId,["cycleId"]=data.CycleId,["expectedRevision"]=data.Revision};
                await SubmitMonthlyAttendanceAsync("monthly-auto",data.ActivityId,body.ToString());
            }finally{if(entry==monthlyEntry)monthlyEntering=false;}
        }
        public MonthlyRecoveryConfirmation PrepareMonthlyRecovery()
        {
            SynchronizeSession();if(Busy||Pending!=null||MonthlyLoading||!MonthlyReady||MonthlyAttendance?.CanRecover!=true)return null;var data=MonthlyAttendance;var quote=data.Quote;
            var body=new JObject{["policyVersion"]=data.ActivityId,["cycleId"]=data.CycleId,["expectedRevision"]=data.Revision,["walletPolicyVersion"]=(string)quote["walletPolicyVersion"],["confirmedPaid"]=(int)quote["paidSpent"]};
            return new MonthlyRecoveryConfirmation(owner,ownerGeneration,data.ActivityId,body.ToString(),(int)quote["paidSpent"]);
        }
        public Task RecoverMonthlyAttendanceAsync(MonthlyRecoveryConfirmation quote,bool paidConfirmed)
        {
            SynchronizeSession();if(quote==null||!ReferenceEquals(quote.Owner,owner)||quote.Generation!=ownerGeneration||Busy||Pending!=null||quote.Paid>0&&!paidConfirmed)return Task.CompletedTask;
            return SubmitMonthlyAttendanceAsync("monthly-recovery",quote.Activity,quote.Body);
        }
        async Task SubmitMonthlyAttendanceAsync(string kind,string activity,string payload)
        {
            SynchronizeSession();var original=owner;var generation=ownerGeneration;if(writing||!Current(original,generation))return;
            writing=true;MonthlyError=null;ActionErrorCode=null;ActionNotice="출석 처리 결과를 확인하고 있습니다.";Changed?.Invoke();var refresh=false;
            try{
                MonthlyAttendanceContract.Request(kind,activity,payload);if(journal==null)throw new InvalidOperationException();var action=journal.Read(original.AccountId);
                if(action!=null&&(action.Kind!=kind||action.Target!=activity||action.Payload!=payload)){Pending=action;ActionNotice="이전 요청 결과를 먼저 확인해 주세요.";return;}
                if(action==null){action=new PendingAccountAction(original.AccountId,kind,activity,0,Guid.NewGuid().ToString("N"),payload);if(!journal.Save(action))throw new InvalidOperationException();}
                Pending=action;monthlyRequest++;MonthlyReady=false;MonthlyLoading=false;
                var reply=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST","/v1/attendance/"+activity+(kind=="monthly-auto"?"/auto-attend":"/recover-next"),payload,new[]{new RunApiHttpHeader("Idempotency-Key",action.Key)}),original.AccessToken));
                if(!Current(original,generation))return;
                if(!reply.IsNetworkError&&reply.StatusCode==200){
                    var receipt=new MonthlyAttendanceReceipt(reply.BodyJson,action);if(!journal.Remove(action))throw new InvalidOperationException();Pending=null;LastMonthlyReceipt=receipt;refresh=true;ActionNotice=kind=="monthly-auto"?"출석 처리를 확인했습니다.":"출석 복구를 확인했습니다.";
                }else if(!reply.IsNetworkError&&(reply.StatusCode==400||reply.StatusCode==409)){
                    var error=GameApiJson.ReadObject(reply.BodyJson);if(GameApiJson.Boolean(error,"accepted"))throw new FormatException();MonthlyError=ActionErrorCode=GameApiJson.String(error,"errorCode");
                    if(!journal.Remove(action))throw new InvalidOperationException();Pending=null;refresh=true;ActionNotice="출석 조건이 변경되었습니다. 최신 상태를 확인한 뒤 다시 진행해 주세요.";
                }else{
                    try{MonthlyError=GameApiJson.String(GameApiJson.ReadObject(reply.BodyJson),"errorCode");}catch{MonthlyError="response_unknown";}
                    ActionNotice="출석 처리 결과 확인이 필요합니다. 같은 요청으로 재확인해 주세요.";
                }
            }catch{if(Current(original,generation)){MonthlyError="response_unknown";ActionNotice="출석 처리 결과를 확인하지 못했습니다. 같은 요청으로 재확인해 주세요.";}}
            finally{writing=false;if(!disposed)Changed?.Invoke();}
            if(refresh&&Current(original,generation))await Task.WhenAll(RefreshSharedAsync(),RefreshMonthlyAttendanceAsync());
        }
    }
}
