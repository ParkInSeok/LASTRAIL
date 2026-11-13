using LastRail.Run;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Presentation.MissionShell {public sealed partial class LobbyCommandDeck {
 bool lifecycleDialog;string lifecycleRun;
 public void OpenRunLifecycle(string runId,bool reuseKnownState=false)=>OpenResourceDialog(new[]{"NoncombatResult/","NoncombatVictory/","EntryUi/secondary","EntryPopup/Rewards/","EntryPopup/roulette-reward-icons"},()=>OpenRunLifecyclePrepared(runId,reuseKnownState));
 void OpenRunLifecyclePrepared(string runId,bool reuseKnownState){ShowInfo("전투·보상 기록","",ReturnFromRunLifecycle);lifecycleDialog=true;lifecycleRun=runId;dialog.sizeDelta=new Vector2(750,1000);Place(dialog.Find("DialogBody").GetComponent<RectTransform>(),48,140,650,490);Button("RecoverRunFinish",dialog,60,660,630,65,"결과 전송 다시 확인",RecoverCurrentRunFinish);Button("ClaimRunReward",dialog,60,740,630,65,"보상 받기",ClaimCurrentRunReward);Button("RefreshRunLifecycle",dialog,60,820,630,65,"기록 새로고침",RefreshCurrentRunResults);var close=dialog.Find("CloseLobbyDialog").GetComponent<Button>();Place(close.GetComponent<RectTransform>(),60,905,630,65);close.GetComponentInChildren<TMP_Text>().text="로비로 돌아가기";BuildResultPresentation();EvaluateLayout();LifecycleChanged();ReadRunLifecycle(runId,reuseKnownState);}
 async void ClaimCurrentRunReward(){if(accountState==null)return;if(accountState.CombatAds.HasPending(lifecycleRun)){OpenCombatAdView();return;}await accountState.ClaimRunRewardAsync(lifecycleRun);}
 async void RecoverCurrentRunFinish(){if(accountState!=null)await accountState.RecoverFinishAsync(lifecycleRun);}
 async void RefreshCurrentRunResults(){if(accountState!=null){if(accountState.CanRetryRunReceiptHandoff(lifecycleRun))await accountState.RetryRunReceiptHandoffAsync(lifecycleRun);else await accountState.ReadRunResultsAsync(lifecycleRun);}}
 void ReturnFromRunLifecycle(){CloseDialog();Pages?.Close();}
 void OpenResultOperations(){if(!isActiveAndEnabled||!lifecycleDialog||accountState==null)return;accountState.SynchronizeSession();if(!new RunVictoryDisplayState(accountState,lifecycleRun,missions).CanViewOperations)return;OpenOperationList();}
 async void ReadRunLifecycle(string runId,bool reuseKnownState=false){var owner=accountState;if(owner==null)return;if(reuseKnownState)await owner.EnsureRunResultsAsync(runId);else await System.Threading.Tasks.Task.WhenAll(owner.EnsureRunResultsAsync(runId),owner.EnsureAdSkipDisplayAsync());}
 void LifecycleChanged(){
  if(!lifecycleDialog||dialog==null)return;var state=accountState;var data=state?.Lifecycle;UpdateResultPresentation();bool loading=state?.RunResultsLoading==true;bool matching=data?.RunId==lifecycleRun;var receiptSnapshot=state?.VerifiedRunReceipt;var previewLines=state?.RunRewardPreview?.Lines;
  var record=loading?"전투·보상 기록을 확인하고 있습니다.":matching&&data.RewardStatus=="CLAIMED"&&receiptSnapshot==null?"보상 지급 기록을 추가로 확인해야 합니다.":state?.LifecycleNotice??"전투 기록을 확인할 수 없습니다.";
  // The lifecycle notice alone cannot describe the independently loaded reward preview.
  // Presentation follows the existing runtime authority; it never enables a claim.
  if(!loading&&matching&&data.NextAction=="REWARD_REVIEW"){
   record=state.RunRewardPreview==null?"전투 결과가 확인되었습니다.\n보상 내역을 불러오지 못했습니다.":previewLines.Count==0?"전투 결과가 확인되었습니다.\n보상 항목이 없습니다.":state.CanClaimRunReward(lifecycleRun)?state.LifecycleNotice:"전투 결과가 확인되었습니다.\n현재 보상을 수령할 수 없습니다.";
  }
  var text=loading?"보상 정보를 확인하고 있습니다.":"보상 정보 확인 필요";
  if(!loading&&matching){if(state.RunRewardPreview!=null)text=previewLines.Count==0?"보상 항목이 없습니다.":RecurringContract.RewardText(previewLines);else if(receiptSnapshot is JObject receipt)text="지급된 보상\n"+RecurringContract.RewardText((JArray)receipt["rewardPlan"]["rewardLines"]);}
  if(!loading&&!string.IsNullOrEmpty(state?.RunResultNotice))text+="\n"+state.RunResultNotice;
  dialog.Find("ResultRecordText").GetComponent<TMP_Text>().text=record;
  dialog.Find("DialogBody").GetComponent<TMP_Text>().text=text;
  dialog.Find("RefreshRunLifecycle").GetComponent<Button>().interactable=state!=null&&!state.LifecycleLoading&&!state.RunResultsLoading&&!state.RunReceiptHandoffLoading&&!state.Busy;
  dialog.Find("RefreshRunLifecycle").GetComponentInChildren<TMP_Text>().text=state?.CanRetryRunReceiptHandoff(lifecycleRun)==true?"영수증 저장 다시 시도":"기록 새로고침";
  dialog.Find("RecoverRunFinish").GetComponent<Button>().interactable=state?.CanRecoverFinish(lifecycleRun)==true;
  var claim=dialog.Find("ClaimRunReward").GetComponent<Button>();claim.interactable=state?.CanClaimRunReward(lifecycleRun)==true;
  claim.GetComponentInChildren<TMP_Text>().text=state?.SavedRewardClaim(lifecycleRun)?.AcceptedResponseJson==null&&state?.SavedRewardClaim(lifecycleRun)!=null?"지급 결과 다시 확인":receiptSnapshot!=null?"지급 완료":"보상 받기";
  ApplyResultControlVisuals();
 }
} }
