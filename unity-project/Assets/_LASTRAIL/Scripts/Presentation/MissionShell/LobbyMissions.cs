using System;
using UnityEngine;
namespace LastRail.Presentation.MissionShell {public sealed partial class LobbyDestinationPages {
 int questPage;MissionScreenController missionController;
 async void RefreshMissions(){if(missionController!=null)await missionController.LoadAsync();}
 public void OpenQuestMail(){Open(LobbyPage.Missions);Category=2;questPage=0;Rebuild();}
 void LiveMissions(){
  int category=Category;MissionScreenController controller=null;
  Tabs(new[]{"일일","주간","우편","업적"},260);
  Text("MissionScope",content,category==0?"일일 임무 · 매일 04시 갱신":category==1?"주간 임무 · 월요일 04시 갱신":category==2?"임무 보상 우편 · 발송 후 14일":"업적",40,351,861,70,36,true);
  if(category==3){Unavailable(490,"등록된 업적 정보를 불러올 수 없습니다.");missionController=new MissionScreenController(accountState,category,questPage,null,openBulkSelection);releasePageBinding+=missionController.Dispose;return;}
  var notice=Text("MissionSyncNotice",content,"",40,427,861,115,25,true);
  var emptyPanel=Panel("EmptyState",content,34,570,873,130);var empty=Text("EmptyStateText",emptyPanel,"",28,24,817,70,29,true);
  var cards=new RectTransform[4];var names=new TMPro.TMP_Text[4];var progressLabels=new TMPro.TMP_Text[4];var rewards=new TMPro.TMP_Text[4];var expiry=new TMPro.TMP_Text[4];var claims=new UnityEngine.UI.Button[4];
  for(int i=0;i<4;i++){
   int cell=i;float y=570+i*178;var card=cards[i]=Panel("QuestRow"+i,content,34,y,873,164);
   names[i]=Text("QuestName"+i,card,"",24,9,590,45,30);progressLabels[i]=Text("QuestProgress"+i,card,"",24,54,590,36,20);
   rewards[i]=Text("QuestRewards"+i,card,"",24,95,590,58,21);
   claims[i]=Button("ClaimQuest"+i,card,652,39,191,72,"",async()=>await controller.ClaimAsync(cell),true);
   expiry[i]=Text("MailExpiry"+i,card,"",616,119,238,30,19,true);
  }
  var page=Button("QuestPages",content,647,1302,259,63,"",()=>controller.NextPage());
  Text("CooperativeUnknown",content,"협동 참가자 진행은 아직 확인할 수 없습니다.",40,1305,596,63,23,true);
  var bulk=Button("BulkQuests",content,481,1392,420,88,"",()=>controller.OpenBulk());
  var refresh=Button("RefreshQuests",content,40,1392,421,88,"",async()=>await controller.RefreshAsync());
  controller=missionController=new MissionScreenController(accountState,category,questPage,state=>{
   questPage=state.Page;notice.text=state.Notice;emptyPanel.gameObject.SetActive(state.Rows.Count==0);empty.text=state.Empty;
   for(int i=0;i<4;i++){
    bool exists=i<state.Rows.Count;cards[i].gameObject.SetActive(exists);if(!exists)continue;var item=state.Rows[i];
    names[i].text=item.Name;progressLabels[i].gameObject.SetActive(item.ShowProgress);progressLabels[i].text=item.Progress;
    rewards[i].text=item.Rewards;rewards[i].rectTransform.anchoredPosition=new Vector2(24,item.ShowProgress?-95:-59);rewards[i].rectTransform.sizeDelta=new Vector2(590,item.ShowProgress?58:90);rewards[i].fontSizeMax=item.ShowProgress?21:23;
    claims[i].GetComponentInChildren<TMPro.TMP_Text>().text=item.ClaimLabel;claims[i].interactable=item.CanClaim;expiry[i].gameObject.SetActive(item.Mail);expiry[i].text=item.Expiry;
   }
   page.GetComponentInChildren<TMPro.TMP_Text>().text=state.PageLabel;page.gameObject.SetActive(state.ShowPages);
   bulk.GetComponentInChildren<TMPro.TMP_Text>().text=state.BulkLabel;bulk.interactable=state.CanBulk;
   refresh.GetComponentInChildren<TMPro.TMP_Text>().text=state.RefreshLabel;refresh.interactable=state.CanRefresh;
  },openBulkSelection);releasePageBinding+=controller.Dispose;renderBody=controller.Render;renderBody();
 }
} }
