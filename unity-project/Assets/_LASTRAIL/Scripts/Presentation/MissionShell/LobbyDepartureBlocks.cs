using System;
using System.Linq;
using LastRail.Battle;

namespace LastRail.Presentation.MissionShell
{
    public sealed partial class LobbyCommandDeck
    {
        DepartureBlockState departureBlocks;
        // Rebuild only when observed blockers change. Existing owners retain state and execution rules.
        DepartureBlockState CurrentDepartureBlocks()
        {
            var observed=DepartureBlockProjection.Read(accountState,localDeparture,departureOperation,DepartureContract.Risks[departureRisk],CanSelectOperation(departureOperation));
            if(departureBlocks==null||departureBlocks.Flags!=observed.Flags||departureBlocks.PreparationNotice!=observed.PreparationNotice||departureBlocks.SelectionNotice!=observed.SelectionNotice)departureBlocks=observed;
            return departureBlocks;
        }
        async void ResolveDepartureBlock(string code)
        {
            if(!isActiveAndEnabled||!departureDialog)return;
            accountState?.SynchronizeSession();
            if(!CanResolveDepartureBlock(code))return;
            var reason=CurrentDepartureBlocks().Reasons.FirstOrDefault(x=>x.Code==code);if(reason==null)return;
            switch(reason.Action){
                case DepartureBlockAction.Outcome:
                    if(localDeparture?.CanRetryStart==true&&!localDeparture.Busy)await localDeparture.StartAsync();
                    else if(accountState?.CanRetryDepartureLookup==true)await accountState.LookupDepartureStartAsync();
                    else if(accountState?.RunSelection.Kind!=LobbyRunSelectionKind.None)OpenOperations();
                    else if(accountState?.CanRetryDeparture==true)await accountState.RetryPendingAsync();
                    break;
                case DepartureBlockAction.Account:account?.Invoke();break;
                case DepartureBlockAction.Storage:
                    if(localDeparture?.NeedsReceiptSave==true&&!localDeparture.Busy)await localDeparture.StartAsync();
                    else if(accountState?.CanRetryDepartureLookup==true)await accountState.LookupDepartureStartAsync();
                    break;
                case DepartureBlockAction.Formation:if(accountState?.Busy!=true&&accountState?.Pending==null&&localDeparture?.CanChangeSelection!=false)OpenPreparationPage(LobbyPage.Formation);break;
                case DepartureBlockAction.Train:if(accountState?.Busy!=true&&accountState?.Pending==null&&localDeparture?.CanChangeSelection!=false)OpenPreparationPage(LobbyPage.Train);break;
                case DepartureBlockAction.Operations:if(localDeparture?.CanChangeSelection!=false)OpenOperations();break;
                case DepartureBlockAction.Assets:if(localDeparture?.CanRetryEntry==true)localDeparture.RetryEntry();else if(localDeparture?.PreparationBlock==Ce03PreparationBlock.Verification)PreviewDeparture();break;
                case DepartureBlockAction.Support:if(localDeparture?.CanChangeSelection!=false)OpenPreparationSupport();break;
                case DepartureBlockAction.Preview:PreviewDeparture();break;
                case DepartureBlockAction.Refresh:if(accountState?.Busy!=true)RefreshDeparture();break;
            }
        }
        bool CanResolveDepartureBlock(string code)
        {
            var reason=CurrentDepartureBlocks().Reasons.FirstOrDefault(x=>x.Code==code);if(reason==null)return false;
            bool canChange=accountState!=null&&!accountState.Busy&&accountState.Pending==null&&localDeparture?.CanChangeSelection!=false;
            switch(reason.Action){
                case DepartureBlockAction.Outcome:return localDeparture?.CanRetryStart==true||accountState?.CanRetryDepartureLookup==true||accountState?.CanRetryDeparture==true||accountState!=null&&!accountState.Busy&&accountState.RunSelection.Kind!=LobbyRunSelectionKind.None;
                case DepartureBlockAction.Account:return account!=null;
                case DepartureBlockAction.Storage:return localDeparture?.NeedsReceiptSave==true&&localDeparture.CanRetryStart||accountState?.CanRetryDepartureLookup==true;
                case DepartureBlockAction.Formation:case DepartureBlockAction.Train:return canChange;
                case DepartureBlockAction.Operations:return localDeparture?.CanChangeSelection!=false;
                case DepartureBlockAction.Assets:return localDeparture?.CanRetryEntry==true||localDeparture?.PreparationBlock==Ce03PreparationBlock.Verification&&accountState?.CanPreviewDeparture==true&&CanSelectOperation(departureOperation);
                case DepartureBlockAction.Support:return localCommandClient!=null&&localDeparture?.CanChangeSelection!=false;
                case DepartureBlockAction.Preview:return accountState?.CanPreviewDeparture==true&&CanSelectOperation(departureOperation)&&localDeparture?.Busy!=true;
                case DepartureBlockAction.Refresh:return accountState!=null&&!accountState.Busy;
                default:return false;
            }
        }
    }
}
