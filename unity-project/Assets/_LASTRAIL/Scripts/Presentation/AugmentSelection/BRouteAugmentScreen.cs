using System;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using TMPro;
using UnityEngine;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.AugmentSelection {
 // Scene entry for an already supplied grant and pinned definition provider.
 // The route owns APPLIED/HP/checkpoints; this host owns only its view/bindings.
 // Arrival/issuer transport must invoke OpenAsync with its original snapshot.
 public sealed partial class BRouteAugmentScreen:MonoBehaviour {
  AugmentSelectionRuntime runtime;AugmentSelectionView view;long generation;
  BRouteSessionController boundCombat;BRouteBinding boundRoute;AugmentSelectionScope boundScope;
  public AugmentSelectionView View=>view;
  public void AttachLocalNative(BRouteNativeComposition composition,BRouteSessionController combat,string context,Func<JObject> load,Action<JObject,long> save){
   if(runtime!=null)throw new InvalidOperationException("The retained selection transaction still owns its native binding");
   (composition??throw new ArgumentNullException(nameof(composition))).AttachPersistentTo(combat,context,load,save);
  }
  public void AttachLocalNativeWithAcquisition(BRouteNativeComposition composition,BRouteSessionController combat,string context,Func<JObject> load,Action<JObject,long> save,CombatO22ApplicationStore originals,string environment){
   if(runtime!=null&&!runtime.TransactionCompleted)throw new InvalidOperationException("Finish the retained original SELECT journal before entering combat");
   (composition??throw new ArgumentNullException(nameof(composition))).AttachPersistentWithAcquisitionTo(combat,context,load,save,originals,environment);
   // Release the completed screen transaction only after the new native owner
   // is durable. A failed attachment keeps the existing recovery route intact.
   ReleaseSelection();
  }
  void ReleaseSelection(){Close();runtime?.Dispose();runtime=null;boundCombat=null;boundRoute=null;boundScope=null;}
  public Task OpenAsync(BRouteSessionController combat,AugmentSelectionScope scope,AugmentSelectionSnapshot grant,IGameApiAuthenticatedHttpTransport transport,Func<GameApiAuthSessionSnapshot> session,Func<bool> ownerCurrent,IAndroidKeystoreNativeStoragePort crypto,string journalPath,CombatO22ApplicationStore originals,Func<string,string,AugmentCardPresentation> definitions,TMP_FontAsset font)
   =>OpenCoreAsync(combat,scope,grant,transport,session,ownerCurrent,()=>new AugmentSelectionFileJournal(scope,crypto,journalPath),originals,definitions,font);
#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
  // Dedicated LOCAL build only. This port never claims Android storage support.
  public Task OpenExplicitLocalQaAsync(BRouteSessionController combat,AugmentSelectionScope scope,AugmentSelectionSnapshot grant,IGameApiAuthenticatedHttpTransport transport,Func<GameApiAuthSessionSnapshot> session,Func<bool> ownerCurrent,IAugmentSelectionJournal journal,CombatO22ApplicationStore originals,Func<string,string,AugmentCardPresentation> definitions,TMP_FontAsset font)
   =>OpenCoreAsync(combat,scope,grant,transport,session,ownerCurrent,()=>journal??throw new ArgumentNullException(nameof(journal)),originals,definitions,font);
#endif
  async Task OpenCoreAsync(BRouteSessionController combat,AugmentSelectionScope scope,AugmentSelectionSnapshot grant,IGameApiAuthenticatedHttpTransport transport,Func<GameApiAuthSessionSnapshot> session,Func<bool> ownerCurrent,Func<IAugmentSelectionJournal> journal,CombatO22ApplicationStore originals,Func<string,string,AugmentCardPresentation> definitions,TMP_FontAsset font){
   if(combat==null||scope==null||grant==null||ownerCurrent==null||definitions==null||font==null)throw new ArgumentNullException();
   var route=combat.Route??throw new InvalidOperationException("Original route unavailable");
   if(route.AccountId!=scope.Owner.AccountId||route.RunId!=scope.RunId)throw new ArgumentException("Selection screen owner differs from combat owner");
   Close();long opened=++generation;
   bool Current()=>this&&ownerCurrent()&&ReferenceEquals(combat.Route,route);
   if(!isActiveAndEnabled||!Current())throw new InvalidOperationException("Selection screen is no longer current");
   bool reuse=runtime!=null&&ReferenceEquals(boundCombat,combat)&&ReferenceEquals(boundRoute,route)&&ReferenceEquals(boundScope?.Owner,scope.Owner)&&boundScope.RunId==scope.RunId&&boundScope.EventId==scope.EventId;
   if(!reuse){runtime?.Dispose();runtime=null;boundCombat=combat;boundRoute=route;boundScope=scope;}
   combat.BeginAugmentSelection(grant.EventId);
   bool acquisitionOnly=route.NativeNode==null;
   if(reuse&&runtime.Completed){ReturnAfterSelection(combat,acquisitionOnly);return;}
   view=AugmentSelectionView.Create(transform,font);
   try{
    if(reuse){runtime.AttachView(view);return;}
    void ApplyOriginal(CombatO22ApplicationOriginal original){
     if(combat.CheckpointRecoveryRequired)combat.RecoverLocalCheckpoint();
     if(!Current())throw new InvalidOperationException("Selection owner changed during checkpoint recovery");
     combat.CompleteO22Selection(original);
    }
    runtime=new AugmentSelectionRuntime(view,scope,transport,session,Current,journal(),originals,(string)route.Capture()["startSnapshotHash"],definitions,acquisitionOnly?null:(Action<CombatO22ApplicationOriginal>)ApplyOriginal);
    var installed=runtime;
    runtime.SelectionCompleted+=()=>{if(ReferenceEquals(runtime,installed)&&Current())ReturnAfterSelection(combat,acquisitionOnly);};
    await runtime.OpenAsync(grant);
   }catch{if(opened==generation)Close();throw;}
  }
  public void Close(){generation++;runtime?.DetachView();if(view){view.gameObject.SetActive(false);if(Application.isPlaying)Destroy(view.gameObject);else DestroyImmediate(view.gameObject);}view=null;}
  void ReturnAfterSelection(BRouteSessionController combat,bool acquisitionOnly){Close();ArrivalState=acquisitionOnly?"ACQUIRED_PENDING_COMBAT":combat.LocalCombatOutcome=="RUNNING"?"COMBAT_READY":"RESULT_REQUIRED";}
  void OnDisable()=>Close();
  void OnDestroy(){Close();runtime?.Dispose();runtime=null;boundCombat=null;boundRoute=null;boundScope=null;}
 }
}
