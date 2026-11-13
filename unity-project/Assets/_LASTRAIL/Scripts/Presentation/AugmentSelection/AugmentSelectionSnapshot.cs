using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.AugmentSelection
{
    // Observation of the internal solo server contract, not a grant issuer or a catalog.
    public sealed class AugmentSelectionSnapshot
    {
        public sealed class Slot
        {
            public int SlotIndex { get; }
            public string CandidateId { get; }
            public int FreeRemaining { get; }
            public int ExtraRemaining { get; }
            internal Slot(JObject value)
            {
                SelectionJson.Keys(value,"slotIndex","candidateId","freeRemaining","extraRemaining");
                SlotIndex=(int)SelectionJson.Number(value,"slotIndex",2);
                CandidateId=SelectionJson.Id(value,"candidateId");
                FreeRemaining=(int)SelectionJson.Number(value,"freeRemaining",3);
                ExtraRemaining=(int)SelectionJson.Number(value,"extraRemaining",1);
                if(ExtraRemaining==0&&FreeRemaining!=0)throw new FormatException("Extra used before free rerolls");
            }
        }
        public string RunId { get; }
        public string EventId { get; }
        public string StageId { get; }
        public string CatalogVersion { get; }
        public long GrantRevision { get; }
        public bool Confirmed { get; }
        public string SelectedCandidateId { get; }
        public IReadOnlyList<Slot> Slots { get; }
        public string Json { get; }
        AugmentSelectionSnapshot(JObject root)
        {
            SelectionJson.Keys(root,"runId","eventId","stageId","catalogVersion","grantRevision","state","slots","selectedCandidateId");
            RunId=SelectionJson.Id(root,"runId");EventId=SelectionJson.Id(root,"eventId");
            StageId=SelectionJson.Id(root,"stageId");CatalogVersion=SelectionJson.Id(root,"catalogVersion");
            GrantRevision=SelectionJson.Number(root,"grantRevision");
            string state=SelectionJson.Id(root,"state");
            if(state!="SELECTING"&&state!="CONFIRMED")throw new FormatException("Selection state");
            Confirmed=state=="CONFIRMED";
            if(!(root["slots"] is JArray array)||(array.Count<1||array.Count>3||!LastRail.Run.CombatUtilityCatalogVersions.Supports(CatalogVersion)&&array.Count!=3))throw new FormatException("Three slots required");
            var slots=array.Select(x=>new Slot(x as JObject??throw new FormatException("Slot object"))).ToArray();
            if(slots.Where((x,i)=>x.SlotIndex!=i).Any()||slots.Select(x=>x.CandidateId).Distinct().Count()!=slots.Length)throw new FormatException("Unique ordered slots");
            SelectedCandidateId=root["selectedCandidateId"].Type==JTokenType.Null?null:SelectionJson.Id(root,"selectedCandidateId");
            if(Confirmed?!slots.Any(x=>x.CandidateId==SelectedCandidateId):SelectedCandidateId!=null)throw new FormatException("Selected candidate");
            Slots=new ReadOnlyCollection<Slot>(slots);Json=root.ToString(Formatting.None);
        }
        public static AugmentSelectionSnapshot Parse(string json)=>new AugmentSelectionSnapshot(SelectionJson.Object(json));
        internal void RequireBinding(AugmentSelectionSnapshot other)
        {
            if(other.RunId!=RunId||other.EventId!=EventId||other.StageId!=StageId||other.CatalogVersion!=CatalogVersion)throw new FormatException("Selection binding changed");
        }
    }
    internal static class SelectionJson
    {
        internal static JObject Object(string json)
        {
            try{return JObject.Parse(json,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});}
            catch(JsonException e){throw new FormatException("Selection JSON",e);}
        }
        internal static void Keys(JObject value,params string[] keys)
        {
            if(value.Count!=keys.Length||value.Properties().Any(x=>!keys.Contains(x.Name)))throw new FormatException("Selection fields");
        }
        internal static string Id(JObject value,string key)
        {
            var token=value[key];if(token?.Type!=JTokenType.String)throw new FormatException(key);
            string id=token.Value<string>();if(string.IsNullOrWhiteSpace(id)||id.Length>200)throw new FormatException(key);return id;
        }
        internal static long Number(JObject value,string key,long max=9007199254740990L)
        {
            if(value[key]?.Type!=JTokenType.Integer)throw new FormatException(key);
            try{long number=value[key].Value<long>();if(number<0||number>max)throw new FormatException(key);return number;}
            catch(OverflowException e){throw new FormatException(key,e);}
        }
    }
}
