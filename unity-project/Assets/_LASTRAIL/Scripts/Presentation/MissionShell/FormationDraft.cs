using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell
{
    // Draft is separate from saved presets and active selection. Never overwrite edits on refresh.
    public sealed class FormationDraft
    {
        readonly AccountScreenRuntime state;string[] slots=new string[5];long revision;
        public string PresetId {get;private set;} public string Name {get;set;}="새 편성";
        public string[] Selection=>(string[])slots.Clone();
        public long ExpectedRevision=>revision;
        public bool Dirty {get;private set;}
        bool committing;
        public bool CanCommit => !committing && (CanActivate || CanSave);
        public async Task CommitAndActivateAsync(Func<bool> stillCurrent)
        {
            if (!CanCommit || stillCurrent == null || !stillCurrent()) return;
            committing = true;
            try
            {
                if (!CanActivate) await SaveAsync();
                // A completed save remains valid, but leaving its screen cancels the next command.
                if (stillCurrent() && CanActivate) await ActivateAsync();
            }
            finally { committing = false; }
        }
        public void Rename(string value){if(state.Busy||state.Pending!=null)return;Name=value;Dirty=true;}
        public FormationDraft(AccountScreenRuntime state){this.state=state;Reset();}
        public void Reset(){slots=new string[5];PresetId="preset_"+Guid.NewGuid().ToString("N");Name="새 편성";revision=0;Dirty=false;}
        public bool Select(int slot,string id)
        {
            if(state.Busy||state.Pending!=null||slot<0||slot>4||state.Account==null||!state.Account.Operators.Any(o=>o.Text("operatorId")==id))return false;
            if(slots[slot]==id)return true;var old=Array.IndexOf(slots,id);if(old>=0)slots[old]=slots[slot];slots[slot]=id;Dirty=true;return true;
        }
        public bool Load(AccountStateRow row)
        {
            if(state.Busy||state.Pending!=null)return false;
            if(!TryReadSelection(row,out var selection))return false;
            PresetId=row.Text("id");Name=row.Text("name");revision=row.Number("revision");Dirty=false;slots=selection;return true;
        }
        public static bool TryReadSelection(AccountStateRow row,out string[] selection)
        {
            selection=new string[5];if(row==null)return false;
            try{var r=GameApiJson.ReadObject(row.Text("definitionJson"));if(AccountStateSnapshot.Number(r,"schemaVersion")!=1)return false;selection=FormationWrite.Selection(r["operators"]);return true;}
            catch(Exception error) when(error is FormatException||error is Newtonsoft.Json.JsonException||error is ArgumentException){return false;}
        }
        public bool CanSave=>!state.Busy&&state.Pending==null&&state.AccountStatus==AccountStateReadStatus.Ready&&state.Account?.ActivationSupported==true&&
            !string.IsNullOrWhiteSpace(Name)&&Name.Trim().Length<=80&&slots.All(s=>s!=null&&state.Account.Operators.Any(o=>o.Text("operatorId")==s))&&slots.Distinct().Count()==5;
        public bool CanActivate=>!state.Busy&&state.Pending==null&&state.AccountStatus==AccountStateReadStatus.Ready&&state.Account?.ActivationSupported==true&&revision>0&&!Dirty&&SavedMatches();
        bool SavedMatches()
        {
            var row=state.Account?.Presets.FirstOrDefault(p=>p.Text("id")==PresetId&&p.Number("revision")==revision);if(row==null)return false;
            return TryReadSelection(row,out var selection)&&selection.SequenceEqual(slots);
        }
        public async Task SaveAsync()
        {
            if(!CanSave)return;
            var payload=new JObject{["expectedRevision"]=revision,["name"]=Name.Trim(),["operators"]=FormationWrite.Operators(slots),["responseVersion"]=FormationWrite.CompleteVersion}.ToString();
            await state.WriteFormationAsync("formation-save",PresetId,payload);
            if(state.LastFormationReceipt?.PresetId==PresetId){revision=state.LastFormationReceipt.PresetRevision;Dirty=false;}
            else if(state.Pending==null)revision=state.Account?.Presets.FirstOrDefault(p=>p.Text("id")==PresetId)?.Number("revision")??0;
        }
        public async Task ActivateAsync()
        {if(CanActivate)await state.WriteFormationAsync("formation-activate",PresetId,new JObject{["expectedRevision"]=revision,["expectedFormationRevision"]=state.Account.FormationRevision,["responseVersion"]=FormationWrite.CompleteVersion}.ToString());}
        public async Task RetryAsync()
        {
            var pending=state.Pending;if(pending==null||!pending.Kind.StartsWith("formation-"))return;
            var r=FormationWrite.Validate(pending.Kind,pending.Target,pending.Payload);
            PresetId=pending.Target;revision=AccountStateSnapshot.Number(r,"expectedRevision");
            if(pending.Kind=="formation-save"){Name=GameApiJson.String(r,"name");slots=FormationWrite.Selection(r["operators"]);Dirty=true;}
            await state.RetryPendingAsync();
            if(state.LastFormationReceipt?.PresetId==PresetId){revision=state.LastFormationReceipt.PresetRevision;if(pending.Kind=="formation-save")Dirty=false;}
        }
    }
}
