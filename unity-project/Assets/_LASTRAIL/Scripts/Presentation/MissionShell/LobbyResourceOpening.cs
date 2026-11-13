using System;
using System.Threading;
using LastRail.Resources;
using UnityEngine;

namespace LastRail.Presentation.MissionShell
{
    public sealed partial class LobbyCommandDeck
    {
        static readonly string[] RouletteResources={"EntryPopup/roulette-room-v02","EntryPopup/roulette-room","EntryPopup/roulette-button-glass-v01","EntryPopup/roulette-glass-v02","EntryPopup/roulette-frame-v01","EntryPopup/roulette-ring","EntryPopup/roulette-hub-v01","EntryPopup/roulette-pointer","EntryPopup/Rewards/","EntryPopup/roulette-reward-icons","NoncombatCodex/codex-dark-steel-surface-v01"};
        CancellationTokenSource resourceDialogRequest;
        void CancelResourceDialog(){var request=resourceDialogRequest;resourceDialogRequest=null;request?.Cancel();}
        void OpenResourceDialog(string[] selectors,Action build){_ = OpenResourceDialogAsync(selectors,build);}
        async System.Threading.Tasks.Task<bool> OpenResourceDialogAsync(string[] selectors,Action build)
        {
            CancelResourceDialog();
            var request=resourceDialogRequest=new CancellationTokenSource();
            var owner=accountState;
            PreparedContent.Scope scope=null;
            try
            {
                scope=await PreparedContent.PrepareAsync(selectors,request.Token);
                if(this==null||!isActiveAndEnabled||request.IsCancellationRequested||resourceDialogRequest!=request||owner!=accountState)return false;
                resourceDialogRequest=null;
                build();
                if(dialog!=null){PreparedContent.Attach(dialog.parent.gameObject,scope);scope=null;}
                return dialog!=null;
            }
            catch(OperationCanceledException){}
            catch(Exception error){Debug.LogException(error);if(this!=null&&isActiveAndEnabled&&!request.IsCancellationRequested)ShowInfo("리소스 준비 실패","화면을 다시 열어 주세요.");}
            finally{if(resourceDialogRequest==request)resourceDialogRequest=null;scope?.Dispose();request.Dispose();}
            return false;
        }
    }
}
