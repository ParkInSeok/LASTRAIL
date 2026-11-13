using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
public sealed class RunRecoveryIsolationTests {
 const string Response="{\"runId\":\"run_test\",\"rewardPreviewId\":\"preview_test\",\"rewardClaimId\":\"claim_test\",\"status\":\"CLAIMED\",\"balanceVersion\":\"1\",\"serverClaimedAt\":\"2026-09-11T00:00:00.000Z\",\"rewardLines\":[],\"errorCode\":null,\"message\":null}";
 sealed class Wire:IRunApiHttpTransport {public int Calls;public readonly TaskCompletionSource<RunApiHttpResponse> Reply=new TaskCompletionSource<RunApiHttpResponse>();public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request){Calls++;return Reply.Task;}}
 static GameApiAuthSessionSnapshot Session(string account)=>new GameApiAuthSessionSnapshot(account,"ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddMinutes(5),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddMinutes(10));
 static object Held(DurableRunClient client,string name)=>typeof(DurableRunClient).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(client);
 [TestCase(false)] [TestCase(true)] public async Task AccountChangeReleasesPriorReferencesWithoutReadingOrDeletingItsJournal(bool responseAlreadyReceived){
  var type=typeof(RunOperationJournalTests).GetNestedType("Native",BindingFlags.NonPublic);var native=(IAndroidKeystoreNativeStoragePort)Activator.CreateInstance(type,true);
  var journal=new EncryptedRunOperationJournal(native);var wire=new Wire();var session=Session("account_test");long generation=1;
  using var client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>session,()=>generation);
  var pending=client.ClaimAsync(RunRewardClaimRequest.Create("run_test","preview_test","original_claim_key"));
  if(responseAlreadyReceived){type.GetField("FailSave").SetValue(native,true);wire.Reply.SetResult(RunApiHttpResponse.Success(200,Response));await pending;Assert.AreEqual(1,((ICollection)Held(client,"awaitingStorage")).Count);}
  int loads=(int)type.GetField("Loads").GetValue(native),saves=(int)type.GetField("Saves").GetValue(native);var stored=type.GetField("Stored").GetValue(native);
  session=Session("account_other");generation++;
  var sync=typeof(DurableRunClient).GetMethod("SynchronizeSession");Assert.IsNotNull(sync,"Auth changes must release prior run references immediately, without waiting for a next command");sync.Invoke(client,null);sync.Invoke(client,null);
  Assert.IsEmpty((IEnumerable)Held(client,"operations"));Assert.IsEmpty((IEnumerable)Held(client,"awaitingStorage"));Assert.IsEmpty(client.Operations);
  Assert.AreEqual(loads,type.GetField("Loads").GetValue(native));Assert.AreEqual(saves,type.GetField("Saves").GetValue(native));Assert.AreSame(stored,type.GetField("Stored").GetValue(native));
  if(!responseAlreadyReceived){wire.Reply.SetResult(RunApiHttpResponse.Success(200,Response));Assert.IsNull(await pending);}
  Assert.IsEmpty(client.Operations);Assert.AreEqual(1,wire.Calls);Assert.AreEqual(0,type.GetField("Clears").GetValue(native));
  type.GetField("FailSave").SetValue(native,false);var restored=await journal.ReadAsync("account_test");Assert.AreEqual(1,restored.Count);Assert.AreEqual("original_claim_key",restored[0].IdempotencyKey);Assert.IsNull(restored[0].AcceptedResponseJson);
 }
}}
