using System;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Diagnostics;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode { public sealed class RunReceiptIdentityTests {
 sealed class Wire:IRunApiHttpTransport{public string Json;public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r)=>Task.FromResult(RunApiHttpResponse.Success(200,Json));}
 [Test] public async Task ForeignClaimIdentityNeverDisplaysReceipt(){
  var data=(JObject)typeof(RunLifecycleTests).GetMethod("Data",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{"CLAIMED"});
  var session=SupportInquiryTests.Session();var wire=new Wire{Json=data.ToString()};
  var port=(IAndroidKeystoreNativeStoragePort)Activator.CreateInstance(typeof(AccountScreenIntegrationTests).GetNestedType("SecurePort",BindingFlags.NonPublic),true);var journal=new EncryptedRunOperationJournal(port);
  var response=new JObject{["runId"]="run_test",["rewardPreviewId"]="preview",["rewardClaimId"]="different_claim",["status"]="CLAIMED",["balanceVersion"]="1",["serverClaimedAt"]="2026-09-11T00:00:00.000Z",["rewardLines"]=new JArray()};
  var pending=PendingRunOperation.Claim(session.AccountId,RunRewardClaimRequest.Create("run_test","preview","original_claim_key"),new AppBuildSnapshot("1",100),DateTimeOffset.UtcNow);Assert.That(await journal.PutAsync(pending),Is.True);var saved=pending.Confirmed(response.ToString());Assert.That(await journal.ConfirmAsync(pending,response.ToString()),Is.True);
  using var runs=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>session,()=>1);using var state=new AccountScreenRuntime(wire,null,()=>session,()=>1);state.BindRunRecovery(runs);
  await state.ReadRunResultsAsync("run_test");Assert.That(state.VerifiedRunReceipt,Is.Null);Assert.That(state.RunResultNotice,Does.Contain("확인하지 못했습니다"));Assert.That((await journal.ReadAsync(session.AccountId))[0].AcceptedResponseJson,Is.EqualTo(saved.AcceptedResponseJson));
 }
} }
