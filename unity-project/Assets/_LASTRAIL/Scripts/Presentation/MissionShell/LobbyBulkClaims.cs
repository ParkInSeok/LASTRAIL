using System;
using TMPro;
using UnityEngine;
namespace LastRail.Presentation.MissionShell
{
 public sealed partial class LobbyCommandDeck
 {
  bool bulkDialog;Action renderBulk;BulkClaimScreenController bulkController;
  void ReleaseBulkBinding(){bulkController?.Dispose();bulkController=null;renderBulk=null;}
  public void OpenBulkSelection(string kind,string policy=null,string track=null)
  {
   if(accountState==null)return;if(accountState.Pending?.Kind=="recurring-bulk"){OpenBulkResults();return;}
   var selection=accountState.PrepareBulkSelection(kind,policy,track);
   if(selection==null){ShowInfo("선택 수령","최신 보상 상태와 이전 요청 결과를 먼저 확인해 주세요.");return;}
   ShowInfo("선택 수령","");bulkDialog=true;PrepareBulkDialog();BuildBulkView(selection,false,false);
  }
  void OpenBulkTracks(string policy){if(accountState?.Pending?.Kind=="recurring-bulk"){OpenBulkResults();return;}ShowInfo("패스 선택 수령","수령할 보상 종류를 선택하세요.");Button("BulkFree",dialog,60,400,630,75,"무료 보상",()=>OpenBulkSelection("PASS_LEVEL",policy,"FREE"));Button("BulkPremium",dialog,60,500,630,75,"프리미엄 보상",()=>OpenBulkSelection("PASS_LEVEL",policy,"PREMIUM"));}
  public void OpenBulkResults()=>ShowBulkResults(false);
  void OpenPassClaimAllResults()=>ShowBulkResults(true);
  void ShowBulkResults(bool passResults){ShowInfo("수령 결과","");bulkDialog=true;PrepareBulkDialog();BuildBulkView(null,true,passResults);}
  void PrepareBulkDialog(){dialog.sizeDelta=new Vector2(750,1240);dialog.Find("DialogBody").gameObject.SetActive(false);Place(dialog.Find("CloseLobbyDialog").GetComponent<RectTransform>(),60,1140,630,65);EvaluateLayout();}
  void BulkChanged(){if(bulkDialog&&dialog!=null)bulkController?.Changed();}
  void BuildBulkView(BulkClaimSelection selection,bool resultsMode,bool passResults)
  {
   if(accountState==null)return;var owner=accountState;var ownedDialog=dialog;
   var area=Rect("BulkContent",dialog);Place(area,0,0,750,1130);
   var summary=Label("BulkSummary",area,"",40,135,670,105,27,TextAlignmentOptions.Center);
   var empty=Label("BulkEmpty",area,"",40,315,670,150,27,TextAlignmentOptions.Center);
   var choices=new UnityEngine.UI.Button[5];var results=new TMP_Text[5];var ids=new string[5];BulkClaimScreenController controller=null;
   for(int i=0;i<5;i++){int cell=i;float y=260+i*133;
    choices[i]=Button("BulkSelect"+i,area,40,y,670,116,"",()=>controller.Toggle(ids[cell]));choices[i].GetComponentInChildren<TMP_Text>().fontSize=23;
    results[i]=Label("BulkItem"+i,area,"",50,y,650,123,23,TextAlignmentOptions.MidlineLeft);
   }
   Button("BulkPrevious",area,40,935,190,55,"‹",()=>controller.MovePage(-1));
   var page=Label("BulkPage",area,"",250,935,250,55,25,TextAlignmentOptions.Center);
   Button("BulkNext",area,520,935,190,55,"›",()=>controller.MovePage(1));
   var all=Button("BulkSelectAll",area,40,1010,325,80,"",()=>controller.ToggleAll());
   var submit=Button("BulkConfirm",area,385,1010,325,80,"",async()=>await controller.SubmitAsync());
   var passResume=Button("PassAllContinue",area,40,1010,670,80,"",async()=>await controller.ContinuePassAsync());
   var read=Button("BulkRead",area,40,1010,325,80,"기록 조회",async()=>await controller.ReadAsync());
   var resume=Button("BulkContinue",area,385,1010,325,80,"",async()=>await controller.ContinueAsync());
   void Render(BulkClaimScreenState state)
   {
    page.text=(state.Page+1)+" / "+state.Pages;summary.text=state.Summary;empty.gameObject.SetActive(state.Rows.Count==0);empty.text=state.EmptyText;
    for(int i=0;i<5;i++){bool exists=i<state.Rows.Count;choices[i].gameObject.SetActive(exists&&!state.Results);results[i].gameObject.SetActive(exists&&state.Results);ids[i]=null;if(!exists)continue;var row=state.Rows[i];ids[i]=row.Id;choices[i].GetComponentInChildren<TMP_Text>().text=row.Text;choices[i].interactable=state.Editable;results[i].text=row.Text;}
    all.gameObject.SetActive(!state.Results);submit.gameObject.SetActive(!state.Results);passResume.gameObject.SetActive(state.Results&&state.PassResults);read.gameObject.SetActive(state.Results&&!state.PassResults);resume.gameObject.SetActive(state.Results&&!state.PassResults);
    all.GetComponentInChildren<TMP_Text>().text=state.SelectAllText;all.interactable=state.CanSelectAll;submit.GetComponentInChildren<TMP_Text>().text=state.SubmitText;submit.interactable=state.CanSubmit;
    passResume.GetComponentInChildren<TMP_Text>().text=state.CanContinuePass?"이어받기":"처리 완료";passResume.interactable=state.CanContinuePass;read.interactable=state.CanRead;resume.GetComponentInChildren<TMP_Text>().text=state.ContinueText;resume.interactable=state.CanContinue;
   }
   controller=new BulkClaimScreenController(owner,selection,resultsMode,passResults,Render,CloseDialog,OpenBulkResults,()=>this!=null&&isActiveAndEnabled&&bulkDialog&&dialog==ownedDialog&&accountState==owner);
   bulkController=controller;renderBulk=controller.Render;renderBulk();
  }
 }
}
