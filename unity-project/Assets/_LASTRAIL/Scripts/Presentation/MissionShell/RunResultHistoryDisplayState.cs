using System;
using System.Collections.Generic;
using System.Linq;

namespace LastRail.Presentation.MissionShell
{
    public sealed class RunResultHistoryDisplayState
    {
        public string Heading => "출발 편성";
        public IReadOnlyList<string> OperatorIds { get; }
        public IReadOnlyList<string> Names { get; }
        public string FormationNotice { get; }
        public string HistoryNotice { get; }
        public bool HasFormation => OperatorIds.Count==5;

        public RunResultHistoryDisplayState(AccountScreenRuntime owner,string runId)
        {
            bool loading=owner?.RunResultsLoading==true||owner?.LifecycleLoading==true;
            var lifecycle=owner?.Lifecycle;
            var formation=!loading&&lifecycle?.RunId==runId?lifecycle.StartFormation:null;
            OperatorIds=formation?.Status==RunStartFormationStatus.Available?formation.OperatorIds:Array.AsReadOnly(Array.Empty<string>());
            Names=OperatorIds.Select(id=>OperatorVisualCatalog.Ids.Contains(id)?OperatorVisualCatalog.KoreanLabel(id):"이름 확인 필요").ToList().AsReadOnly();
            FormationNotice=loading?"출발 편성 기록 확인 중":formation?.Status==RunStartFormationStatus.Malformed?"출발 편성 기록 형식 확인 필요":formation?.Status==RunStartFormationStatus.Unsupported?"출발 편성 기록 버전 확인 필요":"해당 전투의 편성 기록 확인 필요";
            HistoryNotice=HasFormation?"해당 전투의 출발 시 편성 기록입니다.\n웨이브 · 전투 시간 기록 확인 필요":"웨이브 · 전투 시간 기록 확인 필요\n"+FormationNotice;
        }
    }
}
