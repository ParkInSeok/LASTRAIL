using System;
using System.Threading.Tasks;
using System.Reflection;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Diagnostics;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode { public sealed class RunRewardOperationTests {
 const string Response="{\"runId\":\"run_test\",\"rewardPreviewId\":\"preview_test\",\"rewardClaimId\":\"claim_test\",\"status\":\"DUPLICATE_REPLAY\",\"balanceVersion\":\"1\",\"serverClaimedAt\":\"2026-09-11T00:00:00.000Z\",\"rewardLines\":[],\"errorCode\":\"duplicate_replay\",\"message\":\"Reward claim was already committed.\"}";
 [Test] public void ClaimPreservesExactBodyAndReplayEnvelope(){var req=RunRewardClaimRequest.Create("run_test","preview_test","original_claim_key");var op=PendingRunOperation.Claim("account_test",req,new AppBuildSnapshot("1",100),DateTimeOffset.UtcNow);var done=op.Confirmed(Response);Assert.That(done.BodyJson,Is.EqualTo(req.BodyJson));Assert.That(done.AcceptedResponseJson,Is.EqualTo(Response));Assert.That(done.Kind,Is.EqualTo(PendingRunKind.Claim));var other=JObject.Parse(Response);other["rewardPreviewId"]="foreign";Assert.Throws<FormatException>(()=>op.Confirmed(other.ToString()));other=JObject.Parse(Response);other["rewardClaimId"]=null;Assert.Throws<FormatException>(()=>op.Confirmed(other.ToString()));}
 sealed class DelayedWire:IRunApiHttpTransport {public readonly TaskCompletionSource<RunApiHttpResponse> Reply=new TaskCompletionSource<RunApiHttpResponse>();public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)=>Reply.Task;}
 [Test] public async Task OwnerSwitchKeepsUnconfirmedClaimInOriginalEncryptedJournal(){var port=(IAndroidKeystoreNativeStoragePort)Activator.CreateInstance(typeof(AccountScreenIntegrationTests).GetNestedType("SecurePort",BindingFlags.NonPublic),true);var journal=new EncryptedRunOperationJournal(port);var wire=new DelayedWire();var session=new GameApiAuthSessionSnapshot("account_test","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddMinutes(5),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddMinutes(10));long generation=1;using var client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>session,()=>generation);var task=client.ClaimAsync(RunRewardClaimRequest.Create("run_test","preview_test","original_claim_key"));generation++;wire.Reply.SetResult(RunApiHttpResponse.Success(200,Response));Assert.That(await task,Is.Null);Assert.That(client.Operations,Is.Empty);var stored=await journal.ReadAsync("account_test");Assert.That(stored.Count,Is.EqualTo(1));Assert.That(stored[0].AcceptedResponseJson,Is.Null);Assert.That(stored[0].IdempotencyKey,Is.EqualTo("original_claim_key"));} }
}

