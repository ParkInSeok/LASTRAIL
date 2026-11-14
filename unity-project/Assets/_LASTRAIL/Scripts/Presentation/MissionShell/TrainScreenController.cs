using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LastRail.Presentation.MissionShell
{
    public sealed class TrainRowDisplay
    {
        public string Id {get;} public string Label {get;} public bool Enabled {get;} public bool Selected {get;} public int Index {get;}
        public TrainRowDisplay(string id,string label,bool enabled,bool selected,int index){Id=id;Label=label;Enabled=enabled;Selected=selected;Index=index;}
    }
    public sealed class TrainScreenState
    {
        public bool Available {get;} public int Page {get;} public int Pages {get;} public int Slot {get;} public string PageLabel=>(Page+1)+" / "+Pages;
        public IReadOnlyList<TrainRowDisplay> Rows {get;} public IReadOnlyList<TrainRowDisplay> Slots {get;}
        public string Unavailable {get;} public string Name {get;} public string Level {get;} public string Cost {get;} public string Notice {get;}
        public string LoadoutState {get;} public string SaveLabel {get;} public string UpgradeLabel {get;} public bool CanSave {get;} public bool CanUpgrade {get;} public bool CanClear {get;}
        internal TrainScreenState(AccountScreenRuntime owner,int category,int page,int slot)
        {
            Slot=Math.Max(0,Math.Min(slot,3));Rows=Array.Empty<TrainRowDisplay>();Slots=Array.Empty<TrainRowDisplay>();Pages=1;
            Available=category==1?owner?.TrainLoadout!=null:owner?.Account!=null;Unavailable=category==1?owner?.TrainLoadoutNotice??"장착 정보를 불러오지 못했습니다.":"열차 정보를 불러오지 못했습니다.";
            if(!Available)return;
            if(category==1){
                var snapshot=owner.TrainLoadout;var selected=owner.TrainDraft;var rows=snapshot.Carriages;Pages=Math.Max(1,(rows.Count+5)/6);Page=Math.Max(0,page)%Pages;
                LoadoutState=(owner.TrainDraftDirty?"편집 중":"저장된 배치")+" · 슬롯 "+(snapshot.SlotCount?.ToString()??"확인 필요")+(owner.TrainLoadoutReady?"":" · 이전 정보");
                Slots=Array.AsReadOnly(Enumerable.Range(0,4).Select(i=>{bool unlocked=snapshot.SlotCount.HasValue&&i<snapshot.SlotCount.Value;return new TrainRowDisplay(selected[i],(i+1)+" · "+(selected[i]??(unlocked?"차량 선택":"잠김")),unlocked&&owner.CanEditTrain,i==Slot,i);}).ToArray());
                Rows=Array.AsReadOnly(rows.Skip(Page*6).Take(6).Select((c,i)=>{string id=(string)c["carriageId"];bool eligible=(bool)c["installable"];return new TrainRowDisplay(id,id+" · Lv."+c["level"]+(eligible?"":" · "+AccountScreenRuntime.TrainLoadoutProblem((string)c["reason"])),eligible&&owner.CanEditTrain&&snapshot.SlotCount.HasValue&&Slot<snapshot.SlotCount.Value,selected.Contains(id),Page*6+i);}).ToArray());
                Notice="전투칸 1개 필수 · 열린 슬롯 모두 배치 · 중복 금지\n"+owner.TrainLoadoutNotice+"\n"+owner.ActionNotice;
                bool retry=owner.Pending?.Kind=="train-loadout";SaveLabel=retry?"원래 장착 요청 재확인":"차량 배치 저장";CanSave=retry?!owner.Busy:owner.CanSaveTrainLoadout;CanClear=owner.CanEditTrain;return;
            }
            var account=owner.Account;
            if(category!=0){var rows=category==2?account.Carriages:account.Turrets;string key=category==2?"carriageId":"turretId";Pages=Math.Max(1,(rows.Count+5)/6);Page=Math.Max(0,page)%Pages;Rows=Array.AsReadOnly(rows.Skip(Page*6).Take(6).Select((row,i)=>new TrainRowDisplay(row.Text(key),row.Text(key)+" · Lv."+row.Number("level"),false,false,Page*6+i)).ToArray());return;}
            var train=account.Train;string id=train?.Text("trainId");Name=train==null?"보유 기관차가 없습니다.":id;Level=train==null?"—":"Lv."+train.Number("level");
            Cost=OperatorGrowthConfig.CostLabel(owner.GrowthReady?owner.Growth?.Cost("growth-train",id,account):null);Notice=string.IsNullOrEmpty(owner.ActionNotice)?owner.GrowthNotice:owner.ActionNotice;
            bool growthRetry=owner.Pending?.Kind=="growth-train";UpgradeLabel=growthRetry?"강화 결과 재확인":"기관차 강화";CanUpgrade=growthRetry?!owner.Busy:owner.CanGrow("growth-train",id);
        }
    }
    public sealed class TrainScreenController:IDisposable
    {
        readonly AccountScreenRuntime owner;readonly int category;readonly Func<bool> session;Action<TrainScreenState> render;Action<string,string,Func<bool>,Func<Task>> confirm;int page,slot;bool disposed;
        public TrainScreenController(AccountScreenRuntime owner,int category,int page,int slot,Action<TrainScreenState> render,Action<string,string,Func<bool>,Func<Task>> confirm){this.owner=owner;this.category=category;this.page=page;this.slot=slot;this.render=render;this.confirm=confirm;session=owner?.CaptureScreenSession()??(()=>true);if(owner!=null)owner.Changed+=Render;}
        bool Current=>!disposed&&session();
        public void Render(){if(!Current)return;var state=new TrainScreenState(owner,category,page,slot);page=state.Page;slot=state.Slot;render?.Invoke(state);}
        public void NextPage(){if(!Current)return;var state=new TrainScreenState(owner,category,page,slot);page=(state.Page+1)%state.Pages;Render();}
        public void SelectSlot(int value){if(!Current||owner?.CanEditTrain!=true||value<0||value>=owner.TrainLoadout.SlotCount)return;slot=value;Render();}
        public void Install(string id){if(Current&&owner!=null)owner.SelectTrainCarriage(slot,id);}
        public void Clear(){if(Current)owner?.ClearTrainDraft();}
        public Task SaveAsync()=>!Current||owner==null?Task.CompletedTask:owner.Pending?.Kind=="train-loadout"?owner.RetryPendingAsync():owner.SaveTrainLoadoutAsync();
        public async Task UpgradeAsync(){if(!Current||owner==null)return;if(owner.Pending?.Kind=="growth-train"){await owner.RetryPendingAsync();return;}string id=owner.Account?.Train?.Text("trainId");if(!owner.CanGrow("growth-train",id))return;var state=new TrainScreenState(owner,0,page,slot);confirm?.Invoke("기관차 강화",state.Cost+"\n위 재료를 사용해 1레벨 강화합니다.",()=>Current&&owner.CanGrow("growth-train",id),()=>Current?owner.GrowAsync("growth-train",id):Task.CompletedTask);}
        public Task LoadAsync()=>!Current||owner==null?Task.CompletedTask:Task.WhenAll(owner.EnsureSharedAsync(),owner.EnsureTrainLoadoutAsync());
        public Task RefreshAsync()=>!Current||owner==null?Task.CompletedTask:Task.WhenAll(owner.RefreshSharedAsync(),owner.RefreshTrainLoadoutAsync());
        public void Dispose(){if(disposed)return;disposed=true;if(owner!=null)owner.Changed-=Render;render=null;confirm=null;}
    }
}
