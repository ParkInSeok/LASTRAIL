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
            var flags=DepartureBlockFlags.None;var state=accountState;
            if(state==null)flags|=DepartureBlockFlags.Authentication;
            else{
                if(state.AccountStatus==AccountStateReadStatus.AuthRequired)flags|=DepartureBlockFlags.Authentication;
                if(state.DepartureReceiptNeedsSave)flags|=DepartureBlockFlags.Storage;
                var selection=state.RunSelection.Kind;
                if(state.Busy||state.DepartureLookupLoading||(state.Pending!=null&&!state.DepartureReceiptNeedsSave)||selection==LobbyRunSelectionKind.AwaitingConfirmation||selection==LobbyRunSelectionKind.Conflict||selection==LobbyRunSelectionKind.Recovery)flags|=DepartureBlockFlags.Outcome;
                if(state.AccountStatus==AccountStateReadStatus.VersionMismatch)flags|=DepartureBlockFlags.Unsupported;
                if(state.AccountStatus!=AccountStateReadStatus.Ready)flags|=DepartureBlockFlags.Account;
                else if(state.Account?.ActiveOperators.Count!=5)flags|=DepartureBlockFlags.Formation;
                if(!state.TrainLoadoutReady||state.TrainLoadout?.Loadout==null||state.TrainLoadout.OpenRun||state.TrainLoadout.ProgressError!=null||state.TrainLoadout.LoadoutError!=null)flags|=DepartureBlockFlags.Train;
                if(!state.UltimateReady||state.Ultimate==null||state.Ultimate.ChangeLocked||state.Ultimate.FormationRevision!=state.Account?.FormationRevision)flags|=DepartureBlockFlags.Ultimate;
            }
            if(!CanSelectOperation(departureOperation))flags|=DepartureBlockFlags.Progress;
            if(localDeparture!=null){
                if(localDeparture.Busy||localDeparture.CanRetryStart&&!localDeparture.NeedsReceiptSave)flags|=DepartureBlockFlags.Outcome;
                if(localDeparture.NeedsReceiptSave)flags|=DepartureBlockFlags.Storage;
                if(localDeparture.EntryStatus==Ce03EntryStatus.UpdateRequired)flags|=DepartureBlockFlags.Unsupported;
                if(localDeparture.EntryStatus==Ce03EntryStatus.Preparing||localDeparture.EntryStatus==Ce03EntryStatus.PreparationFailed||localDeparture.PreparationBlock==Ce03PreparationBlock.Verification)flags|=DepartureBlockFlags.Assets;
                if(localDeparture.PreparationBlock==Ce03PreparationBlock.SupportSelection)flags|=DepartureBlockFlags.Support;
                if(!localDeparture.HasConfirmedStart&&!localDeparture.CanStart&&localDeparture.PreparationBlock==Ce03PreparationBlock.None)flags|=DepartureBlockFlags.Quote;
            }else{
                var quote=state?.Departure;bool matches=quote!=null&&quote.OperationId==departureOperation&&quote.Risk==DepartureContract.Risks[departureRisk];
                if(matches&&quote.Combat?.CanStart==false)flags|=DepartureBlockFlags.Unsupported;
                if(!matches||quote.ExpiresAt<=DateTimeOffset.UtcNow||state.DepartureSelectionNotice!=null)flags|=DepartureBlockFlags.Quote;
                if(matches&&quote.AvailableFuel<quote.FuelCost)flags|=DepartureBlockFlags.Fuel;
                if(state?.BattleEntryReady?.Invoke()!=true)flags|=DepartureBlockFlags.Assets;
            }
            var detail=localDeparture!=null&&(localDeparture.PreparationBlock!=Ce03PreparationBlock.None||localDeparture.EntryStatus==Ce03EntryStatus.Preparing||localDeparture.EntryStatus==Ce03EntryStatus.PreparationFailed)?localDeparture.Notice:null;
            var selectionNotice=localDeparture==null?(state?.DepartureSelectionNotice??state?.DepartureConditionNotice):null;
            if(departureBlocks==null||departureBlocks.Flags!=(int)flags||departureBlocks.PreparationNotice!=detail||departureBlocks.SelectionNotice!=selectionNotice)departureBlocks=DepartureBlockState.FromObserved((int)flags,detail,selectionNotice);
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
