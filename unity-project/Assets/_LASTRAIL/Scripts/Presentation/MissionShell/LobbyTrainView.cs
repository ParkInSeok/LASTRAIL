using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    public sealed partial class LobbyDestinationPages
    {
        TrainScreenController trainController;
        async void LoadTrain(){if(trainController!=null)await trainController.LoadAsync();}
        void LiveTrain()
        {
            if(Category==0){BuildTrainOverview();return;}
            Tabs(new[]{"기관차","차량 배치","보유 차량"},202);
            TrainScreenController controller=null;Action<TrainScreenState> renderPage;int start=content.childCount;
            if(Category==1)renderPage=BuildTrainLoadout(()=>controller);
            else if(Category!=0){
                Panel("TrainHoldings",content,34,445,873,769);Text("HoldingsTitle",content,Category==2?"보유 차량":"보유 포탑",64,462,813,60,36,true);
                var empty=Text("NoTrainHoldings",content,"보유 정보가 없습니다.",65,650,811,95,31,true);var rows=new TMP_Text[6];
                for(int i=0;i<6;i++)rows[i]=Text("TrainHolding"+i,content,"",65,565+i*84,811,77,30);
                var page=Button("TrainHoldingsPage",content,64,1109,813,76,"",()=>controller.NextPage());
                Text("TrainEquipmentStatus",content,Category==2?"차량 배치 탭에서 장착을 확인하세요.":"포탑 장착은 준비 중입니다.",65,1245,811,85,28,true);
                Button("RefreshTrain",content,65,1360,811,90,"새로고침",async()=>await controller.RefreshAsync());
                renderPage=state=>{empty.gameObject.SetActive(state.Rows.Count==0);for(int i=0;i<6;i++){rows[i].gameObject.SetActive(i<state.Rows.Count);if(i>=state.Rows.Count)continue;rows[i].name="TrainHolding"+state.Rows[i].Index;rows[i].text=state.Rows[i].Label;}page.GetComponentInChildren<TMP_Text>().text=state.PageLabel;};
            }else{
                var name=Text("OwnedTrainName",content,"",65,840,811,80,41,true);
                Panel("TrainGrowthPanel",content,34,947,873,539);var level=Text("TrainLevel",content,"",64,970,813,66,38,true);
                var cost=Text("TrainGrowthCost",content,"",65,1052,811,133,30,true);var notice=Text("TrainGrowthNotice",content,"",65,1200,811,92,25,true);
                var upgrade=Button("UpgradeTrain",content,65,1320,811,100,"기관차 강화",async()=>await controller.UpgradeAsync(),true);
                Button("RefreshTrain",content,65,1432,811,45,"새로고침",async()=>await controller.RefreshAsync());
                renderPage=state=>{name.text=state.Name;level.text=state.Level;cost.text=state.Cost;notice.text=state.Notice;upgrade.GetComponentInChildren<TMP_Text>().text=state.UpgradeLabel;upgrade.interactable=state.CanUpgrade;};
            }
            var parts=content.Cast<Transform>().Skip(start).ToArray();var unavailablePanel=Panel("EmptyState",content,34,650,873,115);var unavailable=Text("EmptyStateText",content,"",62,674,817,70,29,true);
            var missingRefresh=Category==1?Button("RefreshTrainLoadout",content,65,1320,811,90,"다시 확인",async()=>await controller.RefreshAsync()):Button("RefreshTrain",content,60,1350,821,90,"다시 확인",async()=>await controller.RefreshAsync());
            controller=trainController=new TrainScreenController(accountState,Category,rosterPage,SelectedSlot,state=>{
                rosterPage=state.Page;SelectedSlot=state.Slot;foreach(var part in parts)part.gameObject.SetActive(state.Available);
                unavailablePanel.gameObject.SetActive(!state.Available);unavailable.gameObject.SetActive(!state.Available);unavailable.text=state.Unavailable;missingRefresh.gameObject.SetActive(!state.Available);if(state.Available)renderPage(state);
            },CoreConfirm);releasePageBinding+=controller.Dispose;renderBody=controller.Render;renderBody();
        }
        void BuildTrainOverview()
        {
            if(sharedFrame==null)sharedFrame=LobbyArtwork.CreateFrame();
            TrainScreenController controller=null;
            var view=TrainOverviewView.Create(content,font,sharedFrame,async()=>await controller.UpgradeAsync(),async()=>await controller.RefreshAsync(),()=>{Category=1;Rebuild();});
            controller=trainController=new TrainScreenController(accountState,0,0,0,view.Render,CoreConfirm);
            releasePageBinding+=()=>{controller.Dispose();view.Unbind();};renderBody=controller.Render;renderBody();
        }
        Action<TrainScreenState> BuildTrainLoadout(Func<TrainScreenController> controller)
        {
            Panel("LoadoutPanel",content,34,313,873,1190,new Color(.008f,.016f,.024f,.95f));
            var summary=Text("LoadoutState",content,"",55,330,831,62,31,true);var slots=new Button[4];var rows=new Button[6];string[] ids=new string[6];var selectedSkins=new System.Collections.Generic.Dictionary<Button,Sprite>();
            for(int i=0;i<4;i++){int slot=i;slots[i]=Button("TrainLoadoutSlot"+i,content,55+i%2*426,410+i/2*101,405,86,"",()=>controller().SelectSlot(slot),true);selectedSkins[slots[i]]=slots[i].GetComponent<Image>().sprite;}
            for(int i=0;i<6;i++){int cell=i;rows[i]=Button("InstallOwnedCarriage"+i,content,55,640+i*72,831,64,"",()=>controller().Install(ids[cell]),true);selectedSkins[rows[i]]=rows[i].GetComponent<Image>().sprite;}
            var empty=Text("NoLoadoutCarriages",content,"보유 차량이 없습니다.",65,690,811,110,30,true);
            var page=Button("LoadoutRosterPage",content,55,1095,831,61,"",()=>controller().NextPage());
            var notice=Text("LoadoutNotice",content,"",55,1168,831,122,24,true);
            var save=Button("SaveTrainLoadout",content,55,1310,831,87,"차량 배치 저장",async()=>await controller().SaveAsync(),true);
            var clear=Button("ClearTrainLoadout",content,55,1420,405,60,"선택 비우기",()=>controller().Clear());
            Button("RefreshTrainLoadout",content,481,1420,405,60,"새로고침",async()=>await controller().RefreshAsync());
            void RenderRow(Button button,TrainRowDisplay row){button.GetComponentInChildren<TMP_Text>().text=row.Label;button.interactable=row.Enabled;var image=button.GetComponent<Image>();image.sprite=row.Selected?selectedSkins[button]:null;image.type=image.sprite!=null?Image.Type.Sliced:Image.Type.Simple;image.color=image.sprite!=null?Color.white:row.Selected?new Color(.02f,.22f,.37f,.98f):new Color(.025f,.033f,.04f,.97f);button.transform.Find("Frame")?.gameObject.SetActive(image.sprite==null);}
            return state=>{
                summary.text=state.LoadoutState;for(int i=0;i<4;i++)RenderRow(slots[i],state.Slots[i]);
                for(int i=0;i<6;i++){bool exists=i<state.Rows.Count;rows[i].gameObject.SetActive(exists);ids[i]=exists?state.Rows[i].Id:null;if(!exists)continue;rows[i].name="InstallOwnedCarriage"+state.Rows[i].Index;RenderRow(rows[i],state.Rows[i]);}
                empty.gameObject.SetActive(state.Rows.Count==0);page.GetComponentInChildren<TMP_Text>().text=state.PageLabel;notice.text=state.Notice;
                save.GetComponentInChildren<TMP_Text>().text=state.SaveLabel;save.interactable=state.CanSave;clear.interactable=state.CanClear;
            };
        }
    }
}
