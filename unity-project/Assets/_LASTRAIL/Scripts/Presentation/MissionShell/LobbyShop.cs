using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine.UI;
namespace LastRail.Presentation.MissionShell {public sealed partial class LobbyDestinationPages {
 int shopTicketCount=1,shopProductPage;string shopOffer="NORMAL";
 async void RefreshShop(){var state=accountState;if(state!=null)await System.Threading.Tasks.Task.WhenAll(state.RefreshSharedAsync(),state.RefreshShopAsync(),state.RefreshAdSkipAsync());}
 void LiveShop(){var state=accountState;var data=state?.Shop;var wallet=data?.Wallet;
  if(Category==1){var skip=state?.AdSkip;var view=AdSkipShopView.Create(content,font);view.Bind(async()=>{var owner=accountState;if(owner!=null)await owner.RefreshAdSkipAsync();});view.SetNewSalesAllowed(state?.RuntimeAds.Status==AdRuntimePolicyStatus.Enabled);view.Render(skip?.Snapshot,skip?.Loading==true,skip?.Notice);}
  else if(Category==2){var products=data?.Products??new JArray();if(products.Count==0)Text("ShopEmpty",content,"등록된 크리스탈 상품이 없습니다.",45,610,850,100,30,true);
   var count=Math.Max(1,(products.Count+2)/3);shopProductPage=Math.Min(shopProductPage,count-1);for(int i=0;i<3;i++){int index=shopProductPage*3+i;if(index>=products.Count)break;var product=products[index];float y=550+i*190;Panel("CrystalProduct"+i,content,34,y,873,173);Text("CrystalQuantity"+i,content,"크리스탈 "+(long)product["quantity"],58,y+18,809,55,34,true);Text("StorePrice"+i,content,"스토어 가격 확인 필요 · 구매 연결 준비 중",58,y+92,809,55,26,true);}
   if(products.Count>3)Button("CrystalProductsPage",content,647,1178,259,60,(shopProductPage+1)+" / "+count,()=>{shopProductPage=(shopProductPage+1)%count;Rebuild();});
   Button("ShopPassCatalog",content,34,1178,590,78,"시즌 패스",()=>{Close();GetComponentInParent<LobbyCommandDeck>()?.OpenPass();});
  }
  else if(data!=null&&data.Offers.Count>0){var offer=(JObject)data.Offers.First(o=>(string)o["offer"]==shopOffer);var max=(int)offer["maxTicketsPerRequest"];shopTicketCount=Math.Max(1,Math.Min(shopTicketCount,max));
   Button("OfferNormal",content,34,529,425,68,"일반 교환",()=>{shopOffer="NORMAL";shopTicketCount=1;Rebuild();},shopOffer=="NORMAL");Button("OfferDaily",content,482,529,425,68,"일일 할인",()=>{shopOffer="DAILY";shopTicketCount=1;Rebuild();},shopOffer=="DAILY");
   Panel("TicketOffer",content,34,619,873,593);Text("TicketTitle",content,"모집권 교환",65,637,811,60,40,true);
   Text("TicketBalance",content,"보유 크리스탈 "+wallet.TotalBalance+" · 무료 "+wallet.FreeBalance+" / 유료 "+wallet.PaidBalance,65,710,811,64,27,true);
   var unit=(long)offer["crystalsPerTicket"];Text("TicketUnitPrice",content,"1장당 "+unit+" 크리스탈"+(shopOffer=="DAILY"?" · 오늘 남은 횟수 "+(int)offer["dailyRemaining"]:" · 요청당 최대 "+max+"장"),65,787,811,55,29,true);
   var minus=Button("TicketMinus",content,174,865,140,75,"−",()=>{shopTicketCount=Math.Max(1,shopTicketCount-1);Rebuild();});minus.interactable=state.Pending==null&&!state.Busy&&!state.ShopLoading&&shopTicketCount>1;
   Text("TicketCount",content,shopTicketCount+"장",329,865,283,75,38,true);var plus=Button("TicketPlus",content,627,865,140,75,"+",()=>{shopTicketCount=Math.Min(max,shopTicketCount+1);Rebuild();});plus.interactable=state.Pending==null&&!state.Busy&&!state.ShopLoading&&shopTicketCount<max;
   var cost=checked(unit*shopTicketCount);var free=Math.Min(cost,wallet.FreeBalance.Value);Text("TicketSpend",content,"합계 "+cost+" 크리스탈\n무료 "+free+" · 유료 "+(cost-free)+" 사용 예정",65,963,811,100,29,true);
   var buy=Button("ExchangeTickets",content,210,1090,521,86,shopOffer=="DAILY"&&(int)offer["dailyRemaining"]==0?"오늘 교환 완료":"교환",async()=>{await state.BuyShopOfferAsync(shopOffer,shopTicketCount);},true);buy.interactable=state.CanBuyShopOffer(shopOffer,shopTicketCount);
   Text("ShopReset",content,"일일 할인 갱신 "+DateTimeOffset.FromUnixTimeMilliseconds(data.NextReset).ToOffset(TimeSpan.FromHours(9)).ToString("MM/dd HH:mm"),45,1235,850,45,25,true);
  }else Text("ShopUnavailable",content,state?.ShopLoading==true?"상품 정보를 확인하고 있습니다.":state?.ShopNotice??"교환 상품을 불러오지 못했습니다.",45,610,850,100,30,true);
  Text("ShopActionNotice",content,state?.ActionNotice??"",45,1290,850,95,25,true);
  Button("ShopRefresh",content,34,1400,425,80,"새로고침",()=>RefreshShop()).interactable=state!=null&&!state.Busy&&!state.ShopLoading;
  if(state?.Pending?.Kind=="shop-offer"||state?.Pending?.Kind=="ticket-exchange"){var retry=Button("RetryTicketExchange",content,482,1400,425,80,"교환 결과 재확인",async()=>{await state.RetryPendingAsync();},true);retry.interactable=!state.Busy;}
 }
} }
