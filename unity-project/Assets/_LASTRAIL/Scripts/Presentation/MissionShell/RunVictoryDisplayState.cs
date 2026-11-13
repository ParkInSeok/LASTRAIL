using System.Collections.Generic;
using System.Linq;
using LastRail.Narrative;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.MissionShell
{
    // Adapts the existing verified result owner, without inventing a second reward state machine.
    public sealed class RunVictoryDisplayState
    {
        public string Title { get; }
        public string Operation { get; }
        public RunResultHistoryDisplayState History { get; }
        public string HistoryNotice => History.HistoryNotice;
        public string RewardStatus { get; }
        public string Notice { get; }
        public string ClaimLabel { get; }
        public IReadOnlyList<string> Rewards { get; }
        public IReadOnlyList<RunVictoryRewardPresentation> RewardItems { get; }
        public bool HasRewardRead { get; }
        public bool CanClaim { get; }
        public bool CanRefresh { get; }
        public bool CanRecover { get; }
        public bool CanViewOperations { get; }
        public bool CanOpenAd {get;}
        public bool ShowAdEntry {get;}
        public string AdLabel {get;}
        public RunVictoryDisplayState(AccountScreenRuntime owner, string runId, IReadOnlyList<MissionShellMissionRowState> missions)
        {
            History=new RunResultHistoryDisplayState(owner,runId);
            bool loading=owner?.RunResultsLoading==true;
            var lifecycle=owner?.Lifecycle;
            bool matching=!loading&&lifecycle?.RunId==runId;
            bool completed=matching&&lifecycle.ConfirmedOutcome=="VICTORY";
            Title=loading?"결과 확인 중":completed?"작전 완료":"결과 확인 필요";
            var start=matching?lifecycle.StartResponse:null;string operation=(string)start?["operationId"];
            string label=missions?.FirstOrDefault(m=>m.MissionId==operation)?.DisplayLabel;
            string risk=(string)start?["risk"];
            Operation=operation==null?"작전 기록 확인 필요":(label??"작전 기록")+(risk==null?"":risk=="NORMAL"?" · 일반 작전":" · 위험 "+risk);
            CanRefresh=owner!=null&&!owner.Busy&&!owner.RunResultsLoading&&!owner.LifecycleLoading;
            CanClaim=owner?.CanClaimRunReward(runId)==true;
            CanRecover=owner?.CanRecoverFinish(runId)==true;
            bool pendingAd=owner?.CombatAds.HasPending(runId)==true;
            CanOpenAd=matching&&owner!=null&&!owner.Busy&&(pendingAd||completed&&owner.CanClaimRunReward(runId));
            AdLabel=pendingAd?"룰렛 결과 확인":owner?.Pending?.Kind=="reward-approval"?"이용권 사용 결과 확인":owner?.AdSkip.Snapshot?.Owned==true?"광고 없이 룰렛 · 총 2~4배":owner?.AdSkip.Snapshot==null?"이용권 확인 · 룰렛":"광고 룰렛 · 총 2~4배";
            var entry=RewardEntryDisplayState.Combat(owner,runId);ShowAdEntry=entry.Visible;CanOpenAd=CanOpenAd&&ShowAdEntry;AdLabel=entry.Label??"";
            CanClaim=CanClaim&&!entry.Recovery;
            var receipt=matching?owner.VerifiedRunReceipt:null;
            var adReceipt=owner?.CombatAds.Intent?.RunId==runId?owner.CombatAds.Intent.Result:null;
            var preview=matching&&owner.RunRewardPreview?.RunId==runId?owner.RunRewardPreview:null;
            JArray lines=adReceipt!=null?(JArray)adReceipt["rewardLines"]:receipt!=null?(JArray)receipt["rewardPlan"]["rewardLines"]:preview?.Lines;
            HasRewardRead=lines!=null;
            RewardItems=(lines==null?new List<RunVictoryRewardPresentation>():lines.Cast<JObject>().Select(line=>new RunVictoryRewardPresentation(line)).ToList()).AsReadOnly();
            Rewards=RewardItems.Select(line=>line.Label).ToList().AsReadOnly();
            RewardStatus=adReceipt!=null?"지급 완료":loading?"확인 중":receipt!=null?"지급 완료":matching&&lifecycle.RewardStatus=="EXPIRED"?"수령 기한 만료":preview!=null?"수령 전":"보상 확인 필요";
            if(owner?.CombatAds.Intent?.RunId==runId&&owner.CombatAds.Intent.Status=="EXPIRED"&&!owner.CombatAds.Intent.CanClaimBasic)RewardStatus="수령 기한 만료";
            Notice=loading?"전투·보상 기록을 확인하고 있습니다.":!string.IsNullOrEmpty(owner?.RunResultNotice)?owner.RunResultNotice:matching?lifecycle.Notice:"전투 기록을 추가로 확인해야 합니다.";
            if(lines!=null&&lines.Count==0)Notice+="\n보상 항목이 없습니다.";
            ClaimLabel=receipt!=null||adReceipt!=null?"지급 완료":owner?.SavedRewardClaim(runId)!=null?"지급 결과 재확인":"보상 받기";
            if(!ShowAdEntry&&!string.IsNullOrEmpty(entry.Notice))Notice+="\n"+entry.Notice;
            var progress=owner?.StoryArchive;
            CanViewOperations=completed&&owner!=null&&!owner.Busy&&progress?.Status==StoryArchiveReadStatus.Ready&&progress.DisplayState!=null;
            if(progress?.DisplayState!=null)
                Notice+="\n현재 진행 · "+progress.DisplayState.HighestStoryProgressLabel;
        }
    }
}
