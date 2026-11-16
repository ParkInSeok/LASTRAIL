using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.MissionShell
{
    public sealed class MissionRowDisplay
    {
        public string Id {get;} public string Name {get;} public string Progress {get;} public string Rewards {get;} public string ClaimLabel {get;} public string Expiry {get;}
        public bool ShowProgress {get;} public bool Mail {get;} public bool CanClaim {get;}
        internal MissionRowDisplay(AccountScreenRuntime owner,JObject item,bool mail,bool progress)
        {
            Mail=mail;ShowProgress=progress;Id=(string)item[mail?"award_id":progress?"awardId":"id"];
            var award=mail?(JObject)owner.Recurring.Awards.First(a=>(string)a["id"]==Id):item;var status=(string)item["status"];
            Name=RecurringContract.Objective((string)award[progress?"objectiveId":"objective"]);
            Progress=progress?ObjectiveProgress.ProgressText(item):"";Rewards=RecurringContract.RewardText((JArray)award["rewards"]);
            ClaimLabel=progress?ObjectiveProgress.StatusText(item):status=="CLAIMED"?"수령 완료":status=="EXPIRED"?"기한 만료":status=="MAIL_PENDING"?"우편 준비":status=="MAILED"?"우편 발송":"받기";
            CanClaim=Id!=null&&owner.CanClaimRecurring(mail?"recurring-mail":"recurring-quest",Id);
            Expiry=mail?"만료 "+DateTimeOffset.FromUnixTimeMilliseconds((long)item["expires_at"]).ToOffset(TimeSpan.FromHours(9)).ToString("MM/dd HH:mm"):"";
        }
    }
    public sealed class MissionScreenState
    {
        public IReadOnlyList<MissionRowDisplay> Rows {get;} public int Page {get;} public int Pages {get;}
        public string Notice {get;} public string Empty {get;} public bool ShowPages {get;} public string PageLabel=>(Page+1)+" / "+Pages;
        public string BulkLabel {get;} public string RefreshLabel {get;} public bool CanBulk {get;} public bool CanRefresh {get;}
        internal MissionScreenState(AccountScreenRuntime owner,int category,int page)
        {
            var data=owner?.Recurring;Notice=(owner?.RecurringNotice??"정보를 불러올 수 없습니다.")+"\n"+(owner?.ActionNotice??"");
            var system=category<2?data?.Progress?.SystemState("QUESTS"):null;var progressRows=system?["objectives"] as JArray;
            var rows=data==null||category==3?Array.Empty<JObject>():category==2?data.Mail.Cast<JObject>().ToArray():progressRows!=null?progressRows.Cast<JObject>().Where(a=>(string)a["frequency"]==(category==0?"DAILY":"WEEKLY")).ToArray():data.Awards.Cast<JObject>().Where(a=>(string)a["system"]=="QUESTS"&&((string)a["period_id"]).StartsWith(category==0?"D:":"W:")&&(long)a["period_end"]>data.ServerTime).ToArray();
            Pages=Math.Max(1,(rows.Length+3)/4);Page=Math.Max(0,Math.Min(page,Pages-1));ShowPages=data!=null;
            Rows=Array.AsReadOnly(rows.Skip(Page*4).Take(4).Select(item=>new MissionRowDisplay(owner,item,category==2,progressRows!=null)).ToArray());
            Empty=data==null?"임무 정보를 불러오지 못했습니다.":category==2?"도착한 임무 보상 우편이 없습니다.":system!=null?"진행 중인 임무가 없습니다.":data.Policies.Any(p=>(string)p["system"]=="QUESTS"&&(bool)p["active"])?"서버에서 확인된 달성 임무가 없습니다.":"진행 중인 임무가 없습니다.";
            BulkLabel=owner?.Pending?.Kind=="recurring-bulk"?"수령 결과":"선택 수령";CanBulk=owner!=null&&!owner.Busy&&(owner.Pending?.Kind=="recurring-bulk"||owner.Pending==null&&owner.RecurringReady&&!owner.RecurringLoading);
            bool retry=owner?.Pending?.Kind.StartsWith("recurring-")==true;RefreshLabel=retry?"요청 결과 재확인":"진행 갱신";CanRefresh=owner!=null&&!owner.Busy&&!owner.RecurringLoading&&(owner.Pending==null||retry);
        }
    }
    public sealed class MissionScreenController:IDisposable
    {
        readonly AccountScreenRuntime owner;readonly int category;readonly Func<bool> session;Action<MissionScreenState> render;Action<string> bulk;int page;bool disposed;
        public MissionScreenController(AccountScreenRuntime owner,int category,int page,Action<MissionScreenState> render,Action<string> bulk){this.owner=owner;this.category=category;this.page=page;this.render=render;this.bulk=bulk;session=owner?.CaptureScreenSession()??(()=>true);if(owner!=null)owner.Changed+=Render;}
        bool Current=>!disposed&&session();
        public void Render(){if(!Current)return;var state=new MissionScreenState(owner,category,page);page=state.Page;render?.Invoke(state);}
        public void NextPage(){if(!Current)return;var state=new MissionScreenState(owner,category,page);page=(state.Page+1)%state.Pages;Render();}
        public Task ClaimAsync(int cell){if(!Current||owner==null||cell<0)return Task.CompletedTask;var state=new MissionScreenState(owner,category,page);return cell>=state.Rows.Count||!state.Rows[cell].CanClaim?Task.CompletedTask:owner.ClaimRecurringAsync(category==2?"recurring-mail":"recurring-quest",state.Rows[cell].Id);}
        public void OpenBulk(){if(Current&&new MissionScreenState(owner,category,page).CanBulk)bulk?.Invoke(category==2?"MAIL":"QUEST");}
        public Task LoadAsync()=>!Current||owner==null?Task.CompletedTask:owner.SyncRecurringAsync();
        public Task RefreshAsync(){if(!Current||owner==null||!new MissionScreenState(owner,category,page).CanRefresh)return Task.CompletedTask;return owner.Pending?.Kind.StartsWith("recurring-")==true?owner.RetryPendingAsync():owner.SyncRecurringAsync();}
        public void Dispose(){if(disposed)return;disposed=true;if(owner!=null)owner.Changed-=Render;render=null;bulk=null;}
    }
}
