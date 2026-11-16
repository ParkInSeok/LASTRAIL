using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class ShopScreenControllerTests
    {
        sealed class Journal:IAccountActionJournal
        {
            PendingAccountAction row;
            public PendingAccountAction Read(string account)=>row;
            public bool Save(PendingAccountAction action){row=action;return true;}
            public bool Remove(PendingAccountAction action){row=null;return true;}
        }
        sealed class Wire:IRunApiHttpTransport
        {
            public readonly List<RunApiHttpRequest> Requests=new List<RunApiHttpRequest>();
            public Func<RunApiHttpRequest,Task<RunApiHttpResponse>> Reply=request=>Task.FromResult(request.EndpointPath=="/v1/shop"?RunApiHttpResponse.Success(200,ShopJson().ToString()):RunApiHttpResponse.NetworkError("lost"));
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request){Requests.Add(request);return Reply(request);}
        }
        static GameApiAuthSessionSnapshot Session()=>new GameApiAuthSessionSnapshot("account_test","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddMinutes(5),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddMinutes(10));
        static JObject ShopJson(int dailyRemaining=1)=>new JObject{
            ["accepted"]=true,["version"]="shop-v1",["policyVersion"]="fixture-policy",
            ["serverTime"]=DateTimeOffset.Parse("2026-09-01T00:00:00Z").ToUnixTimeMilliseconds(),
            ["dayStart"]=DateTimeOffset.Parse("2026-08-31T19:00:00Z").ToUnixTimeMilliseconds(),
            ["nextResetAt"]=DateTimeOffset.Parse("2026-09-01T19:00:00Z").ToUnixTimeMilliseconds(),
            ["wallet"]=new JObject{["currencyId"]="crystal",["policyVersion"]="fixture-policy",["freeBalance"]=15,["paidBalance"]=250,["totalBalance"]=265,["spendOrder"]=new JArray("FREE","PAID"),["ticketExchange"]=null,["purchaseConfigured"]=false},
            ["ticketUnavailableReason"]=null,
            ["ticketOffers"]=new JArray(
                new JObject{["offer"]="NORMAL",["ticketItemId"]="cur_command_ticket",["crystalsPerTicket"]=100,["maxTicketsPerRequest"]=3,["dailyLimit"]=null},
                new JObject{["offer"]="DAILY",["ticketItemId"]="cur_command_ticket",["crystalsPerTicket"]=50,["maxTicketsPerRequest"]=1,["dailyLimit"]=1,["dailyRemaining"]=dailyRemaining,["offerFamily"]="COMMAND_TICKET_DAILY"}),
            ["capabilities"]=new JObject{["materialShop"]="UNCONFIGURED",["cashPriceSource"]="STORE_REQUIRED",["crystalPurchaseVerifierAvailable"]=false,["passCatalog"]="/v1/recurring/state",["creditRecruitmentExchange"]="NOT_SUPPORTED"},["crystalProducts"]=new JArray()
        };

        [Test] public async Task QuoteAndCommandEligibilityShareFreeFirstCostAndBounds()
        {
            var wire=new Wire();var session=Session();using var account=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1);
            await account.RefreshShopAsync();Assert.That(account.ShopReady,Is.True,account.ShopError);
            var quote=account.Shop.Quote("NORMAL",2);Assert.That(quote.Cost,Is.EqualTo(200));Assert.That(quote.FreeSpend,Is.EqualTo(15));Assert.That(quote.PaidSpend,Is.EqualTo(185));
            foreach(var offer in new[]{"NORMAL","DAILY"})foreach(var count in new[]{0,1,2,3,4,int.MaxValue})Assert.That(account.CanBuyShopOffer(offer,count),Is.EqualTo(account.Shop.Quote(offer,count).CanBuy));
            Assert.That(account.Shop.Quote("NORMAL",3).CanBuy,Is.False,"Insufficient wallet.");Assert.That(account.Shop.Quote("UNKNOWN",1),Is.Null);
            Assert.That(new ShopSnapshot(ShopJson(0).ToString()).Quote("DAILY",1).CanBuy,Is.False);
        }

        [Test] public async Task RebindKeepsOneRenderTargetAndOneCommandThenDisposeBlocksDispatch()
        {
            var wire=new Wire();var session=Session();using var account=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1);await account.RefreshShopAsync();
            var selection=new ShopScreenSelection();var controller=new ShopScreenController(account,selection);int oldRenders=0,newRenders=0;ShopScreenState shown=null;
            controller.Bind(_=>oldRenders++);controller.Bind(state=>{shown=state;newRenders++;});controller.ChangeCount(1);
            Assert.That(oldRenders,Is.EqualTo(1));Assert.That(newRenders,Is.EqualTo(2));Assert.That(shown.Spend,Does.Contain("합계 200"));Assert.That(shown.CanBuy,Is.True);
            await controller.BuyAsync();Assert.That(wire.Requests.Count(r=>r.EndpointPath=="/v1/shop/tickets/exchange"),Is.EqualTo(1));Assert.That(account.Pending,Is.Not.Null);
            controller.Dispose();int renders=newRenders,calls=wire.Requests.Count;controller.SelectOffer("DAILY");controller.ChangeCount(-1);await controller.RetryAsync();await controller.RefreshAsync();await controller.BuyAsync();
            Assert.That(newRenders,Is.EqualTo(renders));Assert.That(wire.Requests.Count,Is.EqualTo(calls));Assert.That(selection.Offer,Is.EqualTo("NORMAL"));Assert.That(selection.Count,Is.EqualTo(2));
            await account.RefreshShopAsync();Assert.That(newRenders,Is.EqualTo(renders),"Borrowed runtime remains usable without disposed rendering.");
        }

        [Test] public async Task ChangedSessionBlocksOldScreenCommandsAndRendering()
        {
            var wire=new Wire();var session=Session();long generation=1;using var account=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>generation);await account.RefreshShopAsync();
            var selection=new ShopScreenSelection();using var controller=new ShopScreenController(account,selection);int renders=0;controller.Bind(_=>renders++);int calls=wire.Requests.Count;
            generation++;controller.SelectOffer("DAILY");controller.ChangeCount(1);controller.Render();await controller.BuyAsync();await controller.RetryAsync();await controller.RefreshAsync();await controller.RefreshAdSkipAsync();
            Assert.That(wire.Requests.Count,Is.EqualTo(calls));Assert.That(renders,Is.EqualTo(1));Assert.That(selection.Offer,Is.EqualTo("NORMAL"));Assert.That(selection.Count,Is.EqualTo(1));
        }

        [Test] public async Task LateRefreshCannotRenderDisposedViewAndRecreatedControllerKeepsSelection()
        {
            var wire=new Wire();var session=Session();using var account=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1);await account.RefreshShopAsync();
            var selection=new ShopScreenSelection();var controller=new ShopScreenController(account,selection);controller.SelectOffer("DAILY");int renders=0;controller.Bind(_=>renders++);
            var completion=new TaskCompletionSource<RunApiHttpResponse>();wire.Reply=_=>completion.Task;var pending=account.RefreshShopAsync();controller.Dispose();int before=renders;
            completion.SetResult(RunApiHttpResponse.Success(200,ShopJson().ToString()));await pending;Assert.That(renders,Is.EqualTo(before));
            using var rebound=new ShopScreenController(account,selection);ShopScreenState shown=null;rebound.Bind(state=>shown=state);Assert.That(shown.NormalSelected,Is.False);Assert.That(shown.Count,Is.EqualTo("1장"));Assert.That(shown.Spend,Does.Contain("유료 35"));
        }
    }
}
