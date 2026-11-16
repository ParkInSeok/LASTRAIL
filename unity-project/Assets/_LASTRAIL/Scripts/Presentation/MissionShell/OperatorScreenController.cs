using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LastRail.Presentation.MissionShell
{
    public sealed class OperatorGrowthDisplay
    {
        public string Cost {get;} public string Title {get;} public bool Enabled {get;}
        public OperatorGrowthDisplay(string cost,string title,bool enabled){Cost=cost;Title=title;Enabled=enabled;}
    }
    public sealed class OperatorScreenState
    {
        public IReadOnlyList<string> Operators {get;} public int Selected {get;} public string Id {get;}
        public string Growth {get;} public string Notice {get;} public IReadOnlyList<OperatorGrowthDisplay> Actions {get;}
        public OperatorScreenState(AccountScreenRuntime owner,int selected)
        {
            var account=owner?.Account;var ids=account?.Operators.Select(o=>o.Text("operatorId")).ToArray()??Array.Empty<string>();
            Operators=Array.AsReadOnly(ids);Selected=Math.Max(0,Math.Min(selected,ids.Length-1));Id=ids.Length==0?null:ids[Selected];
            var row=ids.Length==0?null:account.Operators[Selected];var core=account?.TrainingCoreSlots.FirstOrDefault(s=>s.Text("operatorId")==Id);
            Growth=row==null?(account==null?owner?.AccountNotice??"대원 정보를 불러오지 못했습니다.":"보유한 대원이 없습니다."):(core==null?"훈련 코어 미배치":"훈련 Lv."+core.Number("trainedLevel"))+"\n스킬 훈련 "+row.Number("skillTraining")+" · 승급 "+row.Number("promotion");
            Notice=owner==null?"":string.IsNullOrEmpty(owner.ActionNotice)?owner.GrowthNotice:owner.ActionNotice;
            var actions=new OperatorGrowthDisplay[3];string[] titles={"레벨업","스킬 훈련","승급"};
            for(int i=0;i<3;i++){
                var target=i==0?core?.Number("slot").ToString():Id;var kind=OperatorScreenController.GrowthKind(i);
                var cost=owner?.GrowthReady==true&&account!=null?owner.Growth?.Cost(kind,target,account):null;
                bool retry=i==0&&owner?.Pending?.Kind.StartsWith("growth-")==true;
                actions[i]=new OperatorGrowthDisplay(i==0&&core==null?"훈련 코어 미배치":OperatorGrowthConfig.CostLabel(cost),retry?"결과 재확인":titles[i],retry?!owner.Busy:owner?.CanGrow(kind,target)==true);
            }
            Actions=Array.AsReadOnly(actions);
        }
    }
    public sealed class TrainingCoreSlotDisplay
    {
        public string Id {get;} public long Level {get;} public long Invested {get;} public bool CanReplace {get;}
        public TrainingCoreSlotDisplay(AccountStateRow row,bool allowed){Id=row?.Text("operatorId");Level=row?.Number("trainedLevel")??0;Invested=row?.Number("investedOperatorExp")??0;CanReplace=allowed;}
    }
    public sealed class TrainingCoreDisplay
    {
        public string Candidate {get;} public IReadOnlyList<TrainingCoreSlotDisplay> Slots {get;} public bool CanSync {get;}
        public TrainingCoreDisplay(AccountScreenRuntime owner,string id){Candidate=id;Slots=Array.AsReadOnly(Enumerable.Range(0,5).Select(i=>new TrainingCoreSlotDisplay(owner.Account.TrainingCoreSlots.FirstOrDefault(s=>s.Number("slot")==i),owner.CanReplaceCore(i,id))).ToArray());CanSync=owner.CanGrow("growth-sync","all");}
    }
    public sealed class OperatorScreenController:IDisposable
    {
        readonly AccountScreenRuntime owner;readonly Func<bool> session;Action<OperatorScreenState> render;Action<TrainingCoreDisplay> showCore;
        Action<string,string,Func<bool>,Func<Task>> confirm;int selected;string coreCandidate;bool disposed;
        public OperatorScreenController(AccountScreenRuntime owner,Action<OperatorScreenState> render,Action<TrainingCoreDisplay> showCore,Action<string,string,Func<bool>,Func<Task>> confirm)
        {this.owner=owner;session=owner?.CaptureScreenSession()??(()=>true);this.render=render;this.showCore=showCore;this.confirm=confirm;if(owner!=null)owner.Changed+=Render;}
        bool Current=>!disposed&&session();
        internal static string GrowthKind(int index)=>index==0?"growth-core":index==1?"growth-skill":"growth-promotion";
        public void Render(){if(Current)render?.Invoke(new OperatorScreenState(owner,selected));}
        public void Select(int index){if(!Current||index<0||index>=(owner?.Account?.Operators.Count??0))return;selected=index;Render();}
        string SelectedId=>owner?.Account?.Operators.Count>0?owner.Account.Operators[Math.Min(selected,owner.Account.Operators.Count-1)].Text("operatorId"):null;
        public async Task GrowAsync(int index)
        {
            if(!Current||owner==null||index<0||index>2)return;
            if(index==0&&owner.Pending?.Kind.StartsWith("growth-")==true)await owner.RetryPendingAsync();
            else{var id=SelectedId;var target=index==0?owner.Account?.TrainingCoreSlots.FirstOrDefault(s=>s.Text("operatorId")==id)?.Number("slot").ToString():id;await owner.GrowAsync(GrowthKind(index),target);}
            Render();
        }
        public void OpenCore(){if(!Current||owner?.Account==null||SelectedId==null)return;coreCandidate=SelectedId;showCore?.Invoke(new TrainingCoreDisplay(owner,coreCandidate));}
        public void ReplaceCore(int slot)
        {
            if(!Current||owner?.Account==null||slot<0||slot>=5||coreCandidate==null)return;
            var id=coreCandidate;var core=owner.Account.TrainingCoreSlots.FirstOrDefault(s=>s.Number("slot")==slot);var refund=core?.Number("investedOperatorExp")??0;
            confirm?.Invoke("코어 "+(slot+1)+(core==null?" 배치":" 교체"),OperatorVisualCatalog.Label(id)+"을(를) Lv.1로 배치합니다.\n기록된 투자 EXP "+refund.ToString("N0")+" 전액 환급\n자동 재투자는 하지 않습니다.",()=>Current&&owner.CanReplaceCore(slot,id),async()=>{if(Current)await owner.ReplaceCoreAsync(slot,id);});
        }
        public void SyncCore()
        {
            if(!Current||owner?.Account==null)return;var balance=owner.Account.Currencies.TryGetValue("OPERATOR_EXP",out var exp)?exp:0;
            confirm?.Invoke("동기 성장","보유 EXP "+balance.ToString("N0")+" 범위에서\n낮은 레벨부터 반복 성장합니다.\nEXP가 부족하거나 모두 최고 레벨이면 멈춥니다.",()=>Current&&owner.CanGrow("growth-sync","all"),async()=>{if(Current)await owner.GrowAsync("growth-sync","all");});
        }
        public void Dispose(){if(disposed)return;disposed=true;if(owner!=null)owner.Changed-=Render;render=null;showCore=null;confirm=null;coreCandidate=null;}
    }
}
