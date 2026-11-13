namespace LastRail.Presentation.MissionShell {
 public sealed partial class LobbyCommandDeck {
  CombatAdView combatAdView;
  System.Threading.CancellationTokenSource combatAdOpenRequest;
  async void OpenCombatAdView(){
   if(accountState==null||dialog==null||accountState.Busy||!RewardEntryDisplayState.Combat(accountState,lifecycleRun).Visible)return;var owner=accountState;var run=lifecycleRun;var preview=owner.Lifecycle?.RunId==run?owner.Lifecycle.PreviewId:null;
   bool use=owner.AdSkip.Snapshot?.Owned==true&&!(owner.CombatAds.Intent?.RunId==run&&owner.CombatAds.Intent.Terminal);
   CloseCombatAdView();var request=combatAdOpenRequest=new System.Threading.CancellationTokenSource();var source=dialog;
   LastRail.Resources.PreparedContent.Scope resources=null;
   try{resources=await LastRail.Resources.PreparedContent.PrepareAsync(RouletteResources,request.Token);
    if(this==null||!isActiveAndEnabled||request.IsCancellationRequested||combatAdOpenRequest!=request||source==null||dialog!=source||owner!=accountState)return;
    combatAdView=CombatAdView.Create(dialog,font);LastRail.Resources.PreparedContent.Attach(combatAdView.gameObject,resources);resources=null;
   }catch(System.OperationCanceledException){return;}catch(System.Exception error){UnityEngine.Debug.LogException(error);return;}finally{if(combatAdOpenRequest==request)combatAdOpenRequest=null;resources?.Dispose();request.Dispose();}
   var view=combatAdView;
   combatAdView.Bind(ActCombatAd,RefreshCombatAd,ClaimCombatAdBasic,CloseCombatAdView);RenderCombatAdView();
   if(use&&preview!=null)await owner.UseAdSkipCombatAsync(run,preview);else await owner.RunCombatAdAsync(run,"read");
   if(ReferenceEquals(owner,accountState)&&view==combatAdView)RenderCombatAdView();
  }
  void CloseCombatAdView(){var request=combatAdOpenRequest;combatAdOpenRequest=null;request?.Cancel();if(combatAdView==null)return;combatAdView.Unbind();var go=combatAdView.gameObject;combatAdView=null;go.SetActive(false);if(UnityEngine.Application.isPlaying)Destroy(go);else DestroyImmediate(go);}
  void RenderCombatAdView(){
   if(combatAdView==null||accountState==null)return;var owner=accountState;var c=owner.CombatAds;var intent=c.Intent?.RunId==lifecycleRun?c.Intent:null;
   bool owned=owner.AdSkip.Snapshot?.Owned==true,unknown=owner.AdSkip.Snapshot==null,approval=owner.Pending?.Kind=="reward-approval";
   string label=intent?.Terminal==true||intent?.CanSpin==true?null:approval?"사용 결과 재확인":owned?"광고 없이 룰렛":unknown?c.HasPending(lifecycleRun)?"다시 확인":"이용권 다시 확인":null;
   if(intent?.Terminal!=true&&intent?.CanSpin!=true&&!owned&&c.HasPending(lifecycleRun)&&owner.RuntimeAds.Status!=AdRuntimePolicyStatus.Enabled)label="다시 확인";
   string notice=!string.IsNullOrEmpty(owner.AdSkipUse.Notice)?owner.AdSkipUse.Notice:unknown&&!c.HasPending(lifecycleRun)?owner.AdSkip.Notice:c.Notice;
   if(!owner.Busy&&intent==null&&!c.HasPending(lifecycleRun)&&!unknown&&!owned&&string.IsNullOrEmpty(owner.AdSkipUse.Notice))notice=owner.AdSkip.Snapshot.Status=="REVOKED"?"이용권 상태가 변경되었습니다. 기본 보상은 받을 수 있습니다.":!c.ProviderAvailable?"현재 광고를 이용할 수 없습니다. 기본 보상은 받을 수 있습니다.":"광고 시청 후 룰렛을 이용할 수 있습니다.";
   combatAdView.Render(intent,notice,owner.Busy,c.ProviderAvailable,c.HasPending(lifecycleRun),owner.CanClaimRunReward(lifecycleRun),c.CanPresent(lifecycleRun),label,label==null?null:true);
  }
  async void ActCombatAd(){
   if(accountState==null||accountState.Busy)return;var c=accountState.CombatAds;
   if(c.Intent?.RunId==lifecycleRun&&c.Intent.Terminal){c.Acknowledge();CloseCombatAdView();return;}
   if(accountState.Pending?.Kind=="reward-approval")await accountState.RetryPendingAsync();
   else if(c.Intent?.RunId==lifecycleRun&&c.Intent.CanSpin)await accountState.RunCombatAdAsync(lifecycleRun,"spin");
   else if(accountState.AdSkip.Snapshot?.Owned==true){var preview=accountState.Lifecycle?.RunId==lifecycleRun?accountState.Lifecycle.PreviewId:null;if(preview!=null)await accountState.UseAdSkipCombatAsync(lifecycleRun,preview);}
   else if(accountState.AdSkip.Snapshot==null){if(c.HasPending(lifecycleRun))await accountState.RunCombatAdAsync(lifecycleRun,"read");else await accountState.RefreshAdSkipAsync();}
   else await accountState.RunCombatAdAsync(lifecycleRun,c.CanPresent(lifecycleRun)&&accountState.RuntimeAds.Status==AdRuntimePolicyStatus.Enabled?"begin":c.HasPending(lifecycleRun)?"read":"begin");RenderCombatAdView();
  }
  async void RefreshCombatAd(){if(accountState!=null)await accountState.RunCombatAdAsync(lifecycleRun,"read");RenderCombatAdView();}
  async void ClaimCombatAdBasic(){if(accountState==null)return;var run=lifecycleRun;await accountState.ClaimRunRewardAsync(run);await accountState.RunCombatAdAsync(run,"read");RenderCombatAdView();}
 }
}
