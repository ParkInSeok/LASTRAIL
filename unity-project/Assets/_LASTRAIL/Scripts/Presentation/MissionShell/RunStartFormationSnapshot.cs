using System;
using System.Collections.Generic;
using LastRail.Auth;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.MissionShell
{
    public enum RunStartFormationStatus { Missing, Available, Unsupported, Malformed }

    // Optional historical START projection. Never substitutes today's account formation.
    public sealed class RunStartFormationSnapshot
    {
        public RunStartFormationStatus Status { get; }
        public IReadOnlyList<string> OperatorIds { get; }
        RunStartFormationSnapshot(RunStartFormationStatus status,string[] ids=null)
        {Status=status;OperatorIds=Array.AsReadOnly(ids??Array.Empty<string>());}

        public static RunStartFormationSnapshot Read(JObject start)
        {
            var token=start?["admission"];
            if(token==null||token.Type==JTokenType.Null)return new RunStartFormationSnapshot(RunStartFormationStatus.Missing);
            try
            {
                var admission=GameApiJson.Object(start,"admission");var version=GameApiJson.String(admission,"version");
                if(version!="departure-v1"&&version!="departure-v2")return new RunStartFormationSnapshot(RunStartFormationStatus.Unsupported);
                var formation=GameApiJson.Object(admission,"formation");
                if(AccountStateSnapshot.Number(formation,"schemaVersion")!=1)return new RunStartFormationSnapshot(RunStartFormationStatus.Unsupported);
                var ids=FormationWrite.Selection(formation["operators"]);
                foreach(var id in ids)RecruitmentJson.Id(new JObject{["operatorId"]=id},"operatorId");
                return new RunStartFormationSnapshot(RunStartFormationStatus.Available,ids);
            }
            catch(Exception error) when(error is FormatException||error is OverflowException||error is InvalidCastException)
            {
                // A bad optional roster must remain explicit without erasing the valid reward receipt.
                return new RunStartFormationSnapshot(RunStartFormationStatus.Malformed);
            }
        }
    }
}
