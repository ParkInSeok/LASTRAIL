using LastRail.Presentation.MissionShell;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Tests.EditMode { public sealed class ShopExchangeTests {
 [Test] public void ShopExposesDurableTicketExchange(){Assert.That(typeof(AccountScreenRuntime).GetMethod("ExchangeTicketsAsync"),Is.Not.Null);}
 static string Body(int count=1)=>new JObject{["policyVersion"]="test-policy",["ticketCount"]=count}.ToString();
 static JObject Receipt()=>new JObject{["accepted"]=true,["operationId"]="receipt_1",["currencyId"]="test_crystal",["policyVersion"]="test-policy",["ticketItemId"]="cur_command_ticket",["ticketsGranted"]=1,["ticketBalanceAfter"]=1,["freeBalance"]=0,["paidBalance"]=10,["totalBalance"]=10,["freeSpent"]=11,["paidSpent"]=0,["freeGranted"]=0,["paidGranted"]=0};
 [Test] public void StrictRequestAndReceiptRejectAmountOrPolicyMismatch(){
  Assert.DoesNotThrow(()=>new TicketExchangeReceipt(Receipt().ToString(),Body()));
  foreach(var mutate in new Action<JObject>[]{r=>r["policyVersion"]="other",r=>r["ticketsGranted"]=2,r=>r["freeGranted"]=1,r=>r["totalBalance"]=11,r=>r["ticketItemId"]="invented"}){var r=Receipt();mutate(r);Assert.Throws<FormatException>(()=>new TicketExchangeReceipt(r.ToString(),Body()));}
  Assert.Throws<FormatException>(()=>TicketExchangeReceipt.Validate("cur_command_ticket",Body(0)));
  Assert.Throws<FormatException>(()=>TicketExchangeReceipt.Validate("cur_command_ticket","{\"policyVersion\":\"bad policy\",\"ticketCount\":1}"));
  var body=JObject.Parse(Body());body["price"]=1;Assert.Throws<FormatException>(()=>TicketExchangeReceipt.Validate("cur_command_ticket",body.ToString()));
 }
 sealed class Journal:IAccountActionJournal { public PendingAccountAction Row;public PendingAccountAction Read(string id)=>Row?.Account==id?Row:null;public bool Save(PendingAccountAction a){Row=a;return true;}public bool Remove(PendingAccountAction a){Row=null;return true;} }
 sealed class Wire:IRunApiHttpTransport {public readonly List<RunApiHttpRequest> Requests=new List<RunApiHttpRequest>();public Func<RunApiHttpRequest,Task<RunApiHttpResponse>> Reply;public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r){Requests.Add(r);return Reply(r);} }
 static GameApiAuthSessionSnapshot Session()=>new GameApiAuthSessionSnapshot("account_test","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddMinutes(5),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddMinutes(10));
 [Test] public async Task UnknownCompletionRetainsOriginalRequestAndMalformedSuccessCannotAcknowledge(){
  var action=new PendingAccountAction("account_test","ticket-exchange","cur_command_ticket",0,"original:/+",Body());var journal=new Journal{Row=action};var wire=new Wire{Reply=r=>Task.FromResult(RunApiHttpResponse.NetworkError("lost"))};var session=Session();using var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1);
  await state.RetryPendingAsync();Assert.That(journal.Row,Is.SameAs(action));Assert.That(state.LastTicketExchange,Is.Null);
  wire.Reply=r=>Task.FromResult(RunApiHttpResponse.Success(200,"{\"accepted\":true}"));await state.RetryPendingAsync();Assert.That(journal.Row,Is.SameAs(action));
  Assert.That(wire.Requests.Count,Is.EqualTo(2));foreach(var r in wire.Requests){Assert.That(r.BodyJson,Is.EqualTo(action.Payload));Assert.That(r.EndpointPath,Is.EqualTo("/v1/wallet/crystals/tickets/exchange"));Assert.That(System.Linq.Enumerable.Single(r.Headers,h=>h.Name=="Idempotency-Key").Value,Is.EqualTo(action.Key));}
 }
 [Test] public async Task ChangedOwnerCannotDeleteOriginalExchangeReceipt(){
  var action=new PendingAccountAction("account_test","ticket-exchange","cur_command_ticket",0,"old_key",Body());var journal=new Journal{Row=action};var promise=new TaskCompletionSource<RunApiHttpResponse>();var wire=new Wire{Reply=r=>promise.Task};var session=Session();long generation=1;using var state=new AccountScreenRuntime(wire,journal,()=>session,()=>generation);
  var running=state.RetryPendingAsync();generation++;state.SynchronizeSession();promise.SetResult(RunApiHttpResponse.Success(200,Receipt().ToString()));await running;Assert.That(journal.Row,Is.SameAs(action));Assert.That(state.LastTicketExchange,Is.Null);
 }
 [Test] public async Task UnconfiguredWalletNeverSendsExchange(){
  var wire=new Wire{Reply=r=>Task.FromResult(RunApiHttpResponse.NetworkError("none"))};var session=Session();using var state=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1);await state.ExchangeTicketsAsync(1);Assert.That(wire.Requests.Count,Is.Zero);
 }
} }
