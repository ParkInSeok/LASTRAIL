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
namespace LastRail.Tests.EditMode
{
    public sealed partial class FormationLocalHttpTests
    {
        [Test,Explicit("Frozen b63d5f4 real API, memory-only shop policy/grant fixture. No actual purchase.")]
        public async Task ActualShopNativeExchangeLostReplyPolicyReplayAndInsufficientBalance()
        {
            var(wire,session)=await Setup();var journal=new Journal();
            Func<string,Task<RunApiHttpResponse>> control=path=>wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST","/__fixture/"+path,null,null),session.AccessToken));
            using var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1);await state.RefreshSharedAsync();Assert.That(state.CanExchangeTickets(1),Is.False);
            Assert.That((await control("shop-enable")).StatusCode,Is.EqualTo(200));await state.RefreshSharedAsync();Assert.That(state.Wallet.TotalBalance,Is.EqualTo(115));Assert.That(state.Wallet.TicketExchange.CrystalsPerTicket,Is.EqualTo(11));
            var root=new GameObject("ShopHttpUi",typeof(RectTransform));((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);
            try{
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var pages=LobbyDestinationPages.Create(root.transform,font,()=>{});pages.BindAccount(state);
                Action redraw=()=>{if(pages!=null)pages.RefreshAccountView(state);};state.Changed+=redraw;
                try{
                    pages.Open(LobbyPage.Shop);while(state.WalletLoading||state.AccountStatus==AccountStateReadStatus.Loading)await Task.Yield();
                    // Compatibility API remains callable, but non100 fixture policy no longer appears in launch Shop UI.
                    wire.LoseDeparture="/v1/wallet/crystals/tickets/exchange";await state.ExchangeTicketsAsync(2);while(state.Busy)await Task.Yield();
                    Assert.That(state.Pending?.Kind,Is.EqualTo("ticket-exchange"));var original=journal.Read("account_test");
                    await control("shop-policy-change");await state.RetryPendingAsync();Assert.That(state.Pending,Is.Null,state.ActionNotice);Assert.That(state.LastTicketExchange.Tickets,Is.EqualTo(2));Assert.That(state.LastTicketExchange.FreeSpent,Is.EqualTo(15));Assert.That(state.LastTicketExchange.PaidSpent,Is.EqualTo(7));
                    Assert.That(state.Wallet.PolicyVersion,Is.EqualTo("fixture-shop-v2"));Assert.That(state.Wallet.TotalBalance,Is.EqualTo(93));
                    var writes=wire.Requests.Where(r=>r.EndpointPath=="/v1/wallet/crystals/tickets/exchange").ToArray();Assert.That(writes.Length,Is.EqualTo(2));Assert.That(Key(writes[0]),Is.EqualTo(original.Key));Assert.That(Key(writes[1]),Is.EqualTo(original.Key));Assert.That(writes[1].BodyJson,Is.EqualTo(original.Payload));
                    var metrics=JObject.Parse((await control("shop-metrics")).BodyJson);Assert.That((int)metrics["exchanges"],Is.EqualTo(1));Assert.That((int)metrics["tickets"],Is.EqualTo(2));
                    // Current native Shop offer screenshot is covered by ShopOfferLocalHttpTests.
                    var external=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST","/v1/wallet/crystals/tickets/exchange","{\"policyVersion\":\"fixture-shop-v2\",\"ticketCount\":5}",new[]{new RunApiHttpHeader("Idempotency-Key","external_exchange")}),session.AccessToken));Assert.That(external.StatusCode,Is.EqualTo(200));
                    Assert.That(state.CanExchangeTickets(2),Is.True);await state.ExchangeTicketsAsync(2);Assert.That(state.Pending,Is.Null);Assert.That(state.ActionErrorCode,Is.Not.Null);Assert.That(state.Wallet.TotalBalance,Is.EqualTo(8));Assert.That(state.CanExchangeTickets(1),Is.False);
                    metrics=JObject.Parse((await control("shop-metrics")).BodyJson);Assert.That((int)metrics["exchanges"],Is.EqualTo(2));Assert.That((int)metrics["tickets"],Is.EqualTo(7));
                    await control("shop-unconfigure");await state.RefreshWalletAsync();Assert.That(state.Wallet.Status,Is.EqualTo(CrystalWalletReadStatus.Unconfigured));Assert.That(state.CanExchangeTickets(1),Is.False);
                }finally{state.Changed-=redraw;}
            }finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}

