using System;
using System.Linq;
using UnityEngine.UI;
namespace LastRail.Presentation.MissionShell {public sealed partial class LobbyDestinationPages {
 readonly ShopScreenSelection shopSelection=new ShopScreenSelection();ShopScreenController shopController;
 async void RefreshShop(){if(shopController!=null)await shopController.RefreshAsync();}
 void LiveShop(){
  var controller=shopController=new ShopScreenController(accountState,shopSelection);releasePageBinding+=controller.Dispose;Action<ShopScreenState> renderCatalog;
  if(Category==1){
   var view=AdSkipShopView.Create(content,font);view.Bind(async()=>await controller.RefreshAdSkipAsync());
   renderCatalog=state=>{view.SetNewSalesAllowed(state.AdSkipSalesAllowed);view.Render(state.AdSkip,state.AdSkipLoading,state.AdSkipNotice);};
  }else if(Category==2){
   var empty=Text("ShopEmpty",content,"등록된 크리스탈 상품이 없습니다.",45,610,850,100,30,true);
   var panels=new UnityEngine.RectTransform[3];var quantities=new TMPro.TMP_Text[3];var prices=new TMPro.TMP_Text[3];
   for(int i=0;i<3;i++){float y=550+i*190;panels[i]=Panel("CrystalProduct"+i,content,34,y,873,173);quantities[i]=Text("CrystalQuantity"+i,content,"",58,y+18,809,55,34,true);prices[i]=Text("StorePrice"+i,content,"스토어 가격 확인 필요 · 구매 연결 준비 중",58,y+92,809,55,26,true);}
   var page=Button("CrystalProductsPage",content,647,1178,259,60,"",controller.NextProductPage);
   Button("ShopPassCatalog",content,34,1178,590,78,"시즌 패스",()=>{Close();openPass?.Invoke();});
   renderCatalog=state=>{
    empty.gameObject.SetActive(state.ProductsEmpty);page.gameObject.SetActive(state.ShowProductPage);page.GetComponentInChildren<TMPro.TMP_Text>().text=state.ProductPage;
    for(int i=0;i<3;i++){bool exists=i<state.ProductQuantities.Count;panels[i].gameObject.SetActive(exists);quantities[i].gameObject.SetActive(exists);prices[i].gameObject.SetActive(exists);quantities[i].text=exists?"크리스탈 "+state.ProductQuantities[i]:"";}
   };
  }else{
   int start=content.childCount;
   var normal=Button("OfferNormal",content,34,529,425,68,"일반 교환",()=>controller.SelectOffer("NORMAL"));
   var daily=Button("OfferDaily",content,482,529,425,68,"일일 할인",()=>controller.SelectOffer("DAILY"));
   Panel("TicketOffer",content,34,619,873,593);Text("TicketTitle",content,"모집권 교환",65,637,811,60,40,true);
   var balance=Text("TicketBalance",content,"",65,710,811,64,27,true);var price=Text("TicketUnitPrice",content,"",65,787,811,55,29,true);
   var minus=Button("TicketMinus",content,174,865,140,75,"−",()=>controller.ChangeCount(-1));
   var amount=Text("TicketCount",content,"",329,865,283,75,38,true);var plus=Button("TicketPlus",content,627,865,140,75,"+",()=>controller.ChangeCount(1));
   var spend=Text("TicketSpend",content,"",65,963,811,100,29,true);
   var buy=Button("ExchangeTickets",content,210,1090,521,86,"교환",async()=>await controller.BuyAsync(),true);
   var reset=Text("ShopReset",content,"",45,1235,850,45,25,true);
   var parts=content.Cast<UnityEngine.Transform>().Skip(start).ToArray();var unavailable=Text("ShopUnavailable",content,"",45,610,850,100,30,true);
   renderCatalog=state=>{
    foreach(var part in parts)part.gameObject.SetActive(state.Ready);unavailable.gameObject.SetActive(!state.Ready);unavailable.text=state.Unavailable;if(!state.Ready)return;
    normal.GetComponent<UnityEngine.UI.Image>().color=state.NormalSelected?new UnityEngine.Color(.02f,.22f,.37f,.98f):new UnityEngine.Color(.025f,.033f,.04f,.97f);
    daily.GetComponent<UnityEngine.UI.Image>().color=!state.NormalSelected?new UnityEngine.Color(.02f,.22f,.37f,.98f):new UnityEngine.Color(.025f,.033f,.04f,.97f);
    balance.text=state.Balance;price.text=state.UnitPrice;amount.text=state.Count;
    minus.interactable=state.CanDecrease;plus.interactable=state.CanIncrease;spend.text=state.Spend;
    buy.GetComponentInChildren<TMPro.TMP_Text>().text=state.BuyLabel;buy.interactable=state.CanBuy;reset.text=state.Reset;
   };
  }
  var notice=Text("ShopActionNotice",content,"",45,1290,850,95,25,true);
  var refresh=Button("ShopRefresh",content,34,1400,425,80,"새로고침",async()=>await controller.RefreshAsync());
  var retry=Button("RetryTicketExchange",content,482,1400,425,80,"교환 결과 재확인",async()=>await controller.RetryAsync(),true);
  renderBody=controller.Render;controller.Bind(state=>{renderCatalog(state);notice.text=state.Notice;refresh.interactable=state.CanRefresh;retry.gameObject.SetActive(state.ShowRetry);retry.interactable=state.CanRetry;});
 }
} }
