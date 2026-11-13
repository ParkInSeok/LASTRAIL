using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Presentation.MissionShell
{
    public sealed partial class LobbyDestinationPages
    {
        bool ultimatePanel;
        async void LoadUltimatePage(){var state=accountState;if(state!=null)await System.Threading.Tasks.Task.WhenAll(state.EnsureAccountAsync(),state.EnsureUltimateAsync());}
        async void RefreshUltimatePage(){var state=accountState;if(state!=null)await System.Threading.Tasks.Task.WhenAll(state.RefreshAccountAsync(),state.RefreshUltimateAsync());}
        void LiveUltimate()
        {
            var state=accountState;
            Button("BackToFormation",content,34,212,210,65,"편성으로",()=>{ultimatePanel=false;Rebuild();});
            Text("UltimateTitle",content,"분대장 선택",265,213,610,65,35,true);
            Text("UltimateHelp",content,"분대를 이끌 대원을 선택하세요\n분대장의 궁극기를 전투에서 사용할 수 있습니다",40,310,860,80,26,true);
            var members=state?.Account?.ActiveOperators.OrderBy(o=>o.Number("slot")).ToArray();
            if(members?.Length==5)for(int i=0;i<5;i++){
                var id=members[i].Text("operatorId");var selected=state.UltimateDraft==id;
                var button=Button("UltimateMember"+i,content,55,405+i*145,831,125,OperatorVisualCatalog.Label(id)+(selected?"   분대장":""),()=>state.SelectUltimate(id),selected);
                button.interactable=state.CanEditUltimate;
                var portrait=OperatorVisualCatalog.Portrait(id);
                if(portrait!=null){var image=Rect("UltimatePortrait",button.transform,17,12,98,98).gameObject.AddComponent<RawImage>();image.texture=portrait;image.uvRect=OperatorVisualCatalog.Crop(portrait,98,98);image.raycastTarget=false;}
            }
            Text("UltimateNotice",content,(state?.UltimateNotice??"편성 정보를 불러오지 못했습니다.")+"\n"+(state?.ActionNotice??""),40,1150,861,135,26,true);
            var clear=Button("ClearUltimate",content,34,1310,267,75,"선택 해제",()=>state?.SelectUltimate(null));clear.interactable=state?.CanEditUltimate==true;
            var refresh=Button("RefreshUltimate",content,337,1310,267,75,"다시 확인",()=>RefreshUltimatePage());refresh.interactable=state!=null&&!state.UltimateLoading&&!state.Busy;
            var save=Button("SaveUltimate",content,640,1310,267,75,"저장",async()=>{if(state!=null)await state.SaveUltimateAsync();},true);save.interactable=state?.CanEditUltimate==true;
            if(state?.Pending?.Kind=="ultimate-selection"){
                save.GetComponentInChildren<TMPro.TMP_Text>().text="결과 재확인";save.onClick.RemoveAllListeners();save.onClick.AddListener(async()=>{await state.RetryPendingAsync();});save.interactable=!state.Busy;
            }
        }
    }
}
