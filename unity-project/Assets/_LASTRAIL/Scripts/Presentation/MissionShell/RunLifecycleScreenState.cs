using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell {
 public sealed class RunLifecycleScreenState {
  public string Record {get;} public string Rewards {get;} public string RefreshLabel {get;} public string ClaimLabel {get;}
  public bool CanRefresh {get;} public bool CanRecover {get;} public bool CanClaim {get;}
  public RunLifecycleScreenState(AccountScreenRuntime state,string runId){
  var data=state?.Lifecycle;bool loading=state?.RunResultsLoading==true;bool matching=data?.RunId==runId;var receiptSnapshot=state?.VerifiedRunReceipt;var previewLines=state?.RunRewardPreview?.Lines;
  var record=loading?"전투·보상 기록을 확인하고 있습니다.":matching&&data.RewardStatus=="CLAIMED"&&receiptSnapshot==null?"보상 지급 기록을 추가로 확인해야 합니다.":state?.LifecycleNotice??"전투 기록을 확인할 수 없습니다.";
  // The lifecycle notice alone cannot describe the independently loaded reward preview.
  // Presentation follows the existing runtime authority; it never enables a claim.
  if(!loading&&matching&&data.NextAction=="REWARD_REVIEW"){
   record=state.RunRewardPreview==null?"전투 결과가 확인되었습니다.\n보상 내역을 불러오지 못했습니다.":previewLines.Count==0?"전투 결과가 확인되었습니다.\n보상 항목이 없습니다.":state.CanClaimRunReward(runId)?state.LifecycleNotice:"전투 결과가 확인되었습니다.\n현재 보상을 수령할 수 없습니다.";
  }
  var text=loading?"보상 정보를 확인하고 있습니다.":"보상 정보 확인 필요";
  if(!loading&&matching){if(state.RunRewardPreview!=null)text=previewLines.Count==0?"보상 항목이 없습니다.":RecurringContract.RewardText(previewLines);else if(receiptSnapshot is JObject receipt)text="지급된 보상\n"+RecurringContract.RewardText((JArray)receipt["rewardPlan"]["rewardLines"]);}
  if(!loading&&!string.IsNullOrEmpty(state?.RunResultNotice))text+="\n"+state.RunResultNotice;

  Record=record;Rewards=text;
  CanRefresh=state!=null&&!state.LifecycleLoading&&!state.RunResultsLoading&&!state.RunReceiptHandoffLoading&&!state.Busy;
  RefreshLabel=state?.CanRetryRunReceiptHandoff(runId)==true?"영수증 저장 다시 시도":"기록 새로고침";
  CanRecover=state?.CanRecoverFinish(runId)==true;CanClaim=state?.CanClaimRunReward(runId)==true;
  var saved=state?.SavedRewardClaim(runId);ClaimLabel=saved!=null&&saved.AcceptedResponseJson==null?"지급 결과 다시 확인":receiptSnapshot!=null?"지급 완료":"보상 받기";
  }
 }
}
