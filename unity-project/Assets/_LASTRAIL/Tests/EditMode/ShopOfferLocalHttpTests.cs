using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace LastRail.Tests.EditMode {public sealed partial class FormationLocalHttpTests {
 [Test,Explicit("Frozen9323bf0 API, memory-only approved-price policy and test SKU; no Store/purchase proof.")]
 public async Task ActualShopOffersNativeDailyConflictPolicyReplayAndNormalExchange(){
  var(wire,session)=await Setup();Func<string,Task<RunApiHttpResponse>> control=path=>wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST","/__fixture/"+path,null,null),session.AccessToken));var journal=new Journal();using var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1);using var competitor=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1);
  await state.RefreshShopAsync();Assert.That(state.ShopReady,Is.False);await control("shop-enable");await state.RefreshShopAsync();Assert.That(state.ShopReady,Is.True,state.ShopError);Assert.That(state.Shop.Offers,Is.Empty);await control("shop-offer-policy");await state.RefreshShopAsync();await competitor.RefreshShopAsync();Assert.That(state.ShopReady,Is.True,state.ShopError);Assert.That(state.Shop.Offers.Count,Is.EqualTo(2));Assert.That(state.Shop.Products[0]["price"].Type,Is.EqualTo(JTokenType.Null));Assert.That((bool)state.Shop.Products[0]["purchaseAvailable"],Is.False);
  var root=new GameObject("ShopOfferHttpUi",typeof(RectTransform));((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);var pages=LobbyDestinationPages.Create(root.transform,UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset"),()=>{});pages.BindAccount(state);Action redraw=()=>{if(pages!=null)pages.RefreshAccountView(state);};state.Changed+=redraw;
  try{pages.Open(LobbyPage.Shop);while(state.ShopLoading||state.WalletLoading)await Task.Yield();pages.GetComponentsInChildren<Button>().Single(b=>b.name=="OfferDaily").onClick.Invoke();Assert.That(pages.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="TicketSpend").text,Does.Contain("유료 35"));wire.LoseDeparture="/v1/shop/tickets/exchange";pages.GetComponentsInChildren<Button>().Single(b=>b.name=="ExchangeTickets").onClick.Invoke();while(state.Busy)await Task.Yield();Assert.That(state.Pending?.Kind,Is.EqualTo("shop-offer"));var original=state.Pending;
   await competitor.BuyShopOfferAsync("DAILY",1);Assert.That(competitor.ActionErrorCode,Is.EqualTo("shop_daily_limit_reached"));Assert.That(competitor.Pending,Is.Null);await control("shop-offer-policy-next");await state.RetryPendingAsync();Assert.That(state.Pending,Is.Null,state.ActionNotice);Assert.That(state.LastShopOffer.Exchange.FreeSpent,Is.EqualTo(15));Assert.That(state.LastShopOffer.Exchange.PaidSpent,Is.EqualTo(35));Assert.That(state.Shop.Policy,Is.EqualTo("fixture-launch-v2"));Assert.That(state.CanBuyShopOffer("DAILY",1),Is.False);Assert.That(state.Shop.Wallet.TotalBalance,Is.EqualTo(65));
   var calls=wire.Requests.Where(r=>r.EndpointPath=="/v1/shop/tickets/exchange").ToArray();Assert.That(calls.Length,Is.EqualTo(3));Assert.That(Key(calls[0]),Is.EqualTo(original.Key));Assert.That(Key(calls[2]),Is.EqualTo(original.Key));Assert.That(calls[2].BodyJson,Is.EqualTo(original.Payload));
   typeof(AccountScreenIntegrationTests).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root,"shop-daily-pinned-local-http.png"});
   // A new stale-day request is rejected and refreshed, never silently repurchased today.
   var oldBody=JObject.Parse(original.Payload);oldBody["policyVersion"]=state.Shop.Policy;oldBody["expectedDayStart"]=(long)oldBody["expectedDayStart"]-86400000;journal.Save(new PendingAccountAction("account_test","shop-offer","cur_command_ticket",0,"stale_day",oldBody.ToString()));state.SynchronizeSession();using(var recovery=new AccountScreenRuntime(wire,journal,()=>session,()=>1)){await recovery.RetryPendingAsync();Assert.That(recovery.Pending,Is.Null);Assert.That(recovery.ActionErrorCode,Is.EqualTo("shop_day_changed"));}
   await control("shop-offer-fund");await state.RefreshShopAsync();pages.GetComponentsInChildren<Button>().Single(b=>b.name=="OfferNormal").onClick.Invoke();pages.GetComponentsInChildren<Button>().Single(b=>b.name=="TicketPlus").onClick.Invoke();Assert.That(pages.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="TicketSpend").text,Does.Contain("합계 200"));pages.GetComponentsInChildren<Button>().Single(b=>b.name=="ExchangeTickets").onClick.Invoke();while(state.Busy||state.ShopLoading)await Task.Yield();Assert.That(state.Pending,Is.Null,state.ActionNotice);Assert.That(state.LastShopOffer.Exchange.Tickets,Is.EqualTo(2));Assert.That(state.LastShopOffer.Exchange.FreeSpent,Is.EqualTo(200));Assert.That(state.CanBuyShopOffer("NORMAL",1),Is.False);
   var metrics=JObject.Parse((await control("shop-offer-metrics")).BodyJson);Assert.That((int)metrics["daily"],Is.EqualTo(1));Assert.That((int)metrics["tickets"],Is.EqualTo(3));Assert.That((int)metrics["balance"],Is.EqualTo(65));
  }finally{state.Changed-=redraw;UnityEngine.Object.DestroyImmediate(root);}
 }
} }
