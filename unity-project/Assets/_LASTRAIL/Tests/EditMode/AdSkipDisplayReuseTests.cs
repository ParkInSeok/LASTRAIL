using System.Threading.Tasks;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public class AdSkipDisplayReuseTests {
  sealed class Wire:IRunApiHttpTransport {
   internal int Skip,Policy;internal Task Wait=Task.CompletedTask;
   public async Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request){
    Assert.AreEqual("GET",request.Method);bool skip=request.EndpointPath=="/v1/ad-skip";if(skip)Skip++;else{Assert.AreEqual("/v1/runtime-policy",request.EndpointPath);Policy++;}
    await Wait;return RunApiHttpResponse.Success(200,skip?AdSkipUseTests.Sample("17-revoked"):AdRuntimePolicyTests.Policy(false));
   }
  }
  [Test] public async Task DisplayReusesOwnersButExplicitRefreshAndInvalidationStillRead(){
   var session=SupportInquiryTests.Session();var wire=new Wire();using var state=new AccountScreenRuntime(wire,null,()=>session,()=>1);
   await state.EnsureAdSkipDisplayAsync();Assert.NotNull(state.AdSkip.Snapshot);await state.EnsureAdSkipDisplayAsync();Assert.AreEqual(1,wire.Skip);Assert.AreEqual(1,wire.Policy);
   state.RuntimeAds.Invalidate();await state.EnsureAdSkipDisplayAsync();Assert.AreEqual(1,wire.Skip);Assert.AreEqual(2,wire.Policy);
   await state.RefreshAdSkipAsync();Assert.AreEqual(2,wire.Skip);Assert.AreEqual(3,wire.Policy);
   session=SupportInquiryTests.Session("other");await state.EnsureAdSkipDisplayAsync();Assert.AreEqual(3,wire.Skip);Assert.AreEqual(4,wire.Policy);
  }
  [Test] public async Task RepeatedDisplayDoesNotRestartPendingReads(){
   var session=SupportInquiryTests.Session();var gate=new TaskCompletionSource<bool>();var wire=new Wire{Wait=gate.Task};using var state=new AccountScreenRuntime(wire,null,()=>session,()=>1);
   var first=state.EnsureAdSkipDisplayAsync();await state.EnsureAdSkipDisplayAsync();Assert.AreEqual(1,wire.Skip);Assert.AreEqual(1,wire.Policy);
   gate.SetResult(true);await first;Assert.NotNull(state.AdSkip.Snapshot);Assert.AreEqual(AdRuntimePolicyStatus.Disabled,state.RuntimeAds.Status);
  }
 }
}
