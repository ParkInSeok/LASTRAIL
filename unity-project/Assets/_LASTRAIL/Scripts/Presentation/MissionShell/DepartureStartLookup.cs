using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Submissions;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell
{
    public sealed class DepartureStartResult
    {
        readonly JObject data;
        public string Status=>(string)data["lookupStatus"];
        public string RunId=>(string)data["runId"];
        public string OperationPreviewId=>(string)data["startResponse"]?["operationPreviewId"];
        public string RunStatus=>(string)data["runStatus"];
        public string Error=>(string)data["errorCode"];
        public JObject StartResponse=>data["startResponse"] is JObject r?(JObject)r.DeepClone():null;
        public string Json=>data.ToString(Newtonsoft.Json.Formatting.None);
        LastRail.Battle.OrdinaryFiveStartInput ordinaryInput;
        public LastRail.Battle.OrdinaryFiveStartInput ReadOrdinaryInput(string expectedAccount)
        {
            if(Status!="COMMITTED")throw new InvalidOperationException("A committed original START is required.");
            if(ordinaryInput!=null){if(ordinaryInput.AccountId!=expectedAccount)throw new ArgumentException("Original START account differs.");return ordinaryInput;}
            return ordinaryInput=LastRail.Battle.OrdinaryFiveStartInput.Read((JObject)data["startResponse"],expectedAccount,RunId);
        }
        public DepartureStartResult(int http,string json,string previewId)
        {
            data=GameApiJson.ReadObject(json);var fields=new[]{"accepted","lookupStatus","runId","runStatus","startResponse","errorCode"};
            if(data.Count!=6||data.Properties().Any(p=>!fields.Contains(p.Name)))throw new FormatException();
            var status=GameApiJson.String(data,"lookupStatus");bool success=status=="COMMITTED"||status=="NOT_FOUND"||status=="SNAPSHOT_UNAVAILABLE";
            if(GameApiJson.Boolean(data,"accepted")!=success)throw new FormatException();
            var code=RecruitmentJson.NullableString(data,"errorCode");RecruitmentJson.NullableString(data,"runId");RecruitmentJson.NullableString(data,"runStatus");
            if(success){if(http!=200||code!=null)throw new FormatException();}
            else if(!(status=="CONFLICT"&&http==409&&code=="start_preview_mismatch"||status=="TEMPORARILY_UNAVAILABLE"&&http==503&&code=="start_lookup_unavailable"||status=="INVALID_REQUEST"&&http==400&&code=="contract_violation"))throw new FormatException();
            if(status=="COMMITTED"){
                RecruitmentJson.Id(data,"runId");GameApiJson.String(data,"runStatus");var original=GameApiJson.Object(data,"startResponse");
                if(!GameApiJson.Boolean(original,"accepted")||GameApiJson.String(original,"startStatus")!="STARTED"||GameApiJson.String(original,"runId")!=RunId||GameApiJson.String(original,"operationPreviewId")!=previewId)throw new FormatException();
                var forbidden=new[]{"authorization","accesstoken","refreshtoken","password","idtoken","secret","startidempotencykey"};
                if(original.Descendants().OfType<JProperty>().Any(p=>forbidden.Contains(p.Name.ToLowerInvariant())))throw new FormatException();
            }else{
                if(data["startResponse"]?.Type!=JTokenType.Null)throw new FormatException();
                if(status=="SNAPSHOT_UNAVAILABLE"){
                    if((RunId==null)!=(RunStatus==null))throw new FormatException();if(RunId!=null)RecruitmentJson.Id(data,"runId");
                }else if(RunId!=null||RunStatus!=null)throw new FormatException();
            }
        }
    }
    public interface IAccountStartReceiptJournal
    {bool ConfirmStart(PendingAccountAction original,string resultJson);}
    public sealed partial class AccountScreenRuntime
    {
        long departureLookupRequest;
        PendingAccountAction departureReceiptOriginal;
        DepartureStartResult departureReceiptAwaitingStorage;
        public bool DepartureReceiptNeedsSave=>departureReceiptAwaitingStorage!=null;
        public bool HasDepartureLookupRequest=>DepartureReceiptNeedsSave||Pending?.Kind=="departure-start";
        public bool CanRetryDepartureLookup=>HasDepartureLookupRequest&&!DepartureLookupLoading&&!Busy;
        public DepartureStartResult DepartureStartResult {get;private set;}
        public bool DepartureLookupLoading {get;private set;}
        public string DepartureLookupError {get;private set;}
        public string DepartureLookupStatus {get;private set;}
        public string DepartureLookupNotice {get;private set;}="";
        void ResetDepartureLookup(bool retainConfirmedReceipt=false)
        {
            departureLookupRequest++;
            if(!retainConfirmedReceipt){departureReceiptOriginal=null;departureReceiptAwaitingStorage=null;}
            DepartureStartResult=null;DepartureLookupLoading=false;
            DepartureLookupError=DepartureReceiptNeedsSave?"receipt_storage_unavailable":null;
            DepartureLookupStatus=DepartureReceiptNeedsSave?"COMMITTED":null;
            DepartureLookupNotice=DepartureReceiptNeedsSave?"출발은 확정되었습니다. 기록을 저장하지 못했습니다. 저장을 다시 시도해 주세요.":"";
            if(DepartureReceiptNeedsSave)ReportDepartureStorageFailure();
        }
        void ReportDepartureStorageFailure(Exception error=null)
        {
            var reason=error==null?(journal as ISecureStorageWriteDiagnostics)?.LastWriteFailure??SecureStorageWriteFailure.Unknown:AndroidEncryptedReceiptFilePort.ClassifyWriteFailure(error);
            DepartureLookupError="receipt_storage_unavailable";
            DepartureLookupNotice="출발은 확정되었습니다. 기록을 저장하지 못했습니다. 저장을 다시 시도해 주세요.";
            if(reason==SecureStorageWriteFailure.DiskFull){DepartureLookupError="receipt_storage_disk_full";DepartureLookupNotice="출발은 확정되었습니다. 기기 저장 공간이 부족합니다. 공간을 확보한 뒤 저장을 다시 시도해 주세요.";}
            else if(reason==SecureStorageWriteFailure.LogicalLimit){DepartureLookupError="receipt_storage_limit";DepartureLookupNotice="출발은 확정되었습니다. 기록이 앱의 저장 한도를 넘었습니다. 기기 공간 정리로 해결되지 않습니다. 원래 요청을 유지했습니다.";}
            else if(reason==SecureStorageWriteFailure.AccessDenied){DepartureLookupError="receipt_storage_access_denied";DepartureLookupNotice="출발은 확정되었습니다. 저장 위치에 접근할 수 없습니다. 접근 권한을 확인한 뒤 저장을 다시 시도해 주세요.";}
        }
        void SaveConfirmedDepartureReceipt()
        {
            var action=departureReceiptOriginal;var result=departureReceiptAwaitingStorage;
            DepartureLookupStatus="COMMITTED";
            DepartureLookupError="receipt_storage_unavailable";
            DepartureLookupNotice="출발은 확정되었습니다. 기록을 저장하지 못했습니다. 저장을 다시 시도해 주세요.";
            if(!(journal is IAccountStartReceiptJournal receipts)||!receipts.ConfirmStart(action,result.Json)){ReportDepartureStorageFailure();return;}
            var saved=journal.Read(action.Account);
            if(saved==null||saved.Key!=action.Key||saved.Kind!=action.Kind||saved.Target!=action.Target||saved.Payload!=action.Payload||saved.StartResultJson==null)throw new InvalidOperationException();
            var committed=new DepartureStartResult(200,saved.StartResultJson,saved.Target);
            if(committed.RunId!=result.RunId||!JToken.DeepEquals(committed.StartResponse,result.StartResponse))throw new InvalidOperationException();
            Pending=saved;DepartureStartResult=result;departureReceiptOriginal=null;departureReceiptAwaitingStorage=null;
            DepartureLookupError=null;DepartureLookupNotice="출발 기록을 확인했습니다. 전투 준비가 완료되면 이어갈 수 있습니다.";
        }
        void RestoreDepartureResult()
        {
            if(Pending?.Kind!="departure-start"||Pending.StartResultJson==null)return;
            DepartureStartResult=new DepartureStartResult(200,Pending.StartResultJson,Pending.Target);
            DepartureLookupNotice="보관된 출발 기록이 있습니다. 최신 상태를 확인해 주세요.";
        }
        public async Task LookupDepartureStartAsync()
        {
            SynchronizeSession();var original=owner;var stamp=ownerGeneration;if(writing||DepartureLookupLoading||!Current(original,stamp))return;
            var request=++departureLookupRequest;DepartureLookupLoading=true;DepartureLookupError=null;DepartureLookupStatus=null;DepartureLookupNotice="출발 처리 결과를 확인하고 있습니다.";Changed?.Invoke();
            try{
                if(DepartureReceiptNeedsSave){SaveConfirmedDepartureReceipt();return;}
                var action=journal?.Read(original.AccountId);
                if(action?.Kind!="departure-start"){DepartureLookupNotice="확인할 출발 요청이 없습니다.";return;}
                Pending=action;var saved=DepartureContract.Validate(action.Kind,action.Target,action.Payload);
                var body=new JObject{["startIdempotencyKey"]=action.Key,["operationPreviewId"]=GameApiJson.String(saved,"operationPreviewId")};
                // A distinct read endpoint: original key stays only in the bounded body.
                // Never log/echo this body or add an Idempotency-Key lookup header.
                var response=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST","/v1/runs/start-result",body.ToString(),null),original.AccessToken));
                if(!Current(original,stamp)||request!=departureLookupRequest)return;
                if(response.IsNetworkError){DepartureLookupError="network_error";DepartureLookupNotice="아직 결과를 확인하지 못했습니다. 다시 확인해 주세요.";return;}
                var raw=GameApiJson.ReadObject(response.BodyJson);
                if(raw["lookupStatus"]==null){DepartureLookupError=GameApiJson.String(raw,"errorCode",true)??"http_"+response.StatusCode;DepartureLookupNotice=response.StatusCode==401?"로그인 상태를 다시 확인해 주세요.":"출발 처리 결과를 불러오지 못했습니다. 다시 확인해 주세요.";return;}
                var result=new DepartureStartResult(response.StatusCode,response.BodyJson,action.Target);DepartureLookupError=result.Error;DepartureLookupStatus=result.Status;
                if(result.Status=="COMMITTED"){
                    // Keep the validated response before any storage access can fail.
                    // It is not exposed as a durable battle entry until persistence succeeds.
                    departureReceiptOriginal=action;departureReceiptAwaitingStorage=result;
                    SaveConfirmedDepartureReceipt();
                }else{
                    var current=journal.Read(original.AccountId);
                    if(current==null||current.Kind!=action.Kind||current.Key!=action.Key||current.Payload!=action.Payload||current.Target!=action.Target)return;
                    // An incomplete later read never erases a durably confirmed original response.
                    if(Pending.StartResultJson==null)DepartureStartResult=result;
                    DepartureLookupNotice=result.Status=="NOT_FOUND"?"아직 출발 처리 결과를 확인하지 못했습니다. 잠시 후 다시 확인해 주세요.":
                        result.Status=="CONFLICT"?"출발 요청 정보를 확인할 수 없습니다. 원래 요청을 보관했습니다.":
                        result.Status=="SNAPSHOT_UNAVAILABLE"?"출발 기록을 복원하지 못했습니다. 원래 요청을 보관했습니다.":"출발 처리 결과를 불러오지 못했습니다. 다시 확인해 주세요.";
                }
            }catch(Exception error){if(Current(original,stamp)&&request==departureLookupRequest){if(DepartureReceiptNeedsSave)ReportDepartureStorageFailure(error);else{DepartureLookupError="lookup_unconfirmed";DepartureLookupNotice="출발 처리 결과를 확인하지 못했습니다. 원래 요청을 보관했습니다.";}}}
            finally{if(Current(original,stamp)&&request==departureLookupRequest){DepartureLookupLoading=false;Changed?.Invoke();}}
        }
    }
}
