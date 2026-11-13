using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.MissionShell
{
    public sealed partial class LobbyCommandDeck
    {
        DepartureGuideView departureGuide;
        int departureGuideRequest;
        void CloseDepartureGuide(){departureGuideRequest++;if(departureGuide!=null)departureGuide.Close();departureGuide=null;}
        async void OpenDepartureGuide()
        {
            CloseDepartureGuide();
            var state=accountState;var snapshot=state?.Account;var sourceDialog=dialog;
            if(snapshot==null||state.AccountStatus!=AccountStateReadStatus.Ready)return;
            int request=departureGuideRequest;var train=state.TrainLoadout;var ultimate=state.Ultimate;
            DateTimeOffset? expires=null;bool usesLocalQuote=false;
            bool Current()=>this!=null&&isActiveAndEnabled&&departureDialog&&dialog==sourceDialog&&sourceDialog!=null&&
                request==departureGuideRequest&&accountState==state&&ReferenceEquals(state.Account,snapshot)&&state.AccountStatus==AccountStateReadStatus.Ready&&
                ReferenceEquals(state.TrainLoadout,train)&&ReferenceEquals(state.Ultimate,ultimate)&&(!expires.HasValue||expires>DateTimeOffset.UtcNow)&&(!usesLocalQuote||localDeparture?.CanStart==true);
            var support=Array.Empty<string>();
            if(localCommandClient!=null){
                try{
                    var read=await localCommandClient.ReadSupportAsync();
                    support=((JArray)read["selection"]["slots"]).Select(x=>(string)x["skillId"]).Select(GuideSupportName).ToArray();
                }catch{ /* A missing read remains explicitly unavailable; navigation cannot retry a write. */ }
            }
            if(!Current())return;
            var carriages=(state.TrainLoadout?.Loadout?["carriages"] as JArray)?.Select(x=>((int)x["slot"]+1)+" · "+(string)x["carriageId"]).ToArray()??Array.Empty<string>();
            var quote=state.Departure;string cost="출발 준비에서 최신 비용을 확인해 주세요.";
            if(localDeparture!=null){var value=train!=null&&ultimate!=null?localDeparture.ReadGuideFuelCost(departureOperation,DepartureContract.Risks[departureRisk],snapshot.FormationRevision,train.Revision,ultimate.Revision):null;if(value!=null){cost="소모 연료 "+value;usesLocalQuote=true;}}
            else if(quote!=null&&quote.OperationId==departureOperation&&quote.Risk==DepartureContract.Risks[departureRisk]&&quote.ExpiresAt>DateTimeOffset.UtcNow)
                {cost="소모 연료 "+quote.FuelCost+" · 보유 "+quote.AvailableFuel;expires=quote.ExpiresAt;}
            var data=new DepartureGuideData(departureLabel+" · 위험 "+DepartureContract.Risks[departureRisk],snapshot.ActiveOperators.OrderBy(x=>x.Number("slot")).Select(x=>x.Text("operatorId")),carriages,support,cost);
            departureGuide=DepartureGuideView.OpenFocused(FixedUi,font,frameSkin,data,preparationView.GuideTargets,Current);
        }
        static string GuideSupportName(string id)
        {
            switch(id){case "support_fire":return "지원 사격";case "barrier":return "방어막";case "emergency_repair":return "긴급 수리";
                case "ce03.role.suppressive_barrage":return "제압 포격 · 준비 중";case "ce03.role.medical_support":return "의료 지원 · 준비 중";
                case "ce03.role.smoke":return "연막 전개 · 준비 중";case "ce03.role.weakness_scan":return "약점 스캔 · 준비 중";
                case "ce03.role.firepower_boost":return "화력 증폭 · 준비 중";case "ce03.role.electronic_disruption":return "전자기 교란 · 준비 중";
                case "ce03.role.emergency_protection":return "긴급 방호 · 준비 중";default:return id;}
        }
    }
}
