using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell
{
    public sealed partial class AccountScreenRuntime
    {
        long bannerRequest,detailRequest,historyRequest,batchRequest;
        public IReadOnlyList<RecruitmentBanner> Banners {get;private set;}=new RecruitmentBanner[0];
        public IReadOnlyList<RecruitmentBatch> History {get;private set;}=new RecruitmentBatch[0];
        public RecruitmentBanner SelectedBanner {get;private set;}
        public RecruitmentBatch SelectedBatch {get;private set;}
        public string BannerCursor {get;private set;}public string HistoryCursor {get;private set;}
        public bool BannersReady {get;private set;}public bool BannerReady {get;private set;}public bool HistoryReady {get;private set;}public bool BatchReady {get;private set;}
        public bool BannersLoading {get;private set;}public bool DetailLoading {get;private set;}public bool HistoryLoading {get;private set;}public bool BatchLoading {get;private set;}
        public string RecruitmentNotice {get;private set;}="";
        public string RecoveryBatchId {get;private set;}
        public string ViewedBatchId {get;private set;}
        void ResetRecruitment()
        {
            bannerRequest++;detailRequest++;historyRequest++;batchRequest++;Banners=new RecruitmentBanner[0];History=new RecruitmentBatch[0];SelectedBanner=null;SelectedBatch=null;BannerCursor=null;HistoryCursor=null;
            BannersReady=BannerReady=HistoryReady=BatchReady=BannersLoading=DetailLoading=HistoryLoading=BatchLoading=false;RecruitmentNotice="";RecoveryBatchId=null;ViewedBatchId=null;
        }
        async Task<JObject> ReadRecruitment(string path,GameApiAuthSessionSnapshot original)
        {
            var reply=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("GET",path,null,null),original.AccessToken));
            if(reply.IsNetworkError)throw new InvalidOperationException("network_error");var r=GameApiJson.ReadObject(reply.BodyJson);
            if(reply.StatusCode!=200||!GameApiJson.Boolean(r,"accepted"))throw new InvalidOperationException((string)r["errorCode"]??"read_failed");return r;
        }
        public async Task RefreshBannersAsync(bool more=false)
        {
            SynchronizeSession();if(more&&(BannerCursor==null||BannersLoading||!BannersReady))return;var original=owner;var stamp=ownerGeneration;var request=++bannerRequest;if(!Current(original,stamp))return;
            var cursor=more?BannerCursor:null;BannersReady=false;BannersLoading=true;RecruitmentNotice="모집 정보를 확인하고 있습니다.";Changed?.Invoke();
            try{var r=await ReadRecruitment("/v1/gacha/banners?limit=20"+(cursor==null?"":"&afterBannerId="+RecruitmentJson.PathId(cursor)),original);if(!Current(original,stamp)||request!=bannerRequest)return;
                RecruitmentJson.Utc(r,"serverTime");var rows=RecruitmentJson.Array(r,"banners").Select(t=>new RecruitmentBanner((JObject)t)).ToArray();if(rows.Any(b=>b.Availability!="ACTIVE")||rows.Select(b=>b.Id).Distinct().Count()!=rows.Length)throw new FormatException();
                var next=RecruitmentJson.NullableString(r,"nextAfterBannerId");if(next!=null){RecruitmentJson.PathId(next);if(next==cursor||!rows.Any(b=>b.Id==next))throw new FormatException();}
                Banners=(more?Banners.Concat(rows).GroupBy(b=>b.Id).Select(g=>g.Last()):rows).ToList().AsReadOnly();BannerCursor=next;BannersReady=true;RecruitmentNotice=Banners.Count==0?"현재 진행 중인 모집이 없습니다.":"";
            }catch(Exception e){if(!Current(original,stamp)||request!=bannerRequest)return;RecruitmentNotice="배너 조회 실패 · "+e.Message;}
            finally{if(Current(original,stamp)&&request==bannerRequest){BannersLoading=false;Changed?.Invoke();}}
        }
        public async Task SelectBannerAsync(string id)
        {
            SynchronizeSession();var original=owner;var stamp=ownerGeneration;var request=++detailRequest;if(!Current(original,stamp))return;
            SelectedBanner=null;BannerReady=false;DetailLoading=true;Changed?.Invoke();
            try{var r=await ReadRecruitment("/v1/gacha/banners/"+RecruitmentJson.PathId(id),original);if(!Current(original,stamp)||request!=detailRequest)return;var banner=new RecruitmentBanner(GameApiJson.Object(r,"banner"));RecruitmentJson.Utc(r,"serverTime");if(banner.Id!=id)throw new FormatException();SelectedBanner=banner;BannerReady=true;RecruitmentNotice="";}
            catch(Exception e){if(!Current(original,stamp)||request!=detailRequest)return;RecruitmentNotice="모집 상세 조회 실패 · "+e.Message;}
            finally{if(Current(original,stamp)&&request==detailRequest){DetailLoading=false;Changed?.Invoke();}}
        }
        public async Task RefreshHistoryAsync(bool more=false)
        {
            SynchronizeSession();if(more&&(HistoryCursor==null||HistoryLoading||!HistoryReady))return;var original=owner;var stamp=ownerGeneration;var request=++historyRequest;if(!Current(original,stamp))return;
            var cursor=more?HistoryCursor:null;HistoryReady=false;HistoryLoading=true;Changed?.Invoke();
            try{var r=await ReadRecruitment("/v1/gacha/history?limit=20"+(cursor==null?"":"&beforeBatchId="+RecruitmentJson.PathId(cursor)),original);if(!Current(original,stamp)||request!=historyRequest)return;
                var rows=RecruitmentJson.Array(r,"batches").Select(t=>new RecruitmentBatch((JObject)t)).ToArray();if(rows.Select(b=>b.Id).Distinct().Count()!=rows.Length)throw new FormatException();var next=RecruitmentJson.NullableString(r,"nextBeforeBatchId");if(next!=null){RecruitmentJson.PathId(next);if(next==cursor||!rows.Any(b=>b.Id==next))throw new FormatException();}
                History=(more?History.Concat(rows).GroupBy(b=>b.Id).Select(g=>g.Last()):rows).ToList().AsReadOnly();HistoryCursor=next;HistoryReady=true;RecruitmentNotice=History.Count==0?"모집 기록이 없습니다.":"";
            }catch(Exception e){if(!Current(original,stamp)||request!=historyRequest)return;RecruitmentNotice="기록 조회 실패 · "+e.Message;}
            finally{if(Current(original,stamp)&&request==historyRequest){HistoryLoading=false;Changed?.Invoke();}}
        }
        public async Task RefreshBatchAsync(string id)
        {
            SynchronizeSession();var original=owner;var stamp=ownerGeneration;var request=++batchRequest;if(!Current(original,stamp))return;
            if(ViewedBatchId!=id)SelectedBatch=null;ViewedBatchId=id;BatchReady=false;BatchLoading=true;Changed?.Invoke();
            try{var r=await ReadRecruitment("/v1/gacha/batches/"+RecruitmentJson.PathId(id),original);if(!Current(original,stamp)||request!=batchRequest)return;
                var batch=new RecruitmentBatch(r);if(batch.Id!=id)throw new FormatException();SelectedBatch=batch;BatchReady=true;
                History=History.Select(b=>b.Id==id?batch:b).ToList().AsReadOnly();RecruitmentNotice=batch.Complete?"모집 결과를 확인했습니다.":"일부 결과 또는 수량 확인이 필요합니다. 재조회하거나 고객문의로 확인해 주세요.";
                if(batch.Complete&&RecoveryBatchId==id&&Pending?.Kind=="gacha"&&Pending.Target==batch.BannerId&&Pending.Count==batch.Count&&journal.Remove(Pending)){Pending=null;RecoveryBatchId=null;}
            }catch(Exception e){if(!Current(original,stamp)||request!=batchRequest)return;RecruitmentNotice="결과 조회 실패 · "+e.Message;}
            finally{if(Current(original,stamp)&&request==batchRequest){BatchLoading=false;Changed?.Invoke();}}
        }
        public long RecruitmentTickets=>Account?.Items.FirstOrDefault(r=>r.Text("itemId")=="cur_command_ticket")?.Number("quantity")??0;
        public bool CanRecruit(int count)=>!Busy&&Pending==null&&RecoveryBatchId==null&&AccountStatus==AccountStateReadStatus.Ready&&BannersReady&&BannerReady&&SelectedBanner?.Availability=="ACTIVE"&&Banners.Any(b=>b.Id==SelectedBanner.Id)&&(count==1||count==10)&&RecruitmentTickets>=SelectedBanner.Price(count);
        public Task RecruitAsync(int count)=>CanRecruit(count)?PullAsync(SelectedBanner.Id,count):Task.CompletedTask;
    }
}
