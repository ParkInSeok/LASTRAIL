using System;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;

namespace LastRail.OfflineRewards
{
    public sealed class OfflineClaimReceipt
    {
        public bool Confirmed {get;} public string ReceiptId {get;} public string ErrorCode {get;} public bool DefinitiveRejection {get;}
        public OfflineEconomyMetadata EconomyMetadata {get;}
        public OfflineClaimReceipt(bool confirmed,string receiptId=null,string errorCode=null,bool definitiveRejection=false,OfflineEconomyMetadata economyMetadata=null)
        {Confirmed=confirmed;ReceiptId=receiptId;ErrorCode=errorCode;DefinitiveRejection=definitiveRejection;EconomyMetadata=economyMetadata;}
    }
    public sealed class OfflineRewardHttpClient : IOfflineRewardPreviewClient
    {
        readonly IRunApiHttpTransport wire;
        readonly GameApiAccessToken token;
        public OfflineRewardHttpClient(IRunApiHttpTransport wire, GameApiAccessToken token)
        { this.wire=wire??throw new ArgumentNullException(nameof(wire));this.token=token??throw new ArgumentNullException(nameof(token)); }
        public async Task<OfflineRewardPreviewClientResult> ReadAsync(OfflineRewardPreviewRequest request)
        {
            if(request==null)throw new ArgumentNullException(nameof(request));
            try{
                var response=await Send(new RunApiHttpRequest("GET",request.EndpointPath,null,null));
                if(response.IsNetworkError||response.StatusCode>=500)return OfflineRewardPreviewClientResult.NetworkError("연결 상태를 확인해 주세요.");
                if(response.StatusCode!=200)return OfflineRewardPreviewClientResult.Rejected(Code(response),"보급 정보를 확인하지 못했습니다.");
                var root=GameApiJson.ReadObject(response.BodyJson);
                if(!GameApiJson.Boolean(root,"accepted"))return OfflineRewardPreviewClientResult.Rejected(Code(response),"보급 정보를 확인하지 못했습니다.");
                var dto=OfflineRewardWireJson.Preview(root);
                if(!dto.Accepted)return OfflineRewardPreviewClientResult.Rejected(dto.ErrorCode??"rejected","보급 정보를 확인하지 못했습니다.");
                if(dto.PreviewStatus==OfflineRewardPreviewStatus.Available)return OfflineRewardPreviewClientResult.Available(dto);
                if(dto.PreviewStatus==OfflineRewardPreviewStatus.Empty)return OfflineRewardPreviewClientResult.Empty(dto);
                return OfflineRewardPreviewClientResult.Rejected("invalid_response","보급 정보를 확인하지 못했습니다.");
            }catch{return OfflineRewardPreviewClientResult.NetworkError("보급 응답을 확인하지 못했습니다.");}
        }
        public async Task<OfflineClaimReceipt> ClaimReceiptAsync(OfflineRewardClaimRequest request)
        {
            if(request==null)throw new ArgumentNullException(nameof(request));
            try{
                var response=await Send(new RunApiHttpRequest("POST",request.EndpointPath,null,new[]{new RunApiHttpHeader(request.IdempotencyHeaderName,request.IdempotencyHeaderValue)}));
                if(response.IsNetworkError||response.StatusCode>=500||response.StatusCode==401)return new OfflineClaimReceipt(false,errorCode:Code(response));
                var root=GameApiJson.ReadObject(response.BodyJson);
                var status=GameApiJson.String(root,"claimStatus");var accepted=GameApiJson.Boolean(root,"accepted");
                if(!accepted&&status=="REJECTED")return new OfflineClaimReceipt(false,errorCode:GameApiJson.String(root,"errorCode"),definitiveRejection:true);
                if(response.StatusCode!=200)return new OfflineClaimReceipt(false,errorCode:"invalid_response");
                if(!(accepted&&status=="CLAIMED")&&!(!accepted&&status=="DUPLICATE_REPLAY"&&GameApiJson.String(root,"errorCode")=="duplicate_replay"))
                    return new OfflineClaimReceipt(false,errorCode:Code(response));
                // Duplicate receipts deliberately omit amounts/calculation. Never invent them.
                var id=GameApiJson.String(root,"offlineRewardClaimId");
                GameApiJson.String(root,"rewardTransactionId");GameApiJson.UtcTimestamp(root,"serverClaimedAt");
                return new OfflineClaimReceipt(true,id,economyMetadata:OfflineEconomyMetadata.Read(root));
            }catch{return new OfflineClaimReceipt(false,errorCode:"invalid_response");}
        }
        Task<RunApiHttpResponse> Send(RunApiHttpRequest request)=>wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(request,token));
        static string Code(RunApiHttpResponse response)
        {if(response.StatusCode==401)return "auth_required";try{return (string)JObject.Parse(response.BodyJson)["errorCode"]??"rejected";}catch{return "invalid_response";}}
    }
}
