using System.Collections.Generic;
using System.Linq;
using LastRail.OfflineRewards;
namespace LastRail.Presentation.MissionShell
{
    // Existing account-owned preview and claim availability, projected for the basic supply view.
    public sealed class OfflineSupplyDisplayState
    {
        public IReadOnlyList<OfflineRewardLineDto> Rewards { get; }
        public string TimeText { get; }
        public string Notice { get; }
        public string StorageText { get; }
        public string ClaimText { get; }
        public string AdText { get; }
        public string AdNotice { get; }
        public bool CanOpenAd {get;}
        public bool ShowAdEntry {get;}
        public bool CanClaim { get; }
        public bool CanRefresh { get; }
        public bool HasPreview { get; }
        public OfflineSupplyDisplayState(AccountScreenRuntime owner)
        {
            var preview=owner?.OfflineLoading==true?null:owner?.Offline;HasPreview=preview!=null;
            Rewards=(preview==null?new List<OfflineRewardLineDto>():preview.RewardLines.ToList()).AsReadOnly();
            long seconds=preview?.ClaimableSeconds??0;
            TimeText=preview==null?"—":(seconds/3600).ToString("00")+":"+(seconds%3600/60).ToString("00")+":"+(seconds%60).ToString("00");
            StorageText=preview==null?"저장 상한 확인 필요":"저장 상한 "+preview.OfflineStorageCapHours+"시간";
            Notice=owner?.OfflineLoading==true?"보급 정보를 확인하고 있습니다.":!string.IsNullOrEmpty(owner?.OfflineNotice)?owner.OfflineNotice:preview==null?"보급 정보를 불러오지 못했습니다.":"기본 보급을 수령하세요.";
            if(!string.IsNullOrEmpty(owner?.ActionNotice))Notice+="\n"+owner.ActionNotice;
            bool pending=owner?.Pending?.Kind=="offline";ClaimText=pending?"결과 재확인":"기본 보급 받기";
            CanClaim=owner!=null&&!owner.Busy&&!owner.OfflineLoading&&(pending||owner.Pending==null&&preview?.PreviewStatus==OfflineRewardPreviewStatus.Available);
            CanRefresh=owner!=null&&!owner.Busy&&!owner.OfflineLoading;
            CanOpenAd=owner!=null&&!owner.Busy&&!owner.AdSkip.Loading;
            AdText=owner?.HasPendingAd==true?"광고 보급 결과 확인":owner?.AdProviderAvailable==true?"광고 보급 · 룰렛":"보급 룰렛 확인";
            AdNotice=owner?.AdIntent!=null?"가능 배율  "+string.Join(" · ",owner.AdIntent.Multipliers.Select(x=>x+"배")):owner?.AdProviderAvailable==true?"광고 시청 후 룰렛으로 배율이 결정됩니다.":"현재 광고 보급을 이용할 수 없습니다.";
            if(owner!=null&&!owner.HasPendingAd){
                if(owner.AdSkip.Loading){AdText="이용권 확인 중";AdNotice="기본 보급은 그대로 받을 수 있습니다.";}
                else if(owner.Pending?.Kind=="reward-approval"){AdText="이용권 사용 결과 확인";AdNotice=owner.AdSkipUse.Notice;}
                else if(owner.AdSkip.Snapshot==null){AdText="이용권 확인 · 룰렛";AdNotice="이용권 상태를 다시 확인해 주세요.";}
                else if(owner.AdSkip.Snapshot.Owned){AdText="광고 없이 보급 룰렛";AdNotice="이용권 보유 · 기존 확률로 룰렛을 이용합니다.";}
            }
            var entry=RewardEntryDisplayState.Offline(owner);ShowAdEntry=entry.Visible;CanOpenAd=ShowAdEntry&&owner?.Busy!=true;
            AdText=entry.Label??"";AdNotice=entry.Notice??"";
        }
    }
}
