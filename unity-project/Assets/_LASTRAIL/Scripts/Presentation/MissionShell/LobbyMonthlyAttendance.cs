using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell
{
    public sealed partial class LobbyCommandDeck
    {
        bool monthlyDialog;string shownMonthlyClaim; MonthlyAttendanceView monthlyView; AccountScreenRuntime monthlyConfirmationOwner;
        public void OpenMonthlyAttendance(){ShowMonthlyAttendance();RefreshMonthlyCalendar();}
        async void RefreshMonthlyCalendar(){if(accountState!=null)await accountState.RefreshMonthlyAttendanceAsync();}
        void MonthlyChanged()
        {
            var receipt=accountState?.LastMonthlyReceipt;
            if(receipt==null)shownMonthlyClaim=null;
            if(!visualFixture&&receipt?.Kind=="NATURAL"&&shownMonthlyClaim!=receipt.ClaimId){shownMonthlyClaim=receipt.ClaimId;ShowMonthlyAttendance();return;}
            if(monthlyDialog&&dialog!=null)DrawMonthlyAttendance();
        }
        void ShowMonthlyAttendance()=>OpenResourceDialog(new[]{"NoncombatAttendance/","EntryPopup/popup-backplate","EntryPopup/Rewards/","EntryPopup/roulette-reward-icons"},ShowMonthlyAttendancePrepared);
        void ShowMonthlyAttendancePrepared()
        {
            ShowInfo("월간 출석","");monthlyDialog=true;dialog.sizeDelta=new Vector2(900,1280);
            dialog.parent.GetComponent<UnityEngine.UI.Image>().color=new Color(.015f,.018f,.02f,1);
            var background=Rect("MonthlyScreenMaterial",dialog.parent);Stretch(background);background.SetAsFirstSibling();var material=background.gameObject.AddComponent<RawImage>();material.texture=LastRail.Resources.PreparedContent.Load<Texture2D>("EntryPopup/popup-backplate");material.color=new Color(.32f,.35f,.37f,1);material.raycastTarget=false;
            dialog.GetComponent<UnityEngine.UI.Image>().color=Color.clear;
            foreach(Transform child in dialog)child.gameObject.SetActive(false);
            monthlyView=MonthlyAttendanceView.Create(dialog,font);monthlyView.Bind(CloseDialog,RetryMonthlyAttendance,BeginMonthlyRecovery);
            DrawMonthlyAttendance();EvaluateLayout();
        }
        void DrawMonthlyAttendance(){if(monthlyDialog&&dialog!=null)monthlyView?.Render(new MonthlyAttendanceDisplayState(accountState));}
        async void RetryMonthlyAttendance()
        {
            var state=accountState;if(state==null||state.Busy)return;
            if(state.Pending?.Kind.StartsWith("monthly-",System.StringComparison.Ordinal)==true)await state.RetryPendingAsync();
            else if(!state.MonthlyLoading)await state.EnterMonthlyAttendanceAsync();
        }
        async void BeginMonthlyRecovery()
        {
            var state=accountState;var quote=state?.PrepareMonthlyRecovery();if(quote==null)return;
            if(quote.Paid==0){await state.RecoverMonthlyAttendanceAsync(quote,false);return;}
            ShowInfo("출석 복구 확인","무료 크리스탈 "+quote.Free+"개와\n유료 크리스탈 "+quote.Paid+"개를 사용합니다.\n\n다음 출석 1일을 복구하시겠습니까?");
            monthlyConfirmationOwner=state;var confirmationDialog=dialog;
            Button("ConfirmPaidAttendance",dialog,60,540,630,75,"유료 "+quote.Paid+"개 사용하여 복구",async()=>{
                if(!isActiveAndEnabled||!ReferenceEquals(state,accountState)||!ReferenceEquals(state,monthlyConfirmationOwner)||dialog!=confirmationDialog)return;
                ShowMonthlyAttendance();await state.RecoverMonthlyAttendanceAsync(quote,true);
            });
        }
    }
}
