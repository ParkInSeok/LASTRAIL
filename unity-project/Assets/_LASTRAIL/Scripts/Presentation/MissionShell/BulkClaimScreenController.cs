using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.MissionShell
{
    public sealed class BulkClaimRowDisplay
    {
        public string Id {get;}
        public string Text {get;}
        public BulkClaimRowDisplay(string id,string text){Id=id;Text=text;}
    }
    public sealed class BulkClaimScreenState
    {
        public bool Results {get;}
        public bool PassResults {get;}
        public int Page {get;}
        public int Pages {get;}
        public IReadOnlyList<BulkClaimRowDisplay> Rows {get;}
        public string Summary {get;}
        public string EmptyText=>Results?"아직 확인된 항목이 없습니다.\n이어받기로 원래 요청을 확인하세요.":"지금 수령할 수 있는 보상이 없습니다.";
        public string SelectAllText {get;}
        public string SubmitText {get;}
        public bool Editable {get;}
        public bool CanSelectAll {get;}
        public bool CanSubmit {get;}
        public bool CanRead {get;}
        public bool CanContinue {get;}
        public bool CanContinuePass {get;}
        public string ContinueText {get;}
        internal BulkClaimScreenState(AccountScreenRuntime account,BulkClaimSelection selection,ICollection<string> selected,bool results,bool passResults,int page)
        {
            Results=results;PassResults=passResults;
            var result=account.LastBulkResult;var source=passResults?account.PassClaimAllItems??new JArray():results?result?.Items??new JArray():selection.Rows;
            Pages=Math.Max(1,(source.Count+4)/5);Page=Math.Max(0,Math.Min(page,Pages-1));
            Summary=passResults?account.PassClaimAllNotice??"":results?(string.IsNullOrEmpty(account.BulkNotice)?"요청 결과를 확인해 주세요.":account.BulkNotice):"받을 보상을 선택하세요.\n선택 "+selected.Count+" / "+source.Count;
            var rows=new List<BulkClaimRowDisplay>();
            for(int index=Page*5;index<Math.Min(source.Count,Page*5+5);index++){
                var row=(JObject)source[index];
                if(!results){var id=(string)row["id"];rows.Add(new BulkClaimRowDisplay(id,(selected.Contains(id)?"✓  ":"□  ")+(string)row["label"]));continue;}
                var action=(JObject)row["action"];var kind=(string)action["kind"];var status=(string)row["status"];
                var caption=kind=="PASS_LEVEL"?"Lv."+(int)action["level"]+" · "+((string)action["track"]=="FREE"?"무료":"프리미엄"):(kind=="MAIL"?"우편 보상 ":"임무 보상 ")+(index+1);
                var text=status=="SUCCESS"?"수령 완료":status=="REPLAYED"?"기존 수령 확인":status=="FAILED"?"실패 · "+Error((string)row["errorCode"]):"미확정 · 이어받기 필요";
                if(row["receipt"] is JObject receipt)text+="\n"+RecurringContract.RewardText((JArray)receipt["rewardLines"]);
                rows.Add(new BulkClaimRowDisplay(null,caption+"\n"+text));
            }
            Rows=rows.AsReadOnly();Editable=!account.Busy;SelectAllText=selected.Count==source.Count&&source.Count>0?"선택 해제":"모두 선택";SubmitText=selected.Count+"개 수령";
            CanSelectAll=source.Count>0&&Editable;CanSubmit=selected.Count>0&&Editable;CanContinuePass=account.CanContinuePassClaimAll;
            CanRead=result!=null&&Editable&&!account.BulkReading;CanContinue=account.Pending?.Kind=="recurring-bulk"&&Editable&&!account.BulkReading;
            ContinueText=account.Pending?.Kind=="recurring-bulk"?"이어받기":"처리 완료";
        }
        static string Error(string code){if(code==null)return "조건 확인 필요";if(code.Contains("expired"))return "수령 기한 만료";if(code.Contains("premium"))return "프리미엄 보유 필요";if(code.Contains("earned")||code.Contains("level"))return "진행 조건 미달";if(code.Contains("not_found"))return "보상 확인 불가";return "수령 조건 미충족";}
    }
    public sealed class BulkClaimScreenController:IDisposable
    {
        readonly AccountScreenRuntime account;readonly BulkClaimSelection selection;readonly bool results,passResults;readonly Func<bool> current,sessionCurrent;
        readonly List<string> selected=new List<string>();Action<BulkClaimScreenState> render;Action close,showResults;int page;bool disposed;
        bool Current=>!disposed&&current()&&sessionCurrent();
        public BulkClaimScreenController(AccountScreenRuntime account,BulkClaimSelection selection,bool results,bool passResults,Action<BulkClaimScreenState> render,Action close,Action showResults,Func<bool> current)
        {this.account=account??throw new ArgumentNullException(nameof(account));this.selection=selection;this.results=results;this.passResults=passResults;this.render=render;this.close=close;this.showResults=showResults;this.current=current;sessionCurrent=account.CaptureScreenSession();}
        public void Render(){if(!Current)return;var state=new BulkClaimScreenState(account,selection,selected,results,passResults,page);page=state.Page;render(state);}
        public void Changed(){if(!Current)return;if(!results&&!account.IsBulkSelectionCurrent(selection)||results&&!passResults&&account.LastBulkResult==null&&account.Pending?.Kind!="recurring-bulk"&&string.IsNullOrEmpty(account.BulkNotice)){close();return;}Render();}
        public void Toggle(string id){if(!Current||results||account.Busy||!account.IsBulkSelectionCurrent(selection)||!selection.Rows.Any(r=>(string)r["id"]==id))return;if(selected.Contains(id))selected.Remove(id);else if(selected.Count<50)selected.Add(id);Render();}
        public void ToggleAll(){if(!Current||results||account.Busy||!account.IsBulkSelectionCurrent(selection))return;var rows=selection.Rows;if(selected.Count==rows.Count)selected.Clear();else{selected.Clear();selected.AddRange(rows.Take(50).Select(r=>(string)r["id"]));}Render();}
        public void MovePage(int delta){if(!Current)return;var state=new BulkClaimScreenState(account,selection,selected,results,passResults,page);page=(state.Page+delta%state.Pages+state.Pages)%state.Pages;Render();}
        public async Task SubmitAsync(){if(!Current||results)return;var command=account.PrepareBulkCommand(selection,selected);if(command==null)return;showResults();await account.SubmitBulkAsync(command);}
        public Task ReadAsync()=>Current&&results&&!passResults&&!account.Busy&&!account.BulkReading?account.RefreshBulkResultAsync():Task.CompletedTask;
        public Task ContinueAsync()=>Current&&results&&!passResults&&!account.Busy&&!account.BulkReading?account.ContinueBulkAsync():Task.CompletedTask;
        public Task ContinuePassAsync()=>Current&&results&&passResults&&account.CanContinuePassClaimAll?account.ContinuePassClaimAllAsync():Task.CompletedTask;
        public void Dispose(){if(disposed)return;disposed=true;selected.Clear();render=null;close=null;showResults=null;}
    }
}
