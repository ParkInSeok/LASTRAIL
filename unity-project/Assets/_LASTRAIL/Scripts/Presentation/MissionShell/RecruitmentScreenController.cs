using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace LastRail.Presentation.MissionShell
{
    public enum RecruitmentScreenMode { Main,History,Rates,Batch }
    public sealed class RecruitmentDisplayRow
    {
        public string Id {get;}
        public string Label {get;}
        public bool Selected {get;}
        public RecruitmentDisplayRow(string id,string label,bool selected=false){Id=id;Label=label;Selected=selected;}
    }
    public sealed class RecruitmentScreenState
    {
        public RecruitmentScreenMode Mode {get;}
        public int Page {get;}
        public int Pages {get;}
        public IReadOnlyList<RecruitmentDisplayRow> Rows {get;}
        public string Notice {get;}
        public string Title {get;}
        public string Policy {get;}
        public string Pity {get;}
        public string TicketBalance {get;}
        public string OnePrice {get;}
        public string TenPrice {get;}
        public bool HasBanner {get;}
        public bool RatesReady {get;}
        public bool HasBatch {get;}
        public bool CanMore {get;}
        public bool CanRecruitOne {get;}
        public bool CanRecruitTen {get;}
        public bool PendingPull {get;}
        public bool CanRefresh {get;}
        internal RecruitmentScreenState(AccountScreenRuntime account,RecruitmentScreenMode mode,int page)
        {
            Mode=mode;Notice=account.RecruitmentNotice;PendingPull=account.Pending?.Kind=="gacha";CanRefresh=!account.Busy;
            var banner=account.SelectedBanner;HasBanner=banner!=null;RatesReady=HasBanner&&account.BannerReady;
            CanRecruitOne=account.CanRecruit(1);CanRecruitTen=account.CanRecruit(10);
            OnePrice=banner?.Price(1).ToString()??"—";TenPrice=banner?.Price(10).ToString()??"—";
            TicketBalance=account.AccountStatus==AccountStateReadStatus.Ready?account.RecruitmentTickets.ToString("N0"):"—";
            var rows=new List<RecruitmentDisplayRow>();int size=mode==RecruitmentScreenMode.Main?5:6;
            if(mode==RecruitmentScreenMode.History){
                Title=account.HistoryReady?(account.History.Count==0?"모집 기록이 없습니다.":"내 모집 기록"):"기록 확인 중 · 이전 정보";
                rows.AddRange(account.History.Select(b=>new RecruitmentDisplayRow(b.Id,b.CreatedAt.Substring(0,16).Replace('T',' ')+" · "+b.Count+"회 · "+(b.Complete?"확인 완료":"확인 필요"))));
                CanMore=account.HistoryCursor!=null&&account.HistoryReady&&!account.HistoryLoading;
            }else if(mode==RecruitmentScreenMode.Batch){
                var batch=account.SelectedBatch;HasBatch=batch!=null;Title=batch==null?"모집 결과 확인 중":batch.Count+"회 모집 · "+(batch.Complete?"확인 완료":"일부 확인 필요");
                if(batch!=null){Policy="정책 버전: "+(batch.Version??"미상 (과거 기록)")+(account.BatchReady?"":" · 이전 정보");for(int i=1;i<=batch.Count;i++)rows.Add(new RecruitmentDisplayRow(null,batch.ResultLine(i)));}
                size=10;
            }else if(mode==RecruitmentScreenMode.Rates){
                if(RatesReady)foreach(var r in banner.Probabilities)rows.Add(new RecruitmentDisplayRow(null,OperatorVisualCatalog.Label((string)r["resultId"])+" · "+(string)r["resultRarity"]+"\n"+((double)r["baseProbability"]*100).ToString("0.####",CultureInfo.InvariantCulture)+"% / "+((double)r["nextPullProbability"]*100).ToString("0.####",CultureInfo.InvariantCulture)+"%"));
            }else{
                Title=banner?.Id??(account.BannersReady&&account.Banners.Count==0?"현재 진행 중인 모집이 없습니다.":"배너를 확인해 주세요.");
                rows.AddRange(account.Banners.Select(b=>new RecruitmentDisplayRow(b.Id,b.Id+" · "+(b.Type=="PICKUP"?"특별 모집":"일반 모집"),b.Id==banner?.Id)));
                CanMore=account.BannerCursor!=null&&account.BannersReady&&!account.BannersLoading;
                if(banner!=null){var p=banner.Pity;var c=banner.Thresholds;Pity="누적 "+p["totalPullCount"]+"회 · 다음 보장 "+GuaranteeLabel(banner.Guarantee)+"\n희귀 "+p["rareOrHigherPityCount"]+" / "+c["rare"]+" · 정예 "+p["elitePityCount"]+" / "+c["elite"]+" · 픽업 "+p["pickupElitePityCount"]+" / "+c["pickup"]+"\n"+(banner.Availability=="ACTIVE"?"진행 중":banner.Availability=="ENDED"?"종료된 모집":"진행 예정")+" · 정책 "+banner.Version;}
            }
            Pages=Math.Max(1,(rows.Count+size-1)/size);Page=((page%Pages)+Pages)%Pages;Rows=rows.Skip(Page*size).Take(size).ToList().AsReadOnly();
        }
        static string GuaranteeLabel(string value)=>value=="ELITE_30"?"정예":value=="PICKUP_ELITE_60"?"픽업 정예":value=="RARE_OR_HIGHER_10"?"희귀 이상":"없음";
    }
    // Owns screen navigation and intent routing; the account runtime retains economy authority.
    public sealed class RecruitmentScreenController:IDisposable
    {
        readonly AccountScreenRuntime account;
        readonly Func<bool> current,sessionCurrent;
        Action<RecruitmentScreenState> render;
        Action<string,string,Func<bool>,Func<Task>> confirm;
        RecruitmentScreenMode mode;int page;long loadGeneration,navigationGeneration;bool disposed;
        bool Current=>!disposed&&current()&&sessionCurrent();
        public RecruitmentScreenController(AccountScreenRuntime account,Action<RecruitmentScreenState> render,Action<string,string,Func<bool>,Func<Task>> confirm,Func<bool> current)
        {
            this.account=account??throw new ArgumentNullException(nameof(account));this.render=render??throw new ArgumentNullException(nameof(render));this.confirm=confirm??throw new ArgumentNullException(nameof(confirm));this.current=current??throw new ArgumentNullException(nameof(current));
            sessionCurrent=account.CaptureScreenSession();account.Changed+=Render;
        }
        public void Render(){if(Current){var state=new RecruitmentScreenState(account,mode,page);page=state.Page;render(state);}}
        public async Task LoadAsync(bool refresh=false)
        {
            if(!Current)return;long request=++loadGeneration;
            await Task.WhenAll(refresh?account.RefreshSharedAsync():account.EnsureSharedAsync(),refresh||!account.BannersReady?account.RefreshBannersAsync():Task.CompletedTask);
            if(!Current||request!=loadGeneration||!account.BannersReady||account.Banners.Count==0)return;
            var selected=account.SelectedBanner;var id=account.Banners.Any(b=>b.Id==selected?.Id)?selected.Id:account.Banners[0].Id;
            if(refresh||!account.BannerReady||selected?.Id!=id)await account.SelectBannerAsync(id);
        }
        public async Task SelectModeAsync(RecruitmentScreenMode value)
        {
            if(!Current||value==RecruitmentScreenMode.Batch||!Enum.IsDefined(typeof(RecruitmentScreenMode),value))return;
            navigationGeneration++;mode=value;page=0;Render();
            if(value==RecruitmentScreenMode.History&&!account.HistoryReady&&!account.HistoryLoading)await account.RefreshHistoryAsync();
        }
        public void MovePage(int delta){if(!Current)return;var state=new RecruitmentScreenState(account,mode,page);page=(state.Page+delta%state.Pages+state.Pages)%state.Pages;Render();}
        public async Task SelectRowAsync(string id)
        {
            if(!Current)return;
            if(mode==RecruitmentScreenMode.History&&account.History.Any(b=>b.Id==id)){mode=RecruitmentScreenMode.Batch;navigationGeneration++;Render();if(account.ViewedBatchId!=id||!account.BatchReady)await account.RefreshBatchAsync(id);}
            else if(mode==RecruitmentScreenMode.Main&&account.Banners.Any(b=>b.Id==id)&&(!account.BannerReady||account.SelectedBanner?.Id!=id))await account.SelectBannerAsync(id);
        }
        public Task MoreAsync()=>!Current?Task.CompletedTask:mode==RecruitmentScreenMode.History?account.RefreshHistoryAsync(true):mode==RecruitmentScreenMode.Main?account.RefreshBannersAsync(true):Task.CompletedTask;
        public Task RefreshHistoryAsync()=>Current?account.RefreshHistoryAsync():Task.CompletedTask;
        public Task RefreshBatchAsync()=>Current&&account.ViewedBatchId!=null?account.RefreshBatchAsync(account.ViewedBatchId):Task.CompletedTask;
        public async Task RefreshAsync()
        {
            if(!Current||account.Busy)return;
            if(account.Pending?.Kind=="gacha")await RetryAsync();else await LoadAsync(true);
        }
        public async Task RetryAsync()
        {
            if(!Current||account.Busy||account.Pending?.Kind!="gacha")return;long navigation=navigationGeneration;
            await account.RetryPendingAsync();ShowResult(navigation);
        }
        public void RequestRecruit(int count)
        {
            if(!Current||!account.CanRecruit(count))return;
            var banner=account.SelectedBanner;long price=banner.Price(count);long navigation=navigationGeneration;
            bool Allowed()=>Current&&navigation==navigationGeneration&&account.SelectedBanner?.Id==banner.Id&&account.SelectedBanner?.Price(count)==price&&account.CanRecruit(count);
            confirm(count+"회 모집","모집권 "+price+"개를 사용합니다.",Allowed,async()=>{if(!Allowed())return;await account.RecruitAsync(count);ShowResult(navigation);});
        }
        void ShowResult(long navigation){if(!Current||navigation!=navigationGeneration)return;if(account.SelectedBatch!=null)mode=RecruitmentScreenMode.Batch;Render();}
        public void Dispose(){if(disposed)return;disposed=true;account.Changed-=Render;render=null;confirm=null;}
    }
}
