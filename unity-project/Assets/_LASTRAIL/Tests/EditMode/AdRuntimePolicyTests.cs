using System;
using System.Threading.Tasks;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class AdRuntimePolicyTests
    {
        internal static string Policy(bool enabled,int revision=1)=>"{\"accepted\":true,\"contractVersion\":\"runtime-policy-v1\",\"environment\":\"development\",\"policyStatus\":\"CONFIGURED\",\"adsEnabled\":"+(enabled?"true":"false")+",\"runtimePolicyRevision\":"+revision+",\"serverTime\":1000}";
        sealed class Wire:IRunApiHttpTransport
        {
            public Func<Task<RunApiHttpResponse>> Reply;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request){Assert.That(request.EndpointPath,Is.EqualTo("/v1/runtime-policy"));Assert.That(request.Method,Is.EqualTo("GET"));return Reply();}
        }
        [Test] public async Task OffUnknownAndMalformedAreDifferentFromEnabled()
        {
            var owner=SupportInquiryTests.Session();var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,Policy(false)))};
            using var policy=new AdRuntimePolicyService(wire,()=>owner,()=>1);Assert.That(policy.Status,Is.EqualTo(AdRuntimePolicyStatus.Unknown));await policy.RefreshAsync();Assert.That(policy.Status,Is.EqualTo(AdRuntimePolicyStatus.Disabled));
            wire.Reply=()=>Task.FromResult(RunApiHttpResponse.NetworkError("TEST"));await policy.RefreshAsync();Assert.That(policy.Status,Is.EqualTo(AdRuntimePolicyStatus.Unknown));
            wire.Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,Policy(true,2)));await policy.RefreshAsync();Assert.That(policy.Status,Is.EqualTo(AdRuntimePolicyStatus.Enabled));
            wire.Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,Policy(false)));await policy.RefreshAsync();Assert.That(policy.Status,Is.EqualTo(AdRuntimePolicyStatus.Unknown),"Older revision must not replace current policy");
        }
        [Test] public async Task LateOldReadCannotEnableNewOwnerOrSupersedeNewerRequest()
        {
            var owner=SupportInquiryTests.Session();var wait=new TaskCompletionSource<RunApiHttpResponse>();var wire=new Wire{Reply=()=>wait.Task};using var policy=new AdRuntimePolicyService(wire,()=>owner,()=>1);
            var old=policy.RefreshAsync();wire.Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,Policy(false,2)));await policy.RefreshAsync();wait.SetResult(RunApiHttpResponse.Success(200,Policy(true)));await old;Assert.That(policy.Status,Is.EqualTo(AdRuntimePolicyStatus.Disabled));
            wait=new TaskCompletionSource<RunApiHttpResponse>();wire.Reply=()=>wait.Task;old=policy.RefreshAsync();owner=SupportInquiryTests.Session();policy.Reset();wait.SetResult(RunApiHttpResponse.Success(200,Policy(true,3)));await old;Assert.That(policy.Status,Is.EqualTo(AdRuntimePolicyStatus.Unknown));
        }
        [Test] public async Task BackgroundInvalidationAndRevisionFactsFailClosed()
        {
            var owner=SupportInquiryTests.Session();var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,Policy(true,2)))};using var policy=new AdRuntimePolicyService(wire,()=>owner,()=>1);
            await policy.RefreshAsync();policy.Invalidate();Assert.That(policy.Status,Is.EqualTo(AdRuntimePolicyStatus.Unknown));
            wire.Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,Policy(false,2)));await policy.RefreshAsync();Assert.That(policy.Status,Is.EqualTo(AdRuntimePolicyStatus.Unknown));
            owner=SupportInquiryTests.Session();wire.Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,Policy(false,1)));await policy.RefreshAsync();Assert.That(policy.Status,Is.EqualTo(AdRuntimePolicyStatus.Disabled));
        }
    }
}
