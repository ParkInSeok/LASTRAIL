using LastRail.Run;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Presentation.MissionShell {public sealed partial class LobbyCommandDeck {
 bool lifecycleDialog;string lifecycleRun;RunLifecycleScreenController lifecycleController;
 void ReleaseLifecycleBinding(){lifecycleController?.Dispose();lifecycleController=null;}
 public void OpenRunLifecycle(string runId,bool reuseKnownState=false)=>OpenResourceDialog(new[]{"NoncombatResult/","NoncombatVictory/","EntryUi/secondary","EntryPopup/Rewards/","EntryPopup/roulette-reward-icons"},()=>OpenRunLifecyclePrepared(runId,reuseKnownState));
 void OpenRunLifecyclePrepared(string runId,bool reuseKnownState){ShowInfo("전투·보상 기록","",ReturnFromRunLifecycle);lifecycleDialog=true;lifecycleRun=runId;dialog.sizeDelta=new Vector2(750,1000);Place(dialog.Find("DialogBody").GetComponent<RectTransform>(),48,140,650,490);Button("RecoverRunFinish",dialog,60,660,630,65,"결과 전송 다시 확인",RecoverCurrentRunFinish);Button("ClaimRunReward",dialog,60,740,630,65,"보상 받기",ClaimCurrentRunReward);Button("RefreshRunLifecycle",dialog,60,820,630,65,"기록 새로고침",RefreshCurrentRunResults);var close=dialog.Find("CloseLobbyDialog").GetComponent<Button>();Place(close.GetComponent<RectTransform>(),60,905,630,65);close.GetComponentInChildren<TMP_Text>().text="로비로 돌아가기";lifecycleController=new RunLifecycleScreenController(accountState,runId,OpenCombatAdView);BuildResultPresentation();EvaluateLayout();LifecycleChanged();_ = lifecycleController.LoadAsync(reuseKnownState);}
 async void ClaimCurrentRunReward(){if(lifecycleController!=null)await lifecycleController.ClaimAsync();}
 async void RecoverCurrentRunFinish(){if(lifecycleController!=null)await lifecycleController.RecoverAsync();}
 async void RefreshCurrentRunResults(){if(lifecycleController!=null)await lifecycleController.RefreshAsync();}
 void ReturnFromRunLifecycle(){CloseDialog();Pages?.Close();}
 void OpenResultOperations(){if(!isActiveAndEnabled||!lifecycleDialog||accountState==null)return;accountState.SynchronizeSession();if(!new RunVictoryDisplayState(accountState,lifecycleRun,missions).CanViewOperations)return;OpenOperationList();}
 void LifecycleChanged(){
  if(!lifecycleDialog||dialog==null||lifecycleController==null)return;
  var display=lifecycleController.Display;if(display==null)return;UpdateResultPresentation();
  dialog.Find("ResultRecordText").GetComponent<TMP_Text>().text=display.Record;
  dialog.Find("DialogBody").GetComponent<TMP_Text>().text=display.Rewards;
  dialog.Find("RefreshRunLifecycle").GetComponent<Button>().interactable=display.CanRefresh;
  dialog.Find("RefreshRunLifecycle").GetComponentInChildren<TMP_Text>().text=display.RefreshLabel;
  dialog.Find("RecoverRunFinish").GetComponent<Button>().interactable=display.CanRecover;
  var claim=dialog.Find("ClaimRunReward").GetComponent<Button>();claim.interactable=display.CanClaim;
  claim.GetComponentInChildren<TMP_Text>().text=display.ClaimLabel;ApplyResultControlVisuals();
 }
} }
