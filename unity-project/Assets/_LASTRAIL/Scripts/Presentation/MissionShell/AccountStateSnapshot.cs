using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.MissionShell
{
    // Immutable account projection; financial category and whole response versions
    // are distinct. No inferred squad, equipment positions or crystal balance.
    public sealed class AccountStateSnapshot
    {
        public string AccountId {get;private set;}
        public DateTimeOffset ServerTime {get;private set;}
        public long MutationSequence {get;private set;}
        // A category snapshot must not promote the version of unrelated collections.
        public long FinancialMutationSequence {get;private set;}
        public IReadOnlyDictionary<string,long> Currencies {get;private set;}
        public IReadOnlyList<AccountStateRow> Items {get;private set;}
        public AccountStateRow Fuel {get;private set;}
        public IReadOnlyList<AccountStateRow> Operators {get;private set;}
        public IReadOnlyList<AccountStateRow> TrainingCoreSlots {get;private set;}
        public AccountStateRow Train {get;private set;}
        public IReadOnlyList<AccountStateRow> Carriages {get;private set;}
        public IReadOnlyList<AccountStateRow> Turrets {get;private set;}
        public IReadOnlyList<AccountStateRow> Presets {get;private set;}
        public IReadOnlyList<AccountStateRow> Unlocks {get;private set;}
        public string ActivePresetId {get;private set;}
        public long? ActivePresetRevision {get;private set;}
        public long FormationRevision {get;private set;}
        public IReadOnlyList<AccountStateRow> ActiveOperators {get;private set;}
        public DateTimeOffset? FormationUpdatedAt {get;private set;}
        public bool ActivationSupported {get;private set;}
        static readonly string[] DisplayCurrencyDefaults={"CREDITS","MACHINE_PARTS","TACTICAL_DATA","SPECIAL_CORE","OPERATOR_EXP"};
        private AccountStateSnapshot(){}

        internal bool TryApplyRunClaimFinancial(RunCompleteClaimReceipt receipt,out AccountStateSnapshot next)
        {
            next=this;
            var response=receipt.Capture();if((string)response["accountId"]!=AccountId)return false;
            var owner=(JObject)response["owners"]["account"];long version=Number(owner,"mutationSequence");
            if(version<FinancialMutationSequence)return true;
            var amounts=new Dictionary<string,long>();foreach(JObject row in (JArray)owner["currencies"])amounts.Add((string)row["currency"],Number(row,"balance"));
            var items=Rows(owner,"items","itemId","itemId","quantity","");
            if(version==FinancialMutationSequence){
                // The fuel timer can advance without changing this financial version.
                // account/state projects these known absent DB rows as zero;
                // claim SNAPSHOT preserves the sparse DB rows. Unknown omissions
                // and omitted nonzero balances remain conflicts, not inferred zero.
                bool Matches(System.Collections.Generic.IReadOnlyDictionary<string,long> left,System.Collections.Generic.IReadOnlyDictionary<string,long> right)
                    =>left.All(p=>right.TryGetValue(p.Key,out var n)?n==p.Value:p.Value==0&&DisplayCurrencyDefaults.Contains(p.Key));
                return Matches(amounts,Currencies)&&Matches(Currencies,amounts)&&
                    items.Count==Items.Count&&items.All(p=>Items.Any(v=>v.Text("itemId")==p.Text("itemId")&&v.Number("quantity")==p.Number("quantity")));
            }
            next=(AccountStateSnapshot)MemberwiseClone();next.FinancialMutationSequence=version;
            next.Currencies=new ReadOnlyDictionary<string,long>(amounts);next.Items=items;
            if(owner["fuel"] is JObject fuel)next.Fuel=Row(fuel,"","current max","lastRegenAt updatedAt");
            return true;
        }

        internal AccountStateSnapshot PreserveNewerFinancial(AccountStateSnapshot previous)
        {
            if(previous==null||previous.AccountId!=AccountId)return this;
            if(MutationSequence<previous.MutationSequence)return previous;
            if(FinancialMutationSequence>=previous.FinancialMutationSequence)return this;
            var next=(AccountStateSnapshot)MemberwiseClone();next.FinancialMutationSequence=previous.FinancialMutationSequence;
            next.Currencies=previous.Currencies;next.Items=previous.Items;next.Fuel=previous.Fuel;return next;
        }

        // A save replaces exactly one preset. Independent immutable owner data is shared.
        internal bool TryApplyFormationSave(FormationReceipt receipt,string requestJson,out AccountStateSnapshot next)
        {
            next=this;
            try{
                var patch=receipt.AccountPatch;
                if(receipt.Kind!="SAVE"||patch==null||GameApiJson.String(patch,"kind")!="PATCH"||
                   !(patch["coverage"] is JArray coverage)||coverage.Count!=1||(string)coverage[0]!="FORMATION_PRESETS")return false;
                long before=Number(patch,"beforeMutationSequence"),after=Number(patch,"mutationSequence");
                if(after!=before+1)return false;
                if(FinancialMutationSequence>MutationSequence)return false; // Partial state cannot prove whole-account patch continuity.
                var preset=GameApiJson.Object(patch,"preset");var request=GameApiJson.ReadObject(requestJson);
                var row=Row(preset,"id name definitionJson","revision","updatedAt");
                if(row.Text("id")!=receipt.PresetId||row.Number("revision")!=receipt.PresetRevision||
                   row.Text("name")!=GameApiJson.String(request,"name")||
                   !FormationWrite.Selection(preset["operators"]).SequenceEqual(FormationWrite.Selection(request["operators"])))return false;
                var definition=GameApiJson.ReadObject(row.Text("definitionJson"));
                if(Number(definition,"schemaVersion")!=1||!FormationWrite.Selection(definition["operators"]).SequenceEqual(FormationWrite.Selection(request["operators"])))return false;
                if(MutationSequence>after)return true; // Receipt is historical; retain the newer owner.
                if(MutationSequence==after){
                    var existing=Presets.FirstOrDefault(p=>p.Text("id")==receipt.PresetId);
                    return existing!=null&&existing.Number("revision")==row.Number("revision")&&existing.Text("name")==row.Text("name")&&
                        JToken.DeepEquals(GameApiJson.ReadObject(existing.Text("definitionJson")),definition);
                }
                if(MutationSequence!=before)return false;
                var rows=Presets.ToList();int index=rows.FindIndex(p=>p.Text("id")==receipt.PresetId);
                if(index>=0)rows[index]=row;else rows.Add(row);
                next=(AccountStateSnapshot)MemberwiseClone();next.MutationSequence=after;next.FinancialMutationSequence=after;next.Presets=rows.AsReadOnly();return true;
            }catch(FormatException){return false;}catch(InvalidOperationException){return false;}
        }

        internal bool TryApplyFormationActivation(FormationReceipt receipt,string requestJson,out AccountStateSnapshot next)
        {
            next=this;
            try{
                var patch=receipt.AccountPatch;if(receipt.Kind!="ACTIVATE"||patch==null||GameApiJson.String(patch,"kind")!="PATCH"||!(patch["coverage"] is JArray coverage)||coverage.Count!=1||(string)coverage[0]!="ACTIVE_FORMATION")return false;
                long before=Number(patch,"beforeMutationSequence"),after=Number(patch,"mutationSequence");if(after!=before+1||FinancialMutationSequence>MutationSequence)return false;
                var formation=GameApiJson.Object(patch,"formation");var request=GameApiJson.ReadObject(requestJson);var selection=FormationWrite.Selection(formation["operators"]);long revision=Number(formation,"revision");
                if(GameApiJson.String(formation,"activePresetId")!=receipt.PresetId||Number(formation,"activePresetRevision")!=receipt.PresetRevision||Number(request,"expectedRevision")!=receipt.PresetRevision)return false;
                if(MutationSequence>after)return true;
                if(MutationSequence==after)return ActivePresetId==receipt.PresetId&&ActivePresetRevision==receipt.PresetRevision&&FormationRevision==revision&&ActiveOperators.OrderBy(r=>r.Number("slot")).Select(r=>r.Text("operatorId")).SequenceEqual(selection);
                if(MutationSequence!=before||FormationRevision!=Number(request,"expectedFormationRevision"))return false;
                var preset=Presets.FirstOrDefault(p=>p.Text("id")==receipt.PresetId&&p.Number("revision")==receipt.PresetRevision);
                if(preset==null||!FormationWrite.Selection(GameApiJson.ReadObject(preset.Text("definitionJson"))["operators"]).SequenceEqual(selection))return false;
                bool unchanged=ActivePresetId==receipt.PresetId&&ActivePresetRevision==receipt.PresetRevision;
                if(revision!=FormationRevision+(unchanged?0:1)||receipt.InvalidatesSelection==unchanged)return false;
                next=(AccountStateSnapshot)MemberwiseClone();next.MutationSequence=after;next.FinancialMutationSequence=after;next.ActivePresetId=receipt.PresetId;next.ActivePresetRevision=receipt.PresetRevision;next.FormationRevision=revision;next.ActiveOperators=Rows(formation,"operators","slot","operatorId","slot","");
                // The activation patch supplies no timestamp; never attach the old timestamp to a new revision.
                if(!unchanged)next.FormationUpdatedAt=null;
                return true;
            }catch(FormatException){return false;}catch(InvalidOperationException){return false;}
        }

        public static AccountStateSnapshot Parse(string json,string expectedAccount)
        {
            var r=GameApiJson.ReadObject(json);
            if(!GameApiJson.Boolean(r,"accepted")||GameApiJson.String(r,"accountId")!=expectedAccount)throw new FormatException("Account mismatch.");
            var currencies=GameApiJson.Object(r,"currencies");var amounts=new Dictionary<string,long>();
            foreach(var key in DisplayCurrencyDefaults)amounts.Add(key,Number(currencies,key));
            foreach(var property in currencies.Properties())if(!amounts.ContainsKey(property.Name))amounts.Add(property.Name,Number(currencies,property.Name));
            var formation=GameApiJson.Object(r,"formation");
            var supported=GameApiJson.Boolean(formation,"activationSupported");
            if(formation["activePresetId"]==null)throw new FormatException();
            var active=GameApiJson.String(formation,"activePresetId",true);
            if(!supported&&active!=null)throw new FormatException();
            var snapshot=new AccountStateSnapshot{
                AccountId=expectedAccount,ServerTime=Timestamp(r,"serverTime"),MutationSequence=Number(r,"mutationSequence"),FinancialMutationSequence=Number(r,"mutationSequence"),
                ActivationSupported=supported,ActivePresetId=active,FormationRevision=supported?Number(formation,"revision"):0,
                ActiveOperators=supported?Rows(formation,"operators","slot","operatorId","slot",""):Array.Empty<AccountStateRow>(),
                Currencies=new ReadOnlyDictionary<string,long>(amounts),
                Items=Rows(r,"items","itemId", "itemId", "quantity", ""),
                Operators=Rows(r,"operators","operatorId","operatorId","skillTraining promotion revision","acquiredAt"),
                TrainingCoreSlots=Rows(r,"trainingCoreSlots","slot","operatorId","slot trainedLevel investedOperatorExp revision",""),
                Carriages=Rows(r,"carriages","carriageId","carriageId","level revision",""),
                Turrets=Rows(r,"turrets","turretId","turretId","level revision",""),
                Presets=Rows(formation,"presets","id","id name definitionJson","revision","updatedAt"),
                Unlocks=Rows(r,"unlocks","unlockId","unlockId","","unlockedAt"),
                Fuel=OptionalRow(r,"fuel","","current max","lastRegenAt updatedAt"),
                Train=OptionalRow(r,"train","trainId","level revision","updatedAt")
            };
            if(supported){
                if(formation["activePresetRevision"]==null||formation["updatedAt"]==null)throw new FormatException();
                snapshot.ActivePresetRevision=formation["activePresetRevision"].Type==JTokenType.Null?(long?)null:Number(formation,"activePresetRevision");
                snapshot.FormationUpdatedAt=formation["updatedAt"].Type==JTokenType.Null?(DateTimeOffset?)null:Timestamp(formation,"updatedAt");
                if(active==null&&(snapshot.ActivePresetRevision!=null||snapshot.ActiveOperators.Count!=0||snapshot.FormationUpdatedAt!=null))throw new FormatException();
                if(active!=null&&(snapshot.ActivePresetRevision==null||snapshot.ActiveOperators.Count!=5||snapshot.FormationUpdatedAt==null))throw new FormatException();
                if(snapshot.ActiveOperators.Any(s=>s.Number("slot")>4)||snapshot.ActiveOperators.Select(s=>s.Text("operatorId")).Distinct().Count()!=snapshot.ActiveOperators.Count)throw new FormatException();
            }
            if(snapshot.TrainingCoreSlots.Any(s=>s.Number("slot")>4))throw new FormatException("Invalid training slot.");
            return snapshot;
        }
        static string[] Fields(string text)=>text.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);
        static AccountStateRow Row(JObject r,string strings,string numbers,string timestamps)
        {
            var texts=Fields(strings).ToDictionary(k=>k,k=>GameApiJson.String(r,k));
            var values=Fields(numbers).ToDictionary(k=>k,k=>Number(r,k));
            var dates=Fields(timestamps).ToDictionary(k=>k,k=>Timestamp(r,k));
            return new AccountStateRow(texts,values,dates);
        }
        static IReadOnlyList<AccountStateRow> Rows(JObject r,string field,string key,string strings,string numbers,string timestamps)
        {
            if(!(r[field] is JArray rows))throw new FormatException("Missing collection.");
            var ids=new HashSet<string>();var parsed=new List<AccountStateRow>();
            foreach(var item in rows){if(!(item is JObject row))throw new FormatException();var value=Row(row,strings,numbers,timestamps);
                var id=key=="slot"?value.Number(key).ToString(CultureInfo.InvariantCulture):value.Text(key);
                if(!ids.Add(id))throw new FormatException("Duplicate collection key.");parsed.Add(value);}
            return parsed.AsReadOnly();
        }
        static AccountStateRow OptionalRow(JObject r,string field,string strings,string numbers,string timestamps)
        {if(r[field]==null)throw new FormatException("Missing nullable field.");return r[field].Type==JTokenType.Null?null:Row(GameApiJson.Object(r,field),strings,numbers,timestamps);}
        internal static long Number(JObject r,string field)
        {if(r[field]?.Type!=JTokenType.Integer)throw new FormatException("Invalid integer.");var n=(long)r[field];if(n<0||n>9007199254740991L)throw new FormatException("Integer outside wire range.");return n;}
        internal static DateTimeOffset Timestamp(JObject r,string field)
        {
            var text=GameApiJson.String(r,field);
            if(!text.EndsWith("Z",StringComparison.Ordinal)||!DateTimeOffset.TryParseExact(text,new[]{"yyyy-MM-dd'T'HH:mm:ss'Z'","yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'"},CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal|DateTimeStyles.AdjustToUniversal,out var value))throw new FormatException("Invalid UTC timestamp.");
            return value;
        }
    }
    // Only contract fields are retained. Preset definitionJson stays opaque text.
    public sealed class AccountStateRow
    {
        readonly IReadOnlyDictionary<string,string> text;
        readonly IReadOnlyDictionary<string,long> numbers;
        readonly IReadOnlyDictionary<string,DateTimeOffset> times;
        internal AccountStateRow(IDictionary<string,string> text,IDictionary<string,long> numbers,IDictionary<string,DateTimeOffset> times)
        {this.text=new ReadOnlyDictionary<string,string>(text);this.numbers=new ReadOnlyDictionary<string,long>(numbers);this.times=new ReadOnlyDictionary<string,DateTimeOffset>(times);}
        public string Text(string field)=>text[field];
        public long Number(string field)=>numbers[field];
        public DateTimeOffset Time(string field)=>times[field];
    }
    public enum AccountStateReadStatus { NotLoaded,Loading,Ready,Unavailable,ApiUnavailable,VersionMismatch,AuthRequired,ContractMismatch }
}
