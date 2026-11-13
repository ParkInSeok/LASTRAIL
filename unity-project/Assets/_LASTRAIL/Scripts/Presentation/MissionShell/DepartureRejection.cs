using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell
{
    [Flags] public enum DepartureRefresh { None=0, Account=1, Train=2, Ultimate=4 }
    // Validated rejection metadata only. It never authorizes a new attempt or supplies replacement values.
    public sealed class DepartureRejection
    {
        public string Notice {get;}
        public DepartureRefresh Refresh {get;}
        DepartureRejection(string notice,DepartureRefresh refresh){Notice=notice;Refresh=refresh;}
        static readonly IReadOnlyDictionary<string,string> Labels=new Dictionary<string,string>{
            {"FORMATION","대원 편성"},{"LOADOUT","열차 배치"},{"OWNED_STATE","보유 대원·차량 상태"},{"COST","출격 비용"},
            {"POLICY","적용 규칙"},{"PROGRESSION","진행 조건"},{"COMMANDER","지휘관 특성"},{"ULTIMATE","궁극기 선택"},{"MAP","작전 지도"},{"ADMISSION","출격 조건"}};
        static DepartureRefresh Fields(IEnumerable<string> fields)
        {
            var result=DepartureRefresh.None;
            foreach(var field in fields)result|=field switch {
                "FORMATION"=>DepartureRefresh.Account|DepartureRefresh.Ultimate,
                "OWNED_STATE"=>DepartureRefresh.Account|DepartureRefresh.Train,
                "LOADOUT" or "PROGRESSION"=>DepartureRefresh.Train,
                "ULTIMATE"=>DepartureRefresh.Ultimate,
                _=>DepartureRefresh.None // Cost/policy/map/commander/admission are revalidated by the next explicit preview.
            };
            return result;
        }
        public static DepartureRejection Read(JObject response,string preview)
        {
            const string fallback="출발 조건이 변경되었거나 출발할 수 없습니다. 상태를 확인하고 새 견적을 받아 주세요.";
            string code=response?["errorCode"]?.Type==JTokenType.String?(string)response["errorCode"]:null;
            if(code=="departure_quote_changed"&&response["departureChange"] is JObject d&&
                d["version"]?.Type==JTokenType.String&&(string)d["version"]=="departure.quote-change.v1"&&
                d["operationPreviewId"]?.Type==JTokenType.String&&(string)d["operationPreviewId"]==preview&&
                d["newAttemptAuthorized"]?.Type==JTokenType.Boolean&&!(bool)d["newAttemptAuthorized"]&&
                d["changedFields"] is JArray fields&&fields.Count>0&&fields.Count<=Labels.Count&&
                fields.All(x=>x.Type==JTokenType.String&&Labels.ContainsKey((string)x))&&fields.Values<string>().Distinct().Count()==fields.Count){
                var names=fields.Values<string>().ToArray();
                return new DepartureRejection("출격 조건이 변경되었습니다.\n"+string.Join("·",names.Select(x=>Labels[x]))+"\n준비 정보를 확인한 뒤 비용을 다시 확인해 주세요.",Fields(names));
            }
            return code switch {
                "insufficient_fuel" or "fuel_state_unavailable"=>new DepartureRejection("연료 정보를 확인해 주세요. 보유 연료와 출격 비용을 다시 확인해야 합니다.",DepartureRefresh.Account),
                "operation_preview_not_startable"=>new DepartureRejection("출격 견적이 만료되었거나 사용할 수 없습니다. 비용을 다시 확인해 주세요.",DepartureRefresh.None),
                "combat_not_ready"=>new DepartureRejection("현재 버전에서는 출격할 수 없습니다.",DepartureRefresh.None),
                "departure_selection_changed"=>new DepartureRejection("편성 또는 열차·궁극기 선택이 변경되었습니다. 준비 정보를 확인한 뒤 비용을 다시 확인해 주세요.",DepartureRefresh.Account|DepartureRefresh.Train|DepartureRefresh.Ultimate),
                "formation_required" or "formation_schema_unsupported" or "formation_operator_not_owned"=>new DepartureRejection("대원 편성 또는 보유 정보를 확인해 주세요.",DepartureRefresh.Account|DepartureRefresh.Ultimate),
                "ultimate_selection_required"=>new DepartureRejection("궁극기를 선택해 주세요.",DepartureRefresh.Ultimate),
                "train_loadout_required" or "departure_open_run" or "operation_locked"=>new DepartureRejection("열차 배치 또는 작전 진행 조건을 확인해 주세요.",DepartureRefresh.Train),
                _=>new DepartureRejection(fallback,DepartureRefresh.None)
            };
        }
    }
}
