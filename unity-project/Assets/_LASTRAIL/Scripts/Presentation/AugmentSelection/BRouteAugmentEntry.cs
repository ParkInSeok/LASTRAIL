using System;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using TMPro;
namespace LastRail.Presentation.AugmentSelection {
 public sealed partial class BRouteAugmentScreen {
  bool arrivalBusy;
  public string ArrivalState{get;private set;}="IDLE";
  // Scene controller entry. The original transition owner supplies its durable
  // ARRIVE command; this host never invents arrival evidence or an idempotency key.
  // Reentry uses the controller's retained receipt/grant and the acquired original.
  public async Task ArriveAsync(BRouteSessionController combat,BRouteCommand originalArrival,AugmentSelectionOwner owner,IGameApiAuthenticatedHttpTransport transport,Func<GameApiAuthSessionSnapshot> session,Func<bool> ownerCurrent,IAndroidKeystoreNativeStoragePort crypto,string journalPath,CombatO22ApplicationStore originals,Func<string,string,AugmentCardPresentation> definitions,TMP_FontAsset font){
   if(arrivalBusy)throw new InvalidOperationException("Original arrival is already in flight");
   if(combat==null||originalArrival==null||originalArrival.Kind!="ARRIVE"||owner==null||session==null||ownerCurrent==null||originals==null)throw new ArgumentException("Original arrival dependencies required");
   if(owner.AccountId!=originalArrival.Binding.AccountId||combat.Route?.RunId!=originalArrival.Binding.RunId)throw new ArgumentException("Arrival owner mismatch");
   Close();long stamp=generation;arrivalBusy=true;ArrivalState="PENDING";
   bool Current()=>this&&isActiveAndEnabled&&stamp==generation&&ownerCurrent();
   GameApiAuthSessionSnapshot Auth(){var a=session();if(!Current()||a==null||a.AccountId!=owner.AccountId||a.AccountStatus!="ACTIVE")throw new OperationCanceledException("Arrival owner changed");return a;}
   void RequireCurrent(){_ = Auth();}
   try{
    var http=new BRouteHttpClient(transport);var before=combat.Capture();BRouteCommandResult receipt=null;
    if(JToken.DeepEquals(before["lastCommand"],originalArrival.Capture())&&before["lastResult"] is JObject retained){
     var candidate=BRouteCommandResult.Read((int)before["lastResultStatus"],retained.ToString(),originalArrival);
     if(candidate.Committed&&!candidate.AugmentPending)receipt=candidate;
    }
    if(receipt==null){
     var auth=Auth();bool accepted=await combat.SendAsync(originalArrival,async()=>{var incoming=await http.SendAsync(originalArrival,auth.AccessToken);RequireCurrent();return incoming;});RequireCurrent();
     if(!accepted)throw new OperationCanceledException("Arrival was superseded");
     var settled=combat.Capture();receipt=BRouteCommandResult.Read((int)settled["lastResultStatus"],settled["lastResult"].ToString(),originalArrival);
    }
    RequireCurrent();
    if(!receipt.Committed){ArrivalState="SOURCE_UNAVAILABLE";return;}
    if(receipt.AugmentPending){ArrivalState="ISSUANCE_PENDING";return;}
    // The response has map progress, not the new complete node/source binding.
    // Only that genuinely missing boundary needs a route read; no grant GET.
    if((bool)combat.Capture()["routeRefreshRequired"]){var auth=Auth();if(!await combat.RefreshAsync(async()=>{var incoming=await http.ReadAsync(owner.AccountId,originalArrival.Binding.RunId,auth.AccessToken);RequireCurrent();return incoming;}))throw new OperationCanceledException("Route refresh superseded");RequireCurrent();}
    if(receipt.Augment==null||receipt.Augment.NoAward){ArrivalState=receipt.Augment?.NoAward==true?"NO_AWARD":"NO_SELECTION";return;}
    receipt.Augment.RequireEnvironment(owner.Environment);
    var grant=AugmentSelectionSnapshot.Parse(receipt.Augment.CaptureGrant().ToString());var route=combat.Route;var packet=route.Capture();
    if(route.MapRevision<receipt.RequiredMapRevision||(string)packet["phase"]!="ACTIVE"||(string)packet["runStatus"]!="ACTIVE"||(string)packet["currentNode"]?["nodeId"]!=grant.StageId)throw new InvalidOperationException("Arrival grant is no longer the active node");
    var acquired=originals.LoadForRun(owner.AccountId,route.RunId,owner.Environment,(string)packet["startSnapshotHash"]);RequireCurrent();
    if(acquired?.GrantEventId==grant.EventId){
     grant=AugmentSelectionSnapshot.Parse(acquired.SelectionJson);
     bool acquisitionOnly=route.NativeNode==null;
     combat.BeginAugmentSelection(grant.EventId);
     if(!acquisitionOnly){if(combat.CheckpointRecoveryRequired)combat.RecoverLocalCheckpoint();RequireCurrent();combat.CompleteO22Selection(acquired);RequireCurrent();}
     var savedScope=new AugmentSelectionScope(owner,grant.RunId,grant.EventId);
     // The acquisition and native checkpoint may be durable while completion of
     // the original SELECT journal is still ambiguous. Keep its existing Retry
     // reachable; never erase that request merely because combat state applied.
     var savedRequest=new AugmentSelectionFileJournal(savedScope,crypto,journalPath).Load(savedScope.StorageKey);RequireCurrent();
     if(savedRequest==null){ReleaseSelection();ReturnAfterSelection(combat,acquisitionOnly);return;}
    }else if(grant.Confirmed&&grant.SelectedCandidateId=="O22"){
     var recoveryScope=new AugmentSelectionScope(owner,grant.RunId,grant.EventId);
     var pending=new AugmentSelectionFileJournal(recoveryScope,crypto,journalPath).Load(recoveryScope.StorageKey);RequireCurrent();
     if(pending==null)throw new InvalidOperationException("Confirmed acquisition original and original SELECT request missing");
     var request=AugmentSelectionCommand.Restore(recoveryScope,pending);request.Before.RequireBinding(grant);
     if(request.Operation!="SELECT")throw new InvalidOperationException("Confirmed acquisition requires the original SELECT request");
    }
    long openedGeneration=generation+2;
    await OpenAsync(combat,new AugmentSelectionScope(owner,grant.RunId,grant.EventId),grant,transport,session,ownerCurrent,crypto,journalPath,originals,definitions,font);
    // Opening a confirmed durable original can finish synchronously and close
    // its view. That is completion, not an unrelated screen superseding arrival.
    if(this&&isActiveAndEnabled&&generation==openedGeneration+1&&ownerCurrent()&&view==null
     &&runtime?.Completed==true&&ReferenceEquals(boundCombat,combat)&&ReferenceEquals(boundRoute,combat.Route)
     &&(ArrivalState=="ACQUIRED_PENDING_COMBAT"||ArrivalState=="COMBAT_READY"||ArrivalState=="RESULT_REQUIRED"))return;
    if(!this||!isActiveAndEnabled||generation!=openedGeneration||!ownerCurrent()||view==null)throw new OperationCanceledException("Arrival screen was superseded");
    ArrivalState="OPEN";
   }catch{ArrivalState="RECOVERY_REQUIRED";throw;}
   finally{arrivalBusy=false;}
  }
 }
}
