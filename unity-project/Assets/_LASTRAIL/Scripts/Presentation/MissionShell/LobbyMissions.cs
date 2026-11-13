using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Presentation.MissionShell {public sealed partial class LobbyDestinationPages {
 int questPage;
 async void RefreshMissions(){if(accountState!=null)await accountState.SyncRecurringAsync();}
 public void OpenQuestMail(){Open(LobbyPage.Missions);Category=2;questPage=0;Rebuild();}
 void LiveMissions(){var state=accountState;var data=state?.Recurring;
  Tabs(new[]{"일일","주간","우편","업적"},260);
  Text("MissionScope",content,Category==0?"일일 임무 · 매일 04시 갱신":Category==1?"주간 임무 · 월요일 04시 갱신":Category==2?"임무 보상 우편 · 발송 후 14일":"업적",40,351,861,70,36,true);
  if(Category==3){Unavailable(490,"등록된 업적 정보를 불러올 수 없습니다.");return;}
  Text("MissionSyncNotice",content,(state?.RecurringNotice??"정보를 불러올 수 없습니다.")+"\n"+(state?.ActionNotice??""),40,427,861,115,25,true);
  if(data==null){Unavailable(570,"임무 정보를 불러오지 못했습니다.");}
  else{
   var progressSystem=Category<2?data.Progress?.SystemState("QUESTS"):null;var progressRows=progressSystem?["objectives"] as JArray;
   var rows=Category==2?data.Mail.Cast<JObject>().ToArray():progressRows!=null?progressRows.Cast<JObject>().Where(a=>(string)a["frequency"]==(Category==0?"DAILY":"WEEKLY")).ToArray():data.Awards.Cast<JObject>().Where(a=>(string)a["system"]=="QUESTS"&&((string)a["period_id"]).StartsWith(Category==0?"D:":"W:")&&(long)a["period_end"]>data.ServerTime).ToArray();
   var count=Math.Max(1,(rows.Length+3)/4);questPage=Math.Min(questPage,count-1);
   if(rows.Length==0)Unavailable(580,Category==2?"도착한 임무 보상 우편이 없습니다.":progressSystem!=null?"진행 중인 임무가 없습니다.":data.Policies.Any(p=>(string)p["system"]=="QUESTS"&&(bool)p["active"])?"서버에서 확인된 달성 임무가 없습니다.":"진행 중인 임무가 없습니다.");
   for(int i=0;i<4;i++){int index=questPage*4+i;if(index>=rows.Length)break;var item=rows[index];var mail=Category==2;var progress=progressRows!=null;var id=(string)item[mail?"award_id":progress?"awardId":"id"];var award=mail?(JObject)data.Awards.First(a=>(string)a["id"]==id):item;var status=(string)item["status"];float y=570+i*178;Panel("QuestRow"+i,content,34,y,873,164);
    Text("QuestName"+i,content,RecurringContract.Objective((string)award[progress?"objectiveId":"objective"]),58,y+9,590,45,30);
    if(progress)Text("QuestProgress"+i,content,ObjectiveProgress.ProgressText(item),58,y+54,590,36,20);
    Text("QuestRewards"+i,content,RecurringContract.RewardText((JArray)award["rewards"]),58,y+(progress?95:59),590,progress?58:90,progress?21:23);
    var kind=mail?"recurring-mail":"recurring-quest";var text=status=="CLAIMED"?"수령 완료":status=="EXPIRED"?"기한 만료":status=="MAIL_PENDING"?"우편 준비":status=="MAILED"?"우편 발송":"받기";
    if(progress)text=ObjectiveProgress.StatusText(item);var button=Button("ClaimQuest"+i,content,686,y+39,191,72,text,async()=>{await state.ClaimRecurringAsync(kind,id);},true);button.interactable=id!=null&&state.CanClaimRecurring(kind,id);
    if(mail)Text("MailExpiry"+i,content,"만료 "+DateTimeOffset.FromUnixTimeMilliseconds((long)item["expires_at"]).ToOffset(TimeSpan.FromHours(9)).ToString("MM/dd HH:mm"),650,y+119,238,30,19,true);
   }
   Button("QuestPages",content,647,1302,259,63,(questPage+1)+" / "+count,()=>{questPage=(questPage+1)%count;Rebuild();});
   Text("CooperativeUnknown",content,"협동 참가자 진행은 아직 확인할 수 없습니다.",40,1305,596,63,23,true);
  }
  var bulk=Button("BulkQuests",content,481,1392,420,88,state?.Pending?.Kind=="recurring-bulk"?"수령 결과":"선택 수령",()=>GetComponentInParent<LobbyCommandDeck>()?.OpenBulkSelection(Category==2?"MAIL":"QUEST"));bulk.interactable=state!=null&&!state.Busy&&(state.Pending?.Kind=="recurring-bulk"||state.Pending==null&&state.RecurringReady&&!state.RecurringLoading);
  var retry=state?.Pending?.Kind.StartsWith("recurring-")==true;var refresh=Button("RefreshQuests",content,40,1392,421,88,retry?"요청 결과 재확인":"진행 갱신",async()=>{if(state==null)return;if(retry)await state.RetryPendingAsync();else await state.SyncRecurringAsync();});refresh.interactable=state!=null&&!state.Busy&&!state.RecurringLoading&&(state.Pending==null||retry);
 }
} }
