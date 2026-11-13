using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell {public sealed partial class AccountScreenRuntime {
 long shopRequest;
 public ShopSnapshot Shop {get;private set;}
 public bool ShopReady {get;private set;}
 public bool ShopLoading {get;private set;}
 public string ShopError {get;private set;}
 public string ShopNotice {get;private set;}="상점 정보를 불러올 수 없습니다.";
 public ShopOfferReceipt LastShopOffer {get;private set;}
 void ResetShop(){shopRequest++;Shop=null;ShopReady=false;ShopLoading=false;ShopError=null;LastShopOffer=null;ShopNotice="상점 정보를 불러올 수 없습니다.";}
 public async Task RefreshShopAsync(){SynchronizeSession();var original=owner;var stamp=ownerGeneration;var request=++shopRequest;if(!Current(original,stamp))return;ShopReady=false;ShopLoading=true;ShopError=null;Changed?.Invoke();try{
  var reply=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("GET","/v1/shop",null,null),original.AccessToken));if(!Current(original,stamp)||request!=shopRequest)return;
  if(reply.IsNetworkError||reply.StatusCode!=200){ShopError=reply.IsNetworkError?"network_error":GameApiJson.String(GameApiJson.ReadObject(reply.BodyJson),"errorCode");ShopNotice="상점 정보를 불러오지 못했습니다.";return;}
  Shop=new ShopSnapshot(reply.BodyJson);ShopReady=true;ShopNotice=Shop.UnavailableReason==null?"":"교환 상품을 준비하고 있습니다.";
 }catch{if(Current(original,stamp)&&request==shopRequest){ShopError="invalid_response";ShopNotice="상점 정보 형식을 확인하지 못했습니다.";}}finally{if(Current(original,stamp)&&request==shopRequest){ShopLoading=false;Changed?.Invoke();}}}
 public bool CanBuyShopOffer(string offer,int count){if(Busy||Pending!=null||!ShopReady||ShopLoading||Shop==null||count<1)return false;var row=Shop.Offers.FirstOrDefault(o=>(string)o["offer"]==offer);return row!=null&&count<=(int)row["maxTicketsPerRequest"]&&(offer!="DAILY"||(int)row["dailyRemaining"]==1&&count==1)&&checked((long)row["crystalsPerTicket"]*count)<=Shop.Wallet.TotalBalance;}
 public Task BuyShopOfferAsync(string offer,int count){SynchronizeSession();if(!CanBuyShopOffer(offer,count))return Task.CompletedTask;return SubmitShopOfferAsync(new JObject{["policyVersion"]=Shop.Policy,["offer"]=offer,["ticketCount"]=count,["expectedDayStart"]=Shop.DayStart}.ToString());}
 async Task SubmitShopOfferAsync(string payload){SynchronizeSession();var original=owner;var stamp=ownerGeneration;if(Busy||!Current(original,stamp))return;writing=true;ActionErrorCode=null;ActionNotice="교환 결과를 확인하고 있습니다.";Changed?.Invoke();var refresh=false;try{
  ShopOfferReceipt.Request(payload);if(journal==null)throw new InvalidOperationException();var action=journal.Read(original.AccountId);if(action!=null&&(action.Kind!="shop-offer"||action.Payload!=payload)){Pending=action;ActionNotice="이전 요청 결과를 먼저 확인해 주세요.";return;}if(action==null){action=new PendingAccountAction(original.AccountId,"shop-offer","cur_command_ticket",0,Guid.NewGuid().ToString("N"),payload);if(!journal.Save(action))throw new InvalidOperationException();}Pending=action;shopRequest++;ShopReady=false;ShopLoading=false;walletRequest++;WalletLoading=false;
  var reply=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST","/v1/shop/tickets/exchange",payload,new[]{new RunApiHttpHeader("Idempotency-Key",action.Key)}),original.AccessToken));if(!Current(original,stamp))return;
  if(!reply.IsNetworkError&&reply.StatusCode==200){var receipt=new ShopOfferReceipt(reply.BodyJson,payload);if(!journal.Remove(action))throw new InvalidOperationException();Pending=null;LastShopOffer=receipt;refresh=true;ActionNotice="모집권 "+receipt.Exchange.Tickets+"장 교환 완료\n무료 "+receipt.Exchange.FreeSpent+" · 유료 "+receipt.Exchange.PaidSpent+" 크리스탈 사용";}
  else if(!reply.IsNetworkError&&(reply.StatusCode==400||reply.StatusCode==409)){var r=GameApiJson.ReadObject(reply.BodyJson);if(GameApiJson.Boolean(r,"accepted"))throw new FormatException();ActionErrorCode=GameApiJson.String(r,"errorCode");if(!journal.Remove(action))throw new InvalidOperationException();Pending=null;refresh=true;ActionNotice="교환 조건이 변경되었습니다. 가격·잔액·남은 횟수를 다시 확인해 주세요.";}
  else ActionNotice="교환 결과 확인이 필요합니다. 같은 요청으로 재확인해 주세요.";
 }catch{if(Current(original,stamp))ActionNotice="교환 결과를 확인하지 못했습니다. 같은 요청으로 재확인해 주세요.";}finally{writing=false;if(!disposed)Changed?.Invoke();}if(refresh&&Current(original,stamp))await Task.WhenAll(RefreshSharedAsync(),RefreshShopAsync());}
} }
