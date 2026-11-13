using System.Threading.Tasks;
namespace LastRail.Presentation.MissionShell {
 public sealed partial class AccountScreenRuntime {
  CombatAdController combatAds;
  public CombatAdController CombatAds {
   get {if(combatAds==null){combatAds=new CombatAdController(wire,journal,session,generation,()=>adPresentation,ConfirmNewAdPolicyAsync,()=>RuntimeAds.Status==AdRuntimePolicyStatus.Enabled);combatAds.Changed+=CombatAdChanged;}return combatAds;}
  }
  void CombatAdChanged(){if(disposed)return;try{Pending=journal?.Read(owner?.AccountId);}catch{ActionNotice="요청 보관함을 확인하지 못했습니다.";}Changed?.Invoke();}
  bool CombatBasicAllowed(string run)=>SavedRewardClaim(run)!=null||combatAds?.Intent==null||combatAds.Intent.RunId!=run||combatAds.Intent.CanClaimBasic;
  public async Task RestorePendingCombatAdAsync(){
   SynchronizeSession();var original=owner;var stamp=ownerGeneration;if(Busy||!Current(original,stamp))return;
   var row=Pending;if(row==null||!row.Kind.StartsWith("combat-ad-"))return;
   var payload=LastRail.Auth.GameApiJson.ReadObject(row.Payload);
   await CombatAds.RefreshAsync(row.Target,(string)payload["rewardPreviewId"]);
   if(Current(original,stamp)){ActionNotice=CombatAds.Notice;InvalidateRunReads();await ReadRunResultsAsync(row.Target);}
  }
  public async Task RunCombatAdAsync(string run,string action){
   SynchronizeSession();var original=owner;var stamp=ownerGeneration;
   if(Busy||RunResultsLoading||!Current(original,stamp))return;
   PendingAccountAction pending;
   try{pending=journal?.Read(original.AccountId);}catch{CombatAds.ReportStorageFailure();return;}
   var preview=pending?.Kind.StartsWith("combat-ad-")==true&&pending.Target==run?(string)LastRail.Auth.GameApiJson.ReadObject(pending.Payload)["rewardPreviewId"]:Lifecycle?.RunId==run?Lifecycle.PreviewId:null;
   if(preview==null)return;
   if(action=="begin"&&SavedRewardClaim(run)!=null)return;
   if(action=="spin")await CombatAds.SpinAsync(run,preview);else if(action=="begin")await CombatAds.BeginAsync(run,preview);else await CombatAds.RefreshAsync(run,preview);
   if(Current(original,stamp)&&CombatAds.Intent?.RunId==run&&CombatAds.Intent.Status=="CLAIMED"){
    InvalidateRunReads();await ReadRunResultsAsync(run);if(Current(original,stamp))await RefreshSharedAsync();
   }
  }
 }
}
