using System.Globalization;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.MissionShell
{
    // Immutable display projection of an already validated server reward line.
    // No balances, grants or claim authority live in this object.
    public sealed class RunVictoryRewardPresentation
    {
        public string Kind { get; }
        public string Id { get; }
        public long Quantity { get; }
        public string Name { get; }
        public string IconId { get; }
        public string QuantityText => Quantity.ToString("N0", CultureInfo.InvariantCulture);
        public string Label => Name + " " + Quantity.ToString(CultureInfo.InvariantCulture);
        public string RenderKey => Kind.Length + ":" + Kind + Id.Length + ":" + Id + ":" + Quantity.ToString(CultureInfo.InvariantCulture);

        public RunVictoryRewardPresentation(JObject line)
        {
            Kind = (string)line["kind"];
            Id = (string)line["id"];
            Quantity = (long)line["quantity"];
            Name = Kind == "CRYSTAL" ? "크리스탈" : Kind == "ITEM" ? "아이템" : "미등록 보상";
            if (Kind != "CURRENCY") return;
            switch (Id)
            {
                case "cur_credits": Name = "크레딧"; break;
                case "cur_machine_parts": Name = "기계 부품"; break;
                case "cur_operator_exp": Name = "대원 경험치"; break;
                case "cur_tactical_data": Name = "전술 자료"; break;
                default: return;
            }
            IconId = Id;
        }
    }
}