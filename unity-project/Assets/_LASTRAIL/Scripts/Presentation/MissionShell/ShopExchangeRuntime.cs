using System;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell
{
    public sealed class TicketExchangeReceipt
    {
        public long Tickets {get;}
        public long FreeSpent {get;}
        public long PaidSpent {get;}
        public string OperationId {get;}
        public TicketExchangeReceipt(string json,string requestJson)
        {
            var request=Validate("cur_command_ticket",requestJson);var r=GameApiJson.ReadObject(json);
            if(!GameApiJson.Boolean(r,"accepted")||GameApiJson.String(r,"policyVersion")!=GameApiJson.String(request,"policyVersion")||GameApiJson.String(r,"ticketItemId")!="cur_command_ticket")throw new FormatException();
            OperationId=GameApiJson.String(r,"operationId");GameApiJson.String(r,"currencyId");Tickets=AccountStateSnapshot.Number(r,"ticketsGranted");
            if(Tickets!=AccountStateSnapshot.Number(request,"ticketCount")||AccountStateSnapshot.Number(r,"ticketBalanceAfter")<Tickets)throw new FormatException();
            FreeSpent=AccountStateSnapshot.Number(r,"freeSpent");PaidSpent=AccountStateSnapshot.Number(r,"paidSpent");
            if(checked(FreeSpent+PaidSpent)<1||AccountStateSnapshot.Number(r,"freeGranted")!=0||AccountStateSnapshot.Number(r,"paidGranted")!=0||
                checked(AccountStateSnapshot.Number(r,"freeBalance")+AccountStateSnapshot.Number(r,"paidBalance"))!=AccountStateSnapshot.Number(r,"totalBalance"))throw new FormatException();
        }
        public static JObject Validate(string target,string json)
        {
            if(target!="cur_command_ticket"||json==null||json.Length>1500)throw new FormatException();var r=GameApiJson.ReadObject(json);
            if(r.Count!=2||!System.Text.RegularExpressions.Regex.IsMatch(GameApiJson.String(r,"policyVersion"),@"\A[A-Za-z0-9_-]{1,100}\z"))throw new FormatException();var count=AccountStateSnapshot.Number(r,"ticketCount");if(count<1||count>int.MaxValue)throw new FormatException();return r;
        }
    }
    public sealed partial class AccountScreenRuntime
    {
        public TicketExchangeReceipt LastTicketExchange {get;private set;}
        public bool CanExchangeTickets(int count)
        {
            var offer=Wallet?.TicketExchange;
            if(Busy||Pending!=null||WalletLoading||Wallet?.Status!=CrystalWalletReadStatus.Ready||offer==null||count<1||count>offer.MaxTickets)return false;
            return checked(offer.CrystalsPerTicket*count)<=Wallet.TotalBalance;
        }
        public Task ExchangeTicketsAsync(int count)
        {
            SynchronizeSession();if(!CanExchangeTickets(count))return Task.CompletedTask;
            return SubmitTicketExchangeAsync(new JObject{["policyVersion"]=Wallet.PolicyVersion,["ticketCount"]=count}.ToString());
        }
        async Task SubmitTicketExchangeAsync(string payload)
        {
            SynchronizeSession();var original=owner;var stamp=ownerGeneration;if(Busy||!Current(original,stamp))return;
            writing=true;ActionErrorCode=null;LastTicketExchange=null;ActionNotice="교환 결과를 확인하고 있습니다.";Changed?.Invoke();var refresh=false;
            try{
                TicketExchangeReceipt.Validate("cur_command_ticket",payload);if(journal==null)throw new InvalidOperationException();
                var action=journal.Read(original.AccountId);
                if(action!=null&&(action.Kind!="ticket-exchange"||action.Payload!=payload)){Pending=action;ActionNotice="이전 요청 결과를 먼저 확인해 주세요.";return;}
                if(action==null){action=new PendingAccountAction(original.AccountId,"ticket-exchange","cur_command_ticket",0,Guid.NewGuid().ToString("N"),payload);if(!journal.Save(action))throw new InvalidOperationException();}
                Pending=action;walletRequest++;Wallet=null;WalletLoading=false;
                var reply=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST","/v1/wallet/crystals/tickets/exchange",payload,new[]{new RunApiHttpHeader("Idempotency-Key",action.Key)}),original.AccessToken));
                if(!Current(original,stamp))return;
                if(!reply.IsNetworkError&&reply.StatusCode==200){
                    var receipt=new TicketExchangeReceipt(reply.BodyJson,payload);if(!journal.Remove(action))throw new InvalidOperationException();
                    Pending=null;LastTicketExchange=receipt;refresh=true;ActionNotice="모집권 "+receipt.Tickets+"장 교환을 완료했습니다.\n무료 크리스탈 "+receipt.FreeSpent+" · 유료 크리스탈 "+receipt.PaidSpent+" 사용";
                }else if(!reply.IsNetworkError&&(reply.StatusCode==400||reply.StatusCode==409)){
                    var error=GameApiJson.ReadObject(reply.BodyJson);if(GameApiJson.Boolean(error,"accepted"))throw new FormatException();ActionErrorCode=GameApiJson.String(error,"errorCode");
                    if(!journal.Remove(action))throw new InvalidOperationException();Pending=null;refresh=true;ActionNotice="교환 조건이 변경되었거나 크리스탈이 부족합니다. 최신 가격과 잔액을 확인해 주세요.";
                }else ActionNotice="교환 결과 확인이 필요합니다. 같은 요청으로 재확인해 주세요.";
            }catch{if(Current(original,stamp))ActionNotice="교환 결과를 확인하지 못했습니다. 같은 요청으로 재확인해 주세요.";}
            finally{writing=false;if(!disposed)Changed?.Invoke();}
            if(refresh&&Current(original,stamp))await RefreshSharedAsync();
        }
    }
}

