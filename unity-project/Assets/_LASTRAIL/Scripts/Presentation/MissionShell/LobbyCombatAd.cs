namespace LastRail.Presentation.MissionShell {
 public sealed partial class LobbyCommandDeck {
  CombatAdView combatAdView;CombatRouletteScreenController combatRouletteController;
  System.Threading.CancellationTokenSource combatAdOpenRequest;
  async void OpenCombatAdView(){
   if(accountState==null||dialog==null||accountState.Busy||!RewardEntryDisplayState.Combat(accountState,lifecycleRun).Visible)return;var owner=accountState;var run=lifecycleRun;var preview=owner.Lifecycle?.RunId==run?owner.Lifecycle.PreviewId:null;var currentSession=owner.CaptureScreenSession();
   bool use=owner.AdSkip.Snapshot?.Owned==true&&!(owner.CombatAds.Intent?.RunId==run&&owner.CombatAds.Intent.Terminal);
   CloseCombatAdView();var request=combatAdOpenRequest=new System.Threading.CancellationTokenSource();var source=dialog;
   LastRail.Resources.PreparedContent.Scope resources=null;
   try{resources=await LastRail.Resources.PreparedContent.PrepareAsync(RouletteResources,request.Token);
    if(this==null||!isActiveAndEnabled||request.IsCancellationRequested||combatAdOpenRequest!=request||source==null||dialog!=source||owner!=accountState||!currentSession()||lifecycleRun!=run)return;
    combatAdView=CombatAdView.Create(dialog,font);LastRail.Resources.PreparedContent.Attach(combatAdView.gameObject,resources);resources=null;
   }catch(System.OperationCanceledException){return;}catch(System.Exception error){UnityEngine.Debug.LogException(error);return;}finally{if(combatAdOpenRequest==request)combatAdOpenRequest=null;resources?.Dispose();request.Dispose();}
   var view=combatAdView;
   void Render(CombatRouletteScreenState state)=>view.Render(state.Display);
   var controller=combatRouletteController=new CombatRouletteScreenController(owner,run,Render,CloseCombatAdView,()=>this!=null&&isActiveAndEnabled&&dialog==source&&accountState==owner&&lifecycleRun==run&&combatAdView==view);
   view.Bind(async()=>await controller.ActAsync(),async()=>await controller.RefreshAsync(),async()=>await controller.ClaimBasicAsync(),controller.Close);controller.Render();
   await controller.EnterAsync(use,preview);
  }
  void CloseCombatAdView(){combatRouletteController?.Dispose();combatRouletteController=null;var request=combatAdOpenRequest;combatAdOpenRequest=null;request?.Cancel();if(combatAdView==null)return;combatAdView.Unbind();var go=combatAdView.gameObject;combatAdView=null;go.SetActive(false);if(UnityEngine.Application.isPlaying)Destroy(go);else DestroyImmediate(go);}
  void RenderCombatAdView()=>combatRouletteController?.Render();
 }
}
