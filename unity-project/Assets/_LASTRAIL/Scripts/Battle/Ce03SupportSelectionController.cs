using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle
{
    public sealed class Ce03SupportChoice
    {
        public string Id {get;}
        public bool Available {get;}
        public Ce03SupportChoice(string id,bool available){Id=id;Available=available;}
    }
    public sealed class Ce03SupportSelectionState
    {
        public IReadOnlyList<Ce03SupportChoice> Entries {get;}
        public IReadOnlyList<string> Selected {get;}
        public string Focused {get;}
        public string Notice {get;}
        public bool CanEdit {get;}
        public bool CanSave {get;}
        public bool Pending {get;}
        internal Ce03SupportSelectionState(Ce03SupportSelectionModel model,string notice,bool busy,Ce03LocalClient client)
        {
            Entries=model.Entries;Selected=Array.AsReadOnly(model.Selected.ToArray());Focused=model.Focused;Notice=notice;Pending=client.HasPending;
            CanEdit=!busy&&!Pending;CanSave=!busy&&(!Pending||client.PendingKind=="support")&&(Pending||Selected.Count==3);
        }
    }
    // Owns ordered draft selection and validates external catalog/selection before replacing it.
    sealed class Ce03SupportSelectionModel
    {
        readonly List<string> selected=new List<string>();
        public IReadOnlyList<string> Selected=>selected.AsReadOnly();
        public IReadOnlyList<Ce03SupportChoice> Entries {get;private set;}=Array.Empty<Ce03SupportChoice>();
        public string Focused {get;private set;}
        public long Revision {get;private set;}
        public void Read(JObject data)
        {
            var catalog=data["catalog"] as JObject;Ce03CommandContract.Equal(catalog,"scope","LOCAL_SELECTION_ONLY");
            var raw=catalog["entries"] as JArray;
            if(raw==null||raw.Count!=10)throw new FormatException("Invalid support catalog.");
            var entries=raw.Select(e=>e["skillId"]?.Type==JTokenType.String&&e["executionAvailable"]?.Type==JTokenType.Boolean?new Ce03SupportChoice((string)e["skillId"],(bool)e["executionAvailable"]):throw new FormatException("Invalid support entry.")).ToArray();
            if(entries.Any(e=>string.IsNullOrWhiteSpace(e.Id))||entries.Select(e=>e.Id).Distinct().Count()!=10)throw new FormatException("Invalid support roles.");
            Apply(data["selection"] as JObject,entries);Entries=Array.AsReadOnly(entries);
            if(!Entries.Any(e=>e.Id==Focused))Focused=null;
        }
        public void Apply(JObject data)=>Apply(data,Entries);
        void Apply(JObject data,IReadOnlyList<Ce03SupportChoice> entries)
        {
            if(data?["revision"]?.Type!=JTokenType.Integer||data["slots"] is not JArray slots)throw new FormatException("Invalid support selection.");
            long revision=(long)data["revision"];var ids=slots.Select(s=>s["skillId"]?.Type==JTokenType.String?(string)s["skillId"]:null).ToArray();
            if(revision<0||revision>=9007199254740991L||ids.Length>3||ids.Distinct().Count()!=ids.Length||ids.Any(id=>!entries.Any(e=>e.Id==id)))throw new FormatException("Invalid support selection.");
            Revision=revision;selected.Clear();selected.AddRange(ids);
        }
        public void Toggle(string id){if(!Entries.Any(e=>e.Id==id))return;Focused=id;if(!selected.Remove(id)&&selected.Count<3)selected.Add(id);}
        public void Remove(int index){if(index>=0&&index<selected.Count)selected.RemoveAt(index);}
        public void MoveLeft(int index){if(index<=0||index>=selected.Count)return;var id=selected[index];selected[index]=selected[index-1];selected[index-1]=id;}
    }
    public interface ICe03SupportSelectionView
    {
        event Action<string> ToggleRequested;
        event Action<int> RemoveRequested,MoveLeftRequested;
        event Action SaveRequested,BackRequested;
        void Render(Ce03SupportSelectionState state);
    }
    public sealed class Ce03SupportSelectionController:IDisposable
    {
        readonly Ce03LocalClient client;readonly ICe03SupportSelectionView view;readonly Action back;
        readonly Ce03SupportSelectionModel model=new Ce03SupportSelectionModel();
        Task<JObject> saveOperation;bool active,disposed,busy;int generation;string notice="불러오는 중";
        public Ce03SupportSelectionController(Ce03LocalClient client,ICe03SupportSelectionView view,Action back)
        {this.client=client??throw new ArgumentNullException(nameof(client));this.view=view??throw new ArgumentNullException(nameof(view));this.back=back;}
        bool Current(int stamp)=>active&&!disposed&&stamp==generation;
        public async Task ActivateAsync()
        {
            if(disposed||active)return;active=true;int stamp=++generation;
            view.ToggleRequested+=Toggle;view.RemoveRequested+=Remove;view.MoveLeftRequested+=MoveLeft;view.SaveRequested+=Save;view.BackRequested+=Back;
            busy=true;Render();
            try {
                if(saveOperation!=null){try{await saveOperation;}catch{ /* The retained client request remains retryable after an unresolved save. */ }if(!Current(stamp))return;}
                var data=await client.ReadSupportAsync();if(!Current(stamp))return;model.Read(data);
                notice=model.Entries.Any(e=>model.Selected.Contains(e.Id)&&!e.Available)?"준비 중인 스킬은 저장할 수 있지만 아직 출격에 사용할 수 없습니다.":"선택한 순서대로 전투 화면에 배치됩니다.";
            }catch{if(Current(stamp))notice="스킬 정보를 불러오지 못했습니다.";}
            finally{if(Current(stamp)){busy=false;Render();}}
        }
        public void Deactivate()
        {
            if(!active)return;active=false;generation++;busy=false;
            view.ToggleRequested-=Toggle;view.RemoveRequested-=Remove;view.MoveLeftRequested-=MoveLeft;view.SaveRequested-=Save;view.BackRequested-=Back;
        }
        bool CanEdit=>active&&!disposed&&!busy&&!client.HasPending;
        void Toggle(string id){if(CanEdit){model.Toggle(id);Render();}}
        void Remove(int index){if(CanEdit){model.Remove(index);Render();}}
        void MoveLeft(int index){if(CanEdit){model.MoveLeft(index);Render();}}
        void Back(){if(active&&!disposed&&!busy)back?.Invoke();}
        async void Save(){await SaveAsync();}
        public async Task SaveAsync()
        {
            if(!active||disposed||busy||(client.HasPending?client.PendingKind!="support":model.Selected.Count!=3))return;
            int stamp=generation;busy=true;Render();
            try{saveOperation=client.HasPending?client.RetryAsync():client.SaveSupportAsync(model.Revision,model.Selected.ToArray());var result=await saveOperation;if(!Current(stamp))return;model.Apply(result);notice="저장했습니다.";}
            catch{if(Current(stamp))notice="저장 결과를 확인하지 못했습니다. 같은 요청으로 다시 확인해 주세요.";}
            finally{if(Current(stamp)){busy=false;Render();}}
        }
        void Render(){if(active&&!disposed)view.Render(new Ce03SupportSelectionState(model,notice,busy,client));}
        public void Dispose(){if(disposed)return;Deactivate();disposed=true;}
    }
}
