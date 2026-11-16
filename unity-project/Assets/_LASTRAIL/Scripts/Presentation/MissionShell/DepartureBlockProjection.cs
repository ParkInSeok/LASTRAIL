using System;
using LastRail.Battle;
namespace LastRail.Presentation.MissionShell {
 // Observes existing owners; this display projection never grants entry or changes authoritative state.
 public static class DepartureBlockProjection {
  public static DepartureBlockState Read(AccountScreenRuntime state,Ce03LocalDeparture localDeparture,string operation,string risk,bool operationAvailable){
            var flags=DepartureBlockFlags.None;
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
            if(!operationAvailable)flags|=DepartureBlockFlags.Progress;
            if(localDeparture!=null){
                if(localDeparture.Busy||localDeparture.CanRetryStart&&!localDeparture.NeedsReceiptSave)flags|=DepartureBlockFlags.Outcome;
                if(localDeparture.NeedsReceiptSave)flags|=DepartureBlockFlags.Storage;
                if(localDeparture.EntryStatus==Ce03EntryStatus.UpdateRequired)flags|=DepartureBlockFlags.Unsupported;
                if(localDeparture.EntryStatus==Ce03EntryStatus.Preparing||localDeparture.EntryStatus==Ce03EntryStatus.PreparationFailed||localDeparture.PreparationBlock==Ce03PreparationBlock.Verification)flags|=DepartureBlockFlags.Assets;
                if(localDeparture.PreparationBlock==Ce03PreparationBlock.SupportSelection)flags|=DepartureBlockFlags.Support;
                if(!localDeparture.HasConfirmedStart&&!localDeparture.CanStart&&localDeparture.PreparationBlock==Ce03PreparationBlock.None)flags|=DepartureBlockFlags.Quote;
            }else{
                var quote=state?.Departure;bool matches=quote!=null&&quote.OperationId==operation&&quote.Risk==risk;
                if(matches&&quote.Combat?.CanStart==false)flags|=DepartureBlockFlags.Unsupported;
                if(!matches||quote.ExpiresAt<=DateTimeOffset.UtcNow||state.DepartureSelectionNotice!=null)flags|=DepartureBlockFlags.Quote;
                if(matches&&quote.AvailableFuel<quote.FuelCost)flags|=DepartureBlockFlags.Fuel;
                if(state?.BattleEntryReady?.Invoke()!=true)flags|=DepartureBlockFlags.Assets;
            }
            var detail=localDeparture!=null&&(localDeparture.PreparationBlock!=Ce03PreparationBlock.None||localDeparture.EntryStatus==Ce03EntryStatus.Preparing||localDeparture.EntryStatus==Ce03EntryStatus.PreparationFailed)?localDeparture.Notice:null;
            var selectionNotice=localDeparture==null?(state?.DepartureSelectionNotice??state?.DepartureConditionNotice):null;
            return DepartureBlockState.FromObserved((int)flags,detail,selectionNotice);
  }
 }
}
