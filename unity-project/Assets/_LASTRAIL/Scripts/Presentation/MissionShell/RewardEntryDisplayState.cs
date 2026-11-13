namespace LastRail.Presentation.MissionShell
{
    // Display policy only. The existing reward controllers retain all transaction authority.
    public sealed class RewardEntryDisplayState
    {
        public bool Visible {get;}
        public string Label {get;}
        public string Notice {get;}
        public bool Recovery {get;}
        RewardEntryDisplayState(AccountScreenRuntime owner,string feature,string run)
        {
            if(owner==null){Notice="추가 보상 상태를 확인할 수 없습니다.";return;}
            var pending=owner.Pending;
            bool approval=pending?.Kind=="reward-approval"&&pending.Target==(feature=="OFFLINE"?"OFFLINE":run);
            Recovery=approval||(feature=="OFFLINE"?owner.HasPendingAd:owner.CombatAds.HasPending(run));
            if(Recovery){Visible=true;Label="처리 결과 확인";Notice="기존 요청의 처리 결과를 확인합니다.";return;}
            var entitlement=owner.AdSkip.Snapshot;
            string availability=feature=="OFFLINE"?entitlement?.OfflineAvailability:entitlement?.CombatAvailability;
            if(entitlement?.Owned==true)
            {
                Visible=availability=="AVAILABLE";Label="광고 없이 룰렛";
                Notice=Visible?"이용권 보유 · 기존 룰렛 혜택":"이용권 보유 · 추가 보상 연결 확인 필요";return;
            }
            var policy=owner.RuntimeAds.Status;
            // A production start-authorization/SDK adapter is required before new advertising is exposed.
            Visible=entitlement!=null&&policy==AdRuntimePolicyStatus.Enabled&&owner.AdProviderAvailable;
            Label="광고 룰렛 · 총 2~4배";
            Notice=policy==AdRuntimePolicyStatus.Checking||owner.AdSkip.Loading?"추가 보상 상태 확인 중":entitlement==null?"이용권 상태 확인 필요":policy==AdRuntimePolicyStatus.Disabled?"":policy==AdRuntimePolicyStatus.Unknown?"광고 기능 상태를 확인할 수 없습니다.":Visible?"광고 시청 후 기존 룰렛을 이용합니다.":"현재 광고 기능을 이용할 수 없습니다.";
        }
        public static RewardEntryDisplayState Offline(AccountScreenRuntime owner)=>new RewardEntryDisplayState(owner,"OFFLINE",null);
        public static RewardEntryDisplayState Combat(AccountScreenRuntime owner,string run)=>new RewardEntryDisplayState(owner,"COMBAT",run);
    }
}
