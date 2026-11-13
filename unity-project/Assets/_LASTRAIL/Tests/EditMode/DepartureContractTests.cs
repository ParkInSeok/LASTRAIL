using LastRail.Presentation.MissionShell;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Tests.EditMode
{
    public sealed class DepartureContractTests
    {
        [Test] public void UnknownStartHasDistinctReadOnlyLookupAndDurableReceipt()
        {Assert.That(typeof(AccountScreenRuntime).GetMethod("LookupDepartureStartAsync"),Is.Not.Null);Assert.That(typeof(PendingAccountAction).GetProperty("StartResultJson"),Is.Not.Null);}
        [Test] public void BlockedCombatSupportsReadOnlyOriginalRunRecovery()
        {Assert.That(typeof(AccountScreenRuntime).GetMethod("RefreshDepartureRecoveryAsync"),Is.Not.Null);Assert.That(typeof(DepartureQuote).GetProperty("Combat"),Is.Not.Null);}
        [Test] public void DepartureRequiresServerQuoteAndExplicitStart()
        {
            Assert.That(typeof(AccountScreenRuntime).GetMethod("PreviewDepartureAsync"),Is.Not.Null);
            Assert.That(typeof(AccountScreenRuntime).GetMethod("ConfirmDepartureAsync"),Is.Not.Null);
        }
        [Test] public void PreviewRequestRejectsInventedStatsRiskOperationsAndZeroRevisions()
        {
            var r=new JObject{["operationId"]=DepartureContract.Operations[0],["risk"]="I",["expectedFormationRevision"]=1,["expectedLoadoutRevision"]=1};
            Assert.DoesNotThrow(()=>new PendingAccountAction("account_test","departure-preview",DepartureContract.Operations[0],0,"original_key",r.ToString()));
            foreach(var change in new Action<JObject>[]{x=>x["fuelCost"]=0,x=>x["risk"]="VI",x=>x["operationId"]="invented",x=>x["expectedLoadoutRevision"]=0,x=>x["expectedFormationRevision"]=1.5}){
                var copy=(JObject)r.DeepClone();change(copy);Assert.Throws<FormatException>(()=>DepartureContract.Validate("departure-preview",DepartureContract.Operations[0],copy.ToString()));
            }
            Assert.DoesNotThrow(()=>DepartureContract.Validate("departure-start","preview_a","{\"operationPreviewId\":\"preview_a\"}"));
            Assert.Throws<FormatException>(()=>DepartureContract.Validate("departure-start","preview_a","{\"operationPreviewId\":\"preview_a\",\"seed\":1}"));
        }
        sealed class Journal:IAccountActionJournal
        {public PendingAccountAction Saved;public PendingAccountAction Read(string id)=>Saved;public bool Save(PendingAccountAction a){Saved=a;return true;}public bool Remove(PendingAccountAction a){Saved=null;return true;}}
        sealed class Wire:IRunApiHttpTransport
        {public int Count;public readonly TaskCompletionSource<RunApiHttpResponse> Reply=new TaskCompletionSource<RunApiHttpResponse>();public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r){Count++;return Reply.Task;}}
        static GameApiAuthSessionSnapshot Session()=>new GameApiAuthSessionSnapshot("account_test","ACTIVE",new GameApiAccessToken("lst1.local-test"),DateTime.UtcNow.AddMinutes(5),new GameApiRefreshToken("local-test"),DateTime.UtcNow.AddMinutes(10));
        [Test] public async Task PersistedButNeverSentStartCannotDebitWhenCombatIsUnavailable()
        {
            var s=Session();var wire=new Wire();var journal=new Journal{Saved=new PendingAccountAction("account_test","departure-start","preview_a",0,"original_key","{\"operationPreviewId\":\"preview_a\"}")};
            using(var state=new AccountScreenRuntime(wire,journal,()=>s,()=>1)){
                var task=state.RetryPendingAsync();Assert.That(wire.Count,Is.Zero);await task;Assert.That(journal.Saved.Key,Is.EqualTo("original_key"));Assert.That(state.DepartureStarted,Is.Null);
            }
        }
        [Test] public async Task OldSessionPreviewRejectionCannotDeleteOriginalRequest()
        {
            var s=Session();long generation=1;var wire=new Wire();var body=new JObject{["operationId"]=DepartureContract.Operations[0],["risk"]="I",["expectedFormationRevision"]=1,["expectedLoadoutRevision"]=1}.ToString();
            var journal=new Journal{Saved=new PendingAccountAction("account_test","departure-preview",DepartureContract.Operations[0],0,"original_key",body)};
            using(var state=new AccountScreenRuntime(wire,journal,()=>s,()=>generation)){
                var task=state.RetryPendingAsync();Assert.That(wire.Count,Is.EqualTo(1));generation++;s=Session();state.SynchronizeSession();
                wire.Reply.SetResult(RunApiHttpResponse.Success(409,"{\"accepted\":false,\"errorCode\":\"departure_selection_changed\"}"));await task;
                Assert.That(journal.Saved.Key,Is.EqualTo("original_key"));Assert.That(state.ActionErrorCode,Is.Null);
            }
        }
    }
}
