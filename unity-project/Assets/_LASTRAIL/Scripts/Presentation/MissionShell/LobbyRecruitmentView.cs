using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    public sealed partial class LobbyDestinationPages
    {
        void LiveRecruitment()
        {
            var account=accountState;if(account==null){Unavailable(600,"모집 정보를 불러오지 못했습니다.");return;}
            long entry=entryGeneration;
            bool Current()=>this!=null&&isActiveAndEnabled&&IsOpen&&CurrentPage==LobbyPage.Recruit&&entry==entryGeneration&&accountState==account;
            var nodes=new Dictionary<string,GameObject>();var actions=new Dictionary<string,Action>();
            RecruitmentScreenController controller=null;Sprite primary=null;
            Button Button(string name,Transform parent,float x,float y,float w,float h,string label,Action action,bool selected=false)
            {
                actions[name]=action;
                if(!nodes.TryGetValue(name,out var node)){var created=this.Button(name,parent,x,y,w,h,"",()=>{if(Current()&&actions.TryGetValue(name,out var current))current?.Invoke();});node=created.gameObject;nodes.Add(name,node);}
                node.SetActive(true);var button=node.GetComponent<Button>();button.interactable=true;button.GetComponentInChildren<TMP_Text>().text=label;
                if(selected&&primary==null){var texture=LastRail.Resources.PreparedContent.Load<Texture2D>("EntryUi/primary");if(texture!=null){primary=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(35,14,35,14));skins.Add(primary);}}
                button.image.sprite=selected&&primary!=null?primary:null;button.image.color=selected&&primary!=null?Color.white:selected?new Color(.02f,.22f,.37f,.98f):new Color(.025f,.033f,.04f,.97f);
                var frame=node.transform.Find("Frame");if(frame!=null)frame.gameObject.SetActive(!selected||primary==null);return button;
            }
            TMP_Text Text(string name,Transform parent,string value,float x,float y,float w,float h,float size,bool center=false)
            {
                if(!nodes.TryGetValue(name,out var node)){node=this.Text(name,parent,"",x,y,w,h,size,center).gameObject;nodes.Add(name,node);}
                node.SetActive(true);var label=node.GetComponent<TMP_Text>();label.text=value;return label;
            }
            void ListControls(RecruitmentScreenState state,bool more)
            {
                Button("RecruitPrevious",content,65,1070,208,64,"이전",()=>controller.MovePage(-1));
                Text("RecruitPage",content,(state.Page+1)+" / "+state.Pages,280,1080,162,45,25,true);
                Button("RecruitNext",content,447,1070,208,64,"다음",()=>controller.MovePage(1));
                if(more)Button("RecruitLoadMore",content,666,1070,210,64,"더 불러오기",async()=>await controller.MoreAsync()).interactable=state.CanMore;
            }
            Panel("RecruitReadPanel",content,34,325,873,835,new Color(.008f,.016f,.024f,.94f));
            void Render(RecruitmentScreenState state)
            {
                foreach(var node in nodes.Values)node.SetActive(false);actions.Clear();
                Button("RecruitMain",content,34,210,273,73,"모집",async()=>await controller.SelectModeAsync(RecruitmentScreenMode.Main),state.Mode==RecruitmentScreenMode.Main);
                Button("RecruitHistory",content,334,210,273,73,"모집 기록",async()=>await controller.SelectModeAsync(RecruitmentScreenMode.History),state.Mode==RecruitmentScreenMode.History);
                Button("RecruitRates",content,634,210,273,73,"확률 정보",async()=>await controller.SelectModeAsync(RecruitmentScreenMode.Rates),state.Mode==RecruitmentScreenMode.Rates);
                Text("RecruitReadNotice",content,state.Notice,55,1168,831,80,24,true);
                if(state.Mode==RecruitmentScreenMode.History){
                    Text("RecruitHistoryTitle",content,state.Title,60,350,821,70,33,true);
                    for(int i=0;i<state.Rows.Count;i++){var row=state.Rows[i];Button("ViewBatch"+i,content,65,443+i*99,811,86,row.Label,async()=>await controller.SelectRowAsync(row.Id));}
                    ListControls(state,true);Button("RefreshRecruitHistory",content,65,1267,811,88,"기록 새로고침",async()=>await controller.RefreshHistoryAsync());
                }else if(state.Mode==RecruitmentScreenMode.Batch){
                    Text("BatchTitle",content,state.Title,60,343,821,66,34,true);
                    if(state.HasBatch){Text("BatchPolicy",content,state.Policy,65,412,811,62,23,true);for(int i=0;i<state.Rows.Count;i++)Text("BatchResult"+(i+1),content,state.Rows[i].Label,65,492+i*61,811,57,26);}
                    Button("RefreshRecruitBatch",content,65,1267,811,88,"같은 결과 재조회",async()=>await controller.RefreshBatchAsync());
                    if(state.PendingPull)Button("RetryRecruit",content,65,1370,811,83,"원래 모집 요청 재확인",async()=>await controller.RetryAsync()).interactable=state.CanRefresh;
                }else if(state.Mode==RecruitmentScreenMode.Rates){
                    Text("RatesTitle",content,"기본 확률 / 다음 1회 확률",60,347,821,63,34,true);
                    Text("RatesCaution",content,"다음 1회 확률은 10회 모집 전체에 고정 적용되지 않습니다.",60,420,821,90,25,true);
                    if(state.RatesReady){for(int i=0;i<state.Rows.Count;i++)Text("Probability"+i,content,state.Rows[i].Label,70,535+i*78,801,73,24);ListControls(state,false);}
                    else Text("RatesUnavailable",content,"배너를 선택하고 상세 정보를 확인해 주세요.",70,640,801,100,29,true);
                }else{
                    Text("RecruitBannerTitle",content,state.Title,65,363,811,90,34,true);
                    for(int i=0;i<state.Rows.Count;i++){var row=state.Rows[i];Button("SelectRecruitBanner"+i,content,65,468+i*77,811,65,row.Label,async()=>await controller.SelectRowAsync(row.Id),row.Selected);}
                    if(state.HasBanner)Text("RecruitPity",content,state.Pity,65,881,811,146,25,true);
                    ListControls(state,true);Text("RecruitTicketBalance",content,"모집권 "+state.TicketBalance+"개",65,1233,811,56,29,true);
                    foreach(int count in new[]{1,10}){float x=count==1?36:482;var price=count==1?state.OnePrice:state.TenPrice;var button=Button(count==1?"RecruitOne":"RecruitTen",content,x,1300,423,105,count+"회 모집\n모집권 "+price+"개",()=>controller.RequestRecruit(count),true);button.interactable=count==1?state.CanRecruitOne:state.CanRecruitTen;}
                    Button("RefreshRecruitment",content,36,1423,871,71,state.PendingPull?"원래 모집 요청 재확인":"배너 새로고침",async()=>await controller.RefreshAsync()).interactable=state.CanRefresh;
                }
                Fit();
            }
            controller=new RecruitmentScreenController(account,Render,CoreConfirm,Current);
            releasePageBinding+=controller.Dispose;
            releasePageBinding+=actions.Clear;
            renderBody=controller.Render;renderBody();_ = controller.LoadAsync();
        }
    }
}
