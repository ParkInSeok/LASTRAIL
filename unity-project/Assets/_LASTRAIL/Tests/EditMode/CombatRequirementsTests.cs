using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode
{
    public sealed class CombatRequirementsTests
    {
        static JObject ProductQuote()=>JObject.Parse(System.IO.File.ReadAllText("D:/Project/LASTRAIL/QA/OperatorInformation-20261019/Departure549/server-quote.json"));
        static string ProductRequest()=>new JObject{["operationId"]="opn_01_last_departure",["risk"]="I",["expectedFormationRevision"]=1,["expectedLoadoutRevision"]=1,["expectedUltimateRevision"]=1}.ToString();
        sealed class PreviewJournal:IAccountActionJournal
        {
            public PendingAccountAction Saved=new PendingAccountAction("account_test","departure-preview","opn_01_last_departure",0,"preview_original",ProductRequest());
            public PendingAccountAction Read(string account)=>Saved;public bool Save(PendingAccountAction value){Saved=value;return true;}
            public bool Remove(PendingAccountAction value){Saved=null;return true;}
        }
        sealed class PreviewWire:IRunApiHttpTransport
        {
            public int Calls;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request){Calls++;Assert.AreEqual("POST",request.Method);Assert.AreEqual("/v1/operations/preview",request.EndpointPath);return Task.FromResult(RunApiHttpResponse.Success(200,ProductQuote().ToString()));}
        }
        [Test] public async Task BlockedProductPreviewShowsSupportReasonAndRetainsQuoteWithoutFollowupReads()
        {
            var wire=new PreviewWire();var journal=new PreviewJournal();var session=SupportInquiryTests.Session();
            using var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1);
            await state.RetryPendingAsync();Assert.IsNotNull(state.Departure);Assert.IsNull(state.Pending);
            Assert.That(state.ActionNotice,Does.Contain("현재 버전에서는 출격할 수 없습니다"));
            state.BattleEntryReady=()=>true;state.ExistingRunRecoveryClear=()=>true;
            await state.ConfirmDepartureAsync();Assert.IsFalse(state.CanConfirmDeparture);Assert.AreEqual(1,wire.Calls);
            Assert.That(state.ActionNotice,Does.Not.Contain("combat_effect"));
        }
        [Test] public void CurrentServerDiagnosticQuoteIsRetainedWithoutGrantingCombat()
        {
            var quote=new DepartureQuote(ProductQuote().ToString(),ProductRequest());
            Assert.IsTrue(quote.CurrentVersion);Assert.IsFalse(quote.Combat.CanStart);
            Assert.That(quote.Combat.BlockingReasons,Does.Contain("combat_effect_handler_missing:SENTRY_CREATE"));
            Assert.AreEqual("op_eve",quote.Combat.UltimateOperatorId);
        }
        [TestCase("departure-v2")][TestCase("departure-v3")]
        public void HistoricalDiagnosticQuotesKeepTheirPinnedSelection(string version)
        {
            var root=ProductQuote();var admission=(JObject)root["admission"];var combat=(JObject)root["combatContract"];
            admission["version"]=version;admission.Remove("combatPolicyRequirements");admission.Remove("combatAuthority");
            combat["blockingReasons"]=new JArray(((JArray)combat["blockingReasons"]).Where(x=>!((string)x).StartsWith("combat_effect_handler_missing:")).Select(x=>x.DeepClone()));
            if(version=="departure-v2"){combat["version"]="run-combat-requirements-v2";combat.Remove("policyBinding");combat.Remove("authority");}
            else {combat["policyBinding"]=new JObject{["version"]="combat.policy-binding.v1",["status"]="HISTORICAL",["canExecute"]=false,["policyVersion"]=null,["operators"]=new JArray(),["requiredEffectKinds"]=new JArray(),["blockingReasons"]=new JArray()};combat["authority"]=new JObject{["status"]="HISTORICAL",["contract"]=null};}
            var original=root.ToString();var quote=new DepartureQuote(original,ProductRequest());
            Assert.IsFalse(quote.Combat.CanStart);Assert.AreEqual("op_eve",quote.Combat.UltimateOperatorId);
            Assert.IsTrue(JToken.DeepEquals(admission,quote.Admission));Assert.AreEqual(original,root.ToString());
        }
        [TestCase("canStart")][TestCase("policy")][TestCase("authority")][TestCase("ultimate")]
        public void ProductDiagnosticCannotClaimExecutionOrChangePinnedIdentity(string mutation)
        {
            var root=ProductQuote();var combat=(JObject)root["combatContract"];
            if(mutation=="canStart")combat["canStart"]=true;
            if(mutation=="policy")combat["policyBinding"]["policyVersion"]="foreign";
            if(mutation=="authority")combat["authority"]["contract"]["combatOwner"]="SERVER";
            if(mutation=="ultimate")combat["ultimateOperatorId"]="op_kyle";
            Assert.Throws<FormatException>(()=>new DepartureQuote(root.ToString(),ProductRequest()));
        }
        [Test] public void RecoveryPreservesOldResponseAndUnavailableDoesNotMeanEmptySuccess()
        {
            var original=new JObject{["accepted"]=true,["startStatus"]="STARTED",["runId"]="run_old",["oldField"]="pinned"};
            var root=new JObject{["accepted"]=true,["runId"]="run_old",["runStatus"]="COMPLETED",["recoveryStatus"]="AVAILABLE",["startResponse"]=original};
            var recovery=new DepartureRecoverySnapshot(root.ToString(),"run_old");Assert.That(JToken.DeepEquals(recovery.StartResponse,original),Is.True);var copy=recovery.StartResponse;copy["oldField"]="changed";Assert.That((string)recovery.StartResponse["oldField"],Is.EqualTo("pinned"));
            root["recoveryStatus"]="SNAPSHOT_UNAVAILABLE";Assert.Throws<FormatException>(()=>new DepartureRecoverySnapshot(root.ToString(),"run_old"));root["startResponse"]=null;Assert.That(new DepartureRecoverySnapshot(root.ToString(),"run_old").Available,Is.False);
            root["runId"]="foreign";Assert.Throws<FormatException>(()=>new DepartureRecoverySnapshot(root.ToString(),"run_old"));
        }
        sealed class Wire:IRunApiHttpTransport
        {public readonly List<TaskCompletionSource<RunApiHttpResponse>> Replies=new List<TaskCompletionSource<RunApiHttpResponse>>();public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r){Assert.That(r.Method,Is.EqualTo("GET"));Assert.That(r.BodyJson,Is.Null);var t=new TaskCompletionSource<RunApiHttpResponse>();Replies.Add(t);return t.Task;}}
        [TestCase(false)][TestCase(true)] public async Task RecoveryIgnoresOldSuccessOrFailure(bool failure)
        {
            var session=new GameApiAuthSessionSnapshot("account_test","ACTIVE",new GameApiAccessToken("lst1.local"),DateTime.UtcNow.AddMinutes(5),new GameApiRefreshToken("local"),DateTime.UtcNow.AddMinutes(10));var wire=new Wire();
            using(var state=new AccountScreenRuntime(wire,null,()=>session,()=>1)){
                var old=state.RefreshDepartureRecoveryAsync("run_old");var current=state.RefreshDepartureRecoveryAsync("run_current");
                wire.Replies[1].SetResult(RunApiHttpResponse.Success(200,"{\"accepted\":true,\"runId\":\"run_current\",\"runStatus\":\"COMPLETED\",\"recoveryStatus\":\"SNAPSHOT_UNAVAILABLE\",\"startResponse\":null}"));await current;
                wire.Replies[0].SetResult(failure?RunApiHttpResponse.NetworkError("old failure"):RunApiHttpResponse.Success(200,"{\"accepted\":true,\"runId\":\"run_old\",\"runStatus\":\"ACTIVE\",\"recoveryStatus\":\"SNAPSHOT_UNAVAILABLE\",\"startResponse\":null}"));await old;
                Assert.That(state.DepartureRecovery.RunId,Is.EqualTo("run_current"));Assert.That(state.DepartureRecoveryError,Is.Null);Assert.That(state.DepartureRecoveryLoading,Is.False);
            }
        }
    }
}
