using System;
using UnityEngine;
namespace LastRail.Presentation.MissionShell
{
    public sealed partial class LobbyCommandDeck
    {
        bool passDialog,passObjectiveView;int passPolicyPage;string selectedPassPolicy;SeasonPassView seasonPassView;
        public void OpenPass(){passPolicyPage=0;passObjectiveView=false;ShowPass();RefreshPass();}
        async void RefreshPass(){if(accountState!=null)await accountState.SyncRecurringAsync();}
        void PassChanged(){if(passDialog&&dialog!=null)DrawPass();}
        void ShowPass()=>OpenResourceDialog(new[]{"NoncombatPass/","EntryPopup/Rewards/","EntryPopup/roulette-reward-icons"},ShowPassPrepared);
        void ShowPassPrepared()
        {
            ShowInfo("시즌 패스","");passDialog=true;dialog.sizeDelta=new Vector2(860,1355);dialog.GetComponent<UnityEngine.UI.Image>().color=Color.clear;
            dialog.parent.GetComponent<UnityEngine.UI.Image>().color=new Color(.015f,.018f,.02f,1);
            foreach(Transform child in dialog)child.gameObject.SetActive(false);
            seasonPassView=SeasonPassView.Create(dialog,font);seasonPassView.Bind(CloseDialog,RefreshPassAction,ClaimAllPassRewards,CyclePassSeason,SelectPassTab,ClaimPassReward);DrawPass();EvaluateLayout();
        }
        void DrawPass()
        {
            if(!passDialog||dialog==null||seasonPassView==null)return;var display=new SeasonPassDisplayState(accountState,passPolicyPage,passObjectiveView);selectedPassPolicy=display.PolicyId;
            if(display.PolicyCount>0)passPolicyPage=Math.Min(passPolicyPage,display.PolicyCount-1);seasonPassView.Render(display);
        }
        void CyclePassSeason(){int count=seasonPassView?.CurrentState?.PolicyCount??0;if(count>0){passPolicyPage=(passPolicyPage+1)%count;DrawPass();}}
        void SelectPassTab(bool objectives){passObjectiveView=objectives;DrawPass();}
        async void ClaimPassReward(PassRewardDisplay reward)
        {var state=accountState;if(state!=null&&reward.PolicyId==selectedPassPolicy&&state.CanClaimRecurring("recurring-pass",reward.PolicyId,reward.Payload))await state.ClaimRecurringAsync("recurring-pass",reward.PolicyId,reward.Payload);}
        async void RefreshPassAction()
        {var state=accountState;if(state==null)return;if(state.Pending?.Kind.StartsWith("recurring-",StringComparison.Ordinal)==true)await state.RetryPendingAsync();else await state.SyncRecurringAsync();}
        async void ClaimAllPassRewards()
        {
            var state=accountState;if(state==null||state.Busy)return;
            if(state.Pending?.Kind=="recurring-bulk"){if(state.HasPassClaimAllResult)OpenPassClaimAllResults();else OpenBulkResults();}
            else if(selectedPassPolicy!=null){var operation=state.ClaimAllPassAsync(selectedPassPolicy);OpenPassClaimAllResults();await operation;}
        }
    }
}
