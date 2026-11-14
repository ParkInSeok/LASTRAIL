using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LastRail.Presentation.MissionShell
{
    public sealed class FormationPresetDisplay
    {
        public string Id {get;} public string Name {get;} public bool Active {get;} public bool Valid {get;}
        public IReadOnlyList<string> Operators {get;}
        public FormationPresetDisplay(AccountStateRow row,string active)
        {
            Id=row.Text("id");Name=row.Text("name");Active=Id==active;
            Valid=FormationDraft.TryReadSelection(row,out var slots);Operators=Array.AsReadOnly(slots);
        }
    }
    public sealed class FormationScreenState
    {
        public IReadOnlyList<string> Roster {get;} public IReadOnlyList<string> Selection {get;}
        public IReadOnlyList<FormationPresetDisplay> Presets {get;}
        public string SelectedOperator {get;} public int Slot {get;} public string Name {get;} public string Notice {get;}
        public bool Editable {get;} public bool Retry {get;} public bool CanActivate {get;}
        public FormationScreenState(AccountScreenRuntime owner,int slot,string selected)
        {
            string[] initial={"op_reina","op_kyle","op_eve","op_sera","op_nyx"};
            Roster=Array.AsReadOnly(owner.Account?.Operators.Select(o=>o.Text("operatorId")).OrderBy(id=>{int n=Array.IndexOf(initial,id);return n<0?99:n;}).ToArray()??Array.Empty<string>());
            Selection=Array.AsReadOnly(owner.Formation.Selection);
            Presets=Array.AsReadOnly(owner.Account?.Presets.Select(p=>new FormationPresetDisplay(p,owner.Account.ActivePresetId)).ToArray()??Array.Empty<FormationPresetDisplay>());
            Slot=slot;SelectedOperator=selected;Name=owner.Formation.Name;Notice=owner.ActionNotice;
            Editable=owner.Account!=null&&!owner.Busy&&owner.Pending==null;
            Retry=owner.Pending?.Kind.StartsWith("formation-")==true;CanActivate=Retry?!owner.Busy:owner.Formation.CanCommit;
        }
    }
    // Owns screen selection and command flow; the draft retains saved/unsaved formation authority.
    public sealed class FormationScreenController:IDisposable
    {
        readonly AccountScreenRuntime owner;readonly Func<bool> session;Action<FormationScreenState> render;int slot;string selected;bool disposed;
        public FormationScreenController(AccountScreenRuntime owner,Action<FormationScreenState> render)
        {this.owner=owner??throw new ArgumentNullException(nameof(owner));session=owner.CaptureScreenSession();this.render=render;owner.Changed+=Render;}
        bool Current=>!disposed&&session();
        public void Render(){if(Current)render?.Invoke(new FormationScreenState(owner,slot,selected));}
        bool Editable=>Current&&owner.Account!=null&&!owner.Busy&&owner.Pending==null;
        public void SelectOperator(string id){if(!Editable||!owner.Account.Operators.Any(o=>o.Text("operatorId")==id))return;selected=id;Render();}
        public void SelectPosition(int value)
        {if(!Editable||value<0||value>=5)return;slot=value;if(selected==null)selected=owner.Formation.Selection[slot];else if(owner.Formation.Select(slot,selected))selected=null;Render();}
        public void Rename(string value){if(!Editable)return;owner.Formation.Rename(value);Render();}
        public void NewPreset(){if(!Editable)return;owner.Formation.Reset();slot=0;selected=null;Render();}
        public void LoadPreset(string id){if(!Editable)return;var row=owner.Account.Presets.FirstOrDefault(p=>p.Text("id")==id);if(row!=null&&owner.Formation.Load(row)){slot=0;selected=null;Render();}}
        public async Task ActivateAsync()
        {
            if(!Current||owner.Busy)return;
            if(owner.Pending?.Kind.StartsWith("formation-")==true)await owner.Formation.RetryAsync();
            else await owner.Formation.CommitAndActivateAsync(()=>Current);
            Render();
        }
        public void Dispose(){if(disposed)return;disposed=true;owner.Changed-=Render;render=null;}
    }
}
