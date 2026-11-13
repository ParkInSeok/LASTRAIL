using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell
{
    public sealed class MonthlyAttendanceCellDisplay
    {
        public int Day { get; }
        public bool Received { get; }
        public string Reward { get; }
        public string Symbol { get; }
        public MonthlyAttendanceCellDisplay(JObject cell)
        {
            Day=(int)cell["cell"];Received=cell["receipt"] is JObject;var rewards=(JArray)cell["rewards"];
            Reward=rewards.Count==0?"보상 준비 중":string.Join("\n",rewards.Cast<JObject>().Select(MonthlyAttendanceContract.RewardLabel));
            var reward=rewards.First as JObject;Symbol=(string)reward?["kind"]=="CRYSTAL"?"CRYSTAL":(string)reward?["id"];
        }
    }
    // Read-only projection. AccountScreenRuntime remains the receipt, eligibility and wallet owner.
    public sealed class MonthlyAttendanceDisplayState
    {
        public IReadOnlyList<MonthlyAttendanceCellDisplay> Cells { get; }
        public string CountText { get; }
        public string CycleText { get; }
        public string Notice { get; }
        public string RecoveryText { get; }
        public string RetryText { get; }
        public bool CanRetry { get; }
        public bool CanRecover { get; }
        public bool HasCurrentCalendar { get; }
        public bool TodayAttendanceConfirmed { get; }
        public MonthlyAttendanceDisplayState(AccountScreenRuntime owner)
        {
            var data=owner?.MonthlyReady==true&&!owner.MonthlyLoading?owner.MonthlyAttendance:null;HasCurrentCalendar=data!=null;TodayAttendanceConfirmed=data?.TodayAttended==true;
            Cells=(data==null?new List<MonthlyAttendanceCellDisplay>():data.Cells.Cast<JObject>().Select(c=>new MonthlyAttendanceCellDisplay(c)).ToList()).AsReadOnly();
            CountText=data==null?"— / 28일":data.Completed+" / 28일";CycleText=data?.CycleId??"출석 기록 확인";
            RecoveryText=data==null?"복구 횟수 확인 필요":"복구 "+data.RecoveryUsed+" / 7회";
            Notice=owner?.MonthlyLoading==true?"출석 정보를 확인하고 있습니다.":owner?.MonthlyNotice??"출석 정보를 불러올 수 없습니다.";
            var receipt=owner?.LastMonthlyReceipt;
            if(data!=null&&receipt?.CycleId==data.CycleId)Notice+=receipt.Cell.HasValue?"\n"+string.Join(" · ",receipt.Rewards.Cast<JObject>().Select(MonthlyAttendanceContract.RewardLabel)):"\n이번 달 28일 출석을 모두 완료했습니다.";
            if(!string.IsNullOrEmpty(owner?.ActionNotice))Notice+="\n"+owner.ActionNotice;
            bool pending=owner?.Pending?.Kind.StartsWith("monthly-",System.StringComparison.Ordinal)==true;
            RetryText=pending?"처리 결과 재확인":"다시 확인";CanRetry=owner!=null&&!owner.Busy&&(pending||!owner.MonthlyLoading);
            CanRecover=owner!=null&&!owner.Busy&&owner.Pending==null&&data?.CanRecover==true;
        }
    }
}

