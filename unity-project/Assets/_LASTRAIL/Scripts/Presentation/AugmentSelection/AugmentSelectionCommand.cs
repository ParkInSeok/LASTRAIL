using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.AugmentSelection
{
    public sealed class AugmentSelectionCommand
    {
        public string Key { get; }
        public string Operation { get; }
        public string Mode { get; }
        public int SlotIndex { get; }
        public AugmentSelectionSnapshot Before { get; }
        public string BodyJson { get; }
        public string Serialized { get; }
        AugmentSelectionCommand(AugmentSelectionScope scope,string key,string operation,int slot,string mode,AugmentSelectionSnapshot before)
        {
            Key=AugmentSelectionOwner.Check(key);Operation=operation;SlotIndex=slot;Mode=mode;Before=before;
            if(before.RunId!=scope.RunId||before.EventId!=scope.EventId||before.Confirmed||slot<0||slot>=before.Slots.Count)throw new FormatException("Command binding");
            if(operation!="SELECT"&&operation!="REROLL"||operation=="SELECT"&&mode!=null||operation=="REROLL"&&mode!="FREE"&&mode!="EXTRA")throw new FormatException("Command operation");
            var candidate=before.Slots[slot];
            if(operation=="REROLL"&&(mode=="FREE"?candidate.FreeRemaining==0:candidate.FreeRemaining!=0||candidate.ExtraRemaining==0))throw new InvalidOperationException("Reroll unavailable");
            var command=new JObject{{"eventId",before.EventId},{"expectedRevision",before.GrantRevision},{"slotIndex",slot},{"candidateId",candidate.CandidateId}};
            if(mode!=null)command.Add("mode",mode);
            BodyJson=new JObject{{"operation",operation},{"command",command}}.ToString(Formatting.None);
            Serialized=new JObject{{"version","unity-solo-selection-command-v1"},{"scopeKey",scope.StorageKey},{"key",Key},{"body",BodyJson},{"before",JObject.Parse(before.Json)}}.ToString(Formatting.None);
        }
        public static AugmentSelectionCommand Create(AugmentSelectionScope scope,string operation,int slot,string mode,AugmentSelectionSnapshot before)
            =>new AugmentSelectionCommand(scope,Guid.NewGuid().ToString("N"),operation,slot,mode,before);
        public static AugmentSelectionCommand Restore(AugmentSelectionScope scope,string serialized)
        {
            var root=SelectionJson.Object(serialized);SelectionJson.Keys(root,"version","scopeKey","key","body","before");
            if((string)root["version"]!="unity-solo-selection-command-v1"||(string)root["scopeKey"]!=scope.StorageKey||root["body"]?.Type!=JTokenType.String)throw new FormatException("Journal binding");
            var body=SelectionJson.Object((string)root["body"]);SelectionJson.Keys(body,"operation","command");
            var command=body["command"] as JObject??throw new FormatException("Journal command");
            string operation=SelectionJson.Id(body,"operation");
            SelectionJson.Keys(command,operation=="REROLL"?new[]{"eventId","expectedRevision","slotIndex","candidateId","mode"}:new[]{"eventId","expectedRevision","slotIndex","candidateId"});
            var restored=new AugmentSelectionCommand(scope,SelectionJson.Id(root,"key"),operation,(int)SelectionJson.Number(command,"slotIndex",2),operation=="REROLL"?SelectionJson.Id(command,"mode"):null,AugmentSelectionSnapshot.Parse(root["before"].ToString()));
            if(restored.BodyJson!=(string)root["body"]||restored.Serialized!=serialized)throw new FormatException("Journal changed original command");
            return restored;
        }
        internal AugmentSelectionSnapshot ValidateResult(string json,AugmentSelectionScope scope)
        {
            var result=SelectionJson.Object(json);
            bool originalRequired=Operation=="SELECT"&&LastRail.Run.CombatUtilityCatalogVersions.Supports(Before.CatalogVersion)&&Before.Slots[SlotIndex].CandidateId=="O22";
            SelectionJson.Keys(result,originalRequired?new[]{"outcome","snapshot","approval","applicationOriginal"}:new[]{"outcome","snapshot","approval"});
            var next=AugmentSelectionSnapshot.Parse(result["snapshot"].ToString());Before.RequireBinding(next);
            string outcome=SelectionJson.Id(result,"outcome");bool unchanged=outcome=="NO_CANDIDATE";
            if(Operation=="SELECT"?outcome!="SELECTED":outcome!="REROLLED"&&!unchanged)throw new FormatException("Command outcome");
            if(next.GrantRevision!=Before.GrantRevision+(unchanged?0:1)||next.Confirmed!=(Operation=="SELECT"))throw new FormatException("Command transition");
            if(next.Slots.Count!=Before.Slots.Count)throw new FormatException("Receipt slot count changed");
            for(int i=0;i<Before.Slots.Count;i++)
            {
                var a=Before.Slots[i];var b=next.Slots[i];bool rerolled=Operation=="REROLL"&&!unchanged&&i==SlotIndex;
                if(rerolled)
                {
                    foreach(var old in Before.Slots)if(old.CandidateId==b.CandidateId)throw new FormatException("Rerolled candidate was displayed");
                }
                else if(a.CandidateId!=b.CandidateId)throw new FormatException("Other candidate changed");
                if(b.FreeRemaining!=a.FreeRemaining-(rerolled&&Mode=="FREE"?1:0)||b.ExtraRemaining!=a.ExtraRemaining-(rerolled&&Mode=="EXTRA"?1:0))throw new FormatException("Reroll count transition");
            }
            if(Operation=="SELECT"&&next.SelectedCandidateId!=Before.Slots[SlotIndex].CandidateId)throw new FormatException("Selected wrong candidate");
            if(Operation=="REROLL"&&Mode=="EXTRA"&&!unchanged)
            {
                var approval=result["approval"] as JObject??throw new FormatException("Missing extra approval");
                SelectionJson.Keys(approval,"target","approvalId","cause");SelectionJson.Id(approval,"approvalId");
                string cause=SelectionJson.Id(approval,"cause");if(cause!="AD_SSV"&&cause!="PURCHASE_ENTITLEMENT")throw new FormatException("Approval cause");
                var target=approval["target"] as JObject??throw new FormatException("Approval target");
                SelectionJson.Keys(target,"accountId","environment","runId","eventId","stageId","catalogVersion","slotIndex","candidateId","grantRevision","extraOrdinal");
                if(SelectionJson.Id(target,"accountId")!=scope.Owner.AccountId||SelectionJson.Id(target,"environment")!=scope.Owner.Environment||SelectionJson.Id(target,"runId")!=Before.RunId||SelectionJson.Id(target,"eventId")!=Before.EventId||SelectionJson.Id(target,"stageId")!=Before.StageId||SelectionJson.Id(target,"catalogVersion")!=Before.CatalogVersion||SelectionJson.Number(target,"slotIndex",2)!=SlotIndex||SelectionJson.Id(target,"candidateId")!=Before.Slots[SlotIndex].CandidateId||SelectionJson.Number(target,"grantRevision")!=Before.GrantRevision||SelectionJson.Number(target,"extraOrdinal",1)!=1)throw new FormatException("Approval binding");
            }
            else if(result["approval"].Type!=JTokenType.Null)throw new FormatException("Unexpected approval");
            if(originalRequired){var original=result["applicationOriginal"] as JObject??throw new FormatException("Missing O22 acquisition original");
                if((string)original["version"]!=LastRail.Run.CombatO22ApplicationOriginal.Version||original["applied"]?.Type!=JTokenType.Boolean||(bool)original["applied"]||!JToken.DeepEquals(original["definition"]?["selectionSource"]?["selection"],result["snapshot"]))throw new FormatException("O22 original differs from selection receipt");}
            return next;
        }
    }
}
