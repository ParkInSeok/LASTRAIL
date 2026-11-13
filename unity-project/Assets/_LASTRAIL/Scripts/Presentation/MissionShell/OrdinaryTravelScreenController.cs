using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;

namespace LastRail.Presentation.MissionShell {
 public sealed class OrdinaryTravelScreenController:IDisposable {
  readonly AccountScreenRuntime account;
  readonly string accountId,run;
  readonly Func<bool> current;
  readonly Action back;
  readonly Func<Task> combat;
  readonly OrdinaryTravelPanel view;
  readonly OrdinaryTravelOwner owner;
  readonly EquipmentIconResources equipmentIcons;
  EquipmentInventoryView inventoryView;TMP_FontAsset inventoryFont;
  OperationMapStatus map;
  BDepartureSourceBundle source;
  OrdinaryTravelDisplayPlan plan;
  BTravelReceipt displayed;
  double receivedAt;
  bool disposed,busy,arrivalAttempted,presentationVisible;
  CancellationTokenSource countdown;
  string destinationName;
  string notice;
  public bool IsCurrent=>!disposed&&current();
  public OrdinaryTravelScreenController(AccountScreenRuntime account,string accountId,string run,Transform parent,TMP_FontAsset font,Func<bool> current,Action back,Func<Task> combat,EquipmentIconResources equipmentIcons=null){
   this.equipmentIcons=equipmentIcons;this.account=account;this.accountId=accountId;this.run=run;this.current=current;this.back=back;this.combat=combat;owner=account.ReadOrdinaryTravel(run);
   inventoryFont=font;view=OrdinaryTravelPanel.Create(parent,font);view.BindInventory(OpenInventory);
   view.Bind(Choose,bps=>Run(()=>owner.SetThrottleAsync(bps)),Pause,Resume,Recover,Leave,Pause);
   view.BindVisibility(visible=>{presentationVisible=visible;if(visible)Render();else CancelCountdown();});
  }
  async void OpenInventory(){
   if(!IsCurrent||inventoryView!=null)return;
   try{
    var controller=account.EquipmentInventory(run);
    inventoryView=EquipmentInventoryView.Open(view.transform,inventoryFont,controller,()=>inventoryView=null,equipmentIcons);
    inventoryView.BindRefresh(RefreshInventory);
    await account.ReadEquipmentInventoryAsync(run);
   }catch(OperationCanceledException){}catch(Exception error){if(IsCurrent&&inventoryView!=null)inventoryView.ShowNotice("장비 정보를 불러오지 못했습니다. 닫고 다시 시도해 주세요.");Debug.LogWarning("Equipment inventory: "+error.GetType().Name);}
  }
  async void RefreshInventory(){
   var active=inventoryView;if(!IsCurrent||active==null)return;
   try{await account.ReadEquipmentInventoryAsync(run,true);}catch(OperationCanceledException){}catch(Exception error){if(IsCurrent&&active!=null&&active==inventoryView)active.ShowNotice("장비 정보를 갱신하지 못했습니다. 다시 시도해 주세요.");Debug.LogWarning("Equipment refresh: "+error.GetType().Name);}
  }
  public async Task<bool> OpenAsync(){
   var latest=await owner.RestoreAsync();if(!IsCurrent)return false;
   if(owner.HasResultOperation)return false;
   if(owner.NeedsRecovery){notice="이동 요청 결과를 확인하지 못했습니다. 원래 요청을 보존했습니다.";Render();return true;}
   if(latest?.ArrivalEvidenceRef!=null&&owner.PauseRequested){
    try{await ConsumeArrival();}catch(Exception error){Failure(error);}return true;
   }
   if(latest?.ArrivalEvidenceRef!=null||latest?.IsCurrentMap==true)return false;
   try{
    source=await account.ReadOrdinaryTravelSourceAsync(run);if(!IsCurrent)return false;
    if(owner.DestinationReceipt!=null){
     var intent=owner.DestinationReceipt.Intent;
     if(latest==null){view.ShowUnavailable("목적지는 확정됐지만 이동 정보를 확인하지 못했습니다. 원래 요청을 보존했습니다.");return true;}
     destinationName=latest.NodeId;plan=Plan(intent,latest.NodeId);Render();return true;
    }
    if(latest!=null){view.ShowUnavailable("이동은 보존됐지만 원래 목적지 정보가 필요합니다. 원래 요청 확인을 이용해 주세요.");return true;}
    map=await account.ReadOrdinaryMapAsync(run);if(!IsCurrent)return false;
    if(map.Phase=="ACTIVE"||map.RunStatus!="ACTIVE")return false;
    view.ShowChoices(map.Choices.Select(c=>(c.EdgeId,c.Name,c.EventKind)),map.CanChoose,map.CanChoose?"이동할 목적지를 선택해 주세요.":"현재 목적지 선택을 진행할 수 없습니다. 작전은 유지됩니다.",map.CurrentNodeName);return true;
   }catch(Exception error){if(IsCurrent){view.ShowUnavailable("이동 준비 정보를 불러오지 못했습니다. 작전은 유지됩니다.");Debug.LogWarning("ordinary_travel_prepare: "+error.GetType().Name);}return true;}
  }
  OrdinaryTravelDisplayPlan Plan(BOperationDestinationIntent intent,string node){
   var original=source.Capture();var boundary=source.OriginalCombatSource["profile"]?["definition"]?["bArrivalBoundarySource"] as JObject;
   return OrdinaryTravelDisplayPlan.Read(boundary,new JObject{["accountId"]=accountId,["runId"]=run,["nodeId"]=node,["nativeContextId"]=source.ContextId,
    ["departurePinHash"]=original["departurePinHash"],["mapRevision"]=intent.ExpectedRevision+1,["destinationEventId"]=intent.CommandId,["edgeId"]=intent.EdgeId,["origin"]=original["originalOrigin"]});
  }
  async void Choose(string edge){
   if(!IsCurrent||busy||owner.Busy||map?.CanChoose!=true)return;
   var choice=map.Choices.SingleOrDefault(c=>c.EdgeId==edge);if(choice==null)return;
   busy=true;notice=null;view.ShowChoices(map.Choices.Select(c=>(c.EdgeId,c.Name,c.EventKind)),false,"목적지 확인 중…",map.CurrentNodeName);
   try{
    var intent=new BOperationDestinationIntent(accountId,run,Guid.NewGuid().ToString("N"),map.Revision,edge);
    plan=Plan(intent,choice.NodeId);destinationName=choice.Name;
    var request=owner.ConfirmDestinationAsync(intent,choice.NodeId);await request;
    if(IsCurrent&&owner.DestinationReceipt!=null)account.InvalidateOrdinaryMap(run,owner.DestinationReceipt.Intent.ExpectedRevision+1);
    if(IsCurrent){
     if(owner.DestinationReceipt!=null&&owner.Latest!=null){plan=Plan(owner.DestinationReceipt.Intent,owner.Latest.NodeId);destinationName=map.Choices.FirstOrDefault(c=>c.EdgeId==owner.DestinationReceipt.Intent.EdgeId)?.Name??owner.Latest.NodeId;}
     Render();await ConsumeArrival();
    }
   }catch(Exception error){Failure(error);if(IsCurrent&&owner.Latest!=null)Pause();}finally{busy=false;if(IsCurrent)Render();}
  }
  async void Run(Func<Task<BTravelReceipt>> command){
   if(!IsCurrent||busy)return;busy=true;notice=null;
   try{var request=command();Render();await request;if(IsCurrent){Render();await ConsumeArrival();}}catch(Exception error){Failure(error);}finally{busy=false;if(IsCurrent)Render();}
  }
  async void Pause(){
   if(!IsCurrent)return;
   CancelCountdown();
   try{var request=owner.PauseAsync();Render();await request;if(IsCurrent){Render();await ConsumeArrival();}}catch(Exception error){Failure(error);}
  }
  async void Resume(){
   if(!IsCurrent||busy)return;
   if(owner.MapArrival==null){Run(()=>owner.ResumeAsync());return;}
   busy=true;notice=null;
   try{if(!owner.ContinueAfterArrival())return;await combat();}
   catch(Exception error){if(IsCurrent)await owner.PauseAsync();Failure(error);}
   finally{busy=false;if(IsCurrent)Render();}
  }
  async void Recover(){
   if(!IsCurrent||busy)return;busy=true;notice=null;
   try{await owner.RecoverAsync();if(IsCurrent){arrivalAttempted=false;bool shown=await OpenAsync();if(!shown&&owner.Latest?.IsCurrentMap==true)await combat();else await ConsumeArrival();}}catch(Exception error){Failure(error);}finally{busy=false;if(IsCurrent)Render();}
  }
  async void Leave(){
   if(!IsCurrent)return;
   CancelCountdown();
   try{await owner.PauseAsync();}catch(Exception error){Debug.LogWarning("ordinary_travel_leave: "+error.GetType().Name);}
   if(IsCurrent)back();
  }
  void Render(){
   CancelCountdown();
   if(!IsCurrent)return;
   if(owner.NeedsRecovery){view.ShowUnavailable("이동 요청 결과를 확인하지 못했습니다. 원래 요청 확인으로 이어서 진행해 주세요.");return;}
   if(notice!=null){if(owner.PauseRequested&&owner.MapArrival?.NodeStart!=null)view.ShowArrivalPaused(notice);else view.ShowUnavailable(notice);return;}var receipt=owner.Latest;
   if(receipt==null){if(owner.DestinationReceipt!=null)view.ShowUnavailable("목적지는 확정됐지만 이동 정보를 확인하지 못했습니다. 원래 요청을 보존했습니다.");return;}
   if(receipt.IsCurrentMap){view.ShowUnavailable("현재 작전 상태를 확인했습니다. 전투 준비 정보를 확인해 주세요.");return;}
   if(plan==null)return;
   if(!ReferenceEquals(displayed,receipt)){displayed=receipt;receivedAt=Time.realtimeSinceStartupAsDouble;arrivalAttempted=false;}
   var state=receipt.State;
   view.ShowTravel(destinationName??receipt.NodeId,(state.Paused?"일시정지":"이동 중")+" · 확정 이동 "+(state.DistanceMm/1000m).ToString("0.##")+" / "+(plan.DistanceMm/1000m).ToString("0.##")+" m",state.ThrottleBps,plan.MinimumBps,plan.MaximumBps,state.Paused,busy||owner.Busy);
   ScheduleCountdown();
  }
  bool CanCountDown(BTravelReceipt receipt)=>IsCurrent&&presentationVisible&&view!=null&&view.isActiveAndEnabled&&notice==null&&!busy&&!owner.Busy&&!owner.NeedsRecovery&&plan!=null&&receipt?.State!=null&&!receipt.State.Paused&&!owner.PauseRequested&&!arrivalAttempted&&receipt.ArrivalEvidenceRef==null&&ReferenceEquals(receipt,owner.Latest);
  void CancelCountdown(){var previous=countdown;countdown=null;if(previous==null)return;previous.Cancel();previous.Dispose();}
  void ScheduleCountdown(){
   var receipt=owner.Latest;if(!CanCountDown(receipt))return;
   countdown=new CancellationTokenSource();_ = CountDownAsync(receipt,countdown.Token);
  }
  async Task CountDownAsync(BTravelReceipt receipt,CancellationToken cancellation){
   try{
    var state=receipt.State;double deadline=receivedAt+(double)plan.SecondsUntilBoundary(state.DistanceMm,state.RemainderUnits,state.ThrottleBps);
    // Yield once so a synchronous receipt/arrival render cannot reenter its command.
    await Task.Delay(1,cancellation);
    while(!cancellation.IsCancellationRequested&&CanCountDown(receipt)){
     double remaining=deadline-Time.realtimeSinceStartupAsDouble;
     if(remaining<=0){arrivalAttempted=true;Run(()=>owner.ArriveAsync());return;}
     double seconds=Math.Ceiling(remaining);
     view.SetStatus("이동 중 · 도착까지 약 "+seconds+"초\n확정 이동 "+(state.DistanceMm/1000m).ToString("0.##")+" / "+(plan.DistanceMm/1000m).ToString("0.##")+" m");
     double nextChange=Math.Max(.001,Math.Min(1,remaining-(seconds-1)));
     await Task.Delay((int)Math.Ceiling(nextChange*1000),cancellation);
    }
   }catch(OperationCanceledException) when(cancellation.IsCancellationRequested){}
   catch(Exception error){Failure(error);}
  }
  async Task ConsumeArrival(){
   if(!IsCurrent)return;
   if(owner.Latest?.ArrivalEvidenceRef==null)return;
   var result=await account.CommitOrdinaryMapArrivalAsync(run);if(!IsCurrent)return;
   if(result==null){notice="도착 요청 결과를 확인하지 못했습니다. 원래 요청을 보존했습니다.";Render();return;}
   account.InvalidateOrdinaryMap(run,result.RequiredMapRevision);
   if(owner.PauseRequested){notice="도착했습니다. 일시정지 의도를 유지합니다.";Render();return;}
   await combat();
  }
  void Failure(Exception error){if(!IsCurrent)return;arrivalAttempted=true;notice=error is OrdinaryNodeStartUnavailableException?error.Message:"요청 결과를 확인하지 못했습니다. 원래 요청 확인으로 이어서 진행해 주세요.";Render();Debug.LogWarning("ordinary_travel_command: "+error.GetType().Name+": "+error.Message);}
  public void Dispose(){if(disposed)return;disposed=true;CancelCountdown();view.Unbind();if(view!=null){view.gameObject.SetActive(false);if(Application.isPlaying)UnityEngine.Object.Destroy(view.gameObject);else UnityEngine.Object.DestroyImmediate(view.gameObject);}}
 }
}
